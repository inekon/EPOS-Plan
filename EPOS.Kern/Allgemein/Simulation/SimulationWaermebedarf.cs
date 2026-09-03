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

        // Solare Wärme        
        private float[] Sol_N = new float[365];
        private float[] Sol_w = new float[365];
        private float[] Sol_O = new float[365];
        private float[] Sol_S = new float[365];
        private float[] A_Temp = new float[365];
        private bool[] WE = new bool[365];

        private int[] TagTyp_W = new int[365];
        private int[] TagTyp_NW = new int[365];
        public float[] Solare_Gewinne = new float[365];

        // Gebäudeprofil Wärmebedarf
        public float[] Waermebedarf = new float[8760];
        public float[] Waermebedarf_Gebaeude = new float[8760];
        public float[] Waermebedarf_Gebaeude_Monat = new float[12];
        public float[] HeizwaermebedarfGeb = new float[100];
        public float[] Waermebedarf_sortiert = new float[8760];
        public float Waermebedarf_Max = 0;
        public float Waermebedarf_Gesamt = 0;
        public double Waermebedarf_Gebaeude_Gesamt = 0;


        // Brauchwasser Wärmeenergie 
        public float[] Waermebedarf_Brauchwasser_Monat = new float[12];
        public double Waermebedarf_Brauchwasser = 0;

        // Lastgang Gebäude
        public float[] Waermebedarf_Extern = new float[8760];
        public double Waermebedarf_Extern_Gesamt = 0;

        // Prozesswärme
        public float[] Waermebedarf_Prozess_Monat = new float[12];
        public double Waermebedarf_Prozess = 0;

        // Temperaturgang Klimaregion
        public float[] Stundentemperatur = new float[8760];

        private float[] SpezWaermeverluste = new float[365];
        private float[] Heizlast = new float[365];
        private float[] TagesVerteilung = new float[240];
        private float[] MaxP = new float[100];
        public float[] Dauerlinie = new float[8760];
        public float[] Dauerlinie_nicht_sortiert = new float[8760];
        private bool[] F_Absenkung = new bool[365];

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
        public float[] prozesswerte = new float[8760];

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
        public float[] brauchwasserwerte = new float[8760];

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

        public SimulationWaermebedarf()
        {
            Classes.Simulation.Init init = new Classes.Simulation.Init();
            init.Monatswerte_berechnen(mo_anfang, mo_ende);
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
            float[] kanalHeizung = _kanaele.Heizung;

            // ENERGIEPROBE (Konzept 11.3): eine UNABHÄNGIGE Summe aller Bedarfsanteile,
            // in double und ohne Kanalzuordnung mitgeführt. Sie ist der Gegenwert, an dem
            // am Ende die Kanalsumme gemessen wird - eine Kanalzuordnung, die einen
            // Anteil verschluckt oder doppelt bucht, fällt genau hier auf.
            double[] probe = new double[8760];

            //  if (!DBGelesen)
            {
                KlimadatenCtrl ctrl_klima = new KlimadatenCtrl();
                ctrl_klima.ReadAll(ID_Klimaregion);
                for (int i = 0; i < ctrl_klima.rows; i++)
                {
                    Sol_N[i] = (float)ctrl_klima.items[i].m_Sol_Nord;
                    Sol_w[i] = (float)ctrl_klima.items[i].m_Sol_West;
                    Sol_O[i] = (float)ctrl_klima.items[i].m_Sol_Ost;
                    Sol_S[i] = (float)ctrl_klima.items[i].m_Sol_Sued;
                    A_Temp[i] = (float)ctrl_klima.items[i].m_nTemperatur;
                    WE[i] = (bool)ctrl_klima.items[i].m_WE;
                    TagTyp_W[i] = (int)ctrl_klima.items[i].m_TagTyp_W;
                    TagTyp_NW[i] = (int)ctrl_klima.items[i].m_TagTyp_NW;
                }
                Stundentemperatur_aus_DB(ID_Klimaregion);
                DBGelesen = true;
            }

            // F3 (Konzept 4.2): Der Klimadaten-Kalender ist ab Paket K1 für ALLE
            // Bedarfsarten führend. Die Profilkachelung startet damit mit dem
            // tatsächlichen Wochentag des 1. Januar statt fest mit Sonntag.
            WochentagJan1 = ProfilBedarf.WochentagJan1AusWE(WE);

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
            float[] Waermebedarf_EinGebaeude = new float[8760];

            for (int i = 0; i < ctrl.rows; i++)
            {

                // wenn die Einheit nicht als "Wohnfläche [m²]" angegeben ist...Wohnfläche und Anzahl Bewohner berechnen
                if (ctrl.items[i].Einheit == "Wohnfläche [m²]")
                {
                    ctrl.items[i].Bewohner = ctrl.items[i].Z_AuswahlWohnflaeche / ctrl.items[i].Flaeche_Nutzer;
                }
                else
                {
                    Bewohner_und_Flaeche_berechnen(ctrl.items[i], i);
                }
                Anzahl_Bewohner = (int)ctrl.items[i].Bewohner;
                Wohnflaeche = ctrl.items[i].Z_AuswahlWohnflaeche;

                // Tagesverteilung berechnen
                Berechnung_Gebaeude_Tageswerte(ctrl.items[i], i);

                bool tagv_found = false;
                TagesVerteilung = DBTagesVeteilung(ctrl.items[i].Typ, ctrl.items[i].ID_Gebaeude, ref tagv_found);
                // PAKET 8 (Konzept 13.4): Warnung im Protokollkanal statt MessageBox. Der
                // ABBRUCH der Bedarfsrechnung bleibt unverändert (return an derselben
                // Stelle) — die im Konzept genannte Ersatzlösung „Standardprofil
                // verwenden“ wäre eine Rechenänderung und gehört nicht in ein
                // Infrastrukturpaket (siehe Paket-8-Protokoll, offene Punkte).
                if (!tagv_found)
                {
                    SimulationProtokoll.Aktuell.Warnung(string.Format(
                        MyResource.Resource.SIMENG_TAGESVERTEILUNG_FEHLT, ctrl.items[i].Typ));
                    return;
                }

                // V0-1: Einzelpuffer je Durchlauf nullen - StdWerte addiert auf.
                WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_EinGebaeude);

                // Stundenwerte Wärmebedarf je nach Gebäudetyp und Tagtyp aus Klimaregion
                if (ctrl.items[i].Typ == "Wohngebaeude  VDI 2067")
                {
                    //com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_W, TagesVerteilung, Heizlast);
                    WPPlan.Core.BhkwPlan.StdWerte(Waermebedarf_EinGebaeude, TagTyp_W, TagesVerteilung, Heizlast);
                }
                else
                    //com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_NW, TagesVerteilung, Heizlast);
                    WPPlan.Core.BhkwPlan.StdWerte(Waermebedarf_EinGebaeude, TagTyp_NW, TagesVerteilung, Heizlast);

                //com.CSharp_I_vectoren_addieren(Waermebedarf_Gebaeude, Waermebedarf);
                // K1: Gebäudewärme geht in den HEIZKANAL statt in den Summenvektor.
                WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf_EinGebaeude, kanalHeizung);

                // Energieprobe: derselbe Betrag, unabhängig in double (noch in Watt).
                for (int h = 0; h < 8760; h++) probe[h] += Waermebedarf_EinGebaeude[h];

                // V0-1: Waermebedarf_Gebaeude bleibt die Summe ALLER Gebäude, wird aber
                // nicht mehr selbst als Rechenpuffer benutzt.
                WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf_EinGebaeude, Waermebedarf_Gebaeude);

                // Maximaler Wärmebedarf pro Gebäude (V0-1: des EINZELNEN Gebäudes i,
                // bisher versehentlich der kumulierte Vektor)
                MaxP[i] = Maximaler_Waermebedarf(Waermebedarf_EinGebaeude);

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
            float[] ganglinie_roh = new float[8760 * 4];
            float[] ganglinie = new float[8760];

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
                    if (index < ganglinie_roh.Length) ganglinie_roh[index] = (float)wert;
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
                        ganglinie[h] = (float)((ganglinie_roh[h * 4] + ganglinie_roh[h * 4 + 1]
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
            Waermebedarf_Brauchwasser = brauchwasserwerte.Sum() / 1000;
            //com.CSharp_I_vectoren_addieren(brauchwasserwerte, Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorenAddieren(brauchwasserwerte, _kanaele.Brauchwasser);
            for (int h = 0; h < 8760; h++) probe[h] += brauchwasserwerte[h];

            // Netzverluste
            //Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf);
            // K1: Der Summenvektor ist ab hier eine ABGELEITETE Größe. Für den
            // Netzverlust-Betrag wird er - wie bisher - VOR dem Aufschlag gebildet.
            SummenvektorAusKanaelen();
            Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;


            float stundl_netzverluste = 0;
            if (Netzverluste_Einheit == "%")
            {
                stundl_netzverluste = (Waermebedarf_Gesamt * 1000 * Netzverluste) / (float)876000;
                Waermebedarf_Netzverluste = (Waermebedarf_Gesamt * Netzverluste) / 100;
            }
            else
            {
                stundl_netzverluste = (float)Netzverluste / (float)8760;

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
            float[] summe = _kanaele.Summe();
            Array.Copy(summe, Waermebedarf, Kanalsatz.STUNDEN_JAHR);
        }

        /// <summary>
        /// Energieprobe je Stunde (Konzept 11.3): Die Kanalsumme muss der unabhängig in
        /// <c>double</c> geführten Summe aller Bedarfsanteile entsprechen — Gebäudewärme,
        /// externe Lastgänge, Prozess- und Brauchwasserprofile, Netzverluste.
        ///
        /// Der Maßstab ist die 1-ULP-Klasse (<see cref="Kanalsatz.ErhaltungOk"/>), gefasst
        /// über die <see cref="Kanalsatz.ERHALTUNG_SCHRITTE_SUMME"/> float-Speicherungen,
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

        private void Bewohner_und_Flaeche_berechnen(ProjektGebaeudeModel item, int index)
        {
            double VerbrauchNeu = 0.0;

            if (item.Einheit == "Ölverbrauch [l/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 10.08;
            }
            else if (item.Einheit == "Gasverbrauch [m³/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 11.48;
            }
            else if (item.Einheit == "Gasverbrauch [MWh/a] (Ho)")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad / 1.1 * 1000;
            }
            else if (item.Einheit == "Brennstoffverbrauch [MWh/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 1000;
            }
            else if (item.Einheit == "Verbrauch  [MWh/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * 1000;
            }

            if (item.Einheit == "Wohnfläche [m²]")
            {
                // item.Bewohner = item.AuswahlWohnflaeche / item.Flaeche_Nutzer;
                item.Bewohner = item.Wohnflaeche_gesamt / item.Flaeche_Nutzer;
            }
            else
            {
                item.Z_AuswahlWohnflaeche = item.Wohnflaeche_gesamt;

                Berechnung_Gebaeude_Tageswerte(item, index);
                double FlaecheAlt = item.Wohnflaeche_gesamt;
                //                double VerbrauchAlt = (BrauchwasserGeb[index] + HeizwaermebedarfGeb[index]) / 1000;
                double VerbrauchAlt = HeizwaermebedarfGeb[index] / 1000;
                double FlaecheNeu = VerbrauchNeu / VerbrauchAlt * FlaecheAlt;
                item.Z_AuswahlWohnflaeche = FlaecheNeu;
                item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;

            }
        }

        private float[] DBTagesVeteilung(string TagV_Type, int ID_Gebaeude, ref bool tagv_found)
        {
            float[] tagv = new float[192];
            RecordSet rs = new RecordSet();

            try
            {
                // BEFUND B1 (S7): "Tab_DBTagV.ID" war der Tabellen-, nicht der Sichtname -
                // in SQLite "no such column". Der Ausfall war STILL (nur die Warnung
                // "keine Daten hinterlegt"), die Tagesverteilung blieb leer. Die Sortierung
                // steht im Rumpf der Sicht (ORDER BY Tab_DBTagVDaten.ID) und traegt auch
                // durch dieses aeussere WHERE - an der migrierten Datenbank nachgemessen.
                rs.Open("select * from Abfrage_Tagverteilung where Bezeichner='" + TagV_Type + "' and ID=" + ID_Gebaeude);
                int n = 0;
                while (rs.Next())
                {
                    double val = (double)rs.Read("Verteilung");
                    tagv[n] = (float)val;
                    n++;
                }
                if (n > 0) tagv_found = true;
                return tagv;
            }
            finally { rs.Close(); }
        }

        private void Berechnung_Gebaeude_Tageswerte(ProjektGebaeudeModel item, int GebaeudeNr)
        {
            int WE_Absenkung = 0;
            int Ferien_Absenkung = 0;

            if (item.Raumsolltemperatur_Ferien < 1)
            {
                item.Ferien = 0; // Ferienabsenkung
            }

            for (int Tag = 0; Tag < 365; Tag++)
            {
                F_Absenkung[Tag] = false;
            }

            if (item.Ferien > 0.9)
            {
                if (item.Ferienbeginn_1 > 0 && item.Ferienbeginn_1 <= 365)
                {
                    for (int Tag = (int)item.Ferienbeginn_1; Tag < 365; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                    for (int Tag = 0; Tag < (int)item.Ferienende_1; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_2 > 0 && item.Ferienende_2 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_2 - 1; Tag < item.Ferienende_2; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_3 > 0 && item.Ferienende_3 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_3 - 1; Tag < item.Ferienende_3; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_4 > 0 && item.Ferienende_4 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_4 - 1; Tag < item.Ferienende_4; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
            }

            for (int Tag = 350; Tag < 365; Tag++)
            {
                /*
                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / (float)100;
                */
                Solare_Gewinne[Tag] = WPPlan.Core.BhkwPlan.SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / (float)100;

                /*
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                        (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                        (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                        (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                        (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                        (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;
                */
                SpezWaermeverluste[Tag] = WPPlan.Core.BhkwPlan.SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                       (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                       (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                       (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                       (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                       (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                       (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;

                WE_Absenkung = 0;
                if ((float)item.Raumsolltemperatur_Wochenende > 5)
                {
                    if (WE[Tag]) WE_Absenkung = 1; else WE_Absenkung = 0;
                }
                if (F_Absenkung[Tag]) Ferien_Absenkung = 1; else Ferien_Absenkung = 0;
                /*
                Heizlast[Tag] = com.I_TaeglHeizlastWG(Tag + 1,
                        WE_Absenkung,
                        (float)item.Raumsolltemperatur_Wochenende,
                        Ferien_Absenkung,
                        (float)item.Raumsolltemperatur_Ferien,
                        (float)item.Raumsolltemperatur_Tag,
                        (float)item.Raumsolltemperatur_Nachtabsenkung,
                        (float)item.Interne_Waermegewinne,
                        (float)Solare_Gewinne[Tag],
                        (float)SpezWaermeverluste[Tag],
                        (float)item.Bauweise,
                        (float)A_Temp[Tag],
                        (float)item.Maximaleraumtemperatur,
                        (float)item.Z_AuswahlWohnflaeche,
                        (float)item.Wohnflaeche);
                */
                Heizlast[Tag] = WPPlan.Core.BhkwPlan.TaeglHeizlastWG(Tag + 1,
                        WE_Absenkung,
                        (float)item.Raumsolltemperatur_Wochenende,
                        Ferien_Absenkung,
                        (float)item.Raumsolltemperatur_Ferien,
                        (float)item.Raumsolltemperatur_Tag,
                        (float)item.Raumsolltemperatur_Nachtabsenkung,
                        (float)item.Interne_Waermegewinne,
                        (float)Solare_Gewinne[Tag],
                        (float)SpezWaermeverluste[Tag],
                        (float)item.Bauweise,
                        (float)A_Temp[Tag],
                        (float)item.Maximaleraumtemperatur,
                        (float)item.Z_AuswahlWohnflaeche,
                        (float)item.Wohnflaeche);
            }

            HeizwaermebedarfGeb[GebaeudeNr] = 0;

            for (int Tag = 0; Tag < 365; Tag++)
            {
                /*
                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / 100;
                */
                Solare_Gewinne[Tag] = WPPlan.Core.BhkwPlan.SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                    (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                    (float)item.Fensterdurchlassgrad) / (float)100;
                /*
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                        (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                        (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                        (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                        (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                        (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;
                */
                SpezWaermeverluste[Tag] = WPPlan.Core.BhkwPlan.SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                     (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                     (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                     (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                     (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                     (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                     (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;

                WE_Absenkung = 0;
                if ((float)item.Raumsolltemperatur_Wochenende > 5)
                {
                    if (WE[Tag]) WE_Absenkung = 1; else WE_Absenkung = 0;
                }

                /*
                Heizlast[Tag] = (float)com.I_TaeglHeizlastWG(
                    Tag+1,
                    WE_Absenkung,
                    (float)item.Raumsolltemperatur_Wochenende,
                    Ferien_Absenkung,
                    (float)item.Raumsolltemperatur_Ferien,
                    (float)item.Raumsolltemperatur_Tag,
                    (float)item.Raumsolltemperatur_Nachtabsenkung,
                    (float)item.Interne_Waermegewinne,
                    (float)Solare_Gewinne[Tag],
                    (float)SpezWaermeverluste[Tag],
                    (float)item.Bauweise,
                    (float)A_Temp[Tag],
                    (float)item.Maximaleraumtemperatur,
                    (float)item.Z_AuswahlWohnflaeche,
                    (float)item.Wohnflaeche);
                */
                Heizlast[Tag] = WPPlan.Core.BhkwPlan.TaeglHeizlastWG(Tag + 1,
                      WE_Absenkung,
                      (float)item.Raumsolltemperatur_Wochenende,
                      Ferien_Absenkung,
                      (float)item.Raumsolltemperatur_Ferien,
                      (float)item.Raumsolltemperatur_Tag,
                      (float)item.Raumsolltemperatur_Nachtabsenkung,
                      (float)item.Interne_Waermegewinne,
                      (float)Solare_Gewinne[Tag],
                      (float)SpezWaermeverluste[Tag],
                      (float)item.Bauweise,
                      (float)A_Temp[Tag],
                      (float)item.Maximaleraumtemperatur,
                      (float)item.Z_AuswahlWohnflaeche,
                      (float)item.Wohnflaeche);

                HeizwaermebedarfGeb[GebaeudeNr] = HeizwaermebedarfGeb[GebaeudeNr] + Heizlast[Tag];
            }

        }

        private float Maximaler_Waermebedarf(float[] Waermebedarf)
        {
            float Waermebedarf_Max;

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
                Stundentemperatur[i] = (float)ctrldat.items[i].Außen_Temp;
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

                ProfilQuellmodus modus = (list == null) ? ProfilQuellmodus.Projektrechnung
                                                        : ProfilQuellmodus.Katalogvorschau;

                // F3: Die Projektrechnung folgt dem Klimadaten-Kalender, die Katalog-
                // vorschau der Altkonvention - sie kennt kein Projekt und keine
                // Klimaregion, und ihre Kurven sollen zwischen zwei Katalogeinträgen
                // vergleichbar bleiben.
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

                ProfilQuellmodus modus = (list == null) ? ProfilQuellmodus.Projektrechnung
                                                        : ProfilQuellmodus.Katalogvorschau;

                int wochentag = (modus == ProfilQuellmodus.Projektrechnung)
                                ? WochentagJan1 : ProfilBedarf.WOCHENTAG_ALTKONVENTION;

                ProfilBedarf.Rechnen(ProfilQuelle.Brauchwasser(modus), m_ID_Projekt, list,
                                     wochentag, mo_anfang, mo_ende,
                                     brauchwasserwerte, Waermebedarf_Brauchwasser_Monat);
            }
            // Protokollkanal-Nachzug: WARNUNG, siehe Prozesswärme-Zweig.
            catch (SystemException ex) { SimulationProtokoll.Aktuell.Warnung("Fehler bei der Brauchwasserwärme-Berechnung (Ergebnis unvollständig): " + ex.Message); }
        }
    }
}

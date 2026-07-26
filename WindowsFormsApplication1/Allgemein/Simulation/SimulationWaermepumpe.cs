using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class SimulationWaermepumpe
    {
        public const int MAX_WP = 10;

        public List<int> wp_list = new List<int>();
        public double Waermebedarf_gesamt;
        public float[] Waermebedarf_stuendlich = new float[8760];
        public float[] waermerestbedarf_stuendlich = new float[8760];
        public double waermerestbedarf_gesamt;

        public double[] WP_Strombedarf_monatlich = new double[12];
        public double[] WP_Waermeproduktion_monatlich = new double[12];
        public double[] Heizstab_monatlich = new double[12];
        public float[] Temperatur = new float[8760];

        public float[] WP_Strombedarf_stuendlich = new float[8760];
        public float[] WP_Waermeproduktion_stuendlich = new float[8760];
        public float[] WP_Waermeproduktion_stuendlich_sortiert = new float[8760];
        public float[] Heizstab_stuendlich = new float[8760];

        public double WP_Strombedarf_gesamt = 0;
        public double WP_Waermeproduktion_gesamt = 0;
        public double Heizstab_gesamt = 0;
        public double WP_Laufzeit = 0;

        public double[] Modul_WP_Strombedarf = new double[MAX_WP];
        public double[] Modul_WP_Waermeproduktion = new double[MAX_WP];
        public double[] Modul_Heizstab = new double[MAX_WP];
        public double[] Modul_WP_Laufzeit = new double[MAX_WP];

        public List<WErzeugerModel> wp_model = new List<WErzeugerModel>();
        private List<_Kenndaten> wp_kenndaten = new List<_Kenndaten>();

        // Quelltemperatur-Jahresprofil je WP-Modul (Wärmequelle):
        // Luft-Wasser = Außentemperatur; Sole-/Wasser-Wasser gemäß WQ_Typ
        // (Konstant, Pufferspeicher, Profil, CSV) - siehe WaermequelleClass.
        private List<float[]> wp_quelltemp = new List<float[]>();

        // Quell-Pufferspeicher je WP-Modul (Wärmequelle "Pufferspeicher");
        // null = keine Speicherbilanz (Außenluft, Konstant, Profil, CSV,
        // oder Quelle als unbegrenzt verfügbar konfiguriert).
        private List<SimulationPufferspeicher> wp_quellspeicher = new List<SimulationPufferspeicher>();

        // Wärmesenke je WP-Modul: Beides | Warmwasser | Heizung
        // (WS_Typ der Energieanlage; steuert, welchen Bedarfsanteil das Modul deckt)
        private List<string> wp_senke = new List<string>();

        // Betriebsmodus je WP-Modul: Laufzeit | Leistung | PV (BM_Typ)
        private List<string> wp_modus = new List<string>();

        /// <summary>
        /// Stündlicher PV-Überschuss [kW] für den Betriebsmodus "PV-optimiert".
        /// Wird von SimulationControl gesetzt; null = kein PV-Strom verfügbar.
        /// </summary>
        public float[] PV_Ueberschuss_stuendlich = null;

        /// <summary>
        /// Warmwasser-(Brauchwasser-)Anteil des Wärmebedarfs als Stundenganglinie.
        /// Wird von SimulationControl gesetzt und für die Wärmesenken-Aufteilung
        /// benötigt; null = kein Warmwasseranteil bekannt.
        /// </summary>
        public float[] Warmwasserbedarf_stuendlich = null;
        private string[] WP_Betriebsart = new string[MAX_WP];
        private int[] WP_Heizung = new int[MAX_WP];

        public double Bivalenzpunkt = -100;

        /// <summary>Zustand der Speicher-Hysterese: true = Speicher wird gerade geladen.</summary>
        private bool _speicherLaden = true;

        public bool Mit_Heizstab = false;
        public double Volumen_Pufferspeicher = 0;

        // Pufferspeicher-Integration (Stufe 1): wird von SimulationControl aus der
        // Zuordnung Z_ProjektPufferSp gesetzt; null = ohne Pufferspeicher rechnen.
        public SimulationPufferspeicher Pufferspeicher = null;
        private bool extrapolation = false;
        public string[] WP_Modul = new string[MAX_WP];  

        public class _Kenndaten
        {
            public int ID_WP = 0;
            public int Vorlauf = 0;
            public int anz = 0;
            public _DAT[] dat;
        }

        public class _DAT
        {
            public double Temperatur = 0;
            public double COP = 0;
            public double Leistung = 0;
        }

        public SimulationWaermepumpe()
        {
        }

        const int STATUS = 0;
        const int COP = 1;
        const int PTHERM = 2;
        const int PEL = 3;

        public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();

        public bool Berechnung()
        {
            RecordSet rs = new RecordSet();

            if (wp_list.Count >= 10) return false;

            WErzeugerCtrl wp = new WErzeugerCtrl();

            Cursor.Current = Cursors.WaitCursor;

            Volumen_Pufferspeicher = 0;
            List<double> biv = new List<double>();

            Init();

            wp_model.Clear();
            wp_kenndaten.Clear();
            wp_quelltemp.Clear();
            wp_quellspeicher.Clear();
            wp_senke.Clear();
            wp_modus.Clear();
            for (int i = 0; i < wp_list.Count; i++)
            {
                wp.ReadAllFilter("ID=" + wp_list[i]);
                WErzeugerModel model = wp.items[0];
         
                WP_Betriebsart[i] = model.Betriebsart != null ? model.Betriebsart : "";
                WP_Modul[i] = model.Bezeichner; 

                if (model.Volumen > 0) Volumen_Pufferspeicher = model.Volumen; 

                string wpTyp = "";
                rs.Open("select * from Tab_WP where ID=" + model.ID_WP);
                if (rs.Next())
                {
                    model.Grenzleistung = (int)rs.Read("Nennleistung");
                    WP_Heizung[i] = (int)rs.Read("Heizung");
                    object t = rs.Read("Typ");
                    if (t != null && t != DBNull.Value) wpTyp = t.ToString();
                }
                rs.Close();

                wp_model.Add(model);

                // Wärmequelle des Moduls: Luft-Wasser = Außenluft, sonst gemäß
                // WQ_Typ der Energieanlage (Fallback ist immer die Außenluft).
                wp_quelltemp.Add(WaermequelleClass.Quelltemperatur(wp_list[i], model.ID_Projekt, wpTyp, Temperatur));

                // Dient ein Pufferspeicher als Wärmequelle, muss die Quellwärme
                // tatsächlich aus diesem gedeckt werden (Bilanz je Stunde).
                wp_quellspeicher.Add(WaermequelleClass.Quellspeicher(wp_list[i], wpTyp));

                // Wärmesenke des Moduls (Warmwasser und/oder Heizwärme)
                string senke = WaermequelleClass.WertLesen(wp_list[i], "WS_Typ") as string;
                wp_senke.Add(string.IsNullOrEmpty(senke) ? WaermequelleClass.SENKE_BEIDES : senke);

                // Betriebsmodus des Moduls (Laufzeit-/Leistungs-/PV-optimiert)
                string modus = WaermequelleClass.WertLesen(wp_list[i], "BM_Typ") as string;
                wp_modus.Add(string.IsNullOrEmpty(modus) ? WaermequelleClass.MODUS_LAUFZEIT : modus);

                _Kenndaten item = new _Kenndaten();
                item.Vorlauf = model.Vorlauf;
                item.ID_WP = model.ID_WP;

                RecordSet rsAnz = new RecordSet();
                rsAnz.Open("SELECT Count(*) FROM Tab_Kenndaten WHERE Tab_Kenndaten.ID_WP=" + model.ID_WP + " AND Tab_Kenndaten.Vorlauf=" + model.Vorlauf);
                rsAnz.Next();
                int anz = (int)rsAnz.Read(0);
                rsAnz.Close();

                // Absicherung (wichtig bei mehreren WPs im Projekt): Ohne Kenndaten
                // für den gewählten Vorlauf würde berechne_wptherm mit einem
                // Index-Fehler abstürzen. Stattdessen verständliche Meldung.
                if (anz == 0)
                {
                    Cursor.Current = Cursors.Default;
                    MessageBox.Show("Für die Wärmepumpe '" + model.Bezeichner + "' sind keine Kenndaten" +
                        " (Kennlinie) für Vorlauf " + model.Vorlauf + " °C vorhanden!\n" +
                        "Die Simulation wird abgebrochen.", "Wärmepumpen Simulation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                item.dat = new _DAT[anz];
                item.anz = anz; 

                rs.Open("SELECT * FROM Tab_Kenndaten WHERE ID_WP=" + model.ID_WP + " AND Vorlauf=" + model.Vorlauf + " order by Temperatur DESC");

                int index = 0;
                while (rs.Next())
                {
                    _DAT dat = new _DAT();
                    dat.COP = (double)rs.Read("COP");
                    dat.Temperatur = (int)rs.Read("Temperatur");
                    dat.Leistung = (double)rs.Read("Ptherm");
                    item.dat[index++] = dat;
                }
                rs.Close();

                wp_kenndaten.Add(item);
            }

            WP_Strombedarf_gesamt = 0;
            WP_Waermeproduktion_gesamt = 0;
            Heizstab_gesamt = 0;
            WP_Laufzeit = 0;
            Bivalenzpunkt = -100;

            double Rest_waerme = 0;
            double Rest_Speicher, KapazitaetPendelspeicher, Solar_Speicher, Speicher;
            Rest_Speicher = 0;
            KapazitaetPendelspeicher = 0;
            Solar_Speicher = 0;
            Speicher = 0;

            if (Pufferspeicher != null) Pufferspeicher.Reset();
            _speicherLaden = true; // Simulation startet mit leerem Speicher -> zuerst laden

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                Rest_waerme = Waermebedarf_stuendlich[stunde];

                // Bedarf der Stunde in Warmwasser- und Heizwärmeanteil aufteilen
                // (Grundlage der Wärmesenken-Zuordnung je Modul)
                double rest_ww = 0;
                if (Warmwasserbedarf_stuendlich != null && stunde < Warmwasserbedarf_stuendlich.Length)
                {
                    rest_ww = Warmwasserbedarf_stuendlich[stunde];
                    if (rest_ww > Rest_waerme) rest_ww = Rest_waerme;
                    if (rest_ww < 0) rest_ww = 0;
                }
                double rest_heiz = Rest_waerme - rest_ww;

                // ***********************************************************************
                // Speicherbetrieb (Hysterese): Solange der Pufferspeicher den Bedarf
                // decken kann, bleibt die Wärmepumpe AUS und der Bedarf wird aus dem
                // Speicher gedeckt. Erst wenn der Füllstand unter die Einschaltschwelle
                // fällt (oder der Speicher den Bedarf der Stunde nicht mehr trägt),
                // läuft die Wärmepumpe wieder - dann bis der Speicher voll ist.
                // ***********************************************************************
                bool wpEinsatz = true;
                if (Pufferspeicher != null && Pufferspeicher.Q_max > 0)
                {
                    double einschaltSchwelle = Pufferspeicher.Q_max * Pufferspeicher.SchwelleEin;

                    double abschaltSchwelle = Pufferspeicher.Q_max * Pufferspeicher.SchwelleAus;

                    if (!_speicherLaden && Pufferspeicher.SOC <= einschaltSchwelle) _speicherLaden = true;
                    if (_speicherLaden && Pufferspeicher.SOC >= abschaltSchwelle) _speicherLaden = false;

                    if (!_speicherLaden)
                    {
                        // Bedarf zuerst aus dem Speicher decken
                        double gedeckt = Pufferspeicher.Entladen(rest_ww + rest_heiz, stunde);
                        SenkeAbziehen(WaermequelleClass.SENKE_BEIDES, gedeckt, ref rest_ww, ref rest_heiz);
                        Rest_waerme = rest_ww + rest_heiz;

                        if (Rest_waerme <= 0.0001) wpEinsatz = false;   // vollständig gedeckt -> WP bleibt aus
                        else _speicherLaden = true;                     // Speicher reicht nicht -> WP an
                    }
                }

                // WP-Potenzial dieser Stunde (für die Pufferladung aus Überschuss)
                double potenzialTherm = 0;
                double potenzialEl = 0;

                // Potenzial, das gemäß Betriebsmodus zum LADEN des Speichers
                // eingesetzt werden darf (Laufzeit = alles, Leistung = nichts,
                // PV = begrenzt durch den PV-Überschuss der Stunde)
                double ladePotenzialTherm = 0;
                double ladePotenzialEl = 0;
                double pvRest = (PV_Ueberschuss_stuendlich != null && stunde < PV_Ueberschuss_stuendlich.Length)
                    ? PV_Ueberschuss_stuendlich[stunde] : 0;

                Rest_Speicher = KapazitaetPendelspeicher - Solar_Speicher - Speicher;

                WP_Strombedarf_stuendlich[stunde] = 0;
                WP_Waermeproduktion_stuendlich[stunde] = 0;
                waermerestbedarf_stuendlich[stunde] = 0;
                Heizstab_stuendlich[stunde] = 0;

                double[] result = new double[4] { 0, 0, 0, 0 };

                for (int index = 0; index < wp_model.Count; index++)
                {
                    // Wärmepumpe bleibt in dieser Stunde aus (Bedarf aus dem Speicher gedeckt)
                    if (!wpEinsatz) break;

                    WErzeugerModel model = wp_model[index];
                    _Kenndaten kenndaten = wp_kenndaten[index];

                    // Kennlinien-Auswertung mit der Quelltemperatur des Moduls
                    // (Außenluft bzw. Wärmequelle bei Sole-/Wasser-Wasser-WP).
                    // Betriebsarten/Abschaltpunkt weiter unten bleiben bewusst auf
                    // der Außentemperatur (bivalenzrelevant ist das Außenklima).
                    result = berechne_wptherm(wp_quelltemp[index][stunde], model, kenndaten );
                    if (result[STATUS] == 0)
                    {
                        for (int i = 0; i < MAX_WP; i++)
                        {
                            Modul_WP_Strombedarf[i] = 0;
                            Modul_WP_Waermeproduktion[i] = 0;
                            Modul_Heizstab[i] = 0;
                            Modul_WP_Laufzeit[i] = 0;
                            WP_Modul[i] = "";
                            WP_Waermeproduktion_gesamt = 0;
                            WP_Strombedarf_gesamt = 0;
                            Heizstab_gesamt = 0;
                        }
                        return false;
                    }

                    // Betriebsarten Steuerung https://www.haustechnikverstehen.de/betriebsweisen-von-waermepumpen/
                    if (model.Bivalenter_Betrieb && model.Betriebsart == "Teilparallelbetrieb")
                    {
                        // Teilparallelbetrieb Abschaltpunkt
                        // Der bivalent-teilparallele Betrieb ist eine Mischung aus bivalent-paralleler- und
                        // bivalent-alternativer Betriebsweise. Die Wärmepumpe arbeitet bis zum Bivalenzpunkt alleine
                        // und wird anschließend vom zweiten Wärmeerzeuger unterstützt.
                        // Bei Erreichen einer weiteren festgelegten  Temperatur (z.B. -2 °C) schaltet sich die
                        // Wärmepumpe ab
                        if (Temperatur[stunde] <= model.Abschaltpunkt)
                        {
                            result[PTHERM] = 0;
                            result[PEL] = 0;
                        }
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == "Parallelbetrieb")
                    {
                        // Bei der bivalent-parallelen Betriebsweise wird der Wärmebedarf bis zum Erreichen des
                        // Bivalenzpunktes allein von der Wärmepumpe getragen. Bei der Unterschreitung des Bivalenzpunktes
                        // unterstützt der zweite Wärmeerzeuger den Heizbetrieb der Wärmepumpe
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == "Alternativbetrieb")
                    {
                        // Bei der bivalent-alternativen Betriebsweise wird der Wärmebedarf bis zum Erreichen des
                        // Bivalenzpunktes allein von der Wärmepumpe getragen. Der zweite Wärmeerzeuger springt bei
                        // der Unterschreitung des Bivalenzpunktes von ca. + 3 °C ein und übernimmt den alleinigen Heizbetrieb.
                        if (result[PTHERM] < Rest_waerme)
                        {
                            result[PTHERM] = 0;
                            result[PEL] = 0;
                        }
                    }

                    // Sperrzeiten berücksichtigen
                    int std = stunde % 24;
                    if(std >= model.Sperrzeit_von && std < model.Sperrzeit_bis && model.Sperrung)
                    {
                        result[PTHERM] = 0;
                        result[PEL] = 0;
                    }

                    // Bivalenzpunkt ermitteln
                    //if (result[PTHERM] < Rest_waerme)
                    //{
                    //    if (Temperatur[stunde] > Bivalenzpunkt) { Bivalenzpunkt = Temperatur[stunde];}
                    //}

                    // ***********************************************************
                    // Wärmequelle Pufferspeicher: die Quellwärme (Verdampferwärme
                    // = Wärmeproduktion - Stromaufnahme) muss aus dem Speicher
                    // gedeckt werden. Reicht der Inhalt nicht, wird die Leistung
                    // der Wärmepumpe in dieser Stunde entsprechend begrenzt.
                    // ***********************************************************
                    SimulationPufferspeicher quelle = wp_quellspeicher[index];
                    if (quelle != null && result[PTHERM] > 0)
                    {
                        // Regeneration (Nachladung) der Quelle für diese Stunde
                        if (quelle.RegenerationProStunde > 0)
                            quelle.Laden(quelle.RegenerationProStunde, stunde);

                        double quellAnteil = result[PTHERM] - result[PEL]; // kWh je Stunde
                        if (quellAnteil > 0 && quelle.SOC < quellAnteil)
                        {
                            // Verfügbare Quellwärme begrenzt Wärme- und Stromseite
                            double faktor = quelle.SOC / quellAnteil;
                            if (faktor < 0) faktor = 0;
                            result[PTHERM] *= faktor;
                            result[PEL] *= faktor;
                        }
                    }

                    // ***********************************************************
                    // Wärmesenke des Moduls: Warmwasser und/oder Heizwärme.
                    // Ein auf Warmwasser eingestelltes Modul deckt ausschließlich
                    // den Warmwasserbedarf (analog "Heizung").
                    // ***********************************************************
                    string senke = wp_senke[index];
                    double verfuegbar;
                    if (senke == WaermequelleClass.SENKE_WARMWASSER) verfuegbar = rest_ww;
                    else if (senke == WaermequelleClass.SENKE_HEIZUNG) verfuegbar = rest_heiz;
                    else verfuegbar = rest_ww + rest_heiz;

                    // Kein Bedarf für die Senke dieses Moduls -> Modul bleibt aus
                    if (verfuegbar <= 0)
                    {
                        if (quelle != null) quelle.StundeAbschliessen(stunde);
                        continue;
                    }

                    // Verfügbares WP-Potenzial dieser Stunde aufsummieren
                    // (nach Betriebsart-/Sperrzeit-Korrektur, vor Bedarfsbegrenzung);
                    // nur einsetzbare Module zählen mit - sie können den
                    // Pufferspeicher mit ihrem Überschuss laden.
                    potenzialTherm += result[PTHERM];
                    potenzialEl += result[PEL];

                    // ***********************************************************
                    // Betriebsmodus: wie viel Leistung darf über den Bedarf hinaus
                    // gefahren werden (Speicherladung)?
                    //  Laufzeit  = volle Leistung (maximale Laufzeit, wenig Takten)
                    //  Leistung  = nur den Bedarf decken (kein Überschuss)
                    //  PV        = Überschuss nur soweit PV-Strom verfügbar ist
                    // ***********************************************************
                    string modus = wp_modus[index];
                    if (modus == WaermequelleClass.MODUS_LEISTUNG)
                    {
                        // kein Ladepotenzial - die WP moduliert exakt auf den Bedarf
                    }
                    else if (modus == WaermequelleClass.MODUS_PV)
                    {
                        double copPV = result[COP] > 0 ? result[COP] : 1;
                        double maxThermPV = pvRest * copPV;
                        double thermPV = Math.Min(result[PTHERM], maxThermPV);
                        if (thermPV > 0)
                        {
                            ladePotenzialTherm += thermPV;
                            ladePotenzialEl += thermPV / copPV;
                            pvRest -= thermPV / copPV;      // verbrauchten PV-Strom abziehen
                            if (pvRest < 0) pvRest = 0;
                        }
                    }
                    else // MODUS_LAUFZEIT (Vorgabe)
                    {
                        ladePotenzialTherm += result[PTHERM];
                        ladePotenzialEl += result[PEL];
                    }

                    // Erzeugung dieses Moduls vor dem Auswerten festhalten, um
                    // anschließend die tatsächlich entnommene Quellwärme zu bilanzieren
                    float vorherTherm = WP_Waermeproduktion_stuendlich[stunde];
                    float vorherEl = WP_Strombedarf_stuendlich[stunde];

                    // Leistungsdaten der WP auswerten
                    if (result[PTHERM] < verfuegbar)
                    {
                        WP_Waermeproduktion_stuendlich[stunde] = WP_Waermeproduktion_stuendlich[stunde] + (float)result[PTHERM];
                        WP_Waermeproduktion_gesamt += result[PTHERM];
                        WP_Strombedarf_stuendlich[stunde] = WP_Strombedarf_stuendlich[stunde] + (float)result[PEL];
                        WP_Strombedarf_gesamt += result[PEL];
                        Modul_WP_Waermeproduktion[index] += (float)result[PTHERM];
                        Modul_WP_Strombedarf[index] += result[PEL];

                        WP_Laufzeit = WP_Laufzeit + 1;
                        Modul_WP_Laufzeit[index] += 1;

                        SenkeAbziehen(senke, result[PTHERM], ref rest_ww, ref rest_heiz);
                    }
                    else
                    {
                        WP_Waermeproduktion_stuendlich[stunde] = WP_Waermeproduktion_stuendlich[stunde] + (float)verfuegbar;
                        WP_Waermeproduktion_gesamt += verfuegbar;
                        WP_Strombedarf_stuendlich[stunde] = WP_Strombedarf_stuendlich[stunde] + (float)verfuegbar / (float)result[COP];
                        WP_Strombedarf_gesamt += verfuegbar / result[COP];
                        Modul_WP_Waermeproduktion[index] += (float)verfuegbar;
                        Modul_WP_Strombedarf[index] += verfuegbar / result[COP];

                        // Absicherung: bei begrenzter Quelle bzw. Sperrzeit kann
                        // result[PTHERM] 0 sein - dann keine Laufzeit anrechnen.
                        if (result[PTHERM] > 0)
                        {
                            WP_Laufzeit = WP_Laufzeit + (verfuegbar / (float)result[PTHERM]);
                            Modul_WP_Laufzeit[index] += (verfuegbar / (float)result[PTHERM]);
                        }

                        SenkeAbziehen(senke, verfuegbar, ref rest_ww, ref rest_heiz);
                    }

                    // Aggregierter Restbedarf für Speicher, Heizstab und Folge-Erzeuger
                    Rest_waerme = rest_ww + rest_heiz;

                    // Tatsächlich entnommene Quellwärme aus dem Speicher abziehen
                    if (quelle != null)
                    {
                        double erzeugt = WP_Waermeproduktion_stuendlich[stunde] - vorherTherm;
                        double strom = WP_Strombedarf_stuendlich[stunde] - vorherEl;
                        double entnahme = erzeugt - strom;
                        if (entnahme > 0) quelle.Entladen(entnahme, stunde);
                        quelle.StundeAbschliessen(stunde);
                    }

                } // end alle WP

                // ***********************************************************************
                // Pufferspeicher (Stufe 1): Entladen VOR Heizstab/Folge-Erzeuger.
                // Kann die WP den Bedarf der Stunde nicht decken, wird zuerst der
                // Speicher entladen - erst der verbleibende Rest geht an den Heizstab
                // bzw. als Restwärme an den nächsten Erzeuger der Kaskade.
                // ***********************************************************************
                if (Pufferspeicher != null && Rest_waerme > 0)
                {
                    Rest_waerme -= Pufferspeicher.Entladen(Rest_waerme, stunde);
                    if (Rest_waerme < 0) Rest_waerme = 0;
                }

                // dient zum späteren Bivalenzpunkt ermitteln
                if ( Rest_waerme > 0)
                {
                    biv.Add(Temperatur[stunde]);
                }

                // Heizstab mit einbeziehen 
                for (int index = 0; index < wp_model.Count; index++)
                {
                    if (Mit_Heizstab && Rest_waerme > 0 && WP_Heizung[index] > 0)
                    {
                        if (Rest_waerme > WP_Heizung[index])
                        {
                            Heizstab_stuendlich[stunde] = WP_Heizung[index];
                            Heizstab_gesamt += Heizstab_stuendlich[stunde];
                            Modul_Heizstab[index] += WP_Heizung[index];
                            Rest_waerme = Rest_waerme - WP_Heizung[index];
                        }
                        else
                        {
                            Heizstab_stuendlich[stunde] = (float)Rest_waerme;
                            Heizstab_gesamt += Heizstab_stuendlich[stunde];
                            Modul_Heizstab[index] += Rest_waerme;
                            Rest_waerme = 0;
                        }
                    }
                }

                // ***********************************************************************
                // Pufferspeicher (Stufe 1): Laden aus WP-Überschuss.
                // Hat die WP in dieser Stunde mehr Potenzial als der Bedarf, läuft sie
                // weiter und lädt die Differenz in den Speicher (längere Laufzeiten,
                // weniger Takten). Der zusätzliche Strombedarf wird über den mittleren
                // COP der Stunde verrechnet.
                // ***********************************************************************
                if (Pufferspeicher != null)
                {
                    // Nur das gemäß Betriebsmodus zulässige Potenzial darf laden
                    double ueberschuss = ladePotenzialTherm - WP_Waermeproduktion_stuendlich[stunde];
                    if (ueberschuss > 0)
                    {
                        double ladung = Pufferspeicher.Laden(ueberschuss, stunde);
                        if (ladung > 0)
                        {
                            // Ladung ist real erzeugte WP-Wärme (Verbleib im Speicher)
                            WP_Waermeproduktion_stuendlich[stunde] += (float)ladung;
                            WP_Waermeproduktion_gesamt += ladung;

                            double copMittel = ladePotenzialEl > 0 ? ladePotenzialTherm / ladePotenzialEl : 0;
                            if (copMittel > 0)
                            {
                                WP_Strombedarf_stuendlich[stunde] += (float)(ladung / copMittel);
                                WP_Strombedarf_gesamt += ladung / copMittel;
                            }

                            if (ladePotenzialTherm > 0)
                                WP_Laufzeit += ladung / ladePotenzialTherm;
                        }
                    }

                    // Speicher ist gefüllt -> Wärmepumpe abschalten (Prüfung VOR den
                    // Bereitschaftsverlusten, sonst wird der Vollstand nie erreicht)
                    if (_speicherLaden && Pufferspeicher.SOC >= Pufferspeicher.Q_max * Pufferspeicher.SchwelleAus)
                        _speicherLaden = false;

                    // Bereitschaftsverluste verrechnen und Speicherzustand festhalten
                    Pufferspeicher.StundeAbschliessen(stunde);
                }

                // Wärmerestbedarf speichern
                waermerestbedarf_stuendlich[stunde] = waermerestbedarf_stuendlich[stunde] + (float)Rest_waerme;

            } // end alle Stunden

            // absteigend sortieren
            com.I_heapsort(WP_Waermeproduktion_stuendlich, WP_Waermeproduktion_stuendlich_sortiert);

            // Wärmebedarf gesamt und Restwärme berechnen in kWh
            // Restwärme aus der Stundenganglinie summieren - mit Pufferspeicher ist
            // "Bedarf minus Produktion" nicht mehr identisch mit der echten Restwärme
            // (Ladung/Entladung verschieben Energie zwischen den Stunden).
            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf_stuendlich, value => Waermebedarf_gesamt += value);
            waermerestbedarf_gesamt = 0;
            Array.ForEach(waermerestbedarf_stuendlich, value => waermerestbedarf_gesamt += value);

            Cursor.Current = Cursors.Default;

            if (biv.Count > 0)
                Bivalenzpunkt = biv.Max();
            return true;
        }

        /// <summary>
        /// Zieht die erzeugte Wärmemenge vom passenden Bedarfsanteil ab.
        /// Bei der Wärmesenke "Beides" gilt Warmwasservorrang: zuerst wird der
        /// Warmwasserbedarf gedeckt, der Rest geht auf die Heizwärme.
        /// </summary>
        private void SenkeAbziehen(string senke, double menge, ref double rest_ww, ref double rest_heiz)
        {
            if (menge <= 0) return;

            if (senke == WaermequelleClass.SENKE_WARMWASSER)
            {
                rest_ww -= menge;
            }
            else if (senke == WaermequelleClass.SENKE_HEIZUNG)
            {
                rest_heiz -= menge;
            }
            else
            {
                double ww = Math.Min(menge, rest_ww);
                rest_ww -= ww;
                rest_heiz -= (menge - ww);
            }

            if (rest_ww < 0) rest_ww = 0;
            if (rest_heiz < 0) rest_heiz = 0;
        }

        double[] berechne_wptherm(float temperatur, WErzeugerModel model, _Kenndaten kenndaten)
        {

            double[] result = new double[4] { 0, 0, 0, 0 };
            double wptherm = 0;
            double cop = 0, ptherm = 0, pel = 0;
            double cop_maxSST = 0, t_maxSST = 0, ptherm_maxSST  = 0;
            int maxsst = kenndaten.anz; 

            cop_maxSST = kenndaten.dat[0].COP;
            t_maxSST = kenndaten.dat[0].Temperatur;
            ptherm_maxSST = kenndaten.dat[0].Leistung;

            if (temperatur >= t_maxSST)
            {
                // t grösser als max sst der Kennlinie
                cop = cop_maxSST;
                ptherm = ptherm_maxSST;
                pel = ptherm / cop;
                wptherm = ptherm;
            }
            else
            {
                if (temperatur < kenndaten.dat[kenndaten.anz - 1].Temperatur)
                {
                    if (!extrapolation)
                    {
                        
                        if (MessageBox.Show("Wärmepumpen Simulation:\nTemperatur unterschreitet Kennlinien Untergrenze," +
                                            " soll extrapoliert werden?\nBei nein wird Simulation abgebrochen!", "Temperatur unter Minimum Kennlinie",
                                            MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            result[0] = 0;
                            return result;
                        }
                        extrapolation = true;
                    }
                    double[] x = new double[2];
                    double[] y = new double[2];
                    double[] xq = new double[2];
                    x[0] = kenndaten.dat[kenndaten.anz - 1].Temperatur;
                    x[1] = kenndaten.dat[kenndaten.anz - 2].Temperatur;
                    y[0] = kenndaten.dat[kenndaten.anz - 1].COP;
                    y[1] = kenndaten.dat[kenndaten.anz - 2].COP;
                    xq[0] = temperatur;
                    cop = Interp(x,y,xq);
                    y[0] = kenndaten.dat[kenndaten.anz - 1].Leistung;
                    y[1] = kenndaten.dat[kenndaten.anz - 2].Leistung;
                    ptherm = Interp(x, y, xq);
                    pel = ptherm / cop;
                    wptherm = ptherm;
                }
                else
                {
                    // Interpolation innerhalb der Kennlinie
                    for (int i = 1; i < kenndaten.anz; i++)
                    {
                        if (temperatur >= kenndaten.dat[i].Temperatur)
                        {
                            double[] x = new double[2];
                            double[] y = new double[2];
                            double[] xq = new double[2];
                            x[0] = kenndaten.dat[i - 1].Temperatur;
                            x[1] = kenndaten.dat[i].Temperatur;
                            y[0] = kenndaten.dat[i - 1].COP;
                            y[1] = kenndaten.dat[i].COP;
                            xq[0] = temperatur;
                            cop = Interp(x, y, xq);
                            y[0] = kenndaten.dat[i - 1].Leistung;
                            y[1] = kenndaten.dat[i].Leistung;
                            ptherm = Interp(x, y, xq);
                            pel = ptherm / cop;
                            wptherm = ptherm;
                            break;
                        }
                    }
                }
            }

            result[0] = 1;
            result[1] = cop;
            result[2] = ptherm;
            result[3] = pel;

            return result;
        }

        public static double Interp(double[] x, double[] y, double[] xq)
        {
            return y[0] +  (xq[0] - x[0]) * (y[1] - y[0]) / (x[1] - x[0]);    
        }

        public void Init()
        {
            for (int i = 0; i < MAX_WP; i++)
            {
                Modul_WP_Strombedarf[i] = 0;
                Modul_WP_Waermeproduktion[i] = 0;
                Modul_Heizstab[i] = 0;
                Modul_WP_Laufzeit[i] = 0;
                WP_Modul[i] = "";
            }

            for (int i = 0; i < 8760; i++)
            {
                waermerestbedarf_stuendlich[i] = 0;
                WP_Strombedarf_stuendlich[i] = 0;
                WP_Waermeproduktion_stuendlich[i] = 0;
                WP_Waermeproduktion_stuendlich_sortiert[i] = 0;
                Heizstab_stuendlich[i] = 0;
            }
            WP_Waermeproduktion_gesamt = 0;
            Heizstab_gesamt = 0;
            WP_Strombedarf_gesamt = 0;
            WP_Laufzeit = 0;
            Bivalenzpunkt = -100;
        }
    }
}

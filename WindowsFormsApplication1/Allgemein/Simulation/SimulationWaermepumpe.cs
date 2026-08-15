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

        /// <summary>
        /// Quellspeicher je WP-Modul in Modulreihenfolge (Einträge können null sein).
        /// Lesezugriff für Ergebnis-Persistenz und Anzeigen (Konzept 6.6/13.3) -
        /// die Liste selbst bleibt in der Hand der Simulation.
        /// </summary>
        public IReadOnlyList<SimulationPufferspeicher> Quellspeicher { get { return wp_quellspeicher; } }

        /// <summary>
        /// Quelltemperatur-Jahresprofil je WP-Modul in Modulreihenfolge.
        /// Lesezugriff für die zweite Warnbedingung der Erdreich-Prüfung
        /// (Quelltemperatur minus Spreizung dauerhaft &lt; 0 °C, Konzept 13.1).
        /// </summary>
        public IReadOnlyList<float[]> Quelltemperaturen { get { return wp_quelltemp; } }

        // Bauart der Wärmepumpe je Modul (Tab_WP.Typ): "Luft-Wasser",
        // "Sole-Wasser", "Wasser-Wasser". Wird beim Aufbau der Kaskade gelesen.
        private List<string> wp_typ = new List<string>();

        /// <summary>
        /// Bauart je WP-Modul in Modulreihenfolge (Tab_WP.Typ).
        ///
        /// Sie ist die WIRKSAMKEITSREGEL der Wärmequelle: Für "Luft-Wasser" liefern
        /// <see cref="WaermequelleClass.Quelltemperatur"/> und
        /// <see cref="WaermequelleClass.Quellspeicher"/> immer Außenluft bzw. null -
        /// eine gepflegte WQ_*-Konfiguration bleibt dort ohne jede Wirkung. Auswertungen
        /// (Erdreich-Auslegungsprüfung) müssen das mitprüfen, sonst weisen sie Zahlen
        /// für eine Quelle aus, die gar nicht gerechnet wurde.
        /// </summary>
        public IReadOnlyList<string> WPTypen { get { return wp_typ; } }

        // Wärmesenke je WP-Modul: Beides | Warmwasser | Heizung
        // (WS_Typ der Energieanlage; steuert, welchen Bedarfsanteil das Modul deckt)
        private List<string> wp_senke = new List<string>();

        // Betriebsmodus je WP-Modul: Laufzeit | Leistung | PV (BM_Typ)
        private List<string> wp_modus = new List<string>();

        /// <summary>
        /// Betriebsmodus je WP-Modul in Modulreihenfolge (<c>BM_Typ</c>). Lesezugriff für
        /// den Kontextaufbau der zweikanaligen Kaskade: Die zeitabhängige Ladepriorität
        /// (Konzept 3.5) greift nur bei Betriebsmodus <see cref="WaermequelleClass.MODUS_PV"/>.
        /// </summary>
        public IReadOnlyList<string> Betriebsmodi { get { return wp_modus; } }

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

        /// <summary>
        /// Projekteinstellung <c>Tab_Einstellungen.Extrapolation_erlaubt</c> (Paket 8,
        /// Konzept 13.4). <b>Vorbelegung true</b> — sie ersetzt die Rückfrage, die die
        /// Engine bis Paket 8 mitten in der Stundenschleife als MessageBox stellte
        /// („Temperatur unterschreitet Kennlinien Untergrenze, soll extrapoliert
        /// werden?"). Gesetzt wird sie von <c>SimulationControl</c> aus der
        /// Projektkonfiguration; der Vorbelegungswert gilt, solange niemand sie setzt,
        /// und ist deshalb bewusst der bisherige Antwortwert.
        /// </summary>
        public bool Extrapolation_Erlaubt = true;

        /// <summary>
        /// Fehlertext eines dialogfrei abgebrochenen Laufs (Paket 8, Konzept 13.4). Leer,
        /// wenn nichts anlag. <c>SimulationControl</c> holt ihn ab und reicht ihn über
        /// <see cref="SimulationControl.Fehlertext"/> an den Aufrufer weiter —
        /// dasselbe Muster, das <see cref="SimulationSPK"/> (N10) und
        /// <see cref="SimulationBHKW"/> (N8) bereits verwenden.
        /// </summary>
        public string Fehlertext = "";

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

        //public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();

        public bool Berechnung()
        {
            if (wp_list.Count >= 10) return false;

            Cursor.Current = Cursors.WaitCursor;

            // Modulaufbau (Kenndaten, Wärmequelle, Wärmesenke, Betriebsmodus) und
            // Stundenschleife stehen seit Etappe 4b in zwei eigenen Methoden. Grund: Der
            // zweikanalige Rechenweg braucht denselben Modulaufbau, aber eine andere
            // Stundenschleife. Die AUSGEFÜHRTEN Anweisungen und ihre Reihenfolge sind
            // gegenüber der Fassung davor unverändert — nur die Methodengrenze ist neu;
            // die Regression über neun Referenzprojekte belegt das wertgenau.
            if (!ModuleAufbauen()) return false;

            return Berechnung_Stundenschleife();
        }

        /// <summary>
        /// Baut die Module der Kaskade auf: Kenndaten (Kennlinie je Vorlauf), Bauart,
        /// Wärmequelle (Quelltemperatur und ggf. Quellspeicher), Wärmesenke und
        /// Betriebsmodus je Anlage aus <see cref="wp_list"/>.
        ///
        /// Der Block stand bis Etappe 4b wörtlich am Anfang von <see cref="Berechnung"/>
        /// und ist von dort unverändert hierher gewandert — beide Rechenwege (einkanalig
        /// und zweikanalig) brauchen ihn Zeile für Zeile gleich, und zwei Kopien wären die
        /// sichere Quelle künftiger Abweichungen.
        /// </summary>
        /// <returns>
        /// false = Abbruch (fehlende Kenndaten). Der Grund steht seit Paket 8 in
        /// <see cref="Fehlertext"/> und im <see cref="SimulationProtokoll"/> statt in
        /// einer MessageBox (Konzept 13.4).
        /// </returns>
        private bool ModuleAufbauen()
        {
            RecordSet rs = new RecordSet();
            WErzeugerCtrl wp = new WErzeugerCtrl();

            Volumen_Pufferspeicher = 0;
            Fehlertext = "";

            // NACHARBEIT PAKET 8, BEFUND N3: Auch das Extrapolationsmerkmal gehört in den
            // Rücksetzblock. Es wird beim ersten Unterschreiten der Kennlinie gesetzt und
            // blieb bisher über die Lebensdauer der Instanz stehen - im MDI-Fenster lebt
            // dieselbe SimulationControl (und damit dasselbe WP-Modul) über beliebig
            // viele Läufe. Ab dem zweiten Lauf wäre Extrapolation_Erlaubt sonst
            // wirkungslos: kein Abbruch bei Verbot, kein Hinweis bei Erlaubnis.
            //
            // ERGEBNISNEUTRAL: Das Merkmal steuert AUSSCHLIESSLICH die Meldung und die
            // Verbotsprüfung - die lineare Verlängerung der Kennlinie darunter läuft in
            // jedem Fall (siehe berechne_wptherm, der Zweig hinter "if (!extrapolation)"
            // endet vor der Rechnung).
            extrapolation = false;

            Init();

            wp_model.Clear();
            wp_kenndaten.Clear();
            wp_quelltemp.Clear();
            wp_quellspeicher.Clear();
            wp_typ.Clear();
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

                // K-3: einmaliger Hinweis, wenn eine bivalent-alternative Anlage mit der
                // Vorbelegung 0 °C als Bivalenztemperatur rechnet. Steht hier, weil der
                // Modulaufbau beide Rechenwege bedient und je Anlage genau einmal läuft.
                AlternativHinweisPruefen(model);

                // Bauart merken - sie entscheidet, ob die WQ_*-Konfiguration überhaupt
                // wirkt (siehe WPTypen); Auswertungen brauchen dieselbe Regel.
                wp_typ.Add(wpTyp ?? "");

                // Wärmequelle des Moduls: Luft-Wasser = Außenluft, sonst gemäß
                // WQ_Typ der Energieanlage (Fallback ist immer die Außenluft).
                wp_quelltemp.Add(WaermequelleClass.Quelltemperatur(wp_list[i], model.ID_Projekt, wpTyp, Temperatur));

                // Dient ein Pufferspeicher als Wärmequelle, muss die Quellwärme
                // tatsächlich aus diesem gedeckt werden (Bilanz je Stunde).
                SimulationPufferspeicher quellSp = WaermequelleClass.Quellspeicher(wp_list[i], wpTyp);
                // Zuordnung zur Energieanlage merken: sie bildet den technischen
                // Serienschlüssel QUELLE_<AnlagenID> der Anzeigen (Konzept 13.3).
                if (quellSp != null) quellSp.ID_Anlage = wp_list[i];
                wp_quellspeicher.Add(quellSp);

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
                //
                // PAKET 8 (Konzept 13.4): Die Meldung geht dialogfrei über den
                // Fehlerkanal statt über eine MessageBox. Der ABBRUCH ist unverändert
                // (return false an derselben Stelle) — nur der Meldeweg ist neu. Die
                // Oberfläche zeigt den Text nach dem Lauf als Dialog; im headless-Lauf
                // steht er in "out fehler".
                if (anz == 0)
                {
                    Cursor.Current = Cursors.Default;
                    Fehlertext = string.Format(MyResource.Resource.SIMENG_WP_KEINE_KENNDATEN,
                                               model.Bezeichner, model.Vorlauf);
                    SimulationProtokoll.Aktuell.Fehlermeldung(
                        MyResource.Resource.SIMENG_PRAEFIX_WAERMEPUMPE + Fehlertext);
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

            return true;
        }

        /// <summary>
        /// EINKANALIGE Stundenschleife der Wärmepumpen-Kaskade — der Rechenweg des
        /// Bestands, Anweisung für Anweisung unverändert. Er läuft, solange die
        /// Projekteinstellung <c>Kaskade_Zweikanalig</c> nicht gesetzt ist.
        ///
        /// Voraussetzung: <see cref="ModuleAufbauen"/> ist gelaufen.
        /// Die zweikanalige Fassung steht in <see cref="Berechnung_Zweikanalig"/>.
        /// </summary>
        private bool Berechnung_Stundenschleife()
        {
            List<double> biv = new List<double>();

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

                // ***********************************************************************
                // Wärmepumpe bleibt in dieser Stunde aus (Bedarf ist aus dem Senken-
                // Pufferspeicher gedeckt). Die QUELLspeicher müssen ihre Stunde trotzdem
                // abschließen: StundeAbschliessen verrechnet die Bereitschaftsverluste
                // UND schreibt den Füllstand in SOC_stuendlich. Ohne diesen Aufruf bleibt
                // die Ganglinie in jeder solchen Stunde auf 0 stehen, obwohl der Speicher
                // voll ist - SOC_Mittel und SOC_Max fallen systematisch zu niedrig aus
                // (die Ganglinie speist Anzeige, CSV-Export und Tab_ErgebnisPufferspeicher).
                // Betroffen sind nur Projekte MIT Quellspeicher; ohne Senken-Puffer wird
                // wpEinsatz nie false.
                // ***********************************************************************
                if (!wpEinsatz)
                    for (int index = 0; index < wp_quellspeicher.Count; index++)
                        if (wp_quellspeicher[index] != null) wp_quellspeicher[index].StundeAbschliessen(stunde);

                for (int index = 0; index < wp_model.Count; index++)
                {
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
                    if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_TEILPARALLEL)
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
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_PARALLEL)
                    {
                        // Bei der bivalent-parallelen Betriebsweise wird der Wärmebedarf bis zum Erreichen des
                        // Bivalenzpunktes allein von der Wärmepumpe getragen. Bei der Unterschreitung des Bivalenzpunktes
                        // unterstützt der zweite Wärmeerzeuger den Heizbetrieb der Wärmepumpe
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_ALTERNATIV)
                    {
                        // Bei der bivalent-alternativen Betriebsweise wird der Wärmebedarf bis zum Erreichen des
                        // Bivalenzpunktes allein von der Wärmepumpe getragen. Der zweite Wärmeerzeuger springt bei
                        // der Unterschreitung des Bivalenzpunktes ein und übernimmt den alleinigen Heizbetrieb.
                        // K-3: Umschaltkriterium ist die AUSSENTEMPERATUR — siehe AlternativAus.
                        if (AlternativAus(model, stunde))
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

                        // B0-13: Laufzeit nur zählen, wenn die Wärmepumpe auch gelaufen
                        // ist. result[PTHERM] kann hier 0 sein (Sperrzeit, begrenzte
                        // Quelle, Alternativbetrieb) - dann wurde eine volle Stunde
                        // Betriebszeit gebucht, ohne dass eine kWh entstanden ist.
                        // Derselbe Guard, den der Teillast-Zweig unten längst hat.
                        if (result[PTHERM] > 0)
                        {
                            WP_Laufzeit = WP_Laufzeit + 1;
                            Modul_WP_Laufzeit[index] += 1;
                        }

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
                            // B0-5: "+=" statt "=" — bei mehreren WP-Modulen überschrieb die
                            // Ganglinie sonst den Beitrag der vorherigen Module, während
                            // Heizstab_gesamt korrekt weiter addierte (inkonsistente Summen).
                            // Addiert wird jeweils der Modul-Beitrag, nicht der Stundenstand.
                            Heizstab_stuendlich[stunde] += WP_Heizung[index];
                            Heizstab_gesamt += WP_Heizung[index];
                            Modul_Heizstab[index] += WP_Heizung[index];
                            Rest_waerme = Rest_waerme - WP_Heizung[index];
                        }
                        else
                        {
                            Heizstab_stuendlich[stunde] += (float)Rest_waerme;
                            Heizstab_gesamt += (float)Rest_waerme;
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
            //com.I_heapsort(WP_Waermeproduktion_stuendlich, WP_Waermeproduktion_stuendlich_sortiert);
            WPPlan.Core.BhkwPlan.Heapsort(WP_Waermeproduktion_stuendlich, WP_Waermeproduktion_stuendlich_sortiert);

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

        // ===================================================================
        // Zweikanaliger Rechenweg (Paket 4, Etappe 4b — Konzept 6.3)
        //
        // Bewusst als EIGENE Methodenvariante neben Berechnung_Stundenschleife und
        // nicht als Umbau von Berechnung() in-place. Abgewogen wurde beides:
        //
        //   Umbau in-place  hätte den einen Rechenweg erhalten, aber jede der rund
        //                   zwanzig Verzweigungen (Senke je Modul, Ladefähigkeit statt
        //                   Bedarf, Entladen nach Kanal, StundeAbschliessen zentral)
        //                   in den Bestandscode getragen. Der Altpfad wäre danach nicht
        //                   mehr durch Lesen als unverändert nachweisbar gewesen — nur
        //                   noch durch Messen. Bei einem Feature-Flag, dessen Zweck die
        //                   Rückfallebene ist, ist das die falsche Reihenfolge.
        //   Eigene Methode  kostet eine zweite Stundenschleife (~200 Zeilen). Der
        //                   gemeinsame Teil — Modulaufbau, Kennlinienauswertung,
        //                   SenkeAbziehen — wird geteilt, nicht kopiert; die Doppelung
        //                   beschränkt sich auf die Ablaufsteuerung, und genau die ist
        //                   in beiden Fassungen unterschiedlich.
        //
        // Mit Paket 5/6 (Kessel, BHKW, Solarthermie zweikanalig) wird der einkanalige
        // Weg entbehrlich; dann verschwindet die Doppelung mit dem Flag.
        // ===================================================================

        /// <summary>
        /// Bereitet den zweikanaligen Lauf vor: Modulaufbau wie im Altpfad, danach die
        /// Zusammenführung mehrfach benutzter Quellspeicher.
        ///
        /// Getrennt von <see cref="Berechnung_Zweikanalig"/>, weil
        /// <c>SimulationControl</c> dazwischen die Speicher-Registry vervollständigen
        /// muss: Die Quellspeicher entstehen erst beim Modulaufbau, und die Registry ist
        /// die Menge, über die Phase G läuft.
        /// </summary>
        public bool Vorbereiten_Zweikanalig()
        {
            if (wp_list.Count >= MAX_WP) return false;

            Cursor.Current = Cursors.WaitCursor;

            if (!ModuleAufbauen()) return false;

            QuellspeicherZusammenfuehren();
            return true;
        }

        /// <summary>
        /// Mehrere Module am SELBEN Quellpuffer teilen sich ab Etappe 4b EINE Instanz
        /// (Konzept 6.2/6.3, offener Punkt 4 aus Etappe 4a).
        ///
        /// Bis dahin baute <see cref="WaermequelleClass.Quellspeicher"/> je Modul ein
        /// eigenes Objekt. Zwei Module am selben Speicher rechneten damit gegen zwei
        /// getrennte Füllstände — jedes durfte die volle Quellwärme entnehmen —, und
        /// <c>StundeAbschliessen</c> zog die Bereitschaftsverluste doppelt ab. Beides ist
        /// mit der gemeinsamen Instanz und der zentralen Phase G behoben.
        ///
        /// Es gewinnt die Instanz des ERSTEN Moduls in Modulreihenfolge. Ihre nutzbare
        /// Kapazität folgt der Spreizung <c>WQ_Spreizung</c> jener Anlage; unterscheiden
        /// sich die Spreizungen zweier Anlagen am selben Speicher, ist das eine
        /// Konfigurationsfrage und keine Rechenfrage — der Speicher hat einen
        /// Betriebszustand. Der Fall wird protokolliert.
        /// </summary>
        private void QuellspeicherZusammenfuehren()
        {
            Dictionary<int, SimulationPufferspeicher> ersteInstanz =
                new Dictionary<int, SimulationPufferspeicher>();

            for (int i = 0; i < wp_quellspeicher.Count; i++)
            {
                SimulationPufferspeicher q = wp_quellspeicher[i];
                if (q == null || q.ID_Pufferspeicher <= 0) continue;

                SimulationPufferspeicher vorhanden;
                if (!ersteInstanz.TryGetValue(q.ID_Pufferspeicher, out vorhanden))
                {
                    ersteInstanz[q.ID_Pufferspeicher] = q;
                    continue;
                }

                // Protokollkanal-Nachzug: WARNUNG - die Quellparameter der übrigen
                // Anlagen an diesem Speicher bleiben UNWIRKSAM. Je Speicher einmal.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                                  "quellspeicher-mehrfach-" + q.ID_Pufferspeicher,
                                  "Quellspeicher " + q.ID_Pufferspeicher +
                                  " wird von mehreren Modulen benutzt — ab Etappe 4b rechnen sie " +
                                  "gegen EINEN Füllstand (Konzept 6.3). Maßgeblich bleiben die " +
                                  "Quellparameter der Anlage " + vorhanden.ID_Anlage + ".");
                wp_quellspeicher[i] = vorhanden;
            }
        }

        // ------------------------------------------------------------------
        // Zustand der zweikanaligen Stundenschleife (Paket 5)
        //
        // Bis Paket 4 waren das lokale Variablen von Berechnung_Zweikanalig. Seit
        // Paket 5 führt die Stundenschleife nicht mehr die Wärmepumpe, sondern die
        // KASKADE (Kaskadenschleife): Solarthermie und Heizkessel sind eigene Stufen
        // derselben Schleife geworden, und ein Projekt ohne Wärmepumpe (1017, 1018)
        // braucht die Schleife trotzdem. Deshalb liegen die Größen jetzt als Felder
        // beim Modul und die Phasen als Stundenschritte vor. Die ausgeführten
        // Anweisungen und ihre Reihenfolge sind gegenüber Paket 4 unverändert.
        // ------------------------------------------------------------------

        /// <summary>
        /// Anteil der WP-Produktion, der den Momentanbedarf in Phase B DIREKT gedeckt hat
        /// [kWh] (Paket-5-Nacharbeit, Befund N2). Nur im zweikanaligen Weg gefüllt.
        ///
        /// <c>WP_Waermeproduktion_gesamt = Direktdeckung_gesamt + Speicherladung</c>. Die
        /// Größe ist die Basis des EIGENANTEILS der Wärmepumpe an der Bedarfsdeckung:
        /// Bis zur Nacharbeit bildete <c>SimulationRunner</c> ihn als
        /// „Stufeneingang − Rest nach der Stufe" — mit einem zweiten Erzeuger in der
        /// Speicherstufe enthielt das dessen Lieferung mit, und beide meldeten sie.
        /// </summary>
        public double Direktdeckung_gesamt = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Paket-5-Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        /// Gefüllt von <see cref="Kaskadenschleife"/>; mit genau EINEM Lader je Speicher
        /// ist es dessen gesamte bedarfsdeckende Entladung.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        private int _zkModule = 0;
        private Ladeauftrag[] _zkHauptauftrag = new Ladeauftrag[0];
        private Ladeauftrag[] _zkZweitauftrag = new Ladeauftrag[0];
        private double[] _zkLadeTherm = new double[0];   // Ladepotenzial der Stunde [kWh]
        private double[] _zkLadeEl = new double[0];      // zugehörige Stromaufnahme [kWh]
        private double[] _zkLadeRest = new double[0];    // davon noch nicht verbraucht
        private bool[] _zkPvGebunden = new bool[0];      // Modul im Betriebsmodus PV (13.5)

        /// <summary>
        /// Beginn des zweikanaligen Laufs: Eingangsgrößen festhalten, Zähler nullen,
        /// Senkenspeicher zurücksetzen und die Ladeaufträge je Modul auflösen.
        ///
        /// Voraussetzung: <see cref="Vorbereiten_Zweikanalig"/> ist gelaufen und
        /// <paramref name="kontext"/> ist aufgebaut.
        /// </summary>
        public bool Zweikanalig_Start(Waermekanaele kanaele, Kaskadenkontext kontext)
        {
            if (kanaele == null || kontext == null) return false;

            // Eingangsgrößen als EIGENE Vektoren festhalten (kein Aliasing auf die
            // Kanäle, die gleich fortgeschrieben werden — B0-2).
            Waermebedarf_stuendlich = kanaele.Summe();
            Warmwasserbedarf_stuendlich = (float[])kanaele.WW.Clone();

            WP_Strombedarf_gesamt = 0;
            WP_Waermeproduktion_gesamt = 0;
            Heizstab_gesamt = 0;
            WP_Laufzeit = 0;
            Bivalenzpunkt = -100;

            // Senkenspeicher auf den Laufanfang. QUELLspeicher NICHT: sie starten
            // gefüllt (WaermequelleClass.Quellspeicher setzt SOC = Q_max), ein Reset
            // würde die vorhandene Wärmequelle löschen.
            foreach (SimulationPufferspeicher sp in kontext.AlleSpeicher)
                if (sp != null && !sp.IstQuelle) sp.Reset();

            int module = wp_model.Count;
            _zkModule = module;

            // Ladeaufträge je Modul vorab auflösen — die Ladephase iteriert über die
            // Prioritätsordnung, die Bedarfsphase braucht denselben Auftrag für die
            // Ladefähigkeit als Bezugsgröße.
            Ladeauftrag[] hauptauftrag = new Ladeauftrag[module];
            Ladeauftrag[] zweitauftrag = new Ladeauftrag[module];
            foreach (Ladeauftrag a in kontext.LadenOhnePV)
            {
                if (a == null || a.Erzeugerart != ProjektPuffer.TYP_WP) continue;
                if (a.Modulindex < 0 || a.Modulindex >= module) continue;
                if (a.Zweitsenke) zweitauftrag[a.Modulindex] = a;
                else hauptauftrag[a.Modulindex] = a;
            }

            _zkHauptauftrag = hauptauftrag;
            _zkZweitauftrag = zweitauftrag;
            _zkLadeTherm = new double[module];
            _zkLadeEl = new double[module];
            _zkLadeRest = new double[module];
            _zkPvGebunden = new bool[module];
            return true;
        }

        /// <summary>Stundenbeginn: Ganglinienwerte und Ladepotenziale der Stunde nullen.</summary>
        public void Zweikanalig_StundeStart(int stunde)
        {
            WP_Strombedarf_stuendlich[stunde] = 0;
            WP_Waermeproduktion_stuendlich[stunde] = 0;
            waermerestbedarf_stuendlich[stunde] = 0;
            Heizstab_stuendlich[stunde] = 0;

            Array.Clear(_zkLadeTherm, 0, _zkModule);
            Array.Clear(_zkLadeEl, 0, _zkModule);
            Array.Clear(_zkLadeRest, 0, _zkModule);
            Array.Clear(_zkPvGebunden, 0, _zkModule);
        }

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für die Wärmepumpen-Module:
        /// Bedarfsdeckung in Anlagenpriorität, Kennlinie, Betriebsarten, Sperrzeiten,
        /// Quellbegrenzung und die Bestimmung des Ladepotenzials der Stunde.
        /// </summary>
        /// <returns>false = Abbruch der Kennlinienauswertung.</returns>
        public bool Zweikanalig_Bedarfsphase(int stunde, Kaskadenkontext kontext,
                                             bool pvUeberschuss, double pvRest,
                                             ref double rest_heiz, ref double rest_ww)
        {
                int module = _zkModule;
                Ladeauftrag[] hauptauftrag = _zkHauptauftrag;
                Ladeauftrag[] zweitauftrag = _zkZweitauftrag;
                double[] ladeTherm = _zkLadeTherm;
                double[] ladeEl = _zkLadeEl;
                double[] ladeRest = _zkLadeRest;
                bool[] pvGebunden = _zkPvGebunden;

                for (int index = 0; index < module; index++)
                {
                    WErzeugerModel model = wp_model[index];
                    _Kenndaten kenndaten = wp_kenndaten[index];
                    Senkenzuordnung zuordnung = kontext.SenkeJeModul[index];
                    SimulationPufferspeicher quelle = wp_quellspeicher[index];

                    double[] result = berechne_wptherm(wp_quelltemp[index][stunde], model, kenndaten);
                    if (result[STATUS] == 0)
                    {
                        AbbruchAufraeumen();
                        return false;
                    }

                    // KANALGERECHTE BEZUGSGRÖSSE (Konzept 6.3): der Bedarf der eigenen
                    // Senke bzw. — bei Puffer-Hauptsenke — der Bilanzraum des Speichers
                    // (Ladefähigkeit + absehbare Entnahme, Nutzerentscheidung zu 4b-1).
                    // Im Altpfad steht hier der aggregierte Rest_waerme; das ist im
                    // Speicherbetrieb nicht mehr die maßgebliche Größe.
                    double verfuegbar = Verfuegbar(zuordnung, hauptauftrag[index],
                                                   rest_heiz, rest_ww, pvUeberschuss);

                    // Betriebsarten-Steuerung (unverändert zum Altpfad, nur die
                    // Bezugsgröße des Alternativbetriebs ist jetzt kanalgerecht)
                    if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_TEILPARALLEL)
                    {
                        if (Temperatur[stunde] <= model.Abschaltpunkt)
                        {
                            result[PTHERM] = 0;
                            result[PEL] = 0;
                        }
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_PARALLEL)
                    {
                        // die Wärmepumpe läuft weiter, der zweite Erzeuger unterstützt
                    }
                    else if (model.Bivalenter_Betrieb && model.Betriebsart == DbWerte.WP_BETRIEBSART_ALTERNATIV)
                    {
                        // K-3: identische Semantik wie im Altpfad — die Umschaltung hängt an
                        // der Außentemperatur, nicht mehr an einer Bezugsgröße der Stunde.
                        // Damit entfällt die Kanalfrage hier vollständig (siehe AlternativAus).
                        if (AlternativAus(model, stunde))
                        {
                            result[PTHERM] = 0;
                            result[PEL] = 0;
                        }
                    }

                    // Sperrzeiten berücksichtigen
                    int std = stunde % 24;
                    if (std >= model.Sperrzeit_von && std < model.Sperrzeit_bis && model.Sperrung)
                    {
                        result[PTHERM] = 0;
                        result[PEL] = 0;
                    }

                    // Wärmequelle Pufferspeicher: die Verdampferwärme muss aus dem
                    // Speicher gedeckt werden (Regeneration steht oben, zentral).
                    if (quelle != null && result[PTHERM] > 0)
                    {
                        double quellAnteil = result[PTHERM] - result[PEL];
                        if (quellAnteil > 0 && quelle.SOC < quellAnteil)
                        {
                            double faktor = quelle.SOC / quellAnteil;
                            if (faktor < 0) faktor = 0;
                            result[PTHERM] *= faktor;
                            result[PEL] *= faktor;
                        }
                    }

                    // ABBRUCHBEDINGUNG (Konzept 6.3): aus „kein Bedarf" wird
                    // „kein Bedarf UND kein Ladepotenzial". Ein Modul mit Zweitsenke muss
                    // auch dann laufen, wenn sein Kanal gerade nichts verlangt.
                    bool kannLaden = Ladefaehig(hauptauftrag[index], pvUeberschuss, rest_heiz, rest_ww) ||
                                     Ladefaehig(zweitauftrag[index], pvUeberschuss, rest_heiz, rest_ww);
                    if (verfuegbar <= 0 && !kannLaden) continue;

                    // Betriebsmodus -> Ladepotenzial dieser Stunde
                    LadepotenzialBestimmen(index, zuordnung, result, pvRest,
                                           ladeTherm, ladeEl, pvGebunden);

                    float vorherTherm = WP_Waermeproduktion_stuendlich[stunde];
                    float vorherEl = WP_Strombedarf_stuendlich[stunde];

                    // Bedarfsdeckung NUR bei Hauptsenke Heizkreis. Eine Anlage mit
                    // Puffer-Hauptsenke lädt ausschließlich (Phase C) — genau daraus
                    // folgt, dass es keine Doppelzählung geben kann (Konzept 6.3).
                    if (zuordnung.Haupt == Senke.Heizkreis && verfuegbar > 0)
                    {
                        if (result[PTHERM] < verfuegbar)
                        {
                            WP_Waermeproduktion_stuendlich[stunde] += (float)result[PTHERM];
                            WP_Waermeproduktion_gesamt += result[PTHERM];
                            WP_Strombedarf_stuendlich[stunde] += (float)result[PEL];
                            WP_Strombedarf_gesamt += result[PEL];
                            Modul_WP_Waermeproduktion[index] += result[PTHERM];
                            Modul_WP_Strombedarf[index] += result[PEL];

                            // B0-13 (aus der Parallelsitzung übernommen, Commit ae7b705):
                            // Laufzeit nur bei tatsächlicher Produktion - derselbe Guard
                            // wie im Teillast-Zweig und wie im Altpfad.
                            if (result[PTHERM] > 0)
                            {
                                WP_Laufzeit = WP_Laufzeit + 1;
                                Modul_WP_Laufzeit[index] += 1;
                            }

                            SenkeAbziehen(zuordnung.WSTyp, result[PTHERM], ref rest_ww, ref rest_heiz);
                            Direktdeckung_gesamt += result[PTHERM];   // N2: Eigenanteil
                        }
                        else
                        {
                            WP_Waermeproduktion_stuendlich[stunde] += (float)verfuegbar;
                            WP_Waermeproduktion_gesamt += verfuegbar;
                            WP_Strombedarf_stuendlich[stunde] += (float)verfuegbar / (float)result[COP];
                            WP_Strombedarf_gesamt += verfuegbar / result[COP];
                            Modul_WP_Waermeproduktion[index] += verfuegbar;
                            Modul_WP_Strombedarf[index] += verfuegbar / result[COP];

                            // bei begrenzter Quelle bzw. Sperrzeit kann PTHERM 0 sein
                            if (result[PTHERM] > 0)
                            {
                                WP_Laufzeit = WP_Laufzeit + (verfuegbar / (float)result[PTHERM]);
                                Modul_WP_Laufzeit[index] += (verfuegbar / (float)result[PTHERM]);
                            }

                            SenkeAbziehen(zuordnung.WSTyp, verfuegbar, ref rest_ww, ref rest_heiz);
                            Direktdeckung_gesamt += verfuegbar;       // N2: Eigenanteil
                        }
                    }

                    // Tatsächlich entnommene Quellwärme abziehen. StundeAbschliessen
                    // steht NICHT hier, sondern zentral in Phase G.
                    double erzeugt = WP_Waermeproduktion_stuendlich[stunde] - vorherTherm;
                    double strom = WP_Strombedarf_stuendlich[stunde] - vorherEl;
                    if (quelle != null)
                    {
                        double entnahme = erzeugt - strom;
                        if (entnahme > 0) quelle.Entladen(entnahme, stunde);
                    }

                    // Was vom Ladepotenzial nach der Bedarfsdeckung übrig ist, steht den
                    // Phasen C und D zur Verfügung.
                    ladeRest[index] = ladeTherm[index] - erzeugt;
                    if (ladeRest[index] < 0) ladeRest[index] = 0;

                } // end alle WP-Module

            return true;
        }

        /// <summary>Stundenende: der Restbedarf der Stunde nach allen Phasen.</summary>
        public void Zweikanalig_StundeEnde(int stunde, double rest_heiz, double rest_ww)
        {
            waermerestbedarf_stuendlich[stunde] = (float)(rest_heiz + rest_ww);
        }

        /// <summary>Abschluss des zweikanaligen Laufs: Sortierung, Jahressummen, Bivalenzpunkt.</summary>
        public void Zweikanalig_Ende(List<double> biv)
        {
            WPPlan.Core.BhkwPlan.Heapsort(WP_Waermeproduktion_stuendlich, WP_Waermeproduktion_stuendlich_sortiert);

            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf_stuendlich, value => Waermebedarf_gesamt += value);
            waermerestbedarf_gesamt = 0;
            Array.ForEach(waermerestbedarf_stuendlich, value => waermerestbedarf_gesamt += value);

            Cursor.Current = Cursors.Default;

            if (biv != null && biv.Count > 0)
                Bivalenzpunkt = biv.Max();
        }

        /// <summary>
        /// Bezugsgröße eines Moduls in dieser Stunde — die DREI Fälle aus Konzept 6.3:
        /// Warmwasserkanal, Heizkanal (bzw. beides mit Warmwasservorrang) und, dritter
        /// Fall, der BILANZRAUM der Hauptsenke bei einer Anlage, die einen Puffer lädt.
        ///
        /// Der dritte Fall stand bis zur Paket-4-Review auf der reinen Ladefähigkeit
        /// <c>Q_max · Obergrenze − SOC</c>. Damit drosselte der Speicherinhalt den
        /// Stundendurchsatz der Anlage (Befund 4b-1): Ein 600-l-Puffer ließ höchstens
        /// ~13 kWh/h durch, während der Momentanbedarf ein Vielfaches betragen kann.
        /// Fachlich ist ein Pufferspeicher aber eine hydraulische Weiche — er wird
        /// geladen, WÄHREND die Last aus ihm entnimmt. Nach der Nutzerentscheidung vom
        /// 14.08.2026 ist die Bezugsgröße deshalb
        /// <c>Ladefähigkeit + min(offener Kanalbedarf, Entnahmefähigkeit)</c>
        /// (<see cref="SimulationPufferspeicher.Bilanzraum"/>). Die Phasenstruktur bleibt
        /// unangetastet: Phase C lädt weiterhin ohne <c>SenkeAbziehen</c>, Phase E
        /// entlädt — nur darf die Aufnahme einer Stunde jetzt die freie Kapazität um die
        /// im selben Zeitschritt absehbare Entnahme übersteigen.
        /// </summary>
        private double Verfuegbar(Senkenzuordnung zuordnung, Ladeauftrag haupt,
                                  double rest_heiz, double rest_ww, bool pvUeberschuss)
        {
            if (zuordnung == null) return rest_ww + rest_heiz;

            if (zuordnung.Haupt != Senke.Heizkreis)
            {
                if (haupt == null || haupt.Speicher == null) return 0;
                return haupt.Speicher.Bilanzraum(haupt.ObergrenzeStunde(pvUeberschuss),
                                                 Kanalbedarf(haupt.Speicher, rest_heiz, rest_ww));
            }

            if (zuordnung.WSTyp == WaermequelleClass.SENKE_WARMWASSER) return rest_ww;
            if (zuordnung.WSTyp == WaermequelleClass.SENKE_HEIZUNG) return rest_heiz;
            return rest_ww + rest_heiz;
        }

        /// <summary>
        /// Offener Bedarf des Kanals, den DIESER Speicher bedient (Konzept 3.2). Ein
        /// Brauchwasserspeicher sieht den WW-Kanal, jeder andere den Heizkanal — dieselbe
        /// Regel wie bei der Entladung (<see cref="EntladeKanal"/>).
        /// </summary>
        private static double Kanalbedarf(SimulationPufferspeicher sp, double rest_heiz, double rest_ww)
        {
            if (sp == null) return 0;
            return sp.IstBrauchwasserkanal ? rest_ww : rest_heiz;
        }

        /// <summary>
        /// UMSCHALTKRITERIUM DES BIVALENT-ALTERNATIVEN BETRIEBS (K-3, Nutzerentscheidung
        /// vom 15.08.2026 zu <c>Konzept_KonfigUI_Hydraulik.md</c> Abschnitt 8).
        /// true = die Wärmepumpe ist in dieser Stunde abgeschaltet, der zweite Erzeuger
        /// übernimmt den Heizbetrieb allein.
        ///
        /// <para>
        /// Maßgeblich ist die AUSSENTEMPERATUR gegen die Bivalenztemperatur
        /// <c>Tab_Energieanlagen.Abschaltpunkt</c> — dieselbe Größe und dieselbe Spalte,
        /// die der teilparallele Zweig auswertet. Unterhalb der Bivalenztemperatur ist die
        /// Wärmepumpe aus; ab ihr läuft sie mit ihrer Leistung, und was sie in einer
        /// einzelnen Stunde nicht deckt, geht regulär an die nächste Kaskadenstufe.
        /// </para>
        ///
        /// <para>
        /// <b>Was das ablöst.</b> Bis zu dieser Änderung stand hier
        /// <c>if (result[PTHERM] &lt; Rest_waerme)</c> — die Wärmepumpe fiel in JEDER Stunde
        /// aus, die sie nicht vollständig deckte. Das ist keine Bivalenzregelung, sondern
        /// eine Leistungsprüfung: Sie traf einzelne Sommer-Warmwasserspitzen genauso wie
        /// Frosttage, erzeugte stündliches Pendeln zwischen Wärmepumpe und Kessel und
        /// hing an der Bedarfsganglinie statt am Außenklima. Der gepflegte
        /// <c>Abschaltpunkt</c> blieb dabei wirkungslos.
        /// </para>
        ///
        /// <para>
        /// <b>Bezugsgröße entfällt.</b> Im zweikanaligen Weg beantwortete
        /// <c>AlternativBezug</c> bis hierher die Frage, GEGEN WELCHEN Bedarf verglichen
        /// wird (Kanalbedarf statt Bilanzraum des Speichers, Paket-4-Review Punkt 1).
        /// Mit dem Temperaturkriterium stellt sich die Frage nicht mehr — verglichen wird
        /// Temperatur gegen Temperatur —, deshalb ist die Methode entfallen. Der Vorteil
        /// ist zugleich der Kern der Entscheidung: Die Abschaltung hängt weder am
        /// Momentanbedarf noch am Füllstand eines Puffers.
        /// </para>
        ///
        /// <para>
        /// <b>Vergleichsgrenze.</b> Abgeschaltet wird bei ECHTER Unterschreitung
        /// (<c>&lt;</c>); bei genau der Bivalenztemperatur läuft die Wärmepumpe noch. Der
        /// teilparallele Zweig schaltet dagegen schon bei <c>&lt;=</c> ab. Der Unterschied
        /// betrifft ausschließlich die Stunden mit exakter Gleichheit; der Teilparallel-Zweig
        /// bleibt bewusst unverändert, weil er nicht Gegenstand von K-3 ist.
        /// </para>
        ///
        /// <para>
        /// <b>Der Spaltenwert gilt immer wörtlich, auch 0 °C.</b> <c>Abschaltpunkt</c> ist
        /// im Bestand in keiner Zeile NULL, und der Wizard schreibt über
        /// <c>double.Parse</c> stets einen Wert (Vorbelegung des Eingabefelds: 0). Ein
        /// „nicht gepflegt" ist im Datenmodell daher nicht von einer bewusst gewählten
        /// Bivalenztemperatur von 0 °C zu unterscheiden — und 0 °C ist ein plausibler,
        /// gebräuchlicher Wert. Eine Ersatzregel für 0 würde also genau die Anlagen
        /// falsch rechnen, die den Wert gewollt so führen. Stattdessen weist
        /// <see cref="AlternativHinweisPruefen"/> beim Modulaufbau einmalig darauf hin,
        /// dass der Vorbelegungswert wirksam ist.
        /// </para>
        /// </summary>
        private bool AlternativAus(WErzeugerModel model, int stunde)
        {
            return Temperatur[stunde] < model.Abschaltpunkt;
        }

        /// <summary>
        /// Einmaliger Hinweis beim Modulaufbau, wenn eine bivalent-alternative Anlage die
        /// Bivalenztemperatur auf dem Vorbelegungswert 0 °C führt (K-3).
        ///
        /// Rein informativ — die Rechnung ist davon unberührt, der Wert gilt wörtlich
        /// (siehe <see cref="AlternativAus"/>). Der Hinweis existiert, weil 0 °C zugleich
        /// der Wert ist, den eine nie geöffnete Eingabemaske hinterlässt: Wer die Anlage
        /// nur auf „Alternativbetrieb" gestellt hat, soll sehen, mit welcher
        /// Bivalenztemperatur gerechnet wurde.
        /// </summary>
        private void AlternativHinweisPruefen(WErzeugerModel model)
        {
            if (model == null) return;
            if (!model.Bivalenter_Betrieb) return;
            if (model.Betriebsart != DbWerte.WP_BETRIEBSART_ALTERNATIV) return;
            if (model.Abschaltpunkt != 0) return;

            SimulationProtokoll.Aktuell.HinweisEinmal(
                "wp-alternativ-bivalenztemperatur-0-" + model.ID,
                MyResource.Resource.SIMENG_PRAEFIX_WAERMEPUMPE +
                string.Format(MyResource.Resource.SIMENG_WP_BIVALENZTEMPERATUR_VORBELEGUNG,
                              model.Bezeichner));
        }

        /// <summary>
        /// true, wenn der Auftrag in dieser Stunde noch Wärme aufnehmen kann — Ladung in
        /// den Speicher ODER Durchsatz zur Last (Bilanzraum, siehe <see cref="Verfuegbar"/>).
        /// </summary>
        private static bool Ladefaehig(Ladeauftrag auftrag, bool pvUeberschuss,
                                       double rest_heiz, double rest_ww)
        {
            if (auftrag == null || auftrag.Speicher == null) return false;

            return auftrag.Speicher.Bilanzraum(auftrag.ObergrenzeStunde(pvUeberschuss),
                                               Kanalbedarf(auftrag.Speicher, rest_heiz, rest_ww)) > 0;
        }

        /// <summary>
        /// Ladepotenzial der Stunde je Modul nach Betriebsmodus (Konzept 3.5, 13.5).
        ///
        ///   Laufzeit  — volle Leistung, der Überschuss darf laden,
        ///   Leistung  — kein Überschuss; die WP moduliert exakt auf den Bedarf,
        ///   PV        — Überschuss nur, soweit PV-Strom verfügbar ist.
        ///
        /// DAS PV-BUDGET WIRD HIER NICHT VERBRAUCHT (Paket-4-Review, Punkt 2).
        /// <paramref name="pvRest"/> geht als OBERGRENZE ein, abgezogen wird es erst in
        /// der Ladephase, und zwar um die TATSÄCHLICH geladene Menge (13.5). Der Grund
        /// ist die Reihenfolge der Phasen: Das Potenzial aller Module steht in Phase B
        /// fest, geladen wird erst in C und D. Hätte Modul 1 sein Potenzial hier
        /// abgebucht, es dann aber nicht unterbringen können — weil sein Speicher voll
        /// ist oder gar keiner zugeordnet ist —, wäre das Budget für Modul 2 schon
        /// verbraucht, ohne dass eine kWh PV-Strom geflossen wäre. Nicht untergebrachtes
        /// Potenzial bleibt so dem nächsten Modul erhalten; die modulübergreifende
        /// Reihenfolge (Anlagenpriorität) bleibt dieselbe, weil die Ladephase die
        /// Aufträge in Ladeprioritätsordnung abarbeitet und dabei sequenziell abbucht.
        ///
        /// SONDERFALL PUFFER-HAUPTSENKE: Dort IST die Ladung der Auftrag und nicht der
        /// Überschuss über einen Bedarf hinaus. „Leistung" hätte hier keine Bedeutung —
        /// der Bilanzraum begrenzt ohnehin —, deshalb gilt für diese Anlagen dieselbe
        /// Regel wie für „Laufzeit". Andernfalls stünde eine korrekt konfigurierte
        /// Speicheranlage still.
        /// </summary>
        private void LadepotenzialBestimmen(int index, Senkenzuordnung zuordnung, double[] result,
                                            double pvRest, double[] ladeTherm, double[] ladeEl,
                                            bool[] pvGebunden)
        {
            string modus = wp_modus[index];
            bool pufferSenke = zuordnung != null && zuordnung.Haupt != Senke.Heizkreis;

            if (modus == WaermequelleClass.MODUS_PV)
            {
                pvGebunden[index] = true;

                double copPV = result[COP] > 0 ? result[COP] : 1;
                double maxThermPV = pvRest * copPV;
                double thermPV = Math.Min(result[PTHERM], maxThermPV);
                if (thermPV > 0)
                {
                    ladeTherm[index] += thermPV;
                    ladeEl[index] += thermPV / copPV;
                }
                return;
            }

            if (modus == WaermequelleClass.MODUS_LEISTUNG && !pufferSenke)
                return;   // kein Ladepotenzial

            ladeTherm[index] += result[PTHERM];
            ladeEl[index] += result[PEL];
        }

        /// <summary>
        /// Phasen C/D für EINEN Ladeauftrag (Konzept 6.3): der Anweisungsblock, der bis
        /// Paket 4 im Rumpf von <c>Ladephase</c> stand. Die Schleife über die
        /// kaskadenübergreifende Prioritätsordnung führt seit Paket 5 die
        /// <see cref="Kaskadenschleife"/> — sie muss Wärmepumpen, Solarthermie und
        /// Heizkessel in EINER Ordnung abarbeiten, und die Ordnung gehört nicht in ein
        /// einzelnes Erzeugermodul. Die ausgeführten Anweisungen sind unverändert; aus
        /// jedem <c>continue</c> ist ein <c>return 0</c> geworden.
        ///
        /// KEIN <c>SenkeAbziehen</c> — die geladene Wärme deckt keinen Bedarf, sie liegt
        /// im Speicher. Genau hier entstünde sonst die Doppelzählung, die der
        /// Deckungsgrad in der Detailansicht durch seine 100-%-Kappung verstecken würde.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss,
                                        double[] absehbar, ref double pvRest)
        {
                double[] ladeTherm = _zkLadeTherm;
                double[] ladeEl = _zkLadeEl;
                double[] ladeRest = _zkLadeRest;
                bool[] pvGebunden = _zkPvGebunden;

                if (a == null) return 0;

                int index = a.Modulindex;
                if (index < 0 || index >= ladeRest.Length) return 0;
                if (ladeRest[index] <= 0) return 0;

                SimulationPufferspeicher sp = a.Speicher;
                if (sp == null) return 0;

                // BILANZRAUM statt reiner Ladefähigkeit (Nutzerentscheidung zu 4b-1):
                // Was in dieser Stunde ohnehin wieder entnommen wird, darf der Speicher
                // zusätzlich aufnehmen — er ist eine hydraulische Weiche. Das Budget je
                // Kanal wird nur einmal vergeben (absehbar), damit zwei Speicher desselben
                // Kanals nicht dieselbe Entnahme doppelt durchreichen.
                int kanal = sp.IstBrauchwasserkanal ? 1 : 0;
                double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
                double durchlass = Math.Min(absehbar[kanal] > 0 ? absehbar[kanal] : 0,
                                            sp.Entnahmefaehigkeit());
                if (ladefaehig + durchlass <= 0) return 0;

                double menge = Math.Min(ladeRest[index], ladefaehig + durchlass);

                // Stromseite über den mittleren COP des Ladepotenzials — dieselbe
                // Verbuchung wie im Bestand (dort copMittel aus dem Modulsummen-Paar).
                double cop = ladeEl[index] > 0 ? ladeTherm[index] / ladeEl[index] : 0;
                if (cop <= 0) return 0;

                // PV-BUDGET (13.5, Paket-4-Review Punkt 2): Ein Modul im Betriebsmodus PV
                // lädt nur, soweit PV-Strom übrig ist. Abgebucht wird die tatsächlich
                // geladene Menge — weiter unten, nach sp.Laden.
                if (pvGebunden[index])
                {
                    double maxTherm = pvRest * cop;
                    if (menge > maxTherm) menge = maxTherm;
                }

                // QUELLBILANZ AUCH BEIM LADEN (Konzept 6.3, Prüfbefund): Die
                // Speicherladung entnimmt der Quelle heute keine Wärme — im
                // zweikanaligen Weg tut sie es, und die Quelle begrenzt die Ladung.
                SimulationPufferspeicher quelle = wp_quellspeicher[index];
                if (quelle != null && quelle.Q_max > 0 && cop > 1)
                {
                    double entnahmeVoll = menge * (1.0 - 1.0 / cop);
                    if (entnahmeVoll > quelle.SOC)
                    {
                        double faktor = entnahmeVoll > 0 ? quelle.SOC / entnahmeVoll : 0;
                        if (faktor < 0) faktor = 0;
                        menge *= faktor;
                    }
                }
                if (menge <= 0) return 0;

                double ladung = sp.Laden(menge, stunde, durchlass);
                if (ladung <= 0) return 0;

                // Verbrauchtes Durchsatzbudget des Kanals abbuchen: alles, was über die
                // Ladefähigkeit hinausging, ist die Menge, die Phase E wieder entnimmt.
                double genutzterDurchlass = ladung - ladefaehig;
                if (genutzterDurchlass > 0)
                {
                    absehbar[kanal] -= genutzterDurchlass;
                    if (absehbar[kanal] < 0) absehbar[kanal] = 0;
                }

                if (pvGebunden[index])
                {
                    pvRest -= ladung / cop;
                    if (pvRest < 0) pvRest = 0;
                }

                ladeRest[index] -= ladung;
                if (ladeRest[index] < 0) ladeRest[index] = 0;

                WP_Waermeproduktion_stuendlich[stunde] += (float)ladung;
                WP_Waermeproduktion_gesamt += ladung;
                Modul_WP_Waermeproduktion[index] += ladung;

                double strom = ladung / cop;
                WP_Strombedarf_stuendlich[stunde] += (float)strom;
                WP_Strombedarf_gesamt += strom;
                Modul_WP_Strombedarf[index] += strom;

                if (ladeTherm[index] > 0)
                {
                    WP_Laufzeit += ladung / ladeTherm[index];
                    Modul_WP_Laufzeit[index] += ladung / ladeTherm[index];
                }

                if (quelle != null)
                {
                    double entnahme = ladung - strom;
                    if (entnahme > 0) quelle.Entladen(entnahme, stunde);
                }

                return ladung;
        }

        /// <summary>
        /// Phase F: Heizstab auf den Kanalrest, mit der Semantik des Bestands — er sieht
        /// den AGGREGIERTEN Rest und ist je Modul auf <c>Tab_WP.Heizung</c> begrenzt.
        /// Die Aufteilung auf die Kanäle folgt dem Warmwasservorrang von
        /// <c>SENKE_BEIDES</c>; die Additionslogik ist die aus B0-5 (<c>+=</c> statt
        /// <c>=</c>, sonst überschreiben sich die Modulbeiträge in der Ganglinie).
        /// </summary>
        public void Heizstabphase(int stunde, ref double rest_heiz, ref double rest_ww)
        {
            if (!Mit_Heizstab) return;

            for (int index = 0; index < wp_model.Count; index++)
            {
                double rest = rest_heiz + rest_ww;
                if (rest <= 0) break;
                if (WP_Heizung[index] <= 0) continue;

                double menge = Math.Min(rest, WP_Heizung[index]);
                Heizstab_stuendlich[stunde] += (float)menge;
                Heizstab_gesamt += menge;
                Modul_Heizstab[index] += menge;

                SenkeAbziehen(WaermequelleClass.SENKE_BEIDES, menge, ref rest_ww, ref rest_heiz);
            }
        }

        /// <summary>
        /// Aufräumen beim Abbruch der Kennlinienauswertung — dieselben Größen wie im
        /// Altpfad, damit ein abgebrochener Lauf keine Teilergebnisse hinterlässt.
        /// </summary>
        private void AbbruchAufraeumen()
        {
            for (int i = 0; i < MAX_WP; i++)
            {
                Modul_WP_Strombedarf[i] = 0;
                Modul_WP_Waermeproduktion[i] = 0;
                Modul_Heizstab[i] = 0;
                Modul_WP_Laufzeit[i] = 0;
                WP_Modul[i] = "";
            }
            WP_Waermeproduktion_gesamt = 0;
            WP_Strombedarf_gesamt = 0;
            Heizstab_gesamt = 0;
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Zieht die erzeugte Wärmemenge vom passenden Bedarfsanteil ab.
        /// Bei der Wärmesenke "Beides" gilt Warmwasservorrang: zuerst wird der
        /// Warmwasserbedarf gedeckt, der Rest geht auf die Heizwärme.
        /// </summary>
        private void SenkeAbziehen(string senke, double menge, ref double rest_ww, ref double rest_heiz)
        {
            // EINE Implementierung für alle Erzeugerstufen (Paket 5): Die Regel steht seit
            // der Aufteilung der Stundenschleife in Kaskadenschleife.SenkeAbziehen, damit
            // Wärmepumpe, Solarthermie, Heizkessel und die Speicherentladung denselben
            // Warmwasservorrang benutzen. Der Rumpf ist unverändert übernommen; der
            // einkanalige Altpfad ruft weiter diese Methode.
            Kaskadenschleife.SenkeAbziehen(senke, menge, ref rest_ww, ref rest_heiz);
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

            if (kenndaten.anz < 2)
            {
                result[0] = 1;
                result[1] = cop_maxSST;
                result[2] = ptherm_maxSST;
                result[3] = ptherm_maxSST / cop_maxSST; // pel
                return result;   
            }


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
                    // PAKET 8 (Konzept 13.4) — die EINZIGE echte Interaktion der Engine
                    // ist zur Vorab-Einstellung geworden.
                    //
                    // BISHER: MessageBox mitten in der Stundenschleife, „soll
                    // extrapoliert werden? Bei nein wird Simulation abgebrochen!". Jeder
                    // unbeaufsichtigte Lauf blieb daran hängen; die Referenzlauf-Suite
                    // musste einen Dialogwächter mitlaufen lassen, der "Ja" drückte.
                    //
                    // JETZT: Tab_Einstellungen.Extrapolation_erlaubt entscheidet vorab.
                    //   erlaubt (Vorbelegung, entspricht dem bisherigen "Ja"):
                    //       es wird extrapoliert wie bisher - Zeile für Zeile derselbe
                    //       Rechenweg - und der Lauf vermerkt es EINMAL im Protokoll.
                    //       Damit ist der Grenzfall erstmals sichtbar statt stumm.
                    //   verboten:
                    //       Abbruch über den Fehlerkanal, mit demselben Ausgang wie das
                    //       bisherige "Nein" (result[STATUS] = 0), aber mit sprechendem
                    //       Text statt einer Dialogantwort.
                    //
                    // Die Meldung steht in beiden Zweigen hinter dem extrapolation-Flag
                    // bzw. hinter HinweisEinmal: Der Fall tritt je Modul in bis zu 8760
                    // Stunden auf.
                    if (!extrapolation)
                    {
                        string bezeichner = (model != null && model.Bezeichner != null) ? model.Bezeichner : "";
                        string untergrenze = kenndaten.dat[kenndaten.anz - 1].Temperatur.ToString("F1");

                        if (!Extrapolation_Erlaubt)
                        {
                            Fehlertext = string.Format(
                                MyResource.Resource.SIMENG_WP_EXTRAPOLATION_VERBOTEN,
                                bezeichner, untergrenze);
                            SimulationProtokoll.Aktuell.Fehlermeldung(
                                MyResource.Resource.SIMENG_PRAEFIX_WAERMEPUMPE + Fehlertext);
                            result[0] = 0;
                            return result;
                        }

                        extrapolation = true;
                        // Der Einmal-Schlüssel ist sprachneutral (Schicht 2) - er darf sich
                        // mit der Oberflächensprache NICHT ändern, sonst käme die Meldung
                        // in einer Sprache mehrfach und in der anderen gar nicht.
                        SimulationProtokoll.Aktuell.HinweisEinmal(
                            "WP_Extrapolation_" + bezeichner + "_" + kenndaten.Vorlauf,
                            string.Format(MyResource.Resource.SIMENG_WP_EXTRAPOLATION_HINWEIS,
                                          bezeichner, untergrenze));
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
            // Quellenbezogene Listen mit zurücksetzen. Sie werden in Berechnung() neu
            // aufgebaut - ein Lauf, der die Wärmepumpe NICHT rechnet (Gewerk abgewählt)
            // oder vorzeitig abbricht, käme sonst nie dazu: SimulationControl.AlleSpeicher()
            // liefert dann weiter die Quellspeicher des Vorlaufs, und der veraltete
            // Speicher landete in Anzeige, CSV-Export und Tab_ErgebnisPufferspeicher.
            // Die Clear()-Aufrufe am Anfang von Berechnung() bleiben stehen (Init wird
            // dort als Erstes gerufen) - doppeltes Leeren ist unschädlich.
            wp_quelltemp.Clear();
            wp_quellspeicher.Clear();
            wp_typ.Clear();

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
            // B0-7: Bilanzgrößen mit zurücksetzen — bei einem Abbruch der Berechnung
            // blieben sonst Werte des Vorlaufs stehen und BaueErgebnis meldete eine
            // falsche Deckung/einen falschen Restbedarf.
            Waermebedarf_gesamt = 0;
            waermerestbedarf_gesamt = 0;
            Bivalenzpunkt = -100;

            // Paket-5-Nacharbeit N2: Eigenanteils-Größen des zweikanaligen Wegs. Im
            // Altpfad bleiben sie auf 0 - dort bildet SimulationRunner den Deckungsgrad
            // unverändert nach der Formel aus B0-7b.
            Direktdeckung_gesamt = 0;
            Speicherentladung_Anteil = 0;
        }
    }
}

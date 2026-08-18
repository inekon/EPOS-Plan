using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{

    public class SimulationBHKW
    {
        public List<int> bhkw_list = new List<int>();
        public List<string> bhkw_list_Namen = new List<string>();

        public Dictionary<string, int> bhkw_carrier = new Dictionary<string, int>();


        /// <summary>
        /// <c>Tab_Energieanlagen.ID</c> je BHKW, INDEXGLEICH zu <see cref="bhkw_list"/>
        /// (Konzept 6.2). Gefüllt von <c>SimulationControl.BHKW_Liste_Laden</c>.
        ///
        /// <see cref="bhkw_list"/> trägt die <c>ID_BHKW</c> — die KATALOGZEILE, nicht die
        /// Anlage. Senke, Ladepriorität und Speicherzuordnung hängen aber an der Anlage;
        /// zwei BHKW desselben Typs im Projekt wären über <see cref="bhkw_list"/> nicht
        /// unterscheidbar.
        ///
        /// SEIT PAKET 6 ausgewertet: Der zweikanalige Weg löst darüber die Senke,
        /// die Ladepriorität und den Ladeauftrag jeder BHKW-Anlage auf und ersetzt damit
        /// den skalaren <see cref="kapazitaetPendelspeicher"/> durch einen zugeordneten
        /// <see cref="SimulationPufferspeicher"/> (Konzept 6.5, zweiter Punkt). Der
        /// einkanalige Altpfad liest die Liste weiterhin nicht.
        /// </summary>
        public List<int> bhkw_anlagen_ids = new List<int>();

        public int m_ID_Projekt = 0;

        public float[] waermebedarf = new float[8760];
        public float[] strombedarf = new float[8760];
        public float[] stromproduktion = new float[8760];
        public float[] waermerestbedarf = new float[8760];
        public float[] waermeproduktion = new float[8760];

        public float[] s_waerme_MWh = new float[10]; // Summenzähler für Wärme (10 Module laut VBA-Code)
        public float[] s_strom_MWh = new float[10];  // Summenzähler für Strom (10 Module laut VBA-Code)
        public float[] s_waerme_ueberschuss = new float[10]; // Summenzähler für Wärmeüberschuss (10 Module laut VBA-Code)

        public int modeBHKW;

        float[] bhkwWaermeLeistung = new float[10];
        float[] bhkwStromLeistung = new float[10];
        public float[] bhkwGrenzL = new float[10];
        public float bhkwGrenzleistungAllgemein = 0;
        public float Waermeueberschuss = 0f;

        // PAKET BHKW-REGULÄR: Hier standen die drei Solar-Felder (solarVorhanden,
        // solarSpeicher, solarWaerme) des einkanaligen Altpfads. Sie waren eine
        // VBA-Erbschaft ohne Anbindung - solarVorhanden war fest false, und
        // SolareErzeugung() lieferte als Platzhalter immer 0. Mit dem Altpfad sind sie
        // ersatzlos entfallen; die Solarthermie rechnet in ihrem eigenen Modul
        // (SimulationSolarthermie) und speist die Kanäle über die Speicherstufe.

        /// <summary>
        /// Speicherraum-Skalar der gemeinsamen Motorläufe [kWh].
        ///
        /// Historisch die Kapazität des BHKW-Pendelspeichers im einkanaligen Altpfad; der
        /// Name ist seit PAKET BHKW-REGULÄR eine Altlast. Der Altpfad, der ihn aus einem
        /// Volumen befüllte, ist entfallen — heute setzt <see cref="Fahrweise_Stunde"/> ihn
        /// je Stunde als SPIEGEL des zugeordneten <see cref="SimulationPufferspeicher"/>
        /// (Ladefähigkeit bzw. Bilanzraum, Konzept 6.5 zweiter Punkt).
        /// <c>SimulationControl</c> setzt das Feld beim Vorbereiten ausdrücklich auf 0,
        /// damit ein versehentlicher Rückgriff auf einen Vorlaufwert sofort auffiele.
        /// </summary>
        public float kapazitaetPendelspeicher = 0.0f; // Pendelspeicher = 0

        int anzahlBhkw = 0;

        // --- Neue Ergebnis- und Verbrauchsvariablen aus VBA ---
        public float BruttoBHKWErzeugung = 0f;
        public float Waermeproduktion_BHKW_MWh = 0f;
        public float Stromproduktion_BHKW_MWh = 0f;
        public float Gasspitze_BHKW = 0f;

        public float Gasverbrauch_BHKW = 0f;
        public float Oelverbrauch_BHKW = 0f;
        public float Rapsoelverbrauch_BHKW = 0f;
        public float Holzmenge_BHKW = 0f;
        public float Sonstigemenge_BHKW = 0f;
        public double Koks_BHKW = 0;
        public double Kohle_BHKW = 0;
        public double Pellets_BHKW = 0;
        public double TierischeFette_BHKW = 0;
        public float Stromverbrauch_BHKW = 0f; // Nie befüllt, nirgends gelesen — Brennstoff 13 (Strom) läuft in Auswertung() in den Sammelposten Sonstigemenge_BHKW

        //public float Biogasverbrauch_BHKW = 0f;
        //public float Fluessiggasverbrauch_BHKW = 0f;
        //public float BioErdgasverbrauch = 0f;
        //public float BioErdgasleistung = 0f;


        // Emissionswerte
        public float Em_CO2_BHKW = 0f;
        public float Em_SO2_BHKW = 0f;
        public float Em_NOX_BHKW = 0f;
        public float Em_CO_BHKW = 0f;
        public float Em_Staub_BHKW = 0f;

        // Laufzeiten
        //
        // ETAPPE E2 — WAS DIESE DREI FELDER WIRKLICH SIND. Laufzeiten[i] entsteht in
        // Auswertung() als Waerme_MWh[i] / Waermeleistung[i] * 1000, ist also eine
        // THERMISCHE VOLLBENUTZUNGSSTUNDENZAHL und KEINE Betriebsstundenzahl: Taktung
        // (An/Aus innerhalb einer Stunde, Teillast) bildet das Modell nicht ab. Ein
        // Modul, das ein Jahr lang zur Haelfte moduliert laeuft, hat 8.760
        // Betriebsstunden, aber nur 4.380 thermische Vbh.
        //
        // Betriebsstunden ist die SUMME dieser Groesse ueber alle Module und kann
        // deshalb 8.760 h ueberschreiten - bei zwei voll ausgelasteten Modulen sind es
        // 17.520. Der Name ist Altbestand aus dem VBA-Vorlaeufer und wird hier bewusst
        // nicht umbenannt (Tab_ErgebnisBHKW.Betriebsstunden_Gesamt haengt daran); die
        // ANZEIGE benennt die Groesse seit E2 richtig, und die neuen Felder darunter
        // tragen die Groesse, an der der KWK-Zuschlag haengt.
        public float Betriebsstunden = 0f;
        public float dLaufzeiten = 0f;
        public float[] Laufzeiten = new float[10];

        /// <summary>
        /// ETAPPE E2 — ELEKTRISCHE Vollbenutzungsstunden je Modul [h/a]:
        /// <c>Stromproduktion [MWh] × 1000 / P_el [kW]</c>.
        ///
        /// <para>Das ist die Groesse, an der der KWK-Zuschlag haengt (KWKG 2025 § 8:
        /// Zuschlag je Kilowattstunde KWK-Strom, gedeckelt ueber Vollbenutzungsstunden).
        /// Sie kann 8.760 h konstruktiv nicht ueberschreiten, weil ein Modul nie mehr
        /// Strom erzeugt als seine Nennleistung mal Jahresstunden.</para>
        ///
        /// <para>0, solange <see cref="bhkwStromLeistung"/> des Moduls 0 ist — dann ist
        /// P_el im Katalog nicht gepflegt und eine Vbh-Zahl waere frei erfunden.</para>
        /// </summary>
        public float[] VbhElektrisch = new float[10];

        /// <summary>
        /// ETAPPE E2 — LEISTUNGSGEWICHTETE elektrische Vollbenutzungsstunden der ganzen
        /// Anlage [h/a]: <c>Σ Stromproduktion [MWh] × 1000 / Σ P_el [kW]</c>.
        ///
        /// <para>Bewusst NICHT die Summe der Modulwerte (die koennte wie
        /// <see cref="Betriebsstunden"/> ueber 8.760 h steigen) und auch nicht deren
        /// arithmetisches Mittel (das gewichtete ein 5-kW-Modul so stark wie ein
        /// 500-kW-Modul). Die leistungsgewichtete Groesse ist die einzige, die die
        /// Aussage „so viele Vollbenutzungsstunden hat die installierte elektrische
        /// Leistung erreicht" traegt — und genau die deckelt der Zuschlag, solange er
        /// projektweit gerechnet wird (Etappe E6 macht ihn modulscharf).</para>
        ///
        /// <para>0, wenn keine elektrische Leistung gepflegt ist.</para>
        /// </summary>
        public float VbhElektrischGesamt = 0f;

        // Arrays für die Modulkonfigurationen zur späteren Berechnung
        private int[] bhkwBrennstoffart = new int[10];
        private float[] bhkwWirkungsgrad = new float[10];
        private float[] bhkwSKZ = new float[10];
        private float[] bhkwCO2Factor = new float[10];
        private float[] bhkwSO2Factor = new float[10];
        private float[] bhkwNOXFactor = new float[10];
        private float[] bhkwCOFactor = new float[10];
        private float[] bhkwStaubFactor = new float[10];

        public SimulationBHKW()
        {

            anzahlBhkw = bhkw_list.Count;

            BHKWCtrl ctrl = new BHKWCtrl();
            for (int i = 0; i < anzahlBhkw; i++)
            {
                ctrl.ReadSingle(bhkw_list[i]);
                bhkwWaermeLeistung[i] = (float)ctrl.m_Ptherm;
                bhkwStromLeistung[i] = (float)ctrl.m_Pel;
                //hkwGrenzL[i] = (float)ctrl.m_Grenzleistung;
            }
        }

        // =====================================================================
        // PAKET BHKW-REGULÄR (Entscheidung des Anwenders 17.08.2026, revidiert 6-1):
        //
        // Hier stand Berechnung(int ProjektID) - der EINSTIEG DES EINKANALIGEN ALTPFADS.
        // Er verteilte den Lauf nach modeBHKW auf drei Jahresschleifen
        // (BhkwSimulationWaermegefuehrt, SimulationStromgefuehrt,
        // SimulationOhneEinspeisung) und rechnete gegen den skalaren Pendelspeicher.
        //
        // Alle vier Methoden sind ersatzlos entfallen. Der EINZIGE Einstieg ist jetzt
        // Berechnung_Zweikanalig bzw. - als Mitglied der Speicherstufe - die Kette
        // Vorbereiten_Zweikanalig / Stunde_Start / Stunde_Bedarf / Zweikanalig_Laden /
        // Stunde_Ende / Abschluss_Zweikanalig.
        //
        // WAS AN PHYSIK ERHALTEN BLIEB: die drei Motorläufe (Motorlauf_Waermegefuehrt,
        // Motorlauf_Stromgefuehrt, Motorlauf_OhneEinspeisung). Sie bestimmen unverändert,
        // WANN die Maschine läuft und wie viel sie produziert - Fahrweise_Stunde ruft
        // genau dieselben Methoden, nur gegen einen Speicherspiegel statt gegen den
        // skalaren Pendelspeicher (Paket-5-Lehre N6: die Physik steht EINMAL).
        //
        // WAS AN BILANZ VERSCHWAND: die Vektordifferenz „Restwärme = Bedarf − Produktion"
        // beim Aufrufer und die Speicherabrechnung am Stundenende. Beide waren mit einem
        // Puffer nicht mehr richtig - geladene Wärme deckt noch keinen Bedarf
        // (Konzept 6.5). Der neue Weg führt Direktdeckung, Speicherladung und Überschuss
        // getrennt und prüft ihre Summe gegen die Produktion (Energieprobe).
        // =====================================================================

        // =====================================================================
        // Gemeinsame Bausteine beider Rechenwege (Paket 6, Lehre N6 aus Paket 5:
        // die Physik steht EINMAL, nicht zweimal). Herausgelöst aus dem entfallenen
        // Berechnung(); die ausgeführten Anweisungen und ihre Reihenfolge sind
        // unverändert.
        // =====================================================================

        /// <summary>
        /// Schritt 0 des Laufs (vormals aus <c>Berechnung</c>): alle Jahressummen und
        /// Emissionszähler auf den Laufanfang.
        /// </summary>
        private void Kennzahlen_Zuruecksetzen()
        {
            BruttoBHKWErzeugung = 0f;
            Waermeproduktion_BHKW_MWh = 0f;
            Stromproduktion_BHKW_MWh = 0f;
            Gasverbrauch_BHKW = 0f;
            Oelverbrauch_BHKW = 0f;
            Gasspitze_BHKW = 0f;
            //Biogasverbrauch_BHKW = 0f;
            Rapsoelverbrauch_BHKW = 0f;
            Holzmenge_BHKW = 0f;
            Sonstigemenge_BHKW = 0f;
            //Fluessiggasverbrauch_BHKW = 0f;
            //BioErdgasverbrauch = 0f;
            //BioErdgasleistung = 0f;
            Stromverbrauch_BHKW = 0f;
            Em_CO2_BHKW = 0f;
            Em_SO2_BHKW = 0f;
            Em_NOX_BHKW = 0f;
            Em_CO_BHKW = 0f;
            Em_Staub_BHKW = 0f;
            dLaufzeiten = 0f;

            // NACHARBEIT PAKET 6, BEFUND N9: Zustandsrest des Wärmeüberschusses.
            // Die Größe wurde bisher NIRGENDS auf den Laufanfang gesetzt - nur die
            // wärmegeführte Fahrweise überschrieb sie zufällig (dort trug sie den toten
            // Solar-Überschuss). Auf einer wiederverwendeten Instanz - im Programm über
            // Form_Simulation_Detail der Normalfall - meldete deshalb ein Folgelauf den
            // Überschuss seines Vorlaufs.
            //
            // NACHWEISLICH BYTE-NEUTRAL: Das Feld ist mit 0f initialisiert, und diese
            // Methode ist in beiden Rechenwegen der erste Schritt des Laufs. Beim ERSTEN
            // Lauf einer Instanz - dem Fall jedes Referenzlaufs (ein Prozess je Projekt) -
            // setzt die Zeile 0 auf 0.
            //
            // Bewusst NICHT über einen Guard im SimulationRunner gelöst: Der hätte den
            // Wert im Altpfad still auf 0 gezwungen und damit den Überschuss der
            // stromgeführten Fahrweise aus Tab_ErgebnisBHKW entfernt - eine echte
            // Altpfad-Regression.
            Waermeueberschuss = 0f;
        }

        /// <summary>
        /// Schritt 1 des Laufs (vormals aus <c>Berechnung</c>): Grenzleistung auflösen und
        /// die Modulkennwerte aus dem Katalog lesen.
        ///
        /// ACHTUNG, Bestandsverhalten: <see cref="bhkwGrenzleistungAllgemein"/> wird
        /// hier IN PLACE durch 100 geteilt. Ein zweiter Aufruf auf derselben Instanz
        /// teilte erneut — deshalb ruft der Rechenweg diese Methode genau einmal.
        /// </summary>
        private void Moduldaten_Einlesen(int anzahl)
        {
            bhkwGrenzleistungAllgemein /= 100;

            // PAKET BHKW-REGULÄR (Entscheidung des Anwenders 17.08.2026, Punkt 2):
            // Fallback 50 % -> 30 %. Er greift, wenn das Projekt keine
            // Leistungsuntergrenze gepflegt hat (Tab_Einstellungen.Leistungsgrenze = 0).
            // Migrationsschritt 13 hebt genau diese Sätze auf 30 an, sodass Migration und
            // Fallback denselben Wert nennen; der Fallback bleibt trotzdem stehen, weil
            // eine Datenbank ohne gelaufene Migration nicht plötzlich anders rechnen soll
            // als eine mit.
            if (bhkwGrenzleistungAllgemein == 0) bhkwGrenzleistungAllgemein = 0.3f;

            BHKWCtrl ctrl = new BHKWCtrl();
            for (int i = 0; i < anzahl; i++)
            {
                ctrl.ReadSingle(bhkw_list[i]);
                bhkwWaermeLeistung[i] = (float)ctrl.m_Ptherm;
                bhkwStromLeistung[i] = (float)ctrl.m_Pel;

                // Neue Werte für die Verbrauchs- und Emissionsberechnung mappen:
                bhkwBrennstoffart[i] = ctrl.m_Brennstoff; // oder passendes Feld aus deiner BHKWCtrl
                bhkwWirkungsgrad[i] = (float)ctrl.m_Wirkungsgrad; // Gesamtwirkungsgrad (Elektrisch + Thermisch)
                bhkwSKZ[i] = bhkwStromLeistung[i] / bhkwWaermeLeistung[i];

                if ((float)ctrl.m_Grenzleistung == 0)
                    bhkwGrenzL[i] = bhkwGrenzleistungAllgemein; // Grenzleistung als Faktor (z.B. 0.8 für 80% Modulation)
                else
                    // PAKET BHKW-REGULÄR — EINHEITEN-FIX. Hier stand der Katalogwert OHNE
                    // Division: bhkwGrenzL[i] = (float)ctrl.m_Grenzleistung.
                    //
                    // DER BRUCH. bhkwGrenzL ist ein FAKTOR (0,3 = 30 % Teillast) - so
                    // wird das Feld in allen drei Motorläufen benutzt
                    // (bhkwWaermeLeistung[motor] * bhkwGrenzL[motor]). Der Anlagenwert aus
                    // Tab_Energieanlagen.Grenzleistung wird deshalb beim Laden durch 100
                    // geteilt (SimulationControl.BHKW_Liste_Laden), und die projektweite
                    // Grenze ebenso (die Zeile am Methodenanfang). Der KATALOGWERT aus
                    // Tab_BHKW.Grenzleistung wurde es nicht - er trägt aber dieselbe
                    // Einheit, nämlich Prozent.
                    //
                    // DIE FOLGE war ein Faktor 100 zu viel: Ein Katalog-BHKW mit 50 %
                    // Modulationsgrenze rechnete mit bhkwGrenzL = 50. Da der Katalogwert
                    // den Anlagenwert überschreibt, sobald er ungleich 0 ist, betraf das
                    // jedes Modul mit gepflegter Katalog-Grenzleistung. Die Bedingung
                    // „bhkwWaermeLeistung * bhkwGrenzL <= Wärmeraum" war damit praktisch
                    // nie erfüllt - der Teillastzweig fiel aus, das Modul lief nur noch
                    // Volllast oder gar nicht.
                    bhkwGrenzL[i] = (float)(ctrl.m_Grenzleistung / 100.0); // Prozent -> Faktor (50 -> 0,5)

                // Emissionsfaktoren aus der DB auslesen (Äquivalent zu Cells(zaehler+2, X))
                bhkwCO2Factor[i] = (float)ctrl.m_CO2;
                bhkwSO2Factor[i] = (float)ctrl.m_SO2;
                bhkwNOXFactor[i] = (float)ctrl.m_NOx;
                bhkwCOFactor[i] = (float)ctrl.m_CO;
                bhkwStaubFactor[i] = (float)ctrl.m_Staub;
            }
        }

        /// <summary>
        /// Schritt 2 des Laufs (vormals aus <c>Berechnung</c>): Laufzeiten,
        /// Brennstoffverbrauch und Emissionen aus den Modul-Jahressummen. Wort für Wort
        /// unverändert bis auf die Brennstoff-Kaskade (Kategorien-Zuordnung nach
        /// Tab_Brennstoff_Stamm, siehe Kommentar dort).
        /// </summary>
        private void Auswertung(int anzahl)
        {
            for (int zaehler = 0; zaehler < anzahl; zaehler++)
            {
                Waermeproduktion_BHKW_MWh += s_waerme_MWh[zaehler];
                Stromproduktion_BHKW_MWh += s_strom_MWh[zaehler];

                // Laufzeitberechnung des Moduls
                if (bhkwWaermeLeistung[zaehler] > 0)
                {
                    // s_waerme ist am Ende bereits in MWh umgerechnet worden (laut deinem Code-Ende / 1000f),
                    // für die Stundenlaufzeit multiplizieren wir wieder mit 1000, um auf kWh/kW zu kommen.
                    Laufzeiten[zaehler] = (s_waerme_MWh[zaehler] / bhkwWaermeLeistung[zaehler]) * 1000f;
                }
                else
                {
                    Laufzeiten[zaehler] = 0f;
                }
                dLaufzeiten += Laufzeiten[zaehler];

                // ETAPPE E2 — ELEKTRISCHE Vollbenutzungsstunden des Moduls. Dieselbe
                // Rechnung wie eine Zeile darueber, nur mit der ELEKTRISCHEN Nennleistung
                // im Nenner und der Stromproduktion im Zaehler. Rein additiv: Kein
                // bestehender Rechenweg liest die beiden neuen Felder, sie werden
                // ausschliesslich in die Ergebniszeilen geschrieben.
                if (bhkwStromLeistung[zaehler] > 0)
                    VbhElektrisch[zaehler] =
                        (s_strom_MWh[zaehler] / bhkwStromLeistung[zaehler]) * 1000f;
                else
                    VbhElektrisch[zaehler] = 0f;   // P_el nicht gepflegt -> keine Zahl erfinden

                // Verbrauch & Emissionen
                if (bhkwWirkungsgrad[zaehler] > 0)
                {
                    // Verbrauch berechnen (Wärme + Strom) / Wirkungsgrad
                    float ModulVerbrauch = (s_waerme_MWh[zaehler] + s_strom_MWh[zaehler]) / bhkwWirkungsgrad[zaehler];

                    BruttoBHKWErzeugung += ModulVerbrauch;

                    // Emissionen addieren und von g in kg oder Tonnen skalieren (/1000)
                    Em_CO2_BHKW += ModulVerbrauch * bhkwCO2Factor[zaehler] / 1000f;
                    Em_SO2_BHKW += ModulVerbrauch * bhkwSO2Factor[zaehler] / 1000f;
                    Em_NOX_BHKW += ModulVerbrauch * bhkwNOXFactor[zaehler] / 1000f;
                    Em_CO_BHKW += ModulVerbrauch * bhkwCOFactor[zaehler] / 1000f;
                    Em_Staub_BHKW += ModulVerbrauch * bhkwStaubFactor[zaehler] / 1000f;

                    // Brennstoffarten-Verzweigung

                    // Den Verbrauch auf die globalen Brennstoffzähler buchen. Die Bereiche
                    // spiegeln Tab_Brennstoff_Stamm.ID_Kategorie (Vorbild: SimulationSPK,
                    // Bilanz_und_Nutzungsgrad): 14 (Biogas) ist Kategorie 1 und zählt zum
                    // Gas — einschließlich der Gasspitze, wie beim reinen Biogas-Kessel.
                    // Was keinen eigenen Zähler hat (13 Strom, 23 Fernwärme, 24 Sonstige,
                    // 25 Wasserstoff, künftige IDs), fängt das else auf den Sammelposten
                    // Sonstigemenge_BHKW, den Runner und Anzeige bereits konsumieren.
                    // 13 bekommt bewusst keinen Strom-Sonderzweig wie der Kessel:
                    // Stromverbrauch_BHKW wird nirgends gelesen, ein Strom-BHKW kommt im
                    // Katalog nicht vor.
                    int art = bhkwBrennstoffart[zaehler];

                    if ((art >= 1 && art <= 5) || art == 14)
                    {
                        Gasverbrauch_BHKW += ModulVerbrauch;
                        Gasspitze_BHKW += bhkwWaermeLeistung[zaehler] * (1f + bhkwSKZ[zaehler]) / bhkwWirkungsgrad[zaehler];
                    }
                    else if ((art >= 6 && art <= 9) || (art >= 18 && art <= 22)) Oelverbrauch_BHKW += ModulVerbrauch;
                    else if (art == 10) Koks_BHKW += ModulVerbrauch;
                    else if (art == 11) Kohle_BHKW += ModulVerbrauch;
                    else if (art == 12) Holzmenge_BHKW += ModulVerbrauch;
                    else if (art == 17) TierischeFette_BHKW += ModulVerbrauch;
                    else if (art == 15) Pellets_BHKW += ModulVerbrauch;
                    else if (art == 16) Rapsoelverbrauch_BHKW += ModulVerbrauch;
                    else Sonstigemenge_BHKW += ModulVerbrauch;

                }
            }

            // gesamte Laufzeiten
            Betriebsstunden = dLaufzeiten;
            // Durchschnittliche Laufzeit berechnen
            if (anzahl > 0)
            {
                dLaufzeiten = dLaufzeiten / anzahl;
            }

            // ETAPPE E2 — leistungsgewichtete elektrische Vollbenutzungsstunden der
            // Anlage. Summe der Stromproduktion durch Summe der elektrischen
            // Nennleistungen; Module ohne gepflegtes P_el bleiben in BEIDEN Summen
            // aussen vor, sonst zoege ihre Stromproduktion (die es ohne P_el ohnehin
            // nicht gibt) den Wert nach oben.
            float summeStromMWh = 0f;
            float summePelKW = 0f;
            for (int i = 0; i < anzahl; i++)
            {
                if (bhkwStromLeistung[i] <= 0) continue;
                summeStromMWh += s_strom_MWh[i];
                summePelKW += bhkwStromLeistung[i];
            }
            VbhElektrischGesamt = (summePelKW > 0) ? (summeStromMWh / summePelKW) * 1000f : 0f;
        }


        // PAKET BHKW-REGULÄR: Hier stand BhkwSimulationWaermegefuehrt - die
        // JAHRESSCHLEIFE der wärmegeführten Fahrweise im einkanaligen Altpfad. Sie ist
        // mit dem Altpfad entfallen; der Stundenschritt selbst
        // (Motorlauf_Waermegefuehrt) bleibt und wird von Fahrweise_Stunde gerufen.
        //
        // Sie trug außer der Schleife nur noch VBA-Erbschaft: einen Solar-Vorlauf, der
        // wegen solarVorhanden = false und SolareErzeugung() == 0 nie etwas tat, und die
        // Speicherabrechnung am Stundenende, die der neue Weg durch die Phasen A und E
        // der Kaskadenschleife ersetzt.

        /// <summary>
        /// Der STUNDENSCHRITT der wärmegeführten Fahrweise — die Motorzuschaltung gegen
        /// den Wärmeraum aus Momentanbedarf und Speicherraum.
        ///
        /// <para><b>PAKET BHKW-REGULÄR — TOTE ZWEIGE ENTFERNT.</b> Die Methode war in eine
        /// Betriebsartenweiche gehüllt, deren erste Bedingung <c>if (stunde &lt; 8760)</c>
        /// für JEDE Stunde des Jahres wahr ist (die Jahresschleife läuft 0..8759). Damit
        /// waren unerreichbar:
        /// <list type="bullet">
        ///   <item>der SOMMERBETRIEB (<c>else if (stdTag > 10 &amp;&amp; stdTag &lt; 22)</c>)
        ///         mit seinen vier stromseitigen Zuschaltfällen,</item>
        ///   <item>die darin liegende 30-%-Schwelle und die 10-%-NOTSCHALTUNG,</item>
        ///   <item>die 20-%-NOTSCHALTUNG als letzter <c>else if</c>-Zweig.</item>
        /// </list>
        /// Ihr Entfernen ist ERGEBNISNEUTRAL — nachweisbar durch Lesen, nicht erst durch
        /// Messen: Kein Aufruf konnte sie erreichen. Die auskommentierte Altbedingung
        /// <c>//if (stunde &lt; 3600 || stunde > 5760)</c> zeigt, dass die Weiche einmal
        /// gemeint war und irgendwann stillgelegt wurde; sie zurückzuholen wäre eine
        /// fachliche Änderung und gehört nicht in dieses Paket (dokumentiert in
        /// Paket6_BHKW_Protokoll.md:74-94, Entscheidung 6-2 vom Anwender revidiert).
        ///
        /// Mit den Zweigen entfielen ihre Parameter: <c>stdTag</c> und <c>strombedarf</c>
        /// (nur im Sommerbetrieb gelesen), <c>bhkwGrenzleistung</c> (nur in der
        /// 10-%-Notschaltung) und <c>solarSpeicher</c> — letzterer trug seit dem Rückbau
        /// des Altpfads ohnehin konstant <c>0f</c>, sodass die Terme
        /// <c>kapazitaetPendelspeicher - solarSpeicher - speicher</c> bitgleich zu
        /// <c>kapazitaetPendelspeicher - speicher</c> sind.</para>
        ///
        /// <para><b>Speicher als SPIEGEL:</b> <paramref name="speicher"/> ist der
        /// Füllstand, <paramref name="kapazitaetPendelspeicher"/> der Zielfüllstand aus
        /// Ladefähigkeit bzw. Bilanzraum des zugeordneten
        /// <see cref="SimulationPufferspeicher"/> (Konzept 3.4). Der Zuwachs von
        /// <paramref name="speicher"/> ist die zu ladende Menge, der Rückgang von
        /// <paramref name="restWaerme"/> die Direktdeckung. Aufrufer ist
        /// <see cref="Fahrweise_Stunde"/>.</para>
        /// </summary>
        private void Motorlauf_Waermegefuehrt(
            int stunde, int anzahl,
            float[] stromproduktion, float[] waermeproduktion,
            float[] s_waerme, float[] s_strom,
            float[] bhkwWaermeLeistung, float[] bhkwStromLeistung, float[] bhkwGrenzL,
            float kapazitaetPendelspeicher,
            ref float speicher, ref float restWaerme)
        {
            for (int motor = 0; motor < anzahl; motor++)
            {
                // Freier Speicherraum VOR diesem Motor. Er wird je Motor neu gebildet,
                // weil der vorige den Füllstand verändert haben kann. (Die frühere
                // Fassung hatte dieselbe Zuweisung zusätzlich vor der Schleife; sie wurde
                // dort nie gelesen und ist mit der Weichenhülle entfallen.)
                float restSpeicher = kapazitaetPendelspeicher - speicher;

                if (bhkwWaermeLeistung[motor] < restWaerme + restSpeicher)
                {
                    waermeproduktion[stunde] += bhkwWaermeLeistung[motor];
                    s_waerme[motor] += bhkwWaermeLeistung[motor];
                    stromproduktion[stunde] += bhkwStromLeistung[motor];
                    s_strom[motor] += bhkwStromLeistung[motor];
                    restWaerme -= bhkwWaermeLeistung[motor];

                    if (restWaerme < 0)
                    {
                        speicher -= restWaerme;
                        restWaerme = 0f;
                    }
                }
                else if (bhkwWaermeLeistung[motor] * bhkwGrenzL[motor] <= restWaerme + restSpeicher)
                {
                    waermeproduktion[stunde] += restWaerme + restSpeicher;
                    s_waerme[motor] += restWaerme + restSpeicher;
                    stromproduktion[stunde] += (restWaerme + restSpeicher) / bhkwWaermeLeistung[motor] * bhkwStromLeistung[motor];
                    s_strom[motor] += (restWaerme + restSpeicher) / bhkwWaermeLeistung[motor] * bhkwStromLeistung[motor];
                    speicher = kapazitaetPendelspeicher;
                    restWaerme = 0f;
                }
            }
        }

        // PAKET BHKW-REGULÄR: Hier standen zwei Altpfad-Bausteine.
        //
        //   Speicherabrechnung(ref speicher, ref restWaerme) - die Endabrechnung der
        //   Stunde, in der der skalare Pendelspeicher deckte, was offen geblieben war.
        //   Der neue Weg entlädt die Speicher in den Phasen A und E der
        //   Kaskadenschleife (Konzept 6.3), mit Herkunftsrechnung und Zurechnung der
        //   Entladung auf die Erzeuger - beides konnte diese Methode nicht leisten.
        //
        //   SimulationStromgefuehrt(...) - die JAHRESSCHLEIFE der stromgeführten
        //   Fahrweise. Der Stundenschritt Motorlauf_Stromgefuehrt bleibt und wird von
        //   Fahrweise_Stunde gerufen; die Schleife samt Speicherüberlauf-Buchung ist
        //   entfallen (der Überschuss wird jetzt je Stunde in Ueberschuss_stuendlich
        //   gebucht und über die Energieprobe geprüft).

        /// <summary>
        /// Der STUNDENSCHRITT der stromgeführten Fahrweise: Die Motoren folgen dem
        /// Strombedarf, die Wärme ist Koppelprodukt (Paket 6, herausgelöst aus der mit dem
        /// Altpfad entfallenen <c>SimulationStromgefuehrt</c> — Anweisungen unverändert).
        ///
        /// Der Speicher kommt hier bewusst NICHT vor: Eine stromgeführte Maschine richtet
        /// ihre Leistung nach dem Strombedarf, nicht nach dem Füllstand. Was an Wärme
        /// übrig bleibt, entscheidet erst der Aufrufer — <see cref="Fahrweise_Stunde"/>
        /// gibt es an die Ladephase C/D weiter (Konzept 6.3).
        /// </summary>
        private void Motorlauf_Stromgefuehrt(
            int stunde, int anzahl,
            float[] stromproduktion, float[] waermeproduktion,
            float[] s_waerme, float[] s_strom,
            float[] bhkwWaermeLeistung, float[] bhkwStromLeistung, float bhkwGrenzleistung,
            ref float restStrom, ref float restWaerme)
        {
            for (int motor = 0; motor < anzahl; motor++)
            {
                // Fall 1: Reststrombedarf ist größer als die Volllast-Leistung des aktuellen Motors
                if (bhkwStromLeistung[motor] < restStrom)
                {
                    stromproduktion[stunde] += bhkwStromLeistung[motor];
                    waermeproduktion[stunde] += bhkwWaermeLeistung[motor];

                    s_strom[motor] += bhkwStromLeistung[motor];
                    s_waerme[motor] += bhkwWaermeLeistung[motor];

                    restStrom -= bhkwStromLeistung[motor];
                    restWaerme -= bhkwWaermeLeistung[motor];
                }
                // Fall 2: Der Motor kann modulieren (Teillastbetrieb), um den Reststrom exakt zu decken
                else if (bhkwStromLeistung[motor] * bhkwGrenzleistung <= restStrom)
                {
                    stromproduktion[stunde] += restStrom;

                    // Anteilige Wärmeproduktion berechnen (Dreisatz über den elektrischen Wirkungsgrad)
                    float anteiligeWaerme = restStrom / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];
                    waermeproduktion[stunde] += anteiligeWaerme;

                    s_strom[motor] += restStrom;
                    s_waerme[motor] += anteiligeWaerme;

                    restWaerme -= anteiligeWaerme;
                    restStrom = 0f;
                }
            }
        }

        // PAKET BHKW-REGULÄR: Hier stand SimulationOhneEinspeisung(...) - die
        // JAHRESSCHLEIFE der Fahrweise ohne Einspeisung im einkanaligen Altpfad. Wie bei
        // den beiden anderen Fahrweisen bleibt der Stundenschritt
        // (Motorlauf_OhneEinspeisung) und wird von Fahrweise_Stunde gerufen; die Schleife
        // samt Speicherabrechnung am Stundenende ist entfallen.

        /// <summary>
        /// Der STUNDENSCHRITT der Fahrweise OHNE EINSPEISUNG: erst die wärmeseitige
        /// Zuschaltung (W1/W2), danach die stromseitige (S1…S3) — beide Schleifen
        /// unverändert aus der mit dem Altpfad entfallenen
        /// <c>SimulationOhneEinspeisung</c> herausgelöst (Paket 6); nur die erste
        /// Zuweisung an <c>restSpeicher</c> und die beiden Hilfsgrößen
        /// <c>wLeistung</c>/<c>sLeistung</c> sind hierher gewandert.
        ///
        /// Wie bei der wärmegeführten Fahrweise sind <paramref name="speicher"/> und
        /// <paramref name="kapazitaetPendelspeicher"/> der Spiegel eines
        /// <see cref="SimulationPufferspeicher"/>.
        /// </summary>
        private void Motorlauf_OhneEinspeisung(
            int stunde, int anzahl,
            float[] stromproduktion, float[] waermeproduktion,
            float[] s_waerme, float[] s_strom,
            float[] bhkwWaermeLeistung, float[] bhkwStromLeistung, float bhkwGrenzleistung,
            float kapazitaetPendelspeicher,
            ref float speicher, ref float restWaerme, ref float restStrom)
        {
            float restSpeicher = kapazitaetPendelspeicher - speicher;
            float wLeistung = 0f;
            float sLeistung = 0f;

            {
                for (int motor = 0; motor < anzahl; motor++)
                {
                    restSpeicher = kapazitaetPendelspeicher - speicher;

                    // Fall W1: Benötigte Wärme passt in Hausbedarf + freien Puffer
                    if (bhkwWaermeLeistung[motor] < restWaerme + restSpeicher)
                    {
                        if (bhkwStromLeistung[motor] < restStrom)
                        {
                            sLeistung = bhkwStromLeistung[motor];
                            wLeistung = bhkwWaermeLeistung[motor];
                        }
                        else if (bhkwStromLeistung[motor] * bhkwGrenzleistung < restStrom)
                        {
                            sLeistung = restStrom;
                            wLeistung = restStrom / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];
                        }
                        else
                        {
                            // Strombedarf zu gering -> Motor darf nicht einspeisen und bleibt aus/regelt ab
                            sLeistung = 0f;
                            wLeistung = 0f;
                        }

                        waermeproduktion[stunde] += wLeistung;
                        s_waerme[motor] += wLeistung;
                        stromproduktion[stunde] += sLeistung;
                        s_strom[motor] += sLeistung;

                        restStrom -= sLeistung;
                        restWaerme -= wLeistung;

                        if (restWaerme < 0)
                        {
                            speicher -= restWaerme; // restWaerme ist negativ, erhöht den Speicher
                            restWaerme = 0f;
                        }
                    }
                    // Fall W2: Modulierter Betrieb bis zur Füllung des Speichers
                    else if (bhkwWaermeLeistung[motor] * bhkwGrenzleistung <= restWaerme + restSpeicher)
                    {
                        sLeistung = (restWaerme + restSpeicher) / bhkwWaermeLeistung[motor] * bhkwStromLeistung[motor];

                        if (sLeistung < restStrom)
                        {
                            wLeistung = restWaerme + restSpeicher;
                        }
                        else if (bhkwStromLeistung[motor] * bhkwGrenzleistung < restStrom)
                        {
                            sLeistung = restStrom;
                            wLeistung = restStrom / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];
                        }
                        else
                        {
                            sLeistung = 0f;
                            wLeistung = 0f;
                        }

                        waermeproduktion[stunde] += wLeistung;
                        s_waerme[motor] += wLeistung;
                        stromproduktion[stunde] += sLeistung;
                        s_strom[motor] += sLeistung;

                        restStrom -= sLeistung;
                        restWaerme -= wLeistung;

                        // Überschüssige Wärme in den Speicher (restWaerme negativ -> erhöht den Speicher).
                        // Im Volllastzweig füllt das den Speicher exakt bis kapazitaetPendelspeicher.
                        if (restWaerme < 0)
                        {
                            speicher -= restWaerme;
                            restWaerme = 0f;
                        }
                    }
                }
                for (int motor = 0; motor < anzahl; motor++)
                {
                    restSpeicher = kapazitaetPendelspeicher - speicher;

                    // Fall S1: Volllast auf den Strombedarf gedeckelt
                    if (bhkwStromLeistung[motor] < restStrom && (restSpeicher + restWaerme > bhkwWaermeLeistung[motor]))
                    {
                        stromproduktion[stunde] += bhkwStromLeistung[motor];
                        s_strom[motor] += bhkwStromLeistung[motor];
                        waermeproduktion[stunde] += bhkwWaermeLeistung[motor];
                        s_waerme[motor] += bhkwWaermeLeistung[motor];

                        restWaerme -= bhkwWaermeLeistung[motor];
                        restStrom -= bhkwStromLeistung[motor];

                        if (restWaerme < 0)
                        {
                            speicher -= restWaerme;
                            restWaerme = 0f;
                        }
                    }
                    // Fall S2: Teillastbetrieb exakt auf den Reststrombedarf geregelt
                    else if (bhkwStromLeistung[motor] * bhkwGrenzleistung <= restStrom &&
                             (restSpeicher + restWaerme > restStrom / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor]))
                    {
                        stromproduktion[stunde] += restStrom;
                        s_strom[motor] += restStrom;

                        float anteiligeWaerme = restStrom / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];
                        waermeproduktion[stunde] += anteiligeWaerme;
                        s_waerme[motor] += anteiligeWaerme;

                        restWaerme -= anteiligeWaerme;
                        restStrom = 0f;

                        if (restWaerme < 0)
                        {
                            speicher -= restWaerme;
                            restWaerme = 0f;
                        }
                    }
                    // Fall S3: Teillastbetrieb bis zur thermischen Speichergrenze
                    else if (bhkwStromLeistung[motor] * bhkwGrenzleistung <= restStrom &&
                             (restSpeicher + restWaerme > bhkwWaermeLeistung[motor] * bhkwGrenzleistung))
                    {
                        waermeproduktion[stunde] += (restSpeicher + restWaerme);
                        s_waerme[motor] += (restSpeicher + restWaerme);

                        float berechneterStrom = (restSpeicher + restWaerme) / bhkwWaermeLeistung[motor] * bhkwStromLeistung[motor];
                        stromproduktion[stunde] += berechneterStrom;
                        s_strom[motor] += berechneterStrom;

                        restWaerme = 0f;
                        restStrom -= berechneterStrom;
                        speicher = kapazitaetPendelspeicher;
                    }
                }
            }
        }



        // PAKET BHKW-REGULÄR: Hier stand SolareErzeugung(int stunde) - eine
        // Platzhalter-Methode aus der VBA-Übersetzung, die immer 0f zurückgab. Ihr
        // einziger Aufrufer war der Solar-Vorlauf der entfallenen wärmegeführten
        // Jahresschleife; sie ist mit ihm entfallen.

        // =====================================================================
        // ZWEIKANALIGER WEG (Paket 6 - Konzept 6.3 und 6.5, zweiter Punkt)
        //
        // Das BHKW verlässt den Kompatibilitätsanker Waermekanaele.Uebernehmen und
        // wird Mitglied der gemeinsamen Stundenschleife (Kaskadenschleife). Die drei
        // Fahrweisen bleiben fachlich unangetastet - sie bestimmen weiterhin, WANN
        // die Maschine läuft und wie viel sie produziert. Neu ist ausschließlich,
        // WOHIN die Wärme geht:
        //
        //   Hauptsenke HEIZKREIS -> Phase B deckt den Momentanbedarf, der Rest der
        //                           Stunde geht in Phase D an eine (Ersatz-)Senke.
        //   Hauptsenke PUFFER    -> Phase B deckt NICHTS (Doppelzählungs-Freibeweis,
        //                           Konzept 6.3); die Maschine läuft erst in Phase C
        //                           gegen den Bilanzraum des Speichers.
        //
        // Der skalare kapazitaetPendelspeicher ist damit abgelöst: Wo der Altpfad mit
        // ihm rechnet, arbeitet der neue Weg mit dem SPIEGEL eines
        // SimulationPufferspeicher - dieselbe Motorlogik, eine Speicherphysik.
        // =====================================================================

        /// <summary>Höchste Zahl von BHKW-Modulen, die die festen Felder tragen.</summary>
        public const int MAX_BHKW = 10;

        /// <summary>In Pufferspeicher geladene BHKW-Wärme je Stunde [kWh]; im Altpfad 0.</summary>
        public double[] Speicherladung_stuendlich = new double[8760];

        /// <summary>Jahressumme der Speicherladung [kWh]; im Altpfad exakt 0.</summary>
        public double Speicherladung_gesamt = 0;

        /// <summary>
        /// Verworfene Wärme je Stunde [kWh] (zweikanaliger Weg): produziert, aber weder
        /// gedeckt noch gespeichert. Die Jahressumme steht in
        /// <see cref="Waermeueberschuss"/>; die Ganglinie macht die Energieerhaltung
        /// prüfbar (Produktion = Direktdeckung + Speicherladung + Überschuss).
        /// </summary>
        public double[] Ueberschuss_stuendlich = new double[8760];

        /// <summary>
        /// Wärme, die das BHKW in Phase B unmittelbar an den Bedarf abgegeben hat [kWh]
        /// (Nacharbeit-N1-Muster). Zusammen mit
        /// <see cref="Speicherentladung_Anteil"/> ist das der EIGENANTEIL des BHKW an
        /// der Bedarfsdeckung — die Größe, die
        /// <c>Tab_ErgebnisBHKW.Waermebedarfsdeckung</c> künftig ausweist, statt der
        /// bloßen Produktion (offener Punkt 4 der Paket-5-Nacharbeit).
        /// </summary>
        public double Direktdeckung_gesamt = 0;

        /// <summary>
        /// Anteil des BHKW an der bedarfsdeckenden Speicherentladung [kWh], zugerechnet
        /// von der <see cref="Kaskadenschleife"/> nach der Interimsregel „Vermischung im
        /// Speicher" (Paket-5-Nacharbeit, Befund N2). Im Altpfad 0.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        /// <summary>
        /// Jahressumme des Stufeneingangs [kWh] (zweikanaliger Weg) — dieselbe Größe wie
        /// <c>waermebedarf</c>, nur in <c>double</c> summiert.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N1: Der Stufeneingang wird jetzt in
        /// <see cref="Stunde_Start"/> festgehalten, also VOR der Vorabentladung (Phase A) —
        /// dieselbe Bezugsgröße, die der Altpfad an der Kaskadenposition des BHKW sieht,
        /// und dieselbe, die die Wärmepumpe seit Etappe 4b führt
        /// (<c>Zweikanalig_Start</c> nimmt die Kanäle vor der Schleife). Vorher stand
        /// hier der Eingang NACH Phase A; <c>Tab_ErgebnisBHKW.Waermebedarf</c> fiel damit
        /// gegenüber dem Altpfad um bis zu 56 % ab (gemessen an 1017 mit Pendelspeicher:
        /// 62,91 -> 27,80 MWh).
        /// </summary>
        public double Waermebedarf_gesamt = 0;

        /// <summary>
        /// Fehlertext des zweikanaligen Wegs (Konzept 13.4: die Engine bleibt dialogfrei).
        /// </summary>
        public string Fehlertext = "";

        /// <summary>Ladeauftrag der HAUPTsenke; <c>null</c>, wenn die Hauptsenke der Heizkreis ist.</summary>
        public Ladeauftrag Auftrag_Haupt = null;

        /// <summary>Ladeauftrag der ZWEITsenke bzw. des Ersatz-Pendelspeichers; <c>null</c> = keiner.</summary>
        public Ladeauftrag Auftrag_Zweit = null;

        /// <summary>
        /// <c>true</c>, wenn das BHKW die LETZTE Stufe der Bedarfsreihenfolge (Phase B)
        /// ist — Voraussetzung für den Durchsatzterm des Bilanzraums (Befund N5).
        ///
        /// Die Kaskadenschleife setzt das Feld je Lauf; in der Vektorstufe gilt es
        /// ohnehin (dort gibt es keine weiteren Mitglieder). Siehe
        /// <see cref="ZweitsenkenRaum"/>.
        /// </summary>
        public bool LetzteBedarfsstufe = true;

        private int _anzahlZweikanalig = 0;
        private readonly List<Senkenzuordnung> _bhkwSenke = new List<Senkenzuordnung>();
        private Senkenzuordnung _stufensenke = new Senkenzuordnung();
        private int _fuehrendeAnlage = 0;

        /// <summary>In dieser Stunde produzierte, noch nicht untergebrachte Wärme [kWh].</summary>
        private double _ueberschussStunde = 0;

        /// <summary>In dieser Stunde unmittelbar an den Bedarf abgegebene Wärme [kWh] (Phase B).</summary>
        private double _direktStunde = 0;

        /// <summary>Speicher, an dem diese Stunde Ladefähigkeit reserviert wurde (Befund N3).</summary>
        private SimulationPufferspeicher _reservierterSpeicher = null;

        /// <summary>Anzahl der BHKW-Module, die im zweikanaligen Weg rechnen.</summary>
        public int ModulAnzahl { get { return _anzahlZweikanalig; } }

        /// <summary>Anlage, deren Senke für die GANZE BHKW-Stufe gilt (siehe <see cref="Vorbereiten_Zweikanalig"/>).</summary>
        public int FuehrendeAnlage { get { return _fuehrendeAnlage; } }

        /// <summary>Wirksame Senke der BHKW-Stufe.</summary>
        public Senkenzuordnung Stufensenke { get { return _stufensenke; } }

        /// <summary>Senkenzuordnung eines Moduls; <c>null</c> außerhalb des Indexbereichs.</summary>
        public Senkenzuordnung BhkwSenke(int index)
        {
            if (index < 0 || index >= _bhkwSenke.Count) return null;
            return _bhkwSenke[index];
        }

        /// <summary>
        /// Setzt den Modulzustand auf den Laufanfang. Aufgerufen NUR aus dem
        /// zweikanaligen Weg — der Altpfad hat nie eine <c>Init()</c> gehabt und bleibt
        /// deshalb unberührt (der Bestandsbefund „<c>s_waerme_MWh</c> wird in der
        /// stromgeführten Fahrweise nicht genullt" ist im Protokoll vermerkt).
        /// </summary>
        public void Init()
        {
            _anzahlZweikanalig = 0;
            _bhkwSenke.Clear();
            _stufensenke = new Senkenzuordnung();
            _fuehrendeAnlage = 0;
            _ueberschussStunde = 0;
            _direktStunde = 0;
            _reservierterSpeicher = null;
            Auftrag_Haupt = null;
            Auftrag_Zweit = null;
            LetzteBedarfsstufe = true;      // Vektorstufe: es gibt keine weitere Stufe
            Fehlertext = "";

            Array.Clear(Speicherladung_stuendlich, 0, Speicherladung_stuendlich.Length);
            Array.Clear(Ueberschuss_stuendlich, 0, Ueberschuss_stuendlich.Length);
            Speicherladung_gesamt = 0;
            Direktdeckung_gesamt = 0;
            Speicherentladung_Anteil = 0;
            Waermebedarf_gesamt = 0;
            Waermeueberschuss = 0f;

            Array.Clear(waermeproduktion, 0, waermeproduktion.Length);
            Array.Clear(stromproduktion, 0, stromproduktion.Length);
            Array.Clear(waermerestbedarf, 0, waermerestbedarf.Length);

            // EIGENER Vektor für den Stufeneingang. Im Altpfad zeigt waermebedarf auf das
            // Ausgangsarray der Vorstufe (Aliasing wie B0-2); im zweikanaligen Weg
            // SCHREIBT das Modul hier stündlich hinein - auf einem geerbten Alias würde
            // das den Bedarfsvektor des Projekts überschreiben.
            waermebedarf = new float[8760];

            VbhElektrischGesamt = 0f;

            for (int i = 0; i < MAX_BHKW; i++)
            {
                s_waerme_MWh[i] = 0f;
                s_strom_MWh[i] = 0f;
                s_waerme_ueberschuss[i] = 0f;
                Laufzeiten[i] = 0f;
                VbhElektrisch[i] = 0f;
            }
        }

        /// <summary>
        /// Modulaufbau des Laufs — dieselben Schritte, die das entfallene
        /// <c>Berechnung</c> des Altpfads voranstellte (Kennzahlen zurücksetzen,
        /// Moduldaten einlesen), zusätzlich die Senkenzuordnung.
        ///
        /// EINE SENKE JE STUFE: Die drei Fahrweisen schalten ihre Motoren GEMEINSAM
        /// gegen einen Wärmeraum zu (<c>restWaerme + restSpeicher</c>). Eine Senke je
        /// Motor würde diese Zuschaltlogik auseinanderreißen — also neue Physik, und
        /// genau das ist ausgeschlossen. Maßgeblich ist deshalb die Senke der ERSTEN
        /// Anlage mit Puffer-Hauptsenke, sonst die der ersten Anlage überhaupt;
        /// abweichende Zuordnungen weiterer Anlagen werden protokolliert.
        /// </summary>
        /// <returns>false = Abbruch.</returns>
        public bool Vorbereiten_Zweikanalig(int ID_Projekt, List<Senkenzuordnung> senken)
        {
            m_ID_Projekt = ID_Projekt;

            Init();

            int anzahl = bhkw_list.Count;
            if (anzahl > MAX_BHKW)
            {
                // NACHARBEIT PAKET 6, BEFUND N8: ABBRUCH über den Fehlerkanal statt einer
                // stillen Kürzung. Konzept 13.4 verlangt eine dialogfreie Engine, nicht
                // eine schweigende - SimulationControl reicht den Text an
                // SimulationRunner weiter, und der Lauf speichert kein Ergebnis.
                //
                // ABWEICHUNG VOM HEIZKESSEL, mit Absicht: Der Kessel kürzt und rechnet
                // weiter, weil sein Altpfad genau das tut (MessageBox + erste MAX_SPK) -
                // das VERHALTEN bleibt dort dasselbe. Das BHKW hat diese Vorlage nicht:
                // Sein Altpfad läuft ab dem 11. Modul in eine IndexOutOfRangeException
                // (Bestandsbefund B-3). Es gibt also kein Verhalten zu erhalten, und ein
                // stillschweigend um Module gekürztes Ergebnis sähe plausibel aus, wäre
                // aber falsch - sichtbar falsch ist besser als still falsch.
                Fehlertext = string.Format(MyResource.Resource.SIMENG_BHKW_MAX_UEBERSCHRITTEN,
                                           anzahl, MAX_BHKW);
                // NACHARBEIT PAKET 8, BEFUND N9: über den Fehlerkanal statt über eine
                // blanke Konsolenzeile. Der Kanal schreibt weiterhin auf die Konsole
                // (mit Präfix "Simulation FEHLER:"), trägt die Meldung aber zusätzlich
                // ins Lauf-Protokoll und in die Sammelanzeige der Oberfläche.
                SimulationProtokoll.Aktuell.Fehlermeldung(Fehlertext);
                return false;
            }

            anzahlBhkw = anzahl;

            Kennzahlen_Zuruecksetzen();
            Moduldaten_Einlesen(anzahl);

            for (int i = 0; i < anzahl; i++)
            {
                int idAnlage = (i < bhkw_anlagen_ids.Count) ? bhkw_anlagen_ids[i] : 0;
                _bhkwSenke.Add(SenkeZuAnlage(senken, idAnlage));
            }

            _stufensenke = new Senkenzuordnung();
            _fuehrendeAnlage = 0;
            for (int i = 0; i < _bhkwSenke.Count; i++)
            {
                if (_bhkwSenke[i] == null) continue;
                if (_fuehrendeAnlage == 0 || (_stufensenke.Haupt == Senke.Heizkreis &&
                                              _bhkwSenke[i].Haupt != Senke.Heizkreis))
                {
                    _stufensenke = _bhkwSenke[i];
                    _fuehrendeAnlage = _bhkwSenke[i].AnlagenID;
                }
            }

            for (int i = 0; i < _bhkwSenke.Count; i++)
            {
                if (_bhkwSenke[i] == null || _bhkwSenke[i] == _stufensenke) continue;
                if (_bhkwSenke[i].Haupt == _stufensenke.Haupt &&
                    _bhkwSenke[i].IDPufferHaupt == _stufensenke.IDPufferHaupt &&
                    _bhkwSenke[i].IDPufferZweit == _stufensenke.IDPufferZweit) continue;

                // Protokollkanal-Nachzug: WARNUNG - die gepflegte Senke dieser Anlage
                // bleibt in diesem Lauf UNWIRKSAM (Ersatzannahme: Senke der führenden
                // Anlage). Je Anlage einmal.
                SimulationProtokoll.Aktuell.WarnungEinmal(
                                  "bhkw-abweichende-senke-" + _bhkwSenke[i].AnlagenID,
                                  "BHKW: Die Anlage " + _bhkwSenke[i].AnlagenID + " hat eine andere " +
                                  "Wärmesenke als die führende Anlage " + _fuehrendeAnlage +
                                  ". Die Fahrweisen schalten alle Module gemeinsam zu; für die " +
                                  "gesamte BHKW-Stufe gilt deshalb die Senke der führenden Anlage.");
            }

            _anzahlZweikanalig = anzahl;
            return true;
        }

        /// <summary>Senkenzuordnung einer Anlage; ohne Zeile gilt Heizkreis/Beides (Konzept 4.6).</summary>
        private static Senkenzuordnung SenkeZuAnlage(List<Senkenzuordnung> senken, int idAnlage)
        {
            if (senken != null && idAnlage > 0)
                foreach (Senkenzuordnung z in senken)
                    if (z != null && z.AnlagenID == idAnlage) return z;

            return new Senkenzuordnung { AnlagenID = idAnlage };
        }

        /// <summary>
        /// Stundenbeginn: Ganglinien der Stunde nullen, Zähler leeren und den
        /// STUFENEINGANG festhalten.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N1: Der Stufeneingang ist der Kanalstand VOR der
        /// Vorabentladung (Phase A) — die Größe, die der Altpfad an der Kaskadenposition
        /// des BHKW sieht, und dieselbe Bezugsgröße, die die Wärmepumpe seit Etappe 4b
        /// führt. Bis zur Nacharbeit stand er in <see cref="Stunde_Bedarf"/> und damit
        /// NACH Phase A; <c>Tab_ErgebnisBHKW.Waermebedarf</c> war dadurch systematisch zu
        /// klein, ohne dass es irgendwo dokumentiert gewesen wäre.
        ///
        /// In der VEKTORSTUFE (<see cref="Berechnung_Zweikanalig"/>) gibt es keine
        /// Phase A — dort liefert der Aufruf denselben Wert wie bisher.
        /// </summary>
        public void Stunde_Start(int stunde, double rest_heiz, double rest_ww)
        {
            if (stunde >= 0 && stunde < 8760)
            {
                stromproduktion[stunde] = 0f;
                waermeproduktion[stunde] = 0f;
            }
            _ueberschussStunde = 0;
            _direktStunde = 0;
            _reservierterSpeicher = null;

            double eingang = rest_heiz + rest_ww;
            if (eingang < 0) eingang = 0;
            if (stunde >= 0 && stunde < 8760) waermebedarf[stunde] = (float)eingang;
            Waermebedarf_gesamt += eingang;
        }

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für das BHKW.
        ///
        /// Mit Hauptsenke HEIZKREIS läuft hier die Fahrweise: Sie bekommt als Wärmeraum
        /// den offenen Kanalbedarf (nach <c>WS_Typ</c>) PLUS die Ladefähigkeit der
        /// Zweitsenke — genau die Rolle, die im Altpfad der Pendelspeicher hatte. Was
        /// die Maschine über den Bedarf hinaus erzeugt, bleibt bis zur Ladephase D in
        /// <see cref="_ueberschussStunde"/> stehen.
        ///
        /// Mit Puffer-Hauptsenke deckt das BHKW hier NICHTS (Konzept 6.3,
        /// Doppelzählungs-Freibeweis) — es läuft erst in Phase C.
        /// </summary>
        public void Stunde_Bedarf(int stunde, bool pvUeberschuss,
                                  ref double rest_heiz, ref double rest_ww)
        {
            if (_stufensenke.Haupt != Senke.Heizkreis)
            {
                // Reine Ladeanlage: die Produktion entscheidet sich in Phase C.
                return;
            }

            string wsTyp = _stufensenke.WSTyp;
            double verfuegbar;
            if (wsTyp == WaermequelleClass.SENKE_WARMWASSER) verfuegbar = rest_ww;
            else if (wsTyp == WaermequelleClass.SENKE_HEIZUNG) verfuegbar = rest_heiz;
            else verfuegbar = rest_heiz + rest_ww;
            if (verfuegbar < 0) verfuegbar = 0;

            // Wärmeraum der (Ersatz-)Zweitsenke = der Speicherraum der Stunde. OHNE
            // Zweitsenke ist er 0, und die Fahrweise rechnet wie ein Pendelspeicher mit
            // Volumen 0 - also genau wie im Altpfad ohne Pendelspeicher.
            double raum = ZweitsenkenRaum(pvUeberschuss, wsTyp, rest_heiz, rest_ww);

            double gedeckt, geladen;
            Fahrweise_Stunde(stunde, verfuegbar, raum, out gedeckt, out geladen);

            if (gedeckt > 0)
            {
                Kaskadenschleife.SenkeAbziehen(wsTyp, gedeckt, ref rest_ww, ref rest_heiz);
                Direktdeckung_gesamt += gedeckt;
                _direktStunde += gedeckt;
            }

            _ueberschussStunde += geladen;

            // NACHARBEIT PAKET 6, BEFUND N3: Die Motoren laufen JETZT, eingelagert wird
            // erst in Phase D. Zwischen beidem stehen die Ladeaufträge der Erzeuger mit
            // besserer Ladepriorität (Solarthermie 10, Wärmepumpe 20 gegen BHKW 30) - sie
            // könnten den Raum aufbrauchen, gegen den das BHKW gerade zugeschaltet hat.
            // Die erzeugte Wärme wäre dann verworfen, der Brennstoff aber verbraucht.
            //
            // Reserviert wird GENAU die Menge, die eingelagert werden soll - nicht der
            // ganze Wärmeraum. Damit bleibt für die anderen Lader alles frei, was das
            // BHKW nicht beansprucht, und der Bilanzraum ist unberührt: Es wird nichts
            // zusätzlich geladen, nur die Reihenfolge der Vergabe festgehalten.
            if (geladen > 0 && Auftrag_Zweit != null && Auftrag_Zweit.Speicher != null)
            {
                _reservierterSpeicher = Auftrag_Zweit.Speicher;
                _reservierterSpeicher.Reservieren(geladen);
            }
        }

        /// <summary>
        /// Wärmeraum der Zweitsenke in Phase B — Ladefähigkeit PLUS Durchsatz
        /// (Bilanzraum, Nutzerentscheidung zu Befund 4b-1).
        ///
        /// NACHARBEIT PAKET 6, BEFUND N5: Bis dahin ging hier nur die Ladefähigkeit ein.
        /// Ein voller Puffer hielt das BHKW damit an, obwohl er als hydraulische Weiche
        /// hätte durchreichen können — der Heizkessel hat diesen Summanden in Phase D
        /// längst (<c>SimulationSPK.Zweikanalig_Laden</c>).
        ///
        /// ZWEI EINSCHRÄNKUNGEN, beide aus dem Grundsatz „es entsteht keine Wärme, die
        /// niemand angefordert hat":
        ///
        ///   1. KANALÜBERSCHNEIDUNG. Deckt das BHKW mit seinem <c>WS_Typ</c> denselben
        ///      Kanal, den der Speicher bedient, ist dessen offener Bedarf bereits als
        ///      DIREKTdeckung verplant und darf nicht ein zweites Mal als Durchsatz
        ///      angesetzt werden.
        ///   2. NUR ALS LETZTE STUFE der Bedarfsreihenfolge. Der Durchsatz der Ladephase
        ///      bemisst sich am Budget <c>absehbar</c>, und das steht erst NACH der
        ///      GESAMTEN Phase B fest. Nur wenn das BHKW dort zuletzt kommt, ist der
        ///      Kanalstand, den es sieht, genau dieses Budget. Steht es davor, decken die
        ///      folgenden Stufen den Kanal noch weiter ab — die Schätzung wäre zu
        ///      optimistisch, und die Maschine liefe für einen Durchsatz an, den es nicht
        ///      gibt. Gemessen an einem präparierten 1024 mit dem BHKW an Position 1:
        ///      +9,14 MWh Produktion, davon 8,87 MWh verworfen. Deshalb: keine Schätzung,
        ///      wo sie nachweislich danebenliegt.
        ///
        /// In beiden Fällen ist der Term 0 und es bleibt beim bisherigen Verhalten. Er
        /// wirkt genau dort, wofür er gedacht ist: getrennte Kanäle, BHKW als letzte
        /// Bedarfsstufe — etwa ein Heizungs-BHKW mit Brauchwasserpuffer als Zweitsenke.
        ///
        /// OFFEN BLEIBT (Konzeptfrage 5-2): Ein Erzeuger mit besserer Ladepriorität kann
        /// dem BHKW in Phase C/D das DURCHSATZbudget desselben Kanals wegnehmen. Die
        /// Reservierung aus Befund N3 sichert nur die Ladefähigkeit des Speichers.
        /// </summary>
        private double ZweitsenkenRaum(bool pvUeberschuss, string wsTyp,
                                       double rest_heiz, double rest_ww)
        {
            if (Auftrag_Zweit == null || Auftrag_Zweit.Speicher == null) return 0;

            SimulationPufferspeicher sp = Auftrag_Zweit.Speicher;
            double ladefaehig = sp.Ladefaehigkeit(Auftrag_Zweit.ObergrenzeStunde(pvUeberschuss));
            if (!LetzteBedarfsstufe) return ladefaehig;

            // D5a: Ein KOMBISPEICHER bedient beide Kanäle - er ist damit immer „der
            // eigene Kanal", ganz gleich, welchen Bedarfsanteil das BHKW deckt.
            bool spWW = sp.IstBrauchwasserkanal;
            bool eigenerKanal = sp.IstKombi ||
                                (wsTyp != WaermequelleClass.SENKE_WARMWASSER &&
                                 wsTyp != WaermequelleClass.SENKE_HEIZUNG) ||
                                (spWW ? wsTyp == WaermequelleClass.SENKE_WARMWASSER
                                      : wsTyp == WaermequelleClass.SENKE_HEIZUNG);
            if (eigenerKanal) return ladefaehig;

            double offen = spWW ? rest_ww : rest_heiz;
            if (offen <= 0) return ladefaehig;

            return ladefaehig + Math.Min(offen, sp.Entnahmefaehigkeit());
        }

        /// <summary>
        /// Phasen C und D für EINEN Ladeauftrag (Konzept 6.3).
        ///
        /// Mit Puffer-HAUPTsenke läuft die Fahrweise erst hier — gegen den BILANZRAUM
        /// des Speichers (Ladefähigkeit + Durchsatz, Nutzerentscheidung zu Befund 4b-1).
        /// Das ist exakt die Größe, die der Altpfad als <c>restWaerme + restSpeicher</c>
        /// gebildet hat; nur ist der Durchsatz jetzt eine eigene, ausgewiesene Größe.
        ///
        /// Mit Heizkreis-Hauptsenke liegt die Produktion aus Phase B bereits vor und
        /// wird hier nur noch eingelagert.
        ///
        /// KEIN <see cref="Kaskadenschleife.SenkeAbziehen"/> — das ist der
        /// Doppelzählungs-Freibeweis.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss, double[] absehbar)
        {
            if (a == null || a.Speicher == null) return 0;

            // EIN Ladevorgang je Stufe: Die Fahrweisen schalten alle Module gemeinsam zu,
            // ein zweiter Auftrag derselben Stufe würde dieselbe Stunde erneut rechnen.
            if (a.AnlagenID != _fuehrendeAnlage) return 0;

            SimulationPufferspeicher sp = a.Speicher;

            // BEFUND N3: Die in Phase B für DIESES Modul festgehaltene Ladefähigkeit
            // wieder freigeben - sie war nur gegen die Erzeuger vor ihm gesperrt.
            if (ReferenceEquals(_reservierterSpeicher, sp))
            {
                sp.ReservierungFreigeben();
                _reservierterSpeicher = null;
            }

            // D5a: Beim KOMBISPEICHER ist das Durchsatzbudget die Summe beider Kanäle —
            // die gemeinsame Fassung steht in der Kaskadenschleife und liefert ohne
            // Kombispeicher Anweisung für Anweisung das Bisherige.
            double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
            double durchlass = Kaskadenschleife.DurchlassBudget(sp, absehbar);

            if (!a.Zweitsenke && _stufensenke.Haupt != Senke.Heizkreis)
            {
                // Reine Ladeanlage: JETZT läuft die Fahrweise, gegen den Bilanzraum.
                double gedeckt, geladen;
                Fahrweise_Stunde(stunde, 0, ladefaehig + durchlass, out gedeckt, out geladen);
                _ueberschussStunde += geladen + gedeckt;
            }

            if (_ueberschussStunde <= 0) return 0;

            double menge = Math.Min(_ueberschussStunde, ladefaehig + durchlass);
            if (menge <= 0) return 0;

            double ladung = sp.Laden(menge, stunde, durchlass);
            if (ladung <= 0) return 0;

            double genutzterDurchlass = ladung - ladefaehig;
            if (genutzterDurchlass > 0)
                Kaskadenschleife.DurchlassBuchen(sp, absehbar, genutzterDurchlass);

            _ueberschussStunde -= ladung;
            Speicherladung_gesamt += ladung;
            if (stunde >= 0 && stunde < 8760) Speicherladung_stuendlich[stunde] += ladung;

            return ladung;
        }

        /// <summary>
        /// Stundenende: Was weder gedeckt noch gespeichert wurde, ist Wärmeüberschuss.
        ///
        /// Damit ist <see cref="Waermeueberschuss"/> in allen drei Fahrweisen dieselbe
        /// Größe — im Altpfad kannte sie nur die stromgeführte Fahrweise (Überlauf des
        /// Pendelspeichers), die wärmegeführte trug dort den toten Solar-Überschuss und
        /// die Fahrweise ohne Einspeisung gar nichts.
        ///
        /// Zugleich entsteht hier die Ganglinie des RESTWÄRMEBEDARFS.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N4: Sie stand bisher auf dem PROJEKTrest nach
        /// Phase F — also nach dem Heizstab der Wärmepumpe und nach den Beiträgen aller
        /// anderen Erzeuger. Der Skalar in <c>Tab_ErgebnisBHKW</c> entstand dagegen an
        /// der BHKW-Position; beide Größen unterschieden sich um genau den Heizstab
        /// (gemessen an 1024: 72,13 gegen 46,14 MWh). Jetzt bilden BEIDE dieselbe
        /// Rechnung ab —
        ///
        /// <code>
        /// Rest = Stufeneingang − Direktdeckung − zugerechnete Speicherentladung
        /// </code>
        ///
        /// —, die Ganglinie stundenweise, der Skalar als Jahressumme. Der Ausdruck ist
        /// KONSTRUKTIV nicht negativ: Direktdeckung und Entladung dieser Stunde kommen
        /// beide aus dem Stufeneingang derselben Stunde und können ihn zusammen nicht
        /// überschreiten.
        ///
        /// Die PROJEKTREST-Übergabe an die nächste Kaskadenstufe ist davon getrennt: Sie
        /// läuft über die Kanäle (<c>Waermekanaele</c>) bzw. über
        /// <c>SimulationControl.Rest_Waermebedarf_stuendlich</c> und war nie diese
        /// Ganglinie.
        /// </summary>
        /// <param name="entladungsAnteilStunde">
        /// Der dem BHKW in dieser Stunde zugerechnete Anteil an der bedarfsdeckenden
        /// Speicherentladung [kWh] (Herkunftsrechnung der <c>Kaskadenschleife</c>,
        /// Interimsregel „Vermischung im Speicher"). 0 in der Vektorstufe — dort gibt es
        /// keinen Speicher.
        /// </param>
        public void Stunde_Ende(int stunde, double entladungsAnteilStunde)
        {
            if (_ueberschussStunde > 0)
            {
                Waermeueberschuss += (float)_ueberschussStunde;
                if (stunde >= 0 && stunde < 8760)
                    Ueberschuss_stuendlich[stunde] += _ueberschussStunde;
                _ueberschussStunde = 0;
            }

            if (stunde >= 0 && stunde < 8760)
            {
                double rest = waermebedarf[stunde] - _direktStunde - entladungsAnteilStunde;
                if (rest < 0) rest = 0;
                waermerestbedarf[stunde] = (float)rest;
            }

            _direktStunde = 0;
        }

        /// <summary>
        /// EIN Stundenschritt der eingestellten Fahrweise gegen einen Wärmeraum aus
        /// <paramref name="bedarf"/> (Direktdeckung) und <paramref name="speicherraum"/>
        /// (Ladefähigkeit bzw. Bilanzraum).
        ///
        /// Der Speicher wird dabei als SKALARER SPIEGEL geführt: <c>speicher</c> beginnt
        /// bei 0, <c>kapazitaet</c> ist der Speicherraum. Der Zuwachs von <c>speicher</c>
        /// ist damit genau die einzulagernde Menge, der Rückgang von <c>restWaerme</c>
        /// die Direktdeckung. Gerechnet wird mit denselben drei Motorläufen, die auch der
        /// entfallene Altpfad benutzt hat
        /// (<see cref="Motorlauf_Waermegefuehrt"/>, <see cref="Motorlauf_Stromgefuehrt"/>,
        /// <see cref="Motorlauf_OhneEinspeisung"/>) — es gibt keine zweite Physik.
        /// </summary>
        private void Fahrweise_Stunde(int stunde, double bedarf, double speicherraum,
                                      out double gedeckt, out double geladen)
        {
            if (speicherraum < 0) speicherraum = 0;

            float speicher = 0f;
            float restWaerme = (float)bedarf;
            float kapazitaet = (float)speicherraum;

            if (modeBHKW == 1)
            {
                float restStrom = (stunde >= 0 && stunde < strombedarf.Length) ? strombedarf[stunde] : 0f;
                Motorlauf_Stromgefuehrt(stunde, _anzahlZweikanalig, stromproduktion, waermeproduktion,
                                        s_waerme_MWh, s_strom_MWh, bhkwWaermeLeistung, bhkwStromLeistung,
                                        bhkwGrenzleistungAllgemein, ref restStrom, ref restWaerme);

                // Stromgeführt kennt keine Speichergrenze in der Zuschaltung: Was über den
                // Bedarf hinaus entsteht, ist Koppelprodukt und geht in die Ladephase.
                if (restWaerme < 0)
                {
                    speicher = -restWaerme;
                    restWaerme = 0f;
                }
            }
            else if (modeBHKW == 2)
            {
                float restStrom = (stunde >= 0 && stunde < strombedarf.Length) ? strombedarf[stunde] : 0f;
                Motorlauf_OhneEinspeisung(stunde, _anzahlZweikanalig, stromproduktion, waermeproduktion,
                                          s_waerme_MWh, s_strom_MWh, bhkwWaermeLeistung, bhkwStromLeistung,
                                          bhkwGrenzleistungAllgemein, kapazitaet,
                                          ref speicher, ref restWaerme, ref restStrom);
            }
            else
            {
                // PAKET BHKW-REGULÄR: Die Argumente stdTag, strombedarf,
                // bhkwGrenzleistungAllgemein und der Solar-Speicheranteil 0f sind
                // entfallen - sie speisten ausschließlich die toten Sommer- und
                // Notschaltungszweige des Motorlaufs (Begründung dort).
                Motorlauf_Waermegefuehrt(stunde, _anzahlZweikanalig, stromproduktion, waermeproduktion,
                                         s_waerme_MWh, s_strom_MWh, bhkwWaermeLeistung, bhkwStromLeistung,
                                         bhkwGrenzL, kapazitaet, ref speicher, ref restWaerme);
            }

            gedeckt = bedarf - restWaerme;
            if (gedeckt < 0) gedeckt = 0;

            geladen = speicher;
            if (geladen < 0) geladen = 0;
        }

        /// <summary>
        /// Jahressummen, Laufzeiten, Verbrauch und Emissionen des zweikanaligen Wegs —
        /// und die ENERGIEPROBE des Moduls.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N8: <see cref="Speicherladung_stuendlich"/>,
        /// <see cref="Speicherladung_gesamt"/> und <see cref="Ueberschuss_stuendlich"/>
        /// wurden bisher nur GESCHRIEBEN. Sie sind jetzt angebunden — als die Probe, die
        /// ihr Kopfkommentar seit jeher verspricht:
        ///
        /// <code>
        /// Produktion = Direktdeckung + Speicherladung + Überschuss
        /// </code>
        ///
        /// Jede kWh, die die Maschine erzeugt, muss genau einen dieser drei Wege gegangen
        /// sein. Die Probe läuft über die Jahressummen UND über jede einzelne Stunde;
        /// eine Verletzung wäre ein Buchungsfehler in der Phasenstruktur und wird
        /// dialogfrei gemeldet (Konzept 13.4).
        /// </summary>
        public void Abschluss_Zweikanalig()
        {
            for (int j = 0; j < _anzahlZweikanalig; j++)
            {
                s_waerme_MWh[j] /= 1000f;
                s_strom_MWh[j] /= 1000f;
            }

            Auswertung(_anzahlZweikanalig);
            Energieprobe();
        }

        /// <summary>
        /// Probe „Produktion = Direktdeckung + Speicherladung + Überschuss" über die
        /// Jahressumme und über jede Stunde (Befund N8). Die Toleranz trägt die
        /// <c>float</c>-Ganglinien: 1 kWh im Jahr, 0,01 kWh je Stunde.
        ///
        /// ENTWICKLER-SELBSTTEST (Kategorie c des Protokollkanal-Nachzugs): Die Probe
        /// prüft die Bilanz des Moduls gegen sich selbst und sagt dem Anwender nichts
        /// über seine Konfiguration. Sie bleibt deshalb reine Konsolenausgabe.
        /// </summary>
        private void Energieprobe()
        {
            double produktion = 0, maxAbw = 0;
            int maxStunde = -1;

            for (int h = 0; h < 8760; h++)
            {
                produktion += waermeproduktion[h];

                double abw = Math.Abs(waermeproduktion[h] -
                                      (Speicherladung_stuendlich[h] + Ueberschuss_stuendlich[h]));
                // Die Direktdeckung führt das Modul nur als Jahressumme; stundenweise
                // prüfbar ist deshalb der Teil OHNE sie - er muss kleiner oder gleich der
                // Produktion der Stunde sein.
                if (Speicherladung_stuendlich[h] + Ueberschuss_stuendlich[h] >
                    waermeproduktion[h] + 0.01)
                {
                    if (abw > maxAbw) { maxAbw = abw; maxStunde = h; }
                }
            }

            double summe = Direktdeckung_gesamt + Speicherladung_gesamt + Waermeueberschuss;
            double abwJahr = Math.Abs(produktion - summe);

            if (abwJahr > 1.0)
                Console.WriteLine("BHKW-Energieprobe: Produktion " + produktion.ToString("0.###") +
                                  " kWh gegen Direktdeckung " + Direktdeckung_gesamt.ToString("0.###") +
                                  " + Speicherladung " + Speicherladung_gesamt.ToString("0.###") +
                                  " + Überschuss " + Waermeueberschuss.ToString("0.###") +
                                  " = " + summe.ToString("0.###") + " kWh (Abweichung " +
                                  abwJahr.ToString("0.###") + " kWh).");

            if (maxStunde >= 0)
                Console.WriteLine("BHKW-Energieprobe: In Stunde " + maxStunde + " sind Speicherladung " +
                                  "und Überschuss zusammen um " + maxAbw.ToString("0.####") +
                                  " kWh größer als die Produktion dieser Stunde.");
        }

        /// <summary>
        /// Zweikanalige Stufe OHNE Speicherbeteiligung: dieselben Stundenschritte in
        /// einer eigenen Jahresschleife an der Kaskadenposition des BHKW.
        ///
        /// Der Weg für Projekte, in denen das BHKW weder eine Puffer-Senke noch einen
        /// Pendelspeicher hat. Gegenüber dem Altpfad ändert sich zweierlei: Die Deckung
        /// folgt dem Kanal nach <c>WS_Typ</c> (statt der proportionalen Rückverteilung
        /// über <c>Uebernehmen</c>), und der Restbedarf ist der TATSÄCHLICHE Rest statt
        /// der Vektordifferenz <c>Bedarf − Produktion</c> (Bilanzfehler aus Konzept 6.5).
        /// </summary>
        public bool Berechnung_Zweikanalig(int ID_Projekt, Waermekanaele kanaele,
                                           List<Senkenzuordnung> senken)
        {
            if (kanaele == null) return false;
            if (!Vorbereiten_Zweikanalig(ID_Projekt, senken)) return false;

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                double rest_heiz = kanaele.Heiz[stunde];
                double rest_ww = kanaele.WW[stunde];

                // Ohne Speicher gibt es weder eine Vorabentladung noch eine zugerechnete
                // Speicherentladung: Der Stufeneingang ist der Kanalstand an dieser
                // Kaskadenposition, der Rest genau der nach der eigenen Deckung.
                Stunde_Start(stunde, rest_heiz, rest_ww);
                Stunde_Bedarf(stunde, false, ref rest_heiz, ref rest_ww);
                Stunde_Ende(stunde, 0);

                kanaele.Heiz[stunde] = (float)rest_heiz;
                kanaele.WW[stunde] = (float)rest_ww;
            }

            Abschluss_Zweikanalig();
            return true;
        }

    }
}

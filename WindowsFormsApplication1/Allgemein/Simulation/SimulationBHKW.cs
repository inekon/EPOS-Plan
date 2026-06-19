using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{

    public class SimulationBHKW
    {
        public List<int> bhkw_list = new List<int>();
        public int m_ID_Projekt = 0;

        public float[] waermebedarf = new float[8760];
        public float[] strombedarf = new float[8760];
        public float[] stromproduktion = new float[8760];
        public float[] waermerestbedarf = new float[8760];
        public float[] waermeproduktion = new float[8760];

        public float[] s_waerme = new float[10]; // Summenzähler für Wärme (10 Module laut VBA-Code)
        public float[] s_strom = new float[10];  // Summenzähler für Strom (10 Module laut VBA-Code)

        public float Stromproduktion_gesamt => stromproduktion.Sum();
        public float Waermeproduktion_gesamt => waermeproduktion.Sum();

        public int modeBHKW;

        float[] bhkwWaermeLeistung = new float[10];
        float[] bhkwStromLeistung = new float[10];
        public float[] bhkwGrenzL = new float[10];
        public float bhkwGrenzleistungAllgemein = 0;
   
        // Vorgaben für Solar und Speicher setzen
        bool solarVorhanden = false; // Solar = 0
        float solarSpeicher = 0.0f;
        float solarWaerme = 0.0f;
        float solarUeberschuss = 0.0f;
        public float kapazitaetPendelspeicher = 0.0f; // Pendelspeicher = 0

        int anzahlBhkw = 0;

        // --- Neue Ergebnis- und Verbrauchsvariablen aus VBA ---
        public float BruttoBHKWErzeugung = 0f;
        public float Waermeproduktion_BHKW = 0f;
        public float Stromproduktion_BHKW = 0f;
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
        public float Stromverbrauch_BHKW = 0f; // Ergänzt aus Brennstoffart 14

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
        public float Betriebsstunden = 0f;
        public float dLaufzeiten = 0f;
        public float[] Laufzeiten = new float[10];

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

        public bool Berechnung(int ProjektID)
        {
            m_ID_Projekt = ProjektID;
            anzahlBhkw = bhkw_list.Count;

            // Variablen vor jeder Berechnung sauber zurücksetzen
            BruttoBHKWErzeugung = 0f;
            Waermeproduktion_BHKW = 0f;
            Stromproduktion_BHKW = 0f;
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


            bhkwGrenzleistungAllgemein /= 100;
            if (bhkwGrenzleistungAllgemein == 0) bhkwGrenzleistungAllgemein = 0.5f;

            BHKWCtrl ctrl = new BHKWCtrl();
            for (int i = 0; i < anzahlBhkw; i++)
            {
                ctrl.ReadSingle(bhkw_list[i]);
                bhkwWaermeLeistung[i] = (float)ctrl.m_Ptherm;
                bhkwStromLeistung[i] = (float)ctrl.m_Pel;

                // Neue Werte für die Verbrauchs- und Emissionsberechnung mappen:
                bhkwBrennstoffart[i] = ctrl.m_Brennstoff; // oder passendes Feld aus deiner BHKWCtrl
                bhkwWirkungsgrad[i] = (float)ctrl.m_Wirkungsgrad; // Gesamtwirkungsgrad (Elektrisch + Thermisch)
                bhkwSKZ[i] = bhkwStromLeistung[i] / bhkwWaermeLeistung[i];
                
                if((float)ctrl.m_Grenzleistung == 0)
                    bhkwGrenzL[i] = bhkwGrenzleistungAllgemein; // Grenzleistung als Faktor (z.B. 0.8 für 80% Modulation)
                else
                    bhkwGrenzL[i] = (float)ctrl.m_Grenzleistung; // Grenzleistung als Faktor (z.B. 0.8 für 80% Modulation)

                // Emissionsfaktoren aus der DB auslesen (Äquivalent zu Cells(zaehler+2, X))
                bhkwCO2Factor[i] = (float)ctrl.m_CO2;
                bhkwSO2Factor[i] = (float)ctrl.m_SO2;
                bhkwNOXFactor[i] = (float)ctrl.m_NOx;
                bhkwCOFactor[i] = (float)ctrl.m_CO;
                bhkwStaubFactor[i] = (float)ctrl.m_Staub;
            }

            // --- 1. Simulationen ausführen ---
            if (modeBHKW == 0)
            {
                BhkwSimulationWaermegefuehrt(
                    waermebedarf, stromproduktion, waermerestbedarf, waermeproduktion,
                    s_waerme, s_strom, kapazitaetPendelspeicher, anzahlBhkw,
                    bhkwWaermeLeistung, bhkwStromLeistung, ref solarSpeicher,
                    bhkwGrenzL, solarVorhanden, ref solarWaerme, ref solarUeberschuss,
                    strombedarf, bhkwGrenzleistungAllgemein
                );
            }
            else if (modeBHKW == 1)
            {
                SimulationStromgefuehrt(
                    waermebedarf, strombedarf, stromproduktion, waermerestbedarf, waermeproduktion,
                    s_waerme, s_strom, kapazitaetPendelspeicher, anzahlBhkw,
                    bhkwWaermeLeistung, bhkwStromLeistung, bhkwGrenzleistungAllgemein, ref solarUeberschuss
                );
            }
            else if (modeBHKW == 2)
            {
                SimulationOhneEinspeisung(
                    waermebedarf, strombedarf, stromproduktion, waermerestbedarf, waermeproduktion,
                    s_waerme, s_strom, kapazitaetPendelspeicher, anzahlBhkw,
                    bhkwWaermeLeistung, bhkwStromLeistung, bhkwGrenzleistungAllgemein
                );
            }

            // --- 2. Das übersetzte VBA-Code-Fragment für die Auswertung anhängen ---
            for (int zaehler = 0; zaehler < anzahlBhkw; zaehler++)
            {
                Waermeproduktion_BHKW += s_waerme[zaehler];
                Stromproduktion_BHKW += s_strom[zaehler];

                // Laufzeitberechnung des Moduls
                if (bhkwWaermeLeistung[zaehler] > 0)
                {
                    // s_waerme ist am Ende bereits in MWh umgerechnet worden (laut deinem Code-Ende / 1000f),
                    // für die Stundenlaufzeit multiplizieren wir wieder mit 1000, um auf kWh/kW zu kommen.
                    Laufzeiten[zaehler] = (s_waerme[zaehler] / bhkwWaermeLeistung[zaehler]) * 1000f;
                }
                else
                {
                    Laufzeiten[zaehler] = 0f;
                }
                dLaufzeiten += Laufzeiten[zaehler];

                // Verbrauch & Emissionen
                if (bhkwWirkungsgrad[zaehler] > 0)
                {
                    // Verbrauch berechnen (Wärme + Strom) / Wirkungsgrad
                    float ModulVerbrauch = (s_waerme[zaehler] + s_strom[zaehler]) / bhkwWirkungsgrad[zaehler];

                    BruttoBHKWErzeugung += ModulVerbrauch;

                    // Emissionen addieren und von g in kg oder Tonnen skalieren (/1000)
                    Em_CO2_BHKW += ModulVerbrauch * bhkwCO2Factor[zaehler] / 1000f;
                    Em_SO2_BHKW += ModulVerbrauch * bhkwSO2Factor[zaehler] / 1000f;
                    Em_NOX_BHKW += ModulVerbrauch * bhkwNOXFactor[zaehler] / 1000f;
                    Em_CO_BHKW += ModulVerbrauch * bhkwCOFactor[zaehler] / 1000f;
                    Em_Staub_BHKW += ModulVerbrauch * bhkwStaubFactor[zaehler] / 1000f;

                    // Brennstoffarten-Verzweigung

                    // Den Verbrauch auf die globalen Brennstoffzähler buchen
                    int art = bhkwBrennstoffart[zaehler];

                    if (art >= 1 && art <= 5)
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
  
                }
            }

            // gesamte Laufzeiten
            Betriebsstunden = dLaufzeiten;
            // Durchschnittliche Laufzeit berechnen
            if (anzahlBhkw > 0)
            {
                dLaufzeiten = dLaufzeiten / anzahlBhkw;
            }

            return true;
        }


        // Hinweis: Die globalen VBA-Variablen (wie solar_waerme, solar_vorhanden etc.) 
        // werden hier als 'ref' übergeben, damit Änderungen zurückgegeben werden.
        public void BhkwSimulationWaermegefuehrt(
                float[] waermebedarf,
                float[] stromproduktion,
                float[] waermerestbedarf,
                float[] waermeproduktion,
                float[] s_waerme,
                float[] s_strom,
                float kapazitaetPendelspeicher,
                int anzahl,
                float[] bhkwWaermeLeistung,
                float[] bhkwStromLeistung,
                ref float solarSpeicher,
                float[] bhkwGrenzL,
                bool solarVorhanden,
                ref float solarWaerme,
                ref float solarUeberschuss,
                float[] strombedarf, // Im Sommerbetrieb des VBA-Codes genutzt!
                float bhkwGrenzleistung // In der Notschaltung des VBA-Codes genutzt!
            )
        {
            float speicher = 0f;
            float restWaerme = 0f;
            float restSpeicher = 0f;
            int stdTag = 0;
            bool solar = false;
            float solW = 0f;
            float solSp = 0f;
            
            // Arrays initialisieren (die ersten 10 Elemente nullen)
            for (int i = 0; i <= 9; i++)
            {
                s_waerme[i] = 0f;
                s_strom[i] = 0f;
            }

            // Solar-Verfügbarkeit prüfen
            // HINWEIS: 'Bericht.Cells(50, 4)' wurde hier als Platzhalter-Bedingung eingebaut,
            // da das Excel-Objekt in C# so nicht existiert.
            bool berichtAusschluss = false; // Hier Logik für Bericht.Cells(50, 4) = "Ja" abbilden falls nötig

            if (solarVorhanden && !berichtAusschluss)
            {
                solar = true;
                solarWaerme = 0f;
                solarUeberschuss = 0f;
                solarSpeicher = kapazitaetPendelspeicher * solarSpeicher / 100f;
            }
            else
            {
                solarSpeicher = 0f;
                solar = false;
            }

            solSp = 0f;

            // Die Jahresschleife über 8760 Stunden
            for (int stunde = 0; stunde <= 8759; stunde++)
            {
                stdTag = (stunde % 24) + 1;

                // ********************************************** Solar
                if (solar)
                {
                    solW = SolareErzeugung(stunde);
                    if (solW + solSp > waermebedarf[stunde])
                    {
                        restWaerme = 0f;
                        solSp = solSp + solW - waermebedarf[stunde];

                        if (solSp > solarSpeicher)
                        {
                            solarWaerme = solarWaerme + SolareErzeugung(stunde) - (solSp - solarSpeicher);
                            solarUeberschuss = solarUeberschuss + (solSp - solarSpeicher);
                            solSp = solarSpeicher;
                        }
                        else
                        {
                            solarWaerme = solarWaerme + SolareErzeugung(stunde);
                        }
                    }
                    else
                    {
                        restWaerme = waermebedarf[stunde] - SolareErzeugung(stunde) - solSp;
                        solSp = 0f;
                        solarWaerme = solarWaerme + SolareErzeugung(stunde);
                    }
                }
                else
                {
                    restWaerme = waermebedarf[stunde];
                }
                // **********************************************

                stromproduktion[stunde] = 0f;
                waermeproduktion[stunde] = 0f;
                restSpeicher = kapazitaetPendelspeicher - solarSpeicher - speicher;

                // Winterbetrieb
                if (stunde < 3600 || stunde > 5760)
                {
                    for (int motor = 0; motor < anzahl; motor++)
                    {
                        restSpeicher = kapazitaetPendelspeicher - solarSpeicher - speicher;

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
                            speicher = kapazitaetPendelspeicher - solarSpeicher;
                            restWaerme = 0f;
                        }
                    }
                }
                // Sommerbetrieb
                else if (stdTag > 10 && stdTag < 22)
                {
                    for (int motor = 0; motor < anzahl; motor++)
                    {
                        restSpeicher = kapazitaetPendelspeicher - solarSpeicher - speicher;

                        if (bhkwStromLeistung[motor] < strombedarf[stunde] && (restSpeicher + restWaerme > bhkwWaermeLeistung[motor]) && bhkwStromLeistung[motor] > 0.2f)
                        {
                            stromproduktion[stunde] += bhkwStromLeistung[motor];
                            s_strom[motor] += bhkwStromLeistung[motor];
                            waermeproduktion[stunde] += bhkwWaermeLeistung[motor];
                            s_waerme[motor] += bhkwWaermeLeistung[motor];
                            restWaerme -= bhkwWaermeLeistung[motor];

                            if (restWaerme < 0)
                            {
                                speicher -= restWaerme;
                                restWaerme = 0f;
                            }
                        }
                        else if (bhkwStromLeistung[motor] * bhkwGrenzL[motor] <= strombedarf[stunde] && (restSpeicher + restWaerme > strombedarf[stunde] / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor]) && bhkwStromLeistung[motor] > 0.2f)
                        {
                            stromproduktion[stunde] += strombedarf[stunde];
                            s_strom[motor] += strombedarf[stunde];
                            waermeproduktion[stunde] += strombedarf[stunde] / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];
                            s_waerme[motor] += strombedarf[stunde] / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];
                            restWaerme -= strombedarf[stunde] / bhkwStromLeistung[motor] * bhkwWaermeLeistung[motor];

                            if (restWaerme < 0)
                            {
                                speicher -= restWaerme;
                                restWaerme = 0f;
                            }
                        }
                        else if (bhkwStromLeistung[motor] * bhkwGrenzL[motor] <= strombedarf[stunde] && (restSpeicher + restWaerme > bhkwWaermeLeistung[motor] * bhkwGrenzL[motor]) && bhkwStromLeistung[motor] > 0.2f)
                        {
                            waermeproduktion[stunde] += restSpeicher + restWaerme;
                            s_waerme[motor] += restSpeicher + restWaerme;
                            stromproduktion[stunde] += (restSpeicher + restWaerme) / bhkwWaermeLeistung[motor] * bhkwStromLeistung[motor];
                            s_strom[motor] += (restSpeicher + restWaerme) / bhkwWaermeLeistung[motor] * bhkwStromLeistung[motor];
                            restWaerme = 0f;
                            speicher = kapazitaetPendelspeicher - solarSpeicher;
                        }
                        else if (bhkwStromLeistung[motor] * bhkwGrenzL[motor] * 0.8f <= strombedarf[stunde] && speicher < kapazitaetPendelspeicher * 0.3f && bhkwStromLeistung[motor] > 0.2f)
                        {
                            waermeproduktion[stunde] += bhkwWaermeLeistung[motor] * bhkwGrenzL[motor];
                            s_waerme[motor] += bhkwWaermeLeistung[motor] * bhkwGrenzL[motor];
                            stromproduktion[stunde] += bhkwStromLeistung[motor] * bhkwGrenzL[motor];
                            s_strom[motor] += bhkwStromLeistung[motor] * bhkwGrenzL[motor];
                            restWaerme -= bhkwWaermeLeistung[motor] * bhkwGrenzL[motor];

                            if (restWaerme < 0)
                            {
                                speicher -= restWaerme;
                                restWaerme = 0f;
                            }
                        }
                        // Notschaltung: es müssen immer 10 % im Speicher sein
                        else if (speicher < kapazitaetPendelspeicher * 0.1f)
                        {
                            waermeproduktion[stunde] += bhkwWaermeLeistung[motor] * bhkwGrenzL[motor];
                            s_waerme[motor] += bhkwWaermeLeistung[motor] * bhkwGrenzL[motor];
                            stromproduktion[stunde] += bhkwStromLeistung[motor] * bhkwGrenzL[motor];
                            s_strom[motor] += bhkwStromLeistung[motor] * bhkwGrenzL[motor];
                            restWaerme -= bhkwWaermeLeistung[motor] * bhkwGrenzleistung;

                            if (restWaerme < 0)
                            {
                                speicher -= restWaerme;
                                restWaerme = 0f;
                            }
                        }
                    }
                }
                // Notschaltung: es müssen immer 20 % im Speicher sein
                else if (speicher - restWaerme < kapazitaetPendelspeicher * 0.2f && (stdTag > 5 && stdTag < 10))
                {
                    for (int motor = 0; motor < anzahl; motor++)
                    {
                        restSpeicher = kapazitaetPendelspeicher - solarSpeicher - speicher;

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
                            speicher = kapazitaetPendelspeicher - solarSpeicher;
                            restWaerme = 0f;
                        }
                    }
                }

                // Endabrechnung der Stunde für den Speicher
                if (restWaerme > speicher)
                {
                    restWaerme -= speicher;
                    speicher = 0f;
                }
                else
                {
                    speicher -= restWaerme;
                    restWaerme = 0f;
                }

                waermerestbedarf[stunde] = restWaerme;
            }

            // Ergebnisse auf MWh (bzw. durch 1000) herunterskalieren
            for (int j = 0; j < anzahl; j++)
            {
                s_waerme[j] /= 1000f;
                s_strom[j] /= 1000f;
            }

            if (solarUeberschuss > 0)
            {
                solarWaerme -= solarUeberschuss;
                solarUeberschuss /= 1000f;
            }

            if (solarWaerme > 0)
            {
                solarWaerme /= 1000f;
            }
        }

        public void SimulationStromgefuehrt(
            float[] waermebedarf,
            float[] strombedarf,
            float[] stromproduktion,
            float[] waermerestbedarf,
            float[] waermeproduktion,
            float[] s_waerme,
            float[] s_strom,
            float kapazitaetPendelspeicher,
            int anzahl,
            float[] bhkwWaermeLeistung,
            float[] bhkwStromLeistung,
            float bhkwGrenzleistung,
            ref float waermeUeberschuss
        )
        {
            float speicher = 0f;
            float restWaerme = 0f;
            float restStrom = 0f;
            int stdTag = 0;

            // Die Jahresschleife über 8760 Stunden
            for (int stunde = 0; stunde <= 8759; stunde++)
            {
                stdTag = (stunde % 24) + 1;

                // Restwärmebedarf berechnen (Wärmebedarf abzüglich dem, was noch im Puffer ist)
                restWaerme = waermebedarf[stunde] - speicher;
                speicher = 0f; // Speicher wird geleert/verbraucht

                restStrom = strombedarf[stunde];
                stromproduktion[stunde] = 0f;
                waermeproduktion[stunde] = 0f;

                // Die Motoren/Module nacheinander zuschalten
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

                // Wenn restWaerme negativ ist, bedeutet das: Es wurde mehr Wärme produziert als benötigt -> Ab in den Speicher!
                if (restWaerme < 0)
                {
                    speicher = speicher - restWaerme; // restWaerme ist negativ, minus und minus ergibt plus
                    restWaerme = 0f;
                }

                // Wenn der Speicher überläuft, entsteht Wärmeüberschuss
                if (speicher > kapazitaetPendelspeicher)
                {
                    waermeUeberschuss += (speicher - kapazitaetPendelspeicher);
                    speicher = kapazitaetPendelspeicher;
                }

                // Verbleibender ungedeckter Wärmebedarf (z.B. für den Spitzenlastkessel) wegschreiben
                waermerestbedarf[stunde] = restWaerme;
            }

            // Ergebnisse am Ende der Jahressimulation von kW in MWh umrechnen (/ 1000)
            for (int j = 0; j < anzahl; j++)
            {
                s_waerme[j] /= 1000f;
                s_strom[j] /= 1000f;
            }
        }

        public void SimulationOhneEinspeisung(
            float[] waermebedarf,
            float[] strombedarf,
            float[] stromproduktion,
            float[] waermerestbedarf,
            float[] waermeproduktion,
            float[] s_waerme,
            float[] s_strom,
            float kapazitaetPendelspeicher,
            int anzahl,
            float[] bhkwWaermeLeistung,
            float[] bhkwStromLeistung,
            float bhkwGrenzleistung
        )
        {
            float speicher = 0f;
            float restSpeicher = 0f;
            float restStrom = 0f;
            float restWaerme = 0f;
            int stdTag = 0;
            float wLeistung = 0f;
            float sLeistung = 0f;

            // Die ersten 10 Elemente der Summenzähler nullen
            for (int i = 0; i <= 9; i++)
            {
                s_waerme[i] = 0f;
                s_strom[i] = 0f;
            }

            // Die Jahresschleife über 8760 Stunden
            for (int stunde = 0; stunde <= 8759; stunde++)
            {
                stdTag = (stunde % 24) + 1;
                restWaerme = waermebedarf[stunde];
                restStrom = strombedarf[stunde];

                stromproduktion[stunde] = 0f;
                waermeproduktion[stunde] = 0f;
                restSpeicher = kapazitaetPendelspeicher - speicher;


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


                // --- Speicherabrechnung am Ende der Stunde ---
                if (restWaerme > speicher)
                {
                    restWaerme -= speicher;
                    speicher = 0f;
                }
                else
                {
                    speicher -= restWaerme;
                    restWaerme = 0f;
                }

                waermerestbedarf[stunde] = restWaerme;
            }

            // Am Ende der Simulation von kW in MWh umrechnen
            for (int j = 0; j < anzahl; j++)
            {
                s_waerme[j] /= 1000f;
                s_strom[j] /= 1000f;
            }
        }



        // Hilfsmethode als Platzhalter für die solare Erzeugung
        private float SolareErzeugung(int stunde)
        {
            // Hier deine Berechnungslogik für die Solarstunde einfügen
            return 0f;
        }

    }
}

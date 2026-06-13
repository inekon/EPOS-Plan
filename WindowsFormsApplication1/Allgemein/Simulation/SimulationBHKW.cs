using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        // Summenzähler für die Auswertung (Größe 10 laut deinem VBA-Code)

            // 4. Deine Vorgaben für Solar und Speicher setzen
        bool solarVorhanden = false; // Solar = 0
        float solarSpeicher = 0.0f;
        float solarWaerme = 0.0f;
        float solarUeberschuss = 0.0f;
        public float kapazitaetPendelspeicher = 0.0f; // Pendelspeicher = 0

        int anzahlBhkw = 0;

        public SimulationBHKW()
        {
  
            // 3. BHKW-Anlagendaten definieren (Beispiel mit 2 Modulen)
            anzahlBhkw = bhkw_list.Count; //2;

            BHKWCtrl ctrl = new BHKWCtrl();
            for (int i = 0; i < anzahlBhkw; i++)
            {
                ctrl.ReadSingle(bhkw_list[i]);
                // Beispielwerte für Wärme- und Stromleistung der BHKW-Module
                bhkwWaermeLeistung[i] = (float)ctrl.m_Ptherm; 
                bhkwStromLeistung[i] = (float)ctrl.m_Pel;
                //hkwGrenzL[i] = (float)ctrl.m_Grenzleistung;
            }

/*
            // 5. Funktion aufrufen
            System.Diagnostics.Debug.WriteLine("Simulationslauf wird gestartet...");

            BhkwSimulationWaermegefuehrt(
                waermebedarf,
                stromproduktion,
                waermerestbedarf,
                waermeproduktion,
                s_waerme,
                s_strom,
                kapazitaetPendelspeicher,
                anzahlBhkw,
                bhkwWaermeLeistung,
                bhkwStromLeistung,
                ref solarSpeicher,
                bhkwGrenzL,
                solarVorhanden,
                ref solarWaerme,
                ref solarUeberschuss,
                strombedarf,
                bhkwGrenzleistungAllgemein
            );

            // 6. Ergebnisse in der Konsole ausgeben
            System.Diagnostics.Debug.WriteLine("\n--- SIMULATIONS-ERGEBNISSE (Jahressummen) ---");
            for (int m = 0; m < anzahlBhkw; m++)
            {
                System.Diagnostics.Debug.WriteLine($"BHKW Modul {m + 1}:");
                System.Diagnostics.Debug.WriteLine($"  -> Erzeugte Wärme: {s_waerme[m]:F2} MWh");
                System.Diagnostics.Debug.WriteLine($"  -> Erzeugter Strom: {s_strom[m]:F2} MWh");
            }

            // Stichprobe für eine Winter- und eine Sommerstunde ausgeben
            System.Diagnostics.Debug.WriteLine("\n--- STICHPROBEN (Momentanwerte) ---");
            System.Diagnostics.Debug.WriteLine($"Winter (Stunde 1000) - Bedarf: {waermebedarf[1000]} kW | BHKW-Wärme: {waermeproduktion[1000]} kW | Restbedarf: {waermerestbedarf[1000]} kW");
            System.Diagnostics.Debug.WriteLine($"Sommer (Stunde 4000) - Bedarf: {waermebedarf[4000]} kW | BHKW-Wärme: {waermeproduktion[4000]} kW | Restbedarf: {waermerestbedarf[4000]} kW");


            // 6. Ergebnisse in separate Textdateien schreiben
            string ordnerPfad = AppDomain.CurrentDomain.BaseDirectory; // Speicherort ist der Debug/Release-Ordner deiner App
            string pfadBedarf = Path.Combine(ordnerPfad, "Waermebedarf.txt");
            string pfadErzeugung = Path.Combine(ordnerPfad, "Waermeerzeugung.txt");

            try
            {
                // StreamWriter für beide Dateien öffnen (false = Datei wird bei jedem Start überschrieben)
                using (StreamWriter swBedarf = new StreamWriter(pfadBedarf, false))
                using (StreamWriter swErzeugung = new StreamWriter(pfadErzeugung, false))
                {
                    // Spaltenüberschriften in die Textdateien schreiben
                    swBedarf.WriteLine("Stunde;Waermebedarf_kW");
                    swErzeugung.WriteLine("Stunde;Waermeproduktion_BHKW_kW;Waermerestbedarf_kW");

                    // Alle 8760 Stunden durchlaufen und zeilenweise wegschreiben
                    for (int stunde = 0; stunde < 8760; stunde++)
                    {
                        // :F2 sorgt für genau 2 Nachkommastellen
                        swBedarf.WriteLine($"{stunde};{waermebedarf[stunde]:F2}");
                        swErzeugung.WriteLine($"{stunde};{waermeproduktion[stunde]:F2};{waermerestbedarf[stunde]:F2}");
                    }
                }

                // Erfolgsmeldung direkt als Windows-Fenster ausgeben
                MessageBox.Show($"Simulation erfolgreich beendet!\n\nDateien wurden erstellt:\n" +
                                $"1. {pfadBedarf}\n" +
                                $"2. {pfadErzeugung}",
                                "Simulation beendet",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Falls eine Datei z.B. noch in Excel geöffnet ist und blockiert wird:
                MessageBox.Show($"Fehler beim Schreiben der Ergebnisdateien:\n{ex.Message}",
                                "Schreibfehler",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
*/

        }

        public bool Berechnung(int ProjektID)
        {
            // Hier könnte die Logik für die Berechnung basierend auf der ProjektID implementiert werden
            // Aktuell gibt es keine konkrete Berechnung, daher wird nur 0 zurückgegeben
            m_ID_Projekt = ProjektID;

            anzahlBhkw = bhkw_list.Count; //2;

            BHKWCtrl ctrl = new BHKWCtrl();
            for (int i = 0; i < anzahlBhkw; i++)
            {
                ctrl.ReadSingle(bhkw_list[i]);
                // Beispielwerte für Wärme- und Stromleistung der BHKW-Module
                bhkwWaermeLeistung[i] = (float)ctrl.m_Ptherm;
                bhkwStromLeistung[i] = (float)ctrl.m_Pel;
     //           bhkwGrenzL[i] = (float)ctrl.m_Grenzleistung;
            }

            if (modeBHKW == 0)
            {
                BhkwSimulationWaermegefuehrt(
                    waermebedarf,
                    stromproduktion,
                    waermerestbedarf,
                    waermeproduktion,
                    s_waerme,
                    s_strom,
                    kapazitaetPendelspeicher,
                    anzahlBhkw,
                    bhkwWaermeLeistung,
                    bhkwStromLeistung,
                    ref solarSpeicher,
                    bhkwGrenzL,
                    solarVorhanden,
                    ref solarWaerme,
                    ref solarUeberschuss,
                    strombedarf,
                    bhkwGrenzleistungAllgemein
                );
            }
            else if (modeBHKW == 1) {
                SimulationStromgefuehrt(
                    waermebedarf,
                    strombedarf,
                    stromproduktion,
                    waermerestbedarf,
                    waermeproduktion,
                    s_waerme,
                    s_strom,
                    kapazitaetPendelspeicher,
                    anzahlBhkw,
                    bhkwWaermeLeistung,
                    bhkwStromLeistung,
                    bhkwGrenzleistungAllgemein,
                    ref solarUeberschuss
                );
            }
            else if (modeBHKW == 2)
            {
                SimulationOhneEinspeisung(
                    waermebedarf,
                    strombedarf,
                    stromproduktion,
                    waermerestbedarf,
                    waermeproduktion,
                    s_waerme,
                    s_strom,
                    kapazitaetPendelspeicher,
                    anzahlBhkw,
                    bhkwWaermeLeistung,
                    bhkwStromLeistung,
                    bhkwGrenzleistungAllgemein
                );
            }

            /*
            string ordnerPfad = AppDomain.CurrentDomain.BaseDirectory; // Speicherort ist der Debug/Release-Ordner deiner App
            string pfadBedarf = Path.Combine(ordnerPfad, "Waermebedarf.txt");
            string pfadErzeugung = Path.Combine(ordnerPfad, "Waermeerzeugung.txt");


            try
            {
                // StreamWriter für beide Dateien öffnen (false = Datei wird bei jedem Start überschrieben)
                using (StreamWriter swBedarf = new StreamWriter(pfadBedarf, false))
                using (StreamWriter swErzeugung = new StreamWriter(pfadErzeugung, false))
                {
                    // Spaltenüberschriften in die Textdateien schreiben
                    swBedarf.WriteLine("Stunde;Waermebedarf_kW");
                    swErzeugung.WriteLine("Stunde;Waermeproduktion_BHKW_kW;Waermerestbedarf_kW");

                    // Alle 8760 Stunden durchlaufen und zeilenweise wegschreiben
                    for (int stunde = 0; stunde < 8760; stunde++)
                    {
                        // :F2 sorgt für genau 2 Nachkommastellen
                        swBedarf.WriteLine($"{stunde};{waermebedarf[stunde]:F2}");
                        swErzeugung.WriteLine($"{stunde};{waermeproduktion[stunde]:F2};{waermerestbedarf[stunde]:F2}");
                    }
                }

                // Erfolgsmeldung direkt als Windows-Fenster ausgeben
                MessageBox.Show($"Simulation erfolgreich beendet!\n\nDateien wurden erstellt:\n" +
                                $"1. {pfadBedarf}\n" +
                                $"2. {pfadErzeugung}",
                                "Simulation beendet",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Falls eine Datei z.B. noch in Excel geöffnet ist und blockiert wird:
                MessageBox.Show($"Fehler beim Schreiben der Ergebnisdateien:\n{ex.Message}",
                                "Schreibfehler",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
            */
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

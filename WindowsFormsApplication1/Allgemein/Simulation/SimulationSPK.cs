using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WindowsFormsApplication1
{
    // Die fehleranfällige und pauschale Jahres-Verlustberechnung (mit den fiktiven Betriebsstunden und der asymmetrischen Bereitschaft)
    // wurde komplett entfernt. Stattdessen wird der Brennstoffverbrauch nun stündlich direkt in der Simulationsschleife ermittelt:
    //
    // - Läuft ein Kessel in einer Stunde, wird sein Verbrauch über den stündlichen Wirkungsgrad ermittelt.
    // - Steht er in einer Stunde still, wird ihm für diese exakte Stunde der anteilige Bereitschaftsverlust
    //   als Brennstoffverbrauch(Wärmeverlust) aufgeschlagen.
    //
    // Am Ende des Jahres wird der Jahresnutzungsgrad in Schritt 5 absolut präzise aus der summierten Nutzwärme und dem summierten Gesamtverbrauch gebildet.

    public class SimulationSPK
    {
        public const int MAX_SPK = 10;

        // Listen und Projektdaten
        public List<string> spk_list = new List<string>();

        /// <summary>
        /// <c>Tab_Energieanlagen.ID</c> je Kessel, INDEXGLEICH zu <see cref="spk_list"/>
        /// (Konzept 6.2). Gefüllt von <c>SimulationControl.Simulation_SPK_Ctrl</c>.
        ///
        /// Warum eine zweite Liste statt einer Umstellung von <see cref="spk_list"/>:
        /// Der Bezeichner dort ist nicht nur Suchschlüssel der Kesseldaten, er ist
        /// zugleich der MODULNAME der Ergebniszeile (<c>SimulationRunner</c>). Eine
        /// Umstellung auf IDs hätte die Modulnamen aller Kesselergebnisse verändert.
        ///
        /// Gefüllt, aber noch von keinem Rechenpfad ausgewertet — auch der zweikanalige
        /// Weg wertet in Etappe 4b nur Wärmepumpen-Senken aus. Vorbereitung für
        /// Senkenauswertung und Ladepriorität je Kessel (Paket 5).
        /// </summary>
        public List<int> spk_anlagen_ids = new List<int>();

        public int m_ID_Projekt = 0;
        public double Max_Waermebedarf;
        public float[] Waermebedarf = new float[8760];
        public float[] Restwaerme = new float[8760];
        public float[] Strombedarf_stuendlich = new float[8760];
        public float[] Stromverbrauch_stuendlich = new float[8760];
        public float[] Kesselleistung_stuendlich = new float[8760];
        public int Vorgabe_Betriebsbereitschaft;

        // Globale Ergebnisse
        public double Waermebedarf_gesamt = 0;
        public double Strombedarf_gesamt = 0;
        public double Maximale_Kesselleistung_Spk = 0;
        public double Stromverbrauch_Spk = 0;
        public double BruttoWaermeSpkErzeugung = 0;
        public double S_Waerme_spk = 0;
        public double Gasspitze_Spk = 0;

        // Globale Brennstoffzähler (in MWh)
        public double Gasverbrauch_SPK = 0;
        public double Oelverbrauch_SPK = 0;
        public double Rapsoelverbrauch_SPK = 0;
        public double Holzverbrauch_SPK = 0;
        public double Sonstigverbrauch_SPK = 0;
        public double Koks_SPK = 0;
        public double Kohle_SPK = 0;
        public double Pellets_SPK = 0;
        public double TierischeFette_SPK = 0;

        // Emissionen gesamt in kg
        public double Em_CO2_SPK = 0;
        public double Em_CO_SPK = 0;
        public double Em_SO2_SPK = 0;
        public double Em_NOX_SPK = 0;
        public double Em_Staub_SPK = 0;

        // Emissionen je Kessel
        public double[] CO2_SPK = new double[MAX_SPK];
        public double[] CO_SPK = new double[MAX_SPK];
        public double[] SO2_SPK = new double[MAX_SPK];
        public double[] NOX_SPK = new double[MAX_SPK];
        public double[] Staub_SPK = new double[MAX_SPK];

        // Kesselspezifische Arrays (Nutzwärme und Wirkungsgrade)
        public double[] s_waerme_Oel_Spk = new double[MAX_SPK];
        public double[] s_waerme_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Oel_Spk = new double[MAX_SPK];

        // Speicher für die korrekte Nutzungsgrad-Bilanz
        public double[] Kessel_Jahresnutzungsgrad_Spk = new double[MAX_SPK];
        private double[] Kessel_Verbrauch_MWh_Spk = new double[MAX_SPK];

        // Interne Kesselkonfigurationen
        double[] Betriebsbereitschaft_Verluste = new double[MAX_SPK];
        string[] Kessel_Name = new string[MAX_SPK];
        int[] Brennstoff_Betrieb_Spk = new int[MAX_SPK];
        int[] Brennstoff_Art = new int[MAX_SPK];
        double[] Kessel_Leistung_Spk = new double[MAX_SPK];

        public bool Berechnung(int ID_Projekt)
        {
            int Anzahl = 0;
            m_ID_Projekt = ID_Projekt;

            Init();

            // 1. Gesamten Wärmebedarf ermitteln (in MWh)
            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000;
            
            Strombedarf_gesamt = Strombedarf_stuendlich.Sum();
            HeizkesselCtrl heizkesselctrl = new HeizkesselCtrl();
            Anzahl = spk_list.Count;
            // B0-2: Kein Aliasing! "Restwaerme = Waermebedarf" band dasselbe Array-Objekt —
            // Init() des nächsten Laufs (Array.Clear) löschte damit den Projekt-Wärmebedarf.
            if (Anzahl == 0) { Restwaerme = (float[])Waermebedarf.Clone(); return true; }

            // B0-12: Alle Kessel-Arrays sind fest auf MAX_SPK dimensioniert — mehr Einträge
            // in spk_list liefen ungeprüft in die Einlese-Schleife und ab dem 11. Kessel
            // in einen Überlauf sämtlicher Kessel-Arrays.
            if (Anzahl > MAX_SPK)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Im Projekt sind " + Anzahl + " Heizkessel hinterlegt, die Simulation unterstützt maximal " + MAX_SPK + ".\n" +
                    "Es werden nur die ersten " + MAX_SPK + " Kessel berücksichtigt.");
                Anzahl = MAX_SPK;
            }

            // 2. Kesseldaten laden und Wirkungsgrade normieren
            //
            // Der Block steht seit der Paket-5-Nacharbeit (Befund N6) in einer eigenen
            // Methode: Der zweikanalige Weg braucht ihn Zeile für Zeile gleich, und zwei
            // Kopien wären die sichere Quelle künftiger Abweichungen — ein Fix am Altpfad
            // wirkte im neuen Weg nicht, und die Regressionssuite (Flag aus) fände das nie.
            // Die AUSGEFÜHRTEN Anweisungen und ihre Reihenfolge sind unverändert; der
            // bereits erzeugte HeizkesselCtrl wird hineingereicht, damit auch seine
            // Erzeugungsstelle bleibt, wo sie war.
            if (!Kesseldaten_Einlesen(heizkesselctrl, Anzahl, true)) return false;

            // 3. Die stündliche Simulation durchführen (Ermittelt Nutzwärme UND stündlichen Verbrauch)
            Heizkessel_Simulation(Waermebedarf, ref Gasspitze_Spk, s_waerme_Gas_Spk, s_waerme_Oel_Spk,
                Max_Waermebedarf, Anzahl, Kessel_Leistung_Spk, Kessel_Wirk_Gas_Spk, Kessel_Wirk_Oel_Spk,
                Betriebsbereitschaft_Verluste, Brennstoff_Betrieb_Spk, Kessel_Verbrauch_MWh_Spk);

            // 4./5. Verbrauch global bilanzieren, Emissionen und Jahresnutzungsgrad.
            //
            // Der Block steht seit Paket 5 in einer eigenen Methode: Der zweikanalige
            // Weg braucht ihn Zeile für Zeile gleich, und zwei Kopien wären die sichere
            // Quelle künftiger Abweichungen. Die AUSGEFÜHRTEN Anweisungen und ihre
            // Reihenfolge sind unverändert.
            Bilanz_und_Nutzungsgrad(Anzahl);

            return true;
        }

        /// <summary>
        /// Schritt 2 der Kesselbilanz: Kesseldaten, Emissionsfaktoren, Wirkungsgrade und
        /// Bereitschaftsverluste je Kessel einlesen. EINE Fassung für beide Rechenwege
        /// (Paket-5-Nacharbeit, Befund N6) — der zweikanalige Weg hatte den Block bis
        /// dahin kopiert.
        /// </summary>
        /// <param name="heizkesselctrl">bereits erzeugter Controller des Aufrufers</param>
        /// <param name="Anzahl">Zahl der zu lesenden Kessel (bereits auf MAX_SPK begrenzt)</param>
        /// <param name="mitDialog">
        /// true = Altpfad: fehlender Kessel wird als MessageBox gemeldet (unverändertes
        /// Verhalten). false = zweikanaliger Weg: die Meldung geht dialogfrei über
        /// <see cref="Fehlertext"/> (Konzept 13.4, Nacharbeit N10).
        /// </param>
        /// <returns>false = Abbruch (Kessel im Projekt nicht hinterlegt, B0-3).</returns>
        private bool Kesseldaten_Einlesen(HeizkesselCtrl heizkesselctrl, int Anzahl, bool mitDialog)
        {
            for (int i = 0; i < Anzahl; i++)
            {
                // B0-3: Projektfilter — gleicher Kesselname in mehreren Projekten lieferte
                // sonst die Daten des ersten Treffers (falsche Leistung/Brennstoff/Emissionen).
                heizkesselctrl.ReadAll("Bezeichner='" + spk_list[i].Replace("'", "''") + "' AND ID_Projekt=" + m_ID_Projekt);

                // B0-3: Mit dem Projektfilter kann die Treffermenge leer sein (Kessel aus
                // dem Projekt entfernt, Altdaten ohne ID_Projekt) — vorher lieferte der
                // erste Namenstreffer falsche, aber vorhandene Daten. Sauber abbrechen
                // statt items[0]-Zugriff mit ArgumentOutOfRangeException.
                if (heizkesselctrl.rows == 0)
                {
                    string text = "Der Heizkessel '" + spk_list[i] + "' ist im Projekt nicht hinterlegt.\n" +
                                  "Die Kessel-Simulation wird abgebrochen.";
                    if (mitDialog) System.Windows.Forms.MessageBox.Show(text);
                    else { Fehlertext = text; Console.WriteLine("Heizkessel: " + text.Replace("\n", " ")); }
                    return false;
                }

                Kessel_Name[i] = heizkesselctrl.items[0].Name;
                Kessel_Leistung_Spk[i] = heizkesselctrl.items[0].Ptherm;

                // Emissionen aus Brennstoff Tabelle laden
                DataTable dt = DataRepository.GetDataTable("select * from Tab_Brennstoff_Stamm where ID=?", new OleDbParameter("@s1", heizkesselctrl.items[0].Brennstoff));
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    CO2_SPK[i] = row["CO2"] != DBNull.Value ? Convert.ToDouble(row["CO2"]) : 0;
                    SO2_SPK[i] = row["SO2"] != DBNull.Value ? Convert.ToDouble(row["SO2"]) : 0;
                    NOX_SPK[i] = row["NOX"] != DBNull.Value ? Convert.ToDouble(row["NOX"]) : 0;
                    Staub_SPK[i] = row["Staub"] != DBNull.Value ? Convert.ToDouble(row["Staub"]) : 0;
                }

                // Wirkungsgrade einlesen
                Kessel_Wirk_Gas_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Gas;
                Kessel_Wirk_Oel_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Oel;

                // Absicherung Prozentwerte -> Faktor
                if (Kessel_Wirk_Gas_Spk[i] > 1.0) Kessel_Wirk_Gas_Spk[i] /= 100.0;
                if (Kessel_Wirk_Oel_Spk[i] > 1.0) Kessel_Wirk_Oel_Spk[i] /= 100.0;

                Brennstoff_Betrieb_Spk[i] = heizkesselctrl.items[0].Brennstoff;
                Brennstoff_Art[i] = Brennstoff_Betrieb_Spk[i];

                Betriebsbereitschaft_Verluste[i] = heizkesselctrl.items[0].Betriebsbereitschaftverlust;
                if (Betriebsbereitschaft_Verluste[i] > 1.0) Betriebsbereitschaft_Verluste[i] /= 100.0;

                Maximale_Kesselleistung_Spk += Kessel_Leistung_Spk[i];
            }

            return true;
        }

        /// <summary>
        /// Schritte 4 und 5 der Kesselbilanz: globale Brennstoffzähler, Emissionen und
        /// Jahresnutzungsgrad je Kessel. Beide Rechenwege (einkanalig und zweikanalig)
        /// benutzen sie unverändert.
        ///
        /// Voraussetzung: <c>s_waerme_Gas_Spk</c>, <c>s_waerme_Oel_Spk</c> und
        /// <c>Kessel_Verbrauch_MWh_Spk</c> stehen bereits in MWh, und <c>Gasspitze_Spk</c>
        /// ist aufsummiert.
        /// </summary>
        private void Bilanz_und_Nutzungsgrad(int Anzahl)
        {
            // 4. Verbrauch global bilanzieren und Emissionen berechnen
            for (int i = 0; i < Anzahl; i++)
            {
                double Kessel_Nutzkraft_Jahr = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i];
                S_Waerme_spk += Kessel_Nutzkraft_Jahr;

                double Kessel_Gesamtverbrauch_MWh = Kessel_Verbrauch_MWh_Spk[i];
                BruttoWaermeSpkErzeugung += Kessel_Gesamtverbrauch_MWh;

                // Den Verbrauch auf die globalen Brennstoffzähler buchen
                if (Brennstoff_Art[i] >= 1 && Brennstoff_Art[i] <= 5) Gasverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if ((Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <= 9) || (Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22)) Oelverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 10) Koks_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 11) Kohle_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 12) Holzverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 17) TierischeFette_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 13)
                {
                    // Elektrowärme / Wärmepumpe
                    Stromverbrauch_Spk += Kessel_Nutzkraft_Jahr;
                    // B0-2: auch hier kein Aliasing — sonst bleibt der Strom-Vektor ab dem
                    // zweiten Lauf dauerhaft an die Kessel-Ganglinie gebunden.
                    Stromverbrauch_stuendlich = (float[])Kesselleistung_stuendlich.Clone();
                }
                else if (Brennstoff_Art[i] == 15) Pellets_SPK += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 16) Rapsoelverbrauch_SPK += Kessel_Gesamtverbrauch_MWh;

                // Emissionen basierend auf dem echten stündlich ermittelten Gesamtverbrauch
                Em_CO2_SPK += Kessel_Gesamtverbrauch_MWh * CO2_SPK[i];
                Em_SO2_SPK += Kessel_Gesamtverbrauch_MWh * SO2_SPK[i];
                Em_NOX_SPK += Kessel_Gesamtverbrauch_MWh * NOX_SPK[i];
                Em_CO_SPK += Kessel_Gesamtverbrauch_MWh * CO_SPK[i];
                Em_Staub_SPK += Kessel_Gesamtverbrauch_MWh * Staub_SPK[i];
            }

            // Emissionen final herunterskalieren (in kg)
            Em_CO2_SPK /= 1000;
            Em_SO2_SPK /= 1000;
            Em_NOX_SPK /= 1000;
            Em_CO_SPK /= 1000;
            Em_Staub_SPK /= 1000;
            if (Gasverbrauch_SPK < 0.1) Gasspitze_Spk = 0;

            // 5. JAHRESNUTZUNGSGRAD PRO KESSEL SAUBER ERMITTELN
            for (int i = 0; i < Anzahl; i++)
            {
                double erzeugteWaerme = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]; // Nutzwärme (MWh)
                double verbrauchterBrennstoff = Kessel_Verbrauch_MWh_Spk[i];       // Gesamtverbrauch inkl. Stillstand (MWh)

                if (erzeugteWaerme > 0 && verbrauchterBrennstoff > 0)
                {
                    double ngrad = (erzeugteWaerme / verbrauchterBrennstoff) * 100;

                    // Plausibilitätsgrenzen nach DIN
                    if (ngrad > 110.0) ngrad = 108.0;
                    if (ngrad < 1.0) ngrad = 1.0;

                    Kessel_Jahresnutzungsgrad_Spk[i] = ngrad;
                }
                else
                {
                    Kessel_Jahresnutzungsgrad_Spk[i] = 0; // Kessel stand still
                }
            }
        }

        private void Heizkessel_Simulation(float[] Waermebedarf, ref double GasSpitze, double[] s_waerme_gas, double[] s_waerme_oel,
                double Max_Waermebedarf, int Anzahl, double[] Leistung, double[] Wirk_Gas, double[] Wirk_Oel,
                double[] BereitschaftsVerlustFaktor, int[] Brennstoff, double[] Kessel_Verbrauch_MWh_Spk)
        {
            double KesselLeistung;
            double Gasleistung;
            // B0-12: war double[5] — ab dem 6. Kessel IndexOutOfRangeException bei der
            // Gasspitzenberechnung. Jetzt gleiche Größe wie alle übrigen Kessel-Arrays.
            double[] Gasspitze_Kessel = new double[MAX_SPK];
            double waerme;

            Max_Waermebedarf = 0;
            GasSpitze = 0;
            for (int i = 0; i < MAX_SPK; i++) { Gasspitze_Kessel[i] = 0; }

            // Stündliche Lastverteilung (Einheit: kW)
            for (int Stunde = 0; Stunde < 8760; Stunde++)
            {
                waerme = Waermebedarf[Stunde];

                if (Max_Waermebedarf < waerme) Max_Waermebedarf = waerme;

                for (int Kessel = 0; Kessel < Anzahl; Kessel++)
                {
                    // 1. Nutzwärme-Zuweisung für diese Stunde
                    if (waerme > Leistung[Kessel])
                    {
                        KesselLeistung = Leistung[Kessel];
                        waerme -= Leistung[Kessel];
                    }
                    else
                    {
                        KesselLeistung = waerme;
                        waerme = 0;
                    }

                    // Basis-Wirkungsgrad bestimmen
                    double wirk = (Brennstoff[Kessel] >= 6 && Brennstoff[Kessel] <= 9 || Brennstoff[Kessel] >= 18 && Brennstoff[Kessel] <= 22)
                        ? Wirk_Oel[Kessel]
                        : Wirk_Gas[Kessel];
                    if (wirk <= 0) wirk = 0.90; // Fallback

                    double stündlicherBrennstoffverbrauchKW = 0;

                    // 2. Stündliche energetische Bilanzierung (Ansatz A)
                    if (KesselLeistung > 0)
                    {
                        // Kessel läuft -> Verbrauch über Wirkungsgrad (in dieser Stunde kein Stillstandsverlust)
                        stündlicherBrennstoffverbrauchKW = KesselLeistung / wirk;

                        // Nutzwärme-Zähler aufaddieren (wird am Ende in MWh umgerechnet)
                        if (Brennstoff[Kessel] >= 6 && Brennstoff[Kessel] <= 9 || Brennstoff[Kessel] >= 18 && Brennstoff[Kessel] <= 22)
                        {
                            s_waerme_oel[Kessel] += KesselLeistung;
                        }
                        else
                        {
                            s_waerme_gas[Kessel] += KesselLeistung;

                            // Gasspitzenberechnung
                            Gasleistung = KesselLeistung / wirk;
                            if (Gasspitze_Kessel[Kessel] < Gasleistung) Gasspitze_Kessel[Kessel] = Gasleistung;
                        }
                    }
                    else
                    {
                        // Kessel steht in dieser Stunde still -> Er verliert Wärme durch Auskühlung (Bereitschaftsverlust)
                        // Verlust = Faktor * Nennleistung (kW) * 1 Stunde
                        stündlicherBrennstoffverbrauchKW = BereitschaftsVerlustFaktor[Kessel] * Leistung[Kessel];
                    }

                    // Stündlichen Verbrauch direkt auf den Jahreszähler des Kessels addieren (von kW in kWh)
                    Kessel_Verbrauch_MWh_Spk[Kessel] += stündlicherBrennstoffverbrauchKW;

                    Kesselleistung_stuendlich[Stunde] += (float)KesselLeistung;
                    Restwaerme[Stunde] = (float)waerme;
                }
            }

            // Umrechnung der Jahressummen von kWh in MWh (/ 1000)
            for (int i = 0; i < Anzahl; i++)
            {
                s_waerme_gas[i] /= 1000;
                s_waerme_oel[i] /= 1000;
                Kessel_Verbrauch_MWh_Spk[i] /= 1000; // Verbrauch ebenfalls von kWh nach MWh wandeln
                GasSpitze += Gasspitze_Kessel[i];
            }
        }

        // ===================================================================
        // Zweikanaliger Weg (Paket 5 - Konzept 6.5, erster Punkt)
        // ===================================================================

        /// <summary>Anzahl der Kessel des zweikanaligen Wegs (nach der MAX_SPK-Grenze).</summary>
        private int _anzahlZweikanalig = 0;

        /// <summary>Nutzwärme, die ein Kessel in der LAUFENDEN Stunde abgegeben hat [kWh].</summary>
        private readonly double[] _kesselStunde = new double[MAX_SPK];

        /// <summary>Noch nicht vergebene Leistung eines Kessels in der laufenden Stunde [kW].</summary>
        private readonly double[] _restLeistung = new double[MAX_SPK];

        /// <summary>Gasspitze je Kessel [kW] (zweikanaliger Weg).</summary>
        private readonly double[] _gasspitzeKessel = new double[MAX_SPK];

        /// <summary>Senkenzuordnung je Kessel, indexgleich zu <see cref="spk_list"/>.</summary>
        private readonly List<Senkenzuordnung> _kesselSenke = new List<Senkenzuordnung>();

        /// <summary>
        /// In Pufferspeicher geladene Kesselwärme je Stunde [kWh] (zweikanaliger Weg,
        /// Nacharbeit N1).
        ///
        /// Sie ist ein TEIL der Nutzwärme (<see cref="S_Waerme_spk"/>): Dort steht die
        /// gesamte abgegebene Wärme, also Direktdeckung PLUS Speicherladung — und genau
        /// so gehört sie dorthin, denn der Brennstoffverbrauch und der Jahresnutzungsgrad
        /// beziehen sich auf sie. Getrennt geführt wird die Ladung, weil die
        /// Ergebnispersistenz Restbedarf und Deckungsgrad aus der DIREKTDECKUNG bilden
        /// muss — sonst wird der Restbedarf negativ und die Summe der Deckungen
        /// überschreitet 100 % (dieselbe Mitkorrektur wie bei der Solarthermie,
        /// Konzept 6.4).
        /// </summary>
        public double[] Speicherladung_stuendlich = new double[8760];

        /// <summary>Jahressumme der Speicherladung [kWh]; im Altpfad immer exakt 0.</summary>
        public double Speicherladung_gesamt = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        ///
        /// Gefüllt von <see cref="Kaskadenschleife"/>; im Altpfad und ohne Puffer-Senke
        /// exakt 0. Zusammen mit der Direktdeckung ergibt sich daraus der EIGENANTEIL des
        /// Kessels an der Bedarfsdeckung — die Größe, die
        /// <c>Tab_ErgebnisHeizkessel.Waermebedarfsdeckung</c> ausweist.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        /// <summary>
        /// Fehlertext des zweikanaligen Wegs (Konzept 13.4: die Engine bleibt dialogfrei).
        /// Der Altpfad zeigt an denselben Stellen eine MessageBox; im zweikanaligen Weg
        /// geht die Meldung über den Fehlerkanal Richtung
        /// <c>SimulationRunner.SimuliereUndSpeichere(… out fehler)</c> (Nacharbeit N10).
        /// </summary>
        public string Fehlertext = "";

        /// <summary>Anzahl der Kessel, die im zweikanaligen Weg rechnen.</summary>
        public int KesselAnzahl { get { return _anzahlZweikanalig; } }

        /// <summary>Senkenzuordnung eines Kessels; <c>null</c> außerhalb des Indexbereichs.</summary>
        public Senkenzuordnung KesselSenke(int index)
        {
            if (index < 0 || index >= _kesselSenke.Count) return null;
            return _kesselSenke[index];
        }

        /// <summary>
        /// Baut die Kessel des zweikanaligen Wegs auf — Schritte 1 und 2 aus
        /// <see cref="Berechnung"/>, Zeile für Zeile dieselben Abfragen und dieselben
        /// Absicherungen (B0-3, B0-12).
        /// </summary>
        /// <returns>false = Abbruch (Kessel nicht im Projekt hinterlegt).</returns>
        public bool Vorbereiten_Zweikanalig(int ID_Projekt, List<Senkenzuordnung> senken)
        {
            m_ID_Projekt = ID_Projekt;

            Init();
            Fehlertext = "";
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);
            Array.Clear(_kesselStunde, 0, _kesselStunde.Length);
            Array.Clear(_restLeistung, 0, _restLeistung.Length);
            Array.Clear(_gasspitzeKessel, 0, _gasspitzeKessel.Length);
            _kesselSenke.Clear();

            Waermebedarf_gesamt = 0;
            Max_Waermebedarf = 0;
            Strombedarf_gesamt = Strombedarf_stuendlich.Sum();

            HeizkesselCtrl heizkesselctrl = new HeizkesselCtrl();
            int Anzahl = spk_list.Count;

            // B0-12, dialogfrei (Nacharbeit N10): Der Altpfad zeigt hier eine MessageBox
            // und rechnet mit den ersten MAX_SPK Kesseln weiter. Der zweikanalige Weg
            // rechnet ebenso weiter, meldet aber auf die Konsole statt in einen Dialog —
            // die Engine bleibt dialogfrei (Konzept 13.4), und das VERHALTEN ist dasselbe.
            if (Anzahl > MAX_SPK)
            {
                Console.WriteLine("Heizkessel: Im Projekt sind " + Anzahl + " Kessel hinterlegt, " +
                                  "die Simulation unterstützt maximal " + MAX_SPK +
                                  ". Es werden nur die ersten " + MAX_SPK + " Kessel berücksichtigt.");
                Anzahl = MAX_SPK;
            }

            // Schritt 2 aus Berechnung() — EINE Fassung für beide Wege (Nacharbeit N6).
            if (!Kesseldaten_Einlesen(heizkesselctrl, Anzahl, false)) return false;

            // Senkenzuordnung je Kessel: keine Physik, sondern die Konfiguration des
            // zweikanaligen Wegs — deshalb hier und nicht im gemeinsamen Einlesen.
            for (int i = 0; i < Anzahl; i++)
            {
                int idAnlage = (i < spk_anlagen_ids.Count) ? spk_anlagen_ids[i] : 0;
                _kesselSenke.Add(SenkeZuAnlage(senken, idAnlage));
            }

            _anzahlZweikanalig = Anzahl;
            return true;
        }

        /// <summary>
        /// Senkenzuordnung einer Anlage; ohne Zeile gilt die Vorbelegung Heizkreis/Beides
        /// (Konzept 4.6, erste Zeile der Tabelle).
        /// </summary>
        private static Senkenzuordnung SenkeZuAnlage(List<Senkenzuordnung> senken, int idAnlage)
        {
            if (senken != null && idAnlage > 0)
                foreach (Senkenzuordnung z in senken)
                    if (z != null && z.AnlagenID == idAnlage) return z;

            return new Senkenzuordnung { AnlagenID = idAnlage };
        }

        /// <summary>Stundenbeginn: jeder Kessel hat seine volle Nennleistung zur Verfügung.</summary>
        public void Stunde_Start(int stunde)
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                _kesselStunde[i] = 0;
                _restLeistung[i] = Kessel_Leistung_Spk[i];
            }
        }

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für die Heizkessel: die
        /// ZWEIKANALIGE Fassung der Lastverteilung aus <see cref="Heizkessel_Simulation"/>.
        ///
        /// Konzept 6.5 beschreibt sie als „zweiten Schleifendurchlauf mit erhaltenem
        /// Zwischenzustand". Umgesetzt ist genau das, nur ohne zweiten Durchlauf: Der
        /// Kessel bedient in EINER Stunde erst den einen, dann den anderen Kanal — bei
        /// <c>WS_Typ = Beides</c> mit Warmwasservorrang, wie überall in dieser Engine
        /// (<c>SenkeAbziehen</c>) —, und die abgegebene Nutzwärme sammelt sich in
        /// <see cref="_kesselStunde"/>. Der Zwischenzustand ist damit erhalten, und die
        /// BEREITSCHAFTSVERLUSTE fallen nur EINMAL je Stunde und Kessel an: Sie werden
        /// nicht hier, sondern in <see cref="Stunde_Abschluss"/> gebucht, und zwar an
        /// genau einer Stelle für beide Kanäle und die Speicherladung zusammen.
        ///
        /// Ein Kessel mit Puffer-Hauptsenke deckt hier NICHTS — er lädt ausschließlich
        /// (Phase C), und damit gilt derselbe Doppelzählungs-Freibeweis wie bei der
        /// Wärmepumpe.
        /// </summary>
        public void Stunde_Bedarf(int stunde, ref double rest_heiz, ref double rest_ww)
        {
            double eingang = rest_heiz + rest_ww;
            if (stunde >= 0 && stunde < 8760) Waermebedarf[stunde] = (float)eingang;
            if (Max_Waermebedarf < eingang) Max_Waermebedarf = eingang;

            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                if (_restLeistung[i] <= 0) continue;

                Senkenzuordnung z = _kesselSenke[i];
                if (z != null && z.Haupt != Senke.Heizkreis) continue;

                string wsTyp = (z != null) ? z.WSTyp : WaermequelleClass.SENKE_BEIDES;
                double verfuegbar;
                if (wsTyp == WaermequelleClass.SENKE_WARMWASSER) verfuegbar = rest_ww;
                else if (wsTyp == WaermequelleClass.SENKE_HEIZUNG) verfuegbar = rest_heiz;
                else verfuegbar = rest_heiz + rest_ww;

                if (verfuegbar <= 0) continue;

                double menge = Math.Min(_restLeistung[i], verfuegbar);
                if (menge <= 0) continue;

                Kaskadenschleife.SenkeAbziehen(wsTyp, menge, ref rest_ww, ref rest_heiz);

                _restLeistung[i] -= menge;
                _kesselStunde[i] += menge;
            }

            if (stunde >= 0 && stunde < 8760) Restwaerme[stunde] = (float)(rest_heiz + rest_ww);
        }

        /// <summary>
        /// Phasen C/D für EINEN Ladeauftrag (Konzept 6.5: „Senkenauswertung je Kessel —
        /// Puffer laden bis Abschaltschwelle").
        ///
        /// Die Abschaltschwelle steckt in <see cref="Ladeauftrag.ObergrenzeStunde"/>: Sie
        /// ist nach der Auflösungsregel 3.4 bereits bestimmt — eigene Ladegrenze, sonst
        /// <c>Schwelle_Aus</c> für die vorrangige und <c>Schwelle_Aus_Nachrang</c> für
        /// nachrangige Anlagen. Der Kessel ist mit Vorgaberang 40 der letzte Lader; wo
        /// eine Solar-Reservezone gepflegt ist, lädt er also nur bis dorthin.
        ///
        /// KEIN <c>SenkeAbziehen</c>; Bilanzraum und Durchsatzbudget wie in Paket 4.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss, double[] absehbar)
        {
            if (a == null || a.Speicher == null) return 0;

            int i = a.Modulindex;
            if (i < 0 || i >= _anzahlZweikanalig) return 0;
            if (_restLeistung[i] <= 0) return 0;

            SimulationPufferspeicher sp = a.Speicher;

            int kanal = sp.IstBrauchwasserkanal ? 1 : 0;
            double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
            double durchlass = Math.Min(absehbar[kanal] > 0 ? absehbar[kanal] : 0, sp.Entnahmefaehigkeit());
            if (ladefaehig + durchlass <= 0) return 0;

            double menge = Math.Min(_restLeistung[i], ladefaehig + durchlass);
            if (menge <= 0) return 0;

            double ladung = sp.Laden(menge, stunde, durchlass);
            if (ladung <= 0) return 0;

            double genutzterDurchlass = ladung - ladefaehig;
            if (genutzterDurchlass > 0)
            {
                absehbar[kanal] -= genutzterDurchlass;
                if (absehbar[kanal] < 0) absehbar[kanal] = 0;
            }

            _restLeistung[i] -= ladung;
            _kesselStunde[i] += ladung;

            // N1 (Paket-5-Nacharbeit): Die Speicherladung getrennt mitführen. Sie bleibt
            // Teil der Nutzwärme (der Brennstoff dafür ist geflossen), darf aber nicht als
            // BEDARFSDECKUNG gelten — sonst meldet Tab_ErgebnisHeizkessel einen negativen
            // Restwärmebedarf und eine Deckungssumme über 100 % (gemessen an 1018/1023).
            Speicherladung_gesamt += ladung;
            if (stunde >= 0 && stunde < 8760) Speicherladung_stuendlich[stunde] += ladung;

            return ladung;
        }

        /// <summary>
        /// Brennstoffbilanz der Stunde — GENAU EINMAL je Stunde und Kessel (Konzept 6.5).
        ///
        /// Das ist die zentrale Bedingung der zweikanaligen Umstellung: Läuft der Kessel,
        /// folgt sein Verbrauch dem Wirkungsgrad; steht er, wird ihm der anteilige
        /// BEREITSCHAFTSVERLUST als Verbrauch aufgeschlagen. Würde diese Entscheidung je
        /// Kanal getroffen, fiele der Stillstandsverlust in einer Stunde zweimal an — der
        /// Jahresnutzungsgrad (Schritt 5) kippte entsprechend.
        ///
        /// Aufgerufen wird die Methode in Phase G, also nach Bedarfsdeckung, Ladephase und
        /// Nachentladung: Erst dann steht fest, was der Kessel in dieser Stunde insgesamt
        /// abgegeben hat.
        /// </summary>
        public void Stunde_Abschluss(int stunde)
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                double KesselLeistung = _kesselStunde[i];

                bool oel = Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <= 9 ||
                           Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22;

                double wirk = oel ? Kessel_Wirk_Oel_Spk[i] : Kessel_Wirk_Gas_Spk[i];
                if (wirk <= 0) wirk = 0.90; // Fallback

                double stuendlicherBrennstoffverbrauchKW;

                if (KesselLeistung > 0)
                {
                    // Kessel läuft -> Verbrauch über Wirkungsgrad (in dieser Stunde kein Stillstandsverlust)
                    stuendlicherBrennstoffverbrauchKW = KesselLeistung / wirk;

                    if (oel)
                    {
                        s_waerme_Oel_Spk[i] += KesselLeistung;
                    }
                    else
                    {
                        s_waerme_Gas_Spk[i] += KesselLeistung;

                        double Gasleistung = KesselLeistung / wirk;
                        if (_gasspitzeKessel[i] < Gasleistung) _gasspitzeKessel[i] = Gasleistung;
                    }
                }
                else
                {
                    // Kessel steht in dieser Stunde still -> Bereitschaftsverlust, EINMAL.
                    stuendlicherBrennstoffverbrauchKW = Betriebsbereitschaft_Verluste[i] * Kessel_Leistung_Spk[i];
                }

                Kessel_Verbrauch_MWh_Spk[i] += stuendlicherBrennstoffverbrauchKW;

                if (stunde >= 0 && stunde < 8760)
                    Kesselleistung_stuendlich[stunde] += (float)KesselLeistung;
            }
        }

        /// <summary>Jahressummen, Emissionen und Jahresnutzungsgrad des zweikanaligen Wegs.</summary>
        public void Abschluss_Zweikanalig()
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                s_waerme_Gas_Spk[i] /= 1000;
                s_waerme_Oel_Spk[i] /= 1000;
                Kessel_Verbrauch_MWh_Spk[i] /= 1000;
                Gasspitze_Spk += _gasspitzeKessel[i];
            }

            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf, value => Waermebedarf_gesamt += value);
            Waermebedarf_gesamt /= 1000;

            Bilanz_und_Nutzungsgrad(_anzahlZweikanalig);
        }

        /// <summary>
        /// Zweikanalige Stufe OHNE Speicherbeteiligung: dieselben Stundenschritte in einer
        /// eigenen Jahresschleife an der Kaskadenposition des Heizkessels.
        ///
        /// Der Weg für Projekte, in denen kein Kessel eine Puffer-Senke trägt. Ohne
        /// Speicher haben die Phasen A, C, D und E für diese Stufe keinen Inhalt; Phase G
        /// beschränkt sich auf die Brennstoffbilanz der Stunde. Gegenüber dem Altpfad
        /// ändert sich allein die Kanalführung — die je Stunde und Kessel abgegebene
        /// Nutzwärme, der Brennstoffverbrauch und die Restwärme sind dieselben Zahlen.
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

                Stunde_Start(stunde);
                Stunde_Bedarf(stunde, ref rest_heiz, ref rest_ww);
                Stunde_Abschluss(stunde);

                kanaele.Heiz[stunde] = (float)rest_heiz;
                kanaele.WW[stunde] = (float)rest_ww;
            }

            Abschluss_Zweikanalig();
            return true;
        }

        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays müssen die gleiche Länge aufweisen.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++) { result[i] = array1[i] + array2[i]; }
            return result;
        }

        public void Init()
        {
            // N8 (Paket-5-Nacharbeit): Die Kesselzahl des zweikanaligen Wegs gehört zum
            // Zustand dieses Moduls und muss deshalb HIER zurückgesetzt werden. Bisher
            // stand sie nur am Ende von Vorbereiten_Zweikanalig - bricht das mittendrin
            // ab (Kessel nicht im Projekt), stünde der Wert des Vorlaufs neben einer
            // bereits geleerten _kesselSenke, und die Stundenschritte liefen über
            // Kessel, die es in diesem Lauf nicht gibt.
            _anzahlZweikanalig = 0;

            // Zweikanaliger Weg (Paket 5 / Nacharbeit N1, N2): Im Altpfad bleiben diese
            // Größen auf 0, damit die Ergebnisbildung in SimulationRunner dort
            // nachweislich bitgleich der bisherigen ist.
            Array.Clear(Speicherladung_stuendlich, 0, Speicherladung_stuendlich.Length);
            Speicherladung_gesamt = 0;
            Speicherentladung_Anteil = 0;

            Maximale_Kesselleistung_Spk = 0;
            Stromverbrauch_Spk = 0;

            for (int j = 0; j < MAX_SPK; j++)
            {
                s_waerme_Gas_Spk[j] = 0;
                s_waerme_Oel_Spk[j] = 0;
                Kessel_Wirk_Gas_Spk[j] = 0;
                Kessel_Wirk_Oel_Spk[j] = 0;
                Betriebsbereitschaft_Verluste[j] = 0;
                Kessel_Name[j] = "";
                Brennstoff_Betrieb_Spk[j] = 0;
                Kessel_Leistung_Spk[j] = 0;
                Kessel_Verbrauch_MWh_Spk[j] = 0;
                Kessel_Jahresnutzungsgrad_Spk[j] = 0;

                CO2_SPK[j] = 0;
                CO_SPK[j] = 0;
                CO2_SPK[j] = 0;
                SO2_SPK[j] = 0;
                NOX_SPK[j] = 0;
                Staub_SPK[j] = 0;
            }

            BruttoWaermeSpkErzeugung = 0;
            S_Waerme_spk = 0;
            Gasverbrauch_SPK = 0;
            Oelverbrauch_SPK = 0;
            Rapsoelverbrauch_SPK = 0;
            Holzverbrauch_SPK = 0;
            Sonstigverbrauch_SPK = 0;
            Stromverbrauch_Spk = 0;
            Kohle_SPK = 0;
            Koks_SPK = 0;
            Pellets_SPK = 0;
            TierischeFette_SPK = 0;

            Em_CO2_SPK = 0;
            Em_CO_SPK = 0;
            Em_SO2_SPK = 0;
            Em_NOX_SPK = 0;
            Em_Staub_SPK = 0;

            Gasspitze_Spk = 0;

            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Stromverbrauch_stuendlich, 0, Stromverbrauch_stuendlich.Length);
            Array.Clear(Kesselleistung_stuendlich, 0, Kesselleistung_stuendlich.Length);
        }
    }
}
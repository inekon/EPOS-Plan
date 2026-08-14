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
                    System.Windows.Forms.MessageBox.Show(
                        "Der Heizkessel '" + spk_list[i] + "' ist im Projekt nicht hinterlegt.\n" +
                        "Die Kessel-Simulation wird abgebrochen.");
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

            // 3. Die stündliche Simulation durchführen (Ermittelt Nutzwärme UND stündlichen Verbrauch)
            Heizkessel_Simulation(Waermebedarf, ref Gasspitze_Spk, s_waerme_Gas_Spk, s_waerme_Oel_Spk,
                Max_Waermebedarf, Anzahl, Kessel_Leistung_Spk, Kessel_Wirk_Gas_Spk, Kessel_Wirk_Oel_Spk,
                Betriebsbereitschaft_Verluste, Brennstoff_Betrieb_Spk, Kessel_Verbrauch_MWh_Spk);

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

            return true;
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
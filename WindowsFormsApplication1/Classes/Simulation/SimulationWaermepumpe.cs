using System;
using System.Collections.Generic;
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

        public double[] WP_Strombedarf_stuendlich = new double[8760];
        public float[] WP_Waermeproduktion_stuendlich = new float[8760];
        public float[] WP_Waermeproduktion_stuendlich_sortiert = new float[8760];
        public float[] Heizstab_stuendlich = new float[8760];

        public double WP_Strombedarf_gesamt;
        public double WP_Waermeproduktion_gesamt;
        public double Heizstab_gesamt;
        public double WP_Laufzeit;

        public double[] Modul_WP_Strombedarf = new double[MAX_WP];
        public double[] Modul_WP_Waermeproduktion = new double[MAX_WP];
        public double[] Modul_Heizstab = new double[MAX_WP];
        public double[] Modul_WP_Laufzeit = new double[MAX_WP];

        private List<WErzeugerModel> wp_model = new List<WErzeugerModel>();
        private List<_Kenndaten> wp_kenndaten = new List<_Kenndaten>();
        private string[] WP_Betriebsart = new string[MAX_WP];
        private int[] WP_Heizung = new int[MAX_WP];

        public double Bivalenzpunkt = -100;

        public bool Mit_Heizstab = false;
        public double Volumen_Pufferspeicher;
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

            for (int i = 0; i < MAX_WP; i++)
            {
                Modul_WP_Strombedarf[i] =0;
                Modul_WP_Waermeproduktion[i] = 0;
                Modul_Heizstab[i] = 0;
                Modul_WP_Laufzeit[i] = 0;
                WP_Modul[i] = "";
            }


            wp_model.Clear();
            for (int i = 0; i < wp_list.Count; i++)
            {
                wp.ReadAllFilter("ID=" + wp_list[i]);
                WErzeugerModel model = wp.items[0];
                wp_model.Add(model);
                WP_Betriebsart[i] = model.Betriebsart;
                WP_Modul[i] = model.Bezeichner; 

                if (model.Volumen > 0) Volumen_Pufferspeicher = model.Volumen; 

                rs.Open("select * from Tab_WP where ID_WP=" + model.ID_WP);
                if (rs.Next())
                {
                    WP_Heizung[i] = (int)rs.Read("Heizung");
                }
                rs.Close();

                _Kenndaten item = new _Kenndaten();
                item.Vorlauf = model.Vorlauf;
                item.ID_WP = model.ID_WP;

                RecordSet rsAnz = new RecordSet();
                rsAnz.Open("SELECT Count(*) FROM Tab_Kenndaten WHERE Tab_Kenndaten.ID_WP=" + model.ID_WP + " AND Tab_Kenndaten.Vorlauf=" + model.Vorlauf);
                rsAnz.Next();
                int anz = (int)rsAnz.Read(0);
                rsAnz.Close();

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

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                Rest_waerme = Waermebedarf_stuendlich[stunde];

                Rest_Speicher = KapazitaetPendelspeicher - Solar_Speicher - Speicher;

                WP_Strombedarf_stuendlich[stunde] = 0;
                WP_Waermeproduktion_stuendlich[stunde] = 0;
                waermerestbedarf_stuendlich[stunde] = 0;
                Heizstab_stuendlich[stunde] = 0;

                double[] result = new double[4] { 0, 0, 0, 0 };

                for (int index = 0; index < wp_model.Count; index++)
                {
                    WErzeugerModel model = wp_model[index];
                    _Kenndaten kenndaten = wp_kenndaten[index];

                    result = berechne_wptherm(Temperatur[stunde], model, kenndaten );
                    if(result[STATUS] == 0) return false;

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
                    if (result[PTHERM] < Rest_waerme)
                    {
                        if (Temperatur[stunde] > Bivalenzpunkt) Bivalenzpunkt = Temperatur[stunde];
                    }

                    // Leistungsdaten der WP auswerten  
                    if (result[PTHERM] < Rest_waerme)
                    {
                        WP_Waermeproduktion_stuendlich[stunde] = WP_Waermeproduktion_stuendlich[stunde] + (float)result[PTHERM];
                        WP_Waermeproduktion_gesamt += result[PTHERM];
                        WP_Strombedarf_stuendlich[stunde] = WP_Strombedarf_stuendlich[stunde] + result[PEL];
                        WP_Strombedarf_gesamt += result[PEL];
                        Modul_WP_Waermeproduktion[index] += (float)result[PTHERM];
                        Modul_WP_Strombedarf[index] += result[PEL];

                        WP_Laufzeit = WP_Laufzeit + 1;
                        Modul_WP_Laufzeit[index] += 1;
                        
                        Rest_waerme = Rest_waerme - result[PTHERM];
                        if (Rest_waerme < 0) Rest_waerme = 0;
                    }
                    else
                    {
                        WP_Waermeproduktion_stuendlich[stunde] = WP_Waermeproduktion_stuendlich[stunde] + (float)Rest_waerme;
                        WP_Waermeproduktion_gesamt += Rest_waerme;
                        WP_Strombedarf_stuendlich[stunde] = WP_Strombedarf_stuendlich[stunde] + Rest_waerme / result[COP];
                        WP_Strombedarf_gesamt += Rest_waerme / result[COP];
                        Modul_WP_Waermeproduktion[index] += (float)Rest_waerme;
                        Modul_WP_Strombedarf[index] += Rest_waerme / result[COP];

                        WP_Laufzeit = WP_Laufzeit + (Rest_waerme / (float)result[PTHERM]);
                        Modul_WP_Laufzeit[index] += (Rest_waerme / (float)result[PTHERM]);
                        
                        Rest_waerme = 0;
                    }

                } // end alle WP    

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

                // Wärmerestbedarf speichern    
                waermerestbedarf_stuendlich[stunde] = waermerestbedarf_stuendlich[stunde] + (float)Rest_waerme;
            
            } // end alle Stunden

            // absteigend sortieren
            com.I_heapsort(WP_Waermeproduktion_stuendlich, WP_Waermeproduktion_stuendlich_sortiert);

            // Wärmebedarf gesamt und Restwärme berechnen in kWh
            Waermebedarf_gesamt = 0;
            Array.ForEach(Waermebedarf_stuendlich, value => Waermebedarf_gesamt += value);
            waermerestbedarf_gesamt = Waermebedarf_gesamt - WP_Waermeproduktion_gesamt - Heizstab_gesamt;

            Cursor.Current = Cursors.Default;
            return true;
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class SimulationStrombedarf
    {
        public int m_ID_Projekt = 0;

        public int[] mo_anfang = new int[12];
        public int[] mo_ende = new int[12];
        public float[] monats_werte = new float[12];
        public float[] wochen_werte = new float[168];
        public float[] prozesswerte = new float[8760 *4 ];
        public float[] temp = new float[8760 * 4];

        public float[] Strombedarf_viertelStundenwerte = new float[8760 * 4];
        private float[] Strombedarf_sortiert = new float[8760 * 4];
        public float[] Stromganglinie = new float[8760 *4];
        public float[] Strombedarf_monat = new float[12];

        public float Strombedarf_Gebaeude_gesamt;
        public float Stromganglinie_gesamt;
        public float Strombedarf_gesamt;
        public float Strombedarf_Max;

        public float[] Dauerlinie = new float[8760 * 4];
        public float[] Dauerlinie_nicht_sortiert = new float[8760 * 4];

       // public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();

        public SimulationStrombedarf()
        {
            Classes.Simulation.Init init = new Classes.Simulation.Init();
            init.Monatswerte_berechnen(mo_anfang, mo_ende);
        }

        public void Berechnung(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();
            int index = 0;
            double wert = 0;
            int Interval = 0;

            m_ID_Projekt = ID_Projekt;

            Strombedarf_Gebaeude_gesamt = 0;
            Stromganglinie_gesamt = 0;
            Strombedarf_gesamt = 0;
            Strombedarf_Max = 0;

            Array.Clear(Strombedarf_viertelStundenwerte, 0, Strombedarf_viertelStundenwerte.Length);
            Array.Clear(Strombedarf_sortiert, 0, Strombedarf_sortiert.Length);
            Array.Clear(prozesswerte, 0, prozesswerte.Length);
            Array.Clear(temp, 0, temp.Length);
            Array.Clear(Stromganglinie, 0, Stromganglinie.Length);
            Array.Clear(Dauerlinie, 0, Dauerlinie.Length);
            Array.Clear(Dauerlinie_nicht_sortiert, 0, Dauerlinie_nicht_sortiert.Length);

            // ***********************************************************************
            // Stromprofile (Stundenwerte)
            // ***********************************************************************
            prozesswerte = Stromprofil_Strombedarf_berechnen();
            if(prozesswerte == null)
            {
                MessageBox.Show("Fehler bei der Berechnung der Stromprofile!");
                return;
            }

            // auf 1/4 Stundenwerte umrechnen
            prozesswerte = Stundenwerte_zu_viertelstunden(prozesswerte);

            Strombedarf_viertelStundenwerte = (float[])prozesswerte.Clone();

            Strombedarf_Gebaeude_gesamt += prozesswerte.Sum() / 4000;

            // ***********************************************************************
            // Stromganglinien Stundenwerte bzw. Viertelstundenwerte gemäß Interval
            // 1=Stundenwerte, 4=Viertelstundenwerte
            // ***********************************************************************
            Z_ProjektStromganglinieCtrl waectrl = new Z_ProjektStromganglinieCtrl();
            waectrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + m_ID_Projekt);
            Stromganglinie_gesamt = 0;
            
            for (int n = 0; n < waectrl.rows; n++)
            {
                rs.Open("select * from Abfrage_ProjektStromGanglinie where Tab_Stromganglinie.ID=" + waectrl.items[n].m_ID_Stromganglinie + " order by Tab_StromganglinieDaten.ID");

                index = 0;
                wert = 0;

                while (rs.Next())
                {
                    Interval = (int)rs.Read("Zeitinterval");
                    wert = (double)rs.Read("Wert");
                    Stromganglinie[index++] = (float)wert;
                }
                rs.Close();
                
                // Ganglinie mit Stundenwerte aufspreitzen auf 1/4 Stunden
                if (Interval == 1) 
                    Stromganglinie = Stundenwerte_zu_viertelstunden(Stromganglinie);

                for (int i = 0; i < Strombedarf_viertelStundenwerte.Length && i < Stromganglinie.Length; i++)
                    Strombedarf_viertelStundenwerte[i] += Stromganglinie[i];

                Stromganglinie_gesamt += Stromganglinie.Sum();
            }

            Stromganglinie_gesamt = Stromganglinie_gesamt / 4000f; // MWh
            Strombedarf_monat = MonatsSumme_MW(Strombedarf_viertelStundenwerte, mo_anfang, mo_ende); // in MWh
            Strombedarf_Max = Maximaler_Strombedarf(Strombedarf_viertelStundenwerte); // in kWh
            Strombedarf_gesamt = Strombedarf_viertelStundenwerte.Sum() / 4000f; // in MWh 
            Strombedarf_sortiert = (float[])Strombedarf_viertelStundenwerte.Clone();
            Dauerlinie_nicht_sortiert = Strombedarf_viertelStundenwerte;
            Strombedarf_sortiert = NormVector(Strombedarf_sortiert, Strombedarf_Max);
            Dauerlinie_nicht_sortiert = NormVector(Dauerlinie_nicht_sortiert, Strombedarf_Max);
            Dauerlinie = SortVector(Strombedarf_sortiert);

            Array.Reverse(Dauerlinie);
        }

        public float[] Stromprofil_Strombedarf_berechnen(List<string> list = null)
        {
            RecordSet rs = new RecordSet();
            RecordSet rs_pwtyp = new RecordSet();
            List<string> stromprofil_list = new List<string>();
            float[] temp = new float[8760];

            try
            {
                if (list == null)
                {
                    // Abfrage über Projekt Stromprofile
                    stromprofil_list.Clear();
                    rs.Open("select * from Abfrage_Monatsstrom where ID_Projekt=" + m_ID_Projekt);
                    while (rs.Next())
                    {
                        stromprofil_list.Add((string)rs.Read("Bezeichner").ToString());
                    }
                    rs.Close();
                }
                else
                {
                    // Parameter Liste mit Stromprofilnamen
                    stromprofil_list = list;
                }

                for (int k = 0; k < stromprofil_list.Count; k++)
                {
                    rs.Open("select * from Tab_Stromverbraucher where Bezeichner='" + stromprofil_list[k] + "'");
                    if (rs.Next())
                    {
                        float pjv = 0;
                        float jv = 0;
                        if (m_ID_Projekt != 0) // skalieren ggf. mit geändertem Projekt Jahresverbrauch
                        {
                            Z_ProjektStromverbraucherCtrl ctrl = new Z_ProjektStromverbraucherCtrl();
                            ctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + m_ID_Projekt + " AND Bezeichner='" + (string)rs.Read("Bezeichner") + "'");
                            if (ctrl.rows > 0)
                                pjv = (float)ctrl.items[0].m_Summe;
                        }

                        for (int i = 0; i < 12; i++)
                        {
                            double d = (double)rs.Read("Monat_" + (i + 1).ToString());
                            monats_werte[i] = (float)d;
                            jv += monats_werte[i];
                        }

                        if (pjv > 0)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                monats_werte[i] = monats_werte[i] * pjv / jv;
                            }
                        }

                        // Tagesverteilung für den Prozess ermitteln
                        rs_pwtyp.Open("select * from Tab_Stromverbrauchertyp where Typname='" + (string)rs.Read("Typ") + "'");

                        if (rs_pwtyp.Next())
                        {
                            for (int i = 0; i < 168; i++)
                            {
                                double dw = (double)rs_pwtyp.Read((i + 1).ToString());
                                wochen_werte[i] = (float)dw;
                            }
                        }
                        rs_pwtyp.Close();

                        // Wärmebedarf jährlich gemäß wöchentlicher Verteilung
                        //temp = com.I_strom_wochetojahr(wochen_werte, monats_werte, mo_anfang, mo_ende);
                        WPPlan.Core.BhkwPlan.StromWocheToJahr(wochen_werte, monats_werte, temp, mo_anfang, mo_ende);
                        //com.CSharp_I_vectoren_addieren(temp, prozesswerte);
                    }
                    rs.Close();
                }
                return temp;
            }
            catch (SystemException ex)
            {
                rs.Close();
                Console.WriteLine("Fehler in Simulation: " + ex.Message);
                MessageBox.Show("Fehler in Simulation!");
                return null;
            }
        }

        public float Maximaler_Strombedarf(float[] Strombedarf)
        {
            float Strombedarf_Max;

            Strombedarf_Max = 0;
            for (int i = 0; i < Strombedarf.Length; i++)
            {
                if (Strombedarf_Max < Strombedarf[i]) Strombedarf_Max = Strombedarf[i];
            }

            return Strombedarf_Max;
        }

        public float[] MonatsSumme_MW(float[] werte_array, int[] mo_anfang, int[] mo_ende)
        {
            float[] z = new float[12];
            for (int indexMonat = 0; indexMonat < 12; indexMonat++)
            {
                //var result = werte_array..GetRange(mo_anfang[indexMonat], mo_ende[indexMonat] - mo_anfang[indexMonat] + 1);

                for (int n = mo_anfang[indexMonat]*4; n <= mo_ende[indexMonat]*4; n++)
                {
                    z[indexMonat] += werte_array[n]; // Addiert numbers[1], numbers[2], numbers[3]
                }

                z[indexMonat] = z[indexMonat] / 4000.0f;
            }
            return z;
        }

        public float[] NormVector(float[] array1, float value)
        {
            // sort numbers in vector
            float[] z = array1.Select(x => (x / value) * 100).ToArray();
            return z;
        }

        public float[] SortVector(float[] array1)
        {
            // sort numbers in vector
            float[] z = array1.OrderBy(x => x).ToArray();
            return z;
        }

        public float[] Stundenwerte_zu_viertelstunden(float[] stundenwerte)
        {  
            float[] viertelstundenwerte = new float[8760 * 4];
            for (int i = 0; i < 8760; i++)
            {
                viertelstundenwerte[i * 4] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 1] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 2] = stundenwerte[i];
                viertelstundenwerte[i * 4 + 3] = stundenwerte[i];
            }
            return viertelstundenwerte;
        }
        
        public float[] AddVectors(float[] array1, float[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays must be of the same length.");

            float[] result = new float[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] + array2[i];
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public class SimulationStrombedarf
    {
        public int m_ID_Projekt = 0;

        public int[] mo_anfang = new int[12];
        public int[] mo_ende = new int[12];
        public float[] monats_waerme = new float[12];
        public float[] wochen_waerme = new float[168];
        public float[] prozesswerte = new float[8760];
        public float[] temp = new float[8760];

        public float[] Strombedarf = new float[8760];
        private float[] Strombedarf_sortiert = new float[8760];
        public float[] Strombedarf_Gebaeude = new float[8760];
        public float[] Stromganglinie = new float[8760];
        public float[] Strombedarf_monat = new float[12];

        public float Strombedarf_Gebaeude_gesamt;
        public float Stromganglinie_gesamt;
        public float Strombedarf_gesamt;
        public float Strombedarf_Max;

        public float[] Dauerlinie = new float[8760];
        public float[] Dauerlinie_nicht_sortiert = new float[8760];

        public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();

        public SimulationStrombedarf()
        {
            Classes.Simulation.Init init = new Classes.Simulation.Init();
            init.Monatswerte_berechnen(mo_anfang, mo_ende);
        }

        public void Berechnung(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();

            m_ID_Projekt = ID_Projekt; 
            Strombedarf_berechnen();
            com.CSharp_I_vectoren_addieren(prozesswerte, Strombedarf);
            Strombedarf_Gebaeude_gesamt = com.I_vector_summe(prozesswerte);


            // Stromganglinien 
            Z_ProjektStromganglinieCtrl waectrl = new Z_ProjektStromganglinieCtrl();
            waectrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + m_ID_Projekt);
            Stromganglinie_gesamt = 0;
            for (int n = 0; n < waectrl.rows; n++)
            {
                rs.Open("select * from Abfrage_ProjektStromGanglinie where Tab_Stromganglinie.ID=" + waectrl.items[n].m_ID_Stromganglinie + " order by ID");

                int index = 0;
                double wert = 0;

                while (rs.Next())
                {
                    wert = (double)rs.Read("Wert");
                    Stromganglinie[index++] = (float)wert;
                }
                rs.Close();

                com.CSharp_I_vectoren_addieren(Stromganglinie, Strombedarf);
                Stromganglinie_gesamt += com.I_vector_summe(Stromganglinie);
            }


            com.I_monats_summe(Strombedarf, Strombedarf_monat, mo_anfang, mo_ende);

            Strombedarf_Max = Maximaler_Strombedarf(Strombedarf);

            Strombedarf_gesamt = com.I_vector_summe(Strombedarf);

            com.CSharp_I_vectoren_addieren(Strombedarf, Strombedarf_sortiert);

            Dauerlinie_nicht_sortiert = Strombedarf;
         
            com.I_normieren(Strombedarf_sortiert, Strombedarf_Max);
            com.I_normieren(Dauerlinie_nicht_sortiert, Strombedarf_Max);

            com.I_heapsort(Strombedarf_sortiert, Dauerlinie); // absteigend sortiert

            Array.Reverse(Dauerlinie);

        }

        public void Strombedarf_berechnen(List<string> list = null)
        {
            RecordSet rs = new RecordSet();
            RecordSet rs_pwtyp = new RecordSet();
            List<string> pw_list = new List<string>();

            com.I_vector_init(ref Strombedarf);
            com.I_vector_init(ref prozesswerte);
            com.I_vector_init(ref temp);

            if (list == null)
            {
                // Abfrage über gespeicherte Prozesse im Projekt
                pw_list.Clear();
                rs.Open("select * from Abfrage_Monatsstrom where ID_Projekt=" + m_ID_Projekt);
                while (rs.Next())
                {
                    pw_list.Add((string)rs.Read("Bezeichner").ToString());
                }
                rs.Close();
            }
            else
            {
                // über Liste mit Prozessnamen
                pw_list = list;
            }

            for (int k = 0; k < pw_list.Count; k++)
            {
                rs.Open("select * from Tab_Stromverbraucher where Bezeichner='" + pw_list[k] + "'");
                rs.Next();
  
                for (int i=0; i<12; i++)
                {
                    double d = (double)rs.Read("Monat_" + (i + 1).ToString());
                    monats_waerme[i] = (float)d;
                }
               
                // Tagesverteilung für den Prozess ermitteln
                rs_pwtyp.Open("select * from Tab_Stromverbrauchertyp where Typname='" + (string)rs.Read("Typ") + "'");
                rs.Close(); 

                if (rs_pwtyp.Next())
                {
                    for (int i = 0; i < 168; i++)
                    {
                        double dw = (double)rs_pwtyp.Read((i + 1).ToString());
                        wochen_waerme[i] = (float)dw;
                    }
                }
                rs_pwtyp.Close();

                // Wärmebedarf jährlich gemäß wöchentlicher Verteilung
                temp = com.I_strom_wochetojahr(wochen_waerme, monats_waerme, mo_anfang, mo_ende);
                com.CSharp_I_vectoren_addieren(temp, prozesswerte);
            }
 
        }

        public float Maximaler_Strombedarf(float[] Strombedarf)
        {
            float Strombedarf_Max;

            Strombedarf_Max = 0;
            for (int i = 0; i < 8760; i++)
            {
                if (Strombedarf_Max < Strombedarf[i]) Strombedarf_Max = Strombedarf[i];
            }

            return Strombedarf_Max;
        }

        public void SimulationErgebis_in_DB()
        {
            OdbcCommand DBCommand = Program.DBConnection.CreateCommand();

            string sql = "update Tab_Simulation_Ergebnis " +
                "set Strombedarf_Max=" + Strombedarf_Max.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Strombedarf_Gesamt=" + Strombedarf_gesamt.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Strombedarf_Profil=" + Strombedarf_Gebaeude_gesamt.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Strombedarf_Extern=" + Stromganglinie_gesamt.ToString(CultureInfo.CreateSpecificCulture("en-US"));

            Cursor.Current = Cursors.WaitCursor;

            try
            {
                DBCommand.CommandText = sql;
                DBCommand.ExecuteNonQuery();
                DBCommand.Dispose();
                Cursor.Current = Cursors.Default;
            }
            catch (SystemException ex)
            {
                Console.WriteLine("Fehler in Simulation: " + ex.Message);
                DBCommand.Dispose();
                Cursor.Current = Cursors.Default;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public void Berechnung(int ID_Projekt)
        {
            m_ID_Projekt = ID_Projekt; 
            Strombedarf_berechnen();
            com.CSharp_I_vectoren_addieren(prozesswerte, Strombedarf);
            Strombedarf_Gebaeude_gesamt = com.I_vector_summe(prozesswerte);

            com.I_monats_summe(prozesswerte, Strombedarf_monat, mo_anfang, mo_ende);

            Strombedarf_Max = Maximaler_Strombedarf(Strombedarf);
            Strombedarf_gesamt = Strombedarf_Gebaeude_gesamt;

            com.CSharp_I_vectoren_addieren(Strombedarf, Strombedarf_sortiert);

            Dauerlinie_nicht_sortiert = Strombedarf;

            com.I_normieren(Strombedarf_sortiert, Strombedarf_Max);
            com.I_normieren(Dauerlinie_nicht_sortiert, Strombedarf_Max);

            com.I_heapsort(Strombedarf_sortiert, Dauerlinie); // absteigend sortiert

            Array.Reverse(Dauerlinie);

        }

        public void Strombedarf_berechnen(string szName = "")
        {
            RecordSet rs = new RecordSet();
            RecordSet rs_pwtyp = new RecordSet();
            int tageImMonat;

            com.I_vector_init(ref Strombedarf);
            com.I_vector_init(ref prozesswerte);
            com.I_vector_init(ref temp);

            // Monatsanfang bzw. Monatsende bestimmen
            DateTime date = DateTime.Today;
            tageImMonat = DateTime.DaysInMonth(date.Year, 1) * 24;

            mo_anfang[0] = 0;
            mo_ende[0] = (mo_anfang[0] + tageImMonat) - 1;

            for (int i = 2; i <= 12; i++)
            {
                tageImMonat = DateTime.DaysInMonth(date.Year, i) * 24;
                mo_anfang[i - 1] = (mo_ende[i - 2] + 1);
                mo_ende[i - 1] = (mo_anfang[i - 1] + tageImMonat) - 1;
            }

            if (szName == "")
                rs.Open("select * from Abfrage_Monatsstrom where ID_Projekt=" + m_ID_Projekt);
            else
                rs.Open("select * from Tab_Stromverbraucher where Bezeichner='" + szName + "'");

            while (rs.Next())
            {
                for (int i = 0; i < 12; i++)
                {
                    double d = (double)rs.Read("Monat_" + (i + 1).ToString());
                    monats_waerme[i] = (float)d;
                }

                // Tagesverteilung für den Prozess ermitteln
                rs_pwtyp.Open("select * from Tab_Stromverbrauchertyp where Typname='" + (string)rs.Read("Typ").ToString() + "'");

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
            rs.Close();
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
    }
}

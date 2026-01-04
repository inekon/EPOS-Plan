using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    class StromTestClass
    {
        public string m_szStromspeicher;
        public int m_ID_Projekt;

        public void MyTestProfil(string stromprofil)
        {
            // stündlicher Strombedarf berechnen gemäß Profil, z.B. "Type_A"
            // in Tab_Stromverbraucher stehen die Profile
            // in Strombedarf_berechnen wird das Profil geladen und in prozesswerte
            // die stündliche Verteilung geschrieben
            SimulationStrombedarf sim = new SimulationStrombedarf();
            List<string> list = new List<string>();

            list.Add(stromprofil);
            sim.Strombedarf_berechnen(list);

            // alle Prozesswerte durchlaufen
            for (int i = 0; i < 8760; i++)
            {
                float val = sim.prozesswerte[i];
            }

        }

        public float[] MyTestLastgang(string stromgang)
        {
            // stündlicher Strombedarf berechnen gemäß Profil, z.B. "Type_A"
            // in Tab_Stromverbraucher stehen die Profile
            // in Strombedarf_berechnen wird das Profil geladen und in prozesswerte
            // die stündliche Verteilung geschrieben
            float energie_speicher_curr;
            // max. Lade-Energie Speicher in % oder kW (muss noch berechnet werden!)
            float energie_speicher_max=1;
            // min. Lade-Energie Speicher in % oder kW (muss noch berechnet werden!)
            float energie_speicher_min =0;
            // Leistung Speicher
            float ladeschwelle_speicher = 0;
            // Ladeleistung in % von max.
            float leistung_speicher_laden_max = 1;
            float lastspitze = 0;
            // Zeitintervall Leistungsmessung
            float timeinter;

            float[] Stromganglinie = new float[8760];
            float[] Stromganglinie_neu = new float[8760];
            Z_ProjektStromganglinieCtrl waectrl = new Z_ProjektStromganglinieCtrl();
            RecordSet rs = new RecordSet();
            waectrl.ReadAll("select * from Z_ProjektStromganglinie where Bezeichner='" + stromgang + "'");

            // da der Bezeichner eindeutig ist, ist das Ergenis 1 Datensatz, ist nur expemplarisch
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
            }
            // prüfen ob stromspeicher ausgewählt wurde
            energie_speicher_max = Form_Simulation_Config.textBox_Stromspeicher_Ladeenergie_min;
            timeinter = 1;
            for (int  i = 0; i<Stromganglinie.Length; i++)
            {
                if (lastspitze < Stromganglinie[i]) lastspitze = Stromganglinie[i];
                // speicher laden bis max

                // 
                if (energie_speicher_curr < energie_speicher_max) energie_speicher_curr = Leistung_Speicher * leistung_speicher_laden_max * timeinter;
                if (energie_speicher_curr > energie_speicher_max) energie_speicher_curr = energie_speicher_max;

                if ((Stromganglinie[i] > ladeschwelle_speicher) && (Stromganglinie[i] > lastspitze - Leistung_Speicher * leistung_speicher_laden_max))
                {
                    // entladen
                    energie_speicher_curr = energie_Speicher_curr - Leistung_Speicher * leistung_speicher_laden_max  * timeinter;
                    if (energie_speicher_curr < energie_speicher_min)
                    {
                        energie_speicher_curr = energie_speicher_min;
                    }
                    Stromganglinie[i] = Stromganglinie[i] - energie_speicher_curr / timeinter;

                }


            }

            return Stromganglinie;
        }

        public void StromspeicherDaten()
        {
            // Stromspeicher Beispiel Daten holen für "test1"
            StromspeicherCtrl ctrl = new StromspeicherCtrl();

            // Daten für "test1" holen
            ctrl.ReadSingle(m_szStromspeicher);
            // Nennleistung leistung_speicher
            double Leistung_Speicher = ctrl.m_Leistung;
            double Energie_Speicher = ctrl.m_Energie;
            double Ladezustand = ctrl.m_Ladezustand;
            double Degradation = ctrl.m_Degradation;
        }

        public void KühlleistungDaten(string szWaermepumpe)
        {
            WPCtrl wpCtrl = new WPCtrl();
            wpCtrl.ReadSingle("select * from Tab_WP where WPName='" + szWaermepumpe + "'");
            int idwp = wpCtrl.ID;

            KenndatenKuehlungCtrl kdkCtrl = new KenndatenKuehlungCtrl();
            kdkCtrl.ReadAll(idwp);

            for (int n = 0; n < kdkCtrl.rows; n++)
            {


            }
        }
    }
}

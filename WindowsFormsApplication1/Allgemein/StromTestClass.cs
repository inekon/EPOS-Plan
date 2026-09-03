using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            sim.Stromprofil_Strombedarf_berechnen(list);

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

            float[] Stromganglinie = new float[8760];
            Z_ProjektStromganglinieCtrl waectrl = new Z_ProjektStromganglinieCtrl();
            RecordSet rs = new RecordSet();
            waectrl.ReadAll("select * from Z_ProjektStromganglinie where Bezeichner='" + stromgang + "'");

            // da der Bezeichner eindeutig ist, ist das Ergenis 1 Datensatz, ist nur expemplarisch
            for (int n = 0; n < waectrl.rows; n++)
            {
                // BEFUND B1 (S7): wie SimulationStrombedarf.Berechnung - Sichtspalten ueber den
                // Tabellennamen angesprochen, in SQLite "no such column". Das alte
                // "order by ID" sortierte nach der Ganglinien-ID, die das WHERE ohnehin
                // festnagelt, war also wirkungslos; sortiert wird jetzt wie im Simulationszweig
                // nach der Datensatz-ID (ID_Daten).
                rs.Open("select * from Abfrage_ProjektStromGanglinie where ID=" + waectrl.items[n].m_ID_Stromganglinie + " order by ID_Daten");

                int index = 0;
                double wert = 0;

                while (rs.Next())
                {
                    wert = (double)rs.Read("Wert");
                    Stromganglinie[index++] = (float)wert;
                }
                rs.Close();
            }
            return Stromganglinie;
        }

        public void StromspeicherDaten()
        {
            // Stromspeicher Beispiel Daten holen für "test1"
            StromspeicherCtrl ctrl = new StromspeicherCtrl();

            // Daten für "test1" holen
            ctrl.ReadSingle(m_szStromspeicher);

            double Leistung = ctrl.m_Leistung;
            double Energie = ctrl.m_Energie;
            double Ladezustand = ctrl.m_Ladezustand;
            double Degradation = ctrl.m_Degradation;
        }

        public void KühlleistungDaten(string szWaermepumpe)
        {
            WPCtrl wpCtrl = new WPCtrl();
            // SQL-Dialekt-Audit 03.09.2026: Tab_WP fuehrt den Namen als Bezeichner, eine
            // Spalte WPName gibt es im ganzen Schema nicht ("no such column: WPName").
            // Der Knopf "Kuehlung" in Form_StromTest lief deshalb in eine Fehlermeldung.
            wpCtrl.ReadSingle("select * from Tab_WP where Bezeichner='" + szWaermepumpe + "'");
            int idwp = wpCtrl.ID;

            KenndatenKuehlungCtrl kdkCtrl = new KenndatenKuehlungCtrl();
            kdkCtrl.ReadAll(idwp);

            for (int n = 0; n < kdkCtrl.rows; n++)
            {


            }
        }
    }
}

using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public class SimulationSPK
    {
        public const int MAX_SPK = 5;

        public List<string> spk_list = new List<string>();
        public int m_ID_Projekt = 0;

        public double Maximale_Kesselleistung_Spk;
        public double Stromverbrauch_Spk;
        double[] Kessel_waerme_gas_spk = new double[MAX_SPK];
        double[] s_waerme_oel_spk = new double[MAX_SPK];
        double[] s_waerme_gas_spk = new double[MAX_SPK];
        double[] Kessel_Wirk_Gas_Spk = new double[MAX_SPK];
        double[] Kessel_Wirk_Oel_Spk = new double[MAX_SPK];
        double[] Betriebsbereitschaft_Verluste = new double[MAX_SPK];
        string [] Kessel_name = new string[MAX_SPK];
        int [] Brennstoff_Betrieb_Spk = new int[MAX_SPK];
        int [] Brennstoff_Art = new int[MAX_SPK];
        double [] kessel_leistung_spk = new double[MAX_SPK];


        public bool Berechnung(int ID_Projekt)
        {
            m_ID_Projekt = ID_Projekt;

            Maximale_Kesselleistung_Spk = 0;
            Stromverbrauch_Spk = 0;

            for (int i = 0; i < MAX_SPK; i++)
            {
                s_waerme_gas_spk[i] = 0;
                s_waerme_oel_spk[i] = 0;
                Kessel_Wirk_Gas_Spk[i] = 0;
                Kessel_Wirk_Oel_Spk[i] = 0;
                Betriebsbereitschaft_Verluste[i] = 0;
                Kessel_name[i] = "";
                Brennstoff_Betrieb_Spk[i] = 0;
                kessel_leistung_spk[i] = 0;
            }

            BrennstoffCtrl brennstoffctrl = new BrennstoffCtrl();
            for (int i = 0; i < spk_list.Count; i++)
            {
                brennstoffctrl.ReadAll("Name='" + spk_list[i] + "'");
                Kessel_name[i] = brennstoffctrl.items[0].Name;
                kessel_leistung_spk[i] = brennstoffctrl.items[0].Ptherm;
                Kessel_Wirk_Gas_Spk[i] = brennstoffctrl.items[0].Wirkungsgrad_Gas; 
                Kessel_Wirk_Oel_Spk[i] = brennstoffctrl.items[0].Wirkungsgrad_Oel;
                Brennstoff_Betrieb_Spk[i] = brennstoffctrl.items[0].Brennstoff;
                Brennstoff_Art[i] = Brennstoff_Betrieb_Spk[i];
                if (Brennstoff_Betrieb_Spk[i] > 1) Brennstoff_Betrieb_Spk[i] = 1;
                Betriebsbereitschaft_Verluste[i] = brennstoffctrl.items[0].Betriebsbereitschaftverlust;
                Maximale_Kesselleistung_Spk = Maximale_Kesselleistung_Spk + kessel_leistung_spk[i];
            }






            

            return true;
        }


    }
}

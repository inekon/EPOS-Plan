using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Data.Odbc;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    public class SimulationGebaeude
    {
        private float[] BrauchwasserGeb = new float[100];
        public float[] HeizwaermebedarfGeb = new float[100];
        private float[] Sol_N = new float[365];
        private float[] Sol_w = new float[365];
        private float[] Sol_O = new float[365];
        private float[] Sol_S = new float[365];
        public float[] A_Temp = new float[365];
        private bool[] WE = new bool[365];
        private int[] TagTyp_W = new int[365];
        private int[] TagTyp_NW = new int[365];
        public float[] Solare_Gewinne = new float[365];
        public float[] Waermebedarf = new float[8760];
        public float[] Waermebedarf_Gebaeude = new float[8760];
        public float[] Waermebedarf_Extern = new float[8760];
        private float[] Waermebedarf_sortiert = new float[8760];
        private float[] SpezWaermeverluste = new float[365];
        private float[] Heizlast = new float[365];
        public float[] WarmwasserBedarf = new float[365];
        private float[] TagesVerteilung = new float[240];
        public float[] MaxP = new float[100];
        public float[] Dauerlinie = new float[8760];
        public float[] Dauerlinie_nicht_sortiert = new float[8760];
        private bool[] F_Absenkung = new bool[365];
        public float[] Tagestemperatur = new float[8760];
        private bool DBGelesen = false;
        public float[] Prozesswaerme = new float[8760];

        public int Anzahlgebaeude = 0;
        public float Waermebedarf_Max = 0;
        public float Waermebedarf_Gesamt = 0;
        public int Netzverluste = 0;
        public string Netzverluste_Einheit = "";
        public double Waermebedarf_Netzverluste = 0;
        public double Waermebedarf_Prozess = 0;
        public double Waermebedarf_Gebaeude_Gesamt = 0;
        public double Waermebedarf_Extern_Gesamt = 0;
        public float[] Waermebedarf_monat = new float[12]; 

        public int m_ID_Projekt = 0;

        public int[] mo_anfang = new int[12];
        public int[] mo_ende = new int[12];
        public float[] monats_waerme = new float[12];
        public float[] wochen_waerme = new float[168];
        public float[] prozesswerte = new float[8760];
        public float[] temp = new float[8760];

        public class Ergebnis
        {
            public Ergebnis()
            {
                Waermebedarf_Max = 0;
                Gesamt_Waermebedarf = 0;
            }
            public double Waermebedarf_Max;
            public double Gesamt_Waermebedarf;

        };

        public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();
 
        public void Berechnung(int ID_Projekt, int ID_Klimaregion)
        {
            m_ID_Projekt = ID_Projekt; 
            com.I_vector_init(ref Dauerlinie);
            com.I_vector_init(ref Dauerlinie_nicht_sortiert);
            com.I_vector_init(ref Waermebedarf_Extern);
            com.I_vector_init(ref Waermebedarf);
            com.I_vector_init(ref Waermebedarf_Gebaeude);
            com.I_vector_init(ref Waermebedarf_sortiert);
            com.I_vector_init(ref prozesswerte);

            if (!DBGelesen)
            {
                KlimadatenCtrl ctrl_klima = new KlimadatenCtrl();
                ctrl_klima.ReadAll(ID_Klimaregion);
                for (int i = 0; i < ctrl_klima.rows; i++)
                {
                    Sol_N[i] = (float)ctrl_klima.items[i].m_Sol_Nord;
                    Sol_w[i] = (float)ctrl_klima.items[i].m_Sol_West;
                    Sol_O[i] = (float)ctrl_klima.items[i].m_Sol_Ost;
                    Sol_S[i] = (float)ctrl_klima.items[i].m_Sol_Sued;
                    A_Temp[i] = (float)ctrl_klima.items[i].m_nTemperatur;
                    WE[i] = (bool)ctrl_klima.items[i].m_WE;
                    TagTyp_W[i] = (int)ctrl_klima.items[i].m_TagTyp_W;
                    TagTyp_NW[i] = (int)ctrl_klima.items[i].m_TagTyp_NW;
                }
                Fill_DBTagestemperatur(ID_Klimaregion);
            }

           
            ProjektGebaeudeCtrl ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(ID_Projekt);

/*
            if (ctrl.rows == 0)
            {
                MessageBox.Show("Aktuell ist im Projekt weder ein Gebäude noch eine Prozesswärme definiert !\n" +
                "Bitte überprüfen Sie die Eingabe", "Information", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
*/
            for (int i = 0; i < ctrl.rows; i++)
            {

                double VerbrauchNeu = 0.0;

                if (ctrl.items[i].Einheit == "Ölverbrauch [l/a]")
                {
                    VerbrauchNeu = ctrl.items[i].AuswahlWohnflaeche * ctrl.items[i].Jahresnutzungsgrad * 10.08;
                }
                else if (ctrl.items[i].Einheit == "Gasverbrauch [m³/a]")
                {
                    VerbrauchNeu = ctrl.items[i].AuswahlWohnflaeche * ctrl.items[i].Jahresnutzungsgrad * 11.48;
                }
                else if (ctrl.items[i].Einheit == "Gasverbrauch [MWh/a] (Ho)")
                {
                    VerbrauchNeu = ctrl.items[i].AuswahlWohnflaeche * ctrl.items[i].Jahresnutzungsgrad / 1.1 * 1000;
                }
                else if (ctrl.items[i].Einheit == "Brennstoffverbrauch [MWh/a]")
                {
                    VerbrauchNeu = ctrl.items[i].AuswahlWohnflaeche * ctrl.items[i].Jahresnutzungsgrad * 1000;
                }
                else if (ctrl.items[i].Einheit == "Verbrauch  [MWh/a]")
                {
                    VerbrauchNeu = ctrl.items[i].AuswahlWohnflaeche * 1000;
                }

                if (ctrl.items[i].Einheit == "Wohnfläche [m²]")
                {
                    ctrl.items[i].Bewohner = ctrl.items[i].AuswahlWohnflaeche / ctrl.items[i].Flaeche_Nutzer;
                }
                else
                {
                    Berechnung_Gebaeude_Tageswerte(ctrl.items[i]);
                    double FlaecheAlt = ctrl.AuswahlWohnflaeche;
                    double VerbrauchAlt = (BrauchwasserGeb[i] + HeizwaermebedarfGeb[i]) / 1000;
                    double FlaecheNeu = VerbrauchNeu / VerbrauchAlt * FlaecheAlt;
                    ctrl.AuswahlWohnflaeche = FlaecheNeu;
                    ctrl.items[i].Bewohner = ctrl.items[i].AuswahlWohnflaeche / ctrl.items[i].Flaeche_Nutzer;
                }

                Berechnung_Gebaeude_Tageswerte(ctrl.items[i]);

                bool tagv_found  = false;
                TagesVerteilung = DBTagesVeteilung(ctrl.items[i].Typ, ref tagv_found);
                if (!tagv_found) { MessageBox.Show("Daten zu Tagverteilungtyp nicht gefunden: " + ctrl.items[i].Typ); return; }  
                
                if (ctrl.items[i].Typ == "Wohngebaeude  VDI 2067")
                {
                    com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_W, TagesVerteilung, Heizlast);
                }
                else com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_NW, TagesVerteilung, Heizlast);
                
                com.CSharp_I_vectoren_addieren(Waermebedarf_Gebaeude, Waermebedarf);

                MaxP[i] = Maximaler_Waermebedarf(Waermebedarf);
            }
            Anzahlgebaeude = ctrl.rows;

            com.I_Watt_To_Kw(ref Waermebedarf);

            // Wärmebedarf Gebäude
            Waermebedarf_Gebaeude_Gesamt = com.I_vector_summe(Waermebedarf); 

            // Wärmebedarf extern 
            ToolsClass tool = new ToolsClass();
            WaermebedarfExternCtrl waectrl = new WaermebedarfExternCtrl();

            waectrl.ReadAll(ID_Projekt);

            Waermebedarf_Extern_Gesamt = 0;
            for(int n=0; n < waectrl.rows; n++) 
            {
                string szPath = Path.Combine(Program.ApplicationPath_User, "Waermebedarf");
                szPath = Path.Combine(szPath, waectrl.items[n].m_szDateiname);
                if(!tool.OpenText(szPath)) return;
                
                for (int i = 0; i < tool.textList.Count; i++)
                    Waermebedarf_Extern[i] = float.Parse(tool.textList[i]);

                com.CSharp_I_vectoren_addieren(Waermebedarf_Extern, Waermebedarf);
                Waermebedarf_Extern_Gesamt += com.I_vector_summe(Waermebedarf_Extern); 
            }

            // Prozesswärme
            Prozesswaerme_berechnen();
            com.CSharp_I_vectoren_addieren(prozesswerte, Waermebedarf);
            Waermebedarf_Prozess = com.I_vector_summe(prozesswerte); 

            com.I_monats_summe(prozesswerte, Waermebedarf_monat, mo_anfang, mo_ende);
            
            // Netzverluste 
            Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf); 
            
            float stundl_netzverluste = 0;
            if (Netzverluste_Einheit == "%")
            {
                stundl_netzverluste = (Waermebedarf_Gesamt * 1000 * Netzverluste) / 876000;
                Waermebedarf_Netzverluste = (Waermebedarf_Gesamt * Netzverluste) / 100;
            }
            else stundl_netzverluste = Netzverluste / 8760;

            com.I_netzverlustec(Waermebedarf, stundl_netzverluste);
    
            Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf); 

            com.CSharp_I_vectoren_addieren(Waermebedarf, Waermebedarf_sortiert);

            Dauerlinie_nicht_sortiert = Waermebedarf;

            Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf);

            com.I_normieren(Waermebedarf_sortiert, Waermebedarf_Max);
            com.I_normieren(Dauerlinie_nicht_sortiert, Waermebedarf_Max);

            com.I_heapsort(Waermebedarf_sortiert, Dauerlinie); // absteigend sortiert

            Array.Reverse(Dauerlinie);

        }

        private float[] DBTagesVeteilung(string TagV_Type, ref bool tagv_found)
        {
            float[] tagv = new float[192];
            RecordSet rs = new RecordSet();

            rs.Open("select * from Abfrage_Tagverteilung where Name='" + TagV_Type + "'");
            int n = 0;
            while (rs.Next())
            {
                double val = (double)rs.Read("Verteilung"); 
                tagv[n] = (float)val;
                n++;
            }
            if (n > 0) tagv_found = true;
            return tagv;
        }

        private void Berechnung_Gebaeude_Tageswerte(ProjektGebaeudeModel item)
        {
            int WE_Absenkung = 0;
            int Ferien_Absenkung = 0;

            if(item.Raumsolltemperatur_Ferien < 1)
            {
                  item.Ferien = 0; // Ferienabsenkung
            }

            for(int Tag = 0; Tag < 365; Tag++)
            {
                F_Absenkung[Tag] = false;
            }
            
            if(item.Ferien > 0.9)
            {
                if(item.Ferienbeginn_1 > 0 &&  item.Ferienbeginn_1 <= 365)
                {
                    for(int Tag = (int)item.Ferienbeginn_1; Tag<365; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                    for(int Tag = 0; Tag < (int)item.Ferienende_1; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_2 > 0 && item.Ferienende_2 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_2 - 1; Tag<item.Ferienende_2; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if(item.Ferienbeginn_3 > 0 && item.Ferienende_3 > 0)
                {
                    for(int Tag = (int)item.Ferienbeginn_3 - 1; Tag<item.Ferienende_3; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if(item.Ferienbeginn_4 > 0 && item.Ferienende_4 > 0)
                {
                    for(int Tag = (int)item.Ferienbeginn_4 - 1; Tag<item.Ferienende_4; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
            }

            for (int Tag = 350; Tag < 365; Tag++)
            {

                WarmwasserBedarf[Tag] = (float)item.WW_Bedarf * (float)item.Bewohner * 1000 / 365;

                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / 100;
                
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                        (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                        (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                        (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                        (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                        (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;

                WE_Absenkung = 0;
                if ((float)item.Raumsolltemperatur_Wochenende > 5)
                {
                    if (WE[Tag]) WE_Absenkung = 1; else WE_Absenkung = 0;
                }
                if (F_Absenkung[Tag]) Ferien_Absenkung = 1; else Ferien_Absenkung = 0;

                Heizlast[Tag] = com.I_TaeglHeizlastWG(Tag + 1,
                        WE_Absenkung,
                        (float)item.Raumsolltemperatur_Wochenende,
                        Ferien_Absenkung,
                        (float)item.Raumsolltemperatur_Ferien,
                        (float)item.Raumsolltemperatur_Tag,
                        (float)item.Raumsolltemperatur_Nachtabsenkung,
                        (float)item.Interne_Waermegewinne,
                        (float)Solare_Gewinne[Tag],
                        (float)SpezWaermeverluste[Tag],
                        (float)item.Bauweise,
                        (float)A_Temp[Tag],
                        (float)item.Maximaleraumtemperatur,
                        (float)item.AuswahlWohnflaeche,
                        (float)item.Wohnflaeche) + WarmwasserBedarf[Tag];
            }

            for (int Tag = 0; Tag < 365; Tag++)
            {
                WarmwasserBedarf[Tag] = (float)item.WW_Bedarf * (float)item.Bewohner * 1000 / 365;

                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / 100;

                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                        (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                        (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                        (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                        (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                        (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;

                WE_Absenkung = 0;
                if ((float)item.Raumsolltemperatur_Wochenende > 5)
                {
                    if (WE[Tag]) WE_Absenkung = 1; else WE_Absenkung = 0;
                }

                Heizlast[Tag] = (float)com.I_TaeglHeizlastWG(
                    Tag+1,
                    WE_Absenkung,
                    (float)item.Raumsolltemperatur_Wochenende,
                    Ferien_Absenkung,
                    (float)item.Raumsolltemperatur_Ferien,
                    (float)item.Raumsolltemperatur_Tag,
                    (float)item.Raumsolltemperatur_Nachtabsenkung,
                    (float)item.Interne_Waermegewinne,
                    (float)Solare_Gewinne[Tag],
                    (float)SpezWaermeverluste[Tag],
                    (float)item.Bauweise,
                    (float)A_Temp[Tag],
                    (float)item.Maximaleraumtemperatur,
                    (float)item.AuswahlWohnflaeche,
                    (float)item.Wohnflaeche) + WarmwasserBedarf[Tag];
            }
  
        }

        private float Maximaler_Waermebedarf(float[] Waermebedarf)
        {
            float Waermebedarf_Max;
        
            Waermebedarf_Max = 0;
            for(int i = 0; i<8760; i++)
            {
                if(Waermebedarf_Max < Waermebedarf[i]) Waermebedarf_Max = Waermebedarf[i];
            }
   
            return Waermebedarf_Max;
        }

        private void Fill_DBTagestemperatur(int ID_Klimaregion)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Solar where ID_Klimaregion=" + ID_Klimaregion + " order by ID "); 
            int i = 0;
            double temp;
            while (rs.Next())
            {
                temp = (double)rs.Read("Temperatur");
                Tagestemperatur[i++] = (float)temp;
            }
        }

        public void SimulationErgebis_in_DB()
        {
            OdbcCommand DBCommand = Program.DBConnection.CreateCommand();

            string sql = "update Tab_Simulation_Ergebnis " + 
                "set Waermebedarf_Max=" + Waermebedarf_Max.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Waermebedarf_Gesamt=" + Waermebedarf_Gesamt.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Waermebedarf_Gebaeude=" + Waermebedarf_Gebaeude_Gesamt.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Waermebedarf_Extern=" + Waermebedarf_Extern_Gesamt.ToString(CultureInfo.CreateSpecificCulture("en-US")) + 
                ", Netzverluste=" + Waermebedarf_Netzverluste.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                ", Waermebedarf_Prozess=" + Waermebedarf_Prozess.ToString(CultureInfo.CreateSpecificCulture("en-US"));
            
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

        public Ergebnis SimulationErgebis_aus_DB()
        {
            RecordSet rs = new RecordSet();
            Ergebnis result = new Ergebnis();
 
            rs.Open("select * from Tab_Simulation_Ergebnis");
            if (rs.Next())
            {
                result.Waermebedarf_Max = (double)rs.Read("Waermebedarf_Max");
                result.Gesamt_Waermebedarf = (double)rs.Read("Waermebedarf_Gesamt");
            }

            return result;
        }

        public void Prozesswaerme_berechnen(string szName = "")
        {
            RecordSet rs = new RecordSet();
            RecordSet rs_pwtyp = new RecordSet();
            int tageImMonat;
    
   
            com.I_vector_init(ref Prozesswaerme);
            com.I_vector_init(ref prozesswerte);
            com.I_vector_init(ref temp);

            // Monatsanfang bzw. Monatsende bestimmen
            DateTime date = DateTime.Today;  
            tageImMonat = DateTime.DaysInMonth(date.Year, 1) * 24;

            mo_anfang[0] = 0;
            mo_ende[0] = (mo_anfang[0] + tageImMonat) -1;
            
            for(int i=2; i<=12; i++)
            {
                tageImMonat = DateTime.DaysInMonth(date.Year, i) * 24;
                mo_anfang[i-1] = (mo_ende[i-2] + 1);
                mo_ende[i-1] = (mo_anfang[i-1] + tageImMonat)-1;
            }
 
            if(szName == "")
                rs.Open("select * from Abfrage_Monatswaerme where ID_Projekt=" + m_ID_Projekt);
            else
                rs.Open("select * from Tab_Prozesswaerme where Prozessname='" + szName + "'");
            
            while(rs.Next())
            {
                for(int i=0; i<12; i++)
                {
                    double d =  (double)rs.Read("Monat_" + (i+1).ToString()) ;
                    monats_waerme[i] = (float)d;
                }
               
                // Tagesverteilung für den Prozess ermitteln
                rs_pwtyp.Open("select * from Tab_Prozesstyp where Typname='" + (string)rs.Read("Typ").ToString()  + "'");
                
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

    }
}

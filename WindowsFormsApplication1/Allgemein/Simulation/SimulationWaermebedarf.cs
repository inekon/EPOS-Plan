using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class SimulationWaermebedarf
    {
        public bool DBGelesen = false;
        public int Anzahl_Gebaeude = 0;
        public int Anzahl_Bewohner = 0;
        public double Wohnflaeche = 0;
        public int m_ID_Projekt = 0;

        // Solare Wärme        
        private float[] Sol_N = new float[365];
        private float[] Sol_w = new float[365];
        private float[] Sol_O = new float[365];
        private float[] Sol_S = new float[365];
        private float[] A_Temp = new float[365];
        private bool[] WE = new bool[365];

        private int[] TagTyp_W = new int[365];
        private int[] TagTyp_NW = new int[365];
        public float[] Solare_Gewinne = new float[365];

        // Gebäudeprofil Wärmebedarf
        public float[] Waermebedarf = new float[8760];
        public float[] Waermebedarf_Gebaeude = new float[8760];
        public float[] Waermebedarf_Gebaeude_Monat = new float[12];
        public float[] HeizwaermebedarfGeb = new float[100];
        public float[] Waermebedarf_sortiert = new float[8760];
        public float Waermebedarf_Max = 0;
        public float Waermebedarf_Gesamt = 0;
        public double Waermebedarf_Gebaeude_Gesamt = 0;


        // Brauchwasser Wärmeenergie 
        public float[] Waermebedarf_Brauchwasser_Monat = new float[12];
        public double Waermebedarf_Brauchwasser = 0;

        // Lastgang Gebäude
        public float[] Waermebedarf_Extern = new float[8760];
        public double Waermebedarf_Extern_Gesamt = 0;

        // Prozesswärme
        public float[] Waermebedarf_Prozess_Monat = new float[12];
        public double Waermebedarf_Prozess = 0;

        // Temperaturgang Klimaregion
        public float[] Stundentemperatur = new float[8760];

        private float[] SpezWaermeverluste = new float[365];
        private float[] Heizlast = new float[365];
        private float[] TagesVerteilung = new float[240];
        private float[] MaxP = new float[100];
        public float[] Dauerlinie = new float[8760];
        public float[] Dauerlinie_nicht_sortiert = new float[8760];
        private bool[] F_Absenkung = new bool[365];

        // Netzverluste
        public int Netzverluste = 0;
        public double Waermebedarf_Netzverluste = 0;
        public string Netzverluste_Einheit = "";

        // Monat-, Wochenwärme
        public int[] mo_anfang = new int[12];
        public int[] mo_ende = new int[12];
        public float[] monats_waerme = new float[12];
        public float[] wochen_waerme = new float[168];
        public float[] prozesswerte = new float[8760];
        public float[] brauchwasserwerte = new float[8760];
        public float[] temp = new float[8760];

        //public CSExeCOMServer.SimpleObject com = new CSExeCOMServer.SimpleObject();

        public SimulationWaermebedarf()
        {
            Classes.Simulation.Init init = new Classes.Simulation.Init();
            init.Monatswerte_berechnen(mo_anfang, mo_ende);
        }

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

        public void Waermebedarf_berechnen(int ID_Projekt, int ID_Klimaregion)
        {
            Z_ProjektGebGanglinieCtrl waectrl;
            RecordSet rs;


            m_ID_Projekt = ID_Projekt;
            /*
            com.I_vector_init(ref Dauerlinie);
            com.I_vector_init(ref Dauerlinie_nicht_sortiert);
            com.I_vector_init(ref Waermebedarf_Extern);
            com.I_vector_init(ref Waermebedarf);
            com.I_vector_init(ref Waermebedarf_Gebaeude);
            com.I_vector_init(ref Waermebedarf_sortiert);
            com.I_vector_init(ref prozesswerte);
            com.I_vector_init(ref brauchwasserwerte);
            */

            WPPlan.Core.BhkwPlan.VectorInit(Dauerlinie);
            WPPlan.Core.BhkwPlan.VectorInit(Dauerlinie_nicht_sortiert);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_Extern);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_Gebaeude);
            WPPlan.Core.BhkwPlan.VectorInit(Waermebedarf_sortiert);
            WPPlan.Core.BhkwPlan.VectorInit(prozesswerte);
            WPPlan.Core.BhkwPlan.VectorInit(brauchwasserwerte);



            //  if (!DBGelesen)
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
                Stundentemperatur_aus_DB(ID_Klimaregion);
                DBGelesen = true;
            }

            ProjektGebaeudeCtrl ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(ID_Projekt);

            for (int i = 0; i < ctrl.rows; i++)
            {

                // wenn die Einheit nicht als "Wohnfläche [m²]" angegeben ist...Wohnfläche und Anzahl Bewohner berechnen
                if (ctrl.items[i].Einheit == "Wohnfläche [m²]")
                {
                    ctrl.items[i].Bewohner = ctrl.items[i].Z_AuswahlWohnflaeche / ctrl.items[i].Flaeche_Nutzer;
                }
                else
                {
                    Bewohner_und_Flaeche_berechnen(ctrl.items[i], i);
                }
                Anzahl_Bewohner = (int)ctrl.items[i].Bewohner;
                Wohnflaeche = ctrl.items[i].Z_AuswahlWohnflaeche;

                // Tagesverteilung berechnen
                Berechnung_Gebaeude_Tageswerte(ctrl.items[i], i);

                bool tagv_found = false;
                TagesVerteilung = DBTagesVeteilung(ctrl.items[i].Typ, ctrl.items[i].ID_Gebaeude, ref tagv_found);
                if (!tagv_found) { MessageBox.Show("Daten zu Tagverteilungtyp nicht gefunden: " + ctrl.items[i].Typ); return; }

                // Stundenwerte Wärmebedarf je nach Gebäudetyp und Tagtyp aus Klimaregion
                if (ctrl.items[i].Typ == "Wohngebaeude  VDI 2067")
                {
                    //com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_W, TagesVerteilung, Heizlast);
                    WPPlan.Core.BhkwPlan.StdWerte(Waermebedarf_Gebaeude, TagTyp_W, TagesVerteilung, Heizlast);
                }
                else
                    //com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_NW, TagesVerteilung, Heizlast);
                    WPPlan.Core.BhkwPlan.StdWerte(Waermebedarf_Gebaeude, TagTyp_NW, TagesVerteilung, Heizlast);

                //com.CSharp_I_vectoren_addieren(Waermebedarf_Gebaeude, Waermebedarf);
                WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf_Gebaeude, Waermebedarf);

                // Maximaler Wärmebedarf pro Gebäude
                MaxP[i] = Maximaler_Waermebedarf(Waermebedarf);

            }

            Anzahl_Gebaeude = ctrl.rows;

            //com.I_Watt_To_Kw(ref Waermebedarf);
            WPPlan.Core.BhkwPlan.WattToKw(Waermebedarf);


            // Wärmebedarf gesamt für alle Gebäude
            //Waermebedarf_Gebaeude_Gesamt = com.I_vector_summe(Waermebedarf);
            Waermebedarf_Gebaeude_Gesamt = Waermebedarf.Sum() / 1000;

            // Wärmebedarf extern 
            waectrl = new Z_ProjektGebGanglinieCtrl();
            waectrl.ReadAll("select * from Z_ProjektWaermebedarf where ID_Projekt=" + m_ID_Projekt);

            Waermebedarf_Extern_Gesamt = 0;
            rs = new RecordSet();
            for (int n = 0; n < waectrl.rows; n++)
            {
                rs.Open("select * from Abfrage_ProjektGebaeudeGanglinie where Tab_Waermebedarf.ID=" + waectrl.items[n].m_ID_Ganglinie + " order by Tab_WaermebedarfDaten.ID");

                int index = 0;
                double wert = 0;

                while (rs.Next())
                {
                    wert = (double)rs.Read("Wert");
                    Waermebedarf_Extern[index++] = (float)wert;
                }
                rs.Close();

                //com.CSharp_I_vectoren_addieren(Waermebedarf_Extern, Waermebedarf);
                WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf_Extern, Waermebedarf);

                //Waermebedarf_Extern_Gesamt += com.I_vector_summe(Waermebedarf_Extern);
                Waermebedarf_Extern_Gesamt += Waermebedarf_Extern.Sum() / 1000;
            }

            // Wärmebedarf Gebäude Monat
            //com.I_monats_summe(Waermebedarf, Waermebedarf_Gebaeude_Monat, mo_anfang, mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(Waermebedarf, Waermebedarf_Gebaeude_Monat, mo_anfang, mo_ende);

            // Prozesswärme
            Prozesswaerme_berechnen();
            //Waermebedarf_Prozess = com.I_vector_summe(prozesswerte);
            Waermebedarf_Prozess = prozesswerte.Sum() / 1000;

            //com.I_monats_summe(prozesswerte, Waermebedarf_Prozess_Monat, mo_anfang, mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(prozesswerte, Waermebedarf_Prozess_Monat, mo_anfang, mo_ende);
            //com.CSharp_I_vectoren_addieren(prozesswerte, Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorenAddieren(prozesswerte, Waermebedarf);

            // Brauchwasserwärme
            Brauchwasserwaerme_berechnen();
            //Waermebedarf_Brauchwasser = com.I_vector_summe(brauchwasserwerte);
            Waermebedarf_Brauchwasser = brauchwasserwerte.Sum() / 1000;
            //com.I_monats_summe(brauchwasserwerte, Waermebedarf_Brauchwasser_Monat, mo_anfang, mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(brauchwasserwerte, Waermebedarf_Brauchwasser_Monat, mo_anfang, mo_ende);
            //com.CSharp_I_vectoren_addieren(brauchwasserwerte, Waermebedarf);
            WPPlan.Core.BhkwPlan.VectorenAddieren(brauchwasserwerte, Waermebedarf);

            // Netzverluste 
            //Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf);
            Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;


            float stundl_netzverluste = 0;
            if (Netzverluste_Einheit == "%")
            {
                stundl_netzverluste = (Waermebedarf_Gesamt * 1000 * Netzverluste) / (float)876000;
                Waermebedarf_Netzverluste = (Waermebedarf_Gesamt * Netzverluste) / 100;
            }
            else stundl_netzverluste = (float)Netzverluste / (float)8760;

            //com.I_netzverlustec(Waermebedarf, stundl_netzverluste);
            WPPlan.Core.BhkwPlan.NetzverlusteC(Waermebedarf, stundl_netzverluste);

            // gesamter Wärmebedarf
            //Waermebedarf_Gesamt = com.I_vector_summe(Waermebedarf);
            Waermebedarf_Gesamt = Waermebedarf.Sum() / 1000;

            //com.CSharp_I_vectoren_addieren(Waermebedarf, Waermebedarf_sortiert);
            WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf, Waermebedarf_sortiert);

            //com.CSharp_I_vectoren_addieren(Waermebedarf, Dauerlinie_nicht_sortiert);
            WPPlan.Core.BhkwPlan.VectorenAddieren(Waermebedarf, Dauerlinie_nicht_sortiert);

            //Dauerlinie_nicht_sortiert = Waermebedarf;

            // Maximaler Stunden Wärmebedarf gesamt
            Waermebedarf_Max = Maximaler_Waermebedarf(Waermebedarf);

            // Normierung Ganglinie
            //com.I_normieren(Waermebedarf_sortiert, Waermebedarf_Max);
            WPPlan.Core.BhkwPlan.Normieren(Waermebedarf_sortiert, Waermebedarf_Max);
            //com.I_normieren(Dauerlinie_nicht_sortiert, Waermebedarf_Max);
            WPPlan.Core.BhkwPlan.Normieren(Dauerlinie_nicht_sortiert, Waermebedarf_Max);

            // absteigend sortieren
            //com.I_heapsort(Waermebedarf_sortiert, Dauerlinie); // absteigend sortiert
            WPPlan.Core.BhkwPlan.Heapsort(Waermebedarf_sortiert, Dauerlinie);

            Array.Reverse(Dauerlinie);
        }

        // ===================================================================
        // Kanalmodell (Paket 4, Etappe 4b - Konzept 3.2)
        // ===================================================================

        /// <summary>
        /// Stunden, in denen der Heizkanal auf 0 gekappt werden musste, weil der
        /// Brauchwasserwert über dem Gesamtwärmebedarf lag (siehe <see cref="Kanaele"/>).
        /// Erwartungswert 0; ein Wert &gt; 0 ist ein Befund, kein Betriebszustand.
        /// </summary>
        public int Kanal_Kappungen = 0;

        /// <summary>Summe der gekappten Wärmemenge [kWh] (siehe <see cref="Kanal_Kappungen"/>).</summary>
        public double Kanal_Kappung_kWh = 0;

        /// <summary>
        /// Die beiden Bedarfskanäle des Projekts (Konzept 3.2, Entscheidung E6):
        ///
        /// <code>
        /// Kanal BRAUCHWASSER [8760] = brauchwasserwerte
        /// Kanal HEIZUNG      [8760] = Waermebedarf − brauchwasserwerte   (elementweise, ≥ 0)
        /// </code>
        ///
        /// Der Heizkanal wird bewusst als RESIDUUM gebildet und nicht aus seinen
        /// Bestandteilen neu zusammengesetzt. Grund: <see cref="Waermebedarf"/> trägt
        /// zusätzlich zu Gebäude-, Prozess- und Brauchwasserwärme die NETZVERLUSTE, die
        /// weiter oben (<see cref="WPPlan.Core.BhkwPlan.NetzverlusteC"/>) als konstanter
        /// Stundenbetrag auf ALLE 8760 Stunden aufgeschlagen werden — also erst NACH der
        /// Addition der Brauchwasserwerte. Das Residuum trägt sie damit vollständig; genau
        /// das ist die heutige implizite Zuordnung und laut Konzept 3.2 (vormals O2) die
        /// einzige altverhaltenserhaltende Variante.
        ///
        /// Summenfelder und alle bestehenden Vektoren bleiben unberührt — die Methode
        /// rechnet ausschließlich lesend und liefert ein neues Objekt. Es gilt
        /// <c>Heiz + WW == Waermebedarf</c>, solange nicht gekappt wurde.
        ///
        /// KAPPUNGSFÄLLE. Elementweise ≥ 0 verlangt zwei Klemmungen, die im Normalbetrieb
        /// beide nicht auftreten:
        ///
        /// 1. <b>negativer Brauchwasserwert</b> — kann aus einer fehlerhaften Ganglinie
        ///    stammen; er wird auf 0 gesetzt, damit der WW-Kanal keine „negative Deckung"
        ///    an die Kaskade weitergibt.
        /// 2. <b>Brauchwasser über Gesamtbedarf</b> — rechnerisch unmöglich, weil
        ///    <c>Waermebedarf</c> die Brauchwasserwerte ENTHÄLT und alle weiteren
        ///    Summanden nichtnegativ sind. Übrig bleibt der Rundungsfall in <c>float</c>
        ///    (Vektorsumme gegen Einzelwert). Dann wird der Heizkanal auf 0 gesetzt und
        ///    der WW-Kanal auf den Gesamtbedarf begrenzt: Die SUMME bleibt exakt der
        ///    Gesamtbedarf — die Energieerhaltung geht dem Kanalanteil vor. Jeder solche
        ///    Fall wird gezählt (<see cref="Kanal_Kappungen"/>) und ist ein Befund, der
        ///    in die Verifikation gehört.
        /// </summary>
        public Waermekanaele Kanaele()
        {
            Kanal_Kappungen = 0;
            Kanal_Kappung_kWh = 0;

            Waermekanaele k = new Waermekanaele();
            for (int h = 0; h < Waermekanaele.STUNDEN_JAHR; h++)
            {
                float gesamt = (h < Waermebedarf.Length) ? Waermebedarf[h] : 0f;
                float ww = (h < brauchwasserwerte.Length) ? brauchwasserwerte[h] : 0f;

                if (ww < 0f) ww = 0f;                       // Kappungsfall 1

                float heiz = gesamt - ww;
                if (heiz < 0f)                              // Kappungsfall 2
                {
                    Kanal_Kappungen++;
                    Kanal_Kappung_kWh += -heiz;
                    heiz = 0f;
                    ww = gesamt > 0f ? gesamt : 0f;
                }

                k.Heiz[h] = heiz;
                k.WW[h] = ww;
            }

            return k;
        }

        private void Bewohner_und_Flaeche_berechnen(ProjektGebaeudeModel item, int index)
        {
            double VerbrauchNeu = 0.0;

            if (item.Einheit == "Ölverbrauch [l/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 10.08;
            }
            else if (item.Einheit == "Gasverbrauch [m³/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 11.48;
            }
            else if (item.Einheit == "Gasverbrauch [MWh/a] (Ho)")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad / 1.1 * 1000;
            }
            else if (item.Einheit == "Brennstoffverbrauch [MWh/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * item.Jahresnutzungsgrad * 1000;
            }
            else if (item.Einheit == "Verbrauch  [MWh/a]")
            {
                VerbrauchNeu = item.Z_AuswahlWohnflaeche * 1000;
            }

            if (item.Einheit == "Wohnfläche [m²]")
            {
                // item.Bewohner = item.AuswahlWohnflaeche / item.Flaeche_Nutzer;
                item.Bewohner = item.Wohnflaeche_gesamt / item.Flaeche_Nutzer;
            }
            else
            {
                item.Z_AuswahlWohnflaeche = item.Wohnflaeche_gesamt;

                Berechnung_Gebaeude_Tageswerte(item, index);
                double FlaecheAlt = item.Wohnflaeche_gesamt;
                //                double VerbrauchAlt = (BrauchwasserGeb[index] + HeizwaermebedarfGeb[index]) / 1000;
                double VerbrauchAlt = HeizwaermebedarfGeb[index] / 1000;
                double FlaecheNeu = VerbrauchNeu / VerbrauchAlt * FlaecheAlt;
                item.Z_AuswahlWohnflaeche = FlaecheNeu;
                item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;

            }
        }

        private float[] DBTagesVeteilung(string TagV_Type, int ID_Gebaeude, ref bool tagv_found)
        {
            float[] tagv = new float[192];
            RecordSet rs = new RecordSet();

            try
            {
                rs.Open("select * from Abfrage_Tagverteilung where Bezeichner='" + TagV_Type + "' and Tab_DBTagV.ID=" + ID_Gebaeude);
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
            finally { rs.Close(); }
        }

        private void Berechnung_Gebaeude_Tageswerte(ProjektGebaeudeModel item, int GebaeudeNr)
        {
            int WE_Absenkung = 0;
            int Ferien_Absenkung = 0;

            if (item.Raumsolltemperatur_Ferien < 1)
            {
                item.Ferien = 0; // Ferienabsenkung
            }

            for (int Tag = 0; Tag < 365; Tag++)
            {
                F_Absenkung[Tag] = false;
            }

            if (item.Ferien > 0.9)
            {
                if (item.Ferienbeginn_1 > 0 && item.Ferienbeginn_1 <= 365)
                {
                    for (int Tag = (int)item.Ferienbeginn_1; Tag < 365; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                    for (int Tag = 0; Tag < (int)item.Ferienende_1; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_2 > 0 && item.Ferienende_2 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_2 - 1; Tag < item.Ferienende_2; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_3 > 0 && item.Ferienende_3 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_3 - 1; Tag < item.Ferienende_3; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
                if (item.Ferienbeginn_4 > 0 && item.Ferienende_4 > 0)
                {
                    for (int Tag = (int)item.Ferienbeginn_4 - 1; Tag < item.Ferienende_4; Tag++)
                    {
                        F_Absenkung[Tag] = true;
                    }
                }
            }

            for (int Tag = 350; Tag < 365; Tag++)
            {
                /*
                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / (float)100;
                */
                Solare_Gewinne[Tag] = WPPlan.Core.BhkwPlan.SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / (float)100;

                /*
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                        (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                        (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                        (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                        (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                        (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;
                */
                SpezWaermeverluste[Tag] = WPPlan.Core.BhkwPlan.SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
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
                /*
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
                        (float)item.Z_AuswahlWohnflaeche,
                        (float)item.Wohnflaeche);
                */
                Heizlast[Tag] = WPPlan.Core.BhkwPlan.TaeglHeizlastWG(Tag + 1,
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
                        (float)item.Z_AuswahlWohnflaeche,
                        (float)item.Wohnflaeche);
            }

            HeizwaermebedarfGeb[GebaeudeNr] = 0;

            for (int Tag = 0; Tag < 365; Tag++)
            {
                /*
                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                        (float)item.Fensterdurchlassgrad) / 100;
                */
                Solare_Gewinne[Tag] = WPPlan.Core.BhkwPlan.SolareGewinneC(Sol_N[Tag], (float)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                    (float)item.Fensterflaeche_Ost, Sol_S[Tag], (float)item.Fensterflaeche_Sued,
                    (float)item.Fensterdurchlassgrad) / (float)100;
                /*
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
                        (float)item.k_Wert_Fenster, (float)item.gesamte_Fensterflaeche, (float)item.k_Wert_Dachflaeche,
                        (float)item.Dachflaeche, (float)item.k_Wert_Grundflaeche, (float)item.Grundflaeche,
                        (float)item.k_Wert_Sonstiges, (float)item.Sonstige_Flaechen, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (float)item.Abmessung_Anschluß_Fenster_Wand, (float)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (float)item.Abmessung_Anschluß_Wand_Dach,
                        (float)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (float)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (float)item.Wohnflaeche,
                        (float)item.Raumhoehe, (float)item.Luftwechselrate) / 100;
                */
                SpezWaermeverluste[Tag] = WPPlan.Core.BhkwPlan.SpezWaermeverlusteC((float)item.k_Wert_Außenwand, (float)item.Flaeche_Außenwand,
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

                /*
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
                    (float)item.Z_AuswahlWohnflaeche,
                    (float)item.Wohnflaeche);
                */
                Heizlast[Tag] = WPPlan.Core.BhkwPlan.TaeglHeizlastWG(Tag + 1,
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
                      (float)item.Z_AuswahlWohnflaeche,
                      (float)item.Wohnflaeche);

                HeizwaermebedarfGeb[GebaeudeNr] = HeizwaermebedarfGeb[GebaeudeNr] + Heizlast[Tag];
            }

        }

        private float Maximaler_Waermebedarf(float[] Waermebedarf)
        {
            float Waermebedarf_Max;

            Waermebedarf_Max = 0;
            for (int i = 0; i < 8760; i++)
            {
                if (Waermebedarf_Max < Waermebedarf[i]) Waermebedarf_Max = Waermebedarf[i];
            }

            return Waermebedarf_Max;
        }

        private void Stundentemperatur_aus_DB(int ID_Klimaregion)
        {
            RecordSet rs = new RecordSet();
            try
            {
                rs.Open("select * from Tab_Solar where ID_Klimaregion=" + ID_Klimaregion + " order by ID ");
                int i = 0;
                double temp;
                while (rs.Next())
                {
                    temp = (double)rs.Read("Temperatur");
                    Stundentemperatur[i++] = (float)temp;
                }
            }
            finally { rs.Close(); }
        }

        public void Prozesswaerme_berechnen(List<string> list = null)
        {
            RecordSet rs = new RecordSet();
            RecordSet rs_pwtyp = new RecordSet();
            List<string> pw_list = new List<string>();

            try
            {
                //com.I_vector_init(ref prozesswerte);
                WPPlan.Core.BhkwPlan.VectorInit(prozesswerte);
                //com.I_vector_init(ref temp);
                WPPlan.Core.BhkwPlan.VectorInit(temp);

                if (list == null)
                {
                    // Abfrage über gespeicherte Prozesse im Projekt
                    pw_list.Clear();
                    rs.Open("select * from Abfrage_Monatswaerme_Prozesse where ID_Projekt=" + m_ID_Projekt);
                    while (rs.Next())
                    {
                        pw_list.Add((string)rs.Read("Prozessname").ToString());
                    }
                    rs.Close();
                }
                else
                {
                    // über Parameter Liste mit Prozessnamen
                    pw_list = list;
                }

                for (int k = 0; k < pw_list.Count; k++)
                {
                    rs.Open("select * from Tab_Prozesswaerme_STAMM where Bezeichner='" + pw_list[k] + "'");
                    if (rs.Next())
                    {
                        float pjv = 0;
                        float jv = 0;
                        if (m_ID_Projekt != 0) // skalieren ggf. mit geändertem Projekt Jahresverbrauch
                        {
                            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
                            ctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + m_ID_Projekt + " AND Bezeichner='" + (string)rs.Read("Bezeichner") + "'");
                            if (ctrl.rows > 0)
                                pjv = (float)ctrl.items[0].Summe;
                        }
                        for (int i = 0; i < 12; i++)
                        {
                            double d = (double)rs.Read("Monat_" + (i + 1).ToString());
                            monats_waerme[i] = (float)d;
                            jv += monats_waerme[i];
                        }

                        if (pjv > 0)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                monats_waerme[i] = monats_waerme[i] * pjv / jv;
                            }
                        }

                        Object objTyp = rs.Read("Typ");
                        if (DBNull.Value.Equals(objTyp))
                        {
                            MessageBox.Show("DerTyp von Prozess " + pw_list[k] + " ist nicht definiert");
                            rs.Close();
                            return;
                        }

                        // Tagesverteilung für den Prozess ermitteln
                        rs_pwtyp.Open("select * from Tab_Prozesstyp where Typname='" + (string)objTyp + "'");

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
                        //temp = com.I_strom_wochetojahr(wochen_waerme, monats_waerme, mo_anfang, mo_ende);
                        WPPlan.Core.BhkwPlan.StromWocheToJahr(wochen_waerme, monats_waerme, temp, mo_anfang, mo_ende);

                        //com.CSharp_I_vectoren_addieren(temp, prozesswerte);
                        WPPlan.Core.BhkwPlan.VectorenAddieren(temp, prozesswerte);
                    }
                    rs.Close();

                }
            }
            catch (SystemException ex) { Console.Write(ex.Message); }
            finally
            {
                try { rs.Close(); } catch { }
                try { rs_pwtyp.Close(); } catch { }
            }
        }

        public void Brauchwasserwaerme_berechnen(List<string> list = null)
        {
            RecordSet rs = new RecordSet();
            RecordSet rs_pwtyp = new RecordSet();
            List<string> pw_list = new List<string>();

            try
            {

                //com.I_vector_init(ref brauchwasserwerte);
                WPPlan.Core.BhkwPlan.VectorInit(brauchwasserwerte);
                //com.I_vector_init(ref temp);
                WPPlan.Core.BhkwPlan.VectorInit(temp);

                if (list == null)
                {
                    // Abfrage über gespeicherte Prozesse im Projekt
                    pw_list.Clear();
                    rs.Open("select * from Abfrage_Monatswaerme_Brauchwasser where ID_Projekt=" + m_ID_Projekt);
                    while (rs.Next())
                    {
                        pw_list.Add((string)rs.Read("Bezeichner").ToString());
                    }
                    rs.Close();
                }
                else
                {
                    // über Parameter Liste mit Brauchwasser Profil Namen
                    pw_list = list;
                }

                // Vorschau (list != null): Namen sind Katalog-Bezeichner -> aus den STAMM-Tabellen lesen.
                // Echte Projektrechnung (list == null): aus den Projektkopien lesen.
                bool bStamm = (list != null);
                string headTable = bStamm ? "Tab_Brauchwasser_STAMM" : "Tab_Brauchwasser";
                string typTable = bStamm ? "Tab_Brauchwassertyp_STAMM" : "Tab_Brauchwassertyp";
                string typCol = bStamm ? "Bezeichner" : "Typname";

                for (int k = 0; k < pw_list.Count; k++)
                {
                    rs.Open("select * from " + headTable + " where Bezeichner='" + pw_list[k] + "'");
                    if (rs.Next())
                    {
                        float pjv = 0;
                        float jv = 0;
                        if (m_ID_Projekt != 0) // skalieren ggf. mit geändertem Projekt Jahresverbrauch
                        {
                            Z_ProjektBrauchwasserCtrl ctrl = new Z_ProjektBrauchwasserCtrl();
                            ctrl.ReadAll("select * from Z_Projekt_Brauchwasser where ID_Projekt=" + m_ID_Projekt + " AND Bezeichner='" + (string)rs.Read("Bezeichner") + "'");
                            if (ctrl.rows > 0)
                                pjv = (float)ctrl.items[0].Summe;
                        }
                        for (int i = 0; i < 12; i++)
                        {
                            double d = (double)rs.Read("Monat_" + (i + 1).ToString());
                            monats_waerme[i] = (float)d;
                            jv += monats_waerme[i];
                        }

                        if (pjv > 0)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                monats_waerme[i] = monats_waerme[i] * pjv / jv;
                            }
                        }

                        Object objTyp = rs.Read("Typ");
                        if (DBNull.Value.Equals(objTyp))
                        {
                            MessageBox.Show("DerTyp von Prozess " + pw_list[k] + " ist nicht definiert");
                            rs.Close();
                            return;
                        }

                        // Tagesverteilung für den Prozess ermitteln
                        rs_pwtyp.Open("select * from " + typTable + " where " + typCol + "='" + (string)objTyp + "'");

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
                        //temp = com.I_strom_wochetojahr(wochen_waerme, monats_waerme, mo_anfang, mo_ende);
                        WPPlan.Core.BhkwPlan.StromWocheToJahr(wochen_waerme, monats_waerme, temp, mo_anfang, mo_ende);
                        //com.CSharp_I_vectoren_addieren(temp, brauchwasserwerte);
                        WPPlan.Core.BhkwPlan.VectorenAddieren(temp, brauchwasserwerte);
                    }
                    rs.Close();

                }
            }
            catch (SystemException ex) { Console.Write(ex.Message); }
            finally
            {
                try { rs.Close(); } catch { }
                try { rs_pwtyp.Close(); } catch { }
            }
        }
    }
}

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class NavigatorUebersicht: UserControl, INavigatableContent
    {
        double waerme_solar = 0;
        double gesamt_waerme = 0;
        double restwaermebedarf = 0;
        double waerme_spk = 0;
        double waerme_wp = 0;
        double waerme_heizstab = 0;
           
        SimulationControl sim;

        // Donat Chart Farben (WP, Solar, Heizstab, Kessel, Rest)
        Color[] palette = new Color[] {
                ColorTranslator.FromHtml("#2ECC71"), // WP
                ColorTranslator.FromHtml("#E67E22"), // Solar
                ColorTranslator.FromHtml("#F1C40F"), // Heizstab
                ColorTranslator.FromHtml("#95A5A6"), // Kessel
                ColorTranslator.FromHtml("#3498DB")  // Rest
        };

        public NavigatorUebersicht(SimulationControl simctrl)
        {
            InitializeComponent();
            sim = simctrl;

            this.DoubleBuffered = true;

            // WICHTIG: Hier die Methode explizit an das Paint-Event binden!
            this.Paint += new PaintEventHandler(NavigatorUebersicht_Paint);

            this.Layout += (s, e) => {
                this.Invalidate();
            };
        }
        
        public void RefreshContent()
        {
            SetControl(this.sim);
            this.Invalidate();
        }
        
        public void SetControl(SimulationControl sim)
        {
            if (sim == null) return;
            if (sim.simulation_Waermebedarf == null) return; // Sicherheitshalber prüfen 
            
            // SPK
            waerme_spk = 0;
            for (int i = 0; i < sim.simulation_spk.spk_list.Count(); i++)
            {
                waerme_spk += sim.simulation_spk.s_waerme_Gas_Spk[i] + sim.simulation_spk.s_waerme_Oel_Spk[i];
            }

            // Wärmepumpe
            waerme_wp = sim.simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            waerme_heizstab = sim.simulation_wp.Heizstab_gesamt / 1000;

            // Solar
            waerme_solar = sim.simulation_solarthermie.Waermeproduktion_gesamt / 1000;
            gesamt_waerme = waerme_spk + waerme_wp + waerme_heizstab + waerme_solar;
            restwaermebedarf = sim.simulation_Waermebedarf.Waermebedarf_Gesamt - gesamt_waerme;
        }

        private void NavigatorUebersicht_Paint(object sender, PaintEventArgs e)
        {
            // 1. IMMER ZUERST DEN HINTERGRUND LEEREN
            e.Graphics.Clear(Color.FromArgb(240, 240, 240));

            // 2. Prüfen ob Daten da sind
            if (sim == null)
            {
                // Falls keine Daten da sind, zeichne wenigstens einen Platzhalter
                e.Graphics.DrawString("Warte auf Daten...", this.Font, Brushes.Black, 10, 10);
                return;
            }

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Deine Kacheln zeichnen
            // WICHTIG: Prüfe ob restwaermebedarf berechnet wurde!
            Kacheln.DrawKPICard(e.Graphics, new Rectangle(label_1.Left, label_1.Top, 200, 80),
                                "Reststrombedarf", sim.Reststrom.ToString("F2"), "MWh/a", Color.DodgerBlue);


            // Kachel für Wärme
            Kacheln.DrawKPICard(e.Graphics, new Rectangle(label_2.Left, label_2.Top, 200, 80),
                        "Restwärmebedarf", restwaermebedarf.ToString("F2"), "MWh/a", Color.SeaGreen);


            double[] werteArr_Prozent = new double[] { 0, 0, 0, 0, 0 };
            double wb_gesamt = 0;
            double werz_gesamt = 0;

            if (sim.simulation_Waermebedarf != null)
            {
                wb_gesamt = sim.simulation_Waermebedarf.Waermebedarf_Gesamt;
                werteArr_Prozent = new double[] { waerme_wp * 100/ wb_gesamt,
                                            waerme_solar* 100/ wb_gesamt,
                                            waerme_heizstab * 100 / wb_gesamt,
                                            waerme_spk * 100 / wb_gesamt,
                                            restwaermebedarf * 100 / wb_gesamt };

                werz_gesamt = waerme_wp + waerme_solar + waerme_heizstab + waerme_spk;
            }

            // Bereich für die Diagramm-Kachel definieren
            // (X=20, Y=150, Breite=220, Höhe=300)
            Rectangle kachelBereich = new Rectangle(label_3.Left, label_3.Top + label_3.Height + 10, 260, 260);

            // Die weiße Kachel zeichnen (mit der Funktion von vorhin)
            Kacheln.DrawKPICard(e.Graphics, kachelBereich, "Wärmedeckung", "", "", Color.SeaGreen);

            // Den Donut + Dynamische Legende darin aufrufen
            // Der Funktion ein etwas kleineres "Innen-Rechteck" geben, damit Abstände zum Rand bleiben
            Rectangle chartInnenBereich = new Rectangle(kachelBereich.X + 10, kachelBereich.Y + 10,
                                                       kachelBereich.Width - 20, kachelBereich.Height - 50);

            // Die Namen der 5 möglichen Quellen
            string[] quellenNamen = { "Wärmepumpe", "Solarthermie", "Heizstab", "Spitzenkessel", "Restwärme" };

            DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, chartInnenBereich, werteArr_Prozent, werz_gesamt * 100 / wb_gesamt, quellenNamen, palette);
   
            double sb_gesamt = 0;
            double serz_gesamt = 0;
            double se_pv = 0;
            
            if (sim.simulation_Strombedarf != null)
            {
                se_pv = sim.simulation_pv.Stromproduktion_gesamt / 1000;
                sb_gesamt = sim.simulation_Strombedarf.Strombedarf_gesamt + sim.simulation_wp.WP_Strombedarf_gesamt / 1000 + sim.simulation_wp.Heizstab_gesamt / 1000; ;
                werteArr_Prozent = new double[] { se_pv * 100 / sb_gesamt, (sb_gesamt-se_pv) * 100 / sb_gesamt };

                serz_gesamt = se_pv;
            }

            // Bereich für die Diagramm-Kachel definieren
            // (X=20, Y=150, Breite=220, Höhe=300)
            Rectangle kachelBereich_Strom = new Rectangle(label_4.Left, label_4.Top + label_4.Height + 10, 260, 260);

            // Die weiße Kachel zeichnen (mit der Funktion von vorhin)
            Kacheln.DrawKPICard(e.Graphics, kachelBereich_Strom , "Stromdeckung", "", "", Color.SeaGreen);

            // Den Donut + Dynamische Legende darin aufrufen
            // Der Funktion ein etwas kleineres "Innen-Rechteck" geben, damit Abstände zum Rand bleiben
            Rectangle chartInnenBereich_Strom = new Rectangle(kachelBereich_Strom.X + 10, kachelBereich_Strom.Y + 10,
                                                       kachelBereich_Strom.Width - 20, kachelBereich_Strom.Height - 50);

            // Die Namen der 5 möglichen Quellen
            string[] quellenNamen_Strom = { "Photovoltaik", "Reststrom" };

            DonutChartDrawer.DrawChartWithDynamicLegend(e.Graphics, chartInnenBereich_Strom, werteArr_Prozent, serz_gesamt * 100 / sb_gesamt, quellenNamen_Strom, palette);


        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Gebaeude2 : Form
    {
        public GebaeudeModel model = new GebaeudeModel();
        public DialogResult result;

        public Form_Gebaeude2()
        {
            InitializeComponent();
        }

        public void SetControls()
        {
            textBox_SollTag.Text = model.Raumsolltemperatur_Tag.ToString("F2"); 
            textBox_NachtAbsenkung.Text = model.Raumsolltemperatur_Nachtabsenkung.ToString("F2");
            textBox_MaxTemperatur.Text = model.Maximaleraumtemperatur.ToString("F2");
            textBox_WEAbsenkung.Text = model.Raumsolltemperatur_Wochenende.ToString("F2");
            
            
            textBox_SollFerien.Text = model.Ferien.ToString("F2");
            
            
            textBox_WBVK_Fenster.Text = model.Waermebrueckenverlustkoeffizient_Anschluﬂ_Fenster_Wand.ToString("F2");
            textBox_WBVK_Keller.Text = model.Waermebruckenverlustkoeffizient_Anschluﬂ_Auﬂenwand_Kellerdecke.ToString("F2");
            textBox_WBVK_Dach.Text = model.Waermebrueckenverlustkoeffizient_Anschluﬂ_Wand_Dach.ToString("F2");
            textBox_AnschussFenster.Text = model.Abmessung_Anschluﬂ_Fenster_Wand.ToString("F2");
            textBox_AnschussDach.Text = model.Abmessung_Anschluﬂ_Wand_Dach.ToString("F2");
            textBox_AnschussKeller.Text = model.Abmessung_Anschluﬂ_Auﬂenwand_Kellerdecke.ToString("F2");
            Winter_Tag_A.Text = model.Ferienbeginn_1.ToString("F2");
            Ostern_Tag_A.Text = model.Ferienbeginn_1.ToString("F2");
            Sommer_Tag_A.Text = model.Ferienbeginn_1.ToString("F2");
            Herbst_Tag_A.Text = model.Ferienbeginn_1.ToString("F2");
            Winter_Monat_A.Text = model.Ferienbeginn_1.ToString("F2");
            Ostern_Monat_A.Text = model.Ferienbeginn_1.ToString("F2");
            Sommer_Monat_A.Text = model.Ferienbeginn_1.ToString("F2");
            Herbst_Monat_A.Text = model.Ferienbeginn_1.ToString("F2");
            Winter_Tag_E.Text = model.Ferienbeginn_1.ToString("F2");
            Ostern_Tag_E.Text = model.Ferienbeginn_1.ToString("F2");
            Sommer_Tag_E.Text = model.Ferienbeginn_1.ToString("F2");
            Herbst_Tag_E.Text = model.Ferienbeginn_1.ToString("F2");
            Winter_Monat_E.Text = model.Ferienbeginn_1.ToString("F2");
            Ostern_Monat_E.Text = model.Ferienbeginn_1.ToString("F2");
            Sommer_Monat_E.Text = model.Ferienbeginn_1.ToString("F2");
            Herbst_Monat_E.Text = model.Ferienbeginn_1.ToString("F2");
            textBox_Luftwechsel.Text = model.Luftwechselrate.ToString("F2");
            textBox_WWNutzer.Text = model.WW_Bedarf.ToString();

            JahrestagUmrechner((int)model.Ferienbeginn_1, Winter_Tag_A, Winter_Monat_A);
            JahrestagUmrechner((int)model.Ferienende_1,Winter_Tag_E, Winter_Monat_E);
            JahrestagUmrechner((int)model.Ferienbeginn_2, Ostern_Tag_A, Ostern_Monat_A);
            JahrestagUmrechner((int)model.Ferienende_2, Ostern_Tag_E, Ostern_Monat_E);
            JahrestagUmrechner((int)model.Ferienbeginn_3, Sommer_Tag_A, Sommer_Monat_A);
            JahrestagUmrechner((int)model.Ferienende_3, Sommer_Tag_E, Sommer_Monat_E);
            JahrestagUmrechner((int)model.Ferienbeginn_4, Herbst_Tag_A, Herbst_Monat_A);
            JahrestagUmrechner((int)model.Ferienende_4, Herbst_Tag_E, Herbst_Monat_E);

        }

        public void JahrestagUmrechner(int jahrestag, Control Tag, Control Monat)
        {
            Tag.Text = Monat.Text = "";
            if (jahrestag == 0 || jahrestag == 366) return;
            DateTime startdatum = new DateTime(DateTime.Now.Year, 1, 1); // Startdatum ist immer der 1. Januar des aktuellen Jahres
            DateTime umgerechnetesDatum = startdatum.AddDays(jahrestag - 1); // Tage abziehen, da der 1. Januar Tag 1 ist
            Tag.Text = umgerechnetesDatum.Day.ToString();
            Monat.Text = umgerechnetesDatum.Month.ToString(); 
        }

        public static int BerechneJahrestag(string szMonat, string szTag)
        {
            if(szMonat == "") return 0;
            else if(szTag == "") return 0;

            int monat = Int32.Parse(szMonat);
            int tag = Int32.Parse(szTag);
 
            DateTime aktuellesJahr = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime benutzerDatum = new DateTime(DateTime.Now.Year, monat, tag);
            TimeSpan differenz = benutzerDatum - aktuellesJahr;
            return differenz.Days + 1;
        }

        private double Text2Wert(string szText)
        {
            if (szText == "") return 0;
            else return double.Parse(szText); 
        }

        private string Wert2Text(double dValue)
        {
            if (dValue == 0) return "";
            else return dValue.ToString("F2");
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // alle TextBoxen auf g¸ltiges double Format ¸berpr¸fen
            for (int i = 0; i < Controls.Count; i++)
            {
                var allControls = Controls[i].Controls;
                foreach (Control tb in allControls)
                {
                    if (tb.GetType().Equals(typeof(TextBox)) && tb.Text != "")
                    {
                        if (!Program.checkDouble(tb, tb.Text))
                        {
                            return;
                        }
                    }
                }
            }

            model.Raumsolltemperatur_Tag = Text2Wert(textBox_SollTag.Text);
            model.Raumsolltemperatur_Nachtabsenkung = Text2Wert(textBox_NachtAbsenkung.Text);
            model.Maximaleraumtemperatur = Text2Wert(textBox_MaxTemperatur.Text);
            if (model.Maximaleraumtemperatur < 1) model.Maximaleraumtemperatur = 24;

            model.Raumsolltemperatur_Wochenende = Text2Wert(textBox_WEAbsenkung.Text);
            if (model.Raumsolltemperatur_Wochenende > 0) model.Wochenende = 1;
            else model.Wochenende = 0;
            model.Ferien = Text2Wert(textBox_SollFerien.Text);
            if (model.Raumsolltemperatur_Ferien > 0) model.Ferien = 1;
            else model.Ferien = 0;
            
            model.Waermebrueckenverlustkoeffizient_Anschluﬂ_Fenster_Wand = Text2Wert(textBox_WBVK_Fenster.Text);
            model.Waermebruckenverlustkoeffizient_Anschluﬂ_Auﬂenwand_Kellerdecke = Text2Wert(textBox_WBVK_Keller.Text);
            model.Waermebrueckenverlustkoeffizient_Anschluﬂ_Wand_Dach = Text2Wert(textBox_WBVK_Dach.Text);
            model.Abmessung_Anschluﬂ_Fenster_Wand = Text2Wert(textBox_AnschussFenster.Text);
            model.Abmessung_Anschluﬂ_Wand_Dach = Text2Wert(textBox_AnschussDach.Text);
            model.Abmessung_Anschluﬂ_Auﬂenwand_Kellerdecke = Text2Wert(textBox_AnschussKeller.Text);

            model.Ferienbeginn_1 = BerechneJahrestag(Winter_Monat_A.Text, Winter_Tag_A.Text);
            model.Ferienbeginn_2 = BerechneJahrestag(Ostern_Monat_A.Text, Ostern_Tag_A.Text);
            model.Ferienbeginn_3 = BerechneJahrestag(Sommer_Monat_A.Text, Sommer_Tag_A.Text);
            model.Ferienbeginn_4 = BerechneJahrestag(Herbst_Monat_A.Text, Herbst_Tag_A.Text);

            model.Ferienende_1 = BerechneJahrestag(Winter_Monat_E.Text, Winter_Tag_E.Text);
            model.Ferienende_2 = BerechneJahrestag(Ostern_Monat_E.Text, Ostern_Tag_E.Text);
            model.Ferienende_3 = BerechneJahrestag(Sommer_Monat_E.Text, Sommer_Tag_E.Text);
            model.Ferienende_4 = BerechneJahrestag(Herbst_Monat_E.Text, Herbst_Tag_E.Text);

            if(model.Ferienbeginn_1 < model.Ferienende_1)
            {
                MessageBox .Show("Die Ferien m¸ssen ¸ber die Jahresgrenze gehen!");
                return;
            }
            if(model.Ferienbeginn_2 > model.Ferienende_2)
            {
                MessageBox.Show("Fehler: Bei der Eingabe der Osterferien!");
                return;
            }
            if (model.Ferienbeginn_3 > model.Ferienende_3)
            {
                MessageBox.Show("Fehler: Bei der Eingabe der Sommerferien!");
                return;
            }
            if (model.Ferienbeginn_4 > model.Ferienende_4)
            {
                MessageBox.Show("Fehler: Bei der Eingabe der Herbstferien!");
                return;
            }

            if (model.Ferienbeginn_1 == 0) model.Ferienbeginn_1 = 366;
            
            model.Luftwechselrate = Text2Wert(textBox_Luftwechsel.Text);
            model.WW_Bedarf = Text2Wert(textBox_WWNutzer.Text);

            result = DialogResult.OK;  
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;  
            Close();
        }

 
    }
}
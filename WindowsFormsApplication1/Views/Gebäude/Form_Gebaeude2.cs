using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Gebaeude2 : Form
    {
        public GebaeudeModel model = new GebaeudeModel();
        public DialogResult result;
        public long m_ID_Projekt;
        public string m_szProjektname;

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
            textBox_SollFerien.Text = model.Raumsolltemperatur_Ferien.ToString("F2");
            textBox_WBVK_Fenster.Text = model.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand.ToString("F2");
            textBox_WBVK_Keller.Text = model.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke.ToString("F2");
            textBox_WBVK_Dach.Text = model.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach.ToString("F2");
            textBox_AnschussFenster.Text = model.Abmessung_Anschluß_Fenster_Wand.ToString("F2");
            textBox_AnschussDach.Text = model.Abmessung_Anschluß_Wand_Dach.ToString("F2");
            textBox_AnschussKeller.Text = model.Abmessung_Anschluß_Außenwand_Kellerdecke.ToString("F2");

            Winter_Tag_A.Text = model.Ferienbeginn_1.ToString();
            Ostern_Tag_A.Text = model.Ferienbeginn_1.ToString();
            Sommer_Tag_A.Text = model.Ferienbeginn_1.ToString();
            Herbst_Tag_A.Text = model.Ferienbeginn_1.ToString();
            Winter_Monat_A.Text = model.Ferienbeginn_1.ToString();
            Ostern_Monat_A.Text = model.Ferienbeginn_1.ToString();
            Sommer_Monat_A.Text = model.Ferienbeginn_1.ToString();
            Herbst_Monat_A.Text = model.Ferienbeginn_1.ToString();
            Winter_Tag_E.Text = model.Ferienbeginn_1.ToString();
            Ostern_Tag_E.Text = model.Ferienbeginn_1.ToString();
            Sommer_Tag_E.Text = model.Ferienbeginn_1.ToString();
            Herbst_Tag_E.Text = model.Ferienbeginn_1.ToString();
            Winter_Monat_E.Text = model.Ferienbeginn_1.ToString();
            Ostern_Monat_E.Text = model.Ferienbeginn_1.ToString();
            Sommer_Monat_E.Text = model.Ferienbeginn_1.ToString();
            Herbst_Monat_E.Text = model.Ferienbeginn_1.ToString();
            textBox_Luftwechsel.Text = model.Luftwechselrate.ToString("F2");

            JahrestagUmrechner((int)model.Ferienbeginn_1, Winter_Tag_A, Winter_Monat_A);
            JahrestagUmrechner((int)model.Ferienende_1, Winter_Tag_E, Winter_Monat_E);
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
            // Keine Angabe (z.B. 0 aus der Datenbank) -> kein Ferientag
            if (string.IsNullOrWhiteSpace(szMonat) || string.IsNullOrWhiteSpace(szTag)) return 0;

            // Ungueltige oder nicht numerische Eingaben abfangen (verhindert Laufzeitfehler)
            if (!Program.GanzzahlParsen(szMonat, out int monat) || !Program.GanzzahlParsen(szTag, out int tag)) return 0;
            if (monat < 1 || monat > 12 || tag < 1 || tag > 31) return 0;

            try
            {
                DateTime aktuellesJahr = new DateTime(DateTime.Now.Year, 1, 1);
                DateTime benutzerDatum = new DateTime(DateTime.Now.Year, monat, tag);
                TimeSpan differenz = benutzerDatum - aktuellesJahr;
                return differenz.Days + 1;
            }
            catch
            {
                // Unmoegliches Datum (z.B. 30. Februar) -> kein Ferientag
                return 0;
            }
        }

        // Stiller Parser statt double.Parse: liest Komma und Punkt gleichermassen
        // ("12.5" wird 12,5 statt still 125) und wirft bei ungueltigem Text nicht.
        // Gemeldet wird bereits in btn_Speichern_Click, leer bleibt wie bisher 0.
        private double Text2Wert(string szText)
        {
            double dWert;
            if (!Program.ZahlParsen(szText, out dWert)) return 0;
            return dWert;
        }

        private string Wert2Text(double dValue)
        {
            if (dValue == 0) return "";
            else return dValue.ToString("F2");
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // Alle TextBoxen der Gruppen auf gültiges Zahlenformat überprüfen.
            // Gemeldet wird erst hier statt bei jedem Tastendruck; leere Felder
            // gelten wie bisher als 0 und werden übersprungen.
            for (int i = 0; i < Controls.Count; i++)
            {
                var allControls = Controls[i].Controls;
                foreach (Control tb in allControls)
                {
                    if (tb.GetType().Equals(typeof(TextBox)) && tb.Text != "")
                    {
                        double dPruefwert;
                        if (!Program.ZahlParsen(tb.Text, out dPruefwert))
                        {
                            MessageBox.Show("Eingaben überprüfen: \"" + tb.Text + "\"" + Environment.NewLine +
                                            "Bitte eine Zahl eingeben (Dezimaltrennzeichen Komma oder Punkt).",
                                            "Ungültige Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            tb.Focus();
                            ((TextBox)tb).SelectAll();
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
            model.Raumsolltemperatur_Ferien = Text2Wert(textBox_SollFerien.Text);
            if (model.Raumsolltemperatur_Ferien > 0) model.Ferien = 1;
            else model.Ferien = 0;

            model.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = Text2Wert(textBox_WBVK_Fenster.Text);
            model.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = Text2Wert(textBox_WBVK_Keller.Text);
            model.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = Text2Wert(textBox_WBVK_Dach.Text);
            model.Abmessung_Anschluß_Fenster_Wand = Text2Wert(textBox_AnschussFenster.Text);
            model.Abmessung_Anschluß_Wand_Dach = Text2Wert(textBox_AnschussDach.Text);
            model.Abmessung_Anschluß_Außenwand_Kellerdecke = Text2Wert(textBox_AnschussKeller.Text);

            model.Ferienbeginn_1 = BerechneJahrestag(Winter_Monat_A.Text, Winter_Tag_A.Text);
            model.Ferienbeginn_2 = BerechneJahrestag(Ostern_Monat_A.Text, Ostern_Tag_A.Text);
            model.Ferienbeginn_3 = BerechneJahrestag(Sommer_Monat_A.Text, Sommer_Tag_A.Text);
            model.Ferienbeginn_4 = BerechneJahrestag(Herbst_Monat_A.Text, Herbst_Tag_A.Text);

            model.Ferienende_1 = BerechneJahrestag(Winter_Monat_E.Text, Winter_Tag_E.Text);
            model.Ferienende_2 = BerechneJahrestag(Ostern_Monat_E.Text, Ostern_Tag_E.Text);
            model.Ferienende_3 = BerechneJahrestag(Sommer_Monat_E.Text, Sommer_Tag_E.Text);
            model.Ferienende_4 = BerechneJahrestag(Herbst_Monat_E.Text, Herbst_Tag_E.Text);

            if (model.Ferienbeginn_1 < model.Ferienende_1)
            {
                MessageBox.Show("Die Ferien müssen über die Jahresgrenze gehen!");
                return;
            }
            if (model.Ferienbeginn_2 > model.Ferienende_2)
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
            model.WW_Bedarf = 0;

            result = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_Brauchwasser_Click(object sender, EventArgs e)
        {
            Form_Brauchwasser frm = new Form_Brauchwasser();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            m_ID_Projekt = Program.startfrm.m_ID_Projekt;
            m_szProjektname = Program.startfrm.m_szProjektname;

            frm.list_pwmodel.Clear();

            string sql = "SELECT Z_Projekt_Brauchwasser.ID, Z_Projekt_Brauchwasser.ID_Projekt, " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser, Tab_Brauchwasser.Bezeichner, Z_Projekt_Brauchwasser.Summe " +
                "FROM Z_Projekt_Brauchwasser INNER JOIN Tab_Brauchwasser ON " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser = Tab_Brauchwasser.ID " +
                " where ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektBrauchwasserModel item = new Z_ProjektBrauchwasserModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = (int)m_ID_Projekt;
                item.ID_Brauchwasser = (int)rs.Read("ID_Brauchwasser");
                item.szBezeichner = (string)rs.Read("Bezeichner");
                item.Summe = (double)rs.Read("Summe");
                frm.list_pwmodel.Add(item);
            }

            frm.m_ID_Projekt = Program.startfrm.m_ID_Projekt;
            frm.SetControls(Program.startfrm.m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Brauchwasser((int)m_ID_Projekt);
                wizctrl.Add_Projekt_Brauchwasser((int)m_ID_Projekt, frm.list_pwmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        // TextChanged färbt ab hier nur noch (rosa = gerade keine Zahl). Gemeldet
        // wird erst in btn_Speichern_Click; das frühere Melden mit tb.Undo() konnte
        // sich aufschaukeln. Begründung ausführlich in Program.cs.
        private void textBox_SollTag_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_NachtAbsenkung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_MaxTemperatur_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_WEAbsenkung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_SollFerien_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_WBVK_Fenster_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_WBVK_Keller_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_WBVK_Dach_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_AnschussFenster_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_AnschussKeller_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_AnschussDach_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void Winter_Tag_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Winter_Monat_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Ostern_Tag_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Ostern_Monat_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Sommer_Tag_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Sommer_Monat_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Herbst_Tag_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Herbst_Monat_A_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Winter_Tag_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Winter_Monat_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_Luftwechsel_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void Ostern_Tag_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Ostern_Monat_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Sommer_Tag_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Sommer_Monat_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Herbst_Tag_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void Herbst_Monat_E_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }
    }
}

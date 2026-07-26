using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    public partial class Form_Gebaeude1 : Form
    {
        private List<string> list_geb = new List<string>() { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978", "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise", "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009", "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016", "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40" };
        public GebaeudeModel model = new GebaeudeModel();
        public bool m_bNeu = false;
        public bool m_bAdmin = false;

        public Form_Gebaeude1()
        {
            InitializeComponent();
        }

        private void Form_Gebaeude1_Load(object sender, EventArgs e)
        {
            if (m_bNeu || m_bAdmin)
            {
                btn_Ueberschreiben.Enabled = false;
                btn_NeuerDatensatz.Enabled = true;
                btn_NeuerDatensatz.Text = "Speichern";

                if (m_bAdmin)
                {
                    btn_Ueberschreiben.Enabled = true;
                    btn_NeuerDatensatz.Enabled = false;
                    RecordSet rs = new RecordSet();
                    rs.Open("SELECT * from Tab_Gebaeude_STAMM");
                    while (rs.Next())
                    {
                        string szGebTyp = (string)rs.Read("Bezeichner");
                        comboBox_Name.Items.Add(szGebTyp);
                    }
                    rs.Close();
                    comboBox_Name.SelectedIndex = 0;
                }
            }
        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();
            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();

            if (!m_bNeu && !m_bAdmin)
            {
                comboBox_Name.Text = model.Gebaeudename;
                ctrl.ReadAll("Bezeichner='" + comboBox_Name.Text + "'");
                model = ctrl.items[0];
            }
            else if (m_bNeu)
            {
                model = new GebaeudeModel();
            }
            else
            {
                if (comboBox_Name.Text == "") return;
                ctrl.ReadAll("Bezeichner='" + comboBox_Name.Text + "'");
                model = ctrl.items[0];
            }

            textBox_Beschreibung.Text = model.Beschreibung;

            rs.Open("SELECT * from Abfrage_Gebaeudetypen");
            while (rs.Next())
            {
                string szGebTyp = (string)rs.Read("Typ");
                comboBox_Gebaeudetyp.Items.Add(szGebTyp);
            }
            rs.Close();
            comboBox_Gebaeudetyp.Text = model.Typ;

            textBox_WohnflaecheGesamt.Text = model.Wohnflaeche_gesamt.ToString("F2");
            textBox_FlaecheNutzer.Text = model.Flaeche_Nutzer.ToString("F2");
            textBox_Fensterdurchlassgrad.Text = model.Fensterdurchlassgrad.ToString("F2");
            textBox_Raumhoehe.Text = model.Raumhoehe.ToString("F2");
            textBox_Waermegewinne.Text = model.Interne_Waermegewinne.ToString("F2");
            comboBox_Bauart.Items.Add("Leichte Bauart");
            comboBox_Bauart.Items.Add("Schwere Bauart");
            comboBox_Bauart.Items.Add("Sehr schwere Bauart");

            rs.Open("SELECT * from Abfrage_Gebaeudearten");
            while (rs.Next())
            {
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebaeudeart.Items.Add(szGebArt);
            }
            rs.Close();
            comboBox_Gebaeudeart.Text = model.Gebaeudeart;

            for (int i = 0; i < list_geb.Count; i++)
            {
                comboBox_Baujahr.Items.Add(list_geb[i]);
            }

            string Baualtersklasse = model.Baualtersklasse.Substring(0, 1);
            int index = (int)Baualtersklasse[0] - (int)'A';
            if (index < 0) index = 0;
            comboBox_Baujahr.Text = list_geb[index];

            double spezGebaeudekapazitaet = model.Bauweise / model.Wohnflaeche;
            if (spezGebaeudekapazitaet < 30) comboBox_Bauart.SelectedIndex = 0;
            else if (spezGebaeudekapazitaet > 75) comboBox_Bauart.SelectedIndex = 2;
            else comboBox_Bauart.SelectedIndex = 1;

            textBox_FFSued.Text = model.Fensterflaeche_Sued.ToString("F2");
            textBox_FFNord.Text = model.Fensterflaeche_Nord.ToString("F2");
            textBox_FFOstWest.Text = model.Fensterflaeche_Ost.ToString("F2");
            textBox_Flaeche_Aussenwand.Text = model.Flaeche_Außenwand.ToString("F2");
            textBox_Gebaeude_Dachflaeche.Text = model.Dachflaeche.ToString("F2");
            textBox_Gebaeude_Grundflaeche.Text = model.Grundflaeche.ToString("F2");
            textBox_Sonstige_Flaechen.Text = model.Sonstige_Flaechen.ToString("F2");
            textBox_UWert_Dachflaeche.Text = model.k_Wert_Dachflaeche.ToString("F2");
            textBox_UWert_Fenster.Text = model.k_Wert_Fenster.ToString("F2");
            textBox_UWert_Sonstige.Text = model.k_Wert_Sonstiges.ToString("F2");
            textBox_UWert_Aussenwand.Text = model.k_Wert_Außenwand.ToString("F2");
            textBox_UWert_Grundflaeche.Text = model.k_Wert_Grundflaeche.ToString("F2");
            comboBox_Verwendung.Text = model.Wohngebaeude_Nicht_Wohngebaeude;
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            if (!InitModelFromControls()) return;

            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();
            // Overwrite prueft selbst auf ReadOnly und meldet ggf. selbst.
            if (ctrl.Overwrite(model))
                MessageBox.Show("Gebäude Datensatz ist überschrieben!");
        }

        // Uebertraegt die Eingaben aus den Steuerelementen in das model.
        // Nicht editierte Felder (Raumsolltemperaturen, Waermebruecken, Ferien, ...) bleiben aus dem geladenen model erhalten.
        private bool InitModelFromControls()
        {
            try
            {
                model.Gebaeudename = comboBox_Name.Text;
                model.Typ = comboBox_Gebaeudetyp.Text;
                model.Beschreibung = textBox_Beschreibung.Text;

                double wfl = double.Parse(textBox_WohnflaecheGesamt.Text);
                model.Wohnflaeche_gesamt = wfl;

                double dWfNutzer = double.Parse(textBox_FlaecheNutzer.Text);
                model.Flaeche_Nutzer = dWfNutzer;
                if (dWfNutzer == 0) { model.Flaeche_Nutzer = 35; dWfNutzer = 35; }
                model.Bewohner = wfl / dWfNutzer; // Wohnfläche pro Nutzer

                model.Interne_Waermegewinne = double.Parse(textBox_Waermegewinne.Text);

                if (comboBox_Gebaeudeart.SelectedIndex == 0) model.Bauweise = wfl * 20;
                else if (comboBox_Gebaeudeart.SelectedIndex == 1) model.Bauweise = wfl * 50;
                else if (comboBox_Gebaeudeart.SelectedIndex == 2) model.Bauweise = wfl * 100;
                else model.Bauweise = 50;

                model.Fensterflaeche_Sued = double.Parse(textBox_FFSued.Text);
                model.Fensterflaeche_Ost = double.Parse(textBox_FFOstWest.Text);
                model.Fensterflaeche_Nord = double.Parse(textBox_FFNord.Text);
                model.Fensterdurchlassgrad = double.Parse(textBox_Fensterdurchlassgrad.Text);

                model.k_Wert_Außenwand = double.Parse(textBox_UWert_Aussenwand.Text);
                model.k_Wert_Fenster = double.Parse(textBox_UWert_Fenster.Text);
                model.k_Wert_Dachflaeche = double.Parse(textBox_UWert_Dachflaeche.Text);
                model.k_Wert_Grundflaeche = double.Parse(textBox_UWert_Grundflaeche.Text);
                model.k_Wert_Sonstiges = double.Parse(textBox_UWert_Sonstige.Text);
                model.Flaeche_Außenwand = double.Parse(textBox_Flaeche_Aussenwand.Text);
                model.gesamte_Fensterflaeche = double.Parse(textBox_FFSued.Text) +
                         double.Parse(textBox_FFOstWest.Text) +
                         double.Parse(textBox_FFNord.Text); // gesamte Fensterfläche
                model.Dachflaeche = double.Parse(textBox_Gebaeude_Dachflaeche.Text);
                model.Grundflaeche = double.Parse(textBox_Gebaeude_Grundflaeche.Text);
                model.Sonstige_Flaechen = double.Parse(textBox_Sonstige_Flaechen.Text);
                model.Wohnflaeche = wfl;
                model.Raumhoehe = double.Parse(textBox_Raumhoehe.Text);

                int index = comboBox_Baujahr.SelectedIndex;
                model.Baualtersklasse = ((char)('A' + index)).ToString();
                model.Gebaeudeart = comboBox_Gebaeudeart.Text;
                model.Wohngebaeude_Nicht_Wohngebaeude = comboBox_Verwendung.Text;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern!\nAlle Eingaben überprüfen!");
                Console.WriteLine("Fehler beim Speichern der Daten: " + ex.Message);
                return false;
            }
        }

        private void btn_NeuerDatensatz_Click(object sender, EventArgs e)
        {
            if (comboBox_Name.Text == "") { MessageBox.Show("Gebäudenamen eingeben!"); return; }

            if (!InitModelFromControls()) return;

            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();
            if (ctrl.Insert(model))
                MessageBox.Show("Gebäude ist gespeichert!");
        }

        private void comboBox_Name_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetControls();
        }

        private void btn_Dialog2_Click(object sender, EventArgs e)
        {
            Form_Gebaeude2 frm = new Form_Gebaeude2();
            frm.model = model;
            frm.SetControls();
            frm.ShowDialog();
        }

        private void textBox_WohnflaecheGesamt_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_FlaecheNutzer_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Fensterdurchlassgrad_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Raumhoehe_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Waermegewinne_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_FFNord_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_FFOstWest_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_FFSued_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Gebaeude_Grundflaeche_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Gebaeude_Dachflaeche_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Flaeche_Aussenwand_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Sonstige_Flaechen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_UWert_Aussenwand_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_UWert_Dachflaeche_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_UWert_Fenster_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_UWert_Grundflaeche_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_UWert_Sonstige_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text == "") { tb.Text = "0"; return; }
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }
    }
}

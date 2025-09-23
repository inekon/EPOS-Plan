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
                    rs.Open("SELECT * from DBGebaeude");
                    while (rs.Next())
                    {
                        string szGebTyp = (string)rs.Read("Gebaeudename");
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
            GebaeudeCtrl ctrl = new GebaeudeCtrl();

            if (!m_bNeu && !m_bAdmin)
            {
                comboBox_Name.Text = model.Gebaeudename;
                ctrl.ReadAll("Gebaeudename='" + comboBox_Name.Text + "'");
                model = ctrl.items[0];
            }
            else if (m_bNeu)
            {
                model = new GebaeudeModel();
            }
            else
            {
                if (comboBox_Name.Text == "") return;
                ctrl.ReadAll("Gebaeudename='" + comboBox_Name.Text + "'");
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
            textBox_Flaeche_Aussenwand.Text = model.Flaeche_Auﬂenwand.ToString("F2");
            textBox_Gebaeude_Dachflaeche.Text = model.Dachflaeche.ToString("F2");
            textBox_Gebaeude_Grundflaeche.Text = model.Grundflaeche.ToString("F2");
            textBox_Sonstige_Flaechen.Text = model.Sonstige_Flaechen.ToString("F2");
            textBox_UWert_Dachflaeche.Text = model.k_Wert_Dachflaeche.ToString("F2");
            textBox_UWert_Fenster.Text = model.k_Wert_Fenster.ToString("F2");
            textBox_UWert_Sonstige.Text = model.k_Wert_Sonstiges.ToString("F2");
            textBox_UWert_Aussenwand.Text = model.k_Wert_Auﬂenwand.ToString("F2");
            textBox_UWert_Grundflaeche.Text = model.k_Wert_Grundflaeche.ToString("F2");
            comboBox_Verwendung.Text = model.Wohngebaeude_Nicht_Wohngebaeude; 
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            GebaeudeCtrl ctrl = new GebaeudeCtrl();
            ctrl.ReadAll("Gebaeudename='" + comboBox_Name.Text + "'");
            DataSet ds = new DataSet();

            OdbcDataAdapter adapter = new OdbcDataAdapter("select ID,Gebaeudename,Typ,Beschreibung,Wohnflaeche_gesamt, " +
                "Bewohner,Flaeche_Nutzer,Interne_Waermegewinne,Bauweise,Fensterflaeche_Sued,Fensterflaeche_Ost_West,Fensterflaeche_Nord," +
                "Fensterdurchlassgrad,Raumsolltemperatur_Nachtabsenkung,Raumsolltemperatur_Tag,Raumsolltemperatur_Wochenende," +
                "Raumsolltemperatur_Ferien,Maximaleraumtemperatur,k_Wert_Auﬂenwand,k_Wert_Fenster,k_Wert_Dachflaeche,k_Wert_Grundflaeche,k_Wert_Sonstiges," +
                "Flaeche_Auﬂenwand,gesamte_Fensterflaeche,Dachflaeche,Grundflaeche,Sonstige_Flaechen,Wohnflaeche," +
                "Raumhoehe,WBVK_Anschluﬂ_Fenster_Wand,WBVK_Anschluﬂ_Wand_Dach,WBVK_Anschluﬂ_Auﬂenwand_Kellerdecke," +
                "Abmessung_Anschluﬂ_Fenster_Wand,Abmessung_Anschluﬂ_Wand_Dach,Abmessung_Anschluﬂ_Auﬂenwand_Kellerdecke," +
                "Luftwechselrate,Wochenende,Ferien,Ferienbeginn_1,Ferienende_1,Ferienbeginn_2,Ferienende_2," + 
                 "Ferienbeginn_3,Ferienende_3,Ferienbeginn_4,Ferienende_4,WW_Bedarf,spez_Waermeverbrauch,Waermebedarf," +
                 "Baualtersklasse,Gebaeudeart,Wohngebaeude_Nicht_Wohngebaeude from DBGebaeude where Gebaeudename='" + comboBox_Name.Text + "'", Program.DBConnection);
            
            adapter.Fill(ds,"DBGebaeude");

            try
            {
                DataTable table = ds.Tables["DBGebaeude"];
                DataRow dr = table.Rows[0];

                if(Init_DataRow(dr) == null) return;
            
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(ds, "DBGebaeude");

                MessageBox.Show("Geb‰ude Datensatz ist ‹berschrieben!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geb‰ude existiert in der DB nicht!\nWenn es ein neues Geb‰ude ist, Option 'Speichern unter' verwenden.");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return;
            }
        }

        private DataRow Init_DataRow(DataRow dr)
        {
            try
            {
                dr[1] = comboBox_Name.Text;
                dr[2] = comboBox_Gebaeudetyp.Text;
                dr[3] = textBox_Beschreibung.Text;
                
                double wfl = double.Parse(textBox_WohnflaecheGesamt.Text);
                dr[4] = wfl;
                
                double dWfNutzer = double.Parse(textBox_FlaecheNutzer.Text);
                dr[6] = dWfNutzer;
                if (dWfNutzer == 0) { dr[6] = 35; dWfNutzer = 35; }
                dr[5] = wfl / dWfNutzer; // Wohnfl‰che pro Nutzer
                
                dr[7] = double.Parse(textBox_Waermegewinne.Text);

                if (comboBox_Gebaeudeart.SelectedIndex == 0) dr[8] = wfl * 20;
                else if (comboBox_Gebaeudeart.SelectedIndex == 1) dr[8] = wfl * 50;
                else if (comboBox_Gebaeudeart.SelectedIndex == 2) dr[8] = wfl * 100;
                else dr[8] = 50;

                dr[9] = double.Parse(textBox_FFSued.Text);
                dr[10] = double.Parse(textBox_FFOstWest.Text);
                dr[11] = double.Parse(textBox_FFNord.Text);
                dr[12] = double.Parse(textBox_Fensterdurchlassgrad.Text);
   
                dr[13] = model.Raumsolltemperatur_Nachtabsenkung; 
                dr[14] = model.Raumsolltemperatur_Tag;
                dr[15] = model.Raumsolltemperatur_Wochenende;
                dr[16] = model.Raumsolltemperatur_Ferien;
                dr[17] = model.Maximaleraumtemperatur;

                dr[18] = double.Parse(textBox_UWert_Aussenwand.Text);
                dr[19] = double.Parse(textBox_UWert_Fenster.Text);
                dr[20] = double.Parse(textBox_UWert_Dachflaeche.Text);
                dr[21] = double.Parse(textBox_UWert_Grundflaeche.Text);
                dr[22] = double.Parse(textBox_UWert_Sonstige.Text);
                dr[23] = double.Parse(textBox_Flaeche_Aussenwand.Text);
                dr[24] = double.Parse(textBox_FFSued.Text) + 
                         double.Parse(textBox_FFOstWest.Text) +
                         double.Parse(textBox_FFNord.Text); // gesamte Fensterfl‰che
                dr[25] = double.Parse(textBox_Gebaeude_Dachflaeche.Text);
                dr[26] = double.Parse(textBox_Gebaeude_Grundflaeche.Text);
                dr[27] = double.Parse(textBox_Sonstige_Flaechen.Text);
                dr[28] = dr[4]; // Wohnfl‰che
                dr[29] = double.Parse(textBox_Raumhoehe.Text);
                
                dr[30] = model.Waermebrueckenverlustkoeffizient_Anschluﬂ_Fenster_Wand;
                dr[31] = model.Waermebrueckenverlustkoeffizient_Anschluﬂ_Wand_Dach;
                dr[32] = model.Waermebruckenverlustkoeffizient_Anschluﬂ_Auﬂenwand_Kellerdecke;
                
                dr[33] = model.Abmessung_Anschluﬂ_Fenster_Wand;
                dr[34] = model.Abmessung_Anschluﬂ_Wand_Dach;
                dr[35] = model.Abmessung_Anschluﬂ_Auﬂenwand_Kellerdecke;
                
                dr[36] = model.Luftwechselrate;
                dr[37] = model.Wochenende;
                
                dr[38] = model.Ferien;
                dr[39] = model.Ferienbeginn_1;
                dr[40] = model.Ferienende_1;
                dr[41] = model.Ferienbeginn_2;
                dr[42] = model.Ferienende_2;
                dr[43] = model.Ferienbeginn_3;
                dr[44] = model.Ferienende_3;
                dr[45] = model.Ferienbeginn_4;
                dr[46] = model.Ferienende_4;
                
                dr[47] = model.WW_Bedarf;
                dr[48] = model.spez_Waermeverbrauch;
                dr[49] = model.Waermebedarf;
      
                int index = comboBox_Baujahr.SelectedIndex;
                dr[50] = Convert.ToChar('A' + index);
                dr[51] = comboBox_Gebaeudeart.Text;
                dr[52] = comboBox_Verwendung.Text;

                return dr;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern!\nAlle Eingaben ¸berpr¸fen!");
                Console.WriteLine("Fehler beim Speichern der Daten: " + ex.Message);
                return null;
            }
        }

        private void btn_NeuerDatensatz_Click(object sender, EventArgs e)
        {
            if (comboBox_Name.Text == "") { MessageBox.Show("Geb‰udenamen eingeben!"); return; }   

            DataSet ds = new DataSet();
            OdbcDataAdapter adapter = new OdbcDataAdapter("select * from DBGebaeude where Gebaeudename='" + comboBox_Name.Text + "'", Program.DBConnection);
            adapter.Fill(ds, "DBGebaeude");

            DataTable table = ds.Tables["DBGebaeude"];
            DataRow newRow = table.NewRow();
            
            newRow = Init_DataRow(newRow);
            if (newRow == null) return;
            table.Rows.Add(newRow);
            
            OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);

            try
            {
                adapter.Update(ds, "DBGebaeude");
                MessageBox.Show("Geb‰ude ist gespeichert!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geb‰ude existiert in der DB bereits!");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return;
            }

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
    }
}
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
    public partial class Form_EingDBProzess : Form
    {
        public string m_szProzessname;
        public string m_szBeschreibung;
        public string m_szProzesstyp;
        public string mode;
        
        public Form_EingDBProzess()
        {
            InitializeComponent();

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Prozesstyp order by Typname");

            while(rs.Next())
            {
                comboBox_Prozesstyp.Items.Add(rs.Read("Typname"));
            }
            rs.Close(); 
 
        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Prozessname.Text = m_szProzessname;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Prozesstyp.Text = m_szProzesstyp;
            rs.Open("select * from Tab_Prozesswaerme where Prozessname='" + textBox_Prozessname.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = rs.Read("Monat_1").ToString();
                Wert2.Text = rs.Read("Monat_2").ToString();
                Wert3.Text = rs.Read("Monat_3").ToString();
                Wert4.Text = rs.Read("Monat_4").ToString();
                Wert5.Text = rs.Read("Monat_5").ToString();
                Wert6.Text = rs.Read("Monat_6").ToString();
                Wert7.Text = rs.Read("Monat_7").ToString();
                Wert8.Text = rs.Read("Monat_8").ToString();
                Wert9.Text = rs.Read("Monat_9").ToString();
                Wert10.Text = rs.Read("Monat_10").ToString();
                Wert11.Text = rs.Read("Monat_11").ToString();
                Wert12.Text = rs.Read("Monat_12").ToString();
            }
            rs.Close();

            if (mode == "Bearbeiten") btn_Speichern.Enabled = false;
            if (mode == "Neu")
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false; 
            }
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            
            for (int i = 1; i <= 12; i++)
            {
                string val = this.Controls["Wert" + i.ToString()].Text;
                if (!Program.checkDouble(this.Controls["Wert" + i.ToString()], val)) return;
            }
            
            OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Prozesswaerme where Prozessname = '" + m_szProzessname + "'", Program.DBConnection);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet, "test");

            DataRow row = dataSet.Tables["test"].Rows[0];

            for (int i = 1; i <= 12; i++)
            {
                 row["Monat_" + i.ToString()] = double.Parse(this.Controls["Wert" + i.ToString()].Text);
            }
            row["Typ"] = comboBox_Prozesstyp.Text;
            row["Beschreibung"] = textBox_Beschreibung.Text; 

            try
            {

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);

                adapter.Update(dataSet,"test");
                MessageBox.Show("Daten aktualisiert!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Aktualisieren der Daten!");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return;
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
             Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            
            frm.m_szName = textBox_Prozessname.Text;
            frm.SetControl();
            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                RecordSet rs = new RecordSet();
                rs.Open("select Prozessname from Tab_Prozesswaerme where prozessname='" + frm.m_szName + "'");
                if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); return; }
                rs.Close(); 
                textBox_Prozessname.Text = frm.m_szName;

                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Prozesswaerme", Program.DBConnection);
                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet, "test");

                DataRow newRow = dataSet.Tables["test"].NewRow();

                newRow["Prozessname"] = textBox_Prozessname.Text;
                newRow["Beschreibung"] = textBox_Beschreibung.Text;
                newRow["Typ"] = comboBox_Prozesstyp.Text;

                for (int i = 1; i <= 12; i++)
                {
                    newRow["Monat_" + i.ToString()] = double.Parse(this.Controls["Wert" + i.ToString()].Text);
                }

                dataSet.Tables["test"].Rows.Add(newRow);
                
                try
                {
                    OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);

                    adapter.Update(dataSet, "test");
                    MessageBox.Show("Daten gespeichert!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Aktualisieren der Daten!");
                    Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                    return;
                }


            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if(comboBox_Prozesstyp.Text == "" )
            {
                MessageBox.Show("Prozesstyp auswählen!");
                return;
            }
            for (int i = 1; i <= 12; i++)
            {
                if (this.Controls["Wert" + i.ToString()].Text == "")
                {
                    MessageBox.Show("Eingaben überprüfen!");
                    return;
                }
            }

            OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Prozesswaerme", Program.DBConnection);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet, "test");

            DataRow newRow = dataSet.Tables["test"].NewRow();

            newRow["Prozessname"] = textBox_Prozessname.Text;
            newRow["Beschreibung"] = textBox_Beschreibung.Text;
            newRow["Typ"] = comboBox_Prozesstyp.Text;

            for (int i = 1; i <= 12; i++)
            {
                newRow["Monat_" + i.ToString()] = double.Parse(this.Controls["Wert" + i.ToString()].Text);
            }

            dataSet.Tables["test"].Rows.Add(newRow);

            try
            {
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);

                adapter.Update(dataSet, "test");
                MessageBox.Show("Daten gespeichert!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Aktualisieren der Daten!");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
            }
        }

 
  
    }
}
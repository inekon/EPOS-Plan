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
    partial class Form_Prozesswaerme_Admin : Form
    {
        private ProzesswaermeModel model = new ProzesswaermeModel();
        private ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
        public List<Z_ProjektProzesswaermeModel> list_pwmodel = new List<Z_ProjektProzesswaermeModel>();
        public int m_ID_Projekt = 0;
        private SimulationGebaeude simulation = new SimulationGebaeude();
        public bool m_bAdmin = false;
        public string m_szProjekt = "";

        public Form_Prozesswaerme_Admin()
        {
            InitializeComponent();
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            ProzesswaermeCtrl ctrl_pw = new ProzesswaermeCtrl();
            Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();

            m_szProjekt = szProjekt; 
            listBox_Prozess_DB.Items.Clear();
            ctrl_pw.ReadAll();
            
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                listBox_Prozess_DB.Items.Add(ctrl_pw.items[i].m_szProzessname);
            }
            if (listBox_Prozess_DB.Items.Count > 0) listBox_Prozess_DB.SelectedIndex = 0;
   
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox list = (ListBox)sender;
            string szName = list.Text;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString();
            SetProzessInfo(szName);
        }

        private void SetProzessInfo(string szName)
        {
            ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Prozess_Name.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Prozess_Type.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
            ctrl.ReadSingle(szName);

            double summe = 0;
            if (ctrl.rows > 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    summe += ctrl.m_Monat[i];
                }
            }
            return summe;  
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; 
            Close();
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            simulation.m_ID_Projekt = m_ID_Projekt;  
            simulation.Prozesswaerme_berechnen(listBox_Prozess_DB.Text );
            simulation.Waermebedarf_Prozess = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.com.I_monats_summe(simulation.prozesswerte, simulation.Waermebedarf_monat, simulation.mo_anfang, simulation.mo_ende);

            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1); 
            frm.ShowDialog();
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1);
            frm.ShowDialog(); 
        }

        private void btn_Prozess_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            frm.m_szProzessname = textBox_Prozess_Name.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szProzesstyp = textBox_Prozess_Type.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt); 
        }

        private void btn_Prozess_loeschen_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Prozess_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return; 

            OdbcCommand DBCommand = Program.DBConnection.CreateCommand();
            try
            {
                DBCommand.CommandText = "DELETE Prozessname FROM Tab_Prozesswaerme WHERE Prozessname='" + listBox_Prozess_DB.Text + "'";
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
            }
            listBox_Prozess_DB.Items.Remove(listBox_Prozess_DB.Text);    
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();
            
            frm_item.ShowDialog();
            
            if (frm_item.result == DialogResult.OK)
            {
                frm.m_szProzessname = frm_item.m_szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.ShowDialog();
                SetControls(m_szProjekt);
            }
        }

        private void btn_ProzTypeDBedit_Click(object sender, EventArgs e)
        {
            Form_EingProzTyp frm = new Form_EingProzTyp();
            frm.SetControls(); 
            frm.ShowDialog(); 
        }
  
    }
}
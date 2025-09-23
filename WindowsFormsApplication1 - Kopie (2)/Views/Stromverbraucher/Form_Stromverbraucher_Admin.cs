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
    partial class Form_Stromverbraucher_Admin : Form
    {
        private SimulationStrombedarf simulation = new SimulationStrombedarf();
        private StromverbraucherModel model = new StromverbraucherModel();
        private StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
        public List<Z_ProjektStromverbraucherModel> list_pwmodel = new List<Z_ProjektStromverbraucherModel>();
        
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public bool m_bAdmin = false;
        
        public Form_Stromverbraucher_Admin()
        {
            InitializeComponent();
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektStromverbraucherModel ctrl = new Z_ProjektStromverbraucherModel();
            StromverbraucherCtrl ctrl_pw = new StromverbraucherCtrl();
            Z_ProjektStromverbraucherModel model = new Z_ProjektStromverbraucherModel();

            m_szProjekt = szProjekt; 
            listBox_Verbraucher_DB.Items.Clear();
            ctrl_pw.ReadAll();
            
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                listBox_Verbraucher_DB.Items.Add(ctrl_pw.items[i].m_szBezeichner);
            }
            if (listBox_Verbraucher_DB.Items.Count > 0) listBox_Verbraucher_DB.SelectedIndex = 0;
   
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox list = (ListBox)sender;
            string szName = list.Text;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString("F2");
            SetProzessInfo(szName);
        }

        private void SetProzessInfo(string szName)
        {
            StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Name.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Type.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
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
            List<string> list = new List<string>();
            list.Add(listBox_Verbraucher_DB.Text); 
            simulation.Strombedarf_berechnen(list);
            simulation.Strombedarf_Gebaeude_gesamt = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.com.CSharp_I_vectoren_addieren(simulation.prozesswerte, simulation.Strombedarf);
            simulation.com.I_monats_summe(simulation.Strombedarf, simulation.Strombedarf_monat, simulation.mo_anfang, simulation.mo_ende);
            simulation.Strombedarf_Max = simulation.Maximaler_Strombedarf(simulation.Strombedarf);
            simulation.Strombedarf_gesamt = simulation.Strombedarf_Gebaeude_gesamt;
            
            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation);
            frm.SetPage(1); 
            frm.ShowDialog();
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation);
            frm.SetPage(1);
            frm.ShowDialog(); 
        }

        private void btn_Prozess_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            frm.m_szStromname = textBox_Name.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szStromtyp = textBox_Type.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt); 
        }

        private void btn_Prozess_loeschen_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Verbraucher_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return; 

            OdbcCommand DBCommand = Program.DBConnection.CreateCommand();
            try
            {
                DBCommand.CommandText = "DELETE Bezeichner FROM Tab_Stromverbraucher WHERE Bezeichner='" + listBox_Verbraucher_DB.Text + "'";
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
            listBox_Verbraucher_DB.Items.Remove(listBox_Verbraucher_DB.Text);    
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();
            
            frm_item.ShowDialog();
            
            if (frm_item.result == DialogResult.OK)
            {
                frm.m_szStromname = frm_item.m_szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.ShowDialog();
                SetControls(m_szProjekt);
            }
        }

        private void btn_ProzTypeDBedit_Click(object sender, EventArgs e)
        {
            Form_EingStromTyp frm = new Form_EingStromTyp();
            frm.SetControls(); 
            frm.ShowDialog(); 
        }

 
  
    }
}
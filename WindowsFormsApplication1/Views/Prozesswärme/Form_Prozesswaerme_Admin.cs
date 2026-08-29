using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Prozesswaerme_Admin : Form
    {
        private ProzesswaermeModel model = new ProzesswaermeModel();
        private ProzesswaermeStammCtrl ctrl = new ProzesswaermeStammCtrl();
        public List<Z_ProjektProzesswaermeModel> list_pwmodel = new List<Z_ProjektProzesswaermeModel>();
        public int m_ID_Projekt = 0;
        private SimulationWaermebedarf simulation = new SimulationWaermebedarf();
        public bool m_bAdmin = false;
        public string m_szProjekt = "";

        public Form_Prozesswaerme_Admin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            ProzesswaermeStammCtrl ctrl_pw = new ProzesswaermeStammCtrl();
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
            ProzesswaermeStammCtrl ctrl = new ProzesswaermeStammCtrl();
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
            ProzesswaermeStammCtrl ctrl = new ProzesswaermeStammCtrl();
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
            List<string> list = new List<string>();
            list.Add(listBox_Prozess_DB.Text);
            simulation.Prozesswaerme_berechnen(list);
            //simulation.Waermebedarf_Prozess = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.Waermebedarf_Prozess = simulation.prozesswerte.Sum() / 1000;
            //simulation.com.I_monats_summe(simulation.prozesswerte, simulation.Waermebedarf_Prozess_Monat, simulation.mo_anfang, simulation.mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(simulation.prozesswerte, simulation.Waermebedarf_Prozess_Monat, simulation.mo_anfang, simulation.mo_ende);

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
            if (string.IsNullOrEmpty(listBox_Prozess_DB.Text))
            {
                MessageBox.Show("Bitte wählen Sie einen Prozess aus, den Sie löschen möchten.");
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Prozess_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            try
            {
                // Parameterbasierte DELETE-Abfrage (schützt vor SQL-Injections und Sonderzeichen)
                ProzesswaermeStammCtrl ctrlDel = new ProzesswaermeStammCtrl();

                // Direkt über das DataRepository ausführen
                if (ctrlDel.Delete(listBox_Prozess_DB.Text))
                {
                    // Erst wenn es in der DB gelöscht wurde, aus der ListBox entfernen
                    listBox_Prozess_DB.Items.Remove(listBox_Prozess_DB.Text);
                    MessageBox.Show("Prozess erfolgreich gelöscht.");
                }
                else
                {
                    MessageBox.Show("Der Prozess konnte nicht aus der Datenbank gelöscht werden.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen des Prozesses: " + ex.Message);
                MessageBox.Show("Fehler beim Löschvorgang!");
            }
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();
            
            if (frm_item.ShowDialog() == DialogResult.OK)
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

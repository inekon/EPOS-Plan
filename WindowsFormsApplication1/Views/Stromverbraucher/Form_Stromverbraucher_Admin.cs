using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Stromverbraucher_Admin : Form
    {
        private SimulationStrombedarf simulation = new SimulationStrombedarf();
        private StromverbraucherModel model = new StromverbraucherModel();
        private StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
        public List<Z_ProjektStromverbraucherModel> list_pwmodel = new List<Z_ProjektStromverbraucherModel>();
        
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public bool m_bAdmin = false;
        
        public Form_Stromverbraucher_Admin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektStromverbraucherModel ctrl = new Z_ProjektStromverbraucherModel();
            StromverbraucherStammCtrl ctrl_pw = new StromverbraucherStammCtrl();
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
            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
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
            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
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
            float[] result = new float[8760];

            list.Add(listBox_Verbraucher_DB.Text);
            result = simulation.Stromprofil_Strombedarf_berechnen(list);
            if (result == null) return;
            //simulation.Strombedarf_Gebaeude_gesamt = simulation.com.I_vector_summe(result);
            simulation.Strombedarf_Gebaeude_gesamt = result.Sum() / 1000;

            //simulation.com.CSharp_I_vectoren_addieren(simulation.prozesswerte, simulation.Strombedarf_viertelStundenwerte);
            Array.Copy(result, simulation.Strombedarf_viertelStundenwerte, result.Length);

            //simulation.com.I_monats_summe(simulation.Strombedarf_viertelStundenwerte, simulation.Strombedarf_monat, simulation.mo_anfang, simulation.mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(simulation.Strombedarf_viertelStundenwerte, simulation.Strombedarf_monat, simulation.mo_anfang, simulation.mo_ende);
            simulation.Strombedarf_Max = simulation.Maximaler_Strombedarf(simulation.Strombedarf_viertelStundenwerte);
            simulation.Strombedarf_gesamt = simulation.Strombedarf_Gebaeude_gesamt;
            
            // iU9-W8.2: Blazor-Huelle statt Form_ErgStromverbraucher (Reiter 1 = monatlich).
            BedarfErgebnisHuelle.Zeigen(this, simulation, 1);
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            // iU9-W8.2: Blazor-Huelle statt Form_ErgStromverbraucher (Reiter 1 = monatlich).
            BedarfErgebnisHuelle.Zeigen(this, simulation, 1);
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
            // Sicherheitsabfrage, ob überhaupt etwas selektiert ist
            if (string.IsNullOrEmpty(listBox_Verbraucher_DB.Text))
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Verbraucher aus!");
                return;
            }

            DialogResult dialogResult = MessageBox.Show(
                $"Soll {listBox_Verbraucher_DB.Text} wirklich gelöscht werden ?",
                "Löschen",
                MessageBoxButtons.YesNo
            );

            if (dialogResult == DialogResult.No) return;

            // Delete prueft selbst auf ReadOnly und meldet ggf.
            StromverbraucherStammCtrl ctrl_del = new StromverbraucherStammCtrl();
            if (!ctrl_del.Delete(listBox_Verbraucher_DB.Text)) return;
            listBox_Verbraucher_DB.Items.Remove(listBox_Verbraucher_DB.Text);
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                frm.m_szStromname = szName;
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

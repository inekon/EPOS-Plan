using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Brauchwasser_Admin : Form
    {
        private BrauchwasserModel model = new BrauchwasserModel();
        public List<Z_ProjektProzesswaermeModel> list_pwmodel = new List<Z_ProjektProzesswaermeModel>();
        public int m_ID_Projekt = 0;
        private SimulationWaermebedarf simulation = new SimulationWaermebedarf();
        public bool m_bAdmin = false;
        public string m_szProjekt = "";

        public Form_Brauchwasser_Admin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            BrauchwasserStammCtrl ctrl_pw = new BrauchwasserStammCtrl();
            Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();

            m_szProjekt = szProjekt;
            listBox_DB.Items.Clear();
            ctrl_pw.ReadAll();

            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                listBox_DB.Items.Add(ctrl_pw.items[i].m_szBezeichner);
            }
            if (listBox_DB.Items.Count > 0) listBox_DB.SelectedIndex = 0;

        }

        private void SetProzessInfo(string szName)
        {
            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
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
            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
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

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            simulation.m_ID_Projekt = m_ID_Projekt;
            List<string> list = new List<string>();
            list.Add(listBox_DB.Text);
            simulation.Brauchwasserwaerme_berechnen(list);
            simulation.Waermebedarf_Brauchwasser = simulation.brauchwasserwerte.Sum();
            //simulation.com.I_monats_summe(simulation.brauchwasserwerte, simulation.Waermebedarf_Brauchwasser_Monat, simulation.mo_anfang, simulation.mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(simulation.brauchwasserwerte, simulation.Waermebedarf_Brauchwasser_Monat, simulation.mo_anfang, simulation.mo_ende);

            Form_ErgBrauchwasserwaerme frm = new Form_ErgBrauchwasserwaerme();
            frm.Init(simulation);
            frm.SetPage(2);
            frm.ShowDialog();
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1);
            frm.ShowDialog();
        }

        private void btn_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBBrauchwasser frm = new Form_EingDBBrauchwasser();
            frm.m_szBezeichner = textBox_Prozess_Name.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szBrauchwassertyp = textBox_Prozess_Type.Text;
            frm.mode = "Bearbeiten";
            Point p1 = btn_DBedit.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt);
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            BrauchwasserStammCtrl ctrl_del = new BrauchwasserStammCtrl();
            // Delete prueft selbst auf ReadOnly und meldet ggf.
            if (!ctrl_del.Delete(listBox_DB.Text)) return;
            listBox_DB.Items.Remove(listBox_DB.Text);
        }

        private void btn_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBBrauchwasser frm = new Form_EingDBBrauchwasser();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();

            Point p1 = btn_DBneu.Location;
            p1 = this.PointToScreen(p1);
            frm_item.Location = p1;

            if (frm_item.ShowDialog() == DialogResult.OK)
            {
                frm.m_szBezeichner = frm_item.m_szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.ShowDialog();
                SetControls(m_szProjekt);
            }
        }

        private void btn_TypeDBedit_Click(object sender, EventArgs e)
        {
            Form_EingBrauchwasserTyp frm = new Form_EingBrauchwasserTyp();

            Point p1 = btn_TypeDBedit.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            frm.SetControls();
            frm.ShowDialog();
        }

        private void listBox_DB_Click(object sender, EventArgs e)
        {
            ListBox list = (ListBox)sender;
            string szName = list.Text;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString("F3");
            SetProzessInfo(szName);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void listBox_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox_DB_Click(sender, e);
        }

    }
}

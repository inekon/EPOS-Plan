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
    public partial class Wizard_Stromprofil : Form
    {
        public string m_szProjekt;
        private WizardParent wizardparent;
        Form_Stromverbraucher frm = new Form_Stromverbraucher();

        public Wizard_Stromprofil()
        {
            InitializeComponent();
        }

        public void SetControls(string szProjekt)
        {
            m_szProjekt = szProjekt;
            frm.list_sbmodel = wizardparent.list_stromverbrauchermodel;
            SetListBox();
            textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
        }

        private void SetListBox()
        {
            listBox_Strom_Auswahl.Items.Clear();
            
            for (int i=0; i<frm.list_sbmodel.Count; i++)
            {
                listBox_Strom_Auswahl.Items.Add(frm.list_sbmodel[i].m_szVerbraucher);
            }
            if(frm.list_sbmodel.Count > 0) listBox_Strom_Auswahl.SelectedIndex = 0; 
        }

        private void Wizard_Stromprofil_Load(object sender, EventArgs e)
        {
            wizardparent = (WizardParent)getWizardPage();
        }

        private Form getWizardPage()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "WizardParent")
                {
                    return form;
                }
            }
            return null;
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            frm.SetControls(m_szProjekt);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizardparent.list_stromverbrauchermodel = frm.list_sbmodel;
                SetListBox();
                textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
            }
        }

        private void listBox_Strom_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
            ctrl.ReadSingle(listBox_Strom_Auswahl.Text);

            if (ctrl.rows > 0)
            {
                textBox_Name.Text = listBox_Strom_Auswahl.Text;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Stromtyp.Text = ctrl.m_szTyp;
                textBox_Jahres_Verbrauch.Text = Prozesssumme(textBox_Name.Text).ToString("F2");
            }
        }

        private double Prozesssumme(string szName)
        {
            StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
            ctrl.ReadSingle(szName);

            double summe = 0;
            if (ctrl.rows > 0)
            {
                for (int i=0; i<12; i++)
                {
                    summe += ctrl.m_Monat[i];
                }
            }
            return summe;
        }

        private double ProzesssummeGesamt()
        {
            double summe = 0;

            for (int i=0; i<listBox_Strom_Auswahl.Items.Count; i++)
            {
                summe += Prozesssumme(listBox_Strom_Auswahl.Items[i].ToString());
            }
            return summe;
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            for (int i=0; i<wizardparent.list_stromverbrauchermodel.Count; i++)
            {
                if (wizardparent.list_stromverbrauchermodel[i].m_szVerbraucher == listBox_Strom_Auswahl.Text) wizardparent.list_stromverbrauchermodel.RemoveAt(i);
            }
            listBox_Strom_Auswahl.Items.RemoveAt(listBox_Strom_Auswahl.SelectedIndex);
            frm.list_sbmodel = wizardparent.list_stromverbrauchermodel;
        }
    }
}

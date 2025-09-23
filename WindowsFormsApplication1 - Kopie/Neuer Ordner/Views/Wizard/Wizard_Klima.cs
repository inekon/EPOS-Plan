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
    public partial class Wizard_Klima : Form
    {
        public int ID_Klimaregion;

        public Wizard_Klima()
        {
            InitializeComponent();
            FillKlimaList();
        }
 
        public void FillKlimaList()
        {
            KlimaregionCtrl ctrl = new KlimaregionCtrl();

            ctrl.ReadAll();
            listBox_Klima.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Klima.Items.Add(ctrl.items[i].m_szName);
            }

        }

        public string GetKlimaname()
        {
            return listBox_Klima.Text;
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

        public void SetControls(string projekt)
        {
            WizardParent wizardparent = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                KlimaregionCtrl klimactrl = new KlimaregionCtrl();
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + projekt + "'");
                klimactrl.ReadSingle("select * from Tab_Klimaregion where ID_Klimaregion=" + projctrl.m_ID_Klimaregion);
                if (klimactrl.rows > 0)
                {
                    listBox_Klima.Text = klimactrl.m_szName;
                }
            }
        }

        private void listBox_Klima_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion where Name = '" + listBox_Klima.Text + "'");
            
            if(rs.Next())
            {
                ID_Klimaregion = (int)rs.Read("ID_Klimaregion"); 
            }
            rs.Close();
        }

        public int GetIDKlimaregion() { return ID_Klimaregion; }
 
    }
    
}

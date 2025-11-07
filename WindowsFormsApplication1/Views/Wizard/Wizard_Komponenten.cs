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
    public partial class Wizard_Komponenten : Form
    {
        public Form parentForm;

        public Wizard_Komponenten()
        {
            InitializeComponent();
            parentForm = null;
        }

        public bool GetWPStatus() { return checkBox_WP.Checked; }
        public bool GetSolarStatus() { return checkBox_Solar.Checked; }
        public bool GetPVStatus() { return checkBox_PV.Checked; }
        public bool GetStromspeicherStatus() { return checkBox_StromSp.Checked; }
        public bool GetProzessStatus() { return checkBox_Prozess.Checked; }
 
 
        private void Wizard_Komponenten_Shown(object sender, EventArgs e)
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "WizardParent")
                {
                    parentForm = form;
                    break;
                }
            }

        }

        public void SetWPCheckBox(bool value)
        {
            checkBox_WP.Checked = value;
        }
        
        public void SetPVPCheckBox(bool value)
        {
            checkBox_PV.Checked = value;
        }
        
        public void SetSolarCheckBox(bool value)
        {
            checkBox_Solar.Checked = value;
        }

        public void SetStromSpCheckBox(bool value)
        {
            checkBox_StromSp.Checked = value;
        }

        public void SetProzessCheckBox(bool value)
        {
            checkBox_Prozess.Checked = value;
        }

        public void SetStromglastgangCheckBox(bool value)
        {
            checkBox_Stromlastgang.Checked = value;
        }

        public void SetKesselCheckBox(bool value)
        {
            checkBox_Kessel.Checked = value;
        }

        public void SetWBedarfDatenCheckBox(bool value)
        {
            checkBox_WBedarfDaten.Checked = value;
        }

        public void SetGebaeudeCheckBox(bool value)
        {
            checkBox_Gebaeude.Checked = value;
        }
        public void SetReferenzCheckBox(bool value)
        {
            checkBox_Referenz.Checked = value;
        }

        public void SetStromprofilCheckBox(bool value)
        {
            checkBox_StdStromprofil.Checked = value;
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.WP_ITEM);
            wizard.aktiv = checkBox_WP.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.WP_ITEM] = wizard;
        }
        
        private void checkBox_Solar_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.SOLAR_ITEM);
            wizard.aktiv = checkBox_Solar.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.SOLAR_ITEM] = wizard;
        }

        private void checkBox_PV_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.PV_ITEM);
            wizard.aktiv = checkBox_PV.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.PV_ITEM] = wizard;
        }

        private void checkBox_StromSp_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.SP_ITEM);
            wizard.aktiv = checkBox_StromSp.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.SP_ITEM] = wizard;
        }

        private void checkBox_Prozess_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.PROZESS_ITEM);
            wizard.aktiv = checkBox_Prozess.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.PROZESS_ITEM] = wizard;
        }

        private void checkBox_Stromlastgang_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.STROMLASTGANG_ITEM);
            wizard.aktiv = checkBox_Stromlastgang.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.STROMLASTGANG_ITEM] = wizard;
        }

        private void checkBox_Kessel_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.KESSEL_ITEM);
            wizard.aktiv = checkBox_Kessel.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.KESSEL_ITEM] = wizard;
        }

        private void checkBox_Gebaeude_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.GEBAEUDE_ITEM);
            wizard.aktiv = checkBox_Gebaeude.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.GEBAEUDE_ITEM] = wizard;
        }

        private void checkBox_Referenz_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.REFERENZ_ITEM);
            wizard.aktiv = checkBox_Referenz.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.REFERENZ_ITEM] = wizard;
        }

        private void checkBox_WBedarfDaten_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.WAERMEBEDARF_ITEM);
            wizard.aktiv = checkBox_WBedarfDaten.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.WAERMEBEDARF_ITEM] = wizard;
        }

        private void checkBox_StdStromprofil_CheckedChanged(object sender, EventArgs e)
        {
            WizardItemClass wizard;
            wizard = ((WizardParent)parentForm).listPages.ElementAt(WizardItemClass.STROMSTD_ITEM);
            wizard.aktiv = checkBox_StdStromprofil.Checked;
            ((WizardParent)parentForm).listPages[WizardItemClass.STROMSTD_ITEM] = wizard;
        }
    }
}

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
    public partial class Form_GebWohnflaeche : Form
    {
        public Z_ProjGebModel ctrl = new Z_ProjGebModel();
        public Form_GebWohnflaeche()
        {
            InitializeComponent();
        }

        public void SetControls()
        {
            txt_Baujahr.Text = ctrl.Baualtersklasse;
            txt_Gebaeudename.Text = ctrl.Gebaeudename;
            txt_Beschreibung.Text = ctrl.Beschreibung;
            txt_Gebaeudeart.Text = ctrl.Gebaeudeart;
            txt_Verbrauch.Text = ctrl.Wohnflaeche.ToString();
            txt_Einheit.Text = ctrl.Einheit.Substring(ctrl.Einheit.IndexOf('[')+1) ;
            txt_Einheit.Text = txt_Einheit.Text.Substring(0, txt_Einheit.Text.Length - 1); 
            txt_Bedarfsart_Auswahl.Text = ctrl.Einheit;
            Jahresnutzungsgrad.Text = ctrl.Jahresnutzungsgrad.ToString();
            D_BW.Checked = ctrl.DezentralWarmwasser;
            cmb_Bedarfsart.Items.Add("Ölverbrauch [l/a]"); 
            cmb_Bedarfsart.Items.Add("Gasverbrauch [m³/a]"); 
            cmb_Bedarfsart.Items.Add("Gasverbrauch [MWh/a] (Ho)"); 
            cmb_Bedarfsart.Items.Add("Brennstoffverbrauch [MWh/a]"); 
            cmb_Bedarfsart.Items.Add("Verbrauch  [MWh/a]"); 
            cmb_Bedarfsart.Items.Add("Wohnfläche [m²]");
            cmb_Bedarfsart.Text = ctrl.Einheit; 

        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            ctrl.Wohnflaeche = Int32.Parse(txt_Verbrauch.Text);
            ctrl.Jahresnutzungsgrad = double.Parse(Jahresnutzungsgrad.Text);
            ctrl.Einheit = txt_Bedarfsart_Auswahl.Text;   
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void cmb_Bedarfsart_SelectedIndexChanged(object sender, EventArgs e)
        {
            txt_Bedarfsart_Auswahl.Text = cmb_Bedarfsart.Text;
            txt_Einheit.Text = cmb_Bedarfsart.Text.Substring(cmb_Bedarfsart.Text.IndexOf('[') + 1);
            txt_Einheit.Text = txt_Einheit.Text.Substring(0, txt_Einheit.Text.Length - 1); 
            
        }


    }
}
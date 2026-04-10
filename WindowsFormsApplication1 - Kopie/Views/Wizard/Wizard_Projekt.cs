using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public partial class Wizard_Projekt : Form
    {
        public int m_ID_Klimaregion = 0;

        public Wizard_Projekt()
        {
            InitializeComponent();
        }

        public void SetProjektbezeichner(String Projektname)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            if (Projektname != "")
            {
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + Projektname + "' order by Projektname");
                textBox_Name.Text = Projektname;
                textBox_Bearbeiter.Text = projctrl.m_szBearbeiter;
                textBox_Beschreibung.Text = projctrl.m_szBeschreibung;
                textBox_Kunde.Text = projctrl.m_szKunde;
                textBox_Aenderungsdatum.Text = projctrl.m_Aenderungsdatum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                textBox_Erstelldatum.Text = projctrl.m_Erstelldatum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                m_ID_Klimaregion = projctrl.m_ID_Klimaregion;
                RecordSet rs = new RecordSet();
                rs.Open("select * from Tab_Klimaregion where ID_Klimaregion=" + m_ID_Klimaregion);
                if (rs.Next())
                {
                    comboBox_Gebaeude.Text = (string)rs.Read("Name");
                }
                rs.Close();

            }
            else
            {
                textBox_Aenderungsdatum.Text = DateTime.Now.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                textBox_Erstelldatum.Text = DateTime.Now.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
            }
            projctrl = null;
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Gebaeude.Items.Add(ctrl.items[i].m_szName);   
            }
        }

        public void SetEditProjektName(bool value) { textBox_Name.Enabled = value; }
        public string GetProjektName() { return textBox_Name.Text; }
        public string GetBeschreibung() { return textBox_Beschreibung.Text; }
        public string GetBearbeiter() { return textBox_Bearbeiter.Text; }
        public string GetKunde() { return textBox_Kunde.Text; }
        public DateTime GetDatum() { return DateTime.Now ; }
        public DateTime GetErstellDatum() { return DateTime.Parse(textBox_Erstelldatum.Text); }
        public int GetIDKlimaregion() { return m_ID_Klimaregion; }
        public string GetKlimaname() { return comboBox_Gebaeude.Text; }

        private void comboBox_Gebaeude_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion where Name='" + comboBox_Gebaeude.Text + "'");
            if (rs.Next())
            {
                m_ID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();
        }
    }
}

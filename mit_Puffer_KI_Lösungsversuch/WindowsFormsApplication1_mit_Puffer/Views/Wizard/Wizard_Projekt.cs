using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using Json.Schema.Generation.Intents;

namespace WindowsFormsApplication1
{
    public partial class Wizard_Projekt : Form
    {
        public int m_ID_Klimaregion = 0;

        public Wizard_Projekt()
        {
            InitializeComponent();
            comboBox_Klima.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox_Klima.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        public void SetProjektbezeichner(String Projektname)
        {
            // Klimaregion-Auswahl (Namen aus den Stammdaten) ZUERST befuellen, damit ein
            // anschliessend gesetzter Text auch bei DropDownList sicher angezeigt wird.
            comboBox_Klima.Items.Clear();
            KlimaregionStammCtrl ctrl = new KlimaregionStammCtrl();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Klima.Items.Add(ctrl.items[i].m_szName);
            }

            ProjektCtrl projctrl = new ProjektCtrl();
            if (Projektname != "")
            {
                projctrl.ReadSingle(Projektname);
                textBox_Name.Text = Projektname;
                textBox_Bearbeiter.Text = projctrl.m_szBearbeiter;
                textBox_Beschreibung.Text = projctrl.m_szBeschreibung;
                textBox_Kunde.Text = projctrl.m_szKunde;
                textBox_Aenderungsdatum.Text = projctrl.m_Aenderungsdatum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                textBox_Erstelldatum.Text = projctrl.m_Erstelldatum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                m_ID_Klimaregion = projctrl.m_ID_Klimaregion;

                // Regionsnamen anzeigen. Neue Speicherweise: ID der Projekt-Kopie (Tab_Klimaregion.ID,
                // auf dieses Projekt eingeschraenkt). Fallback: aeltere Projekte mit STAMM-ID.
                if (m_ID_Klimaregion != 0)
                {
                    string szName = "";
                    RecordSet rs = new RecordSet();
                    rs.Open("select * from Tab_Klimaregion where ID=" + m_ID_Klimaregion + " and ID_Projekt=" + projctrl.m_ID);
                    if (rs.Next())
                    {
                        szName = (string)rs.Read("Bezeichner");
                    }
                    rs.Close();

                    if (szName == "")
                    {
                        rs.Open("select * from Tab_Klimaregion_STAMM where ID_Klimaregion=" + m_ID_Klimaregion);
                        if (rs.Next())
                        {
                            szName = (string)rs.Read("Name");
                        }
                        rs.Close();
                    }

                    comboBox_Klima.Text = szName;
                }
            }
            else
            {
                textBox_Aenderungsdatum.Text = DateTime.Now.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
                textBox_Erstelldatum.Text = DateTime.Now.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
            }
            projctrl = null;
        }

        public void SetEditProjektName(bool value) { textBox_Name.Enabled = value; }
        public string GetProjektName() { return textBox_Name.Text; }
        public string GetBeschreibung() { return textBox_Beschreibung.Text; }
        public string GetBearbeiter() { return textBox_Bearbeiter.Text; }
        public string GetKunde() { return textBox_Kunde.Text; }
        public DateTime GetDatum() { return DateTime.Now ; }
        public DateTime GetErstellDatum() { return DateTime.Parse(textBox_Erstelldatum.Text); }
        public int GetIDKlimaregion() { return m_ID_Klimaregion; }
        public string GetKlimaname() { return comboBox_Klima.Text; }

        private void comboBox_Klima_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion_STAMM where Name='" + comboBox_Klima.Text + "'");
            if (rs.Next())
            {
                m_ID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();
        }
    }
}

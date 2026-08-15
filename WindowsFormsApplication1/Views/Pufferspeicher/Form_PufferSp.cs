using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private PufferSpStammCtrl pufferspctrl = new PufferSpStammCtrl();
        public List<WErzeugerModel> list_pufferspmodel = new List<WErzeugerModel>();

        // Befund 4: Parallelliste zur LINKEN ListBox (Projektauswahl).
        // list_pufferspmodel enthaelt im Wizard-Modus ALLE Erzeugertypen, die ListBox zeigt
        // aber nur die PUFFER_TYP-Eintraege - der ListBox-Index passt also nicht auf die
        // Modell-Liste. Ausserdem sind Projektkopien nicht ueber ihren Namen auffindbar
        // (sie heissen z.B. "... 600 Liter" statt "... 600 Ltr" und duerfen im selben
        // Projekt doppelt vorkommen). Diese Liste haelt zu jedem ListBox-Index das
        // zugehoerige Modell und damit dessen ID_PUFFER.
        private List<WErzeugerModel> _linkeListe = new List<WErzeugerModel>();

        public int m_nType = WizardItemClass.PUFFER_TYP;
        public int m_ID_Projekt = 0;
        int startindex = 100000;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;

        public Form_PufferSp ()
        {
            InitializeComponent();
            listBox_Pufferspeicher_DB.Items.Clear();
            listBox_Pufferspeicher.Items.Clear();
        }

        public void SetControls(int IDProjekt, bool bWizard = false)
        {
            m_ID_Projekt = IDProjekt;
            if (bWizard)
            {
                m_bWizard = true;
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_pufferspmodel = wizardparent.list_werzmodel;
            }
            listBox_Pufferspeicher.Items.Clear();
            _linkeListe.Clear();
            for (int i = 0; i < list_pufferspmodel.Count; i++)
            {
                if (list_pufferspmodel[i].ID_Type == WizardItemClass.PUFFER_TYP)
                {
                    listBox_Pufferspeicher.Items.Add(list_pufferspmodel[i].Bezeichner);
                    _linkeListe.Add(list_pufferspmodel[i]);
                }
            }
            if (listBox_Pufferspeicher.Items.Count > 0) listBox_Pufferspeicher.SelectedIndex = 0;
        }

        private void Form_PufferSp_Load(object sender, EventArgs e)
        {
            pufferspctrl.ReadAll();
            for (int i = 0; i < pufferspctrl.rows; i++)
            {
                listBox_Pufferspeicher_DB.Items.Add(pufferspctrl.items[i].Name);
            }

            pufferspctrl.ReadAll();
            for (int i = 0; i < pufferspctrl.rows; i++)
            {
                if (comboBox_Hersteller.FindStringExact(pufferspctrl.items[i].Firma) == -1) comboBox_Hersteller.Items.Add(pufferspctrl.items[i].Firma);
            }

            PufferSpFilter.VolumenfilterFuellen(comboBox_Volumen);
            PufferSpFilter.HerstellerfilterVorbelegen(comboBox_Hersteller);
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

        private void btn_PufferSp_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
           
            if (listBox_Pufferspeicher_DB.Text == "") return;

            rs.Open("select * from Tab_Pufferspeicher_STAMM where Bezeichner='" + listBox_Pufferspeicher_DB.Text + "'");
            if (rs.Next())
            {
                WErzeugerModel model = new WErzeugerModel();
                model.ID = startindex++;
                model.ID_Projekt = m_ID_Projekt;
                model.ID_PUFFER = (int)rs.Read("ID");
                model.ID_Type = m_nType;
                model.Bezeichner = listBox_Pufferspeicher_DB.Text;

                list_pufferspmodel.Add(model);
                listBox_Pufferspeicher.Items.Add(listBox_Pufferspeicher_DB.Text);
                _linkeListe.Add(model);
                if (m_bWizard) wizardparent.list_werzmodel = list_pufferspmodel;
            }
            rs.Close();
        }

        private void btn_PufferSp_Entfernen_Click(object sender, EventArgs e)
        {
            int index = listBox_Pufferspeicher.SelectedIndex;
            if (index < 0 || index >= _linkeListe.Count) return;

            // Befund 4 (Zusatz): RemoveAt(SelectedIndex) auf list_pufferspmodel traf im
            // Wizard-Modus das falsche Element, weil dort auch Kessel/BHKW/... in der Liste
            // stehen; Items.Remove(Text) traf bei gleichnamigen Eintraegen immer den ersten.
            // Ueber die Parallelliste wird genau das gewaehlte Modell entfernt.
            list_pufferspmodel.Remove(_linkeListe[index]);
            _linkeListe.RemoveAt(index);
            listBox_Pufferspeicher.Items.RemoveAt(index);
            if (m_bWizard) wizardparent.list_werzmodel = list_pufferspmodel;
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

        private void listBox_PufferSp_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Befund 4: Der links gewaehlte Eintrag ist eine PROJEKT-Zeile. Die frueher hier
            // benutzte Namenssuche im KATALOG (Tab_Pufferspeicher_STAMM) fand nichts, sobald
            // die Projektkopie anders heisst als die Vorlage ("... 600 Liter" gegen
            // "... 600 Ltr"), und waere bei gleichnamigen Projektzeilen ohnehin mehrdeutig.
            // Deshalb Zugriff ueber die ID aus der Parallelliste.
            int index = listBox_Pufferspeicher.SelectedIndex;
            if (index < 0 || index >= _linkeListe.Count) return;

            int idPuffer = _linkeListe[index].ID_PUFFER;
            if (idPuffer <= 0) return;

            const string felder = "SELECT Bezeichner, Hersteller, Speichertyp, Bereitschaftsverluste, " +
                                  "Gesamtvolumen, Investitionskosten FROM ";

            DataTable dt = DataRepository.GetDataTable(
                felder + "Tab_Pufferspeicher WHERE ID=? AND ID_Projekt=?",
                new OleDbParameter("@id", idPuffer),
                new OleDbParameter("@proj", m_ID_Projekt));

            // Frisch hinzugefuegte Eintraege haben noch keine Projektkopie - dort steht in
            // ID_PUFFER die STAMM-ID (siehe btn_PufferSp_Hinzu_Click); die Kopie legt erst
            // WizardCtrl beim Speichern an.
            if (dt.Rows.Count == 0)
                dt = DataRepository.GetDataTable(
                    felder + "Tab_Pufferspeicher_STAMM WHERE ID=?",
                    new OleDbParameter("@id", idPuffer));

            if (dt.Rows.Count == 0) return;

            DataRow row = dt.Rows[0];
            textBox_Name.Text = FeldText(row, "Bezeichner");
            textBox_Hersteller.Text = FeldText(row, "Hersteller");
            textBox_Typ.Text = FeldText(row, "Speichertyp");
            textBox_Versluste.Text = FeldText(row, "Bereitschaftsverluste");
            textBox_Volumen.Text = FeldText(row, "Gesamtvolumen");
            textBox_Investitionskosten.Text = FeldText(row, "Investitionskosten");
        }

        /// <summary>Feldwert als Text; NULL und fehlende Spalte ergeben eine leere Zeichenkette.</summary>
        private static string FeldText(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return "";
            object wert = row[spalte];
            return (wert == null || wert == DBNull.Value) ? "" : wert.ToString();
        }

        private void listBox_PufferSp_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Pufferspeicher_STAMM where Bezeichner='" + listBox_Pufferspeicher_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Hersteller.Text = rs.GetString("Hersteller");
                textBox_Typ.Text = (string)rs.Read("Speichertyp");
                textBox_Versluste.Text = rs.Read("Bereitschaftsverluste").ToString();
                textBox_Volumen.Text = rs.Read("Gesamtvolumen").ToString();
                textBox_Investitionskosten.Text = rs.Read("Investitionskosten").ToString();
            }
            rs.Close();
        }

        private void comboBox_Hersteller_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Volumen_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string sql = "";

            // B0-10 (Paket 9 / L5): Die Filterstufe entscheidet der AUSWAHLINDEX, nicht
            // mehr der angezeigte Text - mit lokalisierten Einträgen hätte die frühere
            // Literalkette in keiner Sprache außer Deutsch mehr getroffen. Vorbelegung
            // und Freitextfall liefern unverändert "alle Volumina", siehe PufferSpFilter.
            string szFilterVolumen = PufferSpFilter.VolumenSql(comboBox_Volumen);
            string szFilter = PufferSpFilter.HerstellerSql(comboBox_Hersteller);

            listBox_Pufferspeicher_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from Tab_Pufferspeicher_STAMM where " + szFilterVolumen + " order by Bezeichner";
            else
                sql = "select * from Tab_Pufferspeicher_STAMM where " + szFilter + " and " + szFilterVolumen + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_Pufferspeicher_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();

            int index = listBox_Pufferspeicher_DB.SelectedIndex;
            listBox_Pufferspeicher.SelectedItems.Clear();
            listBox_Pufferspeicher_DB.SelectedItems.Clear();
            ctrl.PufferSp();
            listBox_Pufferspeicher_DB.Items.Clear();
            pufferspctrl.ReadAll();
            for (int i = 0; i < pufferspctrl.rows; i++)
            {
                listBox_Pufferspeicher_DB.Items.Add(pufferspctrl.items[i].Name);
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_Pufferspeicher_DB.SelectedIndex == -1)
            {
                MessageBox.Show(MyResource.Resource.PSP_MELDUNG_MODUL_WAEHLEN);
                return;
            }

            // B0-8: Der Button löscht aus dem KATALOG (Tab_Pufferspeicher_STAMM), nicht
            // aus dem Projekt — bisher ohne Rückfrage und damit global wirksam.
            // Explizite Bestätigung, damit kein Katalogdatensatz versehentlich verschwindet.
            if (MessageBox.Show(
                    string.Format(MyResource.Resource.PSP_MELDUNG_KATALOG_LOESCHEN,
                                  listBox_Pufferspeicher_DB.Text),
                    MyResource.Resource.PSP_TITEL_KATALOG_LOESCHUNG,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            if (!pufferspctrl.Delete(listBox_Pufferspeicher_DB.Text)) return;

            listBox_Pufferspeicher_DB.Items.RemoveAt(listBox_Pufferspeicher_DB.SelectedIndex);
        }

 
    }
}

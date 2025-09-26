using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class WizardParent : Form
    {
        public const int WIZARD_MODE_NEU = 0;
        public const int WIZARD_MODE_BEARBEITEN = 1;
        
        public List<WizardItemClass> listPages = new List<WizardItemClass>();
        public List<WErzeugerModel> list_werzmodel  = new List<WErzeugerModel>();
        public List<Z_ProjektProzesswaermeModel> list_prozmodel = new List<Z_ProjektProzesswaermeModel>();
        public List<Z_ProjektStromganglinieModel> list_stromlastmodel = new List<Z_ProjektStromganglinieModel>();
        public List<Z_ProjGebModel> list_gebmodel = new List<Z_ProjGebModel>();
        public ProjektModel m_Projektmodel  = new ProjektModel();
  
        public int wizardmode;
        public bool gespeichert = false;
        private int top = WizardItemClass.KOMPONENTEN_ITEM;
        private int pagecount;
        public int projektID;

        public WizardParent()
        {
            wizardmode = WIZARD_MODE_NEU;
            projektID = 0;
            pagecount = 0;
            list_werzmodel.Clear();
            list_gebmodel.Clear();
            list_prozmodel.Clear();
            list_stromlastmodel.Clear();
        }

        public WizardParent(List<WizardItemClass> WizardPages)
        {
            InitializeComponent();
            wizardmode = WIZARD_MODE_NEU;
            projektID = 0;
            pagecount = 0;
            list_werzmodel.Clear();
            list_gebmodel.Clear();
            list_prozmodel.Clear();
            list_stromlastmodel.Clear();

            listPages = WizardPages;
            listPages[WizardItemClass.KOMPONENTEN_ITEM].aktiv = true;
            listPages[WizardItemClass.PROJEKT_ITEM].aktiv = true;
            listPages[WizardItemClass.KLIMA_ITEM].aktiv = false;  // Klima Wizard Page ausgeblendet
            listPages[WizardItemClass.GEBAEUDE_ITEM].aktiv = true;
            listPages[WizardItemClass.PROZESS_ITEM].aktiv = true;
            listPages[WizardItemClass.STROMLASTGANG_ITEM].aktiv = true;
            listPages[WizardItemClass.REFERENZ_ITEM].aktiv = true;

            pagecount = listPages.Count();
            
            ApplikationCtrl ctrl = new ApplikationCtrl();
            try
            {
                ctrl.ReadSingle("select * from Tab_Applikation");
                SetImageFromFile(ctrl.m_icon);
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
            }
        }

        private void SetImageFromFile(string imagePath)
        {
            try
            {
                // Bild aus Datei laden
                if (!File.Exists(imagePath))
                {
                    // Wenn die Datei nicht existiert, ein Standardbild verwenden
                    imagePath = Path.Combine(Application.StartupPath, "LogoInekon.jpg");
                    if (File.Exists(imagePath))
                    {
                        Image image = Image.FromFile(imagePath);
                        pictureBox_App.Image = image;
                    }
                }
                else
                    pictureBox_App.Image = Image.FromFile(imagePath); ;
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung, z.B. Fehlermeldung anzeigen
                MessageBox.Show("Fehler beim Laden oder Anzeigen des Bildes: " + ex.Message);
            }
        }

        private void WizardParent_Load(object sender, EventArgs e)
        {
            SetProjektLabel("Projekt auswählen");
            FillProjektList();
            listBox_Projekte.Visible = false;
            top = -1;
            Next();
            btnBack.Enabled = false;
            btnCancel.Enabled = true;
        }
        private void LoadNewForm()
        {
            Form page = listPages.ElementAt(top).wizardform;
            page.FormBorderStyle = FormBorderStyle.None;
            page.TopLevel = false;
            page.AutoScroll = true;
            page.Dock = DockStyle.Fill;
            this.pnlContent.Controls.Clear();
            this.pnlContent.Controls.Add(page);
            page.Show();
         }

        private void Back()
        {
            top = GetNextDownIndex();

            LoadNewForm();
            
            if (wizardmode == WIZARD_MODE_BEARBEITEN)
            {
                if (top == WizardItemClass.KOMPONENTEN_ITEM)
                {
                    listBox_Projekte.Visible = true;
                    label_Projekt.Visible = true;
                }
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;
            btnSpeichern.Enabled = false;
            btnNext.Enabled = true;
   
            if (top <= WizardItemClass.KOMPONENTEN_ITEM)
            {
                btnBack.Enabled = false;
            }

            if (top >= pagecount)
            {
                btnCancel.Enabled = false;
                btnSpeichern.Enabled = true;
            }
        }

        private void Next()
        {
            Form page;

            // Bevor die nächste Seite geladen wird...
            if (wizardmode == WIZARD_MODE_BEARBEITEN && listBox_Projekte.SelectedIndex == -1 )
            {
                btnNext.Enabled = false;
            }
            else btnNext.Enabled = true;


            // Projekt Eingaben in Projektmodel speichern
            if (top == WizardItemClass.PROJEKT_ITEM)
            {
                page = listPages.ElementAt(top).wizardform;
                m_Projektmodel.m_szProjektname = ((Wizard_Projekt)page).GetProjektName();
                m_Projektmodel.m_szBeschreibung = ((Wizard_Projekt)page).GetBeschreibung();
                m_Projektmodel.m_szBearbeiter = ((Wizard_Projekt)page).GetBearbeiter();
                m_Projektmodel.m_szKunde = ((Wizard_Projekt)page).GetKunde();
                m_Projektmodel.m_Aenderungsdatum = ((Wizard_Projekt)page).GetDatum();
                m_Projektmodel.m_Erstelldatum = ((Wizard_Projekt)page).GetErstellDatum();
                LoadWEFromDB(m_Projektmodel.m_szProjektname);
                LoadZGeb(m_Projektmodel.m_szProjektname);
                LoadProzessFromDB(m_Projektmodel.m_szProjektname);
                LoadStromlastFromDB(m_Projektmodel.m_szProjektname);
            }

            // Klimaregion Auswahl in Projektmodel speichern
            if (top == WizardItemClass.KLIMA_ITEM)
            {
                page = listPages.ElementAt(top).wizardform;
                m_Projektmodel.m_ID_Klimaregion = ((Wizard_Klima)page).GetIDKlimaregion();
            }
    
            top = GetNextUpIndex(); //nächste mögliche Seite...
            
            // nächste Seite laden...
            LoadNewForm();

            // nachdem die nächste Seite geladen wurde...
            page = listPages.ElementAt(top).wizardform;
            listBox_Projekte.Visible = false;
            label_Projekt.Visible = false;

            if (wizardmode == WIZARD_MODE_BEARBEITEN)
            {
                // vorhandenes Projekt...
                if (top == WizardItemClass.KOMPONENTEN_ITEM)
                {
                    listBox_Projekte.Visible = true;
                    label_Projekt.Visible = true;
                }
                else if (top == WizardItemClass.PROJEKT_ITEM)
                {
                    ((Wizard_Projekt)page).SetEditProjektName(false);
                    ((Wizard_Projekt)page).SetProjektbezeichner(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.WP_ITEM)
                {
                    ((Wizard_WP)page).SetControls(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.KLIMA_ITEM)
                {
                    ((Wizard_Klima)page).SetControls(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.GEBAEUDE_ITEM)
                {
                    ((Wizard_Gebaeude)page).SetControls(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.SP_ITEM)
                {
                    ((Wizard_Sp)page).SetControls(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.PROZESS_ITEM)
                {
                    ((Wizard_Prozess)page).SetControls(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.STROMLASTGANG_ITEM)
                {
                    ((Wizard_Stromlastgang)page).SetControls(listBox_Projekte.Text);
                }
                else if (top == WizardItemClass.REFERENZ_ITEM)
                {
                    ((Wizard_Referenz)page).SetControls(listBox_Projekte.Text);
                    ((Wizard_Referenz)page).SetWPControls(listBox_Projekte.Text);
                }
            }
            else
            {
                // neues Projekt...
                ProjektCtrl prjctrl = new ProjektCtrl();
                projektID = prjctrl.GetMaxID() + 1;
                if (top == WizardItemClass.PROJEKT_ITEM)
                {
                    ((Wizard_Projekt)page).SetEditProjektName(true);
                    ((Wizard_Projekt)page).SetProjektbezeichner("");
                }
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;
            
            // letzte Seite erreicht ?
            if (lastIndex())
            {
                btnNext.Enabled = false;
                if (top > WizardItemClass.KLIMA_ITEM) btnSpeichern.Enabled = true;
            }

            // bei 1. Seite kein zurück möglich
            if (top <= WizardItemClass.KOMPONENTEN_ITEM)
            {
                btnBack.Enabled = false;
            }
        }

        private bool lastIndex()
        {
            if (top >= pagecount-1) return true;

            WizardItemClass wizard = new WizardItemClass();
            for (int i = top + 1; i < pagecount; i++)
            {
                if (listPages.ElementAt(i).aktiv) return false;
            }
            return true;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            Next();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Back();
        }

        private int GetNextUpIndex()
        {
            int index;

            WizardItemClass wizard = new WizardItemClass();

            for (index = top + 1; index < pagecount - 1; index++)
            {
                if (listPages.ElementAt(index).aktiv) break;
            }
            return index;
        }

        private int GetNextDownIndex()
        {
            int index;
            WizardItemClass wizard = new WizardItemClass();

            for (index = top - 1; index >= WizardItemClass.KOMPONENTEN_ITEM; index--)
            {
                if (listPages.ElementAt(index).aktiv) break;
            }
            return index;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Program.wizardctrl.speichern = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public void FillProjektList()
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll();
            ctrl.FillListBox(listBox_Projekte);
        }

        private void SetKompCheckBoxes()
        {
            // zu einem bestehende Projekt die definierten Komponenten suchen
            ProjektCtrl projctrl = new ProjektCtrl();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            
            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + listBox_Projekte.Text + "'");
            werzctrl.ReadAllFilter("ID_Projekt=" + projctrl.m_ID);
            projektID = projctrl.m_ID;

            Form page = listPages.ElementAt(top).wizardform;
            ((Wizard_Komponenten)page).SetSolarCheckBox(false);
            ((Wizard_Komponenten)page).SetPVPCheckBox(false);
            ((Wizard_Komponenten)page).SetWPCheckBox(false);
            ((Wizard_Komponenten)page).SetStromSpCheckBox(false);
            ((Wizard_Komponenten)page).SetProzessCheckBox(false);
            ((Wizard_Komponenten)page).SetStromglastgangCheckBox(false);

            int rows = werzctrl.rows;
           
            while(rows > 0)
            {
                if (werzctrl.items[rows-1].ID_WP > 0 && werzctrl.items[rows-1].ID_Type == WizardItemClass.WP_TYP) ((Wizard_Komponenten)page).SetWPCheckBox(true);
                if (werzctrl.items[rows-1].ID_Solar > 0 && werzctrl.items[rows-1].ID_Type == WizardItemClass.SOLAR_TYP) ((Wizard_Komponenten)page).SetSolarCheckBox(true);
                if (werzctrl.items[rows-1].ID_PV > 0 && werzctrl.items[rows-1].ID_Type == WizardItemClass.PV_TYP) ((Wizard_Komponenten)page).SetPVPCheckBox(true);
                if (werzctrl.items[rows-1].ID_SP > 0 && werzctrl.items[rows-1].ID_Type == WizardItemClass.SP_TYP) ((Wizard_Komponenten)page).SetStromSpCheckBox(true);
                rows--;
            }
            
            // prüfe Prozess Definition
            Z_ProjektProzesswaermeCtrl prozctrl = new Z_ProjektProzesswaermeCtrl();
            prozctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + projektID);
            if (prozctrl.rows > 0)
            {
                ((Wizard_Komponenten)page).SetProzessCheckBox(true);
            }
            
            // prüfe Stromlastgang Definition
            Z_ProjektStromganglinieCtrl stromctrl = new Z_ProjektStromganglinieCtrl();
            stromctrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + projektID);
            if (stromctrl.rows > 0)
            {
                ((Wizard_Komponenten)page).SetStromglastgangCheckBox(true);
            }

        }
        
        public void LoadWEFromDB(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + projekt + "'");
                werzctrl.ReadAllFilter("ID_Projekt=" + projctrl.m_ID);

                list_werzmodel.Clear();

                for (int n = 0; n < werzctrl.rows; n++)
                {
                    WErzeugerModel item = new WErzeugerModel();
                    ListViewItem lvitem = new ListViewItem();
                    
                    item.ID = werzctrl.items[n].ID;
                    item.ID_Projekt = projctrl.m_ID;
                    item.Bezeichner = werzctrl.items[n].Bezeichner; //(string)rs.Read("WPName");
                    item.ID_Type = werzctrl.items[n].ID_Type;
                    item.Abschaltpunkt = (double)werzctrl.items[n].Abschaltpunkt;
                    item.Betriebsart = (string)werzctrl.items[n].Betriebsart;
                    item.Bivalenter_Betrieb = werzctrl.items[n].Bivalenter_Betrieb;
                    item.Nutzungszeit = werzctrl.items[n].Nutzungszeit;
                    item.Ruecklauf = werzctrl.items[n].Ruecklauf;
                    item.Sperrung = werzctrl.items[n].Sperrung;
                    item.Sperrzeit_bis = werzctrl.items[n].Sperrzeit_bis;
                    item.Sperrzeit_von = werzctrl.items[n].Sperrzeit_von;
                    item.Vorlauf = werzctrl.items[n].Vorlauf;
                    item.Heizstab = werzctrl.items[n].Heizstab;
                    item.Volumen = werzctrl.items[n].Volumen;
                    item.rendeMix = werzctrl.items[n].rendeMix;
                    item.Solaranteil = werzctrl.items[n].Solaranteil;
                    item.ID_WP = werzctrl.items[n].ID_WP;
                    item.ID_SP = werzctrl.items[n].ID_SP;
                    item.ID_PV = werzctrl.items[n].ID_PV;
                    item.ID_Solar = werzctrl.items[n].ID_Solar;
                    item.ID_Kessel = werzctrl.items[n].ID_Kessel;

                    list_werzmodel.Add(item);
                }
            }
        }

        private void listBox_Projekte_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetKompCheckBoxes();
            btnNext.Enabled = true;
        }

        public void SetProjektLabel(string szLabel)
        {
            label_Projekt.Text = szLabel;
        }

        public void SetWizardMode(int mode)
        {
            wizardmode = mode;
        }

        private void btnSpeichern_Click(object sender, EventArgs e)
        {
 
            Form pageklima = listPages.ElementAt(WizardItemClass.KLIMA_ITEM).wizardform;
            Program.wizardctrl.Klimazone = ((Wizard_Klima)pageklima).GetKlimaname();
            Form pageproj = listPages.ElementAt(WizardItemClass.PROJEKT_ITEM).wizardform;
            Program.wizardctrl.Projektname = ((Wizard_Projekt)pageproj).GetProjektName();
            Program.wizardctrl.speichern = false;

            gespeichert = false;
            if (wizardmode == WIZARD_MODE_NEU)
            {
                if (Program.wizardctrl.Add_Projekt(projektID, m_Projektmodel ))
                {
                    bool result = Program.wizardctrl.Add_Projekt_ZuordungGebäude(projektID, list_gebmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_WP_Waermeerzeuger(projektID, list_werzmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_Projekt_Prozess(projektID, list_prozmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_Stromganglinie(projektID, list_stromlastmodel);
                    if (!result) return;

                    this.DialogResult = DialogResult.OK;
                    gespeichert = true;
                }
            }
            else
            {
                list_werzmodel.RemoveAll(entferne_nicht_aktive_elemente);

                bool result;
                result = Program.wizardctrl.Del_Projekt_Waermeerzeuger(projektID);
                if (!result) return;

                result = Program.wizardctrl.Add_WP_Waermeerzeuger(projektID, list_werzmodel);
                if (!result) return;

                result = Program.wizardctrl.Del_Projekt_ZuordungGebäude(projektID);
                if (!result) return;

                result = Program.wizardctrl.Add_Projekt_ZuordungGebäude(projektID, list_gebmodel);
                if (!result) return;

                result = Program.wizardctrl.Del_Projekt_Prozess(projektID);
                if (!result) return;

                result = Program.wizardctrl.Add_Projekt_Prozess(projektID, list_prozmodel);
                if (!result) return;

                result = Program.wizardctrl.Del_Stromganglinie(projektID);
                if (!result) return;

                result = Program.wizardctrl.Add_Stromganglinie(projektID, list_stromlastmodel);
                if (!result) return;

                m_Projektmodel.m_Aenderungsdatum = DateTime.Now;
                m_Projektmodel.m_szBearbeiter = ((Wizard_Projekt)pageproj).GetBearbeiter();
                m_Projektmodel.m_szKunde = ((Wizard_Projekt)pageproj).GetKunde();
                m_Projektmodel.m_szBeschreibung = ((Wizard_Projekt)pageproj).GetBeschreibung();
                m_Projektmodel.m_ID_Klimaregion = ((Wizard_Projekt)pageproj).m_ID_Klimaregion;

                result = Program.wizardctrl.Update_Projekt(projektID, m_Projektmodel);
                if (!result) return;

                gespeichert = true; ;
            }
            this.Close();
        }

        private bool entferne_nicht_aktive_elemente(WErzeugerModel item)
        {
            if (!listPages[WizardItemClass.SOLAR_ITEM].aktiv && item.ID_Type == WizardItemClass.SOLAR_TYP) return true;
            if (!listPages[WizardItemClass.SP_ITEM].aktiv && item.ID_Type == WizardItemClass.SP_TYP) return true;
            if (!listPages[WizardItemClass.PV_ITEM].aktiv && item.ID_Type == WizardItemClass.PV_TYP) return true;
            if (!listPages[WizardItemClass.WP_ITEM].aktiv && item.ID_Type == WizardItemClass.WP_TYP) return true;
            return false;
        }

        private void pictureBox_App_Click(object sender, EventArgs e)
        {
            OpenFileDialog icon = new OpenFileDialog();

            if (icon.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ApplikationCtrl ctrl = new ApplikationCtrl();
                    ctrl.ReadSingle("Select * from Tab_Applikation");
                    ctrl.m_icon = icon.FileName;
                    ctrl.Update();
                    SetImageFromFile(icon.FileName);
                }
                catch (Exception ex)
                {
                    // Allgemeine Fehler abfangen
                    Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                }
            }
 
        }

        public void LoadZGeb(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                RecordSet rs = new RecordSet();

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + projekt + "'");

                string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.ID_Gebaeude, Z_ProjektGebaeude.[ID_Projekt], " +
                    "[DBGebaeude].Gebaeudename, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                    "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [DBGebaeude] " + 
                    "INNER JOIN Z_ProjektGebaeude ON [DBGebaeude].ID = Z_ProjektGebaeude.ID_Gebaeude" +
                    " where Z_ProjektGebaeude.ID_Projekt=" + projctrl.m_ID;

                rs.Open(sql);
                list_gebmodel.Clear();

                while(rs.Next()) 
                {
                    Z_ProjGebModel item = new Z_ProjGebModel();

                    item.ID_Z = (int)rs.Read("ID");
                    item.ID_Projekt = projctrl.m_ID;
                    item.ID_Gebaeude = (int)rs.Read("ID_Gebaeude");
                    item.Gebaeudename = (string)rs.Read("Gebaeudename");
                    item.Wohnflaeche = (double)rs.Read("Wohnflaeche_Waermebedarf");
                    item.Einheit = (string)rs.Read("Einheit_Waermebedarf_Wohnflaeche");
                    item.Jahresnutzungsgrad = (double)rs.Read("Jahresnutzungsgrad");
                    item.DezentralWarmwasser = (bool)rs.Read("dezWarmwasserbereitung");
                    item.Gebaeudeart = (string)rs.Read("Gebaeudeart");
                    item.Beschreibung = (string)rs.Read("Beschreibung");

                    list_gebmodel.Add(item);
                    
                }
            }
        }

        public void LoadProzessFromDB(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                Z_ProjektProzesswaermeCtrl prozctrl = new Z_ProjektProzesswaermeCtrl();

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + projekt + "'");
                prozctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + projctrl.m_ID);

                list_prozmodel.Clear();

                for (int n = 0; n < prozctrl.rows; n++)
                {
                    Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();
      
                    item.ID_Z = prozctrl.items[n].ID_Z;
                    item.ID_Projekt = projctrl.m_ID;
                    item.szProzessname = prozctrl.items[n].szProzessname;
                    item.ID_Prozesswaerme = prozctrl.items[n].ID_Prozesswaerme;

                    list_prozmodel.Add(item);
                }
            }
        }

        public void LoadStromlastFromDB(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                Z_ProjektStromganglinieCtrl prozctrl = new Z_ProjektStromganglinieCtrl();

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + projekt + "'");
                prozctrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + projctrl.m_ID);

                list_stromlastmodel.Clear();

                for (int n = 0; n < prozctrl.rows; n++)
                {
                    Z_ProjektStromganglinieModel item = new Z_ProjektStromganglinieModel();

                    item.m_ID_Z = prozctrl.items[n].m_ID_Z;
                    item.m_ID_Projekt = projctrl.m_ID;
                    item.m_szStromganglinie = prozctrl.items[n].m_szStromganglinie;
                    item.m_ID_Stromganglinie = prozctrl.items[n].m_ID_Stromganglinie;

                    list_stromlastmodel.Add(item);
                }
            }
        }
 
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class WizardParent : Form
    {
        public const int WIZARD_MODE_NEU = 0;
        public const int WIZARD_MODE_BEARBEITEN = 1;

        public List<WizardItemClass> listPages = new List<WizardItemClass>();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public List<Z_ProjektProzesswaermeModel> list_prozmodel = new List<Z_ProjektProzesswaermeModel>();
        public List<Z_ProjektStromganglinieModel> list_stromlastmodel = new List<Z_ProjektStromganglinieModel>();
        public List<Z_ProjGebModel> list_gebmodel = new List<Z_ProjGebModel>();
        public List<Z_ProjektStromverbraucherModel> list_stromverbrauchermodel = new List<Z_ProjektStromverbraucherModel>();
        public List<Z_ProjWaermebedarfModel> list_wbmodel = new List<Z_ProjWaermebedarfModel>();

        public ProjektModel m_Projektmodel = new ProjektModel();

        public int wizardmode;
        public bool gespeichert = false;
        private int top = WizardItemClass.KOMPONENTEN_ITEM;
        private int pagecount;
        public int projektID;
        public bool bBereitsGeladen = false;

        public WizardParent()
        {
            wizardmode = WIZARD_MODE_NEU;
            projektID = 0;
            pagecount = 0;
            list_werzmodel.Clear();
            list_gebmodel.Clear();
            list_prozmodel.Clear();
            list_stromlastmodel.Clear();
            list_stromverbrauchermodel.Clear();
            list_wbmodel.Clear();
        }

        public WizardParent(List<WizardItemClass> WizardPages)
        {
            InitializeComponent();
            //wizardmode = WIZARD_MODE_NEU;
            projektID = 0;
            pagecount = 0;
            list_werzmodel.Clear();
            list_gebmodel.Clear();
            list_prozmodel.Clear();
            list_stromlastmodel.Clear();
            list_stromverbrauchermodel.Clear();
            list_wbmodel.Clear();

            listPages = WizardPages;
            listPages[WizardItemClass.KOMPONENTEN_ITEM].aktiv = true;
            listPages[WizardItemClass.PROJEKT_ITEM].aktiv = true;
            listPages[WizardItemClass.GEBAEUDE_ITEM].aktiv = false;
            listPages[WizardItemClass.PROZESS_ITEM].aktiv = false;
            listPages[WizardItemClass.STROMLASTGANG_ITEM].aktiv = false;
            listPages[WizardItemClass.KESSEL_ITEM].aktiv = false;
            listPages[WizardItemClass.PV_ITEM].aktiv = false;
            listPages[WizardItemClass.SOLAR_ITEM].aktiv = false;
            listPages[WizardItemClass.SP_ITEM].aktiv = false;
            listPages[WizardItemClass.WP_ITEM].aktiv = false;
            listPages[WizardItemClass.STROMSTD_ITEM].aktiv = false;
            listPages[WizardItemClass.WAERMEBEDARF_ITEM].aktiv = false;
            listPages[WizardItemClass.BHKW_ITEM].aktiv = false;

            pagecount = listPages.Count();

            ApplikationCtrl ctrl = new ApplikationCtrl();
            try
            {
                ctrl.ReadSingle();
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
            //SetProjektLabel("bestehendes Projekt auswählen:");
            FillProjektList();
            listBox_Projekte.Visible = false;
            button_NeuProjekt.Visible = false;
            top = -1;
            Next();
            btnBack.Enabled = false;
            btnCancel.Enabled = true;
            if (wizardmode == WizardParent.WIZARD_MODE_NEU) button_NeuProjekt.Visible = false;
        }

        private void LoadNewForm()
        {
            Form page = listPages.ElementAt(top).wizardform;
            page.FormBorderStyle = FormBorderStyle.None;
            page.TopLevel = false;
            page.AutoScroll = true;

            // 1. Dock auf None, um die echte ungestauchte Wunschgröße der Unterseite zu ermitteln
            page.Dock = DockStyle.None;

            this.pnlContent.Controls.Clear();
            this.pnlContent.Controls.Add(page);
            page.Show();

            // 2. Berechnen, wie viel Platz die Unterseite im Panel *tatsächlich* benötigt
            int extraBreite = page.PreferredSize.Width - this.pnlContent.Width;
            int extraHoehe = page.PreferredSize.Height - this.pnlContent.Height;

            // 3. Hauptfenster vergrößern, falls die Unterseite mehr Platz beansprucht als vorhanden ist
            if (extraBreite > 0) this.Width += extraBreite;
            if (extraHoehe > 0) this.Height += extraHoehe;

            // 4. Sicherheitscheck: Wenn das Fenster nun größer als der physische Bildschirm wird,
            // begrenzen wir es auf die Bildschirmarbeitsfläche, damit die Buttons nicht unter die Taskleiste rutschen.
            Rectangle bildschirm = Screen.FromControl(this).WorkingArea;

            if (this.Width > bildschirm.Width) this.Width = bildschirm.Width;
            if (this.Height > bildschirm.Height) this.Height = bildschirm.Height;

            // 5. Erst JETZT die Unterseite auf Fill setzen
            page.Dock = DockStyle.Fill;
        }
        /*
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
        }*/

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
                    button_NeuProjekt.Visible = true;
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

            if (top > WizardItemClass.PROJEKT_ITEM)
            {
                btnSpeichern.Enabled = true;
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
            if (wizardmode == WIZARD_MODE_BEARBEITEN && listBox_Projekte.SelectedIndex == -1)
                btnNext.Enabled = false;
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
                m_Projektmodel.m_ID_Klimaregion = ((Wizard_Projekt)page).GetIDKlimaregion();

                if (!bBereitsGeladen)
                {
                    LoadWEFromDB(m_Projektmodel.m_szProjektname);
                    LoadZGeb(m_Projektmodel.m_szProjektname);
                    LoadProzessFromDB(m_Projektmodel.m_szProjektname);
                    LoadStromlastFromDB(m_Projektmodel.m_szProjektname);
                    LoadWBedarfFromDB(m_Projektmodel.m_szProjektname);
                    LoadStromverbraucherFromDB(m_Projektmodel.m_szProjektname);
                    bBereitsGeladen = true;
                }
            }

            top = GetNextUpIndex(); //nächste mögliche Seite...

            // nächste Seite laden...
            LoadNewForm();

            // nachdem die nächste Seite geladen wurde...

            if (top > WizardItemClass.PROJEKT_ITEM)
            {
                btnSpeichern.Enabled = true;
            }

            page = listPages.ElementAt(top).wizardform;
            listBox_Projekte.Visible = false;
            label_Projekt.Visible = false;
            button_NeuProjekt.Visible = false;

            if (wizardmode == WIZARD_MODE_BEARBEITEN)
            {
                // vorhandenes Projekt...
                if (top == WizardItemClass.KOMPONENTEN_ITEM)
                {
                    listBox_Projekte.Visible = true;
                    label_Projekt.Visible = true;
                    button_NeuProjekt.Visible = true;
                }
                else if (top == WizardItemClass.PROJEKT_ITEM)
                {
                    ((Wizard_Projekt)page).SetEditProjektName(false);
                    ((Wizard_Projekt)page).SetProjektbezeichner(listBox_Projekte.Text);
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

            if (top == WizardItemClass.WP_ITEM)
            {
                ((Form_WPAuswahl)page).list_werzmodel = list_werzmodel;
                ((Form_WPAuswahl)page).SetControls(listBox_Projekte.Text, true);
            }
            else if (top == WizardItemClass.GEBAEUDE_ITEM)
            {
                ((Form_Gebaeude)page).list_gebmodel = list_gebmodel;
                ((Form_Gebaeude)page).SetControls(listBox_Projekte.Text, true);
            }
            else if (top == WizardItemClass.SP_ITEM)
            {
                ((Form_Stromspeicher)page).list_werzmodel = list_werzmodel;
                ((Form_Stromspeicher)page).SetControls(listBox_Projekte.Text, true);
            }
            else if (top == WizardItemClass.PROZESS_ITEM)
            {
                ((Form_Prozesswaerme)page).list_pwmodel = list_prozmodel;
                ((Form_Prozesswaerme)page).SetControls(listBox_Projekte.Text, true);
                ((Form_Prozesswaerme)page).m_ID_Projekt = projektID;
            }
            else if (top == WizardItemClass.STROMLASTGANG_ITEM)
            {
                ((Wizard_Stromlastgang)page).SetControls(listBox_Projekte.Text);
            }
            else if (top == WizardItemClass.KESSEL_ITEM)
            {
                ((Form_Heizkessel)page).list_heizkesselmodel = list_werzmodel;
                ((Form_Heizkessel)page).SetControls(projektID, true);
            }
            else if (top == WizardItemClass.WAERMEBEDARF_ITEM)
            {
                ((Form_Waermebedarf)page).list_wbmodel = list_wbmodel;
                ((Form_Waermebedarf)page).SetControls(listBox_Projekte.Text, true);
            }
            else if (top == WizardItemClass.STROMSTD_ITEM)
            {
                ((Form_Stromverbraucher)page).list_sbmodel = list_stromverbrauchermodel;
                ((Form_Stromverbraucher)page).SetControls(listBox_Projekte.Text, true);
                ((Form_Stromverbraucher)page).m_ID_Projekt = projektID;
            }
            else if (top == WizardItemClass.BHKW_ITEM)
            {
                ((Form_BHKWEing)page).list_werzmodel = list_werzmodel;
                ((Form_BHKWEing)page).SetControls(listBox_Projekte.Text, true);
            }
            else if (top == WizardItemClass.SOLAR_ITEM)
            {
                ((Form_SolarKollektoren)page).list_werzmodel = list_werzmodel;
                ((Form_SolarKollektoren)page).SetControls(projektID, true);
            }
            else if (top == WizardItemClass.PV_ITEM)
            {
                ((Form_PV)page).list_pvmodel = list_werzmodel;
                ((Form_PV)page).SetControls(listBox_Projekte.Text, true);
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;

            // letzte Seite erreicht ?
            if (lastIndex())
            {
                btnNext.Enabled = false;
                if (top >= WizardItemClass.PROJEKT_ITEM) btnSpeichern.Enabled = true;
            }

            // bei 1. Seite kein zurück möglich
            if (top <= WizardItemClass.KOMPONENTEN_ITEM)
            {
                btnBack.Enabled = false;
            }
        }

        private bool lastIndex()
        {
            if (top >= pagecount - 1) return true;

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

        private void btnOeffnen_Click(object sender, EventArgs e)
        {
            // TODO: Aktion fuer "Oeffnen" festlegen (z. B. bestehendes Projekt oeffnen).
            if (listBox_Projekte.Text == "") { MessageBox.Show("Projekt auswählen!"); return; }
            Program.wizardctrl.Projektname = listBox_Projekte.Text;
            Program.wizardctrl.speichern = false;
            this.DialogResult = DialogResult.OK;
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

            projctrl.ReadSingle(listBox_Projekte.Text);
            werzctrl.ReadAllFilter("ID_Projekt=" + projctrl.m_ID);
            projektID = projctrl.m_ID;

            Form page = listPages.ElementAt(top).wizardform;
            ((Wizard_Komponenten)page).SetSolarCheckBox(false);
            ((Wizard_Komponenten)page).SetPVPCheckBox(false);
            ((Wizard_Komponenten)page).SetWPCheckBox(false);
            ((Wizard_Komponenten)page).SetStromSpCheckBox(false);
            ((Wizard_Komponenten)page).SetProzessCheckBox(false);
            ((Wizard_Komponenten)page).SetStromglastgangCheckBox(false);
            ((Wizard_Komponenten)page).SetKesselCheckBox(false);
            ((Wizard_Komponenten)page).SetStromprofilCheckBox(false);
            ((Wizard_Komponenten)page).SetWBedarfDatenCheckBox(false);
            ((Wizard_Komponenten)page).SetGebaeudeCheckBox(false);
            ((Wizard_Komponenten)page).SetBHKWCheckBox(false);

            int rows = werzctrl.rows;

            while (rows > 0)
            {
                if (werzctrl.items[rows - 1].ID_WP > 0 && werzctrl.items[rows - 1].ID_Type == WizardItemClass.WP_TYP) ((Wizard_Komponenten)page).SetWPCheckBox(true);
                if (werzctrl.items[rows - 1].ID_Solar > 0 && werzctrl.items[rows - 1].ID_Type == WizardItemClass.SOLAR_TYP) ((Wizard_Komponenten)page).SetSolarCheckBox(true);
                if (werzctrl.items[rows - 1].ID_PV > 0 && werzctrl.items[rows - 1].ID_Type == WizardItemClass.PV_TYP) ((Wizard_Komponenten)page).SetPVPCheckBox(true);
                if (werzctrl.items[rows - 1].ID_SP > 0 && werzctrl.items[rows - 1].ID_Type == WizardItemClass.SP_TYP) ((Wizard_Komponenten)page).SetStromSpCheckBox(true);
                if (werzctrl.items[rows - 1].ID_Kessel > 0 && werzctrl.items[rows - 1].ID_Type == WizardItemClass.KESSEL_TYP) ((Wizard_Komponenten)page).SetKesselCheckBox(true);
                if (werzctrl.items[rows - 1].ID_BHKW > 0 && werzctrl.items[rows - 1].ID_Type == WizardItemClass.BHKW_TYP) ((Wizard_Komponenten)page).SetBHKWCheckBox(true);
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

            // prüfe Gebaeude Definition
            Z_ProjGebCtrl gebctrl = new Z_ProjGebCtrl();
            gebctrl.ReadAll("select * from Z_ProjektGebaeude where ID_Projekt=" + projektID);
            if (gebctrl.rows > 0)
            {
                ((Wizard_Komponenten)page).SetGebaeudeCheckBox(true);
            }

            // prüfe Strom Profil Definition
            Z_ProjektStromverbraucherCtrl stromvctrl = new Z_ProjektStromverbraucherCtrl();
            stromvctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + projektID);
            if (stromvctrl.rows > 0)
            {
                ((Wizard_Komponenten)page).SetStromprofilCheckBox(true);
            }

            // prüfe Strom Wärmebedarf Lastgang Definition
            RecordSet rs = new RecordSet();
            rs.Open("select * from Z_ProjektWaermebedarf where ID_Projekt=" + projektID);

            if (rs.Next())
            {
                ((Wizard_Komponenten)page).SetWBedarfDatenCheckBox(true);
            }
            rs.Close();
        }

        /// <summary>
        /// Liest die Energieanlagen eines bestehenden Projekts in <see cref="list_werzmodel"/>
        /// - die Liste, die der Bearbeiten-Zweig in <c>btnSpeichern_Click</c> nach
        /// <c>Del_Projekt_Waermeerzeuger</c> an <c>Add_WP_Waermeerzeuger</c> uebergibt.
        ///
        /// <para>
        /// Die vollstaendig gelesenen Modelle werden DURCHGEREICHT statt Feld fuer Feld
        /// umkopiert - dieselbe Umstellung wie in den Kontextmenue-Controllern
        /// (<c>HeizkesselKontextMenuCtrl</c> &amp; Co.) und auf den Karten der Startseite.
        /// Die fruehere Teilkopie legte je Anlage ein NEUES <c>WErzeugerModel</c> an und
        /// fuehrte 28 der 57 Spalten; nicht kopiert wurden <c>ID_PUFFER</c> und die
        /// komplette Quellen-/Senken-Konfiguration (<c>WS_*</c>, <c>WQ_*</c>,
        /// <c>Prioritaet</c>, <c>BM_Typ</c>). Weil der Speicherweg Loeschen +
        /// Neuanlegen ist, war alles davon nach dem Speichern weg - gemessen 34
        /// Feldabweichungen in Projekt 1023 und 13 in Projekt 1024, u. a.
        /// <c>WS_Typ</c>/<c>WS_Ziel2</c>/<c>WS_ID_Puffer2</c> am BHKW, der komplette
        /// Erdreich-Satz und <c>ID_PUFFER</c> eines nicht katalogisierten
        /// Projekt-Puffers. Jede kuenftige Spalte kommt jetzt automatisch mit, sobald
        /// <c>WErzeugerCtrl.AusZeile</c> sie liest.
        /// </para>
        /// </summary>
        public void LoadWEFromDB(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();

                projctrl.ReadSingle(projekt);
                werzctrl.ReadAllFilter("ID_Projekt=" + projctrl.m_ID);

                list_werzmodel.Clear();

                for (int n = 0; n < werzctrl.rows; n++)
                {
                    // Einziges Feld, das die Teilkopie BEWUSST gesetzt hat: Es bleibt
                    // erhalten. Durch den Filter ist der Wert derselbe, den die Zeile
                    // fuehrt; die Zuweisung greift nur, falls die Spalte in einer alten
                    // Datenbank fehlt (AusZeile laesst ID_Projekt dann auf 0 stehen).
                    werzctrl.items[n].ID_Projekt = projctrl.m_ID;

                    list_werzmodel.Add(werzctrl.items[n]);
                }
            }
        }

        private void listBox_Projekte_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetKompCheckBoxes();
            btnNext.Enabled = true;
            bBereitsGeladen = false;
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
            Form pageproj = listPages.ElementAt(WizardItemClass.PROJEKT_ITEM).wizardform;
            Program.wizardctrl.Klimazone = ((Wizard_Projekt)pageproj).GetKlimaname();
            Program.wizardctrl.Projektname = ((Wizard_Projekt)pageproj).GetProjektName();
            Program.wizardctrl.speichern = false;

            list_gebmodel = ((Form_Gebaeude)listPages[WizardItemClass.GEBAEUDE_ITEM].wizardform).list_gebmodel;
            list_prozmodel = ((Form_Prozesswaerme)listPages[WizardItemClass.PROZESS_ITEM].wizardform).list_pwmodel;
            list_wbmodel = ((Form_Waermebedarf)listPages[WizardItemClass.WAERMEBEDARF_ITEM].wizardform).list_wbmodel;

            /*
            Form pagekomp = listPages.ElementAt(WizardItemClass.KOMPONENTEN_ITEM).wizardform;
            if (Program.wizardctrl.Klimazone == "" && ( ((Wizard_Komponenten)pagekomp).GetBebaeudeCheckBox()
                                                   || ((Wizard_Komponenten)pagekomp).GetKesselCheckBox()
                                                   || ((Wizard_Komponenten)pagekomp).GetProzessCheckBox()
                                                   || ((Wizard_Komponenten)pagekomp).GetWPCheckBox()
                                                   || ((Wizard_Komponenten)pagekomp).GetWBedarfDatenCheckBox()))

            {
                MessageBox.Show("Bitte eine Klimazone auswählen!", "Klimazone fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }*/

            if (Program.wizardctrl.Klimazone == "")
            {
                MessageBox.Show("Bitte eine Klimazone auswählen!", "Klimazone fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (Program.wizardctrl.Projektname == "")
            {
                MessageBox.Show("Bitte einen Projektnamen eingeben!", "Projektname fehlt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            m_Projektmodel.m_szProjektname = ((Wizard_Projekt)pageproj).GetProjektName();
            m_Projektmodel.m_szBeschreibung = ((Wizard_Projekt)pageproj).GetBeschreibung();
            m_Projektmodel.m_szBearbeiter = ((Wizard_Projekt)pageproj).GetBearbeiter();
            m_Projektmodel.m_szKunde = ((Wizard_Projekt)pageproj).GetKunde();
            m_Projektmodel.m_Aenderungsdatum = ((Wizard_Projekt)pageproj).GetDatum();
            m_Projektmodel.m_Erstelldatum = ((Wizard_Projekt)pageproj).GetErstellDatum();
            // Nur den Namen der Klimaregion fuehren; die korrekte ID_Klimaregion (Projekt-Kopie)
            // wird beim Speichern in WizardCtrl.Add_Projekt/Update_Projekt gesetzt.
            m_Projektmodel.m_szKlimaregion = ((Wizard_Projekt)pageproj).GetKlimaname();
            m_Projektmodel.m_ID_Klimaregion = 0;

            gespeichert = false;
            if (wizardmode == WIZARD_MODE_NEU)
            {
                if (Program.wizardctrl.Add_Projekt(ref projektID, m_Projektmodel))
                {
                    bool result = Program.wizardctrl.Add_Projekt_ZuordungGebäude(projektID, list_gebmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_WP_Waermeerzeuger(projektID, list_werzmodel);
                    if (!result) return;

                    // Erst hier steht die ECHTE Projekt-ID (Add_Projekt/@@IDENTITY). Die
                    // Formulare haben in ihrem CreateNewEnergyCarrier nur den Katalogträger
                    // angelegt und dessen ID am Modell vermerkt; energy_price und
                    // energy_Project_settings hängen an Tab_Projekt.ID und entstehen deshalb
                    // erst jetzt.
                    result = Program.wizardctrl.Add_Projekt_Energietraeger(projektID, list_werzmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_Projekt_Prozess(projektID, list_prozmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_Stromganglinie(projektID, list_stromlastmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Del_WaermebedarfExtern(projektID);
                    if (!result) return;

                    result = Program.wizardctrl.Add_WaermebedarfExtern(projektID, list_wbmodel);
                    if (!result) return;

                    result = Program.wizardctrl.Add_Projekt_Stromverbraucher(projektID, list_stromverbrauchermodel);
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

                // Auch im Bearbeiten-Zweig: neu hinzugekommene Träger bekommen ihre
                // projektgebundenen Sätze, bereits zugeordnete fängt der COUNT-Test ab.
                result = Program.wizardctrl.Add_Projekt_Energietraeger(projektID, list_werzmodel);
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

                result = Program.wizardctrl.Del_WaermebedarfExtern(projektID);
                if (!result) return;

                result = Program.wizardctrl.Add_WaermebedarfExtern(projektID, list_wbmodel);
                if (!result) return;

                result = Program.wizardctrl.Del_Projekt_Stromverbraucher(projektID);
                if (!result) return;

                result = Program.wizardctrl.Add_Projekt_Stromverbraucher(projektID, list_stromverbrauchermodel);
                if (!result) return;

                m_Projektmodel.m_Aenderungsdatum = DateTime.Now;
                m_Projektmodel.m_szBearbeiter = ((Wizard_Projekt)pageproj).GetBearbeiter();
                m_Projektmodel.m_szKunde = ((Wizard_Projekt)pageproj).GetKunde();
                m_Projektmodel.m_szBeschreibung = ((Wizard_Projekt)pageproj).GetBeschreibung();
                m_Projektmodel.m_szKlimaregion = ((Wizard_Projekt)pageproj).GetKlimaname();

                result = Program.wizardctrl.Update_Projekt(projektID, m_Projektmodel);
                if (!result) return;

                gespeichert = true;
            }
            this.Close();
        }

        private bool entferne_nicht_aktive_elemente(WErzeugerModel item)
        {
            if (!listPages[WizardItemClass.SOLAR_ITEM].aktiv && item.ID_Type == WizardItemClass.SOLAR_TYP) return true;
            if (!listPages[WizardItemClass.SP_ITEM].aktiv && item.ID_Type == WizardItemClass.SP_TYP) return true;
            if (!listPages[WizardItemClass.PV_ITEM].aktiv && item.ID_Type == WizardItemClass.PV_TYP) return true;
            if (!listPages[WizardItemClass.WP_ITEM].aktiv && item.ID_Type == WizardItemClass.WP_TYP) return true;
            if (!listPages[WizardItemClass.BHKW_ITEM].aktiv && item.ID_Type == WizardItemClass.BHKW_TYP) return true;
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
                    ctrl.ReadSingle();
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

                projctrl.ReadSingle(projekt);

                // Neues Schema: Verknuepfung ueber Tab_Gebaeude.ID_ProjektGebaeude -> Z_ProjektGebaeude.ID
                // (Z_ProjektGebaeude hat kein ID_Gebaeude mehr).
                string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.[ID_Projekt], " +
                    "[Tab_Gebaeude].Gebaeudename, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                    "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [Tab_Gebaeude] " +
                    "INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID" +
                    " where Z_ProjektGebaeude.ID_Projekt=" + projctrl.m_ID;

                rs.Open(sql);
                list_gebmodel.Clear();

                while (rs.Next())
                {
                    Z_ProjGebModel item = new Z_ProjGebModel();

                    item.ID_Z = (int)rs.Read("ID");
                    item.ID_Projekt = projctrl.m_ID;
                    item.Gebaeudename = (string)rs.Read("Gebaeudename");
                    // ID_Gebaeude = Katalog-ID (Tab_Gebaeude_STAMM), passend zu GetIDGebaeude in Form_Gebaeude
                    item.ID_Gebaeude = DataRepository.GetIdByName("Tab_Gebaeude_STAMM", "Bezeichner", item.Gebaeudename);
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

                projctrl.ReadSingle(projekt);
                prozctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + projctrl.m_ID);

                list_prozmodel.Clear();

                for (int n = 0; n < prozctrl.rows; n++)
                {
                    Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();

                    item.ID_Z = prozctrl.items[n].ID_Z;
                    item.ID_Projekt = projctrl.m_ID;
                    item.szProzessname = prozctrl.items[n].szProzessname;
                    item.ID_Prozesswaerme = prozctrl.items[n].ID_Prozesswaerme;
                    item.Summe = prozctrl.items[n].Summe;

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

                projctrl.ReadSingle(projekt);
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

        public void LoadWBedarfFromDB(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                RecordSet rs = new RecordSet();

                projctrl.ReadSingle(projekt);
                rs.Open("select * from Z_ProjektWaermebedarf where ID_Projekt=" + projctrl.m_ID);

                list_wbmodel.Clear();

                while (rs.Next())
                {
                    Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                    item.m_ID_Z = (int)rs.Read("ID_Z");
                    item.m_ID_Projekt = projctrl.m_ID;
                    item.m_szBezeichner = (string)rs.Read("Bezeichner");
                    item.m_ID_Ganglinie = (int)rs.Read("ID_Ganglinie");

                    list_wbmodel.Add(item);
                }
            }
        }

        public void LoadStromverbraucherFromDB(string projekt)
        {
            if (projekt != "")
            {
                ProjektCtrl projctrl = new ProjektCtrl();
                Z_ProjektStromverbraucherCtrl svctrl = new Z_ProjektStromverbraucherCtrl();

                projctrl.ReadSingle(projekt);
                svctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + projctrl.m_ID);

                list_stromverbrauchermodel.Clear();

                for (int n = 0; n < svctrl.rows; n++)
                {
                    Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();

                    item.m_ID_Z = svctrl.items[n].m_ID_Z;
                    item.m_ID_Projekt = projctrl.m_ID;
                    item.m_szVerbraucher = svctrl.items[n].m_szVerbraucher;
                    item.m_ID_Stromverbraucher = svctrl.items[n].m_ID_Stromverbraucher;
                    item.m_Summe = svctrl.items[n].m_Summe;

                    list_stromverbrauchermodel.Add(item);
                }
            }
        }

        private void button_NeuProjekt_Click(object sender, EventArgs e)
        {
            projektID = 0;
            pagecount = listPages.Count();

            listBox_Projekte.Visible = false;
            label_Projekt.Visible = false;
            wizardmode = WIZARD_MODE_NEU;
            button_NeuProjekt.Visible = false;

            list_werzmodel.Clear();
            list_gebmodel.Clear();
            list_prozmodel.Clear();
            list_stromlastmodel.Clear();
            list_stromverbrauchermodel.Clear();
            list_wbmodel.Clear();

            top = -1;
            Next();

            Form page = listPages.ElementAt(top).wizardform;
            ((Wizard_Komponenten)page).SetSolarCheckBox(false);
            ((Wizard_Komponenten)page).SetPVPCheckBox(false);
            ((Wizard_Komponenten)page).SetWPCheckBox(false);
            ((Wizard_Komponenten)page).SetStromSpCheckBox(false);
            ((Wizard_Komponenten)page).SetProzessCheckBox(false);
            ((Wizard_Komponenten)page).SetStromglastgangCheckBox(false);
            ((Wizard_Komponenten)page).SetKesselCheckBox(false);
            ((Wizard_Komponenten)page).SetStromprofilCheckBox(false);
            ((Wizard_Komponenten)page).SetWBedarfDatenCheckBox(false);
            ((Wizard_Komponenten)page).SetGebaeudeCheckBox(false);
            ((Wizard_Komponenten)page).SetBHKWCheckBox(false);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Rahmenfenster des <b>Projektassistenten</b> (bis P4 „Projekt Wizard" —
    /// Entscheidung E5: In der Anwenderdokumentation heißt er durchgängig
    /// „Assistent"; Klassen- und Dateinamen bleiben unverändert, um keinen
    /// Umbenennungssturm auszulösen).
    /// </summary>
    public partial class WizardParent : Form, IAssistentRahmen
    {
        public const int WIZARD_MODE_NEU = 0;
        public const int WIZARD_MODE_BEARBEITEN = 1;

        // --------------------------------------------------------------------
        //  Typisierte Rahmen-Erkennung (P4)
        // --------------------------------------------------------------------

        private static WizardParent _aktiver;
        private WizardParent _vorheriger;

        /// <summary>
        /// Der zurzeit laufende Assistenten-Rahmen; <c>null</c>, wenn keiner offen ist.
        ///
        /// <para>
        /// Ersetzt die Namenssuche <c>Application.OpenForms</c> → <c>form.Name ==
        /// "WizardParent"</c>, die in elf Fachformularen wortgleich stand. Der Rahmen
        /// trägt sich im Konstruktor ein und beim Schließen wieder aus; ein zuvor
        /// eingetragener Rahmen wird dabei wiederhergestellt, damit auch ein
        /// verschachtelter Lauf keinen verwaisten Eintrag hinterlässt.
        /// </para>
        /// </summary>
        public static IAssistentRahmen Aktiver
        {
            get { return _aktiver; }
        }

        /// <summary>Die Seiten des Assistenten (<see cref="IAssistentRahmen"/>).</summary>
        public List<WizardItemClass> Seiten
        {
            get { return listPages; }
        }

        /// <summary>Betriebsart des Assistenten (<see cref="IAssistentRahmen"/>).</summary>
        public int Betriebsart
        {
            get { return wizardmode; }
        }

        /// <summary>Projekt-ID, an der der Assistent arbeitet (<see cref="IAssistentRahmen"/>).</summary>
        public int ProjektID
        {
            get { return projektID; }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Abmelden();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Der Rahmen tritt ab. Wird sowohl beim Schließen als auch beim Verwerfen
        /// gerufen: Ein Rahmen, der gebaut, aber nie angezeigt wurde (Prüfstände,
        /// abgebrochene Einstiege), hinterließe sonst einen Eintrag in
        /// <see cref="Aktiver"/>, der auf ein totes Fenster zeigt.
        /// </summary>
        private void Abmelden()
        {
            if (ReferenceEquals(_aktiver, this)) _aktiver = _vorheriger;
            _vorheriger = null;
        }

        private void RahmenVerworfen(object sender, EventArgs e)
        {
            Abmelden();
        }

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

            // Typisierte Rahmen-Erkennung: ab hier finden die Fachformulare den Rahmen
            // ueber WizardParent.Aktiver. Die Anmeldung steht im Konstruktor, weil
            // schon WizardParent_Load ueber Next() die erste Fachseite bestueckt.
            _vorheriger = _aktiver;
            _aktiver = this;
            Disposed += RahmenVerworfen;

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

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        private void WizardParent_Load(object sender, EventArgs e)
        {
            // Linke Spalte ab P4: das UserControl ProjektAuswahl (Liste, Suche,
            // Sortierung) statt der schlichten ListBox - dieselbe Komponente, die auch
            // hinter Menue "Projekt -> Oeffnen..." und der Kachel "Zuletzt geoeffnet"
            // steht. Der Bestand wird EINMAL gelesen; eingeblendet wird die Spalte nur
            // im Bearbeiten-Modus auf der Komponentenseite (siehe Next/Back).
            ucProjektAuswahl.Laden();
            ucProjektAuswahl.Visible = false;
            button_ProjektOeffnen.Visible = false;
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
                    ucProjektAuswahl.Visible = true;
                    label_Projekt.Visible = true;
                    button_ProjektOeffnen.Visible = true;
                }
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;
            btnNext.Enabled = true;

            if (top <= WizardItemClass.KOMPONENTEN_ITEM)
            {
                btnBack.Enabled = false;
            }

            if (top >= pagecount)
            {
                btnCancel.Enabled = false;
            }

            KnopfTexteAnwenden();
        }

        private void Next()
        {
            Form page;

            // Bevor die nächste Seite geladen wird...
            if (wizardmode == WIZARD_MODE_BEARBEITEN && ucProjektAuswahl.GewaehlteID <= 0)
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

            page = listPages.ElementAt(top).wizardform;
            ucProjektAuswahl.Visible = false;
            label_Projekt.Visible = false;
            button_ProjektOeffnen.Visible = false;

            if (wizardmode == WIZARD_MODE_BEARBEITEN)
            {
                // vorhandenes Projekt...
                if (top == WizardItemClass.KOMPONENTEN_ITEM)
                {
                    ucProjektAuswahl.Visible = true;
                    label_Projekt.Visible = true;
                    button_ProjektOeffnen.Visible = true;
                }
                else if (top == WizardItemClass.PROJEKT_ITEM)
                {
                    ((Wizard_Projekt)page).SetEditProjektName(false);
                    ((Wizard_Projekt)page).SetProjektbezeichner(ucProjektAuswahl.GewaehlterName);
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
                ((Form_WPAuswahl)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
            }
            else if (top == WizardItemClass.GEBAEUDE_ITEM)
            {
                ((Form_Gebaeude)page).list_gebmodel = list_gebmodel;
                ((Form_Gebaeude)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
            }
            else if (top == WizardItemClass.SP_ITEM)
            {
                ((Form_Stromspeicher)page).list_werzmodel = list_werzmodel;
                ((Form_Stromspeicher)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
            }
            else if (top == WizardItemClass.PROZESS_ITEM)
            {
                ((Form_Prozesswaerme)page).list_pwmodel = list_prozmodel;
                ((Form_Prozesswaerme)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
                ((Form_Prozesswaerme)page).m_ID_Projekt = projektID;
            }
            else if (top == WizardItemClass.STROMLASTGANG_ITEM)
            {
                ((Wizard_Stromlastgang)page).SetControls(ucProjektAuswahl.GewaehlterName);
            }
            else if (top == WizardItemClass.KESSEL_ITEM)
            {
                ((Form_Heizkessel)page).list_heizkesselmodel = list_werzmodel;
                ((Form_Heizkessel)page).SetControls(projektID, true);
            }
            else if (top == WizardItemClass.WAERMEBEDARF_ITEM)
            {
                ((Form_Waermebedarf)page).list_wbmodel = list_wbmodel;
                ((Form_Waermebedarf)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
            }
            else if (top == WizardItemClass.STROMSTD_ITEM)
            {
                ((Form_Stromverbraucher)page).list_sbmodel = list_stromverbrauchermodel;
                ((Form_Stromverbraucher)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
                ((Form_Stromverbraucher)page).m_ID_Projekt = projektID;
            }
            else if (top == WizardItemClass.BHKW_ITEM)
            {
                ((Form_BHKWEing)page).list_werzmodel = list_werzmodel;
                ((Form_BHKWEing)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
            }
            else if (top == WizardItemClass.SOLAR_ITEM)
            {
                ((Form_SolarKollektoren)page).list_werzmodel = list_werzmodel;
                ((Form_SolarKollektoren)page).SetControls(projektID, true);
            }
            else if (top == WizardItemClass.PV_ITEM)
            {
                ((Form_PV)page).list_pvmodel = list_werzmodel;
                ((Form_PV)page).SetControls(ucProjektAuswahl.GewaehlterName, true);
            }

            btnBack.Enabled = true;
            btnCancel.Enabled = true;

            // Auf der letzten aktiven Seite wird der Weiter-Knopf zum
            // Speichern-Knopf (Fusion, Nutzerwunsch 30.08.2026); sein
            // Enabled-Zustand kommt von der Bearbeiten-Sperre oben.
            KnopfTexteAnwenden();

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

        /// <summary>
        /// Der fruehere Speichern-Knopf ist entfallen (Nutzerwunsch 30.08.2026):
        /// Auf der letzten aktiven Seite traegt dieser Knopf den Speichern-Text
        /// und schliesst den Durchlauf ab.
        /// </summary>
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (lastIndex()) { SpeichernAusfuehren(); return; }
            Next();
        }

        /// <summary>
        /// Setzt die Beschriftung des Weiter-Knopfs: auf der letzten aktiven
        /// Seite "Speichern" (MyResource WIZ_BTN_SPEICHERN), sonst der
        /// Weiter-Text aus der Formular-resx (beim ersten Aufruf gemerkt).
        /// </summary>
        private string _textWeiter;
        private void KnopfTexteAnwenden()
        {
            if (_textWeiter == null) _textWeiter = btnNext.Text;

            btnNext.Text = lastIndex()
                ? (MyResource.Resource.ResourceManager.GetString("WIZ_BTN_SPEICHERN", MyResource.Resource.Culture) ?? "Speichern")
                : _textWeiter;
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

        /// <summary>
        /// Übernimmt den Komponentenbestand des gewählten Projekts auf die Kacheln des
        /// Komponentenschritts (Paket P5).
        ///
        /// <para>
        /// <b>Vorher</b> hieß die Methode <c>SetKompCheckBoxes</c> und setzte elf
        /// Häkchen: erst alle auf <c>false</c>, dann je Fundstelle wieder auf
        /// <c>true</c> — mit eigenen Kriterien, die von der Bitmaske der Startmaske
        /// abwichen (<c>ID_WP &gt; 0</c> &amp; Co.) und ohne Brauchwasser und
        /// Pufferspeicher. <b>Jetzt</b> liest <see cref="KomponentenBestand"/> den
        /// Bestand nach den Kriterien der Startmaske, und die Seite zeigt ihn als
        /// Kachelfeld — eine Quelle, eine Optik, eine Wahrheit.
        /// </para>
        /// </summary>
        private void KomponentenAusBestandSetzen()
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            projctrl.ReadSingle(ucProjektAuswahl.GewaehlterName);
            projektID = projctrl.m_ID;

            Form page = listPages.ElementAt(top).wizardform;
            ((Wizard_Komponenten)page).BestandAnzeigen(KomponentenBestand.Lesen(projektID));
        }

        /// <summary>
        /// Liest die Energieanlagen eines bestehenden Projekts in <see cref="list_werzmodel"/>
        /// - die Liste, die der Bearbeiten-Zweig in <c>SpeichernAusfuehren</c> nach
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

        /// <summary>
        /// Ein anderes Projekt wurde in der linken Spalte markiert: Komponentenkacheln
        /// neu aus dem Bestand füllen. Tritt an die Stelle von
        /// <c>listBox_Projekte_SelectedIndexChanged</c>.
        /// </summary>
        private void ucProjektAuswahl_MarkierungGeaendert(int id, string name)
        {
            // Die Spalte steht nur im Bearbeiten-Modus auf der Komponentenseite; ein
            // Ereignis von anderswo (z. B. beim Fuellen der Liste) darf nichts anfassen.
            if (top != WizardItemClass.KOMPONENTEN_ITEM) return;

            if (id <= 0)
            {
                if (wizardmode == WIZARD_MODE_BEARBEITEN) btnNext.Enabled = false;
                return;
            }

            KomponentenAusBestandSetzen();
            btnNext.Enabled = true;
            bBereitsGeladen = false;
        }

        public void SetWizardMode(int mode)
        {
            wizardmode = mode;
        }

        private void SpeichernAusfuehren()
        {
            Form pageproj = listPages.ElementAt(WizardItemClass.PROJEKT_ITEM).wizardform;
            Program.wizardctrl.Klimazone = ((Wizard_Projekt)pageproj).GetKlimaname();
            Program.wizardctrl.Projektname = ((Wizard_Projekt)pageproj).GetProjektName();
            Program.wizardctrl.speichern = false;

            list_gebmodel = ((Form_Gebaeude)listPages[WizardItemClass.GEBAEUDE_ITEM].wizardform).list_gebmodel;
            list_prozmodel = ((Form_Prozesswaerme)listPages[WizardItemClass.PROZESS_ITEM].wizardform).list_pwmodel;
            list_wbmodel = ((Form_Waermebedarf)listPages[WizardItemClass.WAERMEBEDARF_ITEM].wizardform).list_wbmodel;

            // (Der frühere auskommentierte Block prüfte die Klimazone nur bei bestimmten
            //  Häkchen. Er rief die zehn Get*CheckBox-Methoden von Wizard_Komponenten,
            //  die es mit der Kachelauswahl nicht mehr gibt, und ist mit P5 entfallen;
            //  die Pflichtprüfung darunter gilt unverändert für jedes Projekt.)

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
                EntferneNichtAktiveZuordnungen();

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
            // FR-1: Der Wizard fuehrt keine Puffer-Seite; ID_Type-12-Modelle kommen nur
            // ueber LoadWEFromDB (typloser ReadAllFilter) in die Liste. Der Bearbeiten-
            // Zweig loescht die Pufferzeilen nicht mehr (Del_Projekt_Waermeerzeuger
            // verschont ID_Type 12) - blieben die Modelle in der Liste, legte
            // Add_WP_Waermeerzeuger die stehen gebliebenen Anlagenzeilen doppelt an.
            if (item.ID_Type == WizardItemClass.PUFFER_TYP) return true;

            if (!listPages[WizardItemClass.SOLAR_ITEM].aktiv && item.ID_Type == WizardItemClass.SOLAR_TYP) return true;
            if (!listPages[WizardItemClass.SP_ITEM].aktiv && item.ID_Type == WizardItemClass.SP_TYP) return true;
            if (!listPages[WizardItemClass.PV_ITEM].aktiv && item.ID_Type == WizardItemClass.PV_TYP) return true;
            if (!listPages[WizardItemClass.WP_ITEM].aktiv && item.ID_Type == WizardItemClass.WP_TYP) return true;
            if (!listPages[WizardItemClass.BHKW_ITEM].aktiv && item.ID_Type == WizardItemClass.BHKW_TYP) return true;

            // P5/E3 - KESSEL-LUECKE GESCHLOSSEN. Diese Zeile fehlte: Ein abgewaehlter
            // Spitzenkessel liess seine Anlagenzeilen in der Liste stehen, der
            // Speicherweg (Del_Projekt_Waermeerzeuger + Add_WP_Waermeerzeuger) legte sie
            // danach wieder an - die Kachel blieb also entgegen der Anzeige belegt. Das
            // Entfernen geschieht erst nach der ausdruecklichen Rueckfrage auf der
            // Komponentenseite (Vorbelegung "Nein").
            if (!listPages[WizardItemClass.KESSEL_ITEM].aktiv && item.ID_Type == WizardItemClass.KESSEL_TYP) return true;

            return false;
        }

        /// <summary>
        /// Gegenstück zu <see cref="entferne_nicht_aktive_elemente"/> für die
        /// <b>Zuordnungstabellen</b> (Gebäude, Wärmebedarf, Prozesswärme,
        /// Stromverbraucher, Stromganglinie).
        ///
        /// <para>
        /// <b>Warum das nötig ist.</b> Der Bearbeiten-Zweig schreibt jede Zuordnung als
        /// „Löschen + Neuanlegen". Ob eine abgewählte Komponente dabei verschwand, hing
        /// bisher vom Zufall ab: Gebäude, Wärmebedarf und Prozesswärme wurden aus der
        /// jeweiligen <b>Seite</b> gelesen — nie besucht hieß leere Liste hieß gelöscht;
        /// einmal besucht und danach abgewählt hieß dagegen: die Daten blieben stehen.
        /// Stromganglinie und Stromverbraucher wurden gar nicht angetastet. Ab P5
        /// entscheidet allein die Kachel, und sie fragt vorher (E3).
        /// </para>
        /// </summary>
        private void EntferneNichtAktiveZuordnungen()
        {
            if (!listPages[WizardItemClass.GEBAEUDE_ITEM].aktiv) list_gebmodel.Clear();
            if (!listPages[WizardItemClass.WAERMEBEDARF_ITEM].aktiv) list_wbmodel.Clear();
            if (!listPages[WizardItemClass.PROZESS_ITEM].aktiv) list_prozmodel.Clear();
            if (!listPages[WizardItemClass.STROMSTD_ITEM].aktiv) list_stromverbrauchermodel.Clear();
            if (!listPages[WizardItemClass.STROMLASTGANG_ITEM].aktiv) list_stromlastmodel.Clear();
        }

        // E4 (Nutzerentscheid 29.08.2026, Konzept 4/E4 Option a): Der Klick auf das
        // Logo im linken Band oeffnete einen Dateidialog und schrieb das gewaehlte Bild
        // DAUERHAFT als Anwendungs-Icon nach Tab_Applikation - ohne jeden Hinweis in der
        // Oberflaeche und ohne Rueckfrage. Handler und Verdrahtung sind ersatzlos
        // entfernt. Auf Nutzerwunsch vom 30.08.2026 ist auch das Logo selbst aus dem
        // linken Band entfernt - damit entfiel zugleich der letzte Leseweg des
        // Anwendungs-Icons (ApplikationCtrl.m_icon -> SetImageFromFile).

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

        /// <summary>
        /// „Projekt öffnen" (Nutzerwunsch 30.08.2026): macht das in der Liste
        /// markierte Projekt zum aktiven Projekt und schließt den Assistenten
        /// danach — <b>ohne</b> das Detailformular „Konfiguration Projekt".
        ///
        /// <para>
        /// Ersetzt den früheren Knopf „Neues Projekt…" (Umschalten in den
        /// Neu-Modus samt Rückfrage <c>HatVerwerfbareEingaben</c>): Das Anlegen
        /// hat mit der Startmasken-Kachel „Neues Projekt" einen eigenen,
        /// eindeutigen Einstieg — ein zweiter Weg mitten im Bearbeiten-Lauf
        /// stiftete nur Verwechslungsgefahr.
        /// </para>
        /// </summary>
        private void button_ProjektOeffnen_Click(object sender, EventArgs e)
        {
            ProjektOeffnenUndSchliessen(ucProjektAuswahl.GewaehlteID, ucProjektAuswahl.GewaehlterName);
        }

        /// <summary>
        /// Doppelklick in der Projektliste wirkt wie „Projekt öffnen"
        /// (Nutzerwunsch 30.08.2026).
        /// </summary>
        private void ucProjektAuswahl_ProjektGewaehlt(int id, string name)
        {
            if (top != WizardItemClass.KOMPONENTEN_ITEM) return;
            ProjektOeffnenUndSchliessen(id, name);
        }

        /// <summary>
        /// Setzt das gewählte Projekt aktiv, schließt den Assistenten und meldet den
        /// Wechsel kurz an der Startmaske.
        ///
        /// <para>
        /// <b>Kein Detailformular mehr (Nutzerwunsch 30.08.2026).</b> Bis hierher rief
        /// dieser Weg <c>MenueCtrl.ProjektInFormMainLaden</c> und zeigte damit
        /// „Konfiguration Projekt" als Dialog — der Anwender wollte an dieser Stelle
        /// aber nur wechseln, nicht bearbeiten. Jetzt läuft er über
        /// <c>MenueCtrl.ProjektAktivSetzen</c>: Startmaske und „zuletzt geöffnet"
        /// ziehen nach, es geht kein Fenster auf. Das Detailformular bleibt hinter
        /// Menü „Projekt → Öffnen…" und der Kachel „Projekt Details" unverändert
        /// erreichbar.
        /// </para>
        /// </summary>
        private void ProjektOeffnenUndSchliessen(int id, string name)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return;

            if (!Program.menuectrl.ProjektAktivSetzen(name, id)) return;

            // Der Assistent wird von zwei Stellen aus gestartet, die nach seinem
            // Schliessen den Projektkontext aus Program.wizardctrl.Projektname
            // nachziehen (Form_Start.pBox_ProjektOeffnen_Click,
            // MDIMainForm.MenuItem_ProjektBearbeiten_Click). Das Feld haelt den
            // zuletzt GESPEICHERTEN Namen und wird beim Start des Assistenten nicht
            // geleert - ohne diese Zeile holte der Nachzug ein frueher gespeichertes
            // Projekt zurueck und machte das gerade geoeffnete wieder unwirksam.
            if (Program.wizardctrl != null) Program.wizardctrl.Projektname = name;

            // Close() blendet den modalen Rahmen nur aus; ShowDialog kehrt erst nach
            // diesem Handler zurueck. Der Hinweis liegt deshalb ueber der Startmaske
            // und nicht unter dem Assistenten.
            Close();
            if (Program.startfrm != null) Program.startfrm.HinweisProjektGeoeffnet();
        }
    }
}

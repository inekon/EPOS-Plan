using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Rectangle = System.Drawing.Rectangle;

namespace WindowsFormsApplication1
{
    public partial class Form_Start : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjektname = "";
        public int status = 0;
        private bool bUpdateWizardSymbole = false;

        // Lazy initialisiert – nicht als Field-Initializer, sonst versucht der
        // WinForms-Designer die Klassen zu instanziieren und scheitert am
        // DB-/COM-Zugriff im Konstruktor von SimulationStrombedarf.
        private SimulationStrombedarf _simulationStrombedarf;
        private SimulationStrombedarf simulationStrombedarf
            => _simulationStrombedarf ??= new SimulationStrombedarf();

        private SimulationWaermebedarf _simulationWaermebedarf;
        private SimulationWaermebedarf simulationWaermebedarf
            => _simulationWaermebedarf ??= new SimulationWaermebedarf();

        // Definition des Dictionarys
        // Key: Name des Controls oder ein Tag
        // Value: Die Methode, die aufgerufen werden soll
        private Dictionary<string, Action<object, EventArgs>> _clickEvents;

        // Hilfsklasse, die die Verbindung zwischen Controls und Hilfeseiten herstellt.
        // KEINE eigene Instanz mehr (F5): der Extender ist anwendungsweit, und die
        // HilfeAutomatik erfasst dieses Formular ohnehin von selbst. Der Verweis und
        // der Aufruf in Form_Start_Load bleiben nur als ausdrückliche, wirkungsgleiche
        // Absicherung stehen — RegisterForm ist idempotent.
        private HelpExtender _helpExtender;

        public Form_Start()
        {
            InitializeComponent();
            // H7: Infoknoepfe der Reiter "Energieerzeuger" und "Simulation". Platz und
            // Groesse deckungsgleich zu btn_Help_Strombedarf auf Reiter 3 (51x39, 18 px
            // vom rechten Rand); programmatisch, weil die Startmaske ihre Koordinaten je
            // Sprache in eigenen .resx-Dateien fuehrt.
            InfoKnopf.Anbringen(tabPage4, "btn_Help_Energieerzeuger", 18, 20, breite: 51, hoehe: 39);
            InfoKnopf.Anbringen(tabPage5, "btn_Help_Simulation", 18, 20, breite: 51, hoehe: 39);
            textBox_ProjektOpen.Text = MyResource.Resource.Text_Select;
            // Der Projektkopf zeigt den Namen jetzt im Auswahlfeld (siehe
            // ProjektkopfAufbauen): solange kein Projekt offen ist, steht dort
            // derselbe Platzhalter, den bisher das blaue Textfeld trug.
            KopfEinzeltextZeigen(MyResource.Resource.Text_Select);
            InitEventDictionary();
            _helpExtender = Program.HelpExtender;

            // H11: Kachel "Optimierung" auf dem Reiter Simulation ausblenden -
            // FUNKTION NICHT UMGESETZT. Ihr Handler pBox_Optimierung_Click ist
            // leer, die drei Steuerelemente tragen aber Cursors.Hand und sehen
            // damit anklickbar aus. Ausgeblendet wird per Code; die
            // Designer-Datei bleibt unberuehrt, damit die Kachel beim spaeteren
            // Umsetzen der Funktion nur wieder eingeschaltet werden muss.
            OptimierungskachelVerbergen();

            // P1/P2 (Projektdialoge): Der Aufruf FensterEinpassung.Einhaengen(this) stand
            // hier als "Notebook-Schutz" - er war jedoch WIRKUNGSLOS und ist deshalb
            // entfernt. FensterEinpassung.Zustaendig schliesst Formulare mit
            // TopLevel == false ausdruecklich aus (Allgemein\FensterEinpassung.cs), und
            // MDIMainForm bettet Form_Start genau so ein (TopLevel=false, Dock=Fill).
            // Den Bildlauf, den die Einpassung sonst sichern wuerde, setzt
            // Form_Start_Load ohnehin selbst (this.AutoScroll = true je Reiter).
        }

        private void Form_Start_Load(object sender, EventArgs e)
        {
            // Form_Start wird als eingebettete Form (TopLevel=false) in MDIMainForm
            // angezeigt – kein eigenes WindowState mehr nötig (der Host dockt sie auf Fill).
            tabControl_Wizard.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl_Wizard.DrawItem += tabControl_Wizard_DrawItem;
            for (int i = 1; i < tabControl_Wizard.TabPages.Count; i++) tabControl_Wizard.TabPages[i].Enabled = false;
            btn_Weiter.MakeSmoothButton(6);
            btn_Zurueck.MakeSmoothButton(6);
            btn_Weiter.BackColor = Color.LightGray;
            btn_Zurueck.BackColor = Color.LightGray;
            btn_SimKonfig.MakeSmoothButton(6);
            btn_SimKonfig.BackColor = Color.LightGray;

            label_Haus.Text = "\uE80F";
            label_Haus.Parent = pictureBox2;
            label_Haus.BackColor = Color.Transparent;
            label_Haus.Location = new Point(30, (pictureBox2.Height - label_Haus.Height) / 2); // Achtung: Location ist jetzt relativ zum Panel!

            // Produktname auch im Kopfband der Startseite nennen (Beschriftung
            // stammt aus den Ressourcen, wird hier zur Laufzeit gesetzt)
            try { label20.Text = MDIMainForm.PRODUKTNAME; } catch { }

            // DropDownStyle auf DropDownList
            comboBox_Klima.DropDownStyle = ComboBoxStyle.DropDownList;
            // Hintergrundfarbe auf Weiß setzen
            comboBox_Klima.BackColor = Color.White;
            // Textfarbe auf Schwarz
            comboBox_Klima.ForeColor = Color.Black;
            ComboBox_Klimaregion();
            comboBox_Klima.SetPlaceholder("Bitte zuerst ein Projekt auswählen.");

            // Projektkopf rechts oben: Auswahlfeld an die Stelle des Projektnamens.
            ProjektkopfAufbauen();

            btn_Speichern.Click -= btn_Speichern_Click;
            btn_Speichern.Click += btn_Speichern_Click;

            // Designer-Schutz (wichtig!)
            if (this.DesignMode) return;

            // jedes Control mit einem passenden Key in der Doku verbinden.
            // Die HilfeAutomatik täte das ohnehin; der Aufruf schadet nicht.
            _helpExtender?.RegisterForm(this);

            // Scrollbars aktivieren, falls der Designer-Inhalt (1620x932 px) größer
            // ist als der verfügbare Platz – schneidet sonst auf kleineren Bildschirmen ab.
            this.AutoScroll = true;
            foreach (TabPage tp in tabControl_Wizard.TabPages)
                tp.AutoScroll = true;
        }

        private void tabControl_Wizard_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            Color tabTextColor = Color.FromArgb(0x000000);
            var color = Color.FromArgb(tabTextColor.R, tabTextColor.G, tabTextColor.B);

            if (e.Index == tabControl_Wizard.SelectedIndex)
            {
                TextRenderer.DrawText(e.Graphics, tabControl_Wizard.TabPages[e.Index].Text, e.Font, e.Bounds, Color.FromArgb(0xffffff));
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, tabControl_Wizard.TabPages[e.Index].Text, e.Font, e.Bounds, color);
            }
        }

        public void SetTextProjekt(string szProjekt)
        {
            textBox_ProjektOpen.Text = szProjekt;
            karte_ProjektDetails.Enabled = true;

            // Den Namen auch im Auswahlfeld des Projektkopfs zeigen: es steht an der
            // Stelle, an der bis dahin das blaue Textfeld stand. Diese Methode ist die
            // gemeinsame Stelle aller Wege (auch der Menuewege ueber MenueCtrl, die
            // ProjektKontextUebernehmen nicht durchlaufen).
            KopfNameZeigen(szProjekt);
        }

        /// <summary>
        /// Uebernimmt das Projekt <paramref name="szProjektname"/> als aktuellen Kontext der
        /// Startseite: Name/ID, Kopfband, Klimaregion, Statuszeichen, Freischaltung der
        /// Wizard-Reiter und ein frisches Nachziehen der Wizard-Symbole.
        ///
        /// Befund 3: Die Einstiegswege (Menue "Neu"/"Projekt bearbeiten", Kacheln "Projekt
        /// neu", "Projekt oeffnen", "Zuletzt geoeffnet") haben den Kontext bisher jeder
        /// anders - ueber die Menuepunkte sogar ueberhaupt nicht - nachgezogen. Blieb dabei
        /// <see cref="m_ID_Projekt"/> auf dem zuvor geoeffneten Projekt stehen, schrieben die
        /// Wizard-Kacheln anschliessend in das FALSCHE Projekt. Die Uebernahme liegt deshalb
        /// jetzt an genau einer Stelle.
        /// </summary>
        /// <returns>
        /// false, wenn zu dem Namen kein Projekt existiert; der bisherige Kontext bleibt
        /// dann unveraendert.
        /// </returns>
        public bool ProjektKontextUebernehmen(string szProjektname)
        {
            if (string.IsNullOrEmpty(szProjektname)) return false;

            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            ctrl_projekt.ReadSingle(szProjektname);
            if (ctrl_projekt.m_ID <= 0) return false;

            m_szProjektname = ctrl_projekt.m_szProjektname;
            m_ID_Projekt = ctrl_projekt.m_ID;

            SetTextProjekt(m_szProjektname);
            comboBox_Varianten.Text = m_szProjektname;
            comboBox_Klima.Text = GetProjektKlimaregion(m_ID_Projekt);

            label_ProjektStatus.Text = "✔";
            label_ProjektStatus.ForeColor = Color.Green;

            for (int i = 1; i < tabControl_Wizard.TabPages.Count; i++) tabControl_Wizard.TabPages[i].Enabled = true;

            // Die Einweg-Sperre zuruecksetzen: die Symbole gehoeren ab jetzt zum neuen
            // Projekt, ein spaeterer Reiterwechsel muss sie erneut nachziehen duerfen.
            bUpdateWizardSymbole = false;
            UpdateWizardSymbole();

            // Variantenfeld nachziehen UND den Reiter "Berichte & Kosten" ueber das neue
            // Projekt informieren.
            //
            // Bis hierher stand nur FuelleVariantenCombo(...). Der Reiter erfuhr von einem
            // Projektwechsel damit ausschliesslich beim BETRETEN (tabControl_Wizard_Selecting
            // -> BaueBerichteKostenSeite). Wer schon auf dem Reiter stand und oben im Kopfband
            // auf eine andere Version derselben Gruppe umschaltete, liess Uebersicht, Kosten,
            // Wirtschaftlichkeit und Bericht auf dem VORHERIGEN Projekt stehen: Das Kopfband
            // zeigte "Woehler - Test1", die Kostenseite weiter "Projekt: Woehler" samt dessen
            // Zahlen - und "Kostenverwaltung oeffnen..." startete Form_Kosten mit derselben
            // falschen ID. VariantenAnzeigeAktualisieren() macht beides an einer Stelle und
            // ist genau dafuer schon da (Menueweg "Als Variante speichern...").
            VariantenAnzeigeAktualisieren();
            return true;
        }

        private void pBox_Prozess_Click(object sender, EventArgs e)
        {
            Form_Prozesswaerme frm = new Form_Prozesswaerme();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();

            string sql = "SELECT Z_Projekt_Prozesswaerme.ID, Z_Projekt_Prozesswaerme.ID_Projekt, " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme, Tab_Prozesswaerme.Bezeichner, Z_Projekt_Prozesswaerme.Summe " +
                "FROM Z_Projekt_Prozesswaerme INNER JOIN Tab_Prozesswaerme ON " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme = Tab_Prozesswaerme.ID " +
                " where Z_Projekt_Prozesswaerme.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Prozesswaerme = (int)rs.Read("ID_Prozesswaerme");
                item.szProzessname = (string)rs.Read("Bezeichner");
                item.Summe = (double)rs.Read("Summe");
                frm.list_pwmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Prozess(m_ID_Projekt);
                wizctrl.Add_Projekt_Prozess(m_ID_Projekt, frm.list_pwmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }

            if (frm.list_pwmodel.Count > 0)
                status |= 32;
            else status &= ~32;
            pBox_Prozess.Invalidate();
        }

        private void pBox_WBedarfDaten_Click(object sender, EventArgs e)
        {
            Form_Waermebedarf frm = new Form_Waermebedarf();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            frm.list_wbmodel.Clear();

            string sql = "SELECT Z_ProjektWaermebedarf.ID_Z, Z_ProjektWaermebedarf.ID_Projekt, " +
                  "Z_ProjektWaermebedarf.ID_Ganglinie, Tab_Waermebedarf.Bezeichner " +
                  "FROM Z_ProjektWaermebedarf INNER JOIN Tab_Waermebedarf ON " +
                  "Z_ProjektWaermebedarf.ID_Ganglinie = Tab_Waermebedarf.ID " +
                  " where Z_ProjektWaermebedarf.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektGebGanglinieCtrl item = new Z_ProjektGebGanglinieCtrl();
                item.m_ID_Z = (int)rs.Read("ID_Z");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Ganglinie = (int)rs.Read("ID_Ganglinie");
                item.m_szBezeichner = (string)rs.Read("Bezeichner");//item.Text;
                frm.list_wbmodel.Add(item);
            }
            rs.Close();

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);

            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                wizctrl.Del_WaermebedarfExtern(m_ID_Projekt);
                wizctrl.Add_WaermebedarfExtern(m_ID_Projekt, frm.list_wbmodel);
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                if (frm.list_wbmodel.Count > 0)
                    status |= 16;
                else status &= ~16;
                pBox_WBedarfDaten.Invalidate();
            }
        }

        private void pBox_Gebaude_Click(object sender, EventArgs e)
        {
            Z_ProjGebModel item;
            Form_Gebaeude frm = new Form_Gebaeude();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();


            frm.list_gebmodel.Clear();
            //frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.[ID_Projekt], " +
                "[Tab_Gebaeude].Gebaeudename, [Tab_Gebaeude].Baualtersklasse, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [Tab_Gebaeude] " +
                "INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID" +
                " where Z_ProjektGebaeude.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                item = new Z_ProjGebModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Gebaeude = (int)rs.Read("ID");
                item.Gebaeudename = (string)rs.Read("Gebaeudename");
                item.Wohnflaeche = (double)rs.Read("Wohnflaeche_Waermebedarf");
                item.Einheit = (string)rs.Read("Einheit_Waermebedarf_Wohnflaeche");
                item.Jahresnutzungsgrad = (double)rs.Read("Jahresnutzungsgrad");
                item.DezentralWarmwasser = (bool)rs.Read("dezWarmwasserbereitung");
                item.Gebaeudeart = (string)rs.Read("Gebaeudeart");
                item.Beschreibung = (string)rs.Read("Beschreibung");
                item.Baualtersklasse = (string)rs.Read("Baualtersklasse");

                frm.list_gebmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                if (frm.list_gebmodel.Count > 0)
                    status |= 8;
                else status &= ~8;
                pBox_Gebaude.Invalidate();

                wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt);
                wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, frm.list_gebmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_ProjektNeu_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektNeu();

            ApplikationCtrl ctrl_app = new ApplikationCtrl();
            ProjektCtrl ctrl_projekt = new ProjektCtrl();

            if (Program.wizardctrl.Projektname == "") return;
            ctrl_projekt.ReadSingle(Program.wizardctrl.Projektname);

            // Zuletzt geoeffnetes Projekt merken - Schreiblogik unveraendert.
            ctrl_app.m_ID_Projekt = ctrl_projekt.m_ID;
            ctrl_app.m_szProjektname = ctrl_projekt.m_szProjektname;
            ctrl_app.Update();

            // Befund 3: Bisher wurden hier nur Name/ID/Kopfband/Klimaregion gesetzt - die
            // Wizard-Reiter blieben gesperrt (Form_Start_Load sperrt sie), das Statuszeichen
            // stand weiter auf "kein Projekt" und die Wizard-Symbole zeigten den Stand des
            // vorher geoeffneten Projekts.
            ProjektKontextUebernehmen(Program.wizardctrl.Projektname);
        }

        public void SetKlima(string szKlima)
        {
            comboBox_Klima.Text = szKlima;
        }

        private void pBox_ProjektOeffnen_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();

            // Nach dem Schliessen des Wizards Form_Start auf das bearbeitete Projekt aktualisieren
            // und die evtl. geaenderte Klimaregion in der ComboBox anzeigen.
            // Befund 3: Der Block hat bis auf das Nachziehen der Wizard-Symbole schon alles
            // gemacht, was noetig ist - er wird deshalb komplett von
            // ProjektKontextUebernehmen abgeloest (gleiche Bedingung: nur bei gefundenem
            // Projekt wird Tab_Applikation geschrieben).
            if (Program.wizardctrl.Projektname != ""
                && ProjektKontextUebernehmen(Program.wizardctrl.Projektname))
            {
                ApplikationCtrl ctrl_app = new ApplikationCtrl();
                ctrl_app.m_ID_Projekt = m_ID_Projekt;
                ctrl_app.m_szProjektname = m_szProjektname;
                ctrl_app.Update();
            }
        }

        public string GetKlimaregion(int ID_Klimaregion)
        {
            RecordSet rs = new RecordSet();
            string szKlimaregion = "";
            rs.Open("select * from Tab_Klimaregion_STAMM where ID_Klimaregion = " + ID_Klimaregion);
            if (rs.Next())
            {
                szKlimaregion = (string)rs.Read("Name");
            }
            rs.Close();
            return szKlimaregion;
        }

        public int GetKlimaregion(string szKlimaregion)
        {
            RecordSet rs = new RecordSet();
            int IDKlimaregion = 0;
            rs.Open("select * from Tab_Klimaregion_STAMM where Name = '" + szKlimaregion + "'");
            if (rs.Next())
            {
                IDKlimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();
            return IDKlimaregion;
        }
        public string GetProjektKlimaregion(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();

            string szKlimaregion = "";
            rs.Open("select * from Tab_Projekt where ID = " + ID_Projekt);
            if (rs.Next())
            {
                int id = (int)rs.Read("ID_Klimaregion");
                rs.Close();
                // Am Projekt ist die ID der Projekt-Kopie (Tab_Klimaregion.ID) gespeichert.
                if (id != 0)
                {
                    rs.Open("select * from Tab_Klimaregion where ID = " + id);
                    if (rs.Next())
                    {
                        szKlimaregion = (string)rs.Read("Bezeichner");
                    }
                }
            }

            rs.Close();
            return szKlimaregion;
        }

        private void pBox_SpeichernUnter_Click(object sender, EventArgs e)
        {
            // Die Duplizierung (inkl. Fortschrittsanzeige) laeuft im Dialog selbst.
            // Seit P3 fuehrt der Weg ueber MenueCtrl.ProjektSpeichernUnter(): Das
            // Duplizieren hat damit EINEN benannten Einstieg - und liegt nicht mehr
            // (auch) hinter dem Menuepunkt "Oeffnen...".
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektSpeichernUnter();
        }

        private void pBox_StdLastProfil_Click(object sender, EventArgs e)
        {
            Form_Stromverbraucher frm = new Form_Stromverbraucher();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_sbmodel.Clear();
            frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_Projekt_Stromverbraucher.ID, Z_Projekt_Stromverbraucher.ID_Projekt, " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher, Z_Projekt_Stromverbraucher.Summe, Tab_Stromverbraucher.Bezeichner " +
                "FROM Z_Projekt_Stromverbraucher INNER JOIN Tab_Stromverbraucher ON " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher = Tab_Stromverbraucher.ID " +
                " where Z_Projekt_Stromverbraucher.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);

            while (rs.Next())
            {
                Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();
                item.m_ID_Z = (int)rs.Read("ID");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Stromverbraucher = (int)rs.Read("ID_Stromverbraucher");
                item.m_szVerbraucher = (string)rs.Read("Bezeichner");//item.Text;
                item.m_Summe = (double)rs.Read("Summe");
                frm.list_sbmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Stromverbraucher(m_ID_Projekt);
                wizctrl.Add_Projekt_Stromverbraucher(m_ID_Projekt, frm.list_sbmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }

            if (frm.list_sbmodel.Count > 0)
                status |= 64;
            else status &= ~64;
            pBox_StdLastProfil.Invalidate();
        }

        private void pBox_StromProfilEigenes_Click(object sender, EventArgs e)
        {
            Form_EingStromTyp frm = new Form_EingStromTyp();
            frm.SetControls();
            frm.ShowDialog();
        }

        private void pBox_StromMessdaten_Click(object sender, EventArgs e)
        {
            Form_Stromganglinie frm = new Form_Stromganglinie();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            frm.DateiListe.Clear();

            string sql = "SELECT Z_ProjektStromganglinie.ID, Z_ProjektStromganglinie.ID_Projekt, " +
                  "Z_ProjektStromganglinie.ID_Ganglinie, Tab_Stromganglinie.Bezeichner " +
                  "FROM Z_ProjektStromganglinie INNER JOIN Tab_Stromganglinie ON " +
                  "Z_ProjektStromganglinie.ID_Ganglinie = Tab_Stromganglinie.ID " +
                  " where Z_ProjektStromganglinie.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektStromganglinieCtrl item = new Z_ProjektStromganglinieCtrl();
                item.m_ID_Z = (int)rs.Read("ID");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Stromganglinie = (int)rs.Read("ID_Ganglinie");
                item.m_szStromganglinie = (string)rs.Read("Bezeichner");//item.Text;
                frm.DateiListe.Add(item);
            }
            rs.Close();

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);

            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                wizctrl.Del_Stromganglinie(m_ID_Projekt);
                wizctrl.Add_Stromganglinie(m_ID_Projekt, frm.DateiListe);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }

            if (frm.DateiListe.Count > 0)
                status |= 128;
            else status &= ~128;
            pBox_StromMessdaten.Invalidate();
        }

        private void pBox_WP_Click(object sender, EventArgs e)
        {
            Form_WPAuswahl frm = new Form_WPAuswahl();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);
            id_type = WizardItemClass.WP_TYP;

            WErzeugerModel item = new WErzeugerModel();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_werzmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_szProjektname);
            DialogResult result = frm.ShowDialog();

            if (result == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
            }

            if (frm.list_werzmodel.Count > 0)
                status |= 2;
            else status &= ~2;
            pBox_WP.Invalidate();
        }

        private void pBox_Heizkessel_Click(object sender, EventArgs e)
        {
            Form_Heizkessel frm = new Form_Heizkessel();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_heizkesselmodel.Clear();

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
            id_type = WizardItemClass.KESSEL_TYP;

            // Vollstaendig gelesene Modelle durchreichen (siehe HeizkesselKontextMenuCtrl). Die
            // Teilkopie aus ID/ID_Kessel/ID_Type/Bezeichner/Vorlauf/Ruecklauf hat beim Speichern
            // alle uebrigen Anlagenfelder verloren, weil WizardCtrl unten die Anlagen des Typs
            // loescht und ueber Add_WP_Waermeerzeuger komplett neu schreibt - genullt wurden dabei
            // ID_Carrier, Betriebsart, Sperrung/Sperrzeiten, Bivalenter_Betrieb, Abschaltpunkt,
            // Nutzungszeit, Grenzleistung, Heizstab, Volumen, rendeMix und Solaranteil.
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_heizkesselmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_ID_Projekt);
            frm.m_nType = id_type;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_heizkesselmodel);
                if (frm.list_heizkesselmodel.Count > 0)
                    status |= 1;
                else status &= ~1;
                pBox_Heizkessel.Invalidate();

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_Stromspeicher_Click(object sender, EventArgs e)
        {
            Form_Stromspeicher frm = new Form_Stromspeicher();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
            id_type = WizardItemClass.SP_TYP;

            WErzeugerModel item = new WErzeugerModel();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_werzmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_szProjektname);
            DialogResult result = frm.ShowDialog();

            if (result == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
            }

            if (frm.list_werzmodel.Count > 0)
                status |= 4;
            else status &= ~4;

            pBox_Stromspeicher.Invalidate();
        }

        private void pBox_Heizkessel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 1) == 1)
                {
                    // --- DEIN BESTEHENDER CODE FÜR DAS RECHTECK ---
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;

                    // 1. Grüne Fläche zeichnen (wie gewohnt)
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }

                    // --- DEIN BESTEHENDER CODE FÜR DIE LABELS ---
                    label2_pBox_Heizkessel.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Heizkessel.BackColor = label2_pBox_Heizkessel.BackColor;
                }
                else
                {
                    label2_pBox_Heizkessel.BackColor = Color.Transparent;
                    label_pBox_Heizkessel.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_WP_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 2) == 2)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }

                    label2_pBox_WP.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_WP.BackColor = label2_pBox_WP.BackColor;
                }
                else
                {
                    label2_pBox_WP.BackColor = Color.Transparent;
                    label_pBox_WP.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_Stromspeicher_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 162, 232)))
            {
                if ((status & 4) == 4)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Stromspeicher.BackColor = Color.FromArgb(90, 0, 162, 232);
                    label_pBox_Stromspeicher.BackColor = label2_pBox_Stromspeicher.BackColor;
                }
                else
                {
                    label2_pBox_Stromspeicher.BackColor = Color.Transparent;
                    label_pBox_Stromspeicher.BackColor = Color.Transparent;
                }
            }
        }

        /// <summary>
        /// Kachel „Zuletzt geöffnet".
        ///
        /// <para>
        /// <b>Nutzerkorrektur 29.08.2026 zu P3:</b> Die Kachel wechselt wieder
        /// DIREKT zum zuletzt geöffneten Projekt (Eintrag aus
        /// <c>Tab_Applikation</c>) — der P3-Zwischenstand zeigte hier die
        /// Projektliste und verfehlte damit den Ein-Klick-Zweck der Kachel
        /// („es wird der Dialog Öffnen gezeigt und nicht das zuletzt geöffnete
        /// Projekt"). Die Liste <see cref="Form_ProjektAuswahl"/> bleibt nur
        /// als Rückfall, wenn noch kein Projekt gemerkt ist oder das gemerkte
        /// inzwischen gelöscht wurde — vorsortiert nach „Geändert". Wer die
        /// Liste bewusst will, nimmt die Kachel „Projekt öffnen/bearbeiten"
        /// oder das Menü „Projekt → Öffnen…".
        /// </para>
        /// <para>
        /// Der 200-ms-Grünblitz auf der Kachel entfällt: Er zeichnete über
        /// <c>CreateGraphics()</c> an der Kachel vorbei und blockierte dafür mit
        /// <c>Task.Wait()</c> den UI-Faden. Die Rückmeldung übernimmt der
        /// <see cref="Form_Hinweis"/> darunter, der ohnehin schon da war.
        /// </para>
        /// </summary>
        private void pBox_ProjektZuletzt_Click(object sender, EventArgs e)
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle();

            string gewaehlt = ctrl.m_szProjektname == null ? "" : ctrl.m_szProjektname;

            // ProjektKontextUebernehmen liest die ID zum Namen und meldet zugleich,
            // wenn das Projekt zwischenzeitlich geloescht wurde.
            if (gewaehlt.Trim() == "" || !ProjektKontextUebernehmen(gewaehlt))
            {
                // Rückfall: nichts gemerkt oder das gemerkte Projekt existiert
                // nicht mehr - dann (und nur dann) die Projektliste zeigen.
                using (Form_ProjektAuswahl dlg = new Form_ProjektAuswahl())
                {
                    dlg.ZuletztGeaendertZuerst(gewaehlt);
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    gewaehlt = dlg.m_szProjekt;
                }

                if (gewaehlt == "" || !ProjektKontextUebernehmen(gewaehlt))
                {
                    MessageBox.Show(MyResource.Resource.Text_Form_Start_ProjektGeloescht); return;
                }
            }

            // Zuletzt geoeffnetes Projekt merken - dieselbe Schreiblogik wie bei den
            // Kacheln "Neues Projekt" und "Projekt oeffnen/bearbeiten".
            ApplikationCtrl ctrl_app = new ApplikationCtrl();
            ctrl_app.m_ID_Projekt = m_ID_Projekt;
            ctrl_app.m_szProjektname = m_szProjektname;
            ctrl_app.Update();

            Form_Hinweis frm = new Form_Hinweis(MyResource.Resource.Text_Hinweis, MyResource.Resource.Text_Projekt + " " + m_szProjektname + " " + MyResource.Resource.Text_Geoeffnet + "!");
            frm.Location = this.PointToScreen(tabControl_Wizard.PointToScreen(karte_ProjektZuletzt.Location));
            frm.ShowDialog();
        }


        private void pBox_Gebaude_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 8) == 8)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Gebaude.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Gebaude.BackColor = label2_pBox_Gebaude.BackColor;
                }
                else
                {
                    label2_pBox_Gebaude.BackColor = Color.Transparent;
                    label_pBox_Gebaude.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_WBedarfDaten_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 16) == 16)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_WBedarfDaten.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_WBedarfDaten.BackColor = label2_pBox_WBedarfDaten.BackColor;
                }
                else
                {
                    label2_pBox_WBedarfDaten.BackColor = Color.Transparent;
                    label_pBox_WBedarfDaten.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_Prozess_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 32) == 32)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Prozess.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Prozess.BackColor = label2_pBox_Prozess.BackColor;
                }
                else
                {
                    label2_pBox_Prozess.BackColor = Color.Transparent;
                    label_pBox_Prozess.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_StdLastProfil_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 64) == 64)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_StdLastProfil.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_StdLastProfil.BackColor = label2_pBox_StdLastProfil.BackColor;
                }
                else
                {
                    label2_pBox_StdLastProfil.BackColor = Color.Transparent;
                    label_pBox_StdLastProfil.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_StromMessdaten_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 128) == 128)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_StromMessdaten.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_StromMessdaten.BackColor = label2_pBox_StromMessdaten.BackColor;
                }
                else
                {
                    label2_pBox_StromMessdaten.BackColor = Color.Transparent;
                    label_pBox_StromMessdaten.BackColor = Color.Transparent;
                }
            }
        }

        private void tabPage5_Enter(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadSingle(textBox_ProjektOpen.Text);

            if (ctrl.m_ID_Klimaregion == 0)
            {
                tabControl_Wizard.SelectedIndex = 0;
                MessageBox.Show(MyResource.Resource.Text_Form_Start_KlimaregionNichtGesetzt);
                return;
            }

            label_Name.Text = textBox_ProjektOpen.Text;
            simulationStrombedarf.Berechnung(ctrl.m_ID);
            label_Strom.Text = simulationStrombedarf.Strombedarf_gesamt.ToString("F2") + " MWh/a";

            simulationWaermebedarf.Waermebedarf_berechnen(ctrl.m_ID, ctrl.m_ID_Klimaregion);
            label_WBedarf.Text = simulationWaermebedarf.Waermebedarf_Gesamt.ToString("F2") + " MWh/a";

            label_Name.Left = pictureBox_Zusammenfassung.Width - label_Name.Width - 20;
            label_WBedarf.Left = label_Name.Left + label_Name.Width - label_WBedarf.Width;
            label_Strom.Left = label_Name.Left + label_Name.Width - label_Strom.Width;

            // Reine Anzeige (Drei-Schichten-Regel): die gleichlautenden DB-Werte in DbWerte.cs bleiben deutsch.
            label_Komponenten.Text = "";
            if ((status & 1) == 1) label_Komponenten.Text += MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL;
            if ((status & 2) == 2) label_Komponenten.Text += ", " + MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE;
            if ((status & 4) == 4) label_Komponenten.Text += ", " + MyResource.Resource.SIM_STROMSPEICHER;
            if ((status & 256) == 256) label_Komponenten.Text += ", " + MyResource.Resource.SIM_ERZEUGERNAME_BHKW;

            if (label_Komponenten.Text.StartsWith(", ")) label_Komponenten.Text = label_Komponenten.Text.Substring(2);
            label_Komponenten.Left = label_Name.Left + label_Name.Width - label_Komponenten.Width;
        }

        private void pBox_BHKW_Click(object sender, EventArgs e)
        {
            Form_BHKWEing frm = new Form_BHKWEing();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.BHKW_TYP);
            id_type = WizardItemClass.BHKW_TYP;

            WErzeugerModel item = new WErzeugerModel();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_werzmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_szProjektname);
            frm.m_ID_Projekt = m_ID_Projekt;
            DialogResult result = frm.ShowDialog();

            if (result == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
            }

            if (frm.list_werzmodel.Count > 0)
                status |= 256;
            else status &= ~256;

            pBox_BHKW.Invalidate();
        }

        private void pBox_BHKW_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 256) == 256)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_BHKW.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_BHKW.BackColor = label2_pBox_BHKW.BackColor;
                }
                else
                {
                    label2_pBox_BHKW.BackColor = Color.Transparent;
                    label_pBox_BHKW.BackColor = Color.Transparent;
                }
            }
        }

        private void tabControl_Wizard_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex < 0) return;

            if (e.TabPageIndex >= 1 && textBox_ProjektOpen.Text == MyResource.Resource.Text_Select)
            {
                e.Cancel = true;
                Form_Hinweis frm = new Form_Hinweis(MyResource.Resource.Text_Hinweis,
                    "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox1 + "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox2);

                System.Drawing.Point p1 = tabControl_Wizard.Location;
                p1.X += tabControl_Wizard.Width / 2 - frm.Width / 2;
                frm.Location = p1;
                frm.ShowDialog();
            }
            else
            {
                //if(e.TabPageIndex >= 1 && e.TabPageIndex <= 3)
                if (!bUpdateWizardSymbole) { UpdateWizardSymbole(); bUpdateWizardSymbole = true; }

                // Reiter „Berichte & Kosten": Seite beim ersten Betreten aufbauen und
                // danach jedes Mal auf das aktuell geöffnete Projekt einstellen.
                if (e.TabPage == tabPage6) BaueBerichteKostenSeite();
            }
        }

        private void pBox_Delete_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            string szProjekt = menu.ProjektDelete();
            if (szProjekt == textBox_ProjektOpen.Text)
            {
                // Platzhalter aus der Ressource, nicht als deutsches Literal: an zwei
                // Stellen (Reiterwechsel, "Weiter") wird genau gegen
                // MyResource.Resource.Text_Select verglichen. Ein festes deutsches
                // Literal haette diese Pruefung im englischen Modus ausgehebelt.
                textBox_ProjektOpen.Text = MyResource.Resource.Text_Select;
                // Auswahlfeld des Projektkopfs mitziehen: die Gruppe des geloeschten
                // Projekts ist weg, es bleibt der Platzhalter.
                KopfEinzeltextZeigen(MyResource.Resource.Text_Select);
                label_ProjektStatus.ForeColor = Color.FromArgb(192, 0, 0);
                label_ProjektStatus.Text = "⚠";
                comboBox_Klima.Text = "";
            }
        }
        private void btn_SimKonfig_Click(object sender, EventArgs e)
        {
            Form_Simulation_Config frm = new Form_Simulation_Config();
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            frm.Konfiguration = ctrl.model;
            frm.SetControls(m_ID_Projekt);
            System.Drawing.Point p1 = btn_SimKonfig.Location;
            p1 = tabControl_Wizard.PointToScreen(p1);
            p1.Y /= 2;
            p1.X /= 2;
            frm.Location = p1;
            frm.ShowDialog();
        }

        private void pBox_DetailSim_Click(object sender, EventArgs e)
        {
            Form_Simulation_Detail frm = new Form_Simulation_Detail(m_ID_Projekt);
            frm.simulation_Strombedarf = simulationStrombedarf;
            frm.simulation_Waermebedarf = simulationWaermebedarf;

            frm.SetControls();
            frm.ShowDialog();
        }

        private void pBox_Solarthermie_Click(object sender, EventArgs e)
        {
            Form_SolarKollektoren frm = new Form_SolarKollektoren();
            Form_Solarganglinie frm2 = new Form_Solarganglinie();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            WPCtrl wpctrl = new WPCtrl();
            Z_ProjektSolarganglinieCtrl solgctrl = new Z_ProjektSolarganglinieCtrl();
            RecordSet rs = new RecordSet();

            int id_type;

            System.Drawing.Point p1 = pBox_Solarthermie.Location;
            p1 = tabControl_Wizard.PointToScreen(p1);
            p1.Y /= 2;
            p1.X /= 2;

            if (radioButton_KollektorProfil.Checked)
            {
                frm.list_werzmodel.Clear();
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
                id_type = WizardItemClass.SOLAR_TYP;

                WErzeugerModel item = new WErzeugerModel();
                for (int i = 0; i < werzctrl.rows; i++)
                {
                    frm.list_werzmodel.Add(werzctrl.items[i]);
                }

                frm.SetControls(m_ID_Projekt);
                frm.Location = p1;
                DialogResult result = frm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                    wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);

                    projctrl.ReadSingle(m_szProjektname);
                    projctrl.m_Aenderungsdatum = DateTime.Now;
                    projctrl.Update();
                }
            }
            else
            {
                frm2.DateiListe.Clear();

                string sql = "SELECT Z_ProjektSolarganglinie.ID, Z_ProjektSolarganglinie.ID_Projekt, " +
                      "Z_ProjektSolarganglinie.ID_Ganglinie, Tab_Solarganglinie.Bezeichner " +
                      "FROM Z_ProjektSolarganglinie INNER JOIN Tab_Solarganglinie ON " +
                      "Z_ProjektSolarganglinie.ID_Ganglinie = Tab_Solarganglinie.ID " +
                      " where Z_ProjektSolarganglinie.ID_Projekt=" + m_ID_Projekt;

                rs.Open(sql);
                while (rs.Next())
                {
                    Z_ProjektSolarganglinieCtrl item = new Z_ProjektSolarganglinieCtrl();
                    item.m_ID_Z = (int)rs.Read("ID");
                    item.m_ID_Projekt = m_ID_Projekt;
                    item.m_ID_Solarganglinie = (int)rs.Read("ID_Ganglinie");
                    item.m_szSolarganglinie = (string)rs.Read("Bezeichner");
                    frm2.DateiListe.Add(item);
                }
                rs.Close();

                frm2.m_ID_Projekt = m_ID_Projekt;
                frm2.SetControls(m_szProjektname);
                frm2.Location = p1;
                frm2.ShowDialog();

                if (frm2.result == DialogResult.OK)
                {
                    wizctrl.Del_Solarganglinie(m_ID_Projekt);
                    wizctrl.Add_Solarganglinie(m_ID_Projekt, frm2.DateiListe);

                    projctrl.ReadSingle(m_szProjektname);
                    projctrl.m_Aenderungsdatum = DateTime.Now;
                    projctrl.Update();
                }
            }

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
            solgctrl.ReadAll("select * from Z_ProjektSolarganglinie where ID_Projekt=" + m_ID_Projekt.ToString());

            if (werzctrl.rows > 0 || solgctrl.rows > 0)
            {
                radioButton_Ganglinie.BackColor = Color.FromArgb(90, 0, 255, 0);
                radioButton_KollektorProfil.BackColor = Color.FromArgb(90, 0, 255, 0);
                status |= 512;
            }
            else
            {
                radioButton_Ganglinie.BackColor = Color.Transparent;
                radioButton_KollektorProfil.BackColor = Color.Transparent;
                status &= ~512;
            }

            pBox_Solarthermie.Invalidate();
        }

        private void pBox_Solarthermie_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 512) == 512)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Solarthermie.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Solarthermie.BackColor = label2_pBox_Solarthermie.BackColor;
                }
                else
                {
                    label2_pBox_Solarthermie.BackColor = Color.Transparent;
                    label_pBox_Solarthermie.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_PV_Click(object sender, EventArgs e)
        {
            Form_PV frm = new Form_PV();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_pvmodel.Clear();

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);
            id_type = WizardItemClass.PV_TYP;

            // Vollstaendig gelesene Modelle durchreichen (siehe PhotovoltaikKontextMenuCtrl). Die
            // Teilkopie aus ID/ID_PV/ID_Type/Bezeichner/PV_Leistung/Azimut/Neigung hat beim
            // Speichern alle uebrigen Anlagenfelder verloren, weil WizardCtrl unten die Anlagen
            // des Typs loescht und ueber Add_WP_Waermeerzeuger komplett neu schreibt - genullt
            // wurden dabei ID_Carrier, Betriebsart, Sperrung/Sperrzeiten, Vorlauf/Ruecklauf,
            // Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung, Heizstab, Volumen,
            // rendeMix und Solaranteil.
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_pvmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_szProjektname);
            frm.m_nType = id_type;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_pvmodel);
                if (frm.list_pvmodel.Count > 0)
                    status |= 1024;
                else status &= ~1024;
                pBox_PV.Invalidate();

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_PV_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 1024) == 1024)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_PV.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_PV.BackColor = label2_pBox_PV.BackColor;
                }
                else
                {
                    label2_pBox_PV.BackColor = Color.Transparent;
                    label_pBox_PV.BackColor = Color.Transparent;
                }

            }
        }

        private void pBox_ProjektDetails_Click(object sender, EventArgs e)
        {
            if (textBox_ProjektOpen.Text == MyResource.Resource.Text_Select)
            {
                Form_Hinweis frm = new Form_Hinweis(MyResource.Resource.Text_Hinweis, "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox1 + "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox2);
                System.Drawing.Point p1 = karte_ProjektDetails.Location;
                p1 = this.PointToScreen(p1);
                frm.Location = p1;
                frm.ShowDialog();
                return;
            }

            MenueCtrl ctrl = new MenueCtrl();
            ctrl.ProjektOeffnen(true);
        }

        private void label47_Click(object sender, EventArgs e)
        {
            pBox_WP_Click(sender, e);
        }

        private void label46_Click(object sender, EventArgs e)
        {
            pBox_WP_Click(sender, e);
        }

        private void label51_Click(object sender, EventArgs e)
        {
            pBox_Solarthermie_Click(sender, e);
        }

        private void label50_Click(object sender, EventArgs e)
        {
            pBox_Solarthermie_Click(sender, e);
        }

        private void label63_Click(object sender, EventArgs e)
        {
            pBox_DetailSim_Click(sender, e);
        }

        private void label65_Click(object sender, EventArgs e)
        {
            pBox_Optimierung_Click(sender, e);
        }

        private void label64_Click(object sender, EventArgs e)
        {
            pBox_Optimierung_Click(sender, e);
        }

        /// <summary>
        /// H11 - Funktion nicht umgesetzt: Die Kachel "Optimierung" und ihre beiden
        /// Beschriftungen werden ausgeblendet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum ueberhaupt.</b> <see cref="pBox_Optimierung_Click"/> ist leer -
        /// ein Klick auf die Kachel tut nichts. Alle drei Steuerelemente tragen
        /// jedoch <c>Cursors.Hand</c> und stehen gleichrangig neben der Kachel
        /// "Simulation", die einen echten Dialog oeffnet. Ein Bedienelement, das
        /// bedienbar aussieht und nichts tut, ist schlechter als keines.
        /// </para>
        /// <para>
        /// <b>Warum per Code und nicht im Designer.</b> Die Startmaske fuehrt die
        /// Koordinaten ihrer Kacheln je Sprache in eigenen <c>.resx</c>-Dateien; ein
        /// Entfernen im Designer muesste in allen davon nachgezogen werden. Ein
        /// Ausblenden hier ist umkehrbar - wird die Optimierung umgesetzt, faellt
        /// nur dieser eine Aufruf wieder weg.
        /// </para>
        /// <para>
        /// <b>Achtung Schreibweise:</b> Die Untertitelzeile heisst im Designer
        /// <c>label2_pBox_Optinierung</c> - mit "n" statt "m". Der Tippfehler steht
        /// so auch in den <c>.resx</c>-Dateien und wird hier bewusst NICHT
        /// berichtigt (er zoege eine Designer- und drei Ressourcenaenderungen nach
        /// sich); er ist der Grund, warum die drei Namen einzeln stehen statt ueber
        /// ein Namensmuster gesucht zu werden.
        /// </para>
        /// </remarks>
        private void OptimierungskachelVerbergen()
        {
            Control[] kachel = { pBox_Optimierung, label_pBox_Optimierung, label2_pBox_Optinierung };

            foreach (Control c in kachel)
            {
                if (c == null) continue;

                c.Visible = false;
                // Der Handzeiger versprach eine Reaktion - auch unsichtbar bleibt
                // er ein falsches Versprechen, sobald jemand die Kachel wieder
                // einschaltet, ohne die Funktion umzusetzen.
                c.Cursor = Cursors.Default;
            }
        }

        private void pBox_Optimierung_Click(object sender, EventArgs e)
        {
            // H11: Funktion nicht umgesetzt - die Kachel ist ausgeblendet
            // (OptimierungskachelVerbergen). Handler bleibt stehen, weil der
            // Designer ihn und die beiden Beschriftungen darauf verdrahtet.
        }

        private void label62_Click(object sender, EventArgs e)
        {
            pBox_DetailSim_Click(sender, e);
        }

        private void btn_Weiter_Click(object sender, EventArgs e)
        {
            if (tabControl_Wizard.SelectedIndex >= tabControl_Wizard.TabCount - 1) return;

            UpdateWizardSymbole();
            bUpdateWizardSymbole = true;

            tabControl_Wizard.SelectedIndex = tabControl_Wizard.SelectedIndex + 1;
        }

        private void UpdateWizardSymbole()
        {
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
            if (werzctrl.rows > 0) status |= 1; else status &= ~1;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);
            if (werzctrl.rows > 0) status |= 2; else status &= ~2;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
            if (werzctrl.rows > 0) status |= 4; else status &= ~4;

            Z_ProjGebCtrl gebCtrl = new Z_ProjGebCtrl();
            gebCtrl.ReadAll(" select * from Z_ProjektGebaeude where ID_Projekt=" + m_ID_Projekt.ToString());
            if (gebCtrl.rows > 0) status |= 8; else status &= ~8;

            Z_ProjektGebGanglinieCtrl gebgangctrl = new Z_ProjektGebGanglinieCtrl();
            gebgangctrl.ReadAll(" select * from Z_ProjektWaermebedarf where ID_Projekt=" + m_ID_Projekt.ToString());
            if (gebgangctrl.rows > 0) status |= 16; else status &= ~16;

            Z_ProjektProzesswaermeCtrl proctrl = new Z_ProjektProzesswaermeCtrl();
            proctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + m_ID_Projekt.ToString());
            if (proctrl.rows > 0) status |= 32; else status &= ~32;

            Z_ProjektStromverbraucherCtrl strvctrl = new Z_ProjektStromverbraucherCtrl();
            strvctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + m_ID_Projekt.ToString());
            if (strvctrl.rows > 0) status |= 64; else status &= ~64;

            Z_ProjektStromganglinieCtrl strgctrl = new Z_ProjektStromganglinieCtrl();
            strgctrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + m_ID_Projekt.ToString());
            if (strgctrl.rows > 0) status |= 128; else status &= ~128;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.BHKW_TYP);
            if (werzctrl.rows > 0) status |= 256; else status &= ~256;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
            Z_ProjektSolarganglinieCtrl solgctrl = new Z_ProjektSolarganglinieCtrl();
            solgctrl.ReadAll("select * from Z_ProjektSolarganglinie where ID_Projekt=" + m_ID_Projekt.ToString());

            if (werzctrl.rows > 0 || solgctrl.rows > 0)
            {
                status |= 512;
                radioButton_KollektorProfil.BackColor = Color.FromArgb(90, 0, 255, 0);
                radioButton_Ganglinie.BackColor = Color.FromArgb(90, 0, 255, 0);
            }
            else
            {
                status &= ~512;
                radioButton_KollektorProfil.BackColor = Color.Transparent;
                radioButton_Ganglinie.BackColor = Color.Transparent;
            }

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);
            if (werzctrl.rows > 0) status |= 1024; else status &= ~1024;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PUFFER_TYP);
            if (werzctrl.rows > 0) status |= 2048; else status &= ~2048;

            Z_ProjektBrauchwasserCtrl brauchwctrl = new Z_ProjektBrauchwasserCtrl();
            brauchwctrl.ReadAll("select * from Z_Projekt_Brauchwasser where ID_Projekt=" + m_ID_Projekt.ToString());
            if (brauchwctrl.rows > 0) status |= 4096; else status &= ~4096;
        }

        private void btn_Zurueck_Click(object sender, EventArgs e)
        {
            if (tabControl_Wizard.SelectedIndex <= 0) return;
            tabControl_Wizard.SelectedIndex = tabControl_Wizard.SelectedIndex - 1;
        }

        private void pBox_Pufferspeicher_Click(object sender, EventArgs e)
        {
            Form_PufferSp frm = new Form_PufferSp();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_pufferspmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PUFFER_TYP);
            id_type = WizardItemClass.PUFFER_TYP;

            WErzeugerModel item = new WErzeugerModel();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_pufferspmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_ID_Projekt);
            DialogResult result = frm.ShowDialog();

            if (result == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_pufferspmodel);
                // B0-6a: Im Dialog entfernte Puffer hinterlassen sonst Waisen
                new PufferSpCtrl().ProjektWaisenEntfernen(m_ID_Projekt);
            }

            if (frm.list_pufferspmodel.Count > 0)
                status |= 2048;
            else status &= ~2048;

            pBox_Pufferspeicher.Invalidate();
        }

        private void label55_Click(object sender, EventArgs e)
        {
            pBox_Stromspeicher_Click(sender, e);
        }

        private void label54_Click(object sender, EventArgs e)
        {
            pBox_Stromspeicher_Click(sender, e);
        }

        private void label72_Click(object sender, EventArgs e)
        {
            pBox_Pufferspeicher_Click(sender, e);
        }

        private void label71_Click(object sender, EventArgs e)
        {
            pBox_Pufferspeicher_Click(sender, e);
        }

        private void label57_Click(object sender, EventArgs e)
        {
            pBox_PV_Click(sender, e);
        }

        private void label56_Click(object sender, EventArgs e)
        {
            pBox_PV_Click(sender, e);
        }

        private void pBox_Pufferspeicher_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 162, 232)))
            {
                if ((status & 2048) == 2048)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Pufferspeicher.BackColor = Color.FromArgb(90, 0, 162, 232);
                    label_pBox_Pufferspeicher.BackColor = label2_pBox_Pufferspeicher.BackColor;
                }
                else
                {
                    label2_pBox_Pufferspeicher.BackColor = Color.Transparent;
                    label_pBox_Pufferspeicher.BackColor = Color.Transparent;
                }
            }

        }

        private void pBox_Brauchwasser_Click(object sender, EventArgs e)
        {
            Form_Brauchwasser frm = new Form_Brauchwasser();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();

            string sql = "SELECT Z_Projekt_Brauchwasser.ID, Z_Projekt_Brauchwasser.ID_Projekt, " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser, Tab_Brauchwasser.Bezeichner, Z_Projekt_Brauchwasser.Summe " +
                "FROM Z_Projekt_Brauchwasser INNER JOIN Tab_Brauchwasser ON " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser = Tab_Brauchwasser.ID " +
                " where Z_Projekt_Brauchwasser.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektBrauchwasserModel item = new Z_ProjektBrauchwasserModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Brauchwasser = (int)rs.Read("ID_Brauchwasser");
                item.szBezeichner = (string)rs.Read("Bezeichner");
                item.Summe = (double)rs.Read("Summe");
                frm.list_pwmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Brauchwasser(m_ID_Projekt);
                wizctrl.Add_Projekt_Brauchwasser(m_ID_Projekt, frm.list_pwmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }

            if (frm.list_pwmodel.Count > 0)
                status |= 4096;
            else status &= ~4096;
            pBox_Brauchwasser.Invalidate();
        }

        private void label74_Click(object sender, EventArgs e)
        {
            pBox_Brauchwasser_Click(sender, e);
        }

        private void label73_Click(object sender, EventArgs e)
        {
            pBox_Brauchwasser_Click(sender, e);
        }

        private void pBox_Brauchwasser_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 4096) == 4096)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Brauchwasser.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Brauchwasser.BackColor = label2_pBox_Brauchwasser.BackColor;
                }
                else
                {
                    label2_pBox_Brauchwasser.BackColor = Color.Transparent;
                    label_pBox_Brauchwasser.BackColor = Color.Transparent;
                }
            }
        }

        private void ComboBox_Klimaregion()
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            if (m_szProjektname != "")
            {
                comboBox_Klima.Text = m_szProjektname;
            }
            KlimaregionStammCtrl ctrl = new KlimaregionStammCtrl();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Klima.Items.Add(ctrl.items[i].m_szName);
            }
        }

        // Speichert die in der ComboBox gewaehlte Klimaregion zum aktuellen Projekt.
        // Der Klima-Datensatz (Region + Klimadaten + Solar) wird aus den STAMM-Tabellen in
        // das Projekt kopiert (falls noch nicht vorhanden); am Projekt wird die ID der
        // PROJEKT-Kopie gespeichert, nicht die STAMM-ID.
        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(m_szProjektname))
            {
                MessageBox.Show(MyResource.Resource.Text_Form_Start_MessageBox1, MyResource.Resource.Text_Hinweis);
                return;
            }
            if (string.IsNullOrEmpty(comboBox_Klima.Text))
            {
                MessageBox.Show(MyResource.Resource.Text_Form_Start_KlimaregionAuswaehlen, MyResource.Resource.Text_Hinweis);
                return;
            }

            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            ctrl_projekt.ReadSingle(m_szProjektname);
            int idProjekt = ctrl_projekt.m_ID > 0 ? ctrl_projekt.m_ID : m_ID_Projekt;

            // STAMM-Region-ID zur gewaehlten Klimaregion ermitteln.
            int stammRegionId = GetKlimaregion(comboBox_Klima.Text);
            if (stammRegionId <= 0)
            {
                MessageBox.Show(MyResource.Resource.Text_Form_Start_KlimaregionNichtGefunden, MyResource.Resource.Text_Hinweis);
                return;
            }

            // Klima-Datensatz ins Projekt kopieren (falls noch nicht vorhanden) und die ID der
            // Projekt-Kopie zurueckerhalten.
            int projektRegionId = KlimaregionStammCtrl.CopyRegionToProjekt(stammRegionId, idProjekt);
            if (projektRegionId <= 0)
            {
                MessageBox.Show(MyResource.Resource.Text_Form_Start_KlimaregionNichtUebernommen, MyResource.Resource.SIM_TITEL_FEHLER);
                return;
            }

            // Am Projekt die ID der Projekt-Kopie speichern (nicht die STAMM-ID).
            ctrl_projekt.m_ID_Klimaregion = projektRegionId;
            ctrl_projekt.m_Aenderungsdatum = DateTime.Now;
            ctrl_projekt.Update();

            MessageBox.Show(MyResource.Resource.Text_Form_Start_KlimaregionGespeichert, MyResource.Resource.Text_Hinweis);
        }

        private void btn_Kosten_Click(object sender, EventArgs e)
        {
            using (var form = new Form_Kosten(m_ID_Projekt))
            {
                form.m_ID_Projekt = m_ID_Projekt;
                form.ShowDialog(); // Öffnet das Fenster als modaler Dialog
            }
        }

        private void InitEventDictionary()
        {
            // Die sechs Kacheln des Reiters "Projekt" stehen NICHT mehr in diesem
            // Verteiler: sie sind seit P2 AktionsKarte-Instanzen und haengen im
            // Designer direkt mit ihrem Ereignis Geklickt an denselben sechs
            // Handlern (karte_ProjektNeu.Geklickt += pBox_ProjektNeu_Click usw.).
            // Der Verteiler bleibt fuer die Bildkacheln der Reiter 2/3/4 bestehen,
            // die je drei Steuerelemente auf einen Handler buendeln.
            _clickEvents = new Dictionary<string, Action<object, EventArgs>>
            {
                { "pBox_Gebaude", pBox_Gebaude_Click },
                { "label_pBox_Gebaude", pBox_Gebaude_Click },
                { "label2_pBox_Gebaude", pBox_Gebaude_Click },
                { "pBox_WBedarfDaten", pBox_WBedarfDaten_Click },
                { "label_pBox_WBedarfDaten", pBox_WBedarfDaten_Click },
                { "label2_pBox_WBedarfDaten", pBox_WBedarfDaten_Click },
                { "pBox_Prozess", pBox_Prozess_Click },
                { "label_pBox_Prozess", pBox_Prozess_Click },
                { "label2_pBox_Prozess", pBox_Prozess_Click },
                { "pBox_Brauchwasser", pBox_Brauchwasser_Click },
                { "label_pBox_Brauchwasser", pBox_Brauchwasser_Click },
                { "label2_pBox_Brauchwasser", pBox_Brauchwasser_Click },
                { "pBox_StdLastProfil", pBox_StdLastProfil_Click },
                { "label_pBox_StdLastProfil", pBox_StdLastProfil_Click },
                { "label2_pBox_StdLastProfil", pBox_StdLastProfil_Click },
                { "pBox_StromProfilEigenes", pBox_StromProfilEigenes_Click },
                { "label_pBox_StromProfilEigenes", pBox_StromProfilEigenes_Click },
                { "label2_pBox_StromProfilEigenes", pBox_StromProfilEigenes_Click },
                { "pBox_StromMessdaten", pBox_StromMessdaten_Click },
                { "label_pBox_StromMessdaten", pBox_StromMessdaten_Click },
                { "label2_pBox_StromMessdaten", pBox_StromMessdaten_Click },
                { "pBox_Heizkessel", pBox_Heizkessel_Click },
                { "label_pBox_Heizkessel", pBox_Heizkessel_Click },
                { "label2_pBox_Heizkessel", pBox_Heizkessel_Click },
            };
        }

        private void CentralControl_Click(object sender, EventArgs e)
        {
            Control ctrl = sender as Control;
            if (ctrl != null && _clickEvents.ContainsKey(ctrl.Name))
            {
                // Hier wird die im Dictionary hinterlegte Funktion direkt ausgeführt
                _clickEvents[ctrl.Name](sender, e);
            }
        }

        private void comboBox_Klimaregion_DropDownClosed(object sender, EventArgs e)
        {
            // Schiebt den Fokus auf das Parent-Element (z.B. das Panel oder die Form)
            this.ActiveControl = null;
        }

        private void comboBox_Klima_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.panelKlima.Focus();
        }

        private void btn_Varianten_Click(object sender, EventArgs e)
        {
            // Rückfallweg: der Dialog ist seit dem Umbau des Reiters „Berichte & Kosten"
            // nicht mehr verdrahtet (die Schaltfläche wird in BaueBerichteKostenSeite
            // entfernt). Handler und Formular bleiben stehen, falls der Dialog wieder
            // gebraucht wird.
            new Form_Variantentest(m_ID_Projekt).ShowDialog();
        }

        // ============================================================
        //  Reiter „Berichte & Kosten" (tabPage6)
        //
        //  Der Reiter enthielt bis zum Umbau nur die zwei Schaltflächen „Kosten"
        //  und „Varianten". Er trägt jetzt eine senkrechte Navigation mit vier
        //  Seiten (Übersicht, Kosten, Wirtschaftlichkeit, Bericht) —
        //  siehe UcBerichteKosten.
        //
        //  Programmatisch angehängt, damit Form_Start.Designer.cs und die .resx
        //  unberührt bleiben (CLAUDE.md: Designer-Dateien nicht von Hand
        //  editieren) — dasselbe Vorgehen wie bei Form_Kosten.BaueKostenprofilReiter.
        // ============================================================

        private UcBerichteKosten _berichteKosten;

        /// <summary>
        /// Baut die Reiterseite beim ersten Aufruf auf und übergibt ihr bei jedem
        /// weiteren Aufruf das aktuell geöffnete Projekt (es kann sich zwischenzeitlich
        /// geändert haben).
        /// </summary>
        private void BaueBerichteKostenSeite()
        {
            if (this.DesignMode) return;

            if (_berichteKosten == null)
            {
                // Die zwei Altknöpfe entfallen (Designer bleibt unberührt).
                EntferneAltknopf(btn_Kosten);
                EntferneAltknopf(btn_Varianten);

                _berichteKosten = new UcBerichteKosten { Dock = DockStyle.Fill };
                tabPage6.Controls.Add(_berichteKosten);
            }

            _berichteKosten.SetzeProjekt(m_ID_Projekt);
        }

        /// <summary>
        /// Öffnet den Reiter „Berichte &amp; Kosten" und stellt ihn auf die gewünschte
        /// Seite (Schlüssel aus <see cref="UcBerichteKosten"/>, null = zuletzt gewählte).
        /// Einstieg aus dem MDI-Menü (Projekte › Varianten und Bericht…).
        /// </summary>
        public void ZeigeBerichteKosten(string seite = null)
        {
            if (this.DesignMode) return;

            // Baut die Seite beim ersten Aufruf auf und übergibt ihr das offene Projekt.
            // Der Umweg über die Methode ist nötig, weil das Selected-Ereignis des
            // TabControls ausbleibt, wenn der Reiter bereits vorne liegt.
            tabControl_Wizard.SelectedTab = tabPage6;
            BaueBerichteKostenSeite();

            if (!string.IsNullOrEmpty(seite)) _berichteKosten?.ZeigeSeite(seite);
        }

        /// <summary>
        /// Zieht die Variantenanzeige nach, nachdem an anderer Stelle eine Variante
        /// angelegt oder entfernt wurde (Menü „Als Variante speichern…"): Auswahlfeld
        /// neu füllen und – falls schon aufgebaut – den Reiter „Berichte &amp; Kosten"
        /// über das offene Projekt neu informieren.
        /// </summary>
        public void VariantenAnzeigeAktualisieren()
        {
            if (this.DesignMode) return;

            FuelleVariantenCombo(comboBox_Varianten, m_ID_Projekt, true);
            _berichteKosten?.SetzeProjekt(m_ID_Projekt);
        }

        private static void EntferneAltknopf(Control knopf)
        {
            if (knopf == null || knopf.Parent == null) return;
            knopf.Parent.Controls.Remove(knopf);
            knopf.Dispose();
        }

        /// <summary>
        /// Füllt eine ComboBox mit den Projektvarianten, die zum übergebenen (geöffneten)
        /// Projekt gehören. Ist das Projekt selbst eine Variante, werden die Varianten
        /// seines Stammprojekts geladen. Liefert die Anzahl der eingetragenen Varianten.
        /// </summary>
        /// <param name="cb">die zu füllende ComboBox</param>
        /// <param name="idProjekt">ID des geöffneten Projekts (Stamm oder Variante)</param>
        /// <param name="mitStamm">true = das Stammprojekt als ersten Eintrag mit aufnehmen</param>
        public int FuelleVariantenCombo(ComboBox cb, int idProjekt, bool mitStamm = false)
        {
            VariantenCtrl _ctrl = new VariantenCtrl();

            if (cb == null) return 0;
            cb.Items.Clear();
            if (idProjekt <= 0) return 0;

            comboBox_Varianten.SelectedIndexChanged -= comboBox_Varianten_SelectedIndexChanged;

            // Stammprojekt bestimmen: ist das geöffnete Projekt eine Variante,
            // dessen Stamm nehmen; sonst ist es selbst der Stamm.
            int stammId = _ctrl.StammRefDerVariante(idProjekt);
            if (stammId <= 0) stammId = idProjekt;

            string stammName = LiesProjektname(stammId);
    
            if (stammName == "") cb.Text = LiesProjektname(idProjekt); else cb.Text = stammName;

            int anzahl = 0;
            foreach (VariantenCtrl.VarianteInfo vi in _ctrl.LadeGruppe(stammId, stammName))
            {
                if (vi.IstStamm)
                {
                    // Ohne Vorsatz "Stamm: ": Das Feld steht im Projektkopf an der Stelle
                    // des frueheren blauen Projekttextes und traegt deshalb genau dessen
                    // Format - den Projektnamen, bei Varianten "<Stamm> - <Bezeichner>".
                    // Die Auswahllogik haengt an IdProjekt, nicht am Anzeigetext.
                    cb.Items.Add(new VariantenComboItem(vi.IdProjekt, vi.Projektname, true));
                }
                else
                {
                    cb.Items.Add(new VariantenComboItem(vi.IdProjekt, vi.Projektname, false));
                    anzahl++;
                }
            }

            // Damit du bequem an die ID kommst (cb.SelectedValue)
            cb.DisplayMember = "Anzeige";
            cb.ValueMember = "IdProjekt";
 
            // Vorauswahl: das geöffnete Projekt, sonst erster Eintrag.
            int sel = -1;
            for (int i = 0; i < cb.Items.Count; i++)
                if (((VariantenComboItem)cb.Items[i]).IdProjekt == idProjekt) { sel = i; break; }
            if (sel < 0 && cb.Items.Count > 0) sel = 0;

            // Das geoeffnete Projekt bleibt sichtbar ausgewaehlt: Das Feld traegt im
            // Projektkopf den Namen (frueher stand er im blauen Textfeld daneben).
            // Reine Anzeige - das Ereignis ist hier abgehaengt, es wird nichts umgeschaltet.
            cb.SelectedIndex = sel;

            SetzeDropDownBreite(cb);

            comboBox_Varianten.SelectedIndexChanged += comboBox_Varianten_SelectedIndexChanged;

            return anzahl;
        }

        private void SetzeDropDownBreite(ComboBox cb)
        {
            int max = cb.Width;
            using (Graphics g = cb.CreateGraphics())
                foreach (var item in cb.Items)
                {
                    int w = TextRenderer.MeasureText(g, cb.GetItemText(item), cb.Font).Width;
                    if (w > max) max = w;
                }
            cb.DropDownWidth = max + SystemInformation.VerticalScrollBarWidth + 8;
        }

        // Liest den Projektnamen zu einer ID (leer, wenn nicht gefunden).
        private string LiesProjektname(int idProjekt)
        {
            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadAll();
            foreach (ProjektModel p in pc.items)
                if (p.m_ID == idProjekt) return p.m_szProjektname;
            return "";
        }

        // ComboBox-Eintrag für Varianten (Anzeige = Variantenname, Wert = Projekt-ID).
        private class VariantenComboItem
        {
            public int IdProjekt { get; }
            public string Anzeige { get; }
            public bool IstStamm { get; }
            public VariantenComboItem(int idProjekt, string anzeige, bool istStamm)
            { IdProjekt = idProjekt; Anzeige = anzeige; IstStamm = istStamm; }
            public override string ToString() => Anzeige;
        }

        /// <summary>
        /// Baut den Projektkopf (Kasten rechts oben) um: Das Auswahlfeld fuer Stamm und
        /// Varianten tritt an die Stelle des blauen Projektnamens - gleiche Schrift,
        /// gleiche Farbe, flache Anmutung - und traegt damit selbst den Namen des
        /// geoeffneten Projekts. Die zweite Zeile ("Stamm / Variante:") entfaellt, der
        /// Kasten zieht sich auf die verbliebene Zeile zusammen.
        ///
        /// Das bisherige Textfeld bleibt unsichtbar bestehen: an mehreren Stellen wird
        /// sein Text gelesen bzw. gegen den Platzhalter geprueft (Reiterwechsel,
        /// "Weiter", Projektdetails). Es fuehrt weiter den Namen, es zeigt ihn nur
        /// nicht mehr an.
        ///
        /// Bewusst programmatisch statt im Designer (Hausregel Layout).
        /// </summary>
        private void ProjektkopfAufbauen()
        {
            if (comboBox_Varianten == null || textBox_ProjektOpen == null || panelVariante == null) return;

            // Optik des bisherigen Projekttextes uebernehmen. Auswahlliste bleibt
            // Auswahlliste: im Projektkopf wird gewaehlt, nicht getippt.
            comboBox_Varianten.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_Varianten.FlatStyle = FlatStyle.Flat;
            comboBox_Varianten.BackColor = Color.White;
            comboBox_Varianten.ForeColor = textBox_ProjektOpen.ForeColor;
            comboBox_Varianten.Font = textBox_ProjektOpen.Font;

            // An die Stelle des Textfeldes ruecken (Zeile 1, neben "Projekt:").
            textBox_ProjektOpen.Visible = false;
            comboBox_Varianten.Left = textBox_ProjektOpen.Left - 3;   // die Combo setzt ihren Text ein
            comboBox_Varianten.Width = panelVariante.ClientSize.Width - comboBox_Varianten.Left - 14;
            comboBox_Varianten.Top = label11.Top + (label11.Height - comboBox_Varianten.Height) / 2;

            // Beschriftung der entfallenen zweiten Zeile entfernen ...
            if (label4 != null)
            {
                panelVariante.Controls.Remove(label4);
                label4.Dispose();
                label4 = null;
            }

            // ... und den Kasten auf die verbliebene Zeile zusammenziehen, damit kein
            // leerer Streifen unter dem Auswahlfeld stehen bleibt.
            panelVariante.Height = Math.Max(label_ProjektStatus.Bottom, comboBox_Varianten.Bottom) + 10;
            panelVariante.Invalidate();

            // Aufklappbreite zur neuen Schrift/Breite nachziehen (die Liste kann schon
            // gefuellt sein, wenn beim Start ein Projekt wiederhergestellt wurde).
            SetzeDropDownBreite(comboBox_Varianten);
        }

        /// <summary>
        /// Zeigt <paramref name="szName"/> im Auswahlfeld des Projektkopfs an. Enthaelt
        /// die geladene Gruppe den Namen, wird dieser Eintrag gewaehlt; sonst wird die
        /// Gruppe zum aktuell geoeffneten Projekt neu aufgebaut (Menuewege, die
        /// ProjektKontextUebernehmen nicht durchlaufen); notfalls tritt der Name als
        /// einziger Eintrag an die Stelle. Reine Anzeige: das Auswahlereignis ist dabei
        /// abgehaengt, es wird kein Projektwechsel ausgeloest.
        /// </summary>
        private void KopfNameZeigen(string szName)
        {
            if (comboBox_Varianten == null) return;
            if (KopfNameWaehlen(szName)) return;

            if (m_ID_Projekt > 0)
            {
                FuelleVariantenCombo(comboBox_Varianten, m_ID_Projekt, true);
                if (KopfNameWaehlen(szName)) return;
            }

            KopfEinzeltextZeigen(szName);
        }

        /// <summary>
        /// Waehlt im Auswahlfeld des Projektkopfs den Eintrag mit dem Text
        /// <paramref name="szName"/> - ohne das Auswahlereignis auszuloesen.
        /// Liefert false, wenn es keinen solchen Eintrag gibt.
        /// </summary>
        private bool KopfNameWaehlen(string szName)
        {
            if (comboBox_Varianten == null || string.IsNullOrEmpty(szName)) return false;

            for (int i = 0; i < comboBox_Varianten.Items.Count; i++)
            {
                object item = comboBox_Varianten.Items[i];
                if (item == null || !string.Equals(item.ToString(), szName, StringComparison.Ordinal)) continue;

                if (comboBox_Varianten.SelectedIndex != i)
                {
                    comboBox_Varianten.SelectedIndexChanged -= comboBox_Varianten_SelectedIndexChanged;
                    comboBox_Varianten.SelectedIndex = i;
                    comboBox_Varianten.SelectedIndexChanged += comboBox_Varianten_SelectedIndexChanged;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Setzt <paramref name="szName"/> als einzigen Eintrag des Auswahlfeldes und
        /// zeigt ihn an (Platzhalter "bitte auswaehlen!", solange kein Projekt offen
        /// ist). Der Eintrag ist bewusst kein <see cref="VariantenComboItem"/>: das
        /// Auswahlereignis laesst ihn deshalb wirkungslos.
        /// </summary>
        private void KopfEinzeltextZeigen(string szName)
        {
            if (comboBox_Varianten == null) return;

            comboBox_Varianten.SelectedIndexChanged -= comboBox_Varianten_SelectedIndexChanged;
            comboBox_Varianten.Items.Clear();
            comboBox_Varianten.Items.Add(szName ?? "");
            comboBox_Varianten.SelectedIndex = 0;
            comboBox_Varianten.SelectedIndexChanged += comboBox_Varianten_SelectedIndexChanged;
        }

        private void comboBox_Varianten_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(comboBox_Varianten.SelectedItem is VariantenComboItem vi)) return;  // ""/kein Item -> nichts tun
            string name = LiesProjektname(vi.IdProjekt);
            if (!string.IsNullOrEmpty(name))
                ProjektKontextUebernehmen(name);
            Invalidate(true);

            // Kein Mouseover-Hinweis mehr: Das Feld zeigt den Projektnamen jetzt selbst,
            // ein Kurzinfo-Fenster mit demselben Text daneben stoert nur.
        }


        private void panelVariante_Paint(object sender, PaintEventArgs e)
        {
            var c = (Control)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = c.ClientRectangle;
            r.Width -= 1; r.Height -= 1;            // sonst wird rechte/untere Linie abgeschnitten
            using (GraphicsPath path = RundesRechteck(r, 8))
            using (Pen pen = new Pen(Color.FromArgb(180, 190, 205), 1.5f))
                e.Graphics.DrawPath(pen, path);
        }
        private void panelKlima_Paint(object sender, PaintEventArgs e)
        {
            var c = (Control)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = c.ClientRectangle;
            r.Width -= 1; r.Height -= 1;            // sonst wird rechte/untere Linie abgeschnitten
            using (GraphicsPath path = RundesRechteck(r, 8))
            using (Pen pen = new Pen(Color.FromArgb(180, 190, 205), 1.5f))
                e.Graphics.DrawPath(pen, path);
        }
        private GraphicsPath RundesRechteck(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); // oben links
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90); // oben rechts
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); // unten rechts
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); // unten links
            p.CloseFigure();
            return p;
        }
    }
}


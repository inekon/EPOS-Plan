using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PV : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private PhotovoltaikStammCtrl pvctrl = new PhotovoltaikStammCtrl();
        public List<WErzeugerModel> list_pvmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.PV_TYP;
        public int m_ID_Projekt = 0;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;
        int startindex = 100000;

        // =============================================================================
        // PV-Anlagenparameter (Paket A des PV-Ertragsmodells, Stufe E1.3)
        // =============================================================================
        //
        // PROGRAMMATISCH, nicht im Designer: Der Designer und die .resx dieses
        // Formulars werden nicht von Hand editiert (CLAUDE.md des Hauptprojekts) -
        // dieselbe Linie wie beim KI-Knopf oben. Die Beschriftungen kommen aus
        // MyResource (de + en), die Masse aus den Konstanten darunter.

        /// <summary>Breite von <c>panel1</c> - zwei Spalten nebeneinander.</summary>
        private const int PANEL_BREITE = 420;

        /// <summary>
        /// Hoehe von <c>panel1</c> (Paket B): vier Zeilen statt zwei. Der Bestand hatte
        /// 71 px fuer zwei Zeilen; Paket B braucht drei Wertezeilen und eine Knopfzeile.
        /// </summary>
        private const int PANEL_HOEHE = 128;

        /// <summary>Um wie viel alles UNTER dem Panel nach unten rueckt.</summary>
        private const int VERSATZ_UNTEN = PANEL_HOEHE - 71;

        // Spalte A (links): Neigung, Azimut, Anzahl Module - die Designer-Felder.
        private const int SP_A_LABEL = 8;
        private const int SP_A_LABEL_BREITE = 110;
        private const int SP_A_FELD = 120;
        private const int SP_A_FELD_BREITE = 54;

        // Spalte B (rechts): Rechenmodell, Wechselrichter-Wirkungsgrad, Systemverluste.
        private const int SP_B_LABEL = 186;
        private const int SP_B_LABEL_BREITE = 132;
        private const int SP_B_FELD = 322;
        private const int SP_B_FELD_BREITE = 58;
        private const int SP_B_COMBO_BREITE = 92;

        private const int ZEILE1 = 8, ZEILE2 = 35, ZEILE3 = 62, ZEILE4 = 90;

        private TextBox textBox_WrWirkungsgrad;
        private TextBox textBox_Systemverluste;
        private ComboBox comboBox_Modell;
        private Button btn_Wechselrichter;
        private ToolTip _tipPvAnlage;

        public Form_PV ()
        {
            InitializeComponent();

            // Dezenter Einstieg in den Assistenten, oben rechts im Client-Bereich
            // (Fachkonzept 11.8). Programmatisch, damit Designer und .resx
            // unberuehrt bleiben.
            KiAufrufKnopf.Anbringen(this);

            PvAnlagenfelderAnlegen();

            listBox_DB.Items.Clear();
            listBox_Auswahl.Items.Clear();
        }

        /// <summary>
        /// Baut das Panel „PV Anlage Eigenschaften" auf zwei Spalten und vier Zeilen um
        /// und legt die Felder der Stufen E1.3 und E2 an.
        ///
        /// <para><b>Warum die Bestandsfelder programmatisch umgesetzt werden.</b> Paket A
        /// hatte eine DRITTE Spalte bei x = 252 angehaengt; ihre Beschriftung stiess dort
        /// mit dem AutoSize-Label „Anzahl Module:" zusammen (x 177…282). Paket B braucht
        /// zusaetzlich eine Modellwahl und den Knopf zum Wechselrichterdialog — in zwei
        /// Zeilen zu 418 px ist dafuer kein Platz. Statt einer vierten Spalte stehen
        /// jetzt drei Wertezeilen in zwei sauber getrennten Spalten, darunter die
        /// Knopfzeile. Die Designer-Datei bleibt unberuehrt (Hausregel): Lage und Groesse
        /// der sechs Bestandscontrols werden hier gesetzt.</para>
        ///
        /// <para><b>Was mitwandert.</b> Das Panel waechst von 71 auf
        /// <see cref="PANEL_HOEHE"/> px; alles darunter (Beschriftung „Modul", der
        /// Modulblock <c>panel2</c> und die beiden Knoepfe) rueckt um
        /// <see cref="VERSATZ_UNTEN"/> px nach unten, die Maske entsprechend hoeher. Im
        /// Assistenten passt sich der Rahmen selbst an (<c>WizardParent.LoadNewForm</c>
        /// rechnet mit <c>PreferredSize</c> und schaltet AutoScroll ein). Der
        /// gestrichelte Rahmen in <c>Form_PV_Paint</c> liest Lage und Groesse zur
        /// Zeichenzeit und folgt automatisch.</para>
        /// </summary>
        private void PvAnlagenfelderAnlegen()
        {
            _tipPvAnlage = new ToolTip();

            panel1.Size = new Size(PANEL_BREITE, PANEL_HOEHE);

            // --- Bestandsfelder in das neue Raster ---------------------------------
            BestandsfeldSetzen(label3, textBox_Neigung, ZEILE1);
            BestandsfeldSetzen(label6, textBox_Azimut, ZEILE2);
            BestandsfeldSetzen(label7, textBox_AnlagenLeistung, ZEILE3);

            // --- Spalte B ----------------------------------------------------------
            comboBox_Modell = ModellfeldAnlegen(ZEILE1);

            textBox_WrWirkungsgrad = PvFeldAnlegen(MyResource.Resource.PV_ANLAGE_LABEL_WRWIRKUNGSGRAD,
                                                   MyResource.Resource.PV_ANLAGE_TIP_WRWIRKUNGSGRAD, ZEILE2);
            textBox_Systemverluste = PvFeldAnlegen(MyResource.Resource.PV_ANLAGE_LABEL_SYSTEMVERLUSTE,
                                                   MyResource.Resource.PV_ANLAGE_TIP_SYSTEMVERLUSTE, ZEILE3);

            btn_Wechselrichter = new Button();
            btn_Wechselrichter.Text = MyResource.Resource.PVM_ANLAGE_BTN_WECHSELRICHTER;
            btn_Wechselrichter.Location = new Point(SP_B_LABEL, ZEILE4);
            btn_Wechselrichter.Size = new Size(228, 26);
            btn_Wechselrichter.UseVisualStyleBackColor = true;
            btn_Wechselrichter.Click += btn_Wechselrichter_Click;
            panel1.Controls.Add(btn_Wechselrichter);
            _tipPvAnlage.SetToolTip(btn_Wechselrichter, MyResource.Resource.PVM_ANLAGE_TIP_WECHSELRICHTER);

            // --- alles unter dem Panel nach unten -----------------------------------
            label4.Top += VERSATZ_UNTEN;
            panel2.Top += VERSATZ_UNTEN;
            btn_OK.Top += VERSATZ_UNTEN;
            btn_Abbrechen.Top += VERSATZ_UNTEN;
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + VERSATZ_UNTEN);
        }

        /// <summary>
        /// Ein Designer-Feld in das neue Raster der Spalte A. <c>AutoSize</c> wird
        /// abgeschaltet, damit die Beschriftung nicht wieder in die Nachbarspalte
        /// hineinwaechst (genau daran lag die Ueberlappung des Pakets A).
        /// </summary>
        private static void BestandsfeldSetzen(Label lbl, TextBox feld, int oben)
        {
            lbl.AutoSize = false;
            lbl.Size = new Size(SP_A_LABEL_BREITE, 20);
            lbl.Location = new Point(SP_A_LABEL, oben + 3);

            feld.Location = new Point(SP_A_FELD, oben);
            feld.Size = new Size(SP_A_FELD_BREITE, 25);
        }

        /// <summary>Beschriftung + Zahlenfeld in der zweiten Spalte des Panels.</summary>
        private TextBox PvFeldAnlegen(string beschriftung, string hilfe, int oben)
        {
            SpaltenBeschriftung(beschriftung, hilfe, oben);

            TextBox tb = new TextBox();
            tb.Location = new Point(SP_B_FELD, oben);
            tb.Size = new Size(SP_B_FELD_BREITE, 25);
            tb.Font = new Font("Segoe UI", 10f);
            tb.TextAlign = HorizontalAlignment.Right;
            tb.TextChanged += (s, e) => Program.ZahlFaerben(s);
            panel1.Controls.Add(tb);

            _tipPvAnlage.SetToolTip(tb, hilfe);
            return tb;
        }

        /// <summary>
        /// Die Modellwahl (Stufe E2, Konzept N2.1). Die ANZEIGETEXTE stehen in
        /// MyResource, der PERSISTENZWERT in <c>DbWerte</c> — verbunden sind sie
        /// ausschliesslich ueber den Index (0 = einfach, 1 = erweitert). Ein
        /// Anzeigetext darf nie Steuerwert sein (Drei-Schichten-Regel).
        /// </summary>
        private ComboBox ModellfeldAnlegen(int oben)
        {
            SpaltenBeschriftung(MyResource.Resource.PVM_ANLAGE_LABEL_MODELL,
                                MyResource.Resource.PVM_ANLAGE_TIP_MODELL, oben);

            ComboBox cb = new ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Location = new Point(SP_B_FELD, oben);
            cb.Size = new Size(SP_B_COMBO_BREITE, 25);
            cb.Items.Add(MyResource.Resource.PVM_MODELL_EINFACH);
            cb.Items.Add(MyResource.Resource.PVM_MODELL_ERWEITERT);
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += (s, e) => ModellUmschalten();
            panel1.Controls.Add(cb);

            _tipPvAnlage.SetToolTip(cb, MyResource.Resource.PVM_ANLAGE_TIP_MODELL);
            return cb;
        }

        private void SpaltenBeschriftung(string beschriftung, string hilfe, int oben)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.AutoSize = false;
            lbl.Size = new Size(SP_B_LABEL_BREITE, 19);
            lbl.Location = new Point(SP_B_LABEL, oben + 5);
            lbl.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(0, 0, 192);   // wie die Bestandsbeschriftungen
            panel1.Controls.Add(lbl);
            _tipPvAnlage.SetToolTip(lbl, hilfe);
        }

        /// <summary>true, wenn die Maske gerade das ERWEITERTE Modell zeigt.</summary>
        private bool ModellIstErweitert()
        {
            return comboBox_Modell != null && comboBox_Modell.SelectedIndex == 1;
        }

        /// <summary>
        /// Enabled-Umschaltung nach der Modellwahl (Konzept N2.4: umschalten, nicht
        /// ausblenden). Der Wechselrichter-Wirkungsgrad wirkt NUR im einfachen Modell —
        /// im erweiterten ersetzt ihn die Teillastkennlinie. Die Systemverluste gelten
        /// in beiden.
        /// </summary>
        private void ModellUmschalten()
        {
            bool erweitert = ModellIstErweitert();
            if (textBox_WrWirkungsgrad != null) textBox_WrWirkungsgrad.Enabled = !erweitert;
            if (btn_Wechselrichter != null) btn_Wechselrichter.Enabled = erweitert;
            if (!m_bLaden) UpdateProerties();
        }

        /// <summary>
        /// Sperre gegen das Ereignisfeuer beim BEFUELLEN der Maske: Ohne sie schriebe
        /// <see cref="ModellUmschalten"/> waehrend des Umschaltens der Anlagenauswahl
        /// bereits wieder ins Modell zurueck.
        /// </summary>
        private bool m_bLaden;

        /// <summary>
        /// Der Wechselrichterdialog zur AUSGEWAEHLTEN Anlage (Stufe E2.1/E2.2). Die
        /// Werte gehen unmittelbar in das <c>WErzeugerModel</c> der Liste — derselbe
        /// Weg wie bei den Feldern des Panels.
        /// </summary>
        private void btn_Wechselrichter_Click(object sender, EventArgs e)
        {
            int index = -1;
            for (int i = 0; i < list_pvmodel.Count; i++)
                if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text &&
                    list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                { index = i; break; }

            if (index < 0) return;

            // kWp der AUSGEWAEHLTEN Anlage - Modulleistung [W] x Modulanzahl.
            double kwp = 0;
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + list_pvmodel[index].Bezeichner + "'");
            if (!rs.EOF()) kwp = (double)rs.Read("Leistung") * list_pvmodel[index].PV_Leistung / 1000.0;
            rs.Close();

            using (Form_PVModell dlg = new Form_PVModell(
                       list_pvmodel[index].Bezeichner, kwp, ModellIstErweitert(),
                       list_pvmodel[index].PV_WrNennleistungKw,
                       list_pvmodel[index].PV_WrEta10,
                       list_pvmodel[index].PV_WrEta50,
                       list_pvmodel[index].PV_WrEta100))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                list_pvmodel[index].PV_WrNennleistungKw = dlg.Nennleistung;
                list_pvmodel[index].PV_WrEta10 = dlg.Eta10;
                list_pvmodel[index].PV_WrEta50 = dlg.Eta50;
                list_pvmodel[index].PV_WrEta100 = dlg.Eta100;
            }
        }

        /// <summary>
        /// Anzeigetext eines Anlagenparameters: LEER, wenn er nicht gepflegt ist.
        /// Leer und 0 sind hier zwei verschiedene Aussagen - „es gilt der Vorgabewert"
        /// gegen „ausdruecklich 0".
        /// </summary>
        private static string PvWertText(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString() : "";
        }

        /// <summary>
        /// Der Feldwert eines Anlagenparameters: leer = <c>null</c> („Vorgabewert"),
        /// lesbare Zahl = der Wert, unlesbarer Text = <paramref name="bisher"/>.
        /// </summary>
        private static double? PvWertAusFeld(TextBox feld, double? bisher)
        {
            if (feld == null) return bisher;
            if (feld.Text.Trim().Length == 0) return null;

            double wert;
            return Program.ZahlParsen(feld.Text, out wert) ? (double?)wert : bisher;
        }

        public void SetControls(string projekt, bool bWizard = false)
        {
            if (bWizard)
            {
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_pvmodel = wizardparent.list_werzmodel;
            }

            listBox_Auswahl.Items.Clear();
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    listBox_Auswahl.Items.Add(list_pvmodel[i].Bezeichner);
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;

            textBox_Gesamtleistung.Text = UpdateGesamtleistung().ToString();
        }

        private void Form_PV_Load(object sender, EventArgs e)
        {
            pvctrl.ReadAll();
            for (int i = 0; i < pvctrl.rows; i++)
            {
                listBox_DB.Items.Add(pvctrl.items[i].m_szName);
            }

            comboBox_Hersteller.Items.Add("Alle");
            
            RecordSet rs = new RecordSet(); 
            rs.Open("SELECT Firma FROM Tab_PV_STAMM GROUP BY Firma ORDER BY Firma");
            while (rs.Next())
            {
                comboBox_Hersteller.Items.Add((string)rs.Read("Firma"));
            }
            rs.Close();

            comboBox_Hersteller.Text = "Alle";
        }

        private Form getWizardPage()
        {
            // P4: typisierte Erkennung ueber WizardParent.Aktiver. Die frueheren elf
            // Kopien suchten den Rahmen als Zeichenkette "WizardParent" in
            // Application.OpenForms; der Rahmen meldet sich jetzt selbst an.
            return WizardParent.Aktiver as Form;
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
           
            if (listBox_DB.Text == "") return;

            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + listBox_DB.Text + "'");
            if (rs.Next())
            {
                WErzeugerModel model = new WErzeugerModel();
                model.ID = startindex++;
                model.ID_Projekt = m_ID_Projekt;
                model.ID_PV = (int)rs.Read("ID");
                model.ID_Type = m_nType;
                model.Bezeichner = listBox_DB.Text;

                list_pvmodel.Add(model);
                if (m_bWizard) wizardparent.list_werzmodel = list_pvmodel;
            }
            rs.Close();

            listBox_Auswahl.Items.Add(listBox_DB.Text);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = listBox_Auswahl.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.SelectedIndex == -1) return;
            list_pvmodel.RemoveAt(listBox_Auswahl.SelectedIndex);
            listBox_Auswahl.Items.RemoveAt(listBox_Auswahl.SelectedIndex);
            if (m_bWizard) wizardparent.list_werzmodel = list_pvmodel;
        }

        /// <summary>
        /// Uebernimmt die Panel-Eingaben ins Modell (Aufrufer: panel1_Leave).
        /// Folgepaket zu ab5bf32: stille Parser statt Int32.Parse/double.Parse - ein
        /// ungueltiger Feldtext laesst den bisherigen Wert stehen, statt beim Verlassen
        /// des Panels eine FormatException zu werfen. Leer zaehlt weiter als 0.
        /// </summary>
        private void UpdateProerties()
        {
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text && list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    int neigung;
                    if (Program.GanzzahlParsen(textBox_Neigung.Text, out neigung) || textBox_Neigung.Text.Trim().Length == 0)
                        list_pvmodel[i].m_Neigung = neigung;

                    int azimut;
                    if (Program.GanzzahlParsen(textBox_Azimut.Text, out azimut) || textBox_Azimut.Text.Trim().Length == 0)
                        list_pvmodel[i].m_Azimut = azimut;

                    double anzahlModule;
                    if (Program.ZahlParsen(textBox_AnlagenLeistung.Text, out anzahlModule) || textBox_AnlagenLeistung.Text.Trim().Length == 0)
                        list_pvmodel[i].PV_Leistung = anzahlModule;

                    // E1.3: LEER heisst hier nicht 0, sondern "nicht gepflegt" - der
                    // Rechenweg setzt dann 0,95 bzw. 0 % ein. Ein unlesbarer Text laesst
                    // wie bei den Feldern darueber den bisherigen Wert stehen.
                    list_pvmodel[i].PV_WrWirkungsgrad = PvWertAusFeld(textBox_WrWirkungsgrad,
                                                                      list_pvmodel[i].PV_WrWirkungsgrad);
                    list_pvmodel[i].PV_Systemverluste = PvWertAusFeld(textBox_Systemverluste,
                                                                      list_pvmodel[i].PV_Systemverluste);

                    // E2: die Modellwahl. NULL bleibt NULL, solange "Einfach" steht -
                    // eine nie berührte Anlage bekommt durch das Öffnen der Maske keinen
                    // Persistenzwert und rechnet weiter den Paket-A-Weg.
                    list_pvmodel[i].PV_Modell = ModellIstErweitert()
                        ? DbWerte.PV_MODELL_ERWEITERT
                        : (SimulationPV.IstErweitert(list_pvmodel[i]) ? DbWerte.PV_MODELL_EINFACH
                                                                     : list_pvmodel[i].PV_Modell);

                    break;
                }
            }
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

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + listBox_Auswahl.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Hersteller.Text = (string)rs.Read("Firma");
                double kl = (double)rs.Read("Leistung");
                textBox_Leistung.Text = kl.ToString("F2");
            }
            rs.Close();

            m_bLaden = true;
            try
            {
                for (int i = 0; i < list_pvmodel.Count; i++)
                {
                    if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text && list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                    {
                        textBox_Neigung.Text = list_pvmodel[i].m_Neigung.ToString();
                        textBox_Azimut.Text = list_pvmodel[i].m_Azimut.ToString();
                        textBox_AnlagenLeistung.Text = list_pvmodel[i].PV_Leistung.ToString();
                        textBox_WrWirkungsgrad.Text = PvWertText(list_pvmodel[i].PV_WrWirkungsgrad);
                        textBox_Systemverluste.Text = PvWertText(list_pvmodel[i].PV_Systemverluste);
                        // E2: NULL, leer und PV_MODELL_EINFACH zeigen alle „Einfach".
                        comboBox_Modell.SelectedIndex =
                            SimulationPV.IstErweitert(list_pvmodel[i]) ? 1 : 0;
                        panel1.Visible = true;
                    }
                }
            }
            finally { m_bLaden = false; }

            ModellUmschalten();
            panel1.Visible = true;
        }

        private void listBox_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + listBox_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Hersteller.Text = (string)rs.Read("Firma");
                double kl = (double)rs.Read("Leistung");
                textBox_Leistung.Text = kl.ToString("F2");
                panel1.Visible = false; 

            }
            rs.Close();
            panel1.Visible = false;
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string sql = "";

            if (comboBox_Hersteller.Text == "Alle") szFilter = "Bezeichner Like '%'";
            else szFilter = "Firma='" + comboBox_Hersteller.Text + "'"; 

            listBox_DB.Items.Clear();
            sql = "select * from Tab_PV_STAMM where " + szFilter;
            rs.Open(sql);

            while (rs.Next())
            {
                listBox_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menuectrl = new MenueCtrl();

            int index = listBox_DB.SelectedIndex;
            listBox_Auswahl.SelectedItems.Clear();
            listBox_DB.SelectedItems.Clear();
            menuectrl.PV();
            listBox_DB.Items.Clear();
            pvctrl.ReadAll();
            
            for (int i = 0; i < pvctrl.rows; i++)
            {
                listBox_DB.Items.Add(pvctrl.items[i].m_szName);
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_DB.SelectedIndex == -1) { MessageBox.Show("Bitte ein Modul auswählen!"); return; }

            var result = MessageBox.Show("Wollen Sie wirklich das Modul löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (!pvctrl.Delete(listBox_DB.Text)) return;
                listBox_DB.Items.RemoveAt(listBox_DB.SelectedIndex);
            }
        }

        /// <summary>
        /// Nur noch Faerbung (Folgepaket zu ab5bf32). Das frueher hier eingesetzte
        /// Undo()/ClearUndo() loeste TextChanged erneut aus und liess Meldung und Text
        /// zwischen zwei Zustaenden pendeln; gemeldet wird jetzt erst beim Speichern.
        /// </summary>
        private void textBox_Neigung_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        /// <summary>Wie textBox_Neigung_TextChanged: nur Faerbung, keine Meldung.</summary>
        private void textBox_Azimut_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        /// <summary>
        /// Nur Faerbung; geprueft wird beim Speichern. Gefaerbt wird nach den
        /// Zahlregeln, weil der Speicherweg das Feld als double uebernimmt.
        /// </summary>
        private void textBox_AnlagenLeistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
            textBox_Gesamtleistung.Text = UpdateGesamtleistung().ToString();
        }

        private void Form_PV_Paint(object sender, PaintEventArgs e)
        {
            float[] dashValues = { 5, 2 };
            Pen blackPen = new Pen(Color.Gray, 1);
            blackPen.DashPattern = dashValues;

            int a, b, c, d;
            a = panel1.Left;
            b = panel1.Top;
            c = panel1.Width;
            d = panel1.Height;

            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + 10), new Point(a + c - 10, b + 10));
            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + d - 10), new Point(a + c - 10, b + d - 10));
            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + 10), new Point(a + 10, b + d - 10));
            e.Graphics.DrawLine(blackPen, new Point(a + c - 10, b + 10), new Point(a + c - 10, b + d - 10));
        }

        private void panel1_Leave(object sender, EventArgs e)
        {
            UpdateProerties();
            textBox_Gesamtleistung.Text = UpdateGesamtleistung().ToString(); 
        }

        private double UpdateGesamtleistung()
        {
            RecordSet rs = new RecordSet();
            double gesamtleistung = 0;
            double modulleistung = 0;

            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + list_pvmodel[i].Bezeichner + "'");
                    if (!rs.EOF())
                    {
                        modulleistung = (double)rs.Read("Leistung");
                    }
                    rs.Close();

                    gesamtleistung += (double)(list_pvmodel[i].PV_Leistung * modulleistung);
                }
            }
            return gesamtleistung;
        }
    }

}

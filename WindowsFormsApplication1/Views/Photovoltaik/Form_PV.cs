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

        /// <summary>Neue Breite von <c>panel1</c> - Platz fuer eine dritte Spalte.</summary>
        private const int PANEL_BREITE = 420;

        /// <summary>Linke Kante der dritten Spalte im Panel.</summary>
        private const int SPALTE_LINKS = 252;

        /// <summary>Feste Beschriftungsbreite - AutoSize koennte unter das Feld laufen.</summary>
        private const int LABEL_BREITE = 100;

        /// <summary>Breite der beiden neuen Eingabefelder.</summary>
        private const int FELD_BREITE = 58;

        private TextBox textBox_WrWirkungsgrad;
        private TextBox textBox_Systemverluste;
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
        /// Legt die dritte Spalte des Panels „PV Anlage Eigenschaften" an:
        /// Wechselrichter-Wirkungsgrad und Systemverluste (Stufe E1.3).
        ///
        /// <para>Das Panel waechst dafuer von 308 auf <see cref="PANEL_BREITE"/> px. Der
        /// gestrichelte Rahmen in <c>Form_PV_Paint</c> liest Lage und Groesse des Panels
        /// zur Zeichenzeit und folgt automatisch; rechts daneben beginnt erst bei x = 449
        /// die Herstellerspalte, die Breite ist also frei.</para>
        /// </summary>
        private void PvAnlagenfelderAnlegen()
        {
            panel1.Width = PANEL_BREITE;
            _tipPvAnlage = new ToolTip();

            textBox_WrWirkungsgrad = PvFeldAnlegen(MyResource.Resource.PV_ANLAGE_LABEL_WRWIRKUNGSGRAD,
                                                   MyResource.Resource.PV_ANLAGE_TIP_WRWIRKUNGSGRAD, 8);
            textBox_Systemverluste = PvFeldAnlegen(MyResource.Resource.PV_ANLAGE_LABEL_SYSTEMVERLUSTE,
                                                   MyResource.Resource.PV_ANLAGE_TIP_SYSTEMVERLUSTE, 35);
        }

        /// <summary>Beschriftung + Zahlenfeld in der dritten Spalte des Panels.</summary>
        private TextBox PvFeldAnlegen(string beschriftung, string hilfe, int oben)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.AutoSize = false;
            lbl.Size = new Size(LABEL_BREITE, 19);
            lbl.Location = new Point(SPALTE_LINKS, oben + 5);
            lbl.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(0, 0, 192);   // wie die Bestandsbeschriftungen
            panel1.Controls.Add(lbl);

            TextBox tb = new TextBox();
            tb.Location = new Point(SPALTE_LINKS + LABEL_BREITE + 4, oben);
            tb.Size = new Size(FELD_BREITE, 25);
            tb.Font = new Font("Segoe UI", 10f);
            tb.TextAlign = HorizontalAlignment.Right;
            tb.TextChanged += (s, e) => Program.ZahlFaerben(s);
            panel1.Controls.Add(tb);

            _tipPvAnlage.SetToolTip(lbl, hilfe);
            _tipPvAnlage.SetToolTip(tb, hilfe);
            return tb;
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

            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text && list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    textBox_Neigung.Text = list_pvmodel[i].m_Neigung.ToString();
                    textBox_Azimut.Text = list_pvmodel[i].m_Azimut.ToString();
                    textBox_AnlagenLeistung.Text = list_pvmodel[i].PV_Leistung.ToString();
                    textBox_WrWirkungsgrad.Text = PvWertText(list_pvmodel[i].PV_WrWirkungsgrad);
                    textBox_Systemverluste.Text = PvWertText(list_pvmodel[i].PV_Systemverluste);
                    panel1.Visible = true;
                }
            }
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

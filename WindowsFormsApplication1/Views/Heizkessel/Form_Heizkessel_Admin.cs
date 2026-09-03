using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_Admin : BaseForm
    {
        private HeizkesselStammCtrl heizkesselctrl = new HeizkesselStammCtrl();
        public int m_ID_Projekt = 0;
  
        public Form_Heizkessel_Admin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            listBox_Kessel_DB.Items.Clear();

            InitSpeichern();
        }

        private void Form_Heizkessel_Load(object sender, EventArgs e)
        {
            LoadDBHeizkessel();

            comboBox_Brennstoffart.Items.Add("Alle");
            for (int i = 0; i < heizkesselctrl.Brennstoffart_Gruppe.Count; i++)
            {
                comboBox_Brennstoffart.Items.Add(heizkesselctrl.Brennstoffart_Gruppe[i]);
            }

            comboBox_Leistung.Items.Add("Alle");
            comboBox_Leistung.Items.Add("bis 50 kW");
            comboBox_Leistung.Items.Add(">50 bis 200 kW");
            comboBox_Leistung.Items.Add(">200 bis 500 kW");
            comboBox_Leistung.Items.Add(">500 bis 1.000 kW");
            comboBox_Leistung.Items.Add("über 1.000 kW");
            comboBox_Leistung.Text = "Alle";
            comboBox_Brennstoffart.Text = "Alle";   
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        // Waehrend des Fuellens ist m_bFuellen gesetzt, damit das programmatische
        // Schreiben der Anzeigefelder nicht als Anwenderaenderung gilt.
        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            m_bFuellen = true;

            rs.Open("select * from [Tab_Heizkessel_STAMM] where Bezeichner='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Bezeichner");
                textBox_Kesselbeschreibung.Text = rs.GetString("Beschreibung");
                textBox_Brennstoff.Text = heizkesselctrl.Brennstoffart[(int)rs.Read("Brennstoff")-1].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
                checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");    
                textBox_Vorlauf.Text = rs.Read("Vorlauf") == DBNull.Value ? "" : ((int)rs.Read("Vorlauf")).ToString();
                textBox_Ruecklauf.Text = rs.Read("Ruecklauf") == DBNull.Value ? "" : ((int)rs.Read("Ruecklauf")).ToString();
                m_szGeladen = (string)rs.Read("Bezeichner");
            }
            else
            {
                m_szGeladen = "";
            }
            rs.Close();

            m_bFuellen = false;
            m_bGeaendert = false;
            SpeicherKnopfAktualisieren();
        }

        private void comboBox_Brennstoffart_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

            // Vorbelegung "alle Leistungen" (gleiche Fehlerklasse wie B0-10 im Pufferspeicher):
            // ohne Treffer in der Literalkette blieb der Leistungsteil sonst leer und das
            // SQL endete in "... and  order by ...". Auslöser ist Freitext in der
            // editierbaren ComboBox; das Symptom war eine stumme Leerliste.
            szFilterLeistung = "Ptherm Like '%'";
            if (comboBox_Leistung.Text == "Alle" || comboBox_Leistung.Text == "") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "bis 50 kW") szFilterLeistung = "Ptherm <50";
            else if (comboBox_Leistung.Text == ">50 bis 200 kW") szFilterLeistung = "Ptherm >=50 and Ptherm <200";
            else if (comboBox_Leistung.Text == ">200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == ">500 bis 1.000 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <1000";
            else if (comboBox_Leistung.Text == "über 1.000 kW") szFilterLeistung = "Ptherm >=1000";

            if (comboBox_Brennstoffart.Text == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (comboBox_Brennstoffart.Text == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (comboBox_Brennstoffart.Text == "Koks") szFilter = "Brennstoff=10";
            else if (comboBox_Brennstoffart.Text == "Kohle") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoffart.Text == "Holz") szFilter = "Brennstoff=12";
            else if (comboBox_Brennstoffart.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoffart.Text == "Strom") szFilter = "Brennstoff=13";
            else if (comboBox_Brennstoffart.Text == "Pellets") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoffart.Text == "Rapsöl") szFilter = "Brennstoff=16";
            else if (comboBox_Brennstoffart.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoffart.Text == "Fernwärme") szFilter = "Brennstoff=23";
            else if (comboBox_Brennstoffart.Text == "Sonstige Energieträger") szFilter = "Brennstoff=24";
            else if (comboBox_Brennstoffart.Text == "Wasserstoff") szFilter = "Brennstoff=25";
            else if (comboBox_Brennstoffart.Text == "Alle") szFilter = "Brennstoff Like '%'";

            listBox_Kessel_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from [Tab_Heizkessel_STAMM] where " + szFilterLeistung + " order by Bezeichner";
            else
                sql = "select * from [Tab_Heizkessel_STAMM] where " + szFilter + " and " + szFilterLeistung + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_Kessel_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        /// <summary>
        /// OK speichert offene Aenderungen und schliesst danach. Vorher schloss der
        /// Knopf nur (Befund 18.08.2026): saemtliche Infofelder dieser Maske sind
        /// editierbar - weder Designer noch .resx sperren eines davon -, ihre
        /// Eingaben wurden aber nirgends zurueckgeschrieben und gingen beim
        /// Schliessen still verloren. Ist nichts geaendert, verhaelt sich OK genau
        /// wie frueher: es wird nicht geschrieben und nichts gefragt.
        /// </summary>
        private void btn_OK_Click(object sender, EventArgs e)
        {
            if (m_bGeaendert && !SpeichereStammsatz()) return;   // Dialog offen lassen
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();
            if(listBox_Kessel_DB.Text == "") return;    
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Kessel_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            if (!ctrl.Delete(listBox_Kessel_DB.Text)) return;
            listBox_Kessel_DB.Items.Remove(listBox_Kessel_DB.Text); 
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Bearbeiten frm = new Form_Heizkessel_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_EDIT);
            if(listBox_Kessel_DB.Text == "") return;
            frm.SetControls(listBox_Kessel_DB.Text, textBox_Kesselbeschreibung.Text);
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szKessel;
                LoadDBHeizkessel();
                listBox_Kessel_DB.Text = szKessel;
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Bearbeiten frm = new Form_Heizkessel_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_NEU);
            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                RecordSet rs = new RecordSet();
                rs.Open("select Bezeichner from [Tab_Heizkessel_STAMM] where Bezeichner='" + szName + "'");
                bool bExist = !rs.EOF();
                rs.Close();

                if (bExist)
                {
                    MessageBox.Show("Name existiert bereits!");
                }
                else
                {
                    frm.SetControls(szName, "");

                    DialogResult ret = frm.ShowDialog();
                    if (ret == DialogResult.OK)
                    {
                        string szKessel = frm.m_szKessel;
                        LoadDBHeizkessel();
                        listBox_Kessel_DB.Text = szKessel;
                    }
                }
            }
        }

        // Folgepaket zu ab5bf32: Statt modal zu melden und mit Undo() zu pendeln, wird
        // ungültiger Text nur noch eingefärbt. Der früher hier stehende Zusatz „werden
        // nirgends gespeichert“ stimmt seit dem Speichern-Knopf nicht mehr: gemeldet wird
        // beim Speichern (Program.ZahlPruefen), gefärbt weiterhin beim Tippen.
        private void textBox_Kesselleistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void LoadDBHeizkessel()
        {
            listBox_Kessel_DB.Items.Clear();
            heizkesselctrl.ReadAll();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);
            }
        }

        #region --- Speichern (nicht schliessend) ---

        // Befund 18.08.2026: Diese Maske zeigt die Kessel-Stammdaten editierbar an -
        // weder .Designer.cs noch .resx sperren eines der Felder -, hatte aber KEINEN
        // Speicherweg: btn_OK_Click rief bloss Close(). Derselbe Fehler wie in
        // Form_BHKWAdmin. "Speichern" schreibt jetzt und laesst den Dialog offen,
        // OK schreibt und schliesst danach.

        private SpeichernLeiste leiste;

        /// <summary>Bezeichner des angezeigten Satzes - Schluessel des UPDATE.</summary>
        private string m_szGeladen = "";

        /// <summary>true, solange die Auswahl die Anzeigefelder programmatisch setzt.</summary>
        private bool m_bFuellen;

        /// <summary>true, sobald der Anwender eines der Speicherfelder geaendert hat.</summary>
        private bool m_bGeaendert;

        /// <summary>
        /// Haengt Speichern-Knopf und Statuszeile ein. Der Knopf sitzt in der freien
        /// Flaeche zwischen "Neu..." und OK, die Statuszeile in der Zeile darueber
        /// (unter den Vorlauf/Ruecklauf-Feldern); die Fenstergroesse aendert sich nicht.
        /// </summary>
        private void InitSpeichern()
        {
            // Kesselname ist der Schluessel des UPDATE (WHERE Bezeichner = ?), ein hier
            // geaenderter Name wuerde ins Leere schreiben. Die Brennstoffart ist eine
            // reine Nachschlage-Anzeige (Text statt ID) und kennt keinen Rueckweg.
            // Beides also nur lesen; geaendert wird es ueber "Bearbeiten...".
            textBox_Kesselname.ReadOnly = true;
            textBox_Brennstoff.ReadOnly = true;

            Rectangle rStatus = new Rectangle(textBox_Kesselname.Left, btn_OK.Top - 22,
                                              btn_OK.Right - textBox_Kesselname.Left, 18);
            leiste = new SpeichernLeiste(this, btn_OK, rStatus, btn_Speichern_Click);

            foreach (Control c in Speicherfelder()) c.TextChanged += Speicherfeld_Geaendert;
            checkBox_Brennwert.CheckedChanged += Speicherfeld_Geaendert;

            // Vorlauf/Ruecklauf sind Ganzzahlen; die beiden Zahlfelder faerben bereits
            // ueber ihre vorhandenen TextChanged-Handler.
            textBox_Vorlauf.Validating += (s, e) => Program.GanzzahlFaerben(s);
            textBox_Ruecklauf.Validating += (s, e) => Program.GanzzahlFaerben(s);

            SpeicherKnopfAktualisieren();
        }

        /// <summary>Genau die Textfelder, die der Speicherweg zurueckschreibt.</summary>
        private Control[] Speicherfelder()
        {
            return new Control[]
            {
                textBox_Kesselbeschreibung, textBox_Kesselleistung,
                textBox_Investitionskosten, textBox_Vorlauf, textBox_Ruecklauf
            };
        }

        private void Speicherfeld_Geaendert(object sender, EventArgs e)
        {
            if (m_bFuellen) return;          // programmatisches Fuellen ist keine Aenderung
            m_bGeaendert = true;
            if (leiste != null) leiste.Leeren();
            SpeicherKnopfAktualisieren();
        }

        private void SpeicherKnopfAktualisieren()
        {
            if (leiste == null) return;
            leiste.Zustand(!string.IsNullOrEmpty(m_szGeladen), m_bGeaendert);
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            SpeichereStammsatz();       // schliesst bewusst NICHT
        }

        /// <summary>
        /// Schreibt die angezeigten Felder in den Kessel-Stammsatz zurueck. Liefert
        /// false, wenn nichts geschrieben wurde - der Aufrufer laesst den Dialog dann
        /// offen.
        /// </summary>
        private bool SpeichereStammsatz()
        {
            if (string.IsNullOrEmpty(m_szGeladen)) return false;

            double dPtherm, dInvest;
            int nVorlauf, nRuecklauf;
            if (!Program.ZahlPruefen(textBox_Kesselleistung, label14.Text.TrimEnd(' ', ':'), out dPtherm, true)) return false;
            if (!Program.ZahlPruefen(textBox_Investitionskosten, label3.Text.TrimEnd(' ', ':'), out dInvest, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Vorlauf, label49.Text.TrimEnd(' ', ':'), out nVorlauf, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Ruecklauf, label48.Text.TrimEnd(' ', ':'), out nRuecklauf, true)) return false;

            // Tab_Heizkessel_STAMM fuehrt keinen eindeutigen Schluessel auf Bezeichner,
            // HeizkesselStammCtrl.Update() filtert aber genau darauf. Bei einer Dublette
            // wuerden beide Saetze zugleich ueberschrieben - deshalb hier abbrechen,
            // statt unbemerkt zwei Katalogsaetze zu veraendern.
            object anz = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + HeizkesselStammCtrl.TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@nam", m_szGeladen));
            int nAnzahl = (anz == null || anz == DBNull.Value) ? 0 : Convert.ToInt32(anz);
            if (nAnzahl > 1)
            {
                MessageBox.Show(
                    string.Format(MyResource.Resource.ADM_MEHRDEUTIG_TEXT, m_szGeladen, nAnzahl),
                    MyResource.Resource.ADM_MEHRDEUTIG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                leiste.Fehler();
                return false;
            }

            // Satz VOLLSTAENDIG lesen und nur in den angezeigten Feldern aendern:
            // Update() schreibt alle Spalten, ein halb gefuelltes Model wuerde
            // Wirkungsgrade, Emissionen und Wartungskosten nullen.
            HeizkesselStammCtrl schreiber = new HeizkesselStammCtrl();
            schreiber.ReadSingle(m_szGeladen);
            if (schreiber.rows == 0) { leiste.Fehler(); return false; }

            schreiber.Beschreibung = textBox_Kesselbeschreibung.Text;
            schreiber.Ptherm = dPtherm;
            schreiber.Investitionskosten = dInvest;
            schreiber.Brennwert = checkBox_Brennwert.Checked;
            schreiber.Vorlauf = nVorlauf;
            schreiber.Ruecklauf = nRuecklauf;

            // Update() meldet schreibgeschuetzte Katalogsaetze selbst und liefert false;
            // eine Uebergehen-Freigabe wie bei den BHKW-Stammdaten gibt es hier nicht.
            if (!schreiber.Update()) { leiste.Fehler(); return false; }

            m_bGeaendert = false;
            leiste.Gespeichert();
            SpeicherKnopfAktualisieren();
            return true;
        }

        #endregion
    }
}

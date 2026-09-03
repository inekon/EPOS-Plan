using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_BHKWAdmin : Form
    {
        // Admin-Dialog bearbeitet jetzt die Stammdaten-Tabelle Tab_BHKW_STAMM.
        BHKWStammCtrl ctrl = new BHKWStammCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.BHKW_TYP;
        public int m_ID_Projekt = 0;

        public Form_BHKWAdmin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt

            DataGridView dgv = dataGridView1;
            dgv.AutoGenerateColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);

            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Name",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 50
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Eigenschaften",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 50
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;

			// Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);

            InitSpeichern();
        }

        /// <summary>
        /// OK speichert offene Aenderungen und schliesst danach. Vorher schloss der
        /// Knopf nur (Befund 18.08.2026): die Gruppe "Info markiertes BHKW" fuehrt
        /// editierbare Felder, deren Eingaben nirgends zurueckgeschrieben wurden und
        /// beim Schliessen still verloren gingen. Ist nichts geaendert, verhaelt sich
        /// OK genau wie frueher - es wird nicht geschrieben und nichts gefragt.
        /// </summary>
        private void btn_OK_Click(object sender, EventArgs e)
        {
            if (m_bGeaendert && !SpeichereStammsatz()) return;   // Dialog offen lassen
            Close();
        }

        private void Form_BHKWEing_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
            SetControls();
            SetFilter();
        }

        public void SetControls()
        {
            SetFilter();

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            comboBox_Brennstoff.Items.Clear();
            comboBox_Leistung.Items.Clear();

            comboBox_Brennstoff.Items.Add("Alle");
            for (int i = 0; i <  ctrl.Brennstoffart_Gruppe.Count; i++)
            {
                comboBox_Brennstoff.Items.Add(ctrl.Brennstoffart_Gruppe[i]);
            }

            comboBox_Leistung.Items.Add("Alle");
            for (int i = 0; i < BHKWStammCtrl.LeistungText.Length; i++)
            {
                if (BHKWStammCtrl.LeistungText[i] != "")
                    comboBox_Leistung.Items.Add(BHKWStammCtrl.LeistungText[i]);
            }

            comboBox_Brennstoff.SelectedIndex = 0;
            comboBox_Leistung.SelectedIndex = 0;

            dataGridView1.Select();
            if (dataGridView1.Rows.Count > 0)
                dataGridView1.Rows[0].Cells[0].Selected = true;
        }

        // Liest die Detail-Felder für den ausgewählten STAMM-Datensatz.
        // Waehrend des Fuellens ist m_bFuellen gesetzt, damit das programmatische
        // Schreiben der TextBoxen nicht als Anwenderaenderung gilt.
        private void FillDetails(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE Bezeichner = ?",
                new DbParam("@name", szName));

            if (dt == null || dt.Rows.Count == 0) { SetzeGeladenenSatz(""); return; }
            DataRow r = dt.Rows[0];

            m_bFuellen = true;
            try
            {
                textBox_Name.Text = r["Bezeichner"].ToString();
                textBox_Firma.Text = r["Firma"] == DBNull.Value ? "" : r["Firma"].ToString();
                textBox_Beschreibung.Text = r["Beschreibung"] == DBNull.Value ? "" : r["Beschreibung"].ToString();
                textBox_Leistung_th.Text = r["Ptherm"].ToString();
                textBox_Leistung_el.Text = r["Pel"].ToString();
                textBox_M_GrenzL.Text = r["Grenzleistung"].ToString();
                textBox_Vorlauf.Text = r["Vorlauf"].ToString();
                textBox_Ruecklauf.Text = r["Ruecklauf"].ToString();
            }
            finally
            {
                m_bFuellen = false;
            }

            SetzeGeladenenSatz(r["Bezeichner"].ToString());
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            FillDetails((string)dataGridView1.CurrentRow.Cells[0].Value);
        }

        // Filterliste basiert jetzt auf den STAMM-Daten (Tab_BHKW_STAMM), gelesen über DataRepository.
        private void SetFilter()
        {
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

            if (comboBox_Leistung.Text == "Alle") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "kleiner 20 kW") szFilterLeistung = "Ptherm <20";
            else if (comboBox_Leistung.Text == "20 bis 40 kW") szFilterLeistung = "Ptherm >=20 and Ptherm <40";
            else if (comboBox_Leistung.Text == "40 bis 80 kW") szFilterLeistung = "Ptherm >=40 and Ptherm <80";
            else if (comboBox_Leistung.Text == "80 bis 200 kW") szFilterLeistung = "Ptherm >=80 and Ptherm <200";
            else if (comboBox_Leistung.Text == "200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == "500 bis 800 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <800";
            else if (comboBox_Leistung.Text == "800 bis 1200 kW") szFilterLeistung = "Ptherm >=800 and Ptherm <1200";
            else if (comboBox_Leistung.Text == "über 1.200 kW") szFilterLeistung = "Ptherm >=1200";

            if (comboBox_Brennstoff.Text == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (comboBox_Brennstoff.Text == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (comboBox_Brennstoff.Text == "Koks") szFilter = "Brennstoff=10";
            else if (comboBox_Brennstoff.Text == "Kohle") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoff.Text == "Holz") szFilter = "Brennstoff=12";
            else if (comboBox_Brennstoff.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoff.Text == "Strom") szFilter = "Brennstoff=13";
            else if (comboBox_Brennstoff.Text == "Pellets") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoff.Text == "Rapsöl") szFilter = "Brennstoff=16";
            else if (comboBox_Brennstoff.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoff.Text == "Fernwärme") szFilter = "Brennstoff=23";
            else if (comboBox_Brennstoff.Text == "Sonstige Energieträger") szFilter = "Brennstoff=24";
            else if (comboBox_Brennstoff.Text == "Wasserstoff") szFilter = "Brennstoff=25";
            else if (comboBox_Brennstoff.Text == "Alle") szFilter = "Brennstoff Like '%'";

            if (szFilterLeistung == "") szFilterLeistung = "Ptherm Like '%'";

            if (szFilter == "")
                sql = "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE " + szFilterLeistung + " ORDER BY Bezeichner";
            else
                sql = "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE (" + szFilter + ") and " + szFilterLeistung + " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);

            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                int brennIdx = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
                string brennText = (brennIdx >= 1 && brennIdx <= ctrl.Brennstoffart.Count) ? ctrl.Brennstoffart[brennIdx - 1] : "";
                bool ro = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);

                dgv.Rows.Add(
                    row["Bezeichner"].ToString(),
                    row["Firma"].ToString() + "\nBrennstoff: " + brennText +
                    "\nPtherm: " + row["Ptherm"].ToString() + " kW" +
                    "\nPel: " + row["Pel"].ToString() + " kW");
                // Schreibgeschützte (ReadOnly) Datensätze optisch grau kennzeichnen
                if (ro)
                    dgv.Rows[i].DefaultCellStyle.ForeColor = Color.Gray;
                dgv.Rows[i++].DividerHeight = 5;
            }
        }

        private void comboBox_Brennstoff_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        // Liefert den Bezeichner der aktuell gewählten Zeile.
        private string SelectedBezeichner()
        {
            if (dataGridView1.CurrentRow == null) return "";
            return (string)dataGridView1.CurrentRow.Cells[0].Value;
        }

        private void btn_DBBHKW_Edit_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr =  dataGridView1.SelectedRows;
            if(sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            string szName = SelectedBezeichner();

            // Editor ist auch für schreibgeschützte (ReadOnly) Datensätze aufrufbar;
            // dort ist lediglich der "Überschreiben"-Button gesperrt.
            Form_DBBHKW frm = new Form_DBBHKW();
            frm.m_mode = Form_DBBHKW.MODE_EDIT;
            frm.SetControls(szName);
            frm.ShowDialog();
            SetFilter();
        }

        private void btn_DBBHKW_Neu_Click(object sender, EventArgs e)
        {
            Form_DBBHKW frm = new Form_DBBHKW();
            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                frm.m_mode = Form_DBBHKW.MODE_NEU;
                frm.SetControls(szName);
                frm.m_szName = szName;
                frm.ShowDialog();
                SetFilter();
            }
        }

        private void btn_DBBHKW_Löschen_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            string szName = SelectedBezeichner();

            // ReadOnly-Schutz: schreibgeschützte Datensätze nicht löschbar
            if (ctrl.IsReadOnly(szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + szName + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            // Löschen über den Stamm-Controller (nutzt DataRepository, prüft ReadOnly erneut)
            if (ctrl.Delete(szName))
            {
                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            FillDetails(SelectedBezeichner());
        }

        #region --- Speichern (nicht schliessend) ---

        // Befund 18.08.2026: Die Gruppe "Info markiertes BHKW" zeigt die Stammdaten
        // editierbar an (nur "Beschreibung" ist im Designer ReadOnly, die .resx sperrt
        // nichts), es gab aber KEINEN Speicherweg - btn_OK_Click rief bloss Close().
        // Eingaben in "Untere Grenzleistung", "Vorlauf" und "Ruecklauf" gingen deshalb
        // still verloren. Dieser Block ergaenzt den fehlenden Weg: "Speichern" schreibt
        // und laesst den Dialog offen, OK schreibt und schliesst danach.

        private SpeichernLeiste leiste;

        /// <summary>Bezeichner des angezeigten Satzes - Schluessel des UPDATE.</summary>
        private string m_szGeladen = "";

        /// <summary>true, solange FillDetails die Felder programmatisch setzt.</summary>
        private bool m_bFuellen;

        /// <summary>true, sobald der Anwender eines der Speicherfelder geaendert hat.</summary>
        private bool m_bGeaendert;

        /// <summary>
        /// Haengt Speichern-Knopf und Statuszeile ein. Der Knopf sitzt links neben OK,
        /// die Statuszeile in der freien Flaeche darunter zwischen Info-Gruppe und
        /// Knopf; die Fenstergroesse aendert sich nicht.
        /// </summary>
        private void InitSpeichern()
        {
            // Der Modul-Name ist der Schluessel des UPDATE (WHERE Bezeichner = ?).
            // Ein hier geaenderter Name wuerde ins Leere schreiben, deshalb nur lesen -
            // Umbenennen laeuft ueber "Bearbeiten..." und dort "Speichern unter".
            textBox_Name.ReadOnly = true;

            // Linke Kante des neuen Knopfes (SpeichernLeiste setzt sie genauso);
            // die Statuszeile fuellt den Platz davor bis zur Info-Gruppe.
            int nKnopfLinks = btn_OK.Left - btn_OK.Width - SpeichernLeiste.ABSTAND;
            Rectangle rStatus = new Rectangle(groupBox2.Left, btn_OK.Top,
                                              nKnopfLinks - groupBox2.Left - 8, btn_OK.Height);
            leiste = new SpeichernLeiste(this, btn_OK, rStatus, btn_Speichern_Click);

            foreach (TextBox tb in Speicherfelder()) tb.TextChanged += Speicherfeld_TextChanged;

            // Zahlfelder faerben nur (Program.ZahlFaerben/GanzzahlFaerben); gemeldet
            // wird erst am Knopf - dasselbe Muster wie Form_AdminStromspeicher.
            textBox_Leistung_th.Validating += (s, e) => Program.ZahlFaerben(s);
            textBox_Leistung_el.Validating += (s, e) => Program.ZahlFaerben(s);
            textBox_M_GrenzL.Validating += (s, e) => Program.ZahlFaerben(s);
            textBox_Vorlauf.Validating += (s, e) => Program.GanzzahlFaerben(s);
            textBox_Ruecklauf.Validating += (s, e) => Program.GanzzahlFaerben(s);

            SpeicherKnopfAktualisieren();
        }

        /// <summary>Genau die Felder, die der Speicherweg zurueckschreibt.</summary>
        private TextBox[] Speicherfelder()
        {
            return new TextBox[]
            {
                textBox_Firma, textBox_Leistung_th, textBox_Leistung_el,
                textBox_M_GrenzL, textBox_Vorlauf, textBox_Ruecklauf
            };
        }

        private void Speicherfeld_TextChanged(object sender, EventArgs e)
        {
            if (m_bFuellen) return;          // programmatisches Fuellen ist keine Aenderung
            m_bGeaendert = true;
            if (leiste != null) leiste.Leeren();
            SpeicherKnopfAktualisieren();
        }

        /// <summary>Merkt den angezeigten Satz und setzt den Aenderungsstand zurueck.</summary>
        private void SetzeGeladenenSatz(string szBezeichner)
        {
            m_szGeladen = szBezeichner ?? "";
            m_bGeaendert = false;
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

        /// <summary>Beschriftung eines Labels als Feldname fuer Pruefmeldungen.</summary>
        private static string Beschriftung(Label lbl)
        {
            return lbl == null ? "" : lbl.Text.TrimEnd(' ', ':');
        }

        /// <summary>
        /// Schreibt die angezeigten Felder in den Stammdatensatz zurueck. Liefert
        /// false, wenn nichts geschrieben wurde (ungueltige Eingabe, abgelehnter
        /// Schreibschutz, DB-Fehler) - der Aufrufer laesst den Dialog dann offen.
        /// </summary>
        private bool SpeichereStammsatz()
        {
            if (string.IsNullOrEmpty(m_szGeladen)) return false;

            double dPtherm, dPel, dGrenz;
            int nVorlauf, nRuecklauf;
            if (!Program.ZahlPruefen(textBox_Leistung_th, Beschriftung(Label12), out dPtherm, true)) return false;
            if (!Program.ZahlPruefen(textBox_Leistung_el, Beschriftung(Label14), out dPel, true)) return false;
            if (!Program.ZahlPruefen(textBox_M_GrenzL, Beschriftung(Label4), out dGrenz, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Vorlauf, Beschriftung(label49), out nVorlauf, true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Ruecklauf, Beschriftung(label48), out nRuecklauf, true)) return false;

            // Der Satz wird VOLLSTAENDIG gelesen und nur in den angezeigten Feldern
            // geaendert: BHKWStammCtrl.Update() schreibt alle Spalten, ein halb
            // gefuelltes Model wuerde Kosten, Emissionen und Wirkungsgrad nullen.
            BHKWStammModel m = ctrl.ReadModel(m_szGeladen);
            if (m == null) { leiste.Fehler(); return false; }

            m.m_szFirma = textBox_Firma.Text;
            m.m_Ptherm = dPtherm;
            m.m_Pel = dPel;
            m.m_Grenzleistung = dGrenz;
            m.m_Vorlauf = nVorlauf;
            m.m_Ruecklauf = nRuecklauf;

            BHKWStammCtrl schreiber = new BHKWStammCtrl();
            schreiber.model = m;

            // Auslieferungskatalog: einmal ausdruecklich nachfragen und den Schutz nur
            // fuer genau diesen Schreibvorgang aufheben - gleiche Regel und gleiche
            // Frage wie beim Knopf "Ueberschreiben" in Form_DBBHKW. In der
            // Auslieferungsdatenbank sind ALLE Saetze von Tab_BHKW_STAMM
            // schreibgeschuetzt, die Rueckfrage ist hier also der Regelfall.
            if (m.m_bReadOnly)
            {
                if (MessageBox.Show(
                        string.Format(MyResource.Resource.ADM_SCHUTZ_FRAGE, m_szGeladen),
                        MyResource.Resource.ADM_SCHUTZ_TITEL,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    leiste.Fehler();
                    return false;
                }
                schreiber.SchreibschutzUebergehen = true;
            }

            if (!schreiber.Update()) { leiste.Fehler(); return false; }

            m_bGeaendert = false;
            leiste.Gespeichert();
            ListeAktualisieren(m_szGeladen);
            return true;
        }

        /// <summary>
        /// Baut die Liste neu (geaenderte Leistungen stehen in der Spalte
        /// "Eigenschaften") und stellt die Markierung auf denselben Satz zurueck.
        /// Faellt der Satz durch den eingestellten Leistungsfilter, bleibt keine
        /// Markierung uebrig - dann sperrt sich der Speichern-Knopf wieder.
        /// </summary>
        private void ListeAktualisieren(string szBezeichner)
        {
            SetFilter();

            bool bGefunden = false;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToString(row.Cells[0].Value) != szBezeichner) continue;
                row.Selected = true;
                dataGridView1.CurrentCell = row.Cells[0];
                bGefunden = true;
                break;
            }
            if (!bGefunden) SetzeGeladenenSatz("");
        }

        #endregion
    }

}

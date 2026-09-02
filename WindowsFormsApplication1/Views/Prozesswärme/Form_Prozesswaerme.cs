using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Prozesswaerme : Form
    {
        public List<Z_ProjektProzesswaermeModel> list_pwmodel = new List<Z_ProjektProzesswaermeModel>();
        public int m_ID_Projekt = 0;
        private int startindex = 100000;
        private SimulationWaermebedarf simulation = new SimulationWaermebedarf();
        private string m_szProjekt;
        private int m_ListIndex = 0;
        private bool m_bWizard = false;
        // Hinweis "Projekt noch nicht gespeichert" nur einmal je Wizard-Sitzung zeigen.
        private bool m_bHinweisNeuesProjekt = false;

        public Form_Prozesswaerme()
        {
            InitializeComponent();
            dataGridView1.Rows.Clear();
            listView_Prozess_Auswahl.Items.Clear();
            listView_Prozess_Auswahl.View = View.Details;
            listView_Prozess_Auswahl.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Prozess_Auswahl.Columns[0].Width = listView_Prozess_Auswahl.ClientRectangle.Width;

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
                FillWeight = 60
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Typ",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;
            //dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            //dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);
			
			// Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);

            // D2 (28.08.2026): Fusszeile auf die Norm - bisher Abbrechen links von OK,
            // Groesse 105x31 und ohne Anker. btn_neuerWert ("Uebernehmen", 17/53 mitten
            // in der Maske) bleibt unangetastet: die Norm bewegt nur uebergebene Knoepfe.
            // Im Assistentenbetrieb sind beide Knoepfe unsichtbar (SetControls, bWizard).
            FusszeilenNorm.Einhaengen(this, btn_OK, btn_Abbrechen);

            // Auf derselben Zeile (y = 544) stehen links die beiden Arbeitsknoepfe
            // "monatlicher Verlauf" und "Simulation". Sie behalten Lage und Groesse -
            // ihre Beschriftung braucht die Breite - und bekommen nur den unteren Anker,
            // damit die ganze Zeile beim Aufziehen des Fensters mitwandert.
            FusszeilenNorm.ZeileMitziehen(btn_ErgebnisseVerbrauch, btn_Simulation);

            SchriftAngleichen();   // D3
        }

        /// <summary>
        /// D3 (28.08.2026), D-Check Klasse e: Bringt die acht Steuerelemente mit
        /// Fremdschrift auf die Schrift des Formulars (Segoe UI 10).
        ///
        /// <para>Der Dialog fuehrte vier Schriften: Segoe UI 10 (Formularschrift,
        /// 26 Steuerelemente), Segoe UI 8 (6 Textfelder), Segoe UI 9,75 fett (die beiden
        /// Uebernahmeknoepfe der Auswahlliste) und Segoe UI 12 fett (das Kopfband).</para>
        ///
        /// <para><b>Sechs Textfelder auf 8 pt.</b> Sie tragen dieselben Werte wie die
        /// Beschriftungen daneben, nur zwei Punkt kleiner - keine erkennbare Absicht,
        /// sondern Designer-Bestand. Sie erben jetzt wieder (Font = null). Ihre Hoehe
        /// waechst dabei um 3 px; der engste Nachbarabstand danach ist 2 px
        /// (textBox_Jahres_Verbrauch 474+25 gegen textBox_SummeProzesswaerme 501), alle
        /// uebrigen liegen bei 5 px und mehr. textBox_Beschreibung ist mehrzeilig und
        /// behaelt seine Hoehe ohnehin.</para>
        ///
        /// <para><b>btn_Hinzu / btn_Entfernen.</b> Die FETTUNG ist Absicht - sie hebt die
        /// beiden Uebernahmepfeile hervor. Angeglichen wird nur die Groesse, indem die
        /// Schrift des Formulars mit dem Fettschnitt uebernommen wird. Dasselbe Muster
        /// wie bei lblCO2 im Paket D2.</para>
        ///
        /// <para><b>label_Type bleibt.</b> Segoe UI 12 fett ist das Kopfband der Maske,
        /// also eine Titelrolle.</para>
        /// </summary>
        private void SchriftAngleichen()
        {
            string[] erben =
            {
                "textBox_Verbrauch", "textBox_Prozess_Name", "textBox_Jahres_Verbrauch",
                "textBox_Beschreibung", "textBox_Prozess_Type", "textBox_SummeProzesswaerme"
            };
            foreach (string n in erben)
            {
                Control[] treffer = this.Controls.Find(n, true);
                if (treffer.Length > 0) treffer[0].Font = null;
            }

            Font fett = new Font(this.Font, FontStyle.Bold);
            foreach (string n in new[] { "btn_Hinzu", "btn_Entfernen" })
            {
                Control[] treffer = this.Controls.Find(n, true);
                if (treffer.Length > 0) treffer[0].Font = fett;
            }
        }

        private void SetDBList()
        {
            ProzesswaermeStammCtrl ctrl_pw = new ProzesswaermeStammCtrl();
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            ctrl_pw.ReadAll();
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                dgv.Rows.Add(ctrl_pw.items[i].m_szProzessname, ctrl_pw.items[i].m_szTyp);
                dgv.Rows[i].DividerHeight = 1;
            }
        }

        public void SetControls(string szProjekt, bool bWizard = false)
        {
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            ProzesswaermeStammCtrl ctrl_pw = new ProzesswaermeStammCtrl();
            Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();

            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                m_bWizard = true;
            }

            m_szProjekt = szProjekt;

        
            listView_Prozess_Auswahl.Items.Clear(); 
            for (int i = 0; i < list_pwmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_pwmodel[i].szProzessname;
                lvitem.SubItems.Add(list_pwmodel[i].ID_Z.ToString());
                listView_Prozess_Auswahl.Items.Add(lvitem);
            }
            btn_ErgebnisseVerbrauch.Enabled = false;

            if (listView_Prozess_Auswahl.Items.Count > 0)
            {
                textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
            }

            dataGridView1.Select();
            dataGridView1.ClearSelection();
            listView_Prozess_Auswahl.Select();
            if(listView_Prozess_Auswahl.Items.Count > 0) listView_Prozess_Auswahl.Items[0].Selected = true;
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString();
            SetProzessInfo(szName);
        }

        private void listView_Prozess_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
                m_ListIndex = indexes[0];
                ListViewItem lvitem = listView_Prozess_Auswahl.Items[indexes[0]];
                textBox_Jahres_Verbrauch.Text = list_pwmodel[m_ListIndex].Summe.ToString("F2");
                textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
                SetProzessInfo(lvitem.Text);
            }
            dataGridView1.ClearSelection();
        }
        private void SetProzessInfo(string szName)
        {
            ProzesswaermeStammCtrl ctrl = new ProzesswaermeStammCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Prozess_Name.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Prozess_Type.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            ProzesswaermeStammCtrl ctrl = new ProzesswaermeStammCtrl();
            double summe = 0;
   
            ctrl.ReadSingle(szName);
            if (ctrl.rows > 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    summe += ctrl.m_Monat[i];
                }
            }
            return summe;  
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            // FR-8: Bei leerem Katalog gibt es keine aktuelle Zelle/Zeile -
            // CurrentCell/CurrentRow sind dann null, der alte RowIndex-Vergleich warf.
            if (dataGridView1.CurrentCell == null || dataGridView1.CurrentRow == null) return;

            string sql = "SELECT * from Tab_Prozesswaerme_STAMM where Bezeichner='" + (string)dataGridView1.CurrentRow.Cells[0].Value + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();
                model.ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.ID_Prozesswaerme = (int)rs.Read("ID");
                model.ID_Projekt = m_ID_Projekt;
                model.szProzessname = (string)dataGridView1.CurrentRow.Cells[0].Value;
                model.Summe = Prozesssumme(model.szProzessname);
         
                list_pwmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (string)dataGridView1.CurrentRow.Cells[0].Value;
                lvitem.SubItems.Add(model.ID_Z.ToString());
                listView_Prozess_Auswahl.Items.Add(lvitem);
                listView_Prozess_Auswahl.Select();
                listView_Prozess_Auswahl.SelectedItems.Clear();
                listView_Prozess_Auswahl.Items[listView_Prozess_Auswahl.Items.Count-1].Selected = true;  
            }
            rs.Close();

            textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
       
                ListViewItem lvitem = listView_Prozess_Auswahl.Items[indexes[0]];
    
                for (int i = 0; i < list_pwmodel.Count; i++)
                {
                    if (list_pwmodel[i].szProzessname == lvitem.Text && list_pwmodel[i].ID_Z.ToString()  == lvitem.SubItems[1].Text  )
                    {
                        list_pwmodel.RemoveAt(i);
                        m_ListIndex -= 1;
                        if(m_ListIndex < 0) m_ListIndex = 0;
                        listView_Prozess_Auswahl.Items[indexes[0]].Remove();
                        textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
                        break;
                    }
                }
                if (list_pwmodel.Count == 0)
                {
                    // FR-8: Nur bei gefuelltem Katalog - Rows[0] warf bei leerem Grid.
                    if (dataGridView1.Rows.Count == 0) return;

                    dataGridView1.Rows[0].Selected = true;
                    dataGridView1.Rows[0].Cells[0].Selected = true;

                    // Definiere die Zeile und Spalte des zu klickenden Cells
                    int rowIndex = 0;
                    int columnIndex = 0;

                    // Erstelle ein MouseEventArgs Objekt
                    MouseEventArgs me = new MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, 100, 100, 0);

                    // Erstelle ein DataGridViewCellMouseEventArgs Objekt
                    DataGridViewCellMouseEventArgs dgvme = new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 100, 100, me);

                    // Rufe den CellMouseClick-Ereignis-Handler auf
                    // Ersetzen Sie dataGridView1_CellMouseClick durch den Namen Ihres tatsächlichen Event-Handlers
                    listBox_Prozess_DB_SelectedIndexChanged(this.dataGridView1, dgvme);
                }
                else
                {
                    listView_Prozess_Auswahl.Select(); 
                    listView_Prozess_Auswahl.Items[0].Selected = true;
                }
            }
            
        }

        private double ProzesssummeGesamt()
        {
            double summe = 0;

            for (int i=0; i<listView_Prozess_Auswahl.Items.Count; i++)
            {
                summe += list_pwmodel[i].Summe;
            }
            return summe;
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

        /// <summary>
        /// Legt den im Feld "neuer Wert" eingegebenen Jahresverbrauch vor dem Simulationslauf
        /// in Z_Projekt_Prozesswaerme.Summe ab. Die Simulation liest ihn NICHT aus dem Formular,
        /// sondern aus dieser Zeile - SimulationWaermebedarf.Prozesswaerme_berechnen skaliert
        /// damit das Katalogprofil. Daher der Schreibzugriff mitten in der Bedienung.
        ///
        /// Das setzt ein bereits gespeichertes Projekt voraus. Im Neuanlage-Zweig des Wizards
        /// ist m_ID_Projekt nur die in WizardParent.Next geratene ProjektCtrl.GetMaxID()+1;
        /// weder Tab_Projekt noch Z_Projekt_Prozesswaerme haben dazu eine Zeile. Das UPDATE
        /// traf dort 0 Zeilen und meldete trotzdem Erfolg - ein stiller No-op, der zugleich
        /// vortaeuschte, die Vorschau rechne mit dem eingegebenen Wert.
        ///
        /// Verloren geht dabei nichts: Gespeichert wird der Jahresverbrauch ohnehin aus
        /// list_pwmodel (Schaltflaeche "neuer Wert") ueber WizardParent.Speichern und
        /// WizardCtrl.Add_Projekt_Prozess.
        /// </summary>
        private void SummeFuerSimulationSichern()
        {
            if (!m_bWizard || textBox_Verbrauch.Text.Trim() == "") return;

            // ZahlParsen statt double.Parse: Seit die Pruefung nicht mehr im TextChanged
            // sitzt, kann im Feld auch ein ungueltiger Text stehen - double.Parse haette
            // hier eine FormatException geworfen. Ungueltig heisst jetzt schlicht: nicht
            // sichern, die Vorschau rechnet mit dem zuletzt gespeicherten Wert.
            double dSumme;
            if (!Program.ZahlParsen(textBox_Verbrauch.Text, out dSumme)) return;

            if (ProjektIstGespeichert())
            {
                Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
                ctrl.UpdateSumme(dSumme, textBox_Prozess_Name.Text, m_ID_Projekt);
                return;
            }

            // Neues Projekt: der Wert kann noch nicht in die Projektzeile. Das muss der
            // Anwender wissen, sonst haelt er die Vorschau faelschlich fuer skaliert.
            if (m_bHinweisNeuesProjekt) return;
            m_bHinweisNeuesProjekt = true;
            MessageBox.Show(
                "Das Projekt ist noch nicht gespeichert. Die Vorschau rechnet deshalb mit den " +
                "Katalogwerten; der eingegebene Jahresverbrauch wirkt sich erst nach dem " +
                "Speichern des Projekts auf die Simulation aus.",
                "Vorschau ohne Projektwerte", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// true, wenn m_ID_Projekt auf eine wirklich vorhandene Tab_Projekt-Zeile zeigt.
        /// Im Neuanlage-Zweig des Wizards ist m_ID_Projekt lediglich eine geratene MAX(ID)+1.
        /// </summary>
        private bool ProjektIstGespeichert()
        {
            if (m_ID_Projekt <= 0) return false;

            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?",
                new OleDbParameter("@id", m_ID_Projekt));

            return anzahl != null && Convert.ToInt32(anzahl) > 0;
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            simulation.m_ID_Projekt = m_ID_Projekt;

            if (textBox_Prozess_Name.Text == "")
            {
                MessageBox.Show("Bitte einen Eintrag aus der Liste auswählen!");
                return;
            }
    
            SummeFuerSimulationSichern();

            List<string> list;
            list = listView_Prozess_Auswahl.Items.Cast<ListViewItem>().Select(item => item.Text).ToList();

            simulation.Prozesswaerme_berechnen(list);
            //simulation.Waermebedarf_Prozess = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.Waermebedarf_Prozess = simulation.prozesswerte.Sum();
            //simulation.com.I_monats_summe(simulation.prozesswerte, simulation.Waermebedarf_Prozess_Monat, simulation.mo_anfang, simulation.mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(simulation.prozesswerte, simulation.Waermebedarf_Prozess_Monat, simulation.mo_anfang, simulation.mo_ende);

            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1); 
            frm.ShowDialog();
            btn_ErgebnisseVerbrauch.Enabled = true;
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1);
            frm.ShowDialog(); 
        }

        private void btn_Prozess_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            frm.m_szProzessname = textBox_Prozess_Name.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szProzesstyp = textBox_Prozess_Type.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt);
            SetDBList();
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();

            Point p1 = btn_Prozess_DBneu.Location;
            p1 = this.PointToScreen(p1);
            frm_item.Location = p1;

            if (frm_item.ShowDialog() == DialogResult.OK)
            {
                frm.m_szProzessname = frm_item.m_szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.Location = p1;
                frm.ShowDialog();
                SetControls(m_szProjekt);
                SetDBList();
            }
        }

        private void btn_Prozess_loeschen_Click(object sender, EventArgs e)
        {
            DataGridView dgv = dataGridView1;

            // Absicherung: Prüfen, ob überhaupt eine gültige Zelle/Zeile ausgewählt ist
            if (dgv.CurrentCell == null || dgv.CurrentCell.RowIndex < 0 || dgv.CurrentRow == null)
            {
                MessageBox.Show("Bitte wählen Sie eine Prozesswärme aus der Liste aus!");
                return;
            }

            // Den Prozessnamen sicher aus der ersten Zelle der aktuellen Zeile auslesen
            string szProzessName = dgv.CurrentRow.Cells[0].Value?.ToString();

            if (string.IsNullOrEmpty(szProzessName))
            {
                MessageBox.Show("Der ausgewählte Datensatz enthält keinen gültigen Prozessnamen!");
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + szProzessName + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            try
            {
                // Standardkonformes, parametrisiertes SQL-Statement
                ProzesswaermeStammCtrl ctrlDel = new ProzesswaermeStammCtrl();

                // Löschbefehl über das DataRepository ausführen
                if (ctrlDel.Delete(szProzessName))
                {
                    // Erst wenn das Löschen in der Datenbank erfolgreich war, die Zeile aus der UI entfernen
                    dgv.Rows.RemoveAt(dgv.CurrentRow.Index);
                    MessageBox.Show("Prozess erfolgreich gelöscht.");
                }
                else
                {
                    MessageBox.Show("Der Prozess konnte nicht aus der Datenbank gelöscht werden.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen des Prozesses aus dem DataGridView: " + ex.Message);
                MessageBox.Show("Fehler beim Löschvorgang!");
            }
        }

        private void btn_ProzTypeDBedit_Click(object sender, EventArgs e)
        {
            Form_EingProzTyp frm = new Form_EingProzTyp();
            frm.SetControls();
            frm.ShowDialog(); 
        }

        private void btn_neuerWert_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;
            if (indexes.Count == 0 || textBox_Verbrauch.Text.Trim() == "")
            {
                MessageBox.Show("Bitte einen Eintrag aus der Liste auswählen und einen Wert eingeben!");
                return;
            }

            // Pruefung beim Aktionsknopf statt im TextChanged (Muster
            // ProjektPuffer.TemperaturenPruefen): TryParse, sprechende Meldung, Feld
            // markieren, Dialog bleibt offen. double.Parse() stand hier ungesichert und
            // haette nach dem Wegfall des Undo() eine FormatException geworfen.
            double dVerbrauch;
            if (!Program.ZahlParsen(textBox_Verbrauch.Text, out dVerbrauch) || dVerbrauch < 0)
            {
                MessageBox.Show("Eingaben überprüfen: \"" + textBox_Verbrauch.Text + "\"" + Environment.NewLine +
                                "Bitte den Jahresverbrauch als Zahl in MWh eingeben, z. B. 12,5.",
                                "Ungültige Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_Verbrauch.Focus();
                textBox_Verbrauch.SelectAll();
                return;
            }

            list_pwmodel[indexes[0]].Summe = dVerbrauch;
            textBox_Jahres_Verbrauch.Text = dVerbrauch.ToString("F2");
            textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2") ;
            pictureBox1.Visible = true;
            pictureBox1.Refresh();
            Thread.Sleep(500);
            pictureBox1.Visible = false;
        }
 
        private void Form_Prozesswaerme_Load(object sender, EventArgs e)
        {
            SetDBList();
            dataGridView1.ClearSelection();

            if (listView_Prozess_Auswahl.Items.Count > 0)
            {
                listView_Prozess_Auswahl.Select();
                listView_Prozess_Auswahl.SelectedItems.Clear();
                listView_Prozess_Auswahl.Items[0].Selected = true;
            }

        }

        /// <summary>
        /// Stiller Hinweis statt fokushaltender Pruefung.
        ///
        /// Die alte Fassung meldete jede Zwischeneingabe modal und nahm sie mit
        /// tb.Undo() zurueck. Undo() loest TextChanged erneut aus und schaltet dabei
        /// zwischen Rueckgaengig und Wiederherstellen um: War das Feld vorher leer
        /// (also ebenfalls keine Zahl), pendelte der Text zwischen Fehleingabe und
        /// Leerstand, die Meldung kam nach jedem OK sofort zurueck und der Dialog war
        /// gefangen. Geprueft wird jetzt erst beim Uebernehmen (btn_neuerWert_Click);
        /// hier bleibt nur die Feldfarbe als Hinweis.
        /// </summary>
        private void textBox_Verbrauch_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            double wert;
            bool bOk = tb.Text.Trim().Length == 0 || Program.ZahlParsen(tb.Text, out wert);
            tb.BackColor = bOk ? SystemColors.Window : Color.FromArgb(255, 235, 235);
        }
    }
}

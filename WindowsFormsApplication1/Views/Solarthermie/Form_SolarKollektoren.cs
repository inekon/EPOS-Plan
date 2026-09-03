using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarKollektoren : Form
    {
        SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SOLAR_TYP;
        public int m_ID_Projekt = 0;
        private WErzeugerModel model = new WErzeugerModel();
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;
        private int startindex = 100000;

        public Form_SolarKollektoren()
        {
            InitializeComponent();
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
            //dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            //dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);

            // Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);

            InitAuswahlListe();
            FensterEinpassung.Einhaengen(this);
        }

        // Konfiguriert die Auswahl-ListView (Details, nur Bezeichner-Spalte, keine Kopfzeile).
        private void InitAuswahlListe()
        {
            listBox_Auswahl.View = View.Details;
            listBox_Auswahl.FullRowSelect = true;
            listBox_Auswahl.HeaderStyle = ColumnHeaderStyle.None;
            listBox_Auswahl.MultiSelect = false;
            listBox_Auswahl.Scrollable = true;

            if (listBox_Auswahl.Columns.Count == 0)
            {
                int w = listBox_Auswahl.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                if (w < 50) w = 200;
                listBox_Auswahl.Columns.Add("", w);
            }
        }

        private WErzeugerModel GetSelectedSolar()
        {
            if (listBox_Auswahl.SelectedItems.Count == 0) return null;
            return listBox_Auswahl.SelectedItems[0].Tag as WErzeugerModel;
        }

        private void AddAuswahlRow(WErzeugerModel m)
        {
            ListViewItem lvi = new ListViewItem(m.Bezeichner);
            lvi.Tag = m;
            listBox_Auswahl.Items.Add(lvi);
            FitColumn();
        }

        // Spalte auf den laengsten Bezeichner setzen -> horizontale Scrollbar bei langen Namen.
        private void FitColumn()
        {
            if (listBox_Auswahl.Columns.Count == 0) return;
            int max = 0;
            foreach (ListViewItem it in listBox_Auswahl.Items)
            {
                int wItem = TextRenderer.MeasureText(it.Text, listBox_Auswahl.Font).Width;
                if (wItem > max) max = wItem;
            }
            int avail = listBox_Auswahl.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            int w = max + 24;
            if (w < avail) w = avail;
            listBox_Auswahl.Columns[0].Width = w;
        }

        // Liest einen ganzzahligen Spaltenwert; probiert mehrere Spaltennamen
        // (z.B. "Ruecklauf" ASCII bzw. "Rücklauf" mit Umlaut).
        private static int IntCol(DataRow row, params string[] cols)
        {
            foreach (string c in cols)
                if (row.Table.Columns.Contains(c) && row[c] != DBNull.Value) return Convert.ToInt32(row[c]);
            return 0;
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private Form getWizardPage()
        {
            // P4: typisierte Erkennung ueber WizardParent.Aktiver. Die frueheren elf
            // Kopien suchten den Rahmen als Zeichenkette "WizardParent" in
            // Application.OpenForms; der Rahmen meldet sich jetzt selbst an.
            return WizardParent.Aktiver as Form;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        public void SetControls(int IDProjekt, bool bWizard = false)
        {
            m_ID_Projekt = IDProjekt;
            if (bWizard)
            {
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_werzmodel = wizardparent.list_werzmodel;
            }

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            listBox_Auswahl.Items.Clear();
            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                if (list_werzmodel[n].ID_Type == WizardItemClass.SOLAR_TYP)
                {
                    AddAuswahlRow(list_werzmodel[n]);
                }
            }

            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.Items[0].Selected = true;
        }

        /*
        private double SummeLeistung()
        {
            double summe = 0;

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                ctrl.ReadSingle(list_werzmodel[i].ID_Solar);
                summe += ctrl.m_Modulfläche;
            }
            return summe;
        }*/

        private void SetDBList(string szFilter = "")
        {
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            ctrl.ReadAll(szFilter);
            for (int i = 0; i < ctrl.rows; i++)
            {
                dgv.Rows.Add(ctrl.items[i].m_szKollektorname, ctrl.items[i].m_szFirma + "\nKollektortyp: " + ctrl.items[i].m_szKollektortyp + "\nModulfläche: " + ctrl.items[i].m_Modulfläche + " m²" + "\nAperturfläche: " + ctrl.items[i].m_Aperturfläche + " m²");
                dgv.Rows[i].DividerHeight = 5;
            }
        }

        private void btn_Hinzzu_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell == null || dataGridView1.CurrentCell.RowIndex == -1) return;
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;

            // Stamm-ID des ausgewaehlten Kollektors ermitteln.
            int stammId = DataRepository.GetIdByName(SolarkollektorenStammCtrl.TABLE, "Bezeichner", szName);
            if (stammId <= 0)
            {
                MessageBox.Show("Der ausgewählte Solarkollektor wurde in den Stammdaten nicht gefunden.");
                return;
            }

            WErzeugerModel model = new WErzeugerModel();
            model.ID = startindex++;
            model.ID_Projekt = m_ID_Projekt;
            model.Bezeichner = szName;
            model.ID_Type = WizardItemClass.SOLAR_TYP;
            model.m_Azimut = 0;
            model.Kollektormodulanzahl = 1;
            model.m_Neigung = 0;

            // Vorlauf/Ruecklauf aus dem Stamm-Datensatz vorbelegen -> fliessen als
            // Default in Tab_Energieanlagen (Vorlauf, Ruecklauf) beim Speichern.
            DataTable dtStamm = DataRepository.GetDataTable(
                "SELECT * FROM " + SolarkollektorenStammCtrl.TABLE + " WHERE ID = ?",
                new DbParam("@id", stammId));
            if (dtStamm != null && dtStamm.Rows.Count > 0)
            {
                DataRow sr = dtStamm.Rows[0];
                model.Vorlauf = IntCol(sr, "Vorlauf");
                model.Ruecklauf = IntCol(sr, "Ruecklauf", "Rücklauf");
            }

            // Analog zu BHKW/Heizkessel: im direkten Projektmodus den Stammdatensatz sofort in die
            // Projekt-Tabelle kopieren (idempotent) und die PROJEKT-ID referenzieren. Im Wizard-Vorschau-
            // modus nur die Stamm-ID als Platzhalter; die eigentliche Kopie macht WizardCtrl beim Speichern.
            if (!m_bWizard && m_ID_Projekt > 0)
            {
                int projektId = new SolarkollektorenCtrl().CopyFromStamm(stammId, m_ID_Projekt);
                if (projektId <= 0)
                {
                    MessageBox.Show("Der Datensatz konnte nicht in das Projekt übernommen werden.");
                    return;
                }
                model.ID_Solar = projektId;
            }
            else
            {
                model.ID_Solar = stammId;
            }

            list_werzmodel.Add(model);
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;
            AddAuswahlRow(model);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.Items[listBox_Auswahl.Items.Count - 1].Selected = true;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.SelectedItems.Count == 0) return;
            ListViewItem lvi = listBox_Auswahl.SelectedItems[0];
            WErzeugerModel m = lvi.Tag as WErzeugerModel;
            if (m == null) return;
            string szName = m.Bezeichner;

            list_werzmodel.Remove(m);
            listBox_Auswahl.Items.Remove(lvi);
            FitColumn();
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            // Projekt-Kopie nur entfernen, wenn keine weitere Auswahl mehr darauf verweist.
            bool nochReferenziert = false;
            foreach (WErzeugerModel it in list_werzmodel)
                if (it.ID_Type == WizardItemClass.SOLAR_TYP && it.ID_Solar == m.ID_Solar) { nochReferenziert = true; break; }
            if (!m_bWizard && m_ID_Projekt > 0 && !nochReferenziert)
            {
                new SolarkollektorenCtrl().DeleteFromProjekt(szName, m_ID_Projekt);
            }

            if (listBox_Auswahl.Items.Count > 0)
            {
                listBox_Auswahl.Items[0].Selected = true;
                listBox_Auswahl.Select();
            }
            else
            {
                textBox_Aperturflaeche.Text = "0";
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[0].Selected = true;
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
                }
            }
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Solarkollektoren_STAMM where Bezeichner='" + szName + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                object ktyp = rs.Read("Kollektortyp");
                textBox_Kollektortype.Text = (ktyp == DBNull.Value) ? "" : (string)ktyp;
                object firma = rs.Read("Firma");
                textBox_Firma.Text = (firma == DBNull.Value) ? "" : (string)firma;
                object beschreibungValue = rs.Read("Beschreibung");
                textBox_Beschreibung.Text = (beschreibungValue == DBNull.Value) ? "" : (string)beschreibungValue;
                textBox_Modul_Apertur.Text = rs.Read("Aperturflaeche").ToString();
                textBox_Aperturflaeche.Text = rs.Read("Aperturflaeche").ToString();
            }
            rs.Close();
            groupBox_Kollektor.Visible = false;
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySelectedSolar();
        }

        // Wird auch bei Klick auf das bereits selektierte / einzige Item aufgerufen.
        private void listBox_Auswahl_MouseClick(object sender, MouseEventArgs e)
        {
            ApplySelectedSolar();
        }

        // Aktualisiert die Detailanzeige aus dem aktuell selektierten Solar-Eintrag.
        private void ApplySelectedSolar()
        {
            WErzeugerModel m = GetSelectedSolar();
            if (m == null) return;
            string szName = m.Bezeichner;
            RecordSet rs = new RecordSet();
            double modulflaeche = 0;

            rs.Open("select * from Tab_Solarkollektoren_STAMM where Bezeichner='" + szName + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                object ktyp = rs.Read("Kollektortyp");
                textBox_Kollektortype.Text = (ktyp == DBNull.Value) ? "" : (string)ktyp;
                object firma = rs.Read("Firma");
                textBox_Firma.Text = (firma == DBNull.Value) ? "" : (string)firma;
                object beschreibungValue = rs.Read("Beschreibung");
                textBox_Beschreibung.Text = (beschreibungValue == DBNull.Value) ? "" : (string)beschreibungValue;
                modulflaeche = (double)rs.Read("Aperturflaeche");
                textBox_Modul_Apertur.Text = modulflaeche.ToString();
            }
            rs.Close();

            textBox_Kollektorneigung.Text = m.m_Neigung.ToString();
            int anzahl = m.Kollektormodulanzahl;
            textBox_Anzahl.Text = anzahl.ToString();
            textBox_Aperturflaeche.Text = (modulflaeche * anzahl).ToString();
            textBox_Azimut.Text = m.m_Azimut.ToString();
            textBox_Vorlauf.Text = m.Vorlauf.ToString();
            textBox_Ruecklauf.Text = m.Ruecklauf.ToString();

            groupBox_Kollektor.Visible = true;
        }

        private void btn_Abbrechen_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // TextChanged faerbt nur noch (Program.GanzzahlFaerben), gemeldet wird erst
        // beim Speichern-Knopf. Das alte checkDouble()+Undo()+ClearUndo() war die
        // Notloesung gegen die Endlosmeldung - Begruendung in Program.cs.
        private void textBox_Anzahl_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);

            // Aperturflaeche nur nachfuehren, wenn beide Werte lesbar sind; sonst
            // bleibt die bisherige Anzeige stehen.
            int anzahl;
            double modulApertur;
            if (Program.GanzzahlParsen(textBox_Anzahl.Text, out anzahl) &&
                Program.ZahlParsen(textBox_Modul_Apertur.Text, out modulApertur))
            {
                textBox_Aperturflaeche.Text = (modulApertur * anzahl).ToString();
            }
        }

        private void textBox_Kollektorneigung_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            WErzeugerModel m = GetSelectedSolar();
            if (m != null && m.ID_Type == WizardItemClass.SOLAR_TYP)
            {
                // Pruefung erst hier: leer gilt wie bisher als 0, bei ungueltigem
                // Text meldet GanzzahlPruefen und die Seite bleibt unveraendert.
                int anzahl, neigung, azimut, vorlauf, ruecklauf;
                if (!Program.GanzzahlPruefen(textBox_Anzahl, "Modulanzahl", out anzahl, true)) return;
                if (!Program.GanzzahlPruefen(textBox_Kollektorneigung, "Neigung [°]", out neigung, true)) return;
                if (!Program.GanzzahlPruefen(textBox_Azimut, "Azimut [°]", out azimut, true)) return;
                if (!Program.GanzzahlPruefen(textBox_Vorlauf, "Vorlauf", out vorlauf, true)) return;
                if (!Program.GanzzahlPruefen(textBox_Ruecklauf, "Rücklauf", out ruecklauf, true)) return;

                m.Kollektormodulanzahl = anzahl;
                m.m_Neigung = neigung;
                m.m_Azimut = azimut;
                m.Vorlauf = vorlauf;
                m.Ruecklauf = ruecklauf;
                pictureBox1.Visible = true;
                pictureBox1.Refresh();
                Thread.Sleep(500);
                pictureBox1.Visible = false;
            }
        }

        private void Form_SolarKollektoren_Paint(object sender, PaintEventArgs e)
        {
            float[] dashValues = { 5, 2 };
            Pen blackPen = new Pen(Color.Gray, 1);
            blackPen.DashPattern = dashValues;

            int a, b, c, d;
            a = groupBox_Kollektor.Left;
            b = groupBox_Kollektor.Top;
            c = groupBox_Kollektor.Width;
            d = groupBox_Kollektor.Height;

            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + 10), new Point(a + c - 10, b + 10));
            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + d - 10), new Point(a + c - 10, b + d - 10));
            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + 10), new Point(a + 10, b + d - 10));
            e.Graphics.DrawLine(blackPen, new Point(a + c - 10, b + 10), new Point(a + c - 10, b + d - 10));
        }

        private void btn_Kollektor_DB_loeschen_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte einen Kollektor auswählen!"); return; }

            var result = MessageBox.Show("Wollen Sie wirklich den Solarkollektor löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (!ctrl.Delete((string)dataGridView1.SelectedRows[0].Cells[0].Value)) return;

                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
            }
        }

        private void btn_Kollektor_DB_Edit_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte einen Kollektor auswählen!"); return; }

            // iU9-W7.6: Der Katalogeditor ist die Razor-Komponente
            // SolarkollektorKatalogDialog; Form_SolarDB ist im selben Schritt
            // GELOESCHT (Regel M1). Diese Maske selbst folgt in W7.7 - dann wird der
            // Editor eine Ueberlagerung IM Dialog statt eines zweiten Fensters.
            SolarkollektorHuelle.KatalogBearbeiten(
                this, (string)dataGridView1.CurrentRow.Cells[0].Value, neu: false);
            SetDBList();
        }

        private void btn_Kollektor_DB_neu_Click(object sender, EventArgs e)
        {
            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                SolarkollektorHuelle.KatalogBearbeiten(this, szName, neu: true);
                SetDBList();
            }
        }

        // Validating faerbt nur noch; das Modell wird wie bisher nur unter der
        // vorhandenen ID_Type-Bedingung nachgefuehrt, jetzt aber nur bei lesbarer
        // Ganzzahl (frueher Int32.Parse ungeschuetzt). Geprueft wird beim Speichern.
        private void textBox_Ruecklauf_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Program.GanzzahlFaerben(sender);

            int ruecklauf;
            WErzeugerModel m = GetSelectedSolar();
            if (m != null && m.ID_Type == WizardItemClass.BHKW_TYP &&
                Program.GanzzahlParsen(textBox_Ruecklauf.Text, out ruecklauf))
                m.Ruecklauf = ruecklauf;
        }

        private void textBox_Vorlauf_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Program.GanzzahlFaerben(sender);

            int vorlauf;
            WErzeugerModel m = GetSelectedSolar();
            if (m != null && m.ID_Type == WizardItemClass.BHKW_TYP &&
                Program.GanzzahlParsen(textBox_Vorlauf.Text, out vorlauf))
                m.Vorlauf = vorlauf;
        }

        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            //dataGridView1.ClearSelection();
        }

        private void Form_SolarKollektoren_Load(object sender, EventArgs e)
        {
            SetDBList();
        }
 
    }
}
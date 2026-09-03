using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarKollektorenAdmin : Form
    {
        SolarkollektorenStammCtrl ctrl = new SolarkollektorenStammCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SOLAR_TYP;
        public int m_ID_Projekt = 0;
        private WErzeugerModel model = new WErzeugerModel();

        public Form_SolarKollektorenAdmin ()
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
            //dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            //dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);
			
			// Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);
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
        /*le SummeLeistung()
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
                dgv.Rows.Add(ctrl.items[i].m_szKollektorname, ctrl.items[i].m_szFirma + "\nKollektortyp: " + ctrl.items[i].m_szKollektortyp + "\nAperturfläche: " + ctrl.items[i].m_Aperturfläche + " m²");
                dgv.Rows[i].DividerHeight = 5;
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
                textBox_Modul_A.Text = rs.Read("Modulflaeche").ToString();
                textBox_Modul_A.Text = rs.Read("Aperturflaeche").ToString();
                textBox_Vorlauf.Text = rs.Read("Vorlauf").ToString();
                textBox_Ruecklauf.Text = rs.Read("Ruecklauf").ToString();
            }
            rs.Close();
        }

        private void btn_Abbrechen_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
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
            // GELOESCHT (Regel M1). Die Positionierung an der Knopfposition
            // entfaellt - die Blazor-Huelle kennt kein PointToScreen und erscheint
            // mittig ueber dieser Maske (dieselbe Umstellung wie iU9-W2.1).
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

        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            //dataGridView1.ClearSelection();
        }

        private void Form_SolarKollektorenAdmin_Load(object sender, EventArgs e)
        {
            SetDBList();

            dataGridView1.Select();
            dataGridView1.ClearSelection();
        }
          
    }
}
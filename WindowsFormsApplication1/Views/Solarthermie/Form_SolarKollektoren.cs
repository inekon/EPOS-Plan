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
        SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SOLAR_TYP;
        public int m_ID_Projekt = 0;
        private WErzeugerModel model = new WErzeugerModel();
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;

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
            dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private Form getWizardPage()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "WizardParent")
                {
                    return form;
                }
            }
            return null;
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

            SetDBList();

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            listBox_Auswahl.Items.Clear();
            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                WErzeugerModel item = new WErzeugerModel();

                if (list_werzmodel[n].ID_Type == WizardItemClass.SOLAR_TYP)
                {
                    item.Bezeichner = list_werzmodel[n].Bezeichner;
                    listBox_Auswahl.Items.Add(item.Bezeichner);
                }
            }
            
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
        }

        private double SummeLeistung()
        {
            double summe = 0;

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                ctrl.ReadSingle(list_werzmodel[i].ID_Solar);
                summe += ctrl.m_Modulfläche;
            }
            return summe;
        }

        private void SetDBList(string szFilter = "")
        {
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            ctrl.ReadAll(szFilter);
            for (int i = 0; i < ctrl.rows; i++)
            {
                dgv.Rows.Add(ctrl.items[i].m_szKollektorname, ctrl.items[i].m_szFirma + "\nKollektortyp: " + ctrl.items[i].m_szKollektortyp + "\nModulfläche: " + ctrl.items[i].m_Modulfläche + " m²");
                dgv.Rows[i].DividerHeight = 5;
            }
        }

        private void btn_Hinzzu_Click(object sender, EventArgs e)
        {
            WErzeugerModel model = new WErzeugerModel();
            if (dataGridView1.CurrentCell.RowIndex == -1) return;
            model.Bezeichner = (string)dataGridView1.CurrentRow.Cells[0].Value;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Solarkollektoren where Kollektorname='" + model.Bezeichner + "'");
            if (!rs.EOF())
            {
                model.ID_Solar = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass.SOLAR_TYP;
                model.Kollektorausrichtung = 0;
                model.Kollektormodulanzahl = 1;
                model.Kollektorneigung = 30;
                radioButton_SuedOst.Checked = true;
            }
            rs.Close();

            list_werzmodel.Add(model);
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;
            listBox_Auswahl.Items.Add(model.Bezeichner);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = listBox_Auswahl.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.Text == "") return;
            model.Bezeichner = listBox_Auswahl.Text;

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_Auswahl.Text && list_werzmodel[i].ID_Type == WizardItemClass.SOLAR_TYP)
                {
                    listBox_Auswahl.Items.Remove(listBox_Auswahl.Text);
                    list_werzmodel.RemoveAt(i);
                    break;
                }
            }
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            if (listBox_Auswahl.Items.Count > 0)
            {
                listBox_Auswahl.SelectedIndex = 0;
            }
            else
            {
                textBox_Kollektor_A.Text = "0"; ;
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[0].Selected = true;
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
                }
            }

            if (listBox_Auswahl.Items.Count == 0)
            {
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
                dataGridView1_Click(this.dataGridView1, dgvme);

            }
            else
            {
                listBox_Auswahl.Select();
                listBox_Auswahl.SelectedIndex = 0;
            }
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Solarkollektoren where Kollektorname='" + szName + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Kollektorname");
                object ktyp = rs.Read("Kollektortyp");
                textBox_Kollektortype.Text = (ktyp == DBNull.Value) ? "" : (string)ktyp;
                object firma = rs.Read("Firma");
                textBox_Firma.Text = (firma == DBNull.Value) ? "" : (string)firma;
                object beschreibungValue = rs.Read("Beschreibung");
                textBox_Beschreibung.Text = (beschreibungValue == DBNull.Value) ? "" : (string)beschreibungValue;
                textBox_Modul_A.Text = rs.Read("Modulflaeche").ToString();
                textBox_Kollektor_A.Text = "";
            }
            rs.Close();
            groupBox_Kollektor.Visible = false; 
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szName = listBox_Auswahl.Text;
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();
            double modulflaeche = 0;   

            rs.Open("select * from Tab_Solarkollektoren where Kollektorname='" + szName + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Kollektorname");
                object ktyp = rs.Read("Kollektortyp");
                textBox_Kollektortype.Text = (ktyp == DBNull.Value) ? "" : (string)ktyp;
                object firma = rs.Read("Firma");
                textBox_Firma.Text = (firma == DBNull.Value) ? "" : (string)firma;
                object beschreibungValue = rs.Read("Beschreibung");
                textBox_Beschreibung.Text = (beschreibungValue == DBNull.Value) ? "" : (string)beschreibungValue;
                modulflaeche = (double)rs.Read("Modulflaeche");
                textBox_Modul_A.Text = modulflaeche.ToString();
            }
            rs.Close();

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_Auswahl.Text && list_werzmodel[i].ID_Type == WizardItemClass.SOLAR_TYP)
                {
                    textBox_Kollektorneigung.Text = list_werzmodel[i].Kollektorneigung.ToString();  
                    int anzahl = list_werzmodel[i].Kollektormodulanzahl;
                    textBox_Anzahl.Text = anzahl.ToString();
                    textBox_Kollektor_A.Text = (modulflaeche * anzahl).ToString();
                    int ausrichtung = list_werzmodel[i].Kollektorausrichtung;
                    switch (ausrichtung)
                    {
                        case 0:
                            radioButton_SuedOst.Checked = true;
                            break;
                        case 1:
                            radioButton_Sued.Checked = true;
                            break;
                        case 2:
                            radioButton_SuedWest.Checked = true;
                            break;
                        case 3:
                            radioButton_flach.Checked = true;
                            break;
                        case 4:
                            radioButton_Sued90.Checked = true;
                            break;
                    }

                    break;
                }
            }
            groupBox_Kollektor.Visible = true;

        }

        private void btn_Abbrechen_Click_1(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void textBox_Anzahl_TextChanged(object sender, EventArgs e)
        {
            if (!Program.checkDouble(textBox_Anzahl, textBox_Anzahl.Text))
            {
                textBox_Anzahl.Undo();
                textBox_Anzahl.ClearUndo();
                return;
            }
            textBox_Kollektor_A.Text = (double.Parse(textBox_Modul_A.Text) * Int32.Parse(textBox_Anzahl.Text)).ToString();
        }

        private void textBox_Kollektorneigung_TextChanged(object sender, EventArgs e)
        {
            if (!Program.checkDouble(textBox_Kollektorneigung, textBox_Kollektorneigung.Text))
            {
                textBox_Kollektorneigung.Undo();
                textBox_Kollektorneigung.ClearUndo();
                return;
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_Auswahl.Text && list_werzmodel[i].ID_Type == WizardItemClass.SOLAR_TYP)
                {
                    list_werzmodel[i].Kollektormodulanzahl = textBox_Anzahl.Text == "" ? 0 : Int32.Parse(textBox_Anzahl.Text);
                    list_werzmodel[i].Kollektorneigung = textBox_Kollektorneigung.Text == "" ? 0 : Int32.Parse(textBox_Kollektorneigung.Text);
                    if(radioButton_SuedOst.Checked)
                        list_werzmodel[i].Kollektorausrichtung = 0;
                    else if (radioButton_Sued.Checked)
                        list_werzmodel[i].Kollektorausrichtung = 1;
                    else if (radioButton_SuedWest.Checked)
                        list_werzmodel[i].Kollektorausrichtung = 2;
                    else if (radioButton_flach.Checked)
                        list_werzmodel[i].Kollektorausrichtung = 3;
                    else if (radioButton_Sued90.Checked)    
                        list_werzmodel[i].Kollektorausrichtung = 4;
                    pictureBox1.Visible = true;
                    pictureBox1.Refresh();
                    Thread.Sleep(500);
                    pictureBox1.Visible = false;
                    break;
                }
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

            e.Graphics.DrawLine(blackPen, new Point(a+10, b+10), new Point(a+c-10,b+10));
            e.Graphics.DrawLine(blackPen, new Point(a+10, b+d-10), new Point(a+c-10, b+d-10));
            e.Graphics.DrawLine(blackPen, new Point(a+10, b+10), new Point(a+10, b+d-10));
            e.Graphics.DrawLine(blackPen, new Point(a+c-10, b+10), new Point(a+c-10, b+d-10));
        }

        private void btn_Kollektor_DB_loeschen_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte einen Kollektor auswählen!"); return; }

            var result = MessageBox.Show("Wollen Sie wirklich den Solarkollektor löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                RecordSet rs = new RecordSet();
                rs.Open("Delete * from Tab_Solarkollektoren where Kollektorname='" + (string)dataGridView1.SelectedRows[0].Cells[0].Value + "'");
                rs.Close();

                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
            }
        }

        private void btn_Kollektor_DB_Edit_Click(object sender, EventArgs e)
        {
            Form_SolarDB frm = new Form_SolarDB();
            frm.m_mode = Form_DBBHKW.MODE_EDIT;
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte einen Kollektor auswählen!"); return; }

            frm.SetControls((string)dataGridView1.CurrentRow.Cells[0].Value);
            System.Drawing.Point p1 = btn_Kollektor_DB_Edit.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            frm.ShowDialog();
            SetDBList();
        }

        private void btn_Kollektor_DB_neu_Click(object sender, EventArgs e)
        {
            Form_SolarDB frm = new Form_SolarDB();
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();

            System.Drawing.Point p1 = btn_Kollektor_DB_neu.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {
                frm.m_mode = Form_SolarDB.MODE_NEU;
                frm.SetControls(frmLabel.m_szName);
                frm.m_szName = frmLabel.m_szName;
                frm.ShowDialog();
                SetDBList();
            }
        }

        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            //dataGridView1.ClearSelection();
        }
    }
}
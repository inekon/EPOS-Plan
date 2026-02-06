using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApplication1.Properties;
using WindowsFormsApplication1.Views.Simulation;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Config : Form
    {
        public KonfigurationModel Konfiguration = new KonfigurationModel();
        public int m_ID_Projekt;
        private ComboBox comboBox;
        private int index = -1;
        private List<string> listErzeuger = new List<string>();
        private List<string> listPufferSp = new List<string>();


        public Form_Simulation_Config()
        {
            InitializeComponent();

            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.View = View.Details;
            listView1.Columns.Add("Wärmeerzeuger", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Pufferspeicher", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("", -2, HorizontalAlignment.Left);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Handle double-click for editing
            listView1.MouseDoubleClick += ListView_MouseDoubleClick;

            // Initialize ComboBox (hidden by default)
            comboBox = new ComboBox
            {
                Visible = false,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }
 
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            item.SubItems[1].Text = comboBox.SelectedItem.ToString();
            comboBox.Visible = false;
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];


            ListViewHitTestInfo hit = listView1.HitTest(e.Location);
            int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            index = subItemIndex;
            Rectangle subItemBounds;
            subItemBounds = item.SubItems[subItemIndex].Bounds;
            if (subItemIndex == 1)
            {
                comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
                comboBox.LostFocus += (s, f) =>
                { 
                    comboBox.Visible = false;
                    listView1.Focus();
                };
                this.Controls.Add(comboBox);

                subItemBounds.X += listView1.Left + groupBox_PufferSp.Left + 2;
                subItemBounds.Y += listView1.Top + groupBox_PufferSp.Top;
                // Position ComboBox over the subitem
                comboBox.Bounds = subItemBounds;
                comboBox.Text = item.SubItems[subItemIndex].Text;
                comboBox.Visible = true;
                comboBox.BringToFront();
                comboBox.Focus();
            }
            else if (subItemIndex == 2 || subItemIndex == 3)
            {
                // Edit the "Vorlauf" or "Rücklauf" column
                subItemBounds.X += listView1.Left + groupBox_PufferSp.Left + 2;
                subItemBounds.Y += listView1.Top + groupBox_PufferSp.Top;
                
                TextBox textBox = new TextBox
                {
                    Bounds = subItemBounds,
                    Text = item.SubItems[subItemIndex].Text
                };
                textBox.LostFocus += (s, ev) =>
                {
                    item.SubItems[subItemIndex].Text = textBox.Text;
                    textBox.Dispose();
                };
                this.Controls.Add(textBox);
                textBox.BringToFront();
                textBox.Focus();
            }
            else if (subItemIndex == 4)
            {
                Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
                frm.m_bReadOnly = true;
                frm.ShowDialog();
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
        
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedIndex != -1)
            {
                checkBox1.Checked = true;
                // listBox1.Items.Add(comboBox1.Text);
                AddErzeuger();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex != -1)
            {
                checkBox2.Checked = true;
                //  listBox1.Items.Add(comboBox2.Text);
                AddErzeuger();
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex != -1)
            {
                checkBox3.Checked = true;
                // listBox1.Items.Add(comboBox3.Text);
                AddErzeuger();
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex != -1)
            {
                checkBox4.Checked = true;
                // listBox1.Items.Add(comboBox4.Text);
                AddErzeuger();
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedIndex != -1) { checkBox5.Checked = true; }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedIndex != -1) { checkBox6.Checked = true; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) { comboBox1.Text = ""; }
            AddErzeuger();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox2.Checked) { comboBox2.Text = ""; }
            AddErzeuger();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox3.Checked) { comboBox3.Text = ""; }
            AddErzeuger();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox4.Checked) { comboBox4.Text = ""; }
            AddErzeuger();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox5.Checked) { comboBox5.Text = ""; }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox6.Checked) { comboBox6.Text = ""; }
        }

        public void SetControls(int ID_Projekt)
        {
            m_ID_Projekt = ID_Projekt;
            comboBox1.Text = Konfiguration.m_Tool_1;
            comboBox2.Text = Konfiguration.m_Tool_2;
            comboBox3.Text = Konfiguration.m_Tool_3;
            comboBox4.Text = Konfiguration.m_Tool_4;
            comboBox5.Text = Konfiguration.m_Tool_5;
            comboBox6.Text = Konfiguration.m_Tool_6;
            checkBox_Heizstab.Checked = Konfiguration.m_WP_Heizstab;
            textBox_Netzverluste.Text = Konfiguration.m_Netzverluste.ToString();
            comboBox_NetzvEinheit.Text = Konfiguration.m_szNetzverlusteEinheit;
            textBox_untere_PGrenze.Text = Konfiguration.m_BHKW_Grenzleistung.ToString();
            comboBox_Bereitschaft.Text = Konfiguration.m_Kessel_Betriebsbereitschaft.ToString();

            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();
            ctrlpsp.ReadAll("ID_Projekt= " + m_ID_Projekt);
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                listView1.Items.Add(new ListViewItem(new[] { ctrlpsp.items[i].Erzeuger, ctrlpsp.items[i].PufferSp, ctrlpsp.items[i].Vorlauf.ToString(), ctrlpsp.items[i].Ruecklauf.ToString(), "" }));
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            PufferSpCtrl ctrl = new PufferSpCtrl();
            ctrl.ReadAll("");
            for (int i = 0; i < ctrl.rows; i++) listPufferSp.Add(ctrl.items[i].Name);
            comboBox.Items.AddRange(listPufferSp.ToArray());
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();

            Konfiguration.m_Tool_1 = comboBox1.Text;
            Konfiguration.m_Tool_2 = comboBox2.Text;
            Konfiguration.m_Tool_3 = comboBox3.Text;
            Konfiguration.m_Tool_4 = comboBox4.Text;
            Konfiguration.m_Tool_5 = comboBox5.Text;
            Konfiguration.m_Tool_6 = comboBox6.Text;
            Konfiguration.m_WP_Heizstab = checkBox_Heizstab.Checked;
            Konfiguration.m_Netzverluste = double.Parse(textBox_Netzverluste.Text);
            Konfiguration.m_szNetzverlusteEinheit = comboBox_NetzvEinheit.Text;
            Konfiguration.m_BHKW_Grenzleistung = double.Parse(textBox_untere_PGrenze.Text);
            Konfiguration.m_Kessel_Betriebsbereitschaft = Int32.Parse(comboBox_Bereitschaft.Text);

            ctrl.model = Konfiguration; 
            if(!ctrl.Delete(m_ID_Projekt)) return;
            if(ctrl.Insert(m_ID_Projekt)) MessageBox.Show("Datensatz gespeichert");

            int prioritaet = 1;

            ctrlpsp.ID_Projekt = m_ID_Projekt;
            
            if (!ctrlpsp.Delete()) return;
            for (int i=0; i<listView1.Items.Count; i++)
            {
                ListViewItem item = listView1.Items[i];
                ctrlpsp.Erzeuger = item.SubItems[0].Text;
                ctrlpsp.PufferSp = item.SubItems[1].Text;
                
                ctrlpsp.Vorlauf = Int32.Parse(item.SubItems[2].Text);
                ctrlpsp.Ruecklauf = Int32.Parse(item.SubItems[3].Text);
                ctrlpsp.Prioritaet = prioritaet++;
                ctrlpsp.Insert();
            }
        }

        private void AddErzeuger()
        {
            listErzeuger.Clear();

            if (comboBox1.Text != "")
            {
                if (!listErzeuger.Contains(comboBox1.Text)) listErzeuger.Add(comboBox1.Text);
            }
            if (comboBox2.Text != "")
            {
                if (!listErzeuger.Contains(comboBox2.Text)) listErzeuger.Add(comboBox2.Text);
            }
            if (comboBox3.Text != "")
            {
                if (!listErzeuger.Contains(comboBox3.Text)) listErzeuger.Add(comboBox3.Text);
            }
            if (comboBox4.Text != "")
            {
                if (!listErzeuger.Contains(comboBox4.Text)) listErzeuger.Add(comboBox4.Text);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form_KonfigPufferspeicher frm = new Form_KonfigPufferspeicher();
            frm.listErzeuger = listErzeuger;
            frm.listPufferSp = listPufferSp;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls();
            
            DialogResult result = frm.ShowDialog();   
            if(result == DialogResult.OK)
            {
                listView1.Items.Add(new ListViewItem(new[] { frm.model.Erzeuger, frm.model.PufferSp, frm.model.Vorlauf.ToString(), frm.model.Ruecklauf.ToString(),""}));
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            ListViewItem item = listView1.SelectedItems[0];
            listView1.Items.Remove(item); 
        }

        private void checkBox_PufferSp_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox_PufferSp.Checked)
            {
                groupBox_PufferSp.Visible = true;
            }
            else
            {
                groupBox_PufferSp.Visible = false;
            }   
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 4)
            {
                e.DrawBackground();
                var imageRect = new Rectangle(e.Bounds.X, e.Bounds.Y, 20, 20);
                //  e.Graphics.DrawImage(SystemIcons.Information.ToBitmap(), imageRect);
                // Convert Resources.edit (byte[]) to Image
                using (var ms = new System.IO.MemoryStream(Resources.edit))
                using (var img = Image.FromStream(ms))
                {
                    e.Graphics.DrawImage(img, imageRect);
                }
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_Admin : Form
    {
        private PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
        public int m_ID_Projekt = 0;
        public bool m_bReadOnly = false;

        public Form_PufferSp_Admin ()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            listBox_PufferSp_DB.Items.Clear();
        }

        private void Form_PufferSp_Admin_Load(object sender, EventArgs e)
        {
            LoadDBPufferSp();

            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                if (comboBox_Hersteller.FindStringExact(ctrl.items[i].Firma) == -1) comboBox_Hersteller.Items.Add(ctrl.items[i].Firma);
            }

            PufferSpFilter.VolumenfilterFuellen(comboBox_Volumen);
            PufferSpFilter.HerstellerfilterVorbelegen(comboBox_Hersteller);

            if(m_bReadOnly)
            {
                btn_Neu.Enabled = false;
                btn_Bearbeiten.Enabled = false;
                btn_Loeschen.Enabled = false;
            }   
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string sql = "";

            // B0-10 (Paket 9 / L5): Filterstufe über den AUSWAHLINDEX statt über den
            // angezeigten Text - siehe PufferSpFilter. Wortlaut der Prädikate und die
            // Vorbelegung "alle Volumina" sind unverändert.
            string szFilterVolumen = PufferSpFilter.VolumenSql(comboBox_Volumen);
            string szFilter = PufferSpFilter.HerstellerSql(comboBox_Hersteller);

            listBox_PufferSp_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from Tab_Pufferspeicher_STAMM where " + szFilterVolumen + " order by Bezeichner";
            else
                sql = "select * from Tab_Pufferspeicher_STAMM where " + szFilter + " and " + szFilterVolumen + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_PufferSp_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        /// <summary>
        /// Schliesst den Katalogdialog. Hier wird bewusst NICHT geprueft: die Felder
        /// dieses Formulars sind Anzeigefelder ohne Speicherweg (gespeichert wird in
        /// Form_PufferSp_Bearbeiten), und OK ist der einzige Weg aus dem Dialog.
        /// </summary>
        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
            if(listBox_PufferSp_DB.Text == "") return;    
            DialogResult dialogResult = MessageBox.Show(
                string.Format(MyResource.Resource.PSP_MELDUNG_WIRKLICH_LOESCHEN, listBox_PufferSp_DB.Text),
                MyResource.Resource.PSP_TITEL_LOESCHEN, MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            if (!ctrl.Delete(listBox_PufferSp_DB.Text)) return;
            listBox_PufferSp_DB.Items.Remove(listBox_PufferSp_DB.Text); 
        }

        private void LoadDBPufferSp()
        {
            listBox_PufferSp_DB.Items.Clear();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_PufferSp_DB.Items.Add(ctrl.items[i].Name);
            }
        }

        private void comboBox_Hersteller_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Volumen_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void listBox_PufferSp_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Pufferspeicher_STAMM where Bezeichner='" + listBox_PufferSp_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Hersteller.Text = rs.GetString("Hersteller");
                textBox_Typ.Text = (string)rs.Read("Speichertyp");
                textBox_Versluste.Text = rs.Read("Bereitschaftsverluste").ToString();
                textBox_Volumen.Text = rs.Read("Gesamtvolumen").ToString();
                textBox_Investitionskosten.Text = rs.Read("Investitionskosten").ToString();
            }
            rs.Close();
        }

        /// <summary>
        /// Reines Anzeigefeld (Designer: Enabled = False) - der Text kommt nur aus dem
        /// Katalog. Folgepaket zu ab5bf32: statt modal zu melden und mit Undo()
        /// zurueckzunehmen, wird nur noch gefaerbt, und das erst, wenn das Feld
        /// ueberhaupt Eingaben annimmt. Bereitschaftsverluste sind ein double-Wert.
        /// </summary>
        private void textBox_Versluste_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null || !tb.Enabled) return;
            Program.ZahlFaerben(tb);
        }

        /// <summary>
        /// Wie textBox_Versluste_TextChanged; das Gesamtvolumen wird als Ganzzahl
        /// gespeichert (PufferSpModel.Gesamtvolumen).
        /// </summary>
        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null || !tb.Enabled) return;
            Program.GanzzahlFaerben(tb);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Bearbeiten frm = new Form_PufferSp_Bearbeiten(Form_PufferSp_Bearbeiten.MODE_EDIT);
            if (listBox_PufferSp_DB.Text == "") return;
            frm.SetControls(listBox_PufferSp_DB.Text);
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szPufferSp;
                LoadDBPufferSp();
                listBox_PufferSp_DB.Text = szKessel;
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Bearbeiten frm = new Form_PufferSp_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_NEU);
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();

            Point p1 = btn_Neu.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;

            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                RecordSet rs = new RecordSet();
                rs.Open("select Bezeichner from Tab_Pufferspeicher_STAMM where Bezeichner='" + frmLabel.m_szName + "'");
                bool bExist = !rs.EOF();
                rs.Close();

                if (bExist)
                {
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT);
                }
                else
                {
                    frm.SetControls(frmLabel.m_szName);

                    DialogResult ret = frm.ShowDialog();
                    if (ret == DialogResult.OK)
                    {
                        string szKessel = frm.m_szPufferSp;
                        LoadDBPufferSp();
                        listBox_PufferSp_DB.Text = szKessel;
                    }
                }
            }
        }
    }
}

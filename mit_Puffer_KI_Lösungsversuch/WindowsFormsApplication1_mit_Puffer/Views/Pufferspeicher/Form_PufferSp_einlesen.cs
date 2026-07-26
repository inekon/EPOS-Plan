using System;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_einlesen : Form
    {
        private PufferSpImport ctrl = new PufferSpImport();
        private System.Collections.Generic.List<int> _anzeigeIndex = new System.Collections.Generic.List<int>();

        public Form_PufferSp_einlesen ()
        {
            InitializeComponent();
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_VDI3805_Click(object sender, EventArgs e)
        {
            string filename = "";

            Liste_PufferSp.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Pufferspeicher");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.vdi)|*.vdi";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;

                ctrl.Import(filename);
                FuelleListe();
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            {
                {

                    int sel = Liste_PufferSp.SelectedIndex;
                    if (sel < 0 || sel >= _anzeigeIndex.Count) return;
                    int i = _anzeigeIndex[sel];
                    textBox_Name.Text = Liste_PufferSp.Text;
                    textBox_Firma.Text = ctrl._list[i].m_szFirma;
                    textBox_Volumen.Text = ctrl._list[i].m_szVolumen;
                    textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
                    textBox_Typ.Text = ctrl._list[i].m_szTyp;   

                }
            }
        }

        private void Volumenfilter_ValueChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        private void FuelleListe()
        {
            double min = (double)num_VolumenVon.Value;
            double max = (double)num_VolumenBis.Value;

            Liste_PufferSp.BeginUpdate();
            Liste_PufferSp.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                double volumen = Program.convertTxt2Double(ctrl._list[i].m_szVolumen);
                if (volumen < min || volumen > max) continue;
                Liste_PufferSp.Items.Add(ctrl._list[i].m_szName);
                _anzeigeIndex.Add(i);
            }
            Liste_PufferSp.EndUpdate();
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Pufferspeicher selektieren!");
                return;
            }

            try
            {
                PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                if (ctrl.Exists(textBox_Name.Text))
                {
                    MessageBox.Show("Daten bereits eingelesen!");
                    return;
                }

                if (ctrl.InsertFrom(InitDatensatzUpdate()))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme des Pufferspeichers: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                this.DialogResult = DialogResult.Cancel;
            }
        }

        PufferSpModel InitDatensatzUpdate()
        {
            PufferSpModel model = new PufferSpModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Firma.Text;
            model.Speichertyp = textBox_Typ.Text;   
            model.Betriebsbereitschaftverlust = Program.convertTxt2Double(textBox_Versluste.Text);
            model.Gesamtvolumen = Program.convertTxt2Int(textBox_Volumen.Text);

            return model;
        }

    }
}
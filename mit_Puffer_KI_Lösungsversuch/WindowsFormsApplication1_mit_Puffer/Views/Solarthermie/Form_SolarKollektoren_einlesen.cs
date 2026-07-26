using System;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarKollektoren_einlesen: Form
    {
        private Solarkollektorenlmport ctrl = new Solarkollektorenlmport();
        private int index = 0;
        private System.Collections.Generic.List<int> _anzeigeIndex = new System.Collections.Generic.List<int>();

        public Form_SolarKollektoren_einlesen()
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

            Liste_Kollektoren.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Solarthermie");

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
            int sel = Liste_Kollektoren.SelectedIndex;
            if (sel < 0 || sel >= _anzeigeIndex.Count) return;
            int i = _anzeigeIndex[sel];
            textBox_Name.Text = Liste_Kollektoren.Text;
            textBox_Firma.Text = ctrl._list[i].m_szFirma;
            textBox_Bauart.Text = ctrl._list[i].m_szBauart;
            textBox_Leistung.Text = ctrl._list[i].m_Leistung.ToString();
            textBox_Aperturflaeche.Text = ctrl._list[i].m_Aperturfläche.ToString();
            textBox_a1.Text = ctrl._list[i].m_a1.ToString();
            textBox_a2.Text = ctrl._list[i].m_a2.ToString();
            textBox_h0.Text = ctrl._list[i].m_h0.ToString();
            textBox_Kdir.Text = ctrl._list[i].m_kdir.ToString();
            textBox_Kdiff.Text = ctrl._list[i].m_kdiff.ToString();
            index = i;
        }

        private void Kollektorfilter_ValueChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        private void FuelleListe()
        {
            double aMin = (double)num_AperturVon.Value;
            double aMax = (double)num_AperturBis.Value;

            Liste_Kollektoren.BeginUpdate();
            Liste_Kollektoren.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                double apertur = ctrl._list[i].m_Aperturfläche;
                double leistung = ctrl._list[i].m_Leistung;
                if (apertur < aMin || apertur > aMax) continue;
                Liste_Kollektoren.Items.Add(ctrl._list[i].m_szName);
                _anzeigeIndex.Add(i);
            }
            Liste_Kollektoren.EndUpdate();
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Solarkollektor selektieren!");
                return;
            }

            string checkSql = "SELECT COUNT(*) FROM [Tab_Solarkollektoren_STAMM] WHERE Bezeichner = ?";
            OleDbParameter checkParam = new OleDbParameter("?", textBox_Name.Text);
            object checkResult = DataRepository.ExecuteScalar(checkSql, checkParam);

            if (checkResult != null && Convert.ToInt32(checkResult) > 0)
            {
                MessageBox.Show("Daten bereits eingelesen!");
                return;
            }

            try
            {
                SolarkollektorenStammCtrl sctrl = new SolarkollektorenStammCtrl();
                if (sctrl.InsertFrom(InitDatensatzUpdate()))
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
                Console.WriteLine("Fehler bei der Übernahme des Solarkollektors: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                this.DialogResult = DialogResult.Cancel;
            }
        }

        SolarkollektorenModel InitDatensatzUpdate()
        {
            SolarkollektorenModel model = new SolarkollektorenModel();
            
            model.m_szKollektorname = ctrl._list[index].m_szName;
            model.m_szFirma = ctrl._list[index].m_szFirma;
            model.m_szBeschreibung = ctrl._list[index].m_szBeschreibung;
            model.m_szKollektortyp = ctrl._list[index].m_szBauart;
            model.m_h0 = ctrl._list[index].m_h0;
            model.m_k1 = ctrl._list[index].m_a1;
            model.m_k2 = ctrl._list[index].m_a2;
            model.m_Kdir = ctrl._list[index].m_kdir;
            model.m_Kdfu = ctrl._list[index].m_kdiff;
            model.m_Modulfläche = ctrl._list[index].m_Modulfläche;
            model.m_Aperturfläche = ctrl._list[index].m_Aperturfläche;

            return model;
        }

    }
}
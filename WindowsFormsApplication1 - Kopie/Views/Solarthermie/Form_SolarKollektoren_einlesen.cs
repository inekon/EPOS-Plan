using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Web;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarKollektoren_einlesen: Form
    {
        private Solarkollektorenlmport ctrl = new Solarkollektorenlmport();
        private int index = 0;

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

            string szAppDataPath = Path.Combine(Program.ApplicationPath_User, "VDI_Solarthermie");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.vdi)|*.vdi";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;

                ctrl.Import(filename);
                for (int i = 0; i < ctrl._list.Count; i++)
                {
                    Liste_Kollektoren.Items.Add(ctrl._list[i].m_szName);
                }
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i= Liste_Kollektoren.SelectedIndex;
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

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            if (textBox_Name.Text == "")
            {
                MessageBox.Show("Bitte einen Solarkollektor selektieren!");
                return;
            }

            rs.Open("select * from [Tab_Solarkollektoren] where Kollektorname='" + textBox_Name.Text + "'");
            if (rs.Next()) { MessageBox.Show("Daten bereits eingelesen!"); rs.Close(); return; }
            rs.Close();

            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Insert("INSERT INTO [Tab_Solarkollektoren] (Kollektorname) SELECT '" + textBox_Name.Text + "' AS Ausdr1");
                rs.Close();

                SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
                ctrl.model = InitDatensatzUpdate();
                ctrl.DBCommand.Transaction = transaction;

                if (ctrl.Update())
                {
                    transaction.Commit();
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    transaction.Rollback();
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                try
                {
                    // Attempt to roll back the transaction.
                    transaction.Rollback();
                }
                catch
                {
                    // Do nothing here; transaction is not active.
                }
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
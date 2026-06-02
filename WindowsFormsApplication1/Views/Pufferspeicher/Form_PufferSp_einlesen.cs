using System;
using System.Data.Odbc;
using System.IO;
using System.Web;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_einlesen : Form
    {
        private PufferSpImport ctrl = new PufferSpImport();

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
                for (int i = 0; i < ctrl._list.Count; i++)
                {
                    Liste_PufferSp.Items.Add(ctrl._list[i].m_szName);
                }
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            {
                {

                    int i= Liste_PufferSp.SelectedIndex;
                    textBox_Name.Text = Liste_PufferSp.Text;
                    textBox_Firma.Text = ctrl._list[i].m_szFirma;
                    textBox_Volumen.Text = ctrl._list[i].m_szVolumen;
                    textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
                    textBox_Typ.Text = ctrl._list[i].m_szTyp;   

                }
            }
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            if (textBox_Name.Text == "")
            {
                MessageBox.Show("Bitte einen Pufferspeicher selektieren!");
                return;
            }

            rs.Open("select * from Tab_Pufferspeicher where Bezeichner='" + textBox_Name.Text + "'");
            if (rs.Next()) { MessageBox.Show("Daten bereits eingelesen!"); rs.Close(); return; }
            rs.Close(); 
            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Insert("INSERT INTO Tab_Pufferspeicher (Bezeichner) SELECT '" + textBox_Name.Text + "' AS Ausdr1");
                rs.Close();

                PufferSpCtrl ctrl = new PufferSpCtrl();
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
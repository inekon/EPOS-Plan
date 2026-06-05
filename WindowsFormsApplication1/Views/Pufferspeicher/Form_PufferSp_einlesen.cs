using System;
using System.Data.OleDb;
using System.IO;
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
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Pufferspeicher selektieren!");
                return;
            }

            // 1. Vorabprüfung via DataRepository
            string checkSql = "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE Bezeichner = ?";
            OleDbParameter checkParam = new OleDbParameter("?", textBox_Name.Text);
            object checkResult = DataRepository.ExecuteScalar(checkSql, checkParam);

            if (checkResult != null && Convert.ToInt32(checkResult) > 0)
            {
                MessageBox.Show("Daten bereits eingelesen!");
                return;
            }

            // Variable für das Transaktionsmanagement vorbereiten
            OleDbTransaction transaction = null;

            try
            {
                // 2. Verbindung und Transaktion manuell über das DataRepository aufbauen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // 3. Parametrisierter INSERT-Befehl innerhalb der Transaktion
                    string insertSql = "INSERT INTO Tab_Pufferspeicher (Bezeichner) VALUES (?)";

                    using (OleDbCommand insertCmd = conn.CreateCommand())
                    {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = insertSql;
                        insertCmd.Parameters.Add(new OleDbParameter("?", textBox_Name.Text));

                        insertCmd.ExecuteNonQuery();
                    }

                    // 4. Update-Control initialisieren
                    PufferSpCtrl ctrl = new PufferSpCtrl();
                    ctrl.model = InitDatensatzUpdate();

                    // Direktzuweisung der Verbindung und Transaktion an das Steuerelement
                    ctrl.DBCommand.Connection = conn;
                    ctrl.DBCommand.Transaction = transaction;

                    // 5. Ausführen und Validieren des Updates
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme des Pufferspeichers: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);

                if (transaction != null && transaction.Connection != null)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                        // Ignorieren, falls die Transaktion bereits geschlossen oder ungültig ist
                    }
                }

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
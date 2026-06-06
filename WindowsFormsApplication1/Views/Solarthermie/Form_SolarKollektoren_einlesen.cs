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
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Solarkollektor selektieren!");
                return;
            }

            // 1. Vorabprüfung via DataRepository
            string checkSql = "SELECT COUNT(*) FROM [Tab_Solarkollektoren] WHERE Kollektorname = ?";
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
                // 2. Verbindung und Transaktion manuell über den Connection-String aufbauen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // 3. Parametrisierter INSERT-Befehl innerhalb der Transaktion
                    string insertSql = "INSERT INTO [Tab_Solarkollektoren] (Kollektorname) VALUES (?)";

                    using (OleDbCommand insertCmd = conn.CreateCommand())
                    {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = insertSql;
                        insertCmd.Parameters.Add(new OleDbParameter("?", textBox_Name.Text));

                        insertCmd.ExecuteNonQuery();
                    }

                    // 4. Update-Control initialisieren und mit Daten füttern
                    SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
                    ctrl.model = InitDatensatzUpdate();

                    // Direktzuweisung der aktiven Verbindung und Transaktion an das Control,
                    // da ctrl.DBCommand nun fest als OleDbCommand definiert ist.
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
                Console.WriteLine("Fehler bei der Übernahme des Solarkollektors: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);

                // Rollback versuchen, falls die Transaktion aktiv war
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
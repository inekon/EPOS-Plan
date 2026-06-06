using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_Bearbeiten : Form
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szPufferSp = "";
        private int m_mode = MODE_EDIT;

        public Form_PufferSp_Bearbeiten(int mode)
        {
            InitializeComponent();
            m_mode = mode;
            if (mode == MODE_EDIT)
            {
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;
                btn_Ueberschreiben.Enabled = true;
            }
            else
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false;

                comboBox_Speichertyp.Text = "";
                textBox_Hersteller.Text = "";
                textBox_Verluste.Text = "0";
                textBox_Investitionskosten.Text = "0";
                textBox_Volumen.Text = "0";
            }
        }

        public void SetControls(string szName)
        {
            textBox_Name.Text = szName;
            m_szPufferSp = szName;

            // 1. Daten über das DataRepository mittels DataTable abfragen (Ersetzt RecordSet)
            string sql = "SELECT * FROM Tab_Pufferspeicher WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("?", szName ?? (object)DBNull.Value));

            if (dt == null || dt.Rows.Count == 0) return;

            DataRow row = dt.Rows[0];

            // Zuordnung basierend auf der Tabellenstruktur (Indizes analog zur ReadAll-Logik)
            if (row[2] != DBNull.Value) textBox_Hersteller.Text = row[2].ToString();
            if (row[3] != DBNull.Value) comboBox_Speichertyp.Text = row[3].ToString();
            if (row[5] != DBNull.Value) textBox_Volumen.Text = row[5].ToString();
            if (row[4] != DBNull.Value) textBox_Verluste.Text = Convert.ToDouble(row[4]).ToString("F2");
            if (row[6] != DBNull.Value) textBox_Investitionskosten.Text = Convert.ToDouble(row[6]).ToString("F2");
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            OleDbTransaction transaction = null;

            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(frmLabel.m_szName))
                {
                    MessageBox.Show("Bitte einen gültigen Bezeichner eingeben!");
                    return;
                }

                try
                {
                    using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                    {
                        conn.Open();
                        transaction = conn.BeginTransaction();

                        // Existenzprüfung
                        string checkSql = "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE Bezeichner = ?";
                        using (OleDbCommand checkCmd = conn.CreateCommand())
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.CommandText = checkSql;
                            checkCmd.Parameters.Add(new OleDbParameter("?", frmLabel.m_szName));

                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Name existiert bereits!");
                                transaction.Rollback();
                                return;
                            }
                        }

                        textBox_Name.Text = frmLabel.m_szName;
                        m_szPufferSp = frmLabel.m_szName;

                        // INSERT durchführen
                        string insertSql = "INSERT INTO Tab_Pufferspeicher (Bezeichner) VALUES (?)";
                        using (OleDbCommand insertCmd = conn.CreateCommand())
                        {
                            insertCmd.Transaction = transaction;
                            insertCmd.CommandText = insertSql;
                            insertCmd.Parameters.Add(new OleDbParameter("?", frmLabel.m_szName));
                            insertCmd.ExecuteNonQuery();
                        }

                        // Controller initialisieren und updaten
                        PufferSpCtrl ctrl = new PufferSpCtrl();
                        ctrl.model = InitDatensatzUpdate();
                        ctrl.DBCommand.Connection = conn;
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler bei Speichern Unter: " + ex.Message);
                    MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                    if (transaction != null && transaction.Connection != null)
                    {
                        try { transaction.Rollback(); } catch { }
                    }
                }
            }
        }

        PufferSpModel InitDatensatzUpdate()
        {
            PufferSpModel model = new PufferSpModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Hersteller.Text;
            model.Speichertyp = comboBox_Speichertyp.Text;

            int volumen;
            model.Gesamtvolumen = Int32.TryParse(textBox_Volumen.Text, out volumen) ? volumen : 0;

            double verluste;
            model.Betriebsbereitschaftverlust = double.TryParse(textBox_Verluste.Text, out verluste) ? verluste : 0.0;

            double kosten;
            model.Investitionskosten = double.TryParse(textBox_Investitionskosten.Text, out kosten) ? kosten : 0.0;

            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            OleDbTransaction transaction = null;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // INSERT durchführen
                    string insertSql = "INSERT INTO Tab_Pufferspeicher (Bezeichner) VALUES (?)";
                    using (OleDbCommand insertCmd = conn.CreateCommand())
                    {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = insertSql;
                        insertCmd.Parameters.Add(new OleDbParameter("?", m_szPufferSp ?? (object)DBNull.Value));
                        insertCmd.ExecuteNonQuery();
                    }

                    // Controller initialisieren und updaten
                    PufferSpCtrl ctrl = new PufferSpCtrl();
                    ctrl.model = InitDatensatzUpdate();
                    ctrl.DBCommand.Connection = conn;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                if (transaction != null && transaction.Connection != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
            }
        }

        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            OleDbTransaction transaction = null;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // Controller initialisieren und updaten
                    PufferSpCtrl ctrl = new PufferSpCtrl();
                    ctrl.model = InitDatensatzUpdate();
                    ctrl.DBCommand.Connection = conn;
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
                        MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
                    }
                    Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Überschreiben: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                if (transaction != null && transaction.Connection != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
            }
        }
    }
}
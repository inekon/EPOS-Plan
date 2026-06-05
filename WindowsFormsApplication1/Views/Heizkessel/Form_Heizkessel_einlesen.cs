using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_einlesen : Form
    {
        private HeizkesselImport ctrl = new HeizkesselImport();

        string szBrennstoffIndex = string.Empty;
        string szBrennstoffart = string.Empty;
        string szCO2 = string.Empty;
        string szNOx = string.Empty;
        string szCO = string.Empty;

        public Form_Heizkessel_einlesen()
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

            Liste_Heizkessel.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Heizkessel");

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
                    Liste_Heizkessel.Items.Add(ctrl._list[i].m_szName);
                }
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = Liste_Heizkessel.SelectedIndex;
            if (i >= 0 && i < ctrl._list.Count)
            {
                textBox_Name.Text = Liste_Heizkessel.Text;
                textBox_Firma.Text = ctrl._list[i].m_szFirma;
                textBox_Bauart.Text = ctrl._list[i].m_szBauart;
                textBox_ThLeistung.Text = ctrl._list[i].m_szThLeistung;
                textBox_Brennstoff.Text = ctrl._list[i].m_szBrennstoff;
                textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
                textBox__Wirkungsgrad.Text = ctrl._list[i].m_szWirkungsgrad;
                szBrennstoffIndex = ctrl._list[i].m_szBrennstoffIndex;
                szCO2 = ctrl._list[i].m_szCO2;
                szNOx = ctrl._list[i].m_szNOX;
                szCO = ctrl._list[i].m_szCO;
            }
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            OleDbTransaction transaction = null;

            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Heizkessel selektieren!");
                return;
            }

            try
            {
                // 1. Saubere Verbindung über das DataRepository öffnen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // 2. Transaktion auf der neuen OleDbConnection starten
                    transaction = conn.BeginTransaction();

                    // 3. Existenzprüfung via COUNT (Ersetzt die alte rs.Open-Logik)
                    string checkSql = "SELECT COUNT(*) FROM [Tab_Heizkessel] WHERE Name = ?";
                    using (OleDbCommand checkCmd = conn.CreateCommand())
                    {
                        checkCmd.Transaction = transaction;
                        checkCmd.CommandText = checkSql;
                        checkCmd.Parameters.Add(new OleDbParameter("?", textBox_Name.Text));

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Daten bereits eingelesen!");
                            transaction.Rollback();
                            return;
                        }
                    }

                    // 4. Model initialisieren
                    BrennstoffModel model = InitDatensatzUpdate();

                    // 5. Datensatz in einem Rutsch transaktionssicher speichern
                    if (Insert(model, conn, transaction))
                    {
                        // Nur wenn das Insert mitsamt allen Feldern erfolgreich war, festschreiben
                        transaction.Commit();
                        MessageBox.Show("Datensatz erfolgreich neu angelegt.");
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        transaction.Rollback();
                        MessageBox.Show("Fehler: Name existiert bereits oder Datenbankfehler!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Heizkessel Übernehmen: " + ex.Message);
                MessageBox.Show("Fehler: Name existiert bereits oder Datenbankfehler!");

                if (transaction != null && transaction.Connection != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
            }
        }

        // Überladene Insert-Methode, die voll in der aktiven Transaktion arbeitet
        public bool Insert(BrennstoffModel model, OleDbConnection conn, OleDbTransaction transaction)
        {
            try
            {
                string sql = @"INSERT INTO [Tab_Heizkessel] 
                       (Name, Beschreibung, Firma, Ptherm, Brennstoff, Wirkungsgrad_Gas, Wirkungsgrad_Öl, 
                        Investitionskosten, Raumbedarf, Wartungskosten, Nutzungsdauer, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust) 
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                using (OleDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;

                    cmd.Parameters.Add(new OleDbParameter("@nam", model.Name ?? (object)DBNull.Value));
                    cmd.Parameters.Add(new OleDbParameter("@bes", model.Beschreibung ?? (object)DBNull.Value));
                    cmd.Parameters.Add(new OleDbParameter("@fir", model.Firma ?? (object)DBNull.Value));
                    cmd.Parameters.Add(new OleDbParameter("@pth", model.Ptherm));
                    cmd.Parameters.Add(new OleDbParameter("@bre", model.Brennstoff));
                    cmd.Parameters.Add(new OleDbParameter("@wgg", model.Wirkungsgrad_Gas));
                    cmd.Parameters.Add(new OleDbParameter("@wgo", model.Wirkungsgrad_Oel));
                    cmd.Parameters.Add(new OleDbParameter("@inv", model.Investitionskosten));
                    cmd.Parameters.Add(new OleDbParameter("@rau", model.Raumbedarf));
                    cmd.Parameters.Add(new OleDbParameter("@war", model.Wartungskosten));
                    cmd.Parameters.Add(new OleDbParameter("@nut", model.Nutzungsdauer));
                    cmd.Parameters.Add(new OleDbParameter("@co2", model.CO2));
                    cmd.Parameters.Add(new OleDbParameter("@so2", model.SO2));
                    cmd.Parameters.Add(new OleDbParameter("@nox", model.NOx));
                    cmd.Parameters.Add(new OleDbParameter("@co", model.CO));
                    cmd.Parameters.Add(new OleDbParameter("@sta", model.Staub));
                    cmd.Parameters.Add(new OleDbParameter("@bbv", model.Betriebsbereitschaftverlust));

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert (Transaktions-Kontext): " + ex.Message);
                return false;
            }
        }

        BrennstoffModel InitDatensatzUpdate()
        {
            BrennstoffModel model = new BrennstoffModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Firma.Text;
            model.Beschreibung = textBox_Bauart.Text;
            model.Ptherm = Program.convertTxt2Double(textBox_ThLeistung.Text);

            int nBrennstoffart = Program.convertTxt2Int(szBrennstoffart);
            if (nBrennstoffart == 0) model.Wirkungsgrad_Gas = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) / 100;
            else if (nBrennstoffart == 1) model.Wirkungsgrad_Oel = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) / 100;
            else
            {
                model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) / 100;
            }

            if (model.Wirkungsgrad_Gas == 0 && model.Wirkungsgrad_Oel == 0)
                model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = 1;

            model.Betriebsbereitschaftverlust = Program.convertTxt2Double(textBox_Versluste.Text);
            int Brennstoffindex = Program.convertTxt2Int(szBrennstoffIndex);
            if (Brennstoffindex > 22) Brennstoffindex = 23;
            model.Brennstoff = Brennstoffindex;
            model.NOx = Program.convertTxt2Double(szNOx);
            model.CO2 = Program.convertTxt2Double(szCO2);
            model.CO = Program.convertTxt2Double(szCO);

            return model;
        }
    }
}
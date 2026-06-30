using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_EingDBStromverbraucher : Form
    {
        public string m_szStromname;
        public string m_szBeschreibung;
        public string m_szStromtyp;
        public string mode;

        public Form_EingDBStromverbraucher()
        {
            InitializeComponent();

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Stromverbrauchertyp order by Typname");

            while (rs.Next())
            {
                comboBox_Stromtyp.Items.Add(rs.Read("Typname"));
            }
            rs.Close();

        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Stromname.Text = m_szStromname;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Stromtyp.Text = m_szStromtyp;
            rs.Open("select * from Tab_Stromverbraucher where Bezeichner ='" + textBox_Stromname.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = Convert.ToDouble(rs.Read("Monat_1")).ToString("F4");
                Wert2.Text = Convert.ToDouble(rs.Read("Monat_2")).ToString("F4");
                Wert3.Text = Convert.ToDouble(rs.Read("Monat_3")).ToString("F4");
                Wert4.Text = Convert.ToDouble(rs.Read("Monat_4")).ToString("F4");
                Wert5.Text = Convert.ToDouble(rs.Read("Monat_5")).ToString("F4");
                Wert6.Text = Convert.ToDouble(rs.Read("Monat_6")).ToString("F4");
                Wert7.Text = Convert.ToDouble(rs.Read("Monat_7")).ToString("F4");
                Wert8.Text = Convert.ToDouble(rs.Read("Monat_8")).ToString("F4");
                Wert9.Text = Convert.ToDouble(rs.Read("Monat_9")).ToString("F4");
                Wert10.Text = Convert.ToDouble(rs.Read("Monat_10")).ToString("F4");
                Wert11.Text = Convert.ToDouble(rs.Read("Monat_11")).ToString("F4");
                Wert12.Text = Convert.ToDouble(rs.Read("Monat_12")).ToString("F4");
            }
            rs.Close();

            if (mode == "Bearbeiten") btn_Speichern.Enabled = false;
            if (mode == "Neu")
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false;
            }
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            // 1. Validierung der UI-Eingaben (bleibt identisch)
            for (int i = 1; i <= 12; i++)
            {
                string val = this.Controls["Wert" + i.ToString()].Text;
                if (!Program.checkDouble(this.Controls["Wert" + i.ToString()], val)) return;
            }

            // 2. SQL mit Parameter definieren
            string sql = "SELECT * FROM Tab_Stromverbraucher WHERE Bezeichner = ?";
            DataSet dataSet = new DataSet();

            try
            {
                // 3. Verbindung über den Connection-String des Repositories aufbauen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(sql, conn))
                    {
                        // Parameter für den Stromnamen übergeben
                        adapter.SelectCommand.Parameters.Add(new OleDbParameter("?", m_szStromname ?? (object)DBNull.Value));

                        // Daten in das DataSet laden
                        adapter.Fill(dataSet, "test");

                        // Prüfen, ob überhaupt ein Datensatz gefunden wurde
                        if (dataSet.Tables["test"].Rows.Count == 0)
                        {
                            MessageBox.Show("Der zu aktualisierende Datensatz wurde nicht gefunden!");
                            return;
                        }

                        DataRow row = dataSet.Tables["test"].Rows[0];

                        // 4. Werte aus den Textboxen in die DataRow übertragen
                        for (int i = 1; i <= 12; i++)
                        {
                            row["Monat_" + i.ToString()] = double.Parse(this.Controls["Wert" + i.ToString()].Text);
                        }
                        row["Typ"] = comboBox_Stromtyp.Text;
                        row["Beschreibung"] = textBox_Beschreibung.Text;

                        // 5. Änderungen über den OleDbCommandBuilder zurückschreiben
                        using (OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(adapter))
                        {
                            adapter.Update(dataSet, "test");
                            MessageBox.Show("Daten aktualisiert!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Aktualisieren der Daten!");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return;
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            frm.m_szName = textBox_Stromname.Text;
            frm.SetControl();

            if (frm.ShowDialog() == DialogResult.OK)
            {
                // 1. Vorabprüfung mittels DataRepository (Ersetzt das alte RecordSet)
                string checkSql = "SELECT COUNT(*) FROM Tab_Stromverbraucher WHERE Bezeichner = ?";
                OleDbParameter checkParam = new OleDbParameter("?", frm.m_szName);
                object result = DataRepository.ExecuteScalar(checkSql, checkParam);

                if (result != null && Convert.ToInt32(result) > 0)
                {
                    MessageBox.Show("Name existiert bereits!");
                    return;
                }

                textBox_Stromname.Text = frm.m_szName;

                // 2. Daten über OleDb laden und eine neue Zeile hinzufügen
                string selectSql = "SELECT * FROM Tab_Stromverbraucher";
                DataSet dataSet = new DataSet();

                try
                {
                    using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                    {
                        using (OleDbDataAdapter adapter = new OleDbDataAdapter(selectSql, conn))
                        {
                            // Tabelle in das DataSet laden
                            adapter.Fill(dataSet, "test");

                            // Neue Datenzeile (DataRow) basierend auf dem Tabellenschema erstellen
                            DataRow newRow = dataSet.Tables["test"].NewRow();

                            newRow["Bezeichner"] = textBox_Stromname.Text;
                            newRow["Beschreibung"] = textBox_Beschreibung.Text;
                            newRow["Typ"] = comboBox_Stromtyp.Text;

                            for (int i = 1; i <= 12; i++)
                            {
                                newRow["Monat_" + i.ToString()] = double.Parse(this.Controls["Wert" + i.ToString()].Text);
                            }

                            // Die neue Zeile der Tabelle hinzufügen
                            dataSet.Tables["test"].Rows.Add(newRow);

                            // 3. Änderungen mittels OleDbCommandBuilder in die Access-DB schreiben
                            using (OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(adapter))
                            {
                                adapter.Update(dataSet, "test");
                                MessageBox.Show("Daten gespeichert!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Speichern der Daten!");
                    Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                    return;
                }
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // 1. Validierungen 
            if (comboBox_Stromtyp.Text == "")
            {
                MessageBox.Show("Verbrauchertyp auswählen!");
                return;
            }

            for (int i = 1; i <= 12; i++)
            {
                if (this.Controls["Wert" + i.ToString()].Text == "")
                {
                    MessageBox.Show("Eingaben überprüfen!");
                    return;
                }
            }

            // 2. Datenstruktur vorbereiten 
            string selectSql = "SELECT * FROM Tab_Stromverbraucher";
            DataSet dataSet = new DataSet();

            try
            {
                // Verbindung über den zentralen Connection-String des Repositories aufbauen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(selectSql, conn))
                    {
                        // Tabelle in das DataSet laden
                        adapter.Fill(dataSet, "test");

                        // Neue Datenzeile (DataRow) basierend auf dem geladenen Tabellenschema erstellen
                        DataRow newRow = dataSet.Tables["test"].NewRow();

                        newRow["Bezeichner"] = textBox_Stromname.Text;
                        newRow["Beschreibung"] = textBox_Beschreibung.Text;
                        newRow["Typ"] = comboBox_Stromtyp.Text;

                        // Monatsdaten parsen und in die Zeile eintragen
                        for (int i = 1; i <= 12; i++)
                        {
                            newRow["Monat_" + i.ToString()] = double.Parse(this.Controls["Wert" + i.ToString()].Text);
                        }

                        // Die befüllte Zeile der Tabelle im DataSet hinzufügen
                        dataSet.Tables["test"].Rows.Add(newRow);

                        // 3. Änderungen mittels OleDbCommandBuilder in die Access-DB schreiben
                        using (OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder(adapter))
                        {
                            adapter.Update(dataSet, "test");
                            MessageBox.Show("Daten gespeichert!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Aktualisieren der Daten!");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
            }
        }
    }
}
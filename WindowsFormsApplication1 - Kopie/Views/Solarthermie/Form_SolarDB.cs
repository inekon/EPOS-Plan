using System;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarDB : Form
    {
        public SolarkollektorenModel model = new SolarkollektorenModel();
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public int m_mode = MODE_EDIT;
        public string m_szName = "";

        public Form_SolarDB()
        {
            InitializeComponent();
        }
        public void SetControls(string szName)
        {
            SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();

            if (m_mode == MODE_EDIT)
            {
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;
                btn_Überschreiben.Enabled = true;
                ctrl.ReadAll("Kollektorname='" + szName + "'");
                model = ctrl.items[0];
            }
            else
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Überschreiben.Enabled = false;
                model = new SolarkollektorenModel();
                model.m_szKollektorname = szName;
            }

            textBox_Name.Text = model.m_szKollektorname;
            textBox_Firma.Text = model.m_szFirma;
            textBox_Beschreibung.Text = model.m_szBeschreibung;
            textBox_Typ.Text = model.m_szKollektortyp;
            textBox_Modul_A.Text = model.m_Modulfläche.ToString();
            textBox_Absorber_A.Text = model.m_Aperturfläche.ToString();
            textBox_h0.Text = model.m_h0.ToString();
            textBox_k1.Text = model.m_k1.ToString();
            textBox_k2.Text = model.m_k2.ToString();
            textBox_Kdir.Text = model.m_Kdir.ToString();
            textBox_Kdiff.Text = model.m_Kdfu.ToString();
            textBox_Kosten.Text = model.m_Kosten.ToString();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void textBox_Modul_A_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Absorber_A_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_h0_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_k1_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_k2_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_C_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Kdir_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Kdiff_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Ertrag_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Kosten_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void btn_Überschreiben_Click(object sender, EventArgs e)
        {
            SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
            OleDbTransaction transaction = null;

            try
            {
                ctrl.model = InitDatensatzUpdate();

                // 1. Verbindung manuell über das DataRepository öffnen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // 2. Transaktion auf der Verbindung starten
                    transaction = conn.BeginTransaction();

                    // 3. Dem Control die aktive Verbindung und die Transaktion zuweisen
                    ctrl.DBCommand.Connection = conn;
                    ctrl.DBCommand.Transaction = transaction;

                    // 4. Update ausführen und das Ergebnis prüfen
                    if (ctrl.Update())
                    {
                        transaction.Commit();
                        this.DialogResult = DialogResult.OK; // Optional, falls die Form als Dialog geöffnet wurde
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
                Console.WriteLine("Fehler beim Überschreiben des Solarkollektors: " + ex.Message);
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
            model.m_szKollektorname = textBox_Name.Text;
            model.m_szFirma = textBox_Firma.Text;
            model.m_szBeschreibung = textBox_Beschreibung.Text;
            model.m_szKollektortyp = textBox_Typ.Text;
            model.m_Modulfläche = double.Parse(textBox_Modul_A.Text);
            model.m_Aperturfläche = double.Parse(textBox_Absorber_A.Text);
            model.m_h0 = double.Parse(textBox_h0.Text); 
            model.m_k1 = double.Parse(textBox_k1.Text);
            model.m_k2 = double.Parse(textBox_k2.Text);
            model.m_Kdir = double.Parse(textBox_Kdir.Text);
            model.m_Kdfu = double.Parse(textBox_Kdiff.Text);
            model.m_Kosten = double.Parse(textBox_Kosten.Text);
            
            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox_Name.Text))
            {
                MessageBox.Show("Bitte einen Kollektorname eingeben!");
                return;
            }

            OleDbTransaction transaction = null;

            try
            {
                // 1. Verbindung manuell über das DataRepository öffnen
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    // 2. Transaktion starten
                    transaction = conn.BeginTransaction();

                    // 3. Existenzprüfung innerhalb der Transaktion (mit Parameter)
                    string checkSql = "SELECT COUNT(*) FROM Tab_Solarkollektoren WHERE Kollektorname = ?";
                    using (OleDbCommand checkCmd = conn.CreateCommand())
                    {
                        checkCmd.Transaction = transaction;
                        checkCmd.CommandText = checkSql;
                        checkCmd.Parameters.Add(new OleDbParameter("?", textBox_Name.Text));

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Name existiert bereits!");
                            transaction.Rollback(); // Transaktion sauber beenden
                            return;
                        }
                    }

                    // 4. Parametrisierter INSERT-Befehl innerhalb der Transaktion
                    string insertSql = "INSERT INTO Tab_Solarkollektoren (Kollektorname) VALUES (?)";
                    using (OleDbCommand insertCmd = conn.CreateCommand())
                    {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = insertSql;
                        insertCmd.Parameters.Add(new OleDbParameter("?", textBox_Name.Text));

                        insertCmd.ExecuteNonQuery();
                    }

                    // 5. Update-Control initialisieren und mit Daten füttern
                    SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
                    ctrl.model = InitDatensatzUpdate();

                    // Dem Control die aktive Verbindung und Transaktion übergeben
                    ctrl.DBCommand.Connection = conn;
                    ctrl.DBCommand.Transaction = transaction;

                    // 6. Ausführen und Validieren des Updates
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
                Console.WriteLine("Fehler beim Speichern des Solarkollektors: " + ex.Message);
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

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            OleDbTransaction transaction = null;

            System.Drawing.Point p1 = btn_Speichern_Unter.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                // Falls im Dialog kein Name eingegeben wurde, direkt abbrechen
                if (string.IsNullOrEmpty(frmLabel.m_szName))
                {
                    MessageBox.Show("Bitte einen gültigen Kollektorname eingeben!");
                    return;
                }

                try
                {
                    // 1. Verbindung manuell über das DataRepository öffnen
                    using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                    {
                        conn.Open();

                        // 2. Transaktion starten
                        transaction = conn.BeginTransaction();

                        // 3. Existenzprüfung innerhalb der Transaktion (mit Parameter)
                        string checkSql = "SELECT COUNT(*) FROM Tab_Solarkollektoren WHERE Kollektorname = ?";
                        using (OleDbCommand checkCmd = conn.CreateCommand())
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.CommandText = checkSql;
                            checkCmd.Parameters.Add(new OleDbParameter("?", frmLabel.m_szName));

                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Name existiert bereits!");
                                transaction.Rollback(); // Fehler im Original behoben (war vorher Commit)
                                return;
                            }
                        }

                        // Neuen Namen in die TextBox der UI übernehmen
                        textBox_Name.Text = frmLabel.m_szName;

                        // 4. Parametrisierter INSERT-Befehl innerhalb der Transaktion
                        string insertSql = "INSERT INTO Tab_Solarkollektoren (Kollektorname) VALUES (?)";
                        using (OleDbCommand insertCmd = conn.CreateCommand())
                        {
                            insertCmd.Transaction = transaction;
                            insertCmd.CommandText = insertSql;
                            insertCmd.Parameters.Add(new OleDbParameter("?", frmLabel.m_szName));

                            insertCmd.ExecuteNonQuery();
                        }

                        // 5. Update-Control initialisieren und mit Daten füttern
                        SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
                        ctrl.model = InitDatensatzUpdate();

                        // Dem Control die aktive Verbindung und Transaktion übergeben
                        ctrl.DBCommand.Connection = conn;
                        ctrl.DBCommand.Transaction = transaction;

                        // 6. Ausführen und Validieren des Updates
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
                    Console.WriteLine("Fehler bei 'Speichern unter' des Solarkollektors: " + ex.Message);
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");

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
        }
    }
}
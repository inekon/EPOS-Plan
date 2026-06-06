using System;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_Bearbeiten : Form
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szPufferSp = "";
        private int m_mode = MODE_EDIT;

        public Form_PufferSp_Bearbeiten (int mode)
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
                textBox_Volumen .Text = "0";
            }
        }

        public void SetControls(string szName)
        {
            RecordSet rs = new RecordSet();

            textBox_Name.Text = szName;
            m_szPufferSp = szName;  
               
            rs.Open("select * from Tab_Pufferspeicher where Bezeichner='" + szName + "'");
            if (!rs.Next()) { rs.Close(); return; }
            
            textBox_Hersteller.Text = rs.GetString("Hersteller");
            comboBox_Speichertyp.Text = rs.Read("Speichertyp").ToString();
            textBox_Volumen.Text = rs.Read("Gesamtvolumen").ToString();
            textBox_Verluste.Text = ((double)rs.Read("Bereitschaftsverluste")).ToString("F2");
            textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
                
            rs.Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    transaction = Program.DBConnection.BeginTransaction();
                    rs.DBCommand.Transaction = transaction;
                    rs.Open("select Bezeichner from Tab_Pufferspeicher where Bezeichner='" + frmLabel.m_szName + "'");
                    if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); return; }
                    rs.Close();
                
                    textBox_Name.Text = frmLabel.m_szName;
                    m_szPufferSp = frmLabel.m_szName;
                    rs.Insert("INSERT INTO Tab_Pufferspeicher (Bezeichner) SELECT '" + frmLabel.m_szName + "' AS Ausdr1");
                    rs.Close();

                    PufferSpCtrl ctrl = new PufferSpCtrl();
                    ctrl.DBCommand.Transaction = transaction;
                    ctrl.model = InitDatensatzUpdate();
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
        }

        PufferSpModel InitDatensatzUpdate()
        {
            PufferSpModel model = new PufferSpModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Hersteller.Text;
            model.Speichertyp = comboBox_Speichertyp.Text;
            model.Gesamtvolumen = Int32.Parse(textBox_Volumen.Text);
            model.Betriebsbereitschaftverlust = double.Parse(textBox_Verluste.Text);
            model.Investitionskosten = double.Parse(textBox_Investitionskosten.Text);
     
            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Insert("INSERT INTO Tab_Pufferspeicher (Bezeichner) SELECT '" + m_szPufferSp + "' AS Ausdr1");
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

        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            PufferSpCtrl ctrl = new PufferSpCtrl();
            OdbcTransaction transaction = null;

            try
            {
                ctrl.model = InitDatensatzUpdate();
                transaction = Program.DBConnection.BeginTransaction();
                ctrl.DBCommand.Transaction = transaction;
                if (ctrl.Update())
                {
                    transaction.Commit();
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    transaction.Rollback();
                    MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
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
         
    }
}
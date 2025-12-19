using System;
using System.Data.Odbc;
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
            textBox_C.Text = model.m_C.ToString();
            textBox_Kdir.Text = model.m_Kdir.ToString();
            textBox_Kdiff.Text = model.m_Kdfu.ToString();
            textBox_Ertrag.Text = model.m_Ertrag.ToString();
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
            model.m_C = double.Parse(textBox_C.Text);
            model.m_Kdir = double.Parse(textBox_Kdir.Text);
            model.m_Kdfu = double.Parse(textBox_Kdiff.Text);
            model.m_Ertrag = double.Parse(textBox_Ertrag.Text);
            model.m_Kosten = double.Parse(textBox_Kosten.Text);
            return model;

        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            OdbcTransaction transaction = null;
            RecordSet rs = new RecordSet();

            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Open("select Kollektorname from Tab_Solarkollektoren where Kollektorname='" + textBox_Name.Text + "'");
                if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); transaction.Rollback(); rs.Close(); return; }
                rs.Close();

                rs.Insert("INSERT INTO Tab_Solarkollektoren (Kollektorname) SELECT '" + textBox_Name.Text + "' AS Ausdr1");
                rs.Close();

                SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
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

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            System.Drawing.Point p1 = btn_Speichern_Unter.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {
                try
                {
                    transaction = Program.DBConnection.BeginTransaction();
                    rs.DBCommand.Transaction = transaction;
                    rs.Open("select Kollektorname from Tab_Solarkollektoren where Kollektorname='" + frmLabel.m_szName + "'");
                    if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); transaction.Commit(); return; }
                    rs.Close();

                    textBox_Name.Text = frmLabel.m_szName;
            
                    rs.Insert("INSERT INTO Tab_Solarkollektoren (Kollektorname) SELECT '" + frmLabel.m_szName + "' AS Ausdr1");
                    rs.Close();

                    SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();
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
                        MessageBox.Show("Fehler beim Speichern des Datensatzes!");
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
}
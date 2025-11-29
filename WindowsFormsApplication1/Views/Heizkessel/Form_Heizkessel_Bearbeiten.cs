using System;
using System.Data.Common;
using System.Data.Odbc;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_Bearbeiten : Form
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szKessel = "";
        private int m_mode = MODE_EDIT;

        public Form_Heizkessel_Bearbeiten(int mode)
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

                textBox_Beschreibung.Text = "";
                textBox_Hersteller.Text = "";
                tb_th_Leistung.Text = "0";
                tb_Wirkungsgrad.Text = "0.94";
                tb_Wirkungsgrad_Öl.Text = "0";
                tb_B_Verlust.Text = "0";
                tb_Investitionskosten.Text = "0";
                tb_Nutzungsdauer.Text = "0";
                tb_Raumbedarf.Text = "0";
                tb_NOx.Text = "0";
                tb_CO2.Text = "0";
                tb_CO.Text = "0";
                tb_SO2.Text = "0";
                tb_Staub.Text = "0";
                radioButton_Gas.Checked = true; 
            }
        }

        public void SetControls(string szName, string szBeschreibung)
        {
            RecordSet rs = new RecordSet();

            textBox_Name.Text = szName;
            m_szKessel = szName;  
            textBox_Beschreibung.Text = szBeschreibung;
            
            rs.Open("select * from [DB-Heizung] where Name='" + szName + "'");
            if (!rs.Next()) { rs.Close(); return; }
            
            textBox_Hersteller.Text = rs.GetString("Firma");
            tb_th_Leistung.Text = rs.Read("Ptherm").ToString();
            tb_Wirkungsgrad.Text = rs.Read("Wirkungsgrad_Gas").ToString();
            tb_Wirkungsgrad_Öl.Text = rs.Read("Wirkungsgrad_Öl").ToString();
            tb_B_Verlust.Text = ((double)rs.Read("Betriebsbereitschaftverlust")).ToString("F2");
            tb_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
            tb_Nutzungsdauer.Text = rs.Read("Nutzungsdauer").ToString();
            tb_Raumbedarf.Text = rs.Read("Raumbedarf").ToString();
            tb_NOx.Text = rs.Read("NOx").ToString();
            tb_CO2.Text = rs.Read("CO2").ToString();
            tb_CO.Text = rs.Read("CO").ToString();
            tb_SO2.Text = rs.Read("SO2").ToString();
            tb_Staub.Text = rs.Read("Staub").ToString();
            
            if (rs.Read("Brennstoff") != DBNull.Value)
            {
                int brennstoff = (int)rs.Read("Brennstoff");
                if (brennstoff == 1) radioButton_Gas.Checked = true;
                else if (brennstoff == 0) radioButton_Heizoel.Checked = true;
                else if (brennstoff == 3) radioButton_Rapsoel.Checked = true;
                else if (brennstoff == 4) radioButton_Holz.Checked = true;
                else if (brennstoff == 8) radioButton_Fluessiggas.Checked = true;
                else if (brennstoff == 11) radioButton_Biogas.Checked = true;
                else if (brennstoff == 14) radioButton_Fernwaerme.Checked = true;
                else if (brennstoff == 15) radioButton_Strom.Checked = true;
                else radioButton_Sonstige.Checked = true;
            }
            rs.Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            BrennstoffCtrl ctrl = new BrennstoffCtrl();
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

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {
                try
                {
                    transaction = Program.DBConnection.BeginTransaction();
                    rs.DBCommand.Transaction = transaction;
                    rs.Open("select Name from [DB-Heizung] where Name='" + frmLabel.m_szName + "'");
                    if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); return; }
                    rs.Close();
                
                    textBox_Name.Text = frmLabel.m_szName;
                    m_szKessel = frmLabel.m_szName;
                    rs.Insert("INSERT INTO [DB-Heizung] (Name) SELECT '" + frmLabel.m_szName + "' AS Ausdr1");
                    rs.Close();

                    BrennstoffCtrl ctrl = new BrennstoffCtrl();
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

        private void tb_th_Leistung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_Wirkungsgrad_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_Wirkungsgrad_Öl_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_B_Verlust_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_Raumbedarf_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_Nutzungsdauer_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_CO2_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_SO2_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_NOx_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_CO_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void tb_Staub_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void btn_CO2_Click(object sender, EventArgs e)
        {
            if (radioButton_Heizoel.Checked)
                tb_CO2.Text = ((double)290880).ToString();

            if (radioButton_Gas.Checked)
                tb_CO2.Text = ((double)201600).ToString();

            if (radioButton_Fluessiggas.Checked)
                tb_CO2.Text = ((double)238680).ToString();
        }

        BrennstoffModel InitDatensatzUpdate()
        {
            BrennstoffModel model = new BrennstoffModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Hersteller.Text;
            model.Beschreibung = textBox_Beschreibung.Text;
            model.Ptherm = double.Parse(tb_th_Leistung.Text);
            if (radioButton_Heizoel.Checked) model.Brennstoff = 0;
            else if (radioButton_Gas.Checked) model.Brennstoff = 1;
            else if (radioButton_Rapsoel.Checked) model.Brennstoff = 3;
            else if (radioButton_Holz.Checked) model.Brennstoff = 4;
            else if (radioButton_Fluessiggas.Checked) model.Brennstoff = 8;
            else if (radioButton_Biogas.Checked) model.Brennstoff = 11;
            else if (radioButton_Fernwaerme.Checked) model.Brennstoff = 14;
            else if (radioButton_Strom.Checked) model.Brennstoff = 15;
            else model.Brennstoff = 5;
            model.Wirkungsgrad_Gas = double.Parse(tb_Wirkungsgrad.Text);
            model.Wirkungsgrad_Oel = Program.convertTxt2Double(tb_Wirkungsgrad_Öl.Text);
            model.Betriebsbereitschaftverlust = double.Parse(tb_B_Verlust.Text);
            model.Investitionskosten = double.Parse(tb_Investitionskosten.Text);
            model.Nutzungsdauer = double.Parse(tb_Nutzungsdauer.Text);
            model.Raumbedarf = double.Parse(tb_Raumbedarf.Text);
            model.NOx = double.Parse(tb_NOx.Text);
            model.CO2 = double.Parse(tb_CO2.Text);
            model.CO = double.Parse(tb_CO.Text);
            model.SO2 = double.Parse(tb_SO2.Text);
            model.Staub = double.Parse(tb_Staub.Text);
            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            OdbcTransaction transaction = null;

            try
            {
            //    transaction = Program.DBConnection.BeginTransaction();
           //     rs.DBCommand.Transaction = transaction;
                rs.Insert("INSERT INTO [DB-Heizung] (Name) SELECT '" + m_szKessel + "' AS Ausdr1");
                rs.Close();

                BrennstoffCtrl ctrl = new BrennstoffCtrl();
                ctrl.model = InitDatensatzUpdate();
         //       ctrl.DBCommand.Transaction = transaction;

                if (ctrl.Update())
                {
         //           transaction.Commit();
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
     //               transaction.Rollback();
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
                  //  transaction.Rollback();
                 }
                catch
                {
                    // Do nothing here; transaction is not active.
                }
            }
        }
    }
}
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_DBBHKW : Form
    {
        public BHKWModel model = new BHKWModel();
        public bool m_bNeu = false;
        public bool m_bAdmin = false;
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public  int m_mode = MODE_EDIT;
        public string m_szName = "";

        public Form_DBBHKW()
        {
            InitializeComponent();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_DBBHKW_Load(object sender, EventArgs e)
        {
            comboBox_Name.Items.Add(model.m_szBezeichner);
            textBox_Hersteller.Text = model.m_szFirma;
            textBox_Beschreibung.Text = model.m_szBeschreibung;  
        }


        public void SetControls(string szName)
        {
            RecordSet rs = new RecordSet();
            BHKWCtrl ctrl = new BHKWCtrl();

            ctrl.ReadAll();
            ctrl.FillComboBox(comboBox_Name);

            if (m_mode == MODE_EDIT)
            {
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;
                btn_Überschreiben.Enabled = true;
                comboBox_Name.Text = szName;
                ctrl.ReadAll("Bezeichner='" + szName + "'");
                model = ctrl.items[0];
            }
            else
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Überschreiben.Enabled = false;
                model = new BHKWModel();
                model.m_szBezeichner = szName;
                comboBox_Name.Text = szName;  
            }

            textBox_Beschreibung.Text = model.m_szBeschreibung;
            textBox_Motortyp.Text = model.m_szMotortyp;
            textBox_NOx.Text = model.m_NOx.ToString();
            textBox_CO.Text = model.m_CO.ToString();
            textBox_CO2.Text = model.m_CO2.ToString();  
            textBox_SO2.Text = model.m_SO2.ToString();
            textBox_Staub.Text = model.m_Staub.ToString(); 
            textBox_el_Leistung.Text = model.m_Pel.ToString("F2"); 
            textBox_th_Leistung.Text= model.m_Pel.ToString("F2");
            textBox_Wartungskosten.Text = model.m_Wartungskosten_kWhel.ToString("F2");
            textBox_Hersteller.Text = model.m_szFirma;
            textBox_Wirkungsgrad.Text = model.m_Wirkungsgrad.ToString("F2");  
            textBox_Investitionskosten.Text = model.m_Investition_KWel.ToString("F2"); 
            textBox_Nutzungsdauer.Text = model.m_Nutzungsdauer.ToString();
            textBox_Raumbedarf.Text = model.m_Raumbedarf.ToString("F2");
            textBox_Grenzleistung.Text = model.m_Grenzleistung.ToString("F2");
            textBox_Modul.Text = model.m_Kosten_Modul.ToString("F2");
            textBox_Montage.Text = model.m_Kosten_Montage.ToString("F2");
            textBox_Lieferung.Text = model.m_Kosten_Lieferung.ToString("F2");
            textBox_Schallschutzhaube.Text = model.m_Kosten_Schallschutzhaube.ToString("F2");
            textBox_Abgasreinigung.Text = model.m_Kosten_Abgasreinigung.ToString("F2");

            int brennstoff = model.m_Brennstoff; 
            if (brennstoff == 1) radioButton_Gas.Checked = true;
            else if (brennstoff == 2) radioButton_Biogas.Checked = true;
            else if (brennstoff == 8) radioButton_Fluessiggas.Checked = true;
            else if (brennstoff == 4) radioButton_Holz.Checked = true;
            else if (brennstoff == 3) radioButton_Rapsoel.Checked = true;
            else if (brennstoff == 15) radioButton_Strom.Checked = true;
            else if (brennstoff == 5) radioButton_Sonstige.Checked = true;
            else if (brennstoff == 0) radioButton_Heizoel.Checked = true;
            else if (brennstoff == 11) radioButton_BioErdgas.Checked = true;
        }

        private void btn_Überschreiben_Click(object sender, EventArgs e)
        {
            BHKWCtrl ctrl = new BHKWCtrl();
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

        BHKWModel InitDatensatzUpdate()
        {
            BHKWModel model = new BHKWModel();
            model.m_szBezeichner = comboBox_Name.Text;
            model.m_szFirma = textBox_Hersteller.Text;
            model.m_szBeschreibung = textBox_Beschreibung.Text;
            model.m_Pel = double.Parse(textBox_el_Leistung.Text);
            model.m_Ptherm = double.Parse(textBox_th_Leistung.Text);
            if (radioButton_Heizoel.Checked) model.m_Brennstoff = 0;
            else if (radioButton_Gas.Checked) model.m_Brennstoff = 1;
            else if (radioButton_Rapsoel.Checked) model.m_Brennstoff = 3;
            else if (radioButton_Holz.Checked) model.m_Brennstoff = 4;
            else if (radioButton_Fluessiggas.Checked) model.m_Brennstoff = 8;
            else if (radioButton_Biogas.Checked) model.m_Brennstoff = 2;
            else if (radioButton_Strom.Checked) model.m_Brennstoff = 15;
            else if (radioButton_Sonstige.Checked) model.m_Brennstoff = 5;
            else if (radioButton_BioErdgas.Checked) model.m_Brennstoff = 11;

            model.m_Wirkungsgrad = double.Parse(textBox_Wirkungsgrad.Text);
            model.m_Investition_KWel = double.Parse(textBox_Investitionskosten.Text);
            model.m_Nutzungsdauer = Int32.Parse(textBox_Nutzungsdauer.Text);
            model.m_Raumbedarf = double.Parse(textBox_Raumbedarf.Text);
            model.m_NOx = Int32.Parse(textBox_NOx.Text);
            model.m_CO2 = Int32.Parse(textBox_CO2.Text);
            model.m_CO = Int32.Parse(textBox_CO.Text);
            model.m_SO2 = Int32.Parse(textBox_SO2.Text);
            model.m_Staub = Int32.Parse(textBox_Staub.Text);
            model.m_Grenzleistung = double.Parse(textBox_Grenzleistung.Text);
            model.m_Wartungskosten_kWhel = double.Parse(textBox_Wartungskosten.Text);
            model.m_szMotortyp = textBox_Motortyp.Text;
            model.m_Kosten_Modul = double.Parse(textBox_Modul.Text);
            model.m_Kosten_Montage = double.Parse(textBox_Montage.Text);
            model.m_Kosten_Lieferung = double.Parse(textBox_Lieferung.Text);    
            model.m_Kosten_Schallschutzhaube = double.Parse(textBox_Schallschutzhaube.Text);
            model.m_Kosten_Abgasreinigung = double.Parse(textBox_Abgasreinigung.Text);
            return model;
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
                    rs.Open("select Name from Tab_BHKW where Bezeichner='" + frmLabel.m_szName + "'");
                    if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); rs.Close(); return; }
                    rs.Close();

                    comboBox_Name.Text = frmLabel.m_szName;
                    comboBox_Name.Items.Add(frmLabel.m_szName);
  
                    rs.Insert("INSERT INTO Tab_BHKW (Bezeichner) SELECT '" + frmLabel.m_szName + "' AS Ausdr1");
                    rs.Close();

                    BHKWCtrl ctrl = new BHKWCtrl();
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

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            OdbcTransaction transaction = null;
            RecordSet rs = new RecordSet();

            try
            {
                transaction = Program.DBConnection.BeginTransaction();
                rs.DBCommand.Transaction = transaction;
                rs.Open("select Bezeichner from Tab_BHKW where Bezeichner='" + comboBox_Name.Text + "'");
                if (!rs.EOF()) { MessageBox.Show("Name existiert bereits!"); transaction.Rollback(); rs.Close(); return; }
                rs.Close();

                rs.Insert("INSERT INTO Tab_BHKW (Bezeichner) SELECT '" + comboBox_Name.Text + "' AS Ausdr1");
                rs.Close();

                BHKWCtrl ctrl = new BHKWCtrl();
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

        private void textBox_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_th_Leistung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_el_Leistung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Wirkungsgrad_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Grenzleistung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Raumbedarf_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Wartungskosten_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Nutzungsdauer_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Modul_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Montage_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Lieferung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Schallschutzhaube_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Abgasreinigung_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

        private void textBox_CO2_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_SO2_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_NOx_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_CO_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_Staub_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void btn_Eintragen_Click(object sender, EventArgs e)
        {
            //Wenn Heizöl aktiviert ist, trage die entsprechenden Werte ein
            if (radioButton_Heizoel.Checked)
            {
                textBox_SO2.Text = "270";
                textBox_CO2.Text = "265000";
                //Wenn Checkbox "mit SCR" aktiviert ist
                if(checkBox_SCR.Checked)
                {
                    textBox_NOx.Text = "450";
                    textBox_CO.Text = "280";
                    textBox_Staub.Text = "80"; 
                }
                //Wenn Checkbox "mit SCR" n i c h t aktiviert ist
                else
                {
                    textBox_NOx.Text = "4400";
                    textBox_CO.Text = "140";
                    textBox_Staub.Text = "80";
                }
            }


            //Wenn Gas oder Biogas aktiviert ist, trage die entsprechenden Werte ein
            if(radioButton_Gas.Checked || radioButton_BioErdgas.Checked)
            {
                textBox_SO2.Text = "0";
                textBox_CO2.Text = "200000";
                //Wenn die thermische Leistung größer als 1.000 kW ist
                if (double.Parse(textBox_th_Leistung.Text) > 1000)
                {
                    textBox_NOx.Text = "250";
                    textBox_CO.Text = "250";
                    textBox_Staub.Text = "0";
                }
                else {
                    //Wenn die thermische Leistung n i c h t größer als 1.000 kW ist
                    textBox_NOx.Text = "285";
                    textBox_CO.Text = "370";
                    textBox_Staub.Text = "0";
                }
            }

        }

        private void btn_CO2_Click(object sender, EventArgs e)
        {
            //Wenn Heizöl aktiviert wurde, trage den CO2-Wert für Heizöl ein
            if (radioButton_Heizoel.Checked) textBox_CO2.Text = "290880";
            //Wenn Gas aktiviert wurde, trage den CO2-Wert für Gas ein
            if (radioButton_Gas.Checked) textBox_CO2.Text = "201600";
            //Wenn Flüssiggas aktiviert wurde, trage den CO2-Wert für Flüssiggas ein
            if(radioButton_Fluessiggas.Checked) textBox_CO2.Text = "238680";
        }
    }
}
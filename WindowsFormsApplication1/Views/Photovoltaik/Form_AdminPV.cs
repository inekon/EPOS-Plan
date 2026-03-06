using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace WindowsFormsApplication1
{
    public partial class Form_AdminPV : Form
    {
        OdbcCommand DBCommand;
        PhotovoltaikModel model = new PhotovoltaikModel();
        public List<WErzeugerModel> list_pvmodel = new List<WErzeugerModel>();
        public bool m_bItemBearbeiten = false;
        private bool m_Neu = false;

        public Form_AdminPV ()
        {
            InitializeComponent();
            DBCommand = Program.DBConnection.CreateCommand();
        }
        
        public void SetControls(string projekt)
        {
            listBox_PV.Items.Clear();
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                listBox_PV.Items.Add(list_pvmodel[i].Bezeichner);
            }
            if (listBox_PV.Items.Count > 0) listBox_PV.SelectedIndex = 0;
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_AdminPV_Load(object sender, EventArgs e)
        {
            if (m_bItemBearbeiten) return;

            RecordSet rs = new RecordSet();
            rs.Open("SELECT * FROM Tab_PV");   
            
            while (rs.Next())
            {
                string bezeichner = rs.Read("Modulname").ToString();
                Console.WriteLine("Bezeichner: {bezeichner}");
                listBox_PV.Items.Add(bezeichner);
            }
            if(listBox_PV.Items.Count > 0)  listBox_PV.SelectedIndex = 0;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            try
            {
                model.m_szName = textBox_Bezeichner.Text;
                model.m_szBeschreibung = textBox_Beschreibung.Text;
                model.m_Wirkungsgrad = textBox_Wirkungsgrad.Text == "" ? 0.0 : double.Parse(textBox_Wirkungsgrad.Text);
                model.m_Leistung = textBox_Leistung.Text == "" ? 0.0 : double.Parse(textBox_Leistung.Text);
                model.m_U_Leerlauf = textBox_ULeerlauf.Text == "" ? 0.0 : double.Parse(textBox_ULeerlauf.Text);
                model.m_U_Mpp = textBox_UMpp.Text == "" ? 0.0 : double.Parse(textBox_UMpp.Text);
                model.m_I_Mpp = textBox_IMpp.Text == "" ? 0.0 : double.Parse(textBox_IMpp.Text);
                model.m_I_Kurzschluss = textBox_IKurzschluss.Text == "" ? 0.0 : double.Parse(textBox_IKurzschluss.Text);    
                model.m_Temp_Coeff_Pmax = textBox_TempKoeff.Text == "" ? 0.0 : double.Parse(textBox_TempKoeff.Text);
                model.m_Laenge = textBox_Laenge.Text == "" ? 0.0 : double.Parse(textBox_Laenge.Text);   
                model.m_Breite = textBox_Breite.Text == "" ? 0.0 : double.Parse(textBox_Breite.Text);

                if (m_Neu)
                {
                    string sql = FormattableString.Invariant($@"
                        INSERT INTO TAB_PV ( 
                            Modulname, Firma, Beschreibung, Leistung, Wirkungsgrad,
                            U_Mpp, U_Leerlauf, I_Mpp, I_Kurzschluss, Temp_Koeffizient, Laenge, Breite 
                        ) 
                        SELECT 
                            '{textBox_Bezeichner.Text}' AS Ausdr1, 
                            '{textBox_Firma.Text}' AS Ausdr2, 
                            '{textBox_Beschreibung.Text}' AS Ausdr3, 
                            {model.m_Leistung} AS Ausdr4, 
                            {model.m_Wirkungsgrad} AS Ausdr5, 
                            {model.m_U_Mpp} AS Ausdr6, 
                            {model.m_U_Leerlauf} AS Ausdr7, 
                            {model.m_I_Mpp} AS Ausdr8, 
                            {model.m_I_Kurzschluss} AS Ausdr9, 
                            {model.m_Temp_Coeff_Pmax} AS Ausdr10, 
                            {model.m_Laenge} AS Ausdr11, 
                            {model.m_Breite} AS Ausdr12;");

                    DBCommand.CommandText = sql; 
                    DBCommand.ExecuteNonQuery();
                    listBox_PV.Items.Add(textBox_Bezeichner.Text);
                    listBox_PV.SelectedIndex = listBox_PV.Items.Count - 1;
                    m_Neu = false;
                }
                else
                {
                    string sql = FormattableString.Invariant($@"
                        UPDATE Tab_PV SET 
                            Modulname = '{textBox_Bezeichner.Text}', 
                            Firma = '{textBox_Firma.Text}', 
                            Beschreibung = '{textBox_Beschreibung.Text}', 
                            Leistung = {model.m_Leistung}, 
                            Wirkungsgrad = {model.m_Wirkungsgrad}, 
                            U_Mpp = {model.m_U_Mpp}, 
                            U_Leerlauf = {model.m_U_Leerlauf}, 
                            I_Mpp = {model.m_I_Mpp}, 
                            I_Kurzschluss = {model.m_I_Kurzschluss}, 
                            Temp_Koeffizient = {model.m_Temp_Coeff_Pmax}, 
                            Laenge = {model.m_Laenge}, 
                            Breite = {model.m_Breite}
                        WHERE Modulname = '{listBox_PV.Text}';");

                    DBCommand.CommandText = sql;    
                    DBCommand.ExecuteNonQuery();
                    
                    MessageBox.Show("Datensatz gespeichert!");
                }
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                m_Neu = false;
                InitControls();
                return;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                m_Neu = false;
                InitControls();
                return;
            }
            return;
        }

        private void listBox_PV_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            textBox_Bezeichner.Text = listBox_PV.Text;
            model.m_szName = textBox_Bezeichner.Text;
            
            rs.Open("SELECT * FROM Tab_PV where Modulname='" + listBox_PV.Text + "'");

            if (!rs.EOF())
            {
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                model.m_szBeschreibung = textBox_Beschreibung.Text;
                textBox_Firma.Text = (string)rs.Read("Firma");
                model.m_szFirma = textBox_Firma.Text;
                textBox_Wirkungsgrad.Text = rs.Read("Wirkungsgrad").ToString();
                model.m_Wirkungsgrad = Program.convertTxt2Double(textBox_Wirkungsgrad.Text);
                textBox_Leistung.Text = rs.Read("Leistung").ToString();
                model.m_Leistung = Program.convertTxt2Double(textBox_Leistung.Text);
                
                textBox_ULeerlauf.Text = rs.Read("U_Leerlauf").ToString();
                model.m_U_Leerlauf = Program.convertTxt2Double(textBox_ULeerlauf.Text);
                textBox_UMpp.Text = rs.Read("U_Mpp").ToString();
                model.m_U_Mpp = Program.convertTxt2Double(textBox_UMpp.Text);
                textBox_IMpp.Text = rs.Read("I_Mpp").ToString();
                model.m_I_Mpp = Program.convertTxt2Double(textBox_IMpp.Text);
                textBox_IKurzschluss.Text = rs.Read("I_Kurzschluss").ToString();
                model.m_I_Kurzschluss = Program.convertTxt2Double(textBox_IKurzschluss.Text);
                textBox_TempKoeff.Text = rs.Read("Temp_Koeffizient").ToString();
                model.m_Temp_Coeff_Pmax = Program.convertTxt2Double(textBox_TempKoeff.Text);
                textBox_Laenge.Text = rs.Read("Laenge").ToString();
                model.m_Laenge = Program.convertTxt2Double(textBox_Laenge.Text);
                textBox_Breite.Text = rs.Read("Breite").ToString();
                model.m_Breite = Program.convertTxt2Double(textBox_Breite.Text);
            }
            rs.Close();
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            InitControls();
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            
            Point p1 = btn_Neu.Location;  
            p1 = this.PointToScreen(p1); 
            
            frm.Location = p1;  

            frm.ShowDialog();
            if (frm.result == DialogResult.OK)
            {
                m_Neu = true;
                textBox_Bezeichner.Text = frm.m_szName;
                textBox_Firma.Text = "";
                textBox_Beschreibung.Text = "";
                textBox_ULeerlauf.Text = "0";
                textBox_UMpp.Text = "0";
                textBox_Leistung.Text = "0";
                textBox_Wirkungsgrad.Text = "0";
                textBox_IMpp.Text = "0";
                textBox_IKurzschluss.Text = "0";
                textBox_TempKoeff.Text = "0";
                textBox_Laenge.Text = "0";
                textBox_Breite.Text = "0";
            }
            return;
        }

        private void InitControls()
        {
            m_Neu = false;
            textBox_Bezeichner.Text = "";
            textBox_Firma.Text = "";
            textBox_Beschreibung.Text = "";
            textBox_UMpp.Text = "";
            textBox_ULeerlauf.Text = "";
            textBox_Wirkungsgrad.Text = "";
            textBox_Leistung.Text = "";
            textBox_Firma.Text = "";
            textBox_IMpp.Text = "";
            textBox_IKurzschluss.Text = "";
            textBox_TempKoeff.Text = "";
            textBox_Laenge.Text = "";
            textBox_Breite.Text = "";
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_PV.SelectedIndex == -1)
            {
                MessageBox.Show("Modul in Liste auswählen!");
                return;
            }
            try
            {
                DBCommand.CommandText = "DELETE * from  Tab_PV where Modulname='" + textBox_Bezeichner.Text + "'";
                DBCommand.ExecuteNonQuery();
                listBox_PV.Items.Remove(textBox_Bezeichner.Text);
                listBox_PV.SelectedIndex = listBox_PV.Items.Count - 1;
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                MessageBox.Show("Modul kann nicht gelöscht werden.\nEs besteht eine Projektzordnung!");  
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return;
            }

        }

        private void textBox_Leistung_Validating(object sender, CancelEventArgs e)
        {
            if (textBox_Leistung.Text == "") 
            { 
                MessageBox.Show("Leistungseingabe überprüfen!");
                return;
            }
            if (!Program.checkDouble(textBox_Leistung, textBox_Leistung.Text)) { textBox_Leistung.Undo(); }
        }

        private void textBox_Wirkungsgrad_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Wirkungsgrad, textBox_Wirkungsgrad.Text)) { textBox_Wirkungsgrad.Undo(); }
        }

        private void textBox_UMpp_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_UMpp, textBox_UMpp.Text)) { textBox_UMpp.Undo(); }
        }

        private void textBox_ULeerlauf_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_ULeerlauf, textBox_ULeerlauf.Text)) { textBox_ULeerlauf.Undo(); }
        }

        private void textBox_IMpp_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_IMpp, textBox_IMpp.Text)) { textBox_IMpp.Undo(); }

        }
        private void textBox_IKurzschluss_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_IKurzschluss, textBox_IKurzschluss.Text)) { textBox_IKurzschluss.Undo(); }
        }
        private void textBox_TempKoeff_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_TempKoeff, textBox_TempKoeff.Text)) { textBox_TempKoeff.Undo(); }
        }
        private void textBox_Laenge_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Laenge, textBox_Laenge.Text)) { textBox_Laenge.Undo(); }
        }
        private void textBox_Breite_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Breite, textBox_Breite.Text)) { textBox_Breite.Undo(); }
        }
    }
}

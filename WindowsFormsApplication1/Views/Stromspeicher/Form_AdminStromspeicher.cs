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
    public partial class Form_AdminStromspeicher : Form
    {
        OdbcCommand DBCommand;
        StromspeicherModel model = new StromspeicherModel();
        public List<WErzeugerModel> list_spmodel = new List<WErzeugerModel>();
        public bool m_bItemBearbeiten = false;
        private bool m_Neu = false;

        public Form_AdminStromspeicher()
        {
            InitializeComponent();
            DBCommand = Program.DBConnection.CreateCommand();
        }
        
        public void SetControls(string projekt)
        {
            listBox_Stromspeicher.Items.Clear();
            for (int i = 0; i < list_spmodel.Count; i++)
            {
                listBox_Stromspeicher.Items.Add(list_spmodel[i].Bezeichner);
            }
            if (listBox_Stromspeicher.Items.Count > 0) listBox_Stromspeicher.SelectedIndex = 0;
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_Stromspeicher_Load(object sender, EventArgs e)
        {
            if (m_bItemBearbeiten) return;

            RecordSet rs = new RecordSet();
            rs.Open("SELECT * FROM Tab_Stromspeicher");   
            
            while (rs.Next())
            {
                string bezeichner = rs.Read("Bezeichner").ToString();
                Console.WriteLine("Bezeichner: {bezeichner}");
                listBox_Stromspeicher.Items.Add(bezeichner);
            }
            listBox_Stromspeicher.SelectedIndex = 0;
        }

        private TextBox GetTextBox_Energie()
        {
            return textBox_Energie;
        }
        
        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (textBox_Degradation.Text == "" || textBox_Energie.Text == "" || textBox_Ladezustand.Text == "" ||
                textBox_Leistung.Text == "" || textBox_Typ.Text == "")
            {
                MessageBox.Show("Eingaben überprüfen!");
                return;
            }

            try
            {
                model.m_Energie = double.Parse(textBox_Energie.Text);
                model.m_Leistung = double.Parse(textBox_Leistung.Text);
                model.m_Degradation = double.Parse(textBox_Degradation.Text);
                model.m_Ladezustand = double.Parse(textBox_Ladezustand.Text);

                if (m_Neu)
                {
                    DBCommand.CommandText = "INSERT INTO TAB_Stromspeicher ( Bezeichner, Typ, Leistung, Energie, Degradation, Ladezustand ) SELECT '" + textBox_Bezeichner.Text +
                        "' AS Ausdr1, '" + textBox_Typ.Text + "' AS Ausdr2, " + 
                    model.m_Leistung.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " AS Ausdr3, " + 
                    model.m_Energie.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " AS Ausdr4, " + 
                    model.m_Degradation.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " AS Ausdr5, " +
                    model.m_Ladezustand.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " AS Ausdr6";

                    DBCommand.ExecuteNonQuery();
                    listBox_Stromspeicher.Items.Add(textBox_Bezeichner.Text);
                    listBox_Stromspeicher.SelectedIndex = listBox_Stromspeicher.Items.Count - 1;
                    m_Neu = false;
                }
                else
                {
                    string sql = "UPDATE Tab_Stromspeicher SET " + 
                        "Bezeichner='" + textBox_Bezeichner.Text.ToString(CultureInfo.CreateSpecificCulture("en-US"))  + "', " +
                        "Typ='" + textBox_Typ.Text.ToString(CultureInfo.CreateSpecificCulture("en-US"))  + "', " +
                        "Leistung=" + model.m_Leistung.ToString(CultureInfo.CreateSpecificCulture("en-US")) + ", " +
                        "Energie=" + model.m_Energie.ToString(CultureInfo.CreateSpecificCulture("en-US")) + ", " +
                        "Degradation=" + model.m_Degradation.ToString(CultureInfo.CreateSpecificCulture("en-US")) + ", " + 
                        "Ladezustand=" + model.m_Ladezustand.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " " +
                        "WHERE Bezeichner='" + listBox_Stromspeicher.Text + "'";
                    DBCommand.CommandText = sql;    
                    DBCommand.ExecuteNonQuery();
                }
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                m_Neu = false;
                InitControls();
                return;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                m_Neu = false;
                InitControls();
                return;
            }
            return;
        }

        private void listBox_Stromspeicher_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            textBox_Bezeichner.Text = listBox_Stromspeicher.Text;
            model.m_szBezeichner = textBox_Bezeichner.Text;
            
            rs.Open("SELECT * FROM Tab_Stromspeicher where Bezeichner='" + listBox_Stromspeicher.Text + "'");

            if (!rs.EOF())
            {
                textBox_Energie.Text = rs.Read("Energie").ToString();
                model.m_Energie = double.Parse(textBox_Energie.Text);
                textBox_Leistung.Text = rs.Read("Leistung").ToString();
                model.m_Energie = double.Parse(textBox_Energie.Text);
                textBox_Typ.Text = (string)rs.Read("Typ");
                model.m_szTyp = textBox_Typ.Text;
                textBox_Degradation.Text = rs.Read("Degradation").ToString();
                model.m_Degradation = Double.Parse(textBox_Degradation.Text);
                textBox_Ladezustand.Text = rs.Read("Ladezustand").ToString();
                model.m_Ladezustand = double.Parse(textBox_Ladezustand.Text);
                textBox_Bezeichner.Text = rs.Read("Bezeichner").ToString();
                model.m_szBezeichner = textBox_Bezeichner.Text;
            }

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
                textBox_Typ.Text = "Lithium-Ionen";
                textBox_Degradation.Text = "0";
                textBox_Ladezustand.Text = "0";
                textBox_Leistung.Text = "0";
                textBox_Energie.Text = "0";
            }
            return;
        }

        private void InitControls()
        {
            m_Neu = false;
            textBox_Bezeichner.Text = "";
            textBox_Typ.Text = "";
            textBox_Ladezustand.Text = "";
            textBox_Degradation.Text = "";
            textBox_Energie.Text = "";
            textBox_Leistung.Text = "";
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close(); 
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_Stromspeicher.SelectedIndex == -1)
            {
                MessageBox.Show("Stromspeicher in Liste auswählen!");
                return;
            }
            try
            {
                DBCommand.CommandText = "DELETE * from  Tab_Stromspeicher where Bezeichner='" + textBox_Bezeichner.Text + "'";
                DBCommand.ExecuteNonQuery();
                listBox_Stromspeicher.Items.Remove(textBox_Bezeichner.Text);
                listBox_Stromspeicher.SelectedIndex = listBox_Stromspeicher.Items.Count - 1;
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                MessageBox.Show("Stromspeicher kann nicht gelöscht werden.\nEs besteht eine Projektzordnung!");  
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

        private void textBox_Typ_Validating(object sender, CancelEventArgs e)
        {
            if (textBox_Typ.Text == "") { MessageBox.Show("Eingabe überprüfen!"); }
        }

        private void textBox_Leistung_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Leistung, textBox_Leistung.Text)) { textBox_Leistung.Undo(); }
        }

        private void textBox_Energie_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Energie, textBox_Energie.Text)) { textBox_Energie.Undo(); }
        }

        private void textBox_Ladezustand_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Ladezustand, textBox_Ladezustand.Text)) { textBox_Ladezustand.Undo(); }
        }

        private void textBox_Degradation_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Degradation, textBox_Degradation.Text)) { textBox_Degradation.Undo(); }
        }

    }
}

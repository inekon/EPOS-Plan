using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_KostenfaktorItem : Form
    {
        public int gewählteID;
        public string Nutzungsdauer;
        public string Wert;
        public string Einheit;
        public string Gruppe;

        public Form_KostenfaktorItem()
        {
            InitializeComponent();
   
            List<ComboItem> items = new List<ComboItem>();
            RecordSet rs = new RecordSet();
            
            rs.Open("select * from Tab_Kostenfaktor where IsMainComponent=false");
            while(rs.Next())
            {
                items.Add(new ComboItem((string)rs.Read("Bezeichnung"), (int)rs.Read("StammID")));
            }
            rs.Close();

            // Binden
            comboBox1.DataSource = items;
            comboBox1.DisplayMember = "Text"; // Was der User sieht
            comboBox1.ValueMember = "ID";     // Was der Code verarbeitet
        }

        private void Form_KostenfaktorItem_Load(object sender, EventArgs e)
        {
            try
            {
                // Provider-String zentral aus DataRepository (x64-Umstellung P1.2)
                string connString = DataRepository.GetConnectionString();

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();

                    string sql = "SELECT GruppenName FROM Tab_KostenGruppenKatalog ORDER BY GruppenName";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    OleDbDataReader reader = cmd.ExecuteReader();

                    comboBox_Gruppe.Items.Clear();
                    while (reader.Read())
                    {
                        comboBox_Gruppe.Items.Add(reader["GruppenName"].ToString());
                    }
                }

                // Komfort-Einstellungen
                comboBox_Gruppe.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                comboBox_Gruppe.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            catch { /* Fehlerbehandlung */ }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            // Sicherstellen, dass etwas ausgewählt wurde
            if (comboBox1.SelectedValue != null)
            {
                gewählteID = Convert.ToInt32(comboBox1.SelectedValue);
            }

            // Werte aus den Textboxen/Comboboxen zuweisen
            Nutzungsdauer = textBox_Nutzungsdauer.Text;
            Wert = textBox_Wert.Text;
            Einheit = textBox_Einheit.Text;

            // WICHTIG: Die Gruppe aus der NEUEN ComboBox nehmen
            Gruppe = comboBox_Gruppe.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    public class ComboItem
    {
        public string Text { get; set; }
        public int ID { get; set; }

        // Konstruktor für bequemes Hinzufügen
        public ComboItem(string text, int id)
        {
            Text = text;
            ID = id;
        }
    }
}

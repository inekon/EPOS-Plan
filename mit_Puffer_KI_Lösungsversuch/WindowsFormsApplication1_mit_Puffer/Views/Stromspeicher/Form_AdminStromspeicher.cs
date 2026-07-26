using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_AdminStromspeicher : Form
    {
        private StromspeicherModel model = new StromspeicherModel();
        public List<WErzeugerModel> list_spmodel = new List<WErzeugerModel>();
        public bool m_bItemBearbeiten = false;
        private bool m_Neu = false;

        public Form_AdminStromspeicher()
        {
            InitializeComponent();
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

            string sql = "SELECT Bezeichner FROM Tab_Stromspeicher_STAMM";
            DataTable dt = DataRepository.GetDataTable(sql);

            listBox_Stromspeicher.Items.Clear();
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row["Bezeichner"] != DBNull.Value)
                    {
                        listBox_Stromspeicher.Items.Add(row["Bezeichner"].ToString());
                    }
                }
            }

            if (listBox_Stromspeicher.Items.Count > 0)
            {
                listBox_Stromspeicher.SelectedIndex = 0;
            }
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
                model.m_Modulkosten = double.Parse(textBox_Modulkosten.Text);

                if (m_Neu)
                {
                    StromspeicherStammCtrl sctrl = new StromspeicherStammCtrl();
                    sctrl.m_szBezeichner = textBox_Bezeichner.Text;
                    sctrl.m_szTyp = textBox_Typ.Text;
                    sctrl.m_Leistung = model.m_Leistung;
                    sctrl.m_Energie = model.m_Energie;
                    sctrl.m_Degradation = model.m_Degradation;
                    sctrl.m_Ladezustand = model.m_Ladezustand;
                    sctrl.m_Modulkosten = model.m_Modulkosten;

                    if (!sctrl.Insert()) { MessageBox.Show("Fehler beim Speichern der Daten!"); return; }

                    listBox_Stromspeicher.Items.Add(textBox_Bezeichner.Text);
                    listBox_Stromspeicher.SelectedIndex = listBox_Stromspeicher.Items.Count - 1;
                    m_Neu = false;
                    MessageBox.Show("Daten gespeichert!");
                }
                else
                {
                    StromspeicherStammCtrl sctrl = new StromspeicherStammCtrl();
                    sctrl.m_szBezeichner = textBox_Bezeichner.Text;
                    sctrl.m_szTyp = textBox_Typ.Text;
                    sctrl.m_Leistung = model.m_Leistung;
                    sctrl.m_Energie = model.m_Energie;
                    sctrl.m_Degradation = model.m_Degradation;
                    sctrl.m_Ladezustand = model.m_Ladezustand;
                    sctrl.m_Modulkosten = model.m_Modulkosten;

                    if (!sctrl.Update(listBox_Stromspeicher.Text)) return;

                    int currentIndex = listBox_Stromspeicher.SelectedIndex;
                    if (currentIndex != -1)
                    {
                        listBox_Stromspeicher.Items[currentIndex] = textBox_Bezeichner.Text;
                    }

                    MessageBox.Show("Daten gespeichert!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern des Stromspeichers: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern der Daten!");
                m_Neu = false;
                InitControls();
                return;
            }
        }

        private void listBox_Stromspeicher_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Stromspeicher.Text)) return;

            textBox_Bezeichner.Text = listBox_Stromspeicher.Text;
            model.m_szBezeichner = textBox_Bezeichner.Text;

            string sql = "SELECT * FROM Tab_Stromspeicher_STAMM WHERE Bezeichner = ?";
            OleDbParameter parameter = new OleDbParameter("?", listBox_Stromspeicher.Text);
            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                textBox_Energie.Text = row["Energie"].ToString();
                model.m_Energie = double.Parse(textBox_Energie.Text);

                textBox_Leistung.Text = row["Leistung"].ToString();
                model.m_Leistung = double.Parse(textBox_Leistung.Text); // Fehler korrigiert: war vorher model.m_Energie

                textBox_Typ.Text = row["Typ"] != DBNull.Value ? row["Typ"].ToString() : "";
                model.m_szTyp = textBox_Typ.Text;

                textBox_Degradation.Text = row["Degradation"].ToString();
                model.m_Degradation = double.Parse(textBox_Degradation.Text);

                textBox_Ladezustand.Text = row["Ladezustand"].ToString();
                model.m_Ladezustand = double.Parse(textBox_Ladezustand.Text);

                textBox_Modulkosten.Text = row["Modulkosten"].ToString();
                model.m_Modulkosten = double.Parse(textBox_Modulkosten.Text);

                textBox_Bezeichner.Text = row["Bezeichner"].ToString();
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

            if (frm.ShowDialog() == DialogResult.OK)
            {
                m_Neu = true;
                textBox_Bezeichner.Text = frm.m_szName;
                textBox_Typ.Text = "Lithium-Ionen";
                textBox_Degradation.Text = "0";
                textBox_Ladezustand.Text = "0";
                textBox_Modulkosten.Text = "0";
                textBox_Leistung.Text = "0";
                textBox_Energie.Text = "0";
            }
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
            textBox_Modulkosten.Text = "";
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

            DialogResult confirmResult = MessageBox.Show(
                $"Möchten Sie den Stromspeicher '{textBox_Bezeichner.Text}' wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButtons.YesNo
            );

            if (confirmResult == DialogResult.No) return;

            try
            {
                StromspeicherStammCtrl sctrl = new StromspeicherStammCtrl();
                if (!sctrl.Delete(textBox_Bezeichner.Text)) return;

                string geloeschterText = textBox_Bezeichner.Text;
                InitControls();

                listBox_Stromspeicher.Items.Remove(geloeschterText);

                if (listBox_Stromspeicher.Items.Count > 0)
                {
                    listBox_Stromspeicher.SelectedIndex = listBox_Stromspeicher.Items.Count - 1;
                }
            }
            catch (Exception ex)
            {
                // Fehler beim Datenbankzugriff abfangen (z.B. Fremdschlüssel-Einschränkungen)
                MessageBox.Show("Stromspeicher kann nicht gelöscht werden.\nEs besteht eine Projektzuordnung!");
                Console.WriteLine("Fehler beim Löschen des Stromspeichers: " + ex.Message);
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

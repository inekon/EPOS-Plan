using ScottPlot.Colormaps;
using System;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
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
            }

            BrennstoffCtrl ctrl = new BrennstoffCtrl();
            comboBox_Brennstoff.DataSource = ctrl.Brennstoffart;
        }

        public void SetControls(string szName, string szBeschreibung)
        {
            RecordSet rs = new RecordSet();

            textBox_Name.Text = szName;
            m_szKessel = szName;  
            textBox_Beschreibung.Text = szBeschreibung;
            
            rs.Open("select * from [Tab_Heizkessel] where Name='" + szName + "'");
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
                comboBox_Brennstoff.SelectedIndex = brennstoff >= 1 ? brennstoff - 1 : 1;
            }
            rs.Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            BrennstoffModel model = new BrennstoffModel();
            BrennstoffCtrl ctrl = new BrennstoffCtrl();

            try
            {
                InitDatensatzUpdate(ctrl);
      
                if (ctrl.Update())
                {
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
                }
                DialogResult = DialogResult.OK; 
                Close();
            }
            catch
            {
                MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
            }
        }

        public bool Insert(BrennstoffModel model)
        {

            // Erst prüfen, ob die ID oder der Name bereits existiert (optional, je nach DB-Design)
            string checkSql = "SELECT COUNT(*) FROM [Tab_Heizkessel] WHERE Name = ?";
            DataTable dt = DataRepository.GetDataTable(checkSql, new OleDbParameter[] { new OleDbParameter("@n", model.Name) });
            if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) > 0) return false;

            string sql = @"INSERT INTO [Tab_Heizkessel] 
                   (Name, Beschreibung, Firma, Ptherm, Brennstoff, Wirkungsgrad_Gas, Wirkungsgrad_Öl, 
                    Investitionskosten, Raumbedarf, Wartungskosten, Nutzungsdauer, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust) 
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@nam", model.Name),
                new OleDbParameter("@bes", model.Beschreibung),
                new OleDbParameter("@fir", model.Firma),
                new OleDbParameter("@pth", model.Ptherm),
                new OleDbParameter("@bre", model.Brennstoff),
                new OleDbParameter("@wgg", model.Wirkungsgrad_Gas),
                new OleDbParameter("@wgo", model.Wirkungsgrad_Oel),
                new OleDbParameter("@inv", model.Investitionskosten),
                new OleDbParameter("@rau", model.Raumbedarf),
                new OleDbParameter("@war", model.Wartungskosten),
                new OleDbParameter("@nut", model.Nutzungsdauer),
                new OleDbParameter("@co2", model.CO2),
                new OleDbParameter("@so2", model.SO2),
                new OleDbParameter("@nox", model.NOx),
                new OleDbParameter("@co", model.CO),
                new OleDbParameter("@sta", model.Staub),
                new OleDbParameter("@bbv", model.Betriebsbereitschaftverlust)
            };

            return DataRepository.ExecuteSQL(sql, ps);
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
            // Wir holen uns den Namen aus der Liste der BrennstoffCtrl
            string name = comboBox_Brennstoff.Text;

            // Logik für CO2-Werte basierend auf dem Namen
            if (name.ToUpper().Contains("ÖL"))
            {
                tb_CO2.Text = "290880";
            }
            else if (name.ToUpper().Contains("GAS") && !name.Contains("Flüssiggas"))
            {
                tb_CO2.Text = "201600";
            }
            else if (name.Contains("Flüssiggas"))
            {
                tb_CO2.Text = "238680";
            }
            else tb_CO2.Text = "0";
        }

        BrennstoffModel InitDatensatzUpdate(BrennstoffCtrl model = null)
        {
            if(model == null) model = new BrennstoffCtrl();

            // Strings sind unkritisch, wir nutzen aber .Trim() gegen versehentliche Leerzeichen
            model.Name = textBox_Name.Text.Trim();
            model.Firma = textBox_Hersteller.Text.Trim();
            model.Beschreibung = textBox_Beschreibung.Text.Trim();

            // Zahlen sicher konvertieren
            model.Ptherm = SafeParse(tb_th_Leistung.Text);

            // Brennstoff: Sicherstellen, dass ein gültiger Index gewählt wurde
            // Falls nichts gewählt ist (-1), wird hier die ID 1 gesetzt
            model.Brennstoff = comboBox_Brennstoff.SelectedIndex >= 0
                               ? comboBox_Brennstoff.SelectedIndex + 1
                               : 1;

            model.Wirkungsgrad_Gas = SafeParse(tb_Wirkungsgrad.Text);
            model.Wirkungsgrad_Oel = SafeParse(tb_Wirkungsgrad_Öl.Text);
            model.Betriebsbereitschaftverlust = SafeParse(tb_B_Verlust.Text);
            model.Investitionskosten = SafeParse(tb_Investitionskosten.Text);
            model.Nutzungsdauer = SafeParse(tb_Nutzungsdauer.Text);
            model.Raumbedarf = SafeParse(tb_Raumbedarf.Text);
            model.NOx = SafeParse(tb_NOx.Text);
            model.CO2 = SafeParse(tb_CO2.Text);
            model.CO = SafeParse(tb_CO.Text);
            model.SO2 = SafeParse(tb_SO2.Text);
            model.Staub = SafeParse(tb_Staub.Text);

            return model;
        }
        
        private double SafeParse(string text)
        {
            // Entfernt Leerzeichen und ersetzt Punkt durch Komma (je nach Ländereinstellung)
            if (double.TryParse(text.Replace('.', ','), out double result))
            {
                return result;
            }
            return 0.0; // Standardwert, falls die Eingabe ungültig ist
        }
        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                BrennstoffModel model = new BrennstoffModel();

                // Zuerst das Model mit den UI-Daten füllen
                model = InitDatensatzUpdate();

                // Den neuen Namen aus dem Dialog setzen
                model.Name = frmLabel.m_szName;

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    textBox_Name.Text = frmLabel.m_szName;
                    m_szKessel = frmLabel.m_szName;

                    MessageBox.Show("Datensatz erfolgreich neu angelegt.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Fehler: Name existiert bereits oder Datenbankfehler!");
                }
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            try
            {
                BrennstoffModel model = new BrennstoffModel();
                model = InitDatensatzUpdate();

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    MessageBox.Show("Datensatz gespeichert");
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                    this.DialogResult = DialogResult.Cancel;
                }
                Close();
            }
            catch
            {
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
            }
        }
    }
}
using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_Bearbeiten : Form
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szPufferSp = "";
        private int m_mode = MODE_EDIT;

        public Form_PufferSp_Bearbeiten(int mode)
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
                textBox_Volumen.Text = "0";
            }
        }

        public void SetControls(string szName)
        {
            textBox_Name.Text = szName;
            m_szPufferSp = szName;

            // 1. Daten über das DataRepository mittels DataTable abfragen (Ersetzt RecordSet)
            string sql = "SELECT * FROM Tab_Pufferspeicher_STAMM WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("?", szName ?? (object)DBNull.Value));

            if (dt == null || dt.Rows.Count == 0) return;

            DataRow row = dt.Rows[0];

            // Zuordnung ueber Spaltennamen statt ueber Ordinalzahlen. Die frueheren
            // row[2]..row[6] waren an die aktuelle Spaltenreihenfolge von
            // Tab_Pufferspeicher_STAMM gebunden - die ist kein Vertrag. Die
            // SchemaMigration haengt neue Spalten zwar immer hinten an, aber ein
            // Tabellenumbau (Import aus einer Vorlage, "Komprimieren und reparieren"
            // nach manuellen Aenderungen) verschoebe die Zuordnung stillschweigend.
            SetzeText(textBox_Hersteller, row, "Hersteller");
            SetzeText(comboBox_Speichertyp, row, "Speichertyp");
            SetzeText(textBox_Volumen, row, "Gesamtvolumen");
            SetzeZahl(textBox_Verluste, row, "Bereitschaftsverluste");
            SetzeZahl(textBox_Investitionskosten, row, "Investitionskosten");
        }

        /// <summary>Uebernimmt einen Textwert, wenn Spalte und Wert vorhanden sind.</summary>
        private static void SetzeText(Control ziel, DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return;
            object v = row[spalte];
            if (v == DBNull.Value) return;
            ziel.Text = v.ToString();
        }

        /// <summary>Uebernimmt einen Zahlenwert mit zwei Nachkommastellen (wie bisher).</summary>
        private static void SetzeZahl(Control ziel, DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return;
            object v = row[spalte];
            if (v == DBNull.Value) return;
            try { ziel.Text = Convert.ToDouble(v).ToString("F2"); }
            catch { /* unerwarteter Typ - Vorbelegung stehen lassen */ }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                if (string.IsNullOrEmpty(frmLabel.m_szName))
                {
                    MessageBox.Show("Bitte einen gültigen Bezeichner eingeben!");
                    return;
                }

                try
                {
                    PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                    if (ctrl.Exists(frmLabel.m_szName)) { MessageBox.Show("Name existiert bereits!"); return; }

                    textBox_Name.Text = frmLabel.m_szName;
                    m_szPufferSp = frmLabel.m_szName;

                    PufferSpModel m = InitDatensatzUpdate();
                    m.Name = frmLabel.m_szName;

                    if (ctrl.InsertFrom(m))
                    {
                        this.DialogResult = DialogResult.OK;
                        MessageBox.Show("Datensatz gespeichert");
                    }
                    else
                    {
                        this.DialogResult = DialogResult.Cancel;
                        MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                    }
                    Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler bei Speichern Unter: " + ex.Message);
                    MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
                }
            }
        }

        PufferSpModel InitDatensatzUpdate()
        {
            PufferSpModel model = new PufferSpModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Hersteller.Text;
            model.Speichertyp = comboBox_Speichertyp.Text;

            int volumen;
            model.Gesamtvolumen = Int32.TryParse(textBox_Volumen.Text, out volumen) ? volumen : 0;

            double verluste;
            model.Betriebsbereitschaftverlust = double.TryParse(textBox_Verluste.Text, out verluste) ? verluste : 0.0;

            double kosten;
            model.Investitionskosten = double.TryParse(textBox_Investitionskosten.Text, out kosten) ? kosten : 0.0;

            return model;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            try
            {
                PufferSpModel m = InitDatensatzUpdate();
                PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                if (ctrl.Exists(m.Name)) { MessageBox.Show("Name existiert bereits!"); return; }

                if (ctrl.InsertFrom(m))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
            }
        }

        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            try
            {
                PufferSpModel m = InitDatensatzUpdate();
                PufferSpStammCtrl ctrl = new PufferSpStammCtrl();
                if (ctrl.UpdateFrom(m))
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                }
                Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Überschreiben: " + ex.Message);
                MessageBox.Show("Ein Fehler ist aufgetreten: " + ex.Message);
            }
        }
    }
}
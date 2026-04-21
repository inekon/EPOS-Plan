using Mscc.GenerativeAI.Types;
using System;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_ScriptGenerator : Form
    {
        public Form_ScriptGenerator()
        {
            InitializeComponent();
            SetupComboBox();
        }

        private void SetupComboBox()
        {
            cmbAktion.Items.Add("Tabelle anlegen");
            cmbAktion.Items.Add("Spalte umbenennen");
            cmbAktion.Items.Add("Datentyp ändern");
            cmbAktion.Items.Add("Tabelle umbenennen");
            cmbAktion.SelectedIndex = 0;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            string tabelle = txtTabelle.Text.Trim();
            string alt = txtFeldAlt.Text.Trim();
            string neu = txtFeldNeu.Text.Trim();
            string typ = txtDatentyp.Text.Trim();

            if (string.IsNullOrEmpty(tabelle) && cmbAktion.Text != "Tabelle umbenennen")
            {
                MessageBox.Show("Bitte Tabellennamen angeben.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"// --- Script vom {DateTime.Now:dd.MM.yyyy HH:mm} ---");

            switch (cmbAktion.Text)
            {
                case "Tabelle anlegen":
                    // Typ enthält hier die Felddefinition, z.B. ID AUTOINCREMENT PRIMARY KEY, Name TEXT(255)
                    sb.AppendLine($"SQL=CREATE TABLE [{tabelle}] ({typ})");
                    break;

                case "Spalte umbenennen":
                    sb.AppendLine($"SQL=ALTER TABLE [{tabelle}] ADD COLUMN [{neu}] {typ}");
                    sb.AppendLine($"SQL=UPDATE [{tabelle}] SET [{neu}] = [{alt}]");
                    sb.AppendLine($"BACKUP_REL:{alt}");
                    sb.AppendLine($"CLEAN_COL:{alt}");
                    sb.AppendLine($"SQL=ALTER TABLE [{tabelle}] DROP COLUMN [{alt}]");
                    sb.AppendLine($"RESTORE_REL:{neu}");
                    break;

                case "Datentyp ändern":
                    sb.AppendLine($"// Ändere Typ von {alt} auf {typ}");
                    sb.AppendLine($"SQL=ALTER TABLE [{tabelle}] ADD COLUMN [{alt}_new] {typ}");
                    sb.AppendLine($"SQL=UPDATE [{tabelle}] SET [{alt}_new] = [{alt}]");
                    sb.AppendLine($"BACKUP_REL:{alt}");
                    sb.AppendLine($"CLEAN_COL:{alt}");
                    sb.AppendLine($"SQL=ALTER TABLE [{tabelle}] DROP COLUMN [{alt}]");
                    sb.AppendLine($"SQL=ALTER TABLE [{tabelle}] ADD COLUMN [{alt}] {typ}");
                    sb.AppendLine($"SQL=UPDATE [{tabelle}] SET [{alt}] = [{alt}_new]");
                    sb.AppendLine($"SQL=ALTER TABLE [{tabelle}] DROP COLUMN [{alt}_new]");
                    sb.AppendLine($"RESTORE_REL:{alt}");
                    break;

                case "Tabelle umbenennen":
                    sb.AppendLine($"SQL=SELECT * INTO [{neu}] FROM [{tabelle}]");
                    sb.AppendLine($"SQL=DROP TABLE [{tabelle}]");
                    sb.AppendLine("// Hinweis: Beziehungen müssen für die neue Tabelle manuell geprüft werden!");
                    break;
            }

            rtbOutput.Text = sb.ToString();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(rtbOutput.Text))
                Clipboard.SetText(rtbOutput.Text);
        }
    }
}
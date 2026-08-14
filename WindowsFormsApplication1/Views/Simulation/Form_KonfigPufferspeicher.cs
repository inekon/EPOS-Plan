using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_KonfigPufferspeicher : Form
    {
        public Z_ProjektPufferSpModel model = new Z_ProjektPufferSpModel();
        public List<string> listErzeuger = new List<String>();
        public List<string> listPufferSp = new List<String>();
        public int m_ID_Projekt = 0;    

        public Form_KonfigPufferspeicher()
        {
            InitializeComponent();
        }

        public void SetControls()
        {
            comboBox_Erzeuger.Items.AddRange(listErzeuger.ToArray());
            comboBox_Puffer.Items.AddRange(listPufferSp.ToArray());
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; 
            Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            model.Erzeuger = comboBox_Erzeuger.Text;
            model.PufferSp = comboBox_Puffer.Text;
            model.Vorlauf = string.IsNullOrWhiteSpace(textBox_Vorlauf.Text) ? 0 : Int32.Parse(textBox_Vorlauf.Text);
            model.Ruecklauf = string.IsNullOrWhiteSpace(textBox_Ruecklauf.Text) ? 0 : Int32.Parse(textBox_Ruecklauf.Text);
            this.DialogResult = DialogResult.OK;
        }

        private void comboBox_Puffer_SelectedIndexChanged(object sender, EventArgs e)
        {
            // B0-9: Die Abfragen erwarten den deutschen DB-Bezeichner, die ComboBox
            // trägt den lokalisierten Anzeigenamen — in englischer Oberfläche blieb
            // das Ergebnis leer. Außerdem parametrisiert statt String-Konkatenation.
            // Direkt formuliert statt über die gespeicherten Access-Abfragen: Die
            // Definitionen von Abfrage_Erzeuger_Vor-/Ruecklauftemperaturen enden auf
            // ein hartkodiertes "HAVING ID_Projekt=8" und liefern damit für JEDES
            // Projekt 0 Zeilen — die Vorbelegung war unabhängig von der Sprache tot.
            string typ = ErzeugerDbWert(comboBox_Erzeuger.Text);

            DataTable dtV = DataRepository.GetDataTable(
                "SELECT Min(e.Vorlauf) AS Vorlauf " +
                "FROM Tab_Energieanlagen AS e INNER JOIN Tab_Typ_Energieanlagen AS t ON t.ID = e.ID_Type " +
                "WHERE e.ID_Projekt = ? AND t.Bezeichner = ?",
                new OleDbParameter("@idProj", m_ID_Projekt), new OleDbParameter("@typ", typ));
            if (dtV != null && dtV.Rows.Count > 0 && dtV.Columns.Contains("Vorlauf")
                && dtV.Rows[0]["Vorlauf"] != DBNull.Value)
                textBox_Vorlauf.Text = dtV.Rows[0]["Vorlauf"].ToString();

            DataTable dtR = DataRepository.GetDataTable(
                "SELECT Max(e.[Rücklauf]) AS Ruecklauf " +
                "FROM Tab_Energieanlagen AS e INNER JOIN Tab_Typ_Energieanlagen AS t ON t.ID = e.ID_Type " +
                "WHERE e.ID_Projekt = ? AND t.Bezeichner = ?",
                new OleDbParameter("@idProj", m_ID_Projekt), new OleDbParameter("@typ", typ));
            if (dtR != null && dtR.Rows.Count > 0 && dtR.Columns.Contains("Ruecklauf")
                && dtR.Rows[0]["Ruecklauf"] != DBNull.Value)
                textBox_Ruecklauf.Text = dtR.Rows[0]["Ruecklauf"].ToString();
        }

        // B0-9: Übersetzung Anzeigename -> DB-Bezeichner (Persistenzwerte bleiben
        // deutsch — Drei-Schichten-Regel, Konzept Quellen/Senken Kap. 13.6).
        // Unbekannte Werte laufen unverändert durch (deutsche Oberfläche).
        private static string ErzeugerDbWert(string anzeige)
        {
            if (anzeige == MyResource.Resource.KONFIG_BHKW) return "BHKW";
            if (anzeige == MyResource.Resource.KONFIG_HEIZKESSEL) return "Heizkessel";
            if (anzeige == MyResource.Resource.KONFIG_SOLARTHERMIE) return "Solarthermie";
            if (anzeige == MyResource.Resource.KONFIG_WAERMEPUMPE) return "Wärmepumpe";
            if (anzeige == MyResource.Resource.KONFIG_GESAMTSYSTEM) return "Gesamtsystem";
            return anzeige;
        }
    }
}

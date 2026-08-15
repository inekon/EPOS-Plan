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
            // Etappe 4: einheitliche Prüfung der Betriebstemperaturen.
            //
            // Vorher stand hier Int32.Parse — eine Eingabe wie "35 °C" oder "dreißig"
            // riss den Dialog mit einer unbehandelten FormatException ab (Konzept 4.6),
            // und ein leeres Feld wurde stillschweigend zu 0, was am Speicher eine
            // Spreizung ohne Bedeutung ergab.
            //
            // Die Regeln stehen in ProjektPuffer.TemperaturenPruefen und kennen
            // BEWUSST keine Untergrenze außer "Rücklauf > 0": Niedertemperatursysteme
            // (Flächenheizung, 35/28 und tiefer) müssen hier durchgehen.
            int vorlauf, ruecklauf;
            string fehler;
            if (!ProjektPuffer.TemperaturenPruefen(textBox_Vorlauf.Text, textBox_Ruecklauf.Text,
                                                   out vorlauf, out ruecklauf, out fehler))
            {
                MessageBox.Show(fehler, MyResource.Resource.PSP_TITEL_ZUORDNUNG,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Dialog offen lassen, damit die Eingabe korrigiert werden kann.
                this.DialogResult = DialogResult.None;
                return;
            }

            model.Erzeuger = comboBox_Erzeuger.Text;
            model.PufferSp = comboBox_Puffer.Text;
            model.Vorlauf = vorlauf;
            model.Ruecklauf = ruecklauf;
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
            string typ = ErzeugerKatalog.DbWert(comboBox_Erzeuger.Text);

            // Etappe 4: "> 0" ergaenzt. Tab_Energieanlagen.Vorlauf/[Rücklauf] tragen den
            // Access-Spaltendefault 0 und sind nie NULL - eine einzige unvollstaendig
            // erfasste Anlage zog die Vorbelegung bisher auf 0. Das ist KEINE
            // Temperatur-Untergrenze, sondern der Test auf "gepflegt"; dieselbe Regel
            // wie in ProjektPuffer.SQL_SYSTEM_VORLAUF.
            DataTable dtV = DataRepository.GetDataTable(
                "SELECT Min(e.Vorlauf) AS Vorlauf " +
                "FROM Tab_Energieanlagen AS e INNER JOIN Tab_Typ_Energieanlagen AS t ON t.ID = e.ID_Type " +
                "WHERE e.ID_Projekt = ? AND t.Bezeichner = ? AND e.Vorlauf > 0",
                new OleDbParameter("@idProj", m_ID_Projekt), new OleDbParameter("@typ", typ));
            if (dtV != null && dtV.Rows.Count > 0 && dtV.Columns.Contains("Vorlauf")
                && dtV.Rows[0]["Vorlauf"] != DBNull.Value)
                textBox_Vorlauf.Text = dtV.Rows[0]["Vorlauf"].ToString();

            DataTable dtR = DataRepository.GetDataTable(
                "SELECT Max(e.[Rücklauf]) AS Ruecklauf " +
                "FROM Tab_Energieanlagen AS e INNER JOIN Tab_Typ_Energieanlagen AS t ON t.ID = e.ID_Type " +
                "WHERE e.ID_Projekt = ? AND t.Bezeichner = ? AND e.[Rücklauf] > 0",
                new OleDbParameter("@idProj", m_ID_Projekt), new OleDbParameter("@typ", typ));
            if (dtR != null && dtR.Rows.Count > 0 && dtR.Columns.Contains("Ruecklauf")
                && dtR.Rows[0]["Ruecklauf"] != DBNull.Value)
                textBox_Ruecklauf.Text = dtR.Rows[0]["Ruecklauf"].ToString();
        }

        // Die Übersetzung Anzeigename -> DB-Bezeichner (B0-9) stand hier bis Paket 9 / L7
        // als vierte, eigenständige Kopie im Quelltext. Sie liegt jetzt einmalig in
        // ErzeugerKatalog.DbWert (Paket 9 / L4) und wird von dort benutzt - dieselbe
        // Zuordnung, dieselbe tolerante Regel für unbekannte Werte, aber nur noch EINE
        // Wahrheit (Drei-Schichten-Regel, Konzept Quellen/Senken Kap. 13.6).
    }
}

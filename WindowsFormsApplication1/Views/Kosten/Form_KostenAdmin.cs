using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public partial class Form_KostenAdmin : Form
    {
        // Befund B4 (11.08.2026): zentraler DataRepository-Zugriff statt eigenem
        // Registry-Connection-String — eine Datenquelle für die ganze App.
        public Form_KostenAdmin()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            InitListView();
            LoadKostenfaktoren();
        }

        private void InitListView()
        {
            lvwKostenfaktoren.View = View.SmallIcon;
            lvwKostenfaktoren.Columns.Clear();
            lvwKostenfaktoren.HeaderStyle = ColumnHeaderStyle.None;
            lvwKostenfaktoren.Columns.Add("Bezeichnung");
        }

        private void LoadKostenfaktoren()
        {
            lvwKostenfaktoren.Items.Clear();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT StammID, Bezeichnung FROM Tab_Kostenfaktor " +
                "WHERE IsMainComponent = False ORDER BY Bezeichnung");
            if (dt != null)
                foreach (DataRow row in dt.Rows)
                    lvwKostenfaktoren.Items.Add(new ListViewItem(row["Bezeichnung"].ToString()));

            AnpassenSpaltenbreite();
        }

        // Button: Neuer Kostenfaktor
        private void btnNeuKostenfaktor_Click(object sender, EventArgs e)
        {
            Form_KostenItemNeu frmLabel = new Form_KostenItemNeu();

            System.Drawing.Point p1 = btnNeuKostenfaktor.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                string neueBezeichnung = (frmLabel.m_szName ?? "").Trim();
                if (neueBezeichnung.Length == 0) return;

                // Befund B4: das alte INSERT nutzte den Variablennamen als
                // SQL-Bezeichner (VALUES (neueBezeichnung)) und scheiterte immer.
                // Jetzt: Platzhalter + neue StammID + IsMainComponent = False.
                int stammId = DataRepository.GetMaxID("Tab_Kostenfaktor", "StammID") + 1;
                bool ok = DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) VALUES (?, ?, ?)",
                    new DbParam("@sid", stammId),
                    new DbParam("@bez", neueBezeichnung),
                    new DbParam("@main", DbParamTyp.Boolean) { Wert = false });
                if (!ok)
                    MessageBox.Show("Der Kostenfaktor konnte nicht angelegt werden.", "Fehler",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                LoadKostenfaktoren();
            }
        }

        private void AnpassenSpaltenbreite()
        {
            if (lvwKostenfaktoren.Items.Count == 0) return;

            int maxBreite = 0;

            // Wir nutzen die Schriftart der ListView
            Font font = lvwKostenfaktoren.Font;

            foreach (ListViewItem item in lvwKostenfaktoren.Items)
            {
                // Text ausmessen
                Size textSize = TextRenderer.MeasureText(item.Text, font);

                // Wir vergleichen die Breite und speichern das Maximum
                if (textSize.Width > maxBreite)
                {
                    maxBreite = textSize.Width;
                }
            }

            // WICHTIG: Ein kleiner Puffer für Icons oder Checkboxen (ca. 20-30 Pixel)
            // Damit der Text nicht direkt am Rand klebt oder "..." bekommt.
            int puffer = 30;

            if (lvwKostenfaktoren.Columns.Count > 0)
            {
                lvwKostenfaktoren.Columns[0].Width = maxBreite + puffer;
            }
        }

        private void btnDeleteKostenfaktor_Click(object sender, EventArgs e)
        {
            if (lvwKostenfaktoren.SelectedItems.Count == 0) return;
            string bezeichnung = lvwKostenfaktoren.SelectedItems[0].Text;

            if (MessageBox.Show("Kostenfaktor '" + bezeichnung + "' wirklich löschen?",
                    "Kostenfaktoren", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Befund B4: Platzhalter statt @-Parameter ohne Fragezeichen;
            // Hauptkomponenten sind über IsMainComponent geschützt.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Kostenfaktor WHERE Bezeichnung = ? AND IsMainComponent = False",
                new DbParam("@bez", bezeichnung));

            LoadKostenfaktoren();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
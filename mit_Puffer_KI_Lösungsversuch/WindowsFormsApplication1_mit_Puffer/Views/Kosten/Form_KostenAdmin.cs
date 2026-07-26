using Microsoft.Win32;
using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public partial class Form_KostenAdmin : Form
    {
        // Dein Connection String zu Access
        private string connString = "";

        public Form_KostenAdmin()
        {
            InitializeComponent();
            string dbPath = GetDBPath();
            connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
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
            string sql = $"SELECT StammID, Bezeichnung FROM Tab_Kostenfaktor where IsMainComponent=False ORDER BY Bezeichnung";
            DataTable dt = GetDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                ListViewItem item = new ListViewItem(row["Bezeichnung"].ToString());
                lvwKostenfaktoren.Items.Add(item);
            }

            AnpassenSpaltenbreite();
        }

        // Hilfsmethode für den Datenabruf
        private DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                OleDbDataAdapter adapter = new OleDbDataAdapter(sql, conn);
                adapter.Fill(dt);
            }
            return dt;
        }

        // Button: Neuer Kostenfaktor
        private void btnNeuKostenfaktor_Click(object sender, EventArgs e)
        {
            Form_KostenItemNeu frmLabel = new Form_KostenItemNeu();
            OleDbTransaction trans = null;

            System.Drawing.Point p1 = btnNeuKostenfaktor.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                string neueBezeichnung = frmLabel.m_szName;
                string insSql = @"INSERT INTO Tab_Kostenfaktor (Bezeichnung) Values (neueBezeichnung)";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // Transaktion starten
                    trans = conn.BeginTransaction();

                    using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                    {
           
                        insCmd.Parameters.AddWithValue("@Bezeichnung", neueBezeichnung);

                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }

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

        private string GetDBPath()
        {
            string db = "";
            string userPath = $@"SOFTWARE\ODBC\ODBC.INI\TEST";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(userPath))
            {
                if (key != null)
                {
                    db = key.GetValue("DBQ")?.ToString() ?? key.GetValue("Database")?.ToString();
                }
            }
            return db;
        }

        private void btnDeleteKostenfaktor_Click(object sender, EventArgs e)
        {
            OleDbTransaction trans = null;
            
            if (lvwKostenfaktoren.SelectedItems.Count > 0)
            {
                // Das erste (und einzige) selektierte Item holen
                ListViewItem selectedItem = lvwKostenfaktoren.SelectedItems[0];

                string bezeichnung = selectedItem.Text;

                string insSql = @"DELETE * FROM Tab_Kostenfaktor WHERE Bezeichnung = @Bezeichnung";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // Transaktion starten
                    trans = conn.BeginTransaction();

                    using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                    {
                        insCmd.Parameters.AddWithValue("@Bezeichnung", bezeichnung);
                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }

                LoadKostenfaktoren();
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
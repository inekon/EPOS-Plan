using Microsoft.Win32;
using System;
using System.Data;
using System.Data.OleDb;
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
            LoadKategorien();
        }

        private void InitListView()
        {
            lvwKostenfaktoren.View = View.Details;
            lvwKostenfaktoren.Columns.Clear();
            lvwKostenfaktoren.HeaderStyle = ColumnHeaderStyle.None;

            // Spalte 1: ID (versteckt oder sehr schmal)
            lvwKostenfaktoren.Columns.Add("ID", 0);

            // Spalte 2: Bezeichnung
            // -1 bedeutet, die Spalte passt sich automatisch der Breite an
            lvwKostenfaktoren.Columns.Add("Bezeichnung", 250);

            // Angenommen, Spalte 0 ist die ID (Breite 0)
            // und Spalte 1 ist die Bezeichnung
            lvwKostenfaktoren.Columns[0].Width = 0;

            // -2 sorgt dafür, dass die Spalte den verfügbaren Platz einnimmt
            lvwKostenfaktoren.Columns[1].Width = -2;
        }

        // 1. Kategorien laden (beim Start)
        private void LoadKategorien()
        {
            DataTable dt = GetDataTable("SELECT KategorieID, KategorieName FROM Tab_KostenKategorie ORDER BY KategorieName");
            lstKategorien.DisplayMember = "KategorieName";
            lstKategorien.ValueMember = "KategorieID";
            lstKategorien.DataSource = dt;
        }

        // 2. Event: Wenn eine Kategorie ausgewählt wird, lade die Kostenfaktoren rechts
        private void lstKategorien_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstKategorien.SelectedValue is int kategorieId)
            {
                LoadKostenfaktoren(kategorieId);
                lblKategorieTitel.Text = $"Kostenfaktoren für: {lstKategorien.Text}";
            }
        }

        private void LoadKostenfaktoren(int kategorieId)
        {
            lvwKostenfaktoren.Items.Clear();
            string sql = $"SELECT StammID, Bezeichnung FROM Tab_Kostenfaktor WHERE KategorieID = {kategorieId} and IsMainComponent=False ORDER BY Bezeichnung";
            DataTable dt = GetDataTable(sql);

            foreach (DataRow row in dt.Rows)
            {
                ListViewItem item = new ListViewItem(row["StammID"].ToString());
                item.SubItems.Add(row["Bezeichnung"].ToString());
                lvwKostenfaktoren.Items.Add(item);
            }
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

            if (lstKategorien.SelectedValue == null) return;

            int aktuelleKategorieID = (int)lstKategorien.SelectedValue;

            System.Drawing.Point p1 = btnNeuKostenfaktor.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {
                string neueBezeichnung = frmLabel.m_szName;
                string insSql = @"INSERT INTO Tab_Kostenfaktor (KategorieID, Bezeichnung) Values (aktuelleKategorieID, neueBezeichnung)";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // Transaktion starten
                    trans = conn.BeginTransaction();

                    using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                    {
                        insCmd.Parameters.AddWithValue("@KategorieID", aktuelleKategorieID);
                        insCmd.Parameters.AddWithValue("@Bezeichnung", neueBezeichnung);

                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }

                LoadKostenfaktoren(aktuelleKategorieID);
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

        private void btnNeuKategorie_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            OleDbTransaction trans = null;

            System.Drawing.Point p1 = btnNeuKostenfaktor.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
            {

                string neueBezeichnung = frmLabel.m_szName;
                string insSql = @"INSERT INTO Tab_KostenKategorie (KategorieName) Values (neueBezeichnung)";
  
                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // Transaktion starten
                    trans = conn.BeginTransaction();

                    using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                    {
                        insCmd.Parameters.AddWithValue("@KategorieName", neueBezeichnung);
                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }

                LoadKategorien();
            }
        }

        private void btnDeleteKostenfaktor_Click(object sender, EventArgs e)
        {
            OleDbTransaction trans = null;
            
            if (lstKategorien.SelectedValue == null) return;

            int aktuelleKategorieID = (int)lstKategorien.SelectedValue;

            if (lvwKostenfaktoren.SelectedItems.Count > 0)
            {
                // Das erste (und einzige) selektierte Item holen
                ListViewItem selectedItem = lvwKostenfaktoren.SelectedItems[0];

                // Den Text der ersten Spalte (meist die ID) auslesen
                string id = selectedItem.Text;

                // Den Text der zweiten Spalte (SubItem) auslesen
                string bezeichnung = selectedItem.SubItems[1].Text;

                string insSql = @"DELETE * FROM Tab_Kostenfaktor WHERE StammID = @StammID";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // Transaktion starten
                    trans = conn.BeginTransaction();

                    using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                    {
                        insCmd.Parameters.AddWithValue("@StammID", id);
                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }

                LoadKostenfaktoren(aktuelleKategorieID);
            }
        }

        private void btnDeleteKategorie_Click(object sender, EventArgs e)
        {
            OleDbTransaction trans = null;

            if (lstKategorien.SelectedValue == null) return;

            if (lstKategorien.SelectedItems.Count > 0)
            {
                // Das erste (und einzige) selektierte Item holen
                string selectedItem = lstKategorien.Text;
                string insSql = @"DELETE * FROM Tab_KostenKategorie WHERE KategorieName = @KategorieName";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // Transaktion starten
                    trans = conn.BeginTransaction();

                    using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                    {
                        insCmd.Parameters.AddWithValue("@KategorieName", selectedItem);
                        insCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }

                LoadKategorien();
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
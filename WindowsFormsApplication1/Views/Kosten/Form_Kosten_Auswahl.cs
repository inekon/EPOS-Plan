using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Kosten_Auswahl : Form
    {
        // Diese Eigenschaften liest das Hauptprogramm nach dem Schließen aus
        public string SelectedName => TextBox_Variante.Text;
        public int SelectedBrennstoffID => (int)cmbBrennstoffArt.SelectedValue;
        public string SelectedCode => cmbBrennstoffArt.Text;
        public string SelectedBrennstoffCode;

        // Hilfseigenschaften, um die Daten aus der DB-Abfrage zwischenzuspeichern
        public string SelectedGroupCode { get; private set; }
        public string SelectedBillingUnit { get; private set; }
        public double SelectedHi { get; private set; }
        public double SelectedHs { get; private set; }
        public int SelectedConvID { get; private set; }

        public bool bOhneVariante { get; set; } = false; // Wenn true, wird die Auswahl ohne Variante (Code) erlaubt    
        public string m_szBVrennstoff { get; set; } = ""; // Optional: Vorgabe für die Brennstoffart (Bezeichner) in der ComboBox

        public Form_Kosten_Auswahl()
        {
            InitializeComponent();
            LoadBrennstoffArten();
        }

        private void LoadBrennstoffArten()
        {
            // Lädt die Namen aus Tab_Brennstoff_Stamm in die ComboBox
            string sql = "SELECT ID, Bezeichner FROM Tab_Brennstoff_Stamm ORDER BY Bezeichner";
            cmbBrennstoffArt.DataSource = DataRepository.GetDataTable(sql);
            cmbBrennstoffArt.DisplayMember = "Bezeichner";
            cmbBrennstoffArt.ValueMember = "ID";
            TextBox_Variante.Text = "";
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox_Variante.Text))
            {
                MessageBox.Show("Bitte einen Variantennamen (Code) eingeben.");
                return;
            }

            // Wir holen uns die Zusatzinfos (GroupCode, Unit) bevor der Dialog schließt
            FetchAdditionalData();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FetchAdditionalData()
        {
            // JOIN über Stamm -> Kategorien um group_code und billing_unit zu erhalten
            string sql = @"SELECT k.Gruppe, k.Code, s.Hi, s.Hs, s.Einheit 
                       FROM Tab_Brennstoff_Stamm s
                       INNER JOIN Tab_BrennstoffKategorien k ON s.ID_Kategorie = k.ID
                       WHERE s.ID = ?";

            var tb = DataRepository.GetDataTable(sql, new OleDbParameter[] {
                new OleDbParameter("@id", SelectedBrennstoffID)
            });
            var row = tb.Rows.Count > 0 ? tb.Rows[0] : null;
            if (row != null)
            {
                SelectedGroupCode = row["Gruppe"].ToString();
                SelectedBillingUnit = row["Einheit"].ToString();
                SelectedBrennstoffCode = row["Code"].ToString();
                SelectedHi = (double)row["Hi"];
                SelectedHs = (double)row["Hs"];
            }

            SelectedConvID = SelectedConvID = GetConvID(new EnergyConversion
            {
                IDBrennstoff = SelectedBrennstoffID,
                FromUnit = SelectedBillingUnit,
                ToUnitCode = SelectedBillingUnit
            });
        }

        public int GetConvID(object selectedItem)
        {
            if (selectedItem is EnergyConversion conv)
            {
                string sql = "SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? AND to_unit = ?";
                OleDbParameter[] ps = {
                    new OleDbParameter("@cid", conv.IDBrennstoff),
                    new OleDbParameter("@fu", conv.FromUnit),
                    new OleDbParameter("@tu", conv.ToUnitCode)
                };
                DataTable dt = DataRepository.GetDataTable(sql, ps);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return Convert.ToInt32(dt.Rows[0]["ID"]);
                }
            }
            return -1; // Fehlerfall
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void cmbBrennstoffArt_SelectedIndexChanged(object sender, EventArgs e)
        {
            TextBox_Variante.Text = cmbBrennstoffArt.Text;
        }

        private void Form_Kosten_Auswahl_Load(object sender, EventArgs e)
        {
            if (bOhneVariante)
            {
                TextBox_Variante.Visible = false;
                label_Variante.Visible = false;
                cmbBrennstoffArt.Text = m_szBVrennstoff;
            }
        }
    }
}

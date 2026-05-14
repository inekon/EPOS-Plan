using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class ucFuelSettings : UserControl
    {
        private int _projectId;
        private EnergyCarrier _carrier;
        private List<EnergyConversion> _conversions;
        private double _basePrice; // Speichert immer den Preis pro Liter
        private double _baseHi;    // Speichert immer den Heizwert pro Liter
        private double _baseHs;    // Speichert immer den Brennwert pro Liter
        private double _baseGroundPrice;
        private int id_conversion; // Speichert die ID der aktuell ausgewählten Umrechnung

        public ucFuelSettings(int projectId, EnergyCarrier carrier)
        {
            InitializeComponent();
            
            _projectId = projectId;
            _carrier = carrier;

            // Events abonnieren für Live-Berechnung
            numArbeitspreis.ValueChanged += (s, e) => UpdatePricePerKWh();
            numHeizwert.ValueChanged += (s, e) => UpdatePricePerKWh();
            numBrennwert.ValueChanged += (s, e) => UpdatePricePerKWh();
            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            numArbeitspreis.Maximum = 1000000;
            numHeizwert.Maximum = 1000000;
            numBrennwert.Maximum = 1000000;
            numGrundpreis.Maximum = 1000000;

            if(carrier.PricingModel == "ELECTRICITY")
            {
                lbl_Unit_Arbeitspreis.Text = "€ / kWh";
                lbl_Unit_Heizwert.Text = "kWh / kWh";
                lbl_Unit_Brennwert.Text = "kWh / kWh";
                numHeizwert.Visible = false; // Heizwert bei Strom nicht relevant
                numBrennwert.Visible = false; // Brennwert bei Strom nicht relevant
                lb1_Brennwert .Visible = false;
                lbl_Heizwert .Visible = false;
                lbl_Unit_Brennwert.Visible = false; 
                lbl_Unit_Heizwert.Visible = false;
                cmbUnit.Enabled = false; // Keine Einheiten-Auswahl bei Strom
                groupBox_Formel.Visible = false; // Formel-Box ausblenden, da sie bei Strom keinen Sinn ergibt
            }
            else if (carrier.PricingModel == "LIQUID_FUEL" || carrier.PricingModel == "LIQUID_FUEL" || 
                carrier.PricingModel == "SOLID_FUEL" || carrier.PricingModel == "ANIMAL_FAT")
            {
                numBrennwert.Visible = false; // Heizwert bei Strom nicht relevant
                lb1_Brennwert.Visible = false;
                lbl_Unit_Brennwert.Visible = false;
            }

            LoadData();
        }

        private void LoadData()
        {
            lblCarrierName.Text = $"{_carrier.Name}  (VDI 3805 {_carrier.Code})";
            lblGruppe.Text = $"Gruppe: {_carrier.GroupCode}";   

            // Alle verfügbaren Einheiten/Konvertierungen für diesen Energieträger laden
            _conversions = GetConversions(_carrier.ID_Brennstoff);

            cmbUnit.SelectedIndexChanged -= CmbUnit_SelectedIndexChanged;
            cmbUnit.DataSource = _conversions;
            cmbUnit.DisplayMember = "ToUnitCode"; // z.B. "Liter (L)"
            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            // Suche die Einheit, die der billing_unit entspricht
            var baseUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == _carrier.BillingUnit);
            if (baseUnit != null) lblBasisnheit.Text = $"{_carrier.BillingUnit}";

            // Projektspezifische Daten aus ENERGY_PROJECT_SETTINGS laden
            var projectSettings = GetProjectPrice(_projectId, _carrier.ID);

            if (projectSettings != null)
            {
                numArbeitspreis.Value = (decimal)projectSettings.ArbeitspreisEurUnit;
                numGrundpreis.Value = (decimal)projectSettings.GrundpreisEurYear;
                numHeizwert.Value = (decimal)(projectSettings.CustomHi ?? _carrier.HiKwhPerUnit);
                numBrennwert.Value = (decimal)(projectSettings.CustomHs ?? _carrier.HsKwhPerUnit);

                _basePrice = projectSettings.ArbeitspreisEurUnit;
                _baseHi = projectSettings.CustomHi ?? _carrier.HiKwhPerUnit;
                _baseHs = projectSettings.CustomHs ?? _carrier.HsKwhPerUnit;
                _baseGroundPrice = projectSettings.GrundpreisEurYear;

                // Korrekte Einheit in Combo auswählen
                string project_conversion = GetTargetUnitByConversionId(projectSettings.IDUmrechnung);
                var selectedUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == project_conversion);//projectSettings.ArbeitspreisUnit);
                
                if (selectedUnit != null)
                {
                    cmbUnit.SelectedItem = selectedUnit;
                    lbl_Unit_Arbeitspreis.Text = $"€/{selectedUnit.ToUnitCode}";
                    lbl_Unit_Heizwert.Text = $"kWh/{selectedUnit.ToUnitCode}";
                    lbl_Unit_Brennwert.Text = $"kWh/{selectedUnit.ToUnitCode}";
                    CmbUnit_SelectedIndexChanged(cmbUnit, EventArgs.Empty);
                }
            }
            else
            {
                // Fallback auf Stammdaten aus ENERGY_CARRIER
                numArbeitspreis.Value = 0;
                numGrundpreis.Value = 0;
                numHeizwert.Value = (decimal)_carrier.HiKwhPerUnit;
                numBrennwert.Value = (decimal)_carrier.HsKwhPerUnit;

                _basePrice = 0;
                _baseGroundPrice = 0;
                _baseHi = _carrier.HiKwhPerUnit;
                _baseHs = _carrier.HsKwhPerUnit;

                cmbUnit.SelectedItem = baseUnit;
                CmbUnit_SelectedIndexChanged(cmbUnit, EventArgs.Empty);
            }
            
            UpdatePricePerKWh();
            SetupHistoryGrid();
            LoadHistory(_carrier.ID, _projectId);
        }

        public static List<EnergyConversion> GetConversions(int Idbrennstoff)
        {
            List<EnergyConversion> list = new List<EnergyConversion>();
            string sql = "SELECT id_brennstoff, from_unit, to_unit, factor FROM ENERGY_CONVERSION WHERE id_brennstoff = ?";

            OleDbParameter[] ps = { new OleDbParameter("@id", Idbrennstoff) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new EnergyConversion
                {
                    IDBrennstoff = Convert.ToInt32(row["Id_brennstoff"]),
                    FromUnit = row["from_unit"].ToString(),
                    ToUnitCode = row["to_unit"].ToString(),
                    Factor = Convert.ToDouble(row["factor"])
                });
            }
            return list;
        }

        public static dynamic GetProjectPrice(int projectId, int carrierId)
        {
            // Wir suchen in der Preis-Tabelle nach dem Projektbezug
            string sql = "SELECT * FROM ENERGY_PROJECT_SETTINGS WHERE ID_PROJEKT = ? AND id_ENERGIETRÄGER  = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@p", projectId),
                new OleDbParameter("@c", carrierId)
            };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new
                {
                    ArbeitspreisEurUnit = row["custom_price_work"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_work"]) : null,
                    ArbeitspreisUnit = "",
                    GrundpreisEurYear = row["custom_price_base"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_base"]) : null,
                    // Falls es Custom-Heizwerte in der Preis-Tabelle gibt:
                    CustomHi = row["custom_hi"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_hi"]) : null,
                    CustomHs = row["custom_hs"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_hs"]) : null,
                    IDUmrechnung = row["ID_Umrechnung"] != DBNull.Value ? (int?)Convert.ToInt32(row["ID_Umrechnung"]) : null
                };
            }

            return null; // Nichts gefunden -> UserControl nutzt Stammdaten
        }

        public string GetTargetUnitByConversionId(int idumrechnung)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select to_unit from energy_conversion where id=" + idumrechnung);
            rs.Next();
            string unit= (string)rs.Read("to_Unit");
            rs.Close();
            return unit;

        }
        /*
        public string GetTargetUnitByConversionId(int idproject, int idcarrier)
        {
            // SQL: Verknüpfung der ID aus project_setting mit der Tabelle energy_conversion
            // Wir wollen das Feld 'to_unit' (z.B. "m³" oder "kg") erhalten.
            string sql = @"SELECT conv.to_unit 
                   FROM energy_Project_settings AS proj
                   INNER JOIN energy_conversion AS conv ON proj.ID_Umrechnung = conv.ID
                   WHERE proj.ID_Projekt = ? AND proj.ID_Energieträger = ?";
            try
            {
                // Ausführung der Abfrage. ExecuteScalar gibt den Wert der ersten Spalte zurück.
                object result = DataRepository.ExecuteScalar(sql, new OleDbParameter[] {
                    new OleDbParameter("@id", idproject),
                    new OleDbParameter("@id", idcarrier),
                });

                // Rückgabe des Wertes als String oder leerer String, falls nichts gefunden wurde
                return result != null ? result.ToString() : string.Empty;
            }
            catch (Exception ex)
            {
                // Fehlerlogging (optional)
                Console.WriteLine("Fehler beim Abrufen der Einheit: " + ex.Message);
                return string.Empty;
            }
        }*/

        private void CmbUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                // Event-Handler kurzzeitig stumm schalten, um kein Dauerfeuer auszulösen
                numArbeitspreis.ValueChanged -= Arbeitspreis_Changed;

                // IMMER vom Anker aus rechnen!
                numArbeitspreis.Value = (decimal)(_basePrice / conv.Factor);
                numHeizwert.Value = (decimal)(_baseHi / conv.Factor);
                numBrennwert.Value = (decimal)(_baseHs / conv.Factor);

                numArbeitspreis.ValueChanged += Arbeitspreis_Changed;

                UpdatePricePerKWh();

                id_conversion = GetConvID(cmbUnit.SelectedItem);

                lbl_Unit_Arbeitspreis.Text = $"€/{conv.ToUnitCode}";
                lbl_Unit_Heizwert.Text = $"kWh/{conv.ToUnitCode}";
                lbl_Unit_Brennwert.Text = $"kWh/{conv.ToUnitCode}";
            }
        }

        private int GetConvID(object selectedItem)
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

        private void Arbeitspreis_Changed(object sender, EventArgs e)
        {
            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                // Den eingegebenen Wert zurück auf Liter (Basis) rechnen und speichern
                _basePrice = (double)numArbeitspreis.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void UpdatePricePerKWh()
        {
            decimal price = numArbeitspreis.Value;
            decimal hi = numHeizwert.Value;
            decimal hs = numBrennwert.Value;

            if (hi > 0)
            {
                decimal result = price / hi;
                lblResult.Text = $"{result:N4} €";
                lblFormula.Text = $"{price:N2} € ÷ {hi:N2} kWh = {result:N4} €/kWh";
            }
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            SaveProjectAndHistory();
            LoadHistory(_carrier.ID, _projectId);
        }

        public void SaveProjectAndHistory()
        {
            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                // Aktuelle Werte auf Basis zurückrechnen
                double currentPriceBase = (double)numArbeitspreis.Value * conv.Factor;
                double currentHiBase = (double)numHeizwert.Value * conv.Factor;
                double currentHsBase = (double)numBrennwert.Value * conv.Factor;
                double currentGroundPrice = (double)numGrundpreis.Value;
                int currentConvID = GetConvID(cmbUnit.SelectedItem);
                string currentUnit = ((EnergyConversion)cmbUnit.SelectedItem).ToUnitCode;

                // Prüfung: Hat sich wirklich etwas geändert?
                // Wir vergleichen mit den Anker-Variablen (_basePrice, _baseHi), 
                // die beim Laden oder letzten Speichern gesetzt wurden.
                bool hasChanged = Math.Abs(currentPriceBase - _basePrice) > 0.0001 ||
                                  Math.Abs(currentHiBase - _baseHi) > 0.0001 ||
                                  Math.Abs(currentHsBase - _baseHs) > 0.0001 ||
                                  Math.Abs(currentGroundPrice - _baseGroundPrice) > 0.01;

                if (hasChanged)
                {
                    // Historie nur bei Änderung ---
                    string sqlHistory = @"INSERT INTO energy_price 
                                (carrier_id, id_projekt, arbeitspreis_eur_unit, arbeitspreis_basis, grundpreis_eur_year, valid_from, arbeitspreis_unit) 
                                VALUES (?, ?, ?, ?, ?, ?, ?)";

                    DataRepository.ExecuteSQL(sqlHistory, new OleDbParameter[] {
                        new OleDbParameter("@cid", _carrier.ID),
                        new OleDbParameter("@prid", _projectId),
                        new OleDbParameter("@ap", Math.Round(currentPriceBase,4)),
                        new OleDbParameter("@hi", Math.Round(currentHiBase,4)),
                        new OleDbParameter("@gp", Math.Round(currentGroundPrice,4)),
                        new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                        new OleDbParameter(@"au", lblBasisnheit.Text)
                    });

                    // Wir aktualisieren unsere Anker-Variablen, damit beim nächsten Klick 
                    // ohne Änderung nichts passiert
                    _basePrice = currentPriceBase;
                    _baseHi = currentHiBase;
                    _baseGroundPrice = currentGroundPrice;
                }

                // Projekt-Setting (Wird immer aktualisiert/überschrieben) ---
                // Das Projekt-Setting sollte immer den aktuellen Stand des Editors haben
                string sqlUpsert = @"UPDATE energy_Project_settings 
                            SET custom_price_work = ?, custom_hi = ?, custom_hs = ?, custom_price_base = ?, ID_Umrechnung = ?
                            WHERE ID_Projekt = ? AND ID_Energieträger = ?";

                int rows = (int)DataRepository.ExecuteNonQuery(sqlUpsert, new OleDbParameter[] {
                    new OleDbParameter("@p", currentPriceBase),
                    new OleDbParameter("@hi", currentHiBase),
                    new OleDbParameter("@hs", currentHsBase),
                    new OleDbParameter("@b", currentGroundPrice),
                    new OleDbParameter(@"cid", currentConvID),
                    new OleDbParameter("@pid", _projectId),
                    new OleDbParameter("@eid", _carrier.ID)
                });

                if ((int)rows == 0)
                {
                    string sqlInsert = @"INSERT INTO energy_Project_settings 
                                (ID_Projekt, ID_Energieträger, custom_price_work, custom_hi, custom_Hs, custom_price_base, ID_Umrechnung) 
                                VALUES (?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                        new OleDbParameter("@pid", _projectId),
                        new OleDbParameter("@eid", _carrier.ID),
                        new OleDbParameter("@p", currentPriceBase),
                        new OleDbParameter("@h", currentHiBase),
                        new OleDbParameter("@hs", currentHsBase),
                        new OleDbParameter("@b", currentGroundPrice),
                        new OleDbParameter(@"cid", currentConvID),
                    });
                }
            }
        }

        private void SetupHistoryGrid()
        {
            dgvHistory.AutoGenerateColumns = false;
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.ReadOnly = true;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.RowHeadersVisible = false;

            dgvHistory.Columns.Clear();

            // Spalte: Datum
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "valid_from",
                HeaderText = "Gültig ab",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" }
            });

            // Spalte: Arbeitspreis (Basis)
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis_eur_unit",
                HeaderText = "Preis (Basis)",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N4" }
            });

            // Spalte: Heizwert (Basis)
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis_basis",
                HeaderText = "Heizwert",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            // Spalte: Grundpreis
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis_unit",
                HeaderText = "Basis Einheit",
                Width = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            // Spalte: Basis der Arbeitspreis-Einheit (z.B. €/Liter)
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "grundpreis_eur_year",
                HeaderText = "Grundpreis/a",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill // <--- Füllt den Rest aus
            });

            dgvHistory.DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            // Setzt den Font für die Spaltenköpfe
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        public void LoadHistory(int carrierId, int? projectId = null)
        {
            // Basis-Query: Wir sortieren nach Datum (neueste oben)
            string sql = "SELECT valid_from, arbeitspreis_basis, arbeitspreis_eur_unit, grundpreis_eur_year, arbeitspreis_unit " +
                         "FROM energy_price WHERE carrier_id = ?";

            List<OleDbParameter> parameters = new List<OleDbParameter>();
            parameters.Add(new OleDbParameter("@cid", carrierId));

            // Falls du dich für die projektbezogene Historie entscheidest:
            if (projectId.HasValue)
            {
                sql += " AND id_projekt = ?";
                parameters.Add(new OleDbParameter("@pid", projectId.Value));
            }

            sql += " ORDER BY valid_from DESC";

            try
            {
                // Wir nutzen die GetDataTable Methode aus deinem Repository
                DataTable dt = DataRepository.GetDataTable(sql, parameters.ToArray());
                dgvHistory.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Historie: " + ex.Message);
            }
        }
    }
}

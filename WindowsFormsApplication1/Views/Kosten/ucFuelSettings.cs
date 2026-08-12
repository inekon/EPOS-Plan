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

        // Aktuelle Live-Werte (immer auf Basiseinheit normiert)
        private double _baseHi;
        private double _baseHs;
        private double _baseGroundPrice;
        private double _basePowerPrice;
        private double _baseWorkPrice;
        private double _baseCO2;
        private double _baseSO2;
        private double _baseNOx;

        // Reine Speicher-Anker des originalen DB-Zustands für den hasChanged-Vergleich
        private double _dbWorkPrice;
        private double _dbGroundPrice;
        private double _dbPowerPrice;
        private double _dbHi;
        private double _dbHs;
        private double _dbCO2;
        private double _dbSO2;
        private double _dbNOx;

        private int id_conversion; // Speichert die ID der aktuell ausgewählten Umrechnung
        private bool _isUpdatingUi = false; // Verhindert Endlosschleifen bei automatischer UI-Anpassung

        public ucFuelSettings(int projectId, EnergyCarrier carrier)
        {
            InitializeComponent();

            _projectId = projectId;
            _carrier = carrier;

            // Events abonnieren (Live-Berechnung & Werterfassung)
            numArbeitspreis.ValueChanged += numArbeitspreis_ValueChanged;
            numHeizwert.ValueChanged += numHeizwert_ValueChanged;
            numBrennwert.ValueChanged += numBrennwert_ValueChanged;

            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            numArbeitspreis.Maximum = 1000000;
            numLeistungspreis.Maximum = 1000000;
            numGrundpreis.Maximum = 1000000;

            numBrennwert.Maximum = 1000000;
            numHeizwert.Maximum = 1000000;

            // DYNAMISCHE UI-STEUERUNG AUS DER TABELLE 'pricing_model'

            // 1. Heizwert (has_hi) Sichtbarkeit
            numHeizwert.Visible = _carrier.HasHi;
            lbl_Heizwert.Visible = _carrier.HasHi;
            lbl_Unit_Heizwert.Visible = _carrier.HasHi;

            // 2. Brennwert (has_hs) Sichtbarkeit
            numBrennwert.Visible = _carrier.HasHs;
            lbl_Brennwert.Visible = _carrier.HasHs;
            lbl_Unit_Brennwert.Visible = _carrier.HasHs;

            // 3. Leistungspreis (has_powerprice) Sichtbarkeit
            numLeistungspreis.Visible = _carrier.HasPowerPrice;
            lbl_Leistungspreis.Visible = _carrier.HasPowerPrice;
            lbl_Unit_Leistungspreis.Visible = _carrier.HasPowerPrice;

            // 4. Sonderlogik für leitungsgebundene Energieträger (z.B. Strom, Fernwärme ohne Heizwert)
            if (!_carrier.HasHi)
            {
                lbl_Unit_Arbeitspreis.Text = "€ / kWh";
                groupBox_Formel.Visible = false; // Ohne Heizwert gibt es keine Formel
                cmbUnit.Enabled = true;         // Einheitenwechsel sperren
            }
            else
            {
                cmbUnit.Enabled = true;
                groupBox_Formel.Visible = true;
            }

            LoadData();
        }

        private void LoadData()
        {
            lblCarrierName.Text = $"{_carrier.Name}  (VDI 3805 {_carrier.Code})";
            lblGruppe.Text = $"Gruppe: {_carrier.GroupCode}";
            // Das neue Datum-Feld standardmäßig auf JETZT setzen
            dtpValidFrom.Value = DateTime.Now;

            _conversions = GetConversions(_carrier.ID_Brennstoff);

            cmbUnit.SelectedIndexChanged -= CmbUnit_SelectedIndexChanged;
            cmbUnit.DataSource = _conversions;
            cmbUnit.DisplayMember = "ToUnitCode";
            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            var baseUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == _carrier.BillingUnit);
            if (baseUnit != null) lblBasisnheit.Text = $"{_carrier.BillingUnit}";

            var projectSettings = GetProjectPrice(_projectId, _carrier.ID);

            _isUpdatingUi = true; // Zwingt die NumericUpDowns beim ersten Laden, keine Events zu feuern

            if (projectSettings != null)
            {
                numArbeitspreis.Value = (decimal)projectSettings.ArbeitspreisEurYear;
                numGrundpreis.Value = (decimal)projectSettings.GrundpreisEurYear;
                numLeistungspreis.Value = (decimal)projectSettings.LeistungspreisEurYear;

                numHeizwert.Value = (decimal)(projectSettings.CustomHi ?? _carrier.HiKwhPerUnit);
                numBrennwert.Value = (decimal)(projectSettings.CustomHs ?? _carrier.HsKwhPerUnit);

                numCO2.Value = (decimal)(projectSettings.CO2 ?? _carrier.CO2);
                numSO2.Value = (decimal)(projectSettings.SO2 ?? _carrier.SO2);
                numNOx.Value = (decimal)(projectSettings.NOx ?? _carrier.NOx);

                _baseHi = projectSettings.CustomHi ?? _carrier.HiKwhPerUnit;
                _baseHs = projectSettings.CustomHs ?? _carrier.HsKwhPerUnit;
                _baseGroundPrice = (double)numGrundpreis.Value;
                _baseWorkPrice = (double)numArbeitspreis.Value;
                _basePowerPrice = (double)numLeistungspreis.Value;
                _baseCO2 = (double)numCO2.Value;
                _baseSO2 = (double)numSO2.Value;
                _baseNOx = (double)numNOx.Value;

                string project_conversion = GetTargetUnitByConversionId(projectSettings.IDUmrechnung);
                var selectedUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == project_conversion);

                if (selectedUnit != null)
                {
                    cmbUnit.SelectedItem = selectedUnit;
                    id_conversion = projectSettings.IDUmrechnung ?? -1;
                }
            }
            else
            {
                numArbeitspreis.Value = (decimal)_carrier.price_work;
                numGrundpreis.Value = (decimal)_carrier.price_base;
                numLeistungspreis.Value = (decimal)_carrier.price_power;
                numCO2.Value = (decimal)_carrier.CO2;
                numSO2.Value = (decimal)_carrier.SO2;
                numNOx.Value = (decimal)_carrier.NOx;
                numBrennwert.Value = (decimal)_carrier.HsKwhPerUnit;
                numHeizwert.Value = (decimal)_carrier.HiKwhPerUnit;

                _baseWorkPrice = _carrier.price_work;
                _baseGroundPrice = _carrier.price_base;
                _basePowerPrice = _carrier.price_power;
                _baseHi = _carrier.HiKwhPerUnit;
                _baseHs = _carrier.HsKwhPerUnit;
                _baseCO2 = _carrier.CO2;
                _baseSO2 = _carrier.SO2;
                _baseNOx = _carrier.NOx;

                cmbUnit.SelectedItem = baseUnit;
            }

            // Sichern des unveränderten DB-Zustands für den späteren Historien-Vergleich
            _dbWorkPrice = _baseWorkPrice;
            _dbGroundPrice = _baseGroundPrice;
            _dbPowerPrice = _basePowerPrice;
            _dbHi = _baseHi;
            _dbHs = _baseHs;
            _dbCO2 = _baseCO2;
            _dbSO2 = _baseSO2;
            _dbNOx = _baseNOx;

            _isUpdatingUi = false; // UI-Befüllung beendet, Handler scharf schalten

            // Labels initialisieren (falls ein Heizwert existiert, sonst bleibt es bei €/kWh)
            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                lbl_Unit_Arbeitspreis.Text = $"€/{conv.ToUnitCode}";
                lbl_Unit_Leistungspreis.Text = $"€/{conv.ToUnitCode}";
                lbl_Unit_Heizwert.Text = $"kWh/{conv.ToUnitCode}";
                lbl_Unit_Brennwert.Text = $"kWh/{conv.ToUnitCode}";
            }
            UpdatePricePerKWh();
            SetupHistoryGrid();
            LoadHistory(_carrier.ID, _projectId);
        }

        private void numArbeitspreis_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi) return;

            if (!_carrier.HasHi)
            {
                // Strom/Fernwärme hat keine Umrechnung über Faktoren, Basis ist direkt der eingegebene Wert
                _baseWorkPrice = (double)numArbeitspreis.Value;
            }
            else if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _baseWorkPrice = (double)numArbeitspreis.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void numHeizwert_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi || !_carrier.HasHi) return;

            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _baseHi = (double)numHeizwert.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void numBrennwert_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi || !_carrier.HasHs) return;

            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _baseHs = (double)numBrennwert.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void CmbUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
         //   if (!_carrier.HasHi) return; // Wenn kein Heizwert existiert (Strom), ignorieren wir den Einheitenwechsel

            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _isUpdatingUi = true;

                numArbeitspreis.Value = (decimal)(_baseWorkPrice / conv.Factor);
                numHeizwert.Value = (decimal)(_baseHi / conv.Factor);

                if (_carrier.HasPowerPrice) numLeistungspreis.Value = (decimal)(_basePowerPrice / conv.Factor);
                if (_carrier.HasHs) numBrennwert.Value = (decimal)(_baseHs / conv.Factor);

                id_conversion = GetConvID(cmbUnit.SelectedItem);

                lbl_Unit_Arbeitspreis.Text = $"€/{conv.ToUnitCode}";
                lbl_Unit_Heizwert.Text = $"kWh/{conv.ToUnitCode}";

                if (_carrier.HasPowerPrice) lbl_Unit_Leistungspreis.Text = $"€/{conv.ToUnitCode}";
                if (_carrier.HasHs) lbl_Unit_Brennwert.Text = $"kWh/{conv.ToUnitCode}";

                UpdatePricePerKWh();

                _isUpdatingUi = false;
            }
        }

        private void UpdatePricePerKWh()
        {
            if (!_carrier.HasHi)
            {
                // Bei Strom/Fernwärme entspricht der Arbeitspreis bereits dem Preis/kWh
                lblResult.Text = $"{numArbeitspreis.Value:N4} €";
                lblFormula.Text = "Direktabrechnung nach kWh";
                return;
            }

            decimal price = numArbeitspreis.Value;
            decimal hi = numHeizwert.Value;

            if (hi > 0)
            {
                decimal result = price / hi;
                lblResult.Text = $"{result:N4} €";
                lblFormula.Text = $"{price:N2} € ÷ {hi:N2} kWh = {result:N4} €/kWh";
            }
        }

        /// <summary>Träger-ID dieses Controls (für den Zuordnungs-Check beim Schließen, Phase 7).</summary>
        public int CarrierId
        {
            get { return _carrier != null ? _carrier.ID : 0; }
        }

        public void SaveProjectAndHistory()
        {
            // Bei Strom gibt es keine Conversion aus der Combo
            //int currentConvID = _carrier.HasHi ? GetConvID(cmbUnit.SelectedItem) : -1;
            int currentConvID = GetConvID(cmbUnit.SelectedItem);

            double currentPriceBase = _baseWorkPrice;
            double currentHiBase = _baseHi;
            double currentHsBase = _baseHs;

            double currentGroundPrice = (double)numGrundpreis.Value;
            double currentPowerPrice = (double)numLeistungspreis.Value;
            double currentCO2 = (double)numCO2.Value;
            double currentSO2 = (double)numSO2.Value;
            double currentNOx = (double)numNOx.Value;

            // Das vom Benutzer gewählte (ggf. zukünftige) Datum abgreifen
            DateTime chosenDate = dtpValidFrom.Value;

            // Vergleich auf Basis der unberührten DB-Urwerte
            bool hasChanged = Math.Abs(currentPriceBase - _dbWorkPrice) > 0.0001 ||
                              Math.Abs(currentHiBase - _dbHi) > 0.0001 ||
                              Math.Abs(currentHsBase - _dbHs) > 0.0001 ||
                              Math.Abs(currentGroundPrice - _dbGroundPrice) > 0.01 ||
                              Math.Abs(currentPowerPrice - _dbPowerPrice) > 0.01 ||
                              Math.Abs(currentCO2 - _dbCO2) > 0.01 ||
                              Math.Abs(currentSO2 - _dbSO2) > 0.01 ||
                              Math.Abs(currentNOx - _dbNOx) > 0.01;

            if (hasChanged)
            {
                // NEU: Prüfen, ob für dieses Projekt, diesen Energieträger und GENAU dieses Datum bereits ein Eintrag existiert
                string sqlCheck = "SELECT COUNT(*) FROM energy_price WHERE carrier_id = ? AND id_projekt = ? AND valid_from = ?";
                OleDbParameter[] checkParams = {
                    new OleDbParameter("@cid", _carrier.ID),
                    new OleDbParameter("@prid", _projectId),
                    new OleDbParameter("@date", OleDbType.Date) { Value = chosenDate.Date } // .Date ignoriert Uhrzeit-Störfaktoren
                };

                int existingCount = Convert.ToInt32(DataRepository.ExecuteScalar(sqlCheck, checkParams));

                if (existingCount > 0)
                {
                    // Existiert bereits -> UPDATE
                    string sqlUpdateHistory = @"UPDATE energy_price 
                                                SET arbeitspreis = ?, heizwert = ?, grundpreis = ?, 
                                                    arbeitspreis_unit = ?, leistungspreis = ?
                                                WHERE carrier_id = ? AND id_projekt = ? AND valid_from = ?";

                    DataRepository.ExecuteSQL(sqlUpdateHistory, new OleDbParameter[] {
                        new OleDbParameter("@ap", Math.Round(currentPriceBase, 4)),
                        new OleDbParameter("@hi", Math.Round(currentHiBase, 4)),
                        new OleDbParameter("@gp", Math.Round(currentGroundPrice, 4)),
                        new OleDbParameter("@au", lblBasisnheit.Text),
                        new OleDbParameter("@lp", Math.Round(currentPowerPrice, 4)),
                        new OleDbParameter("@cid", _carrier.ID),
                        new OleDbParameter("@prid", _projectId),
                        new OleDbParameter("@date", OleDbType.Date) { Value = chosenDate.Date }
                    });
                }
                else
                {
                    // Existiert noch nicht -> INSERT
                    string sqlInsertHistory = @"INSERT INTO energy_price 
                                    (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, 
                                    valid_from, arbeitspreis_unit, leistungspreis) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                    DataRepository.ExecuteSQL(sqlInsertHistory, new OleDbParameter[] {
	                    new OleDbParameter("@cid", _carrier.ID),
	                    new OleDbParameter("@prid", _projectId),
	                    new OleDbParameter("@ap", Math.Round(currentPriceBase, 4)),
	                    new OleDbParameter("@hi", Math.Round(currentHiBase, 4)),
	                    new OleDbParameter("@gp", Math.Round(currentGroundPrice, 4)),
	                        new OleDbParameter("@date", OleDbType.Date) { Value = chosenDate.Date },
	                    new OleDbParameter("@au", lblBasisnheit.Text),
	                    new OleDbParameter("@lp", Math.Round(currentPowerPrice, 4))
	                });
                }

                // Speicher-Anker aktualisieren
                _dbWorkPrice = currentPriceBase;
                _dbGroundPrice = currentGroundPrice;
                _dbPowerPrice = currentPowerPrice;
                _dbHi = currentHiBase;
                _dbHs = currentHsBase;
                _dbCO2 = currentCO2;
                _dbSO2 = currentSO2;
                _dbNOx = currentNOx;
            }

            // Projekt-Settings Upsert
            string sqlUpsert = @"UPDATE energy_Project_settings 
                                SET custom_price_work = ?, custom_price_power = ?, custom_hi = ?, custom_hs = ?,
                                custom_price_base = ?, ID_Umrechnung = ?,
                                co2 = ?, so2 = ?, nox = ?
                                WHERE ID_Projekt = ? AND ID_Energieträger = ?";

            int rows = (int)DataRepository.ExecuteNonQuery(sqlUpsert, new OleDbParameter[] {
                new OleDbParameter("@p", currentPriceBase),
                new OleDbParameter("@pl", currentPowerPrice),
                new OleDbParameter("@hi", currentHiBase),
                new OleDbParameter("@hs", currentHsBase),
                new OleDbParameter("@b", currentGroundPrice),
                new OleDbParameter("@cid", currentConvID != -1 ? (object)currentConvID : DBNull.Value),
                new OleDbParameter("@co2", currentCO2),
                new OleDbParameter("@so2", currentSO2),
                new OleDbParameter("@nox", currentNOx),
                new OleDbParameter("@pid", _projectId),
                new OleDbParameter("@eid", _carrier.ID)
            });

            if (rows == 0)
            {
                string sqlInsert = @"INSERT INTO energy_Project_settings 
                                    (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, 
                                    custom_price_base, ID_Umrechnung, co2, so2, nox) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                    new OleDbParameter("@pid", _projectId),
                    new OleDbParameter("@eid", _carrier.ID),
                    new OleDbParameter("@p", currentPriceBase),
                    new OleDbParameter("@pl", currentPowerPrice),
                    new OleDbParameter("@h", currentHiBase),
                    new OleDbParameter("@hs", currentHsBase),
                    new OleDbParameter("@b", currentGroundPrice),
                    new OleDbParameter("@cid", currentConvID != -1 ? (object)currentConvID : DBNull.Value),
                    new OleDbParameter("@co2", currentCO2),
                    new OleDbParameter("@so2", currentSO2),
                    new OleDbParameter("@nox", currentNOx)
                });
            }
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            SaveProjectAndHistory();
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
                    ArbeitspreisEurYear = row["custom_price_work"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_work"]) : null,
                    GrundpreisEurYear = row["custom_price_base"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_base"]) : null,
                    LeistungspreisEurYear = row["custom_price_power"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_power"]) : null,
                    CustomHi = row["custom_hi"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_hi"]) : null,
                    CustomHs = row["custom_hs"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_hs"]) : null,
                    CO2 = row["co2"] != DBNull.Value ? (double?)Convert.ToDouble(row["co2"]) : null,
                    SO2 = row["so2"] != DBNull.Value ? (double?)Convert.ToDouble(row["so2"]) : null,
                    NOx = row["nox"] != DBNull.Value ? (double?)Convert.ToDouble(row["nox"]) : null,
                    IDUmrechnung = row["ID_Umrechnung"] != DBNull.Value ? (int?)Convert.ToInt32(row["ID_Umrechnung"]) : null
                };
            }
            return null;
        }

        public string GetTargetUnitByConversionId(int idumrechnung)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select to_unit from energy_conversion where id=" + idumrechnung);
            rs.Next();
            string unit = (string)rs.Read("to_Unit");
            rs.Close();
            return unit;
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
            return -1;
        }

        private void SetupHistoryGrid()
        {
            dgvHistory.AutoGenerateColumns = false;
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.ReadOnly = true;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.Columns.Clear();

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "valid_from",
                HeaderText = "Gültig ab",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } // Format auf reines Datum gekürzt
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "heizwert",
                HeaderText = "Heizwert",
                Width = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis_unit",
                HeaderText = "Basis Einheit",
                Width = 50
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis",
                HeaderText = "Arbeitspreis",
                Width = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "grundpreis",
                HeaderText = "Grundpreis",
                Width = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" },
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "leistungspreis",
                HeaderText = "Leistungspreis",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" },
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvHistory.DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        public void LoadHistory(int carrierId, int? projectId = null)
        {
            string sql = "SELECT valid_from, heizwert, arbeitspreis, grundpreis, " +
                         "arbeitspreis_unit, leistungspreis " +
                         "FROM energy_price WHERE carrier_id = ?";

            List<OleDbParameter> parameters = new List<OleDbParameter>();
            parameters.Add(new OleDbParameter("@cid", carrierId));

            if (projectId.HasValue)
            {
                sql += " AND id_projekt = ?";
                parameters.Add(new OleDbParameter("@pid", projectId.Value));
            }

            sql += " ORDER BY valid_from DESC";

            try
            {
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
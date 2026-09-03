// Prüfmuster für Formularkarte — Stand vor iU8-9 (92380ea^); die Maske wurde durch
// EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor ersetzt.
//
// Auszug aus WindowsFormsApplication1/Views/Kosten/Form_Kosten.cs, Zeilen 2089-2196:
// die einzige Stelle des Bestands, die Form_Kosten_Auswahl modal geöffnet hat.
// Der Methodenrumpf steht unverändert; ergänzt sind nur Namensraum und Klassenhülle,
// damit der Auszug für sich allein syntaktisch gültiges C# ist — der Aufrufersucher
// des Werkzeugs zerlegt ihn mit Roslyn.

namespace WindowsFormsApplication1
{
    public partial class Form_Kosten : Form
    {
        private string CreateNewEnergyCarrier()
        {
            using (var dlg = new Form_Kosten_Auswahl())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return "";

                try
                {
                    // Default-Werte aus dem Brennstoff-Stamm (Preise/Emissionen)
                    double default_arbeitspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", dlg.SelectedBrennstoffID));
                    double default_grundpreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", dlg.SelectedBrennstoffID));
                    double default_leistungspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", dlg.SelectedBrennstoffID));
                    double default_co2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", dlg.SelectedBrennstoffID));
                    double default_so2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", dlg.SelectedBrennstoffID));
                    double default_nox = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", dlg.SelectedBrennstoffID));

                    // 1) Katalog-Träger suchen; existiert er, wird er wiederverwendet
                    int carrierId = -1;
                    object existing = DataRepository.ExecuteScalar(
                        "SELECT id FROM energy_carrier WHERE name = ?",
                        new DbParam[] { new DbParam("@name", dlg.SelectedName) });
                    if (existing != null && existing != DBNull.Value)
                        carrierId = Convert.ToInt32(existing);

                    if (carrierId < 0)
                    {
                        // Katalog-Datensatz nur anlegen, wenn wirklich neu
                        string insertSql = @"INSERT INTO energy_carrier
                             (ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit,
                              hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active)
                             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        DbParam[] ps = {
                            new DbParam("@idB",   dlg.SelectedBrennstoffID),
                            new DbParam("@code",  dlg.SelectedCode),
                            new DbParam("@name",  dlg.SelectedName),
                            new DbParam("@gc",    dlg.SelectedGroupCode),
                            new DbParam("@pm",    dlg.SelectedBrennstoffCode),
                            new DbParam("@unit",  dlg.SelectedBillingUnit),
                            new DbParam("@shi",   dlg.SelectedHi),
                            new DbParam("@shs",   dlg.SelectedHs),
                            new DbParam("@defap", default_arbeitspreis),
                            new DbParam("@defgp", default_grundpreis),
                            new DbParam("@co2",   default_co2),
                            new DbParam("@so2",   default_so2),
                            new DbParam("@nox",   default_nox),
                            new DbParam("@active", DbParamTyp.Boolean) { Wert = true }
                        };
                        carrierId = DataRepository.ExecuteInsertAndGetId(insertSql, ps);
                    }

                    // 2) Ist der Träger diesem Projekt schon zugeordnet? -> nicht doppeln
                    int vorhanden = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                        new DbParam[] {
                    new DbParam("@pid", m_ID_Projekt),
                    new DbParam("@eid", carrierId)
                        }));
                    if (vorhanden > 0)
                    {
                        MessageBox.Show($"Die Energieträgervariante '{dlg.SelectedName}' ist diesem Projekt bereits zugeordnet.");
                        return dlg.SelectedName;
                    }

                    // 3) Projektbezogene Sätze anlegen (Preis-Historie + Projekt-Einstellungen)
                    // Befund B5 (11.08.2026): der Ersteintrag ließ leistungspreis leer,
                    // obwohl der Standardwert aus Tab_Brennstoff_Stamm ermittelt wurde.
                    string sqlHistory = @"INSERT INTO energy_price
                         (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlHistory, new DbParam[] {
                        new DbParam("@cid",  carrierId),
                        new DbParam("@prid", m_ID_Projekt),
                        new DbParam("@ap",   Math.Round(default_arbeitspreis, 4)),
                        new DbParam("@hi",   Math.Round(dlg.SelectedHi, 4)),
                        new DbParam("@gp",   Math.Round(default_grundpreis, 4)),
                        new DbParam("@date", DbParamTyp.Date) { Wert = DateTime.Now },
                        new DbParam("@au",   dlg.SelectedBillingUnit),
                        new DbParam("@lp",   Math.Round(default_leistungspreis, 4))
                    });

                    string sqlInsert = @"INSERT INTO energy_Project_settings
                         (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                          custom_price_base, ID_Umrechnung, co2, so2, nox)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlInsert, new DbParam[] {
                        new DbParam("@pid",    m_ID_Projekt),
                        new DbParam("@eid",    carrierId),
                        new DbParam("@p",      Math.Round(default_arbeitspreis, 4)),
                        new DbParam("@pl",     Math.Round(default_leistungspreis, 4)),
                        new DbParam("@h",      Math.Round(dlg.SelectedHi, 4)),
                        new DbParam("@hs",     Math.Round(dlg.SelectedHs, 4)),
                        new DbParam("@b",      Math.Round(default_grundpreis, 4)),
                        new DbParam("@convid", dlg.SelectedConvID),
                        new DbParam("@co2",    default_co2),
                        new DbParam("@so2",    default_so2),
                        new DbParam("@nox",    default_nox)
                    });

                    MessageBox.Show("Energieträgervariante erfolgreich angelegt.");
                    return dlg.SelectedName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Speichern: " + ex.Message);
                }
            }
            return "";
        }
    }
}

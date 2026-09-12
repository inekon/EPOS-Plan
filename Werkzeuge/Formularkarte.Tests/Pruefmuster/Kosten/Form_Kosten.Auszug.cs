// Prüfmuster für Formularkarte — die beiden Öffnermethoden der stillgelegten
// Kostenverwaltung.
//
// 1. CreateNewEnergyCarrier, Stand vor iU8-9 (92380ea^), Zeilen 2089-2196 von
//    WindowsFormsApplication1/Views/Kosten/Form_Kosten.cs: die einzige Stelle des
//    Bestands, die Form_Kosten_Auswahl modal geöffnet hat. Diese Maske wurde durch
//    EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor ersetzt.
// 2. AddKostenItem, Stand vor iU9-W0 (16b106a^), Zeilen 1418-1484 derselben Datei:
//    die einzige Stelle, die Form_KostenfaktorItem modal geöffnet hat. Beide Masken
//    sind mit dem Anwenderentscheid iF29 stillgelegt.
//
// Die Methodenrümpfe stehen unverändert; ergänzt sind nur Namensraum und Klassenhülle,
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

        private void AddKostenItem(string komponenete)
        {
            // K4-Wächter: Ohne Kostenkategorie gibt es nichts zu erfassen. Die Prüfung
            // steht VOR dem Dialog — den Anwender erst tippen zu lassen und den Datensatz
            // danach zu verwerfen wäre die schlechtere Hälfte beider Möglichkeiten.
            int? kat = AktuelleKategorieOderNull();
            if (!kat.HasValue) return;

            // Eingabemaske öffnen (bleibt UI-Logik)
            Form_KostenfaktorItem frm = new Form_KostenfaktorItem();

            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                // 2. Werte aus dem Dialog abrufen
                int stammID = frm.gewählteID;
                double nutzungsdauer = Convert.ToDouble(frm.Nutzungsdauer);
                double betrag = Convert.ToDouble(frm.Wert);
                string einheit = frm.Einheit;
                string gewaehlteGruppe = string.IsNullOrWhiteSpace(frm.Gruppe) ? "Allgemein" : frm.Gruppe.Trim();

                // 3. Gruppe in den Katalog aufnehmen ("Lern-Funktion")
                // Wir nutzen den "Insert if not exists" Trick mit deiner neuen Methode
                //
                // SQL-Dialekt-Audit 03.09.2026: Die Unterabfrage hiess ihre Zählspalte
                // frueher gar nicht und wurde als CheckTbl.[Expr1000] angesprochen -
                // "Expr1000" ist der Name, den ACCESS einer unbenannten Ausdrucksspalte
                // von sich aus gibt. SQLite tut das nicht ("no such column:
                // CheckTbl.Expr1000"), der Katalogeintrag entstand also nie. Jetzt traegt
                // die Spalte einen eigenen Namen; Wirkung und Zahl der Parameter bleiben.
                string sqlKatalog = @"INSERT INTO Tab_KostenGruppenKatalog (GruppenName)
                              SELECT ?
                              FROM (SELECT COUNT(*) AS Anzahl
                              FROM Tab_KostenGruppenKatalog
                              WHERE GruppenName = ?) AS CheckTbl
                              WHERE CheckTbl.Anzahl = 0";

                DataRepository.ExecuteSQL(sqlKatalog,
                    new DbParam("@g1", gewaehlteGruppe),
                    new DbParam("@g2", gewaehlteGruppe));

                // 4. INSERT in Tab_ProjektWerte
                string sqlInsert = @"INSERT INTO Tab_ProjektWerte
                                    (ProjektID, StammID, EingegebenerWert, Nutzungsdauer, Einheit, Gruppe, KomponentenID, KategorieID) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                DataRepository.ExecuteSQL(sqlInsert,
                    new DbParam("@pid", m_ID_Projekt),
                    new DbParam("@sid", stammID),
                    new DbParam("@val", betrag),
                    new DbParam("@nd", nutzungsdauer),
                    new DbParam("@ein", einheit),
                    new DbParam("@grp", gewaehlteGruppe),
                    new DbParam("@kid", GetKomponentenID(komponenete)),
                    new DbParam("@kat", kat.Value)
                );

                // 5. UI aktualisieren
                LoadKostenFaktoren(m_ID_Projekt, komponenete);
                Gesamtkosten();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Verarbeiten der Daten: " + ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zusatzdaten eines Energietraegers, wie sie beim Anlegen einer Variante
    /// gebraucht werden.
    /// </summary>
    /// <param name="GroupCode">Gruppe der Brennstoffkategorie (<c>k.Gruppe</c>) - frueher <c>SelectedGroupCode</c>.</param>
    /// <param name="BillingUnit">Abrechnungseinheit des Stammsatzes (<c>s.Einheit</c>) - frueher <c>SelectedBillingUnit</c>.</param>
    /// <param name="Hi">Heizwert - frueher <c>SelectedHi</c>.</param>
    /// <param name="Hs">Brennwert - frueher <c>SelectedHs</c>.</param>
    /// <param name="Code">Code der Brennstoffkategorie (<c>k.Code</c>) - frueher <c>SelectedBrennstoffCode</c>.</param>
    /// <param name="ConvID">Id des Umrechnungssatzes, <c>-1</c> wenn es keinen gibt - frueher <c>SelectedConvID</c>.</param>
    public sealed record EnergietraegerDaten(
        string GroupCode,
        string BillingUnit,
        double Hi,
        double Hs,
        string Code,
        int ConvID);

    /// <summary>
    /// Die Datenbankseite des Dialogs „Energietraeger Variante"
    /// (Umsetzungskonzept iOS, Paket iU8, Stichtag iZ5).
    ///
    /// <para><b>Wozu.</b> Der Vorlaeufer <c>Views\Kosten\Form_Kosten_Auswahl</c> hat
    /// selbst gelesen: die Auswahlliste im Konstruktor
    /// (<c>LoadBrennstoffArten</c>), die sechs abgeleiteten Werte beim Klick auf OK
    /// (<c>FetchAdditionalData</c>, <c>GetConvID</c>). Ein Dialog, der die Datenbank
    /// kennt, laesst sich weder ohne Datenbank pruefen noch auf iOS
    /// wiederverwenden. Die drei Abfragen stehen deshalb hier; die Komponente
    /// <c>EPOS.UI\Dialoge\Kosten\EnergietraegerVarianteDialog.razor</c> bekommt die
    /// Liste fertig herein und gibt nur zurueck, was der Anwender eingegeben hat.</para>
    ///
    /// <para><b>Die Abfragen sind zeichengleich uebernommen.</b> Sie sind der Grund,
    /// warum ein angelegter Traeger dieselben Werte bekommt wie vorher — jede
    /// „Verbesserung" waere eine stille Fachaenderung. Auch die harte Umwandlung von
    /// <c>Hi</c> und <c>Hs</c> ist absichtlich uebernommen: Ein fehlender Heizwert
    /// soll auffallen und nicht als 0 durchrutschen.</para>
    ///
    /// <para><b>Zweitnutzer eingeloest (iU9-1, 03.09.2026).</b> Die zeichengleiche
    /// Schwester <c>Views\Kosten\Form_Kosten_VarAuswahl</c> trug dieselben drei
    /// Abfragen ein zweites Mal (Konzept Einheitenbruch § 4.3). Sie ist geloescht;
    /// ihre beiden Aufrufer <c>Form_Heizkessel</c> und <c>Form_BHKWEing</c> zeigen
    /// jetzt dieselbe Razor-Komponente und holen die abgeleiteten Werte hier —
    /// die Abfragen stehen damit nur noch einmal im Bestand.</para>
    /// </summary>
    public static class EnergietraegerVarianteCtrl
    {
        /// <summary>
        /// Die waehlbaren Energietraeger in Anzeigereihenfolge.
        /// </summary>
        /// <remarks>
        /// Wortgleich <c>Form_Kosten_Auswahl.LoadBrennstoffArten</c>: dieselbe
        /// Abfrage, dieselbe Sortierung, dieselben beiden Spalten
        /// (<c>DisplayMember = "Bezeichner"</c>, <c>ValueMember = "ID"</c>).
        /// </remarks>
        public static IReadOnlyList<(int Id, string Name)> Energietraeger()
            => Energietraeger(null);

        /// <summary>
        /// Wie <see cref="Energietraeger()"/>, eingeengt auf eine Brennstoffkategorie
        /// (<c>Tab_Brennstoff_Stamm.ID_Kategorie</c>) - so, wie es der bis 03.09.2026
        /// gelöschte Zwilling <c>Form_Kosten_VarAuswahl</c> tat: Ein Heizkessel mit
        /// Erdgas bekommt nur die Gasträger angeboten, nicht Strom oder Holz
        /// (Anwenderbefund 03.09.2026). <paramref name="kategorieId"/> null oder 0 =
        /// alle Träger.
        /// </summary>
        public static IReadOnlyList<(int Id, string Name)> Energietraeger(int? kategorieId)
        {
            // Lädt die Namen aus Tab_Brennstoff_Stamm in die Auswahlliste
            bool gefiltert = kategorieId.HasValue && kategorieId.Value > 0;
            string sql = gefiltert
                ? "SELECT ID, Bezeichner FROM Tab_Brennstoff_Stamm WHERE ID_Kategorie = ? ORDER BY Bezeichner"
                : "SELECT ID, Bezeichner FROM Tab_Brennstoff_Stamm ORDER BY Bezeichner";

            var liste = new List<(int Id, string Name)>();
            DataTable tb = gefiltert
                ? DataRepository.GetDataTable(sql, new DbParam("@k", kategorieId.Value))
                : DataRepository.GetDataTable(sql);
            if (tb == null) return liste;

            foreach (DataRow row in tb.Rows)
            {
                if (row["ID"] == null || row["ID"] == DBNull.Value) continue;

                liste.Add((Convert.ToInt32(row["ID"]),
                           row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString()));
            }

            return liste;
        }

        /// <summary>
        /// Die Brennstoffkategorie eines Katalogträgers (<c>Tab_Brennstoff_Stamm.ID_Kategorie</c>),
        /// 0 wenn unbekannt. Die Aufrufer engen damit die Auswahlliste ein.
        /// </summary>
        public static int KategorieZu(int brennstoffId)
        {
            if (brennstoffId <= 0) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT ID_Kategorie FROM Tab_Brennstoff_Stamm WHERE ID = ?",
                new DbParam("@id", brennstoffId));
            return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 0;
        }

        /// <summary>
        /// Die sechs Werte, die sich aus dem gewaehlten Energietraeger ergeben.
        /// </summary>
        /// <param name="brennstoffId">Id aus <see cref="Energietraeger"/>.</param>
        /// <returns>
        /// Die Zusatzdaten. Findet der Stammsatz keine Kategorie, bleiben die Texte
        /// <c>null</c> und die Zahlen 0 — genau wie beim Vorlaeufer, dessen
        /// Eigenschaften dann unbelegt blieben.
        /// </returns>
        /// <remarks>
        /// Fasst <c>FetchAdditionalData</c> und <c>GetConvID</c> zusammen. Der
        /// Vorlaeufer baute fuer die zweite Abfrage ein
        /// <c>EnergyConversion</c>-Objekt, in dem Quell- und Zieleinheit
        /// dieselbe Abrechnungseinheit trugen; das Objekt war reines Transportmittel
        /// und entfaellt. Die Abfrage selbst ist unveraendert.
        /// </remarks>
        public static EnergietraegerDaten Ergaenzen(int brennstoffId)
        {
            string groupCode = null;
            string billingUnit = null;
            string code = null;
            double hi = 0;
            double hs = 0;

            // JOIN über Stamm -> Kategorien um group_code und billing_unit zu erhalten
            string sql = @"SELECT k.Gruppe, k.Code, s.Hi, s.Hs, s.Einheit
                       FROM Tab_Brennstoff_Stamm s
                       INNER JOIN Tab_BrennstoffKategorien k ON s.ID_Kategorie = k.ID
                       WHERE s.ID = ?";

            var tb = DataRepository.GetDataTable(sql, new DbParam[] {
                new DbParam("@id", brennstoffId)
            });
            var row = tb != null && tb.Rows.Count > 0 ? tb.Rows[0] : null;
            if (row != null)
            {
                groupCode = row["Gruppe"].ToString();
                billingUnit = row["Einheit"].ToString();
                code = row["Code"].ToString();
                hi = (double)row["Hi"];
                hs = (double)row["Hs"];
            }

            return new EnergietraegerDaten(groupCode, billingUnit, hi, hs, code,
                                           UmrechnungId(brennstoffId, billingUnit));
        }

        /// <summary>
        /// Der Umrechnungssatz von der Abrechnungseinheit auf sich selbst.
        /// </summary>
        /// <returns><c>-1</c>, wenn der Katalog keinen fuehrt (Fehlerfall).</returns>
        /// <remarks>
        /// Wortgleich <c>Form_Kosten_Auswahl.GetConvID</c>. Dass Quell- und
        /// Zieleinheit gleich sind, ist keine Nachlaessigkeit dieser Uebernahme,
        /// sondern der Bestand: Der Vorlaeufer belegte <c>FromUnit</c> und
        /// <c>ToUnitCode</c> beide mit <c>SelectedBillingUnit</c>.
        /// </remarks>
        private static int UmrechnungId(int brennstoffId, string einheit)
        {
            string sql = "SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? AND to_unit = ?";
            DbParam[] ps = {
                new DbParam("@cid", brennstoffId),
                new DbParam("@fu", einheit),
                new DbParam("@tu", einheit)
            };
            DataTable dt = DataRepository.GetDataTable(sql, ps);
            if (dt != null && dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["ID"]);
            }

            return -1; // Fehlerfall
        }

        // =================================================================================
        // W6.0a/b - die Schreibseite und die Traegerwahl der Erzeugerdialoge
        // =================================================================================

        /// <summary>
        /// Der Ausgang von <see cref="Anlegen"/>. Vier Faelle, weil der Vorlaeufer vier
        /// verschiedene Meldungen zeigte und der Aufrufer sie unterschiedlich behandelt:
        /// nur bei <see cref="Fehler"/> wird KEIN Erzeuger aufgenommen.
        /// </summary>
        public enum VariantenAnlage
        {
            /// <summary>Traeger, Preishistorie und Projektzuordnung sind geschrieben.</summary>
            Angelegt,

            /// <summary>
            /// Nur der Katalogtraeger steht. Im Assistenten und ohne echtes Projekt gibt es
            /// die Projektzeile noch nicht, an der <c>energy_price</c> und
            /// <c>energy_Project_settings</c> haengen - die traegt <c>WizardCtrl</c> nach.
            /// </summary>
            Vorgemerkt,

            /// <summary>Der Traeger war diesem Projekt schon zugeordnet; nichts Neues geschrieben.</summary>
            BereitsZugeordnet,

            /// <summary>Nichts geschrieben - der Aufrufer nimmt den Erzeuger nicht auf.</summary>
            Fehler
        }

        /// <summary>
        /// Was <see cref="Anlegen"/> zurueckgibt: die Traeger-Id, der Ausgang und der
        /// Meldungstext, den der Vorlaeufer selbst als <c>MessageBox</c> zeigte.
        /// </summary>
        /// <param name="CarrierId">
        /// <c>energy_carrier.id</c>; 0 bei <see cref="VariantenAnlage.Fehler"/> - genau das
        /// Signal, an dem <c>btn_Kessel_Hinzu_Click</c> abbrach (<c>carrierID &lt;= 0</c>).
        /// </param>
        /// <param name="Ausgang">Welcher der vier Faelle eingetreten ist.</param>
        /// <param name="Meldung">Bereits lokalisiert; die Oberflaeche zeigt ihn als Banner.</param>
        public sealed record VariantenErgebnis(int CarrierId, VariantenAnlage Ausgang, string Meldung);

        /// <summary>
        /// Legt die Energietraegervariante an - Katalogtraeger, Preishistorie und
        /// Projektzuordnung in EINER Transaktion.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Herkunft.</b> Diese 185 Zeilen standen bis iU9-W6 ZWEIMAL wortgleich in der
        /// Oberflaeche: <c>Form_Heizkessel.CreateNewEnergyCarrier</c> und
        /// <c>Form_BHKWEing.CreateNewEnergyCarrier</c> unterschieden sich allein in ihren
        /// Kommentaren. Beide Masken sind seit iU9-W6.3/W6.4 Razor-Komponenten, und eine
        /// Komponente kennt keine Datenbank (<c>EPOS.UI/CLAUDE.md</c>) - der Schreibweg
        /// gehoert deshalb hierher.
        /// </para>
        /// <para>
        /// <b>Die SQL-Anweisungen sind zeichengleich uebernommen</b>, einschliesslich der
        /// Umlautspalte <c>ID_Energieträger</c>: SQLite faltet Gross- und Kleinschreibung
        /// nur bei ASCII, ein <c>ID_Energietraeger</c> traefe die Spalte nicht mehr
        /// (BETRIEB_SQLITE Abschnitt 6). Auch die Reihenfolge bleibt: die sechs
        /// Vorgabewerte als reine Lesezugriffe VOR dem Vorgang, damit die Transaktion so
        /// kurz wie moeglich ist.
        /// </para>
        /// <para>
        /// <b>Was sich aendert.</b> Die vier <c>MessageBox</c>-Aufrufe werden zum
        /// <c>Meldung</c>-Text des Ergebnisses; wer meldet, entscheidet die Oberflaeche.
        /// Die Parameter kommen als drei Einzelwerte statt als Ergebnis-Record der
        /// Komponente - der Kern referenziert <c>EPOS.UI</c> nicht und darf es auch nicht
        /// (Abweichung A-2 des Protokolls W6).
        /// </para>
        /// </remarks>
        /// <param name="projektId">Projekt, dem der Traeger zugeordnet wird; 0 = keines.</param>
        /// <param name="wizard">
        /// Assistentenbetrieb. Wie <c>projektId &lt;= 0</c> fuehrt er nur zum Katalogtraeger.
        /// </param>
        /// <param name="brennstoffId">Gewaehlter Energietraeger aus <see cref="Energietraeger()"/>.</param>
        /// <param name="brennstoffName">Sein Bezeichner - er wird <c>energy_carrier.code</c>.</param>
        /// <param name="variantenName">Die vom Anwender vergebene Bezeichnung der Variante.</param>
        public static VariantenErgebnis Anlegen(int projektId, bool wizard,
                                                int brennstoffId, string brennstoffName,
                                                string variantenName)
        {
            // Die sechs abgeleiteten Werte - frueher FetchAdditionalData und GetConvID im
            // Dialog selbst, seit iU9-1 derselbe Weg hier.
            EnergietraegerDaten daten = Ergaenzen(brennstoffId);

            // Default-Werte (reine Lesezugriffe) VOR der Transaktion ermitteln.
            double default_arbeitspreis = ZuDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", brennstoffId));
            double default_grundpreis = ZuDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", brennstoffId));
            double default_leistungspreis = ZuDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", brennstoffId));
            double default_co2 = ZuDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", brennstoffId));
            double default_so2 = ZuDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", brennstoffId));
            double default_nox = ZuDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", brennstoffId));

            int carrierId;

            // Punkt 1: Katalog-Träger, Preishistorie und Projekt-Einstellungen in EINER
            // Transaktion schreiben. Schlägt ein Insert fehl, macht Rollback alles rückgängig
            // – kein halbfertiger Zustand (Träger/Preis ohne zugehörige Settings).
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    // 1) Katalog-Träger suchen; existiert er, wird er wiederverwendet.
                    carrierId = -1;
                    {
                        object existing = v.Skalar("SELECT id FROM energy_carrier WHERE name = ?",
                                                   new DbParam("@name", variantenName));
                        if (existing != null && existing != DBNull.Value)
                            carrierId = Convert.ToInt32(existing);
                    }

                    // Katalog-Datensatz nur anlegen, wenn wirklich neu.
                    if (carrierId < 0)
                    {
                        var pTraeger = new List<DbParam>
                        {
                            new DbParam("@idB", brennstoffId),
                            new DbParam("@code", brennstoffName),
                            new DbParam("@name", variantenName),
                            new DbParam("@gc", daten.GroupCode),
                            new DbParam("@pm", daten.Code),
                            new DbParam("@unit", daten.BillingUnit),
                            new DbParam("@shi", daten.Hi),
                            new DbParam("@shs", daten.Hs),
                            new DbParam("@defap", default_arbeitspreis),
                            new DbParam("@defgp", default_grundpreis),
                            new DbParam("@co2", default_co2),
                            new DbParam("@so2", default_so2),
                            new DbParam("@nox", default_nox),
                            new DbParam("@active", DbParamTyp.Boolean) { Wert = true }
                        };
                        // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
                        // Verbindung des Vorgangs (frueher SELECT @@IDENTITY auf con/tx).
                        carrierId = v.EinfuegenUndId(
                            @"INSERT INTO energy_carrier
                                     (ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit,
                                      hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active)
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                            pTraeger.ToArray());
                    }

                    if (carrierId <= 0)
                    {
                        v.Rollback();
                        return new VariantenErgebnis(0, VariantenAnlage.Fehler,
                                                     Text("ETVAR_MSG_ANLAGE_FEHLER",
                                                          "Der Energieträger konnte nicht angelegt werden."));
                    }

                    // 1b) Wizard / kein echtes Projekt: nur der Katalog-Träger. energy_price
                    // und energy_Project_settings haben eine Beziehung auf Tab_Projekt.ID, die
                    // im Wizard noch nicht existiert -> die trägt WizardCtrl beim Speichern nach.
                    if (wizard || projektId <= 0)
                    {
                        v.Commit();
                        return new VariantenErgebnis(carrierId, VariantenAnlage.Vorgemerkt,
                                                     Text("ETVAR_MSG_VORGEMERKT",
                                                          "Energieträgervariante vorgemerkt. Die Preis- und " +
                                                          "Emissionssätze werden beim Speichern des Projekts angelegt."));
                    }

                    // 2) Ist der Träger diesem Projekt schon zugeordnet? -> nicht doppeln.
                    int vorhanden = Convert.ToInt32(v.Skalar(
                        "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                        new DbParam("@pid", projektId),
                        new DbParam("@eid", carrierId)));
                    if (vorhanden > 0)
                    {
                        // Träger existierte bereits und ist zugeordnet: nichts Neues
                        // geschrieben -> Transaktion sauber beenden, carrierId bleibt gültig.
                        v.Commit();
                        return new VariantenErgebnis(carrierId, VariantenAnlage.BereitsZugeordnet,
                            string.Format(Text("ETVAR_MSG_ZUGEORDNET",
                                               "Die Energieträgervariante '{0}' ist diesem Projekt bereits zugeordnet."),
                                          variantenName));
                    }

                    // 3) Projektbezogene Sätze anlegen (Preis-Historie + Projekt-Einstellungen).
                    v.Ausfuehren(@"INSERT INTO energy_price
                                     (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                        new DbParam("@cid", carrierId),
                        new DbParam("@prid", projektId),
                        new DbParam("@ap", Math.Round(default_arbeitspreis, 4)),
                        new DbParam("@hi", Math.Round(daten.Hi, 4)),
                        new DbParam("@gp", Math.Round(default_grundpreis, 4)),
                        new DbParam("@date", DbParamTyp.Date) { Wert = DateTime.Now },
                        new DbParam("@au", daten.BillingUnit),
                        new DbParam("@lp", Math.Round(default_leistungspreis, 4)));

                    v.Ausfuehren(@"INSERT INTO energy_Project_settings
                                     (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                                      custom_price_base, ID_Umrechnung, co2, so2, nox)
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        new DbParam("@pid", projektId),
                        new DbParam("@eid", carrierId),
                        new DbParam("@p", Math.Round(default_arbeitspreis, 4)),
                        new DbParam("@pl", Math.Round(default_leistungspreis, 4)),
                        new DbParam("@h", Math.Round(daten.Hi, 4)),
                        new DbParam("@hs", Math.Round(daten.Hs, 4)),
                        new DbParam("@b", Math.Round(default_grundpreis, 4)),
                        new DbParam("@convid", daten.ConvID),
                        new DbParam("@co2", default_co2),
                        new DbParam("@so2", default_so2),
                        new DbParam("@nox", default_nox));

                    v.Commit();
                    return new VariantenErgebnis(carrierId, VariantenAnlage.Angelegt,
                                                 Text("ETVAR_MSG_ANGELEGT",
                                                      "Energieträgervariante erfolgreich angelegt."));
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { /* Rollback darf den Originalfehler nicht verdecken */ }
                    return new VariantenErgebnis(0, VariantenAnlage.Fehler,
                        Text("ETVAR_MSG_SPEICHERFEHLER", "Fehler beim Speichern: ") + ex.Message);
                }
            }
        }

        /// <summary>
        /// Die Traegervarianten DERSELBEN Gruppe wie der uebergebene Traeger - die
        /// Auswahlliste <c>cmbBrennstoffArt</c> der beiden Projektdialoge.
        /// </summary>
        /// <remarks>
        /// Wortgleich <c>Form_Heizkessel.ApplySelectedKessel</c> (Z. 546-560) und
        /// <c>Form_BHKWEing.ApplySelectedBHKW</c> (Z. 381-395): erst die Gruppe des
        /// zugeordneten Traegers, dann alle Traeger dieser Gruppe nach Namen. Findet die
        /// erste Abfrage nichts, bleibt die Liste leer - dort setzten die Masken
        /// <c>DataSource = null</c>.
        /// </remarks>
        public static (string GroupCode, IReadOnlyList<(int Id, string Name)> Varianten)
            VariantenDerGruppe(int carrierId)
        {
            var liste = new List<(int Id, string Name)>();

            DataTable dtCar = DataRepository.GetDataTable(
                "SELECT name, group_code FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
            if (dtCar == null || dtCar.Rows.Count == 0) return (null, liste);

            string code = dtCar.Rows[0]["group_code"] == DBNull.Value
                        ? "" : dtCar.Rows[0]["group_code"].ToString();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT id, name FROM energy_carrier WHERE group_code = ? ORDER BY name",
                new DbParam("@gc", code));
            if (dt != null)
                foreach (DataRow row in dt.Rows)
                {
                    if (row["id"] == null || row["id"] == DBNull.Value) continue;
                    liste.Add((Convert.ToInt32(row["id"]),
                               row["name"] == DBNull.Value ? "" : row["name"].ToString()));
                }

            return (code, liste);
        }

        /// <summary>
        /// Haengt die Projektzuordnung von einem Traeger auf einen anderen um - der
        /// SOFORT schreibende Wechsel in <c>cmbBrennstoffArt</c>.
        /// </summary>
        /// <remarks>
        /// Wortgleich <c>cmbBrennstoffArt_SelectedIndexChanged</c> (Heizkessel Z. 589-614,
        /// BHKW Z. 912-939). Die Anweisung trifft KEINE Zeile, wenn der alte Traeger dem
        /// Projekt nicht zugeordnet war - das war im Bestand so und bleibt so: Der Wechsel
        /// gilt dann nur im Modell und wird beim Speichern des Projekts wirksam.
        /// </remarks>
        /// <returns><c>true</c>, wenn die Anweisung ohne Fehler lief.</returns>
        public static bool TraegerUmhaengen(int projektId, int alt, int neu)
        {
            try
            {
                DataRepository.ExecuteSQL(
                    "UPDATE energy_Project_settings " +
                    "SET ID_Energieträger = ? " +
                    "WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                    new DbParam[]
                    {
                        new DbParam("@neu", neu),         // SET-Wert
                        new DbParam("@pid", projektId),   // Filter Projekt
                        new DbParam("@alt", alt)          // Filter bisheriger Träger
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static double ZuDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }

        /// <summary>
        /// Ressourcentext mit deutschem Rueckfall (B5b-O4). Solange ein Schluessel im
        /// Katalog fehlt, steht der Bestandssatz da - kein leerer Hinweis.
        /// </summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}

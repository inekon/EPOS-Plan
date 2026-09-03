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
    }
}

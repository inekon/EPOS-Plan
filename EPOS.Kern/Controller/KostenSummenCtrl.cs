using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lesezugriffe rund um die Kostenseite, die MEHRERE Masken brauchen: die
    /// Kostenkategorien, die Energieträgerliste eines Projekts und die
    /// Investitions-/Betriebssummen je Komponente bzw. je Anlagenzeile.
    ///
    /// <para>
    /// <b>Warum es diese Klasse gibt.</b> Bis zur Stilllegung der Altmaske
    /// „Kostenverwaltung" (iU9-W0, Anwenderentscheid iF29) standen diese Bausteine als
    /// <c>static</c>-Mitglieder in <c>Views\Kosten\Form_Kosten.cs</c>. Die Maske hatte
    /// seit KD6a keinen Einstieg mehr — ihre Nachfolge sind die Seite
    /// <see cref="UcBkKosten"/> und der Dialog <c>Form_KostenKomponente</c> —, ihre
    /// Leselogik aber sehr wohl Aufrufer: die Energieträgerverwaltung, die
    /// Photovoltaik-Vergütung, der Wärmepumpen-Assistent und die Kostenseite selbst.
    /// Die Rümpfe sind deshalb <b>unverändert</b> hierher gezogen worden, damit die
    /// Stilllegung einer Maske keine Fachänderung ist.
    /// </para>
    ///
    /// <para>
    /// Der Platz im Kern ist der richtige: Es ist reiner Datenbankzugriff ohne jede
    /// Oberflächenberührung — dieselbe Begründung wie bei
    /// <see cref="KostenPositionCtrl"/>, mit dem diese Klasse sich die Schemavorsorge
    /// für <c>Tab_ProjektWerte.ID_Anlage</c> teilt.
    /// </para>
    ///
    /// <para>
    /// <b>ANWENDERBEFUND W5‑B‑7 (08.09.2026): die Summen kommen aus dem RECHENWEG,
    /// nicht mehr aus <c>SUM(EingegebenerWert)</c>.</b> Die drei Summenfunktionen
    /// summierten die Spalte roh — ohne Menge × Satz und ohne jede Prozentzeile. Die
    /// Anlagenzeile „Photovoltaik" der Kostenseite zeigte deshalb 1.500,00 € (nur der
    /// feste Wechselrichterbetrag), während die Kachel darüber und die
    /// Kapitalwertrechnung mit 6.961,80 € rechneten. Kategorie 1 fragt jetzt
    /// <see cref="InvestKaskade"/>, Kategorie 2 die Nachweisliste
    /// <c>WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen</c> — dieselben Zahlen
    /// wie die Kacheln. Zuschusszeilen bleiben wie in
    /// <c>LiesInvestitionen</c> außen vor; ihre GRUPPE bleibt bestehen, damit eine
    /// Komponente mit Positionen weiter als solche erkannt wird. Auf einer Datenbank
    /// ohne die Spalten aus Schritt 19 fällt alles auf das alte SQL zurück.
    /// </para>
    /// </summary>
    internal static class KostenSummenCtrl
    {
        // Kostenkategorien wie in Tab_KostenKategorie.
        //
        // Die Nummern selbst stehen seit iU3 (Views-Kante 1) bei DbWerte: Es sind
        // Persistenzwerte (KategorieID), und alle Nicht-View-Nutzer — Migration,
        // Wirtschaftlichkeit, Kosten-Controller — holen sie von dort. Hier bleiben sie
        // als Weiterleitung, weil die Kostenmasken sie unter diesen Namen benutzen.
        //
        // Kategorie 3 (Energie) ist mit der Altmaske entfallen: Sie wird seit dem
        // Konzept Kosten/Energieträger (HF1/L1, 19.08.2026) nicht mehr geschrieben und
        // hatte außerhalb von Form_Kosten keinen Leser mehr. Wer sie für einen
        // Migrationsschritt braucht, nimmt DbWerte.KOSTEN_KATEGORIE_ENERGIE direkt.
        internal const int KATEGORIE_INVESTITION = DbWerte.KOSTEN_KATEGORIE_INVESTITION;
        internal const int KATEGORIE_BETRIEB = DbWerte.KOSTEN_KATEGORIE_BETRIEB;

        // =====================================================================
        // Energieträger eines Projekts
        // =====================================================================

        /// <summary>
        /// Alle Energieträger, die dem Projekt zugeordnet sind — im KATALOGkontext
        /// (<paramref name="ID_Projekt"/> &lt;= 0) statt dessen der ganze Trägerkatalog.
        /// </summary>
        internal static List<EnergyCarrier> GetAllCarriers(int ID_Projekt)
        {
            List<EnergyCarrier> carriers = new List<EnergyCarrier>();

            string sql = @"SELECT
                            energy_project_settings.ID_Projekt,
                            ec.*,
                            pm.has_hi,
                            pm.has_hs,
                            pm.has_powerprice
                        FROM
                            energy_project_settings
                            INNER JOIN (
                                energy_carrier AS ec
                                LEFT JOIN
                                pricing_model AS pm ON ec.pricing_model = pm.code
                            ) ON energy_project_settings.ID_Energieträger = ec.id
                        WHERE energy_project_settings.ID_Projekt=?";

            DbParam[] ps = {
                new DbParam("@p", ID_Projekt),
            };

            // KD6a-Nachtrag (Befund 26.08.2026): Im KATALOGkontext (Projekt 0)
            // lieferte der Zuordnungs-Join eine leere Liste — die
            // Energieträgerverwaltung unter Administration blieb leer. Der
            // Katalog listet alle Träger direkt (der Mapper liest nur ec.* + Flags).
            if (ID_Projekt <= 0)
                sql = @"SELECT ec.*, pm.has_hi, pm.has_hs, pm.has_powerprice
                        FROM energy_carrier AS ec
                             LEFT JOIN pricing_model AS pm ON ec.pricing_model = pm.code
                        ORDER BY ec.name";

            DataTable dt = ID_Projekt <= 0
                ? DataRepository.GetDataTable(sql)
                : DataRepository.GetDataTable(sql, ps);

            foreach (DataRow row in dt.Rows)
            {
                carriers.Add(new EnergyCarrier
                {
                    ID = Convert.ToInt32(row["id"]),
                    Code = row["code"].ToString(),
                    Name = row["name"].ToString(),
                    GroupCode = row["group_code"].ToString(),
                    PricingModel = row["pricing_model"].ToString(),
                    BillingUnit = row["billing_unit"].ToString(),
                    HiKwhPerUnit = row["hi_kwh_per_unit"] != DBNull.Value ? Convert.ToDouble(row["hi_kwh_per_unit"]) : 0,
                    HsKwhPerUnit = row["hs_kwh_per_unit"] != DBNull.Value ? Convert.ToDouble(row["hs_kwh_per_unit"]) : 0,
                    ID_Brennstoff = Convert.ToInt32(row["id_brennstoff"]),
                    price_base = row["price_base"] != DBNull.Value ? Convert.ToDouble(row["price_base"]) : 0,
                    price_work = row["price_work"] != DBNull.Value ? Convert.ToDouble(row["price_work"]) : 0,
                    CO2 = row["co2"] != DBNull.Value ? Convert.ToDouble(row["co2"]) : 0,
                    SO2 = row["so2"] != DBNull.Value ? Convert.ToDouble(row["so2"]) : 0,
                    NOx = row["nox"] != DBNull.Value ? Convert.ToDouble(row["nox"]) : 0,
                    HasHi = row["has_hi"] != DBNull.Value ? Convert.ToBoolean(row["has_hi"]) : false,
                    HasHs = row["has_hs"] != DBNull.Value ? Convert.ToBoolean(row["has_hs"]) : false,
                    HasPowerPrice = row["has_powerprice"] != DBNull.Value ? Convert.ToBoolean(row["has_powerprice"]) : false
                });
            }
            return carriers;
        }

        // =====================================================================
        // Summen je Komponente / je Anlagenzeile
        // =====================================================================

        /// <summary>
        /// Summen je Komponente aus <c>Tab_ProjektWerte</c> — <b>getrennt nach Kategorie</b>
        /// (1 Investition, 2 Betrieb, 3 Energie). Spalten der Rückgabe: <c>Komponente</c>,
        /// <c>Summe</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Befund D1 (18.08.2026): Beide Aufrufer lasen zuvor die gespeicherte Abfrage
        /// <c>Abfrage_KostenKomponenten</c>. Die summiert <c>EingegebenerWert</c> nur über
        /// ProjektID und Komponente und filtert <b>nicht</b> nach <c>KategorieID</c> —
        /// Investitions-, Betriebs- und Energiepositionen derselben Komponente landeten in
        /// einer Zahl. Nachweis Projekt 1024: Wärmepumpe 6.100 € = 6.001 € (Investition) +
        /// 99 € (Betrieb), während die Investitions-Kachel der Kostenseite korrekt
        /// 12.001,00 € zeigte und die Tabelle darunter 12.100,00 €.
        /// </para>
        /// <para>
        /// Bewusst als eigenes parametrisiertes SQL statt einer Korrektur der gespeicherten
        /// Abfrage: Die Datenbank liegt außerhalb des Repos, eine Abfrageänderung erreicht
        /// Bestandsinstallationen nur über einen Migrationsschritt.
        /// </para>
        /// <para>
        /// Alle Anzeigen der Kostenlage — die Kompaktanzeige der Seite „Kosten"
        /// (<see cref="UcBkKosten"/>), die Photovoltaik-Vergütung und der
        /// Wärmepumpen-Assistent — verwenden dieselbe Leselogik, damit keine zweite
        /// entsteht; gleiche Begründung wie bei
        /// <see cref="WirtschaftlichkeitCtrl.LiesInvestitionen"/>.
        /// </para>
        /// </remarks>
        internal static DataTable LiesKomponentenSummen(int projektID, int kategorieID)
        {
            // W5‑B‑7: erst der Rechenweg, dann das alte SQL als Rückfall.
            Dictionary<KeyValuePair<int, int>, double> summen =
                Rechenwegsummen(projektID, kategorieID);
            if (summen != null)
            {
                Dictionary<int, string> namen = KomponentenNamen();
                var jeKomponente = new SortedDictionary<string, double>(StringComparer.Ordinal);
                foreach (KeyValuePair<KeyValuePair<int, int>, double> e in summen)
                {
                    string name;
                    if (!namen.TryGetValue(e.Key.Key, out name)) continue;   // wie der INNER JOIN
                    double alt;
                    jeKomponente.TryGetValue(name, out alt);
                    jeKomponente[name] = alt + e.Value;
                }

                var t = new DataTable();
                t.Columns.Add("Komponente", typeof(string));
                t.Columns.Add("Summe", typeof(double));
                foreach (KeyValuePair<string, double> e in jeKomponente)
                    t.Rows.Add(e.Key, e.Value);
                return t;
            }

            string sql = @"SELECT k.Komponente, Sum(w.EingegebenerWert) AS Summe
                           FROM Tab_KostenKomponente AS k
                                INNER JOIN Tab_ProjektWerte AS w ON k.ID = w.KomponentenID
                           WHERE w.ProjektID = ? AND w.KategorieID = ?
                           GROUP BY k.Komponente";

            return DataRepository.GetDataTable(sql,
                new DbParam("@pid", projektID),
                new DbParam("@kat", kategorieID));
        }

        /// <summary>Ä20: dieselbe Summe je ANLAGENZEILE (Spalten Komponente,
        /// ID_Anlage, Summe; ID_Anlage NULL = ohne Anlagenzuordnung). <c>null</c>,
        /// wenn die Spalte auf dieser Datenbank nicht anlegbar ist — der Aufrufer
        /// fällt dann auf die Komponentensummen zurück.</summary>
        internal static DataTable LiesAnlagenSummen(int projektID, int kategorieID)
        {
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa) return null;

            // W5‑B‑7: erst der Rechenweg, dann das alte SQL als Rückfall.
            Dictionary<KeyValuePair<int, int>, double> summen =
                Rechenwegsummen(projektID, kategorieID);
            if (summen != null)
            {
                Dictionary<int, string> namen = KomponentenNamen();
                var t = new DataTable();
                t.Columns.Add("Komponente", typeof(string));
                t.Columns.Add("ID_Anlage", typeof(int));
                t.Columns.Add("Summe", typeof(double));
                foreach (KeyValuePair<KeyValuePair<int, int>, double> e in summen)
                {
                    string name;
                    if (!namen.TryGetValue(e.Key.Key, out name)) continue;   // wie der INNER JOIN
                    // Anlage 0 = NULL in der Spalte: die „ohne Anlagenzuordnung"-Zeilen
                    // der Kostenseite prüfen genau auf DBNull.
                    t.Rows.Add(name,
                               e.Key.Value > 0 ? (object)e.Key.Value : DBNull.Value,
                               e.Value);
                }
                return t;
            }

            string sql = @"SELECT k.Komponente, w.ID_Anlage, Sum(w.EingegebenerWert) AS Summe
                           FROM Tab_KostenKomponente AS k
                                INNER JOIN Tab_ProjektWerte AS w ON k.ID = w.KomponentenID
                           WHERE w.ProjektID = ? AND w.KategorieID = ?
                           GROUP BY k.Komponente, w.ID_Anlage";

            return DataRepository.GetDataTable(sql,
                new DbParam("@pid", projektID),
                new DbParam("@kat", kategorieID));
        }

        /// <summary>
        /// Die Summe EINER Kostenkategorie fuer EINE Anlagenzeile (iU9-W7.0e) —
        /// woertlich aus <c>Wizard_WPItem.AnlagenSumme</c> (Z. 551-564).
        ///
        /// <para>Die Kostenzeile der Waermepumpenmaske zeigt damit „Invest … €" und
        /// „Betrieb … €/a" DIESER Anlage. Ohne die Spalte <c>ID_Anlage</c> — auf einer
        /// Datenbank vor Migrationsschritt 45 — liefert sie 0, genau wie der
        /// Vorlaeufer; die Zeile zeigt dann „Invest — · Betrieb —".</para>
        /// </summary>
        internal static double AnlagenSumme(int projektId, int kategorie, int anlageId)
        {
            if (projektId <= 0 || anlageId <= 0) return 0;

            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa) return 0;

            // W5‑B‑7: erst der Rechenweg, dann das alte SQL als Rückfall.
            Dictionary<KeyValuePair<int, int>, double> summen =
                Rechenwegsummen(projektId, kategorie);
            if (summen != null)
            {
                double s = 0;
                foreach (KeyValuePair<KeyValuePair<int, int>, double> e in summen)
                    if (e.Key.Value == anlageId) s += e.Value;
                return s;
            }

            object o = DataRepository.ExecuteScalar(
                "SELECT SUM(EingegebenerWert) FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KategorieID = ? AND ID_Anlage = ?",
                new DbParam("@p", projektId),
                new DbParam("@k", kategorie),
                new DbParam("@a", anlageId));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToDouble(o);
        }

        // =====================================================================
        // W5‑B‑7 — der eine Rechenweg hinter den drei Summenfunktionen
        // =====================================================================

        /// <summary>
        /// (Komponenten-Id, Anlagen-Id) → wirksamer Betrag [€ bzw. €/a] einer Kategorie;
        /// <c>null</c> = der Rechenweg hat nichts anzubieten (Datenbank ohne die
        /// Spalten aus Schritt 19, oder eine hier nicht geführte Kategorie) — dann
        /// gilt der Bestandsweg über <c>SUM(EingegebenerWert)</c>.
        /// <para>Anlagen-Id 0 = ohne (gültige) Anlagenzuordnung. Der Schlüssel entsteht
        /// für jede Zeile, auch für eine Zuschusszeile mit Beitrag 0.</para>
        /// </summary>
        private static Dictionary<KeyValuePair<int, int>, double> Rechenwegsummen(
            int projektID, int kategorieID)
        {
            try
            {
                if (kategorieID == DbWerte.KOSTEN_KATEGORIE_INVESTITION)
                    return InvestKaskade.Summen(projektID, WirtschaftlichkeitSzenario.ERWARTET);

                if (kategorieID == DbWerte.KOSTEN_KATEGORIE_BETRIEB)
                {
                    List<KostenPositionNachweis> zeilen =
                        WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(
                            projektID, WirtschaftlichkeitSzenario.ERWARTET);
                    // Leer heißt entweder „keine Positionen" oder „keine Spalten aus
                    // Schritt 19". Beide Male ist das alte SQL richtig: Es liefert
                    // dann entweder ebenfalls nichts oder den Bestandsweg.
                    if (zeilen == null || zeilen.Count == 0) return null;

                    var summen = new Dictionary<KeyValuePair<int, int>, double>();
                    foreach (KostenPositionNachweis n in zeilen)
                    {
                        var schluessel = new KeyValuePair<int, int>(n.Komponente, n.Anlage);
                        double alt;
                        summen.TryGetValue(schluessel, out alt);
                        summen[schluessel] = alt + n.BetragJahr;
                    }
                    return summen;
                }
            }
            catch { }
            return null;
        }

        /// <summary>W5‑B‑7: <c>Tab_KostenKomponente.ID</c> → Komponentenname — die
        /// Auflösung, die im alten SQL der INNER JOIN besorgte.</summary>
        private static Dictionary<int, string> KomponentenNamen()
        {
            var namen = new Dictionary<int, string>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Komponente FROM Tab_KostenKomponente");
                if (dt == null) return namen;
                foreach (DataRow r in dt.Rows)
                    if (r["ID"] != DBNull.Value)
                        namen[Convert.ToInt32(r["ID"])] = Convert.ToString(r["Komponente"]);
            }
            catch { }
            return namen;
        }
    }
}

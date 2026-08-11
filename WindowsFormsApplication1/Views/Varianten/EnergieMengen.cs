using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ermittelt aus den gespeicherten Simulationsergebnissen die Brennstoffmengen
    /// (in der Abrechnungseinheit, z. B. Liter) je Erzeuger (BHKW / Heizkessel).
    /// Umrechnung: Menge = Verbrauch[kWh] / effektiver Heizwert[kWh je Einheit].
    /// Heizwert + Einheit kommen aus Abfrage_Energietraeger_Effektiv (custom_hi/hs
    /// des Projekts mit Fallback auf den Katalog-Default), adressiert über carrier_id.
    /// Die Träger-id (energy_carrier.id) steht als FK in der Eingabetabelle des Erzeugers
    /// (Tab_BHKW.Brennstoff bzw. Kessel.Brennstoff) und wird per Bezeichner zugeordnet.
    /// </summary>
    public static class EnergieMengen
    {
        // >>> Falls die Kessel-Eingabetabelle anders heißt, hier anpassen: <<<
        private const string TAB_BHKW_INPUT   = "Tab_BHKW";
        private const string TAB_KESSEL_INPUT = "Tab_Heizkessel";

        // Baut die 3-Spalten-Tabelle (Erzeuger | Bezeichner | Menge) für ein Projekt.
        // useHs = true -> Brennwert (Hs), sonst Heizwert (Hi).
        public static DataTable BaueBrennstoffmengen(int projektId, bool useHs = false)
        {
            DataTable tab = new DataTable();
            tab.Columns.Add("Erzeuger", typeof(string));
            tab.Columns.Add("Bezeichner", typeof(string));
            tab.Columns.Add("Menge", typeof(string));

            ErgebnisModel m = new ErgebnisCtrl().Load(projektId);
            if (m == null) return tab;

            // Bezeichner -> Träger-id (energy_carrier.id) aus den Eingabetabellen
            Dictionary<string, int> bhkwCarrier   = CarrierIdMap(projektId, TAB_BHKW_INPUT);
            Dictionary<string, int> kesselCarrier = CarrierIdMap(projektId, TAB_KESSEL_INPUT);

            // BHKW – je Modul; Verbrauch (Brennstoffenergie) steht bereits im Modul.
            if (m.BHKW != null && m.BHKW.Module != null)
                foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module)
                    Zeile(tab, projektId, "BHKW", mo.Modul, CarrierFor(bhkwCarrier, mo.Modul), mo.Verbrauch, useHs);

            // Heizkessel – dominante Brennstoffenergie aus dem Aggregat, je Modul anteilig nach Wärme.
            if (m.Heizkessel != null)
            {
                double verb = DominanterVerbrauch(m.Heizkessel);

                if (m.Heizkessel.Module != null && m.Heizkessel.Module.Count > 0)
                {
                    double basis = 0;
                    foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module) basis += (mo.Waerme_Gas + mo.Waerme_Oel);
                    foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                    {
                        double anteil = basis > 0 ? (mo.Waerme_Gas + mo.Waerme_Oel) / basis : 1.0 / m.Heizkessel.Module.Count;
                        Zeile(tab, projektId, "Heizkessel", mo.Modul, CarrierFor(kesselCarrier, mo.Modul), verb * anteil, useHs);
                    }
                }
                else
                {
                    // ohne Modulliste: erster/einziger Kessel des Projekts
                    int cid = 0;
                    foreach (KeyValuePair<string, int> kv in kesselCarrier) { cid = kv.Value; break; }
                    Zeile(tab, projektId, "Heizkessel", "Spitzenkessel", cid, verb, useHs);
                }
            }

            return tab;
        }

        // Kernumrechnung: Menge (in billing_unit) aus dem Verbrauch (MWh -> kWh) über carrier_id.
        public static double Menge(int projektId, int carrierId, double verbrauchMWh, bool useHs, out string einheit)
        {
            einheit = "";
            if (verbrauchMWh <= 0 || carrierId <= 0) return 0;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT eff_hi, eff_hs, billing_unit FROM Abfrage_Energietraeger_Effektiv " +
                "WHERE ID_Projekt = ? AND carrier_id = ?",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", carrierId));
            if (dt == null || dt.Rows.Count == 0) return 0;

            DataRow r = dt.Rows[0];
            double heizwert = useHs ? ToD(r["eff_hs"]) : ToD(r["eff_hi"]);   // kWh je Einheit
            einheit = r["billing_unit"] != DBNull.Value ? r["billing_unit"].ToString() : "";
            return heizwert > 0 ? (verbrauchMWh * 1000.0) / heizwert : 0;
        }

        // ------------------------------------------------------------------ intern

        private static void Zeile(DataTable tab, int projektId, string erzeuger, string bezeichner,
                                  int carrierId, double verbrauchMWh, bool useHs)
        {
            string einheit;
            double menge = Menge(projektId, carrierId, verbrauchMWh, useHs, out einheit);
            tab.Rows.Add(erzeuger,
                         string.IsNullOrWhiteSpace(bezeichner) ? erzeuger : bezeichner,
                         menge > 0 ? string.Format("{0:N0} {1}", menge, einheit).Trim() : "–");
        }

        // Bezeichner -> Träger-id aus einer Erzeuger-Eingabetabelle (Spalte Brennstoff = FK auf energy_carrier.id).
        private static Dictionary<string, int> CarrierIdMap(int projektId, string tabelle)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Bezeichner, Brennstoff FROM " + tabelle + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", projektId));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        string bez = r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString().Trim() : "";
                        int cid = r["Brennstoff"] != DBNull.Value ? Convert.ToInt32(r["Brennstoff"]) : 0;
                        if (bez.Length > 0 && !map.ContainsKey(bez)) map[bez] = cid;
                    }
            }
            catch { /* Tabelle/Spalte ggf. anders benannt -> Menge bleibt dann 0 */ }
            return map;
        }

        private static int CarrierFor(Dictionary<string, int> map, string bezeichner)
        {
            int cid = 0;
            map.TryGetValue(bezeichner.Trim(), out cid);
            RecordSet rs = new RecordSet();
            rs.Open("select id from energy_carrier where id_brennstoff=" + cid);
            if (rs.Next()) cid = (int)rs.Read("id");
            rs.Close();
            return cid;
        }

        private static double DominanterVerbrauch(ErgebnisHeizkesselModel h)
        {
            double[] werte = { h.Gasverbrauch, h.Oelverbrauch, h.Pellets, h.Holzverbrauch, h.Kohle,
                               h.Koks, h.Rapsoelverbrauch, h.TierischeFette, h.Sonstigverbrauch, h.Stromverbrauch };
            double max = 0;
            foreach (double v in werte) if (v > max) max = v;
            return max;
        }

        private static double ToD(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }
    }
}

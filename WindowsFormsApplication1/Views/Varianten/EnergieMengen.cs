using System;
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
    /// Die carrier_id (energy_carrier.ID) hängt am MODUL: sie kommt aus
    /// Tab_Energieanlagen.ID_Carrier (gelesen von SimulationControl.EnergietraegerZuordnungLesen)
    /// und steht im Ergebnis als ErgebnisBHKWModulModel/ErgebnisHeizkesselModulModel.CarrierId.
    /// Sie steht NICHT in Tab_BHKW.Brennstoff bzw. Tab_Heizkessel.Brennstoff: dort liegt die
    /// Tab_Brennstoff_Stamm.ID (die Brennstoffart), nicht die energy_carrier.ID. Der Weg zurück
    /// ist auch nicht eindeutig - energy_carrier.ID_Brennstoff zeigt zwar auf
    /// Tab_Brennstoff_Stamm.ID, je Brennstoffart gibt es aber MEHRERE Trägerzeilen mit
    /// unterschiedlichem Hi/Hs und Preis. Ein Modul ohne Zuordnung (CarrierId 0) bleibt deshalb
    /// ohne Menge; das ist ein bewusst gemeldeter Datenzustand (Protokollwarnung in
    /// EnergietraegerZuordnungLesen, kostenVollstaendig = false in KostenEmissionRechner)
    /// und wird hier nicht geraten.
    /// </summary>
    public static class EnergieMengen
    {
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

            // BHKW – je Modul; Verbrauch (Brennstoffenergie) steht bereits im Modul.
            if (m.BHKW != null && m.BHKW.Module != null)
            { 
                foreach (ErgebnisBHKWModulModel mo in m.BHKW.Module)
                {
                    Zeile(tab, projektId, "BHKW", mo.Modul, mo.CarrierId, mo.Verbrauch, useHs);
                }
            }
            
            // Heizkessel – dominante Brennstoffenergie aus dem Aggregat, je Modul anteilig nach Wärme.
            if (m.Heizkessel != null && m.Heizkessel.Module != null)
            {
   
                foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                {
                    double verb = mo.Waerme_Gas + mo.Waerme_Oel;
                    Zeile(tab, projektId, "Heizkessel", mo.Modul, mo.CarrierId, verb , useHs);
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

        private static double ToD(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }
    }
}

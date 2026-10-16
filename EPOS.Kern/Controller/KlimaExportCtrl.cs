using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Datenseite des Klimaexports</b> (Auftrag KD-5): liest EINE Katalogregion (Entscheid KD-Q6) — Herkunft aus
    /// <c>Tab_Klimaregion_STAMM</c>, die Rechenspalten aus <c>Tab_Solar_STAMM</c> und, nur bei <c>Rohdaten = 1</c>, die
    /// Rohwerte aus <see cref="KlimaRohwerteSchema.TAB_ROH"/> (KD-Q4: Rohwerte gibt es nur im Katalog). Alle Texte mit
    /// <c>?</c>-Parametern. Die Herkunftsspalten des Schritts 238 werden nur gelesen, wenn sie stehen —
    /// darum <c>SELECT *</c> auf die Region; die Rohtabelle nur, wenn sie besteht (ein Datenstand vor der Migration).
    /// <see cref="RohwerteLesen"/> liefert allein die Rohwerte für den Reiter „Weitere Reihen“ (KD-8 Teil B).
    /// </summary>
    public static class KlimaExportCtrl
    {
        /// <summary>Die Spalten der Rechentabelle, die der Export führt (Katalog <see cref="KlimaExportOptionen.Groessen"/>).</summary>
        private const string SQL_RECHEN =
            "SELECT Temperatur, Luftfeuchte, Globalstrahlung, Direktstrahlung, Diffusstrahlung, Gegenstrahlung, " +
            "Bedeckungsgrad, Sol_Nord, Sol_Ost, Sol_Sued, Sol_West, Sonnenwinkel " +
            "FROM Tab_Solar_STAMM WHERE ID_Klimaregion = ? ORDER BY ID";

        private const string SQL_REGION = "SELECT * FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = ?";

        /// <summary>Die Rohtabelle des Schritts 238 (Spalten wie <see cref="KlimaRohwerteSchema.ROHSPALTEN"/>), über die Stunde geordnet.</summary>
        private const string SQL_ROH = "SELECT * FROM Tab_Klimastunde_STAMM WHERE ID_Klimaregion = ? ORDER BY Stunde";

        /// <summary>Die Daten einer Region über ihren Namen; <c>null</c>, wenn es sie im Katalog nicht gibt.</summary>
        public static KlimaExportDaten Lesen(string regionsname)
        {
            int id = KlimaregionStammCtrl.IdVonName(regionsname);
            return id > 0 ? Lesen(id) : null;
        }

        /// <summary>Die Daten einer Katalogregion; <c>null</c>, wenn es die Id nicht gibt.</summary>
        public static KlimaExportDaten Lesen(int idStamm)
        {
            DataTable region = DataRepository.GetDataTable(SQL_REGION, new DbParam("@id", idStamm));
            if (region == null || region.Rows.Count == 0) return null;
            DataRow r = region.Rows[0];

            var d = new KlimaExportDaten
            {
                IdRegion = idStamm,
                Name = Text(r, "Name"),
                Quelle = Text(r, "Quelle"),
                Szenario = Text(r, "Szenario"),
                Bezugsjahr = Ganz(r, "Bezugsjahr"),
                Latitude = Zahl(r, "Latitude"),
                Longitude = Zahl(r, "Longitude"),
                Ausfuehrung = Text(r, KlimaRohwerteSchema.SPALTE_AUSFUEHRUNG),
                StandortHoeheM = Zahl(r, KlimaRohwerteSchema.SPALTE_STANDORT_HOEHE),
                BezugspunktName = Text(r, KlimaRohwerteSchema.SPALTE_BEZUG_NAME),
                BezugspunktLatitude = Zahl(r, KlimaRohwerteSchema.SPALTE_BEZUG_LAT),
                BezugspunktLongitude = Zahl(r, KlimaRohwerteSchema.SPALTE_BEZUG_LON),
                BezugspunktHoeheM = Zahl(r, KlimaRohwerteSchema.SPALTE_BEZUG_HOEHE)
            };

            // Rechenspalten (UTC-Raster der Rechentabelle).
            DataTable rechen = DataRepository.GetDataTable(SQL_RECHEN, new DbParam("@id", idStamm));
            if (rechen != null && rechen.Rows.Count > 0)
                foreach (DataColumn c in rechen.Columns)
                    d.Rechenwerte[c.ColumnName] = Spalte(rechen, c.ColumnName);

            RohwerteEinlesen(d, r);
            return d;
        }

        /// <summary>
        /// Allein die Rohwerte einer Katalogregion (KD-8 Teil B, Reiter „Weitere Reihen“): <see cref="KlimaExportDaten.Rohdaten"/>,
        /// <see cref="KlimaExportDaten.RohZeitbezug"/> und <see cref="KlimaExportDaten.Rohwerte"/> — ohne die Rechenspalten, die
        /// die Hülle schon über <c>SolardatenCtrl</c> hält. <c>null</c>, wenn es die Id nicht gibt; ohne Rohtabelle oder mit
        /// <c>Rohdaten = 0</c> (Altbestand, KD-Q7) bleibt <see cref="KlimaExportDaten.Rohwerte"/> leer.
        /// </summary>
        public static KlimaExportDaten RohwerteLesen(int idStamm)
        {
            DataTable region = DataRepository.GetDataTable(SQL_REGION, new DbParam("@id", idStamm));
            if (region == null || region.Rows.Count == 0) return null;
            var d = new KlimaExportDaten { IdRegion = idStamm, Name = Text(region.Rows[0], "Name") };
            RohwerteEinlesen(d, region.Rows[0]);
            return d;
        }

        /// <summary>Zeitbezug und Rohwerte einer Region: nur mit Kennzeichen <c>Rohdaten = 1</c> UND bestehender Rohtabelle.</summary>
        private static void RohwerteEinlesen(KlimaExportDaten d, DataRow r)
        {
            string zb = Text(r, KlimaRohwerteSchema.SPALTE_ZEITBEZUG);
            if (zb.Length > 0) d.RohZeitbezug = zb;

            bool kennzeichen = Ganz(r, KlimaRohwerteSchema.SPALTE_ROHDATEN) == 1;
            if (!kennzeichen || !KlimaRohwerteSchema.RohtabelleVorhanden()) return;
            DataTable roh = DataRepository.GetDataTable(SQL_ROH, new DbParam("@id", d.IdRegion));
            if (roh == null || roh.Rows.Count == 0) return;
            foreach (string spalte in KlimaRohwerteSchema.ROHSPALTEN)
                if (roh.Columns.Contains(spalte))
                    d.Rohwerte[spalte] = RohSpalte(roh, spalte);
            d.Rohdaten = d.Rohwerte.Count > 0;
        }

        /// <summary>Eine Rechenspalte in Zeilenfolge; NULL = NaN.</summary>
        private static double[] Spalte(DataTable t, string spalte)
        {
            var w = new double[t.Rows.Count];
            for (int i = 0; i < w.Length; i++) w[i] = Zahl(t.Rows[i], spalte) ?? double.NaN;
            return w;
        }

        /// <summary>Eine Rohspalte, über <c>Stunde</c> 1 … 8760 eingeordnet; fehlende Stunden und NULL = NaN.</summary>
        private static double[] RohSpalte(DataTable t, string spalte)
        {
            var w = new double[KlimaCsv.STUNDEN];
            for (int i = 0; i < w.Length; i++) w[i] = double.NaN;
            foreach (DataRow z in t.Rows)
            {
                int? h = Ganz(z, KlimaRohwerteSchema.SPALTE_STUNDE);
                if (h is int s && s >= 1 && s <= KlimaCsv.STUNDEN) w[s - 1] = Zahl(z, spalte) ?? double.NaN;
            }
            return w;
        }

        private static string Text(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToString(r[spalte], CultureInfo.InvariantCulture) ?? "" : "";

        private static double? Zahl(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture) : (double?)null;

        private static int? Ganz(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value
                ? Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture) : (int?)null;
    }
}

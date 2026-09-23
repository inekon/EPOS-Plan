using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Datenseite des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 3.3, Stufe Z0: lesend).
    ///
    /// <para><b>Alle Zugriffe über <see cref="DataRepository"/> mit <c>?</c>-Parametern.</b>
    /// Tabellen- und Spaltennamen stammen aus <see cref="TwwSchema"/> bzw. aus Schleifen über
    /// feste Zahlen, nie aus einer Eingabe.</para>
    /// </summary>
    internal static class ZapfprofilCtrl
    {
        // =================================================================================
        // Parameter (Posten P5)
        // =================================================================================

        /// <summary>
        /// Die aktuelle Katalogversion der Parameter: die Version der zuletzt angelegten
        /// Parameterzeile (höchste <c>ID</c>). Die IDs sind <c>AUTOINCREMENT</c> und werden nie
        /// wieder vergeben (<see cref="TwwSchema"/>); eine neue Auslieferungsversion kommt als
        /// neue Zeilen (Konzept 3.2) und trägt deshalb die höchsten IDs. Die Textform der
        /// Version wird bewusst nicht geordnet — „V10" gegen „V9" hätte keine sichere Regel.
        /// <c>null</c>, wenn die Tabelle fehlt oder leer ist.
        /// </summary>
        internal static string AktuelleKatalogversion()
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PARAMETER_STAMM)) return null;

            object v = DataRepository.ExecuteScalar(
                "SELECT Katalogversion FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " " +
                "ORDER BY ID DESC LIMIT 1");
            return v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die gekapselten Parameter der aktuellen Katalogversion
        /// (<see cref="AktuelleKatalogversion"/>). Gibt es keine, die benannte Ablehnung
        /// <see cref="ParametersatzFehler.KeineKatalogversion"/> — kein Rückfallwert.
        /// </summary>
        internal static Parametersatz Parameter()
        {
            string version = AktuelleKatalogversion();
            if (version == null)
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, "", "",
                    "Nicht rechenbar — die Tabelle " + TwwSchema.TAB_TWW_PARAMETER_STAMM +
                    " fehlt oder trägt keine Katalogversion.");
            return Parameter(version);
        }

        /// <summary>
        /// Die Parameter einer bestimmten Katalogversion. Trägt die Version keine Zeile, die
        /// benannte Ablehnung <see cref="ParametersatzFehler.KatalogversionFehlt"/>.
        /// </summary>
        internal static Parametersatz Parameter(string katalogversion)
        {
            if (string.IsNullOrEmpty(katalogversion))
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, "", "",
                    "Nicht rechenbar — es wurde keine Katalogversion der Brauchwasserparameter genannt.");
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PARAMETER_STAMM))
                throw new ParametersatzException(ParametersatzFehler.KeineKatalogversion, katalogversion, "",
                    "Nicht rechenbar — die Tabelle " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " fehlt.");

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Schluessel, Wert, Einheit, Quelle, Ausgabe, Version, Herkunftsart " +
                "FROM " + TwwSchema.TAB_TWW_PARAMETER_STAMM + " WHERE Katalogversion = ? ORDER BY Schluessel",
                new DbParam("@version", katalogversion));

            var zeilen = new List<ZapfParameterwert>();
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    zeilen.Add(new ZapfParameterwert(
                        Text(r, "Schluessel"),
                        Zahl(r, "Wert"),
                        TextOderNull(r, "Einheit"),
                        Herkunft(r, "")));

            // Leere Menge -> KatalogversionFehlt (Parametersatz.Aus).
            return Parametersatz.Aus(katalogversion, zeilen);
        }

        // =================================================================================
        // Lesehilfen — die DataTable liefert je nach Spalte long, int, bool oder double
        // (SqliteDatenzugriff.LadeTabelle, Regeln D9); hier wird einmal umgesetzt.
        // =================================================================================

        /// <summary>Die Provenienz einer Wertgruppe; <paramref name="praefix"/> etwa „Bedarf_" oder leer.</summary>
        internal static Provenienz Herkunft(DataRow r, string praefix)
            => new Provenienz(
                Text(r, praefix + "Quelle"),
                TextOderNull(r, praefix + "Ausgabe"),
                Text(r, praefix + "Version"),
                TwwWertemengen.Herkunft(Text(r, praefix + "Herkunftsart")));

        internal static string Text(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? "" : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        internal static string TextOderNull(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        internal static double Zahl(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? 0.0 : Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        internal static double? ZahlOderNull(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? (double?)null : Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        internal static int Ganz(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        internal static int? GanzOderNull(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        internal static bool Wahr(DataRow r, string spalte)
        {
            object v = r[spalte];
            return v != null && v != DBNull.Value && Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0;
        }
    }
}

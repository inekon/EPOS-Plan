using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Jahreswärmebedarf eines Projektgebäudes aus dem letzten Rechenlauf (<see cref="GebaeudeJahresbedarfCtrl.Letzter"/>).
    /// </summary>
    /// <param name="HeizwaermeMwh">Der Heizwärmebedarf [MWh/a].</param>
    /// <param name="Zeitpunkt">Der Zeitstempel des Laufs (<c>Tab_Ergebnis.Zeitstempel</c>); <c>null</c> = nicht lesbar.</param>
    /// <param name="Projektwert">Der Lauf führt keine Zeile je Gebäude; der Wert ist der Heizwärmebedarf des Projekts
    /// (<c>Tab_ErgebnisEnergiebedarf.Waermebedarf_Heizung</c>, sonst <c>Waermebedarf_Gesamt</c>).</param>
    internal sealed record GebaeudeJahresbedarf(double HeizwaermeMwh, DateTime? Zeitpunkt, bool Projektwert);

    /// <summary>
    /// <b>Der Jahreswärmebedarf in der Zusammenfassung des Gebäudedialogs</b> (GD-N; Konzept Projektdialoge, Zeile
    /// „Jahreswärmebedarf in der Zusammenfassung“): liest den letzten Lauf des Projekts (<c>Tab_Ergebnis</c>, höchste ID) und
    /// darin die Zeile des Gebäudes in <c>Tab_ErgebnisGebaeude</c> (<c>Heizwaerme_Mwh</c>). Führt der Lauf keine Zeile je
    /// Gebäude (Gebäude ohne Gebäudemodell), steht der Projektwert mit Hinweis. Liest nur, über <see cref="DataRepository"/>
    /// mit <c>?</c>-Parametern; ohne Lauf <c>null</c> („noch nicht gerechnet“).
    /// </summary>
    internal static class GebaeudeJahresbedarfCtrl
    {
        private const string SQL_LETZTER_LAUF =
            "SELECT \"ID\", \"Zeitstempel\" FROM \"Tab_Ergebnis\" WHERE \"ID_Projekt\" = ? ORDER BY \"ID\" DESC LIMIT 1";

        private const string SQL_GEBAEUDE =
            "SELECT \"Heizwaerme_Mwh\" FROM \"" + ErgebnisGebaeudeSchema.TAB + "\" WHERE \"ID_Ergebnis\" = ? AND \"ID_Gebaeude\" = ?" +
            " ORDER BY \"Merkplatz\", \"ID\" LIMIT 1";

        private const string SQL_PROJEKT =
            "SELECT \"Waermebedarf_Heizung\", \"Waermebedarf_Gesamt\" FROM \"Tab_ErgebnisEnergiebedarf\" WHERE \"ID_Ergebnis\" = ?" +
            " ORDER BY \"ID\" LIMIT 1";

        /// <summary>
        /// Der Jahreswärmebedarf des Gebäudes im letzten Lauf des Projekts; <c>null</c> ohne Lauf, ohne Wert oder ohne
        /// Ergebnistabellen.
        /// </summary>
        /// <param name="idProjekt">Das Projekt (<c>Tab_Ergebnis.ID_Projekt</c>).</param>
        /// <param name="idGebaeude">Die Projektkopie (<c>Tab_Gebaeude.ID</c>); 0 = nur der Projektwert.</param>
        internal static GebaeudeJahresbedarf Letzter(int idProjekt, int idGebaeude)
        {
            if (idProjekt <= 0 || !DataRepository.TabelleVorhanden("Tab_Ergebnis")) return null;
            DataTable lauf = DataRepository.GetDataTable(SQL_LETZTER_LAUF, new DbParam("@p", idProjekt));
            if (lauf == null || lauf.Rows.Count == 0) return null;
            int idErgebnis = Convert.ToInt32(lauf.Rows[0]["ID"], CultureInfo.InvariantCulture);
            DateTime? zeitpunkt = Zeit(lauf.Rows[0]["Zeitstempel"]);

            if (idGebaeude > 0 && DataRepository.TabelleVorhanden(ErgebnisGebaeudeSchema.TAB))
            {
                DataTable g = DataRepository.GetDataTable(SQL_GEBAEUDE, new DbParam("@e", idErgebnis), new DbParam("@g", idGebaeude));
                if (g != null && g.Rows.Count > 0 && Zahl(g.Rows[0]["Heizwaerme_Mwh"]) is double mwh)
                    return new GebaeudeJahresbedarf(mwh, zeitpunkt, false);
            }
            if (!DataRepository.TabelleVorhanden("Tab_ErgebnisEnergiebedarf")) return null;
            DataTable p = DataRepository.GetDataTable(SQL_PROJEKT, new DbParam("@e", idErgebnis));
            if (p == null || p.Rows.Count == 0) return null;
            double? wert = Zahl(p.Rows[0]["Waermebedarf_Heizung"]) ?? Zahl(p.Rows[0]["Waermebedarf_Gesamt"]);
            return wert is double w ? new GebaeudeJahresbedarf(w, zeitpunkt, true) : null;
        }

        private static double? Zahl(object o)
        {
            if (o == null || o is DBNull) return null;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException) { return null; }
        }

        private static DateTime? Zeit(object o)
        {
            if (o == null || o is DBNull) return null;
            if (o is DateTime d) return d;
            return DateTime.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                                     DateTimeStyles.AllowWhiteSpaces, out DateTime t) ? t : null;
        }
    }
}

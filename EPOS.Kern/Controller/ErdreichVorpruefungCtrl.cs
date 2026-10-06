using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die AUSLEGUNGSWERTE der Wärmepumpen an einer Erdreichquelle für die Vorprüfung
    /// nach VDI 4640 Blatt 2 ohne Simulationslauf (<see cref="VDI4640Pruefung.Vorpruefung"/>).
    ///
    /// <para><b>Woher die Werte kommen.</b> Nennheizleistung und COP stammen aus der
    /// Kennlinie der Projektkopie (<c>Tab_WP</c> → <c>Tab_Kenndaten</c>) am Normpunkt
    /// ihrer Bauart bei 35 °C Vorlauf — Sole/Wasser B0/W35. Der Normpunkt wird über
    /// <see cref="TechnikPlanwertCtrl.WaermepumpeNormpunkt"/> gelesen, dieselbe Stelle,
    /// die auch die elektrische Leistung der Kostenplanung herleitet; fehlt die
    /// Stützstelle, wird zwischen den Nachbarn interpoliert, nie extrapoliert.
    /// <c>Tab_WP.Nennleistung</c> wird bewusst nicht genommen: Sie steht in keinem
    /// festen Verhältnis zur Heizleistung am Normpunkt.</para>
    ///
    /// <para><b>Mehrere Module</b> der Anlage ergeben mehrere Einträge; die Vorprüfung
    /// summiert ihre Entzugsleistungen. Ein Gerät ohne Normpunkt ergibt einen Eintrag
    /// mit Heizleistung und COP 0 — die Vorprüfung nennt es dann als fehlenden Wert.</para>
    ///
    /// <para><b>Fehlertolerant:</b> Ein Lesefehler ergibt eine leere Liste, kein Wurf.</para>
    /// </summary>
    internal static class ErdreichVorpruefungCtrl
    {
        /// <summary>
        /// Die Auslegungswerte der Wärmepumpen der Anlage <paramref name="idAnlage"/> im
        /// Projekt <paramref name="idProjekt"/>; leer, wenn keine Wärmepumpe zugeordnet ist.
        /// </summary>
        internal static List<VDI4640Pruefung.Auslegungswert> Auslegungswerte(int idProjekt, int idAnlage)
        {
            var liste = new List<VDI4640Pruefung.Auslegungswert>();
            if (idProjekt <= 0 || idAnlage <= 0) return liste;

            DataTable dt;
            try
            {
                dt = DataRepository.GetDataTable(
                    "SELECT w.ID, w.Typ, w.Bezeichner FROM Tab_WP AS w " +
                    "INNER JOIN Tab_Energieanlagen AS a ON w.ID = a.ID_WP " +
                    "WHERE a.ID_Projekt = ? AND a.ID = ? ORDER BY w.ID",
                    new DbParam("@p", idProjekt),
                    new DbParam("@a", idAnlage));
            }
            catch { return liste; }
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows)
            {
                int idWp = r["ID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                string typ = r["Typ"] == DBNull.Value ? "" : Convert.ToString(r["Typ"], CultureInfo.InvariantCulture);
                string name = r["Bezeichner"] == DBNull.Value ? "" : Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture);

                TechnikPlanwertCtrl.WpNormpunkt n = idWp > 0 ? TechnikPlanwertCtrl.WaermepumpeNormpunkt(idWp, typ) : null;
                liste.Add(new VDI4640Pruefung.Auslegungswert
                {
                    Modul = name ?? "",
                    NennheizleistungKw = n != null ? n.PthermKw : 0,
                    Cop = n != null ? n.Cop : 0,
                    Normpunkt = n != null ? (n.Interpoliert ? "≈ " : "") + n.Name : "",
                    // dieselbe Regel wie im Lauf (ErdreichAuswertung.IstLuftWasser)
                    LuftWasser = string.IsNullOrEmpty(typ) ||
                                 string.Equals(typ.Trim(), DbWerte.WP_BAUART_LUFT_WASSER, StringComparison.OrdinalIgnoreCase)
                });
            }
            return liste;
        }
    }
}

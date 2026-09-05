using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die beiden Kennlinienbilder EINER Waermepumpe (iU9-W7.0c) — COP und Leistung,
    /// je Vorlauftemperatur eine Reihe.
    ///
    /// <para><b>Warum beides zusammen.</b> Die Masken <c>Form_WP</c> und
    /// <c>Wizard_WPItem</c> zeigen sie als zwei Blaetter EINES Reiters und bauten sie
    /// aus DERSELBEN Abfrage auf. Getrennt zu lesen hiesse, die Stuetzstellen zweimal
    /// zu holen.</para>
    /// </summary>
    /// <param name="Cop">Die COP-Reihen (Blatt „COP").</param>
    /// <param name="Leistung">
    /// Die Leistungsreihen (Blatt „Leistung") — <c>Ptherm</c> im Waermebetrieb,
    /// <c>Pkuehl</c> im Kuehlbetrieb.
    /// </param>
    public sealed record KennlinienSatz(
        IReadOnlyList<ChartRenderer.KennlinienReihe> Cop,
        IReadOnlyList<ChartRenderer.KennlinienReihe> Leistung)
    {
        /// <summary>Ein Satz ohne Kennlinien — beide Blaetter zeigen dann den Platzhalter.</summary>
        public static readonly KennlinienSatz Leer = new KennlinienSatz(
            Array.Empty<ChartRenderer.KennlinienReihe>(),
            Array.Empty<ChartRenderer.KennlinienReihe>());

        /// <summary>Die Vorlaufstufen in Anzeigereihenfolge — die Auswahlliste der Masken.</summary>
        public IReadOnlyList<int> Vorlaeufe
        {
            get
            {
                var l = new List<int>();
                foreach (ChartRenderer.KennlinienReihe r in Cop) l.Add(r.Vorlauf);
                return l;
            }
        }

        /// <summary>
        /// Baut den Satz aus der Vorlaufliste und der Stuetzstellentabelle. Die Tabelle
        /// traegt die Spalten <c>Vorlauf</c>, <c>Temperatur</c>, <c>COP</c> und die
        /// Leistungsspalte, deren Name je Betriebsart wechselt.
        ///
        /// <para>Aufgeteilt wird ueber den Vorlauf, wie es der Vorlaeufer mit
        /// <c>DataTable.Select("Vorlauf=…")</c> tat. Eine Reihe OHNE Stuetzstellen
        /// bleibt dabei stehen — auch der Vorlaeufer legte fuer jede Vorlaufstufe eine
        /// (dann leere) Serie an, und die Legende zeigte sie.</para>
        /// </summary>
        internal static KennlinienSatz Bauen(IReadOnlyList<int> vorlaeufe, DataTable dt, string leistungsspalte)
        {
            var cop = new List<ChartRenderer.KennlinienReihe>();
            var leistung = new List<ChartRenderer.KennlinienReihe>();
            if (vorlaeufe == null) return Leer;

            foreach (int vorlauf in vorlaeufe)
            {
                var punkteCop = new List<(double, double)>();
                var punkteLeistung = new List<(double, double)>();

                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["Vorlauf"] == DBNull.Value || Convert.ToInt32(r["Vorlauf"]) != vorlauf) continue;
                        double temperatur = r["Temperatur"] != DBNull.Value ? Convert.ToDouble(r["Temperatur"]) : 0;
                        punkteCop.Add((temperatur, r["COP"] != DBNull.Value ? Convert.ToDouble(r["COP"]) : 0));
                        punkteLeistung.Add((temperatur,
                            r[leistungsspalte] != DBNull.Value ? Convert.ToDouble(r[leistungsspalte]) : 0));
                    }

                cop.Add(new ChartRenderer.KennlinienReihe(vorlauf, punkteCop));
                leistung.Add(new ChartRenderer.KennlinienReihe(vorlauf, punkteLeistung));
            }

            return new KennlinienSatz(cop, leistung);
        }
    }
}

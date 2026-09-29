using System;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Wohin ein Kern-Baustein seine Anweisungen schickt:</b> an die Datenbank des
    /// Anwenders (<see cref="Datenbank"/>, über <see cref="DataRepository"/>) oder an die
    /// Arbeitsdatenbank einer Paketanhebung (<see cref="Paketarbeitsdatenbank.Zugriff"/>).
    ///
    /// <para>Die Schemaschritte, die Projektwerte nicht mit EINER Anweisung umrechnen,
    /// sondern Zeile für Zeile entscheiden (Strompreis-Faltung, Vergütungsumzug), führen
    /// ihre Anweisungen über diesen Zugriff. So fährt die Paketanhebung dieselben
    /// Anweisungen wie die Migration — kein zweites Regelwerk (Konzept Projektpaket-Migration,
    /// Weg B).</para>
    /// </summary>
    internal sealed class Umformzugriff
    {
        internal Umformzugriff(Func<string, DbParam[], DataTable> lesen,
                               Func<string, DbParam[], int> ausfuehren,
                               Func<string, DbParam[], object> skalar,
                               Func<string, string, bool> spalteVorhanden)
        {
            _lesen = lesen ?? throw new ArgumentNullException(nameof(lesen));
            _ausfuehren = ausfuehren ?? throw new ArgumentNullException(nameof(ausfuehren));
            _skalar = skalar ?? throw new ArgumentNullException(nameof(skalar));
            _spalteVorhanden = spalteVorhanden ?? throw new ArgumentNullException(nameof(spalteVorhanden));
        }

        private readonly Func<string, DbParam[], DataTable> _lesen;
        private readonly Func<string, DbParam[], int> _ausfuehren;
        private readonly Func<string, DbParam[], object> _skalar;
        private readonly Func<string, string, bool> _spalteVorhanden;

        /// <summary>Die Datenbank des Anwenders — der Weg der Migration.</summary>
        internal static readonly Umformzugriff Datenbank = new Umformzugriff(
            DataRepository.GetDataTable, DataRepository.ExecuteNonQuery,
            DataRepository.ExecuteScalar, DataRepository.SpalteVorhanden);

        /// <summary>Eine Abfrage als Tabelle.</summary>
        internal DataTable Lesen(string sql, params DbParam[] parameter) => _lesen(sql, parameter);

        /// <summary>Eine Anweisung; liefert die Zahl der betroffenen Zeilen.</summary>
        internal int Ausfuehren(string sql, params DbParam[] parameter) => _ausfuehren(sql, parameter);

        /// <summary>Ein einzelner Wert.</summary>
        internal object Skalar(string sql, params DbParam[] parameter) => _skalar(sql, parameter);

        /// <summary>Führt die Tabelle diese Spalte?</summary>
        internal bool SpalteVorhanden(string tabelle, string spalte) => _spalteVorhanden(tabelle, spalte);

        /// <summary>Die größte Id der Tabelle, 0 ohne Zeilen.</summary>
        internal int GroessteId(string tabelle)
        {
            object o = Skalar("SELECT MAX(\"ID\") FROM \"" + tabelle + "\"");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

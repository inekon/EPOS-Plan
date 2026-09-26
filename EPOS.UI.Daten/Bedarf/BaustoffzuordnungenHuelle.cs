using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Ansicht „Baustoff-Zuordnungen…" im Gebäudedialog (Nacharbeit G4b): die
    /// Zuordnungen von Materialnamen zu Katalogbaustoffen, die sich ein Projekt beim Gebäudeimport gemerkt
    /// hat (Namensabgleich N7, <c>Tab_Baustoffzuordnung</c>), als Anzeigezeilen, und der Schreibweg für das
    /// Entfernen. Plattformfrei; die Datenbankseite liegt in <see cref="BaustoffabgleichCtrl"/>.
    /// </summary>
    internal static class BaustoffzuordnungenHuelle
    {
        /// <summary>Der Parametersatz der Ansicht für ein Projekt — die Zeilen werden dabei gelesen.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idProjekt) => new Dictionary<string, object>
        {
            ["Zeilen"] = Zeilen(idProjekt),
            ["Entfernen"] = new Func<IReadOnlyList<string>, string>(namen => Entfernen(idProjekt, namen)),
        };

        /// <summary>Die gemerkten Zuordnungen des Projekts als Anzeigezeilen, nach Materialname.</summary>
        internal static IReadOnlyList<BaustoffzuordnungZeile> Zeilen(int idProjekt)
            => BaustoffabgleichCtrl.GemerkteJeProjekt(idProjekt)
                                   .Select(z => new BaustoffzuordnungZeile(
                                       z.Materialname, z.Materialname, BaustoffText(z), ZeitpunktText(z.Zeitpunkt)))
                                   .ToList();

        /// <summary>
        /// Entfernt die Zuordnungen der Schlüssel in EINEM Vorgang (<see cref="BaustoffabgleichCtrl.Schreiben"/>
        /// mit „vergessen" je Name); <c>null</c> = geschrieben, sonst der Grund in der Anzeigekultur.
        /// </summary>
        internal static string Entfernen(int idProjekt, IReadOnlyList<string> namen)
        {
            if (namen == null || namen.Count == 0) return null;
            var vergessen = new Dictionary<string, int?>(StringComparer.Ordinal);
            foreach (string n in namen)
                if (!string.IsNullOrWhiteSpace(n)) vergessen[n] = null;
            BaustoffabgleichCtrl.Ergebnis e = BaustoffabgleichCtrl.Schreiben(idProjekt, vergessen);
            return e.Ok ? null : e.Meldung;
        }

        /// <summary>Der Baustoff einer Zuordnung als Anzeigetext; fehlt er im Katalog, seine Kennung mit Hinweis.</summary>
        private static string BaustoffText(BaustoffabgleichCtrl.GemerkteZuordnung z)
            => z.Baustoff != null
                ? GebaeudeZuordnungsModell.BaustoffText(z.Baustoff)
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BSZU_BAUSTOFF_FEHLT, z.IdBaustoff);

        /// <summary>
        /// Der Zeitpunkt (ISO 8601) in der Anzeigekultur, Datum und Uhrzeit; ein Zeitpunkt in UTC in
        /// Ortszeit. Was sich nicht lesen lässt, steht, wie es gespeichert ist.
        /// </summary>
        internal static string ZeitpunktText(string zeitpunkt)
        {
            if (string.IsNullOrWhiteSpace(zeitpunkt)) return "";
            if (!DateTime.TryParse(zeitpunkt.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime t))
                return zeitpunkt.Trim();
            if (t.Kind == DateTimeKind.Utc) t = t.ToLocalTime();
            return t.ToString("g", CultureInfo.CurrentCulture);
        }
    }
}

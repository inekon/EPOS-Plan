using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KATALOGKOSTEN INVESTITION (Konzept Projektdialoge mit Katalogauswahl, Abschnitt 7 Nr. 3 und 5.3; KA‑E‑14).
    //
    // WAS. Eine Spalte, nullbar, reines DDL: ID_KostenVorlageInvestition an den acht Katalogen mit Kosten
    // (dieselben wie bei KatalogkostenUrsprungSchema.KATALOGE_MIT_KOSTEN). Sie verweist auf die
    // INVESTITIONSvorlage des Satzes (Tab_KostenVorlage.ID, Kategorie Investition), ID_KostenVorlage auf seine
    // Betriebsvorlage. Eine Kostenvorlage fuehrt genau eine Kategorie; die Beziehung des Satzes zu seinen beiden
    // Vorlagen laeuft damit ueber IDs, nie ueber Name oder Bemerkung. Leer heisst: keine eigene
    // Investitionsvorlage.
    //
    // FREMDSCHLUESSEL wie ID_KostenVorlage: REFERENCES "Tab_KostenVorlage" ("ID") ON DELETE SET NULL.
    //
    // ERGEBNISNEUTRAL. Die Spalte entsteht leer; keine Katalogpruefsumme aendert sich (Metaspalte der
    // Katalogfassung), im Projektpaket reist sie nicht (Ziel Tab_KostenVorlage ist reiseloser Katalog).
    //
    // NUMMER. 210, angemeldet; haengt an 209 (KaeltemaschineTeillastSchema). Eingetragen in
    // SchemaStand.Zielversion, im Register der Paketanhebung, in der SchemaMigration der Schale, in
    // Werkzeuge/Testdatenbankschema und in EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>
    /// <b>Katalogkosten Investition</b> — <c>ID_KostenVorlageInvestition</c> an den acht Katalogen mit Kosten. Anlass
    /// und Bauform im Kopf der Datei.
    /// </summary>
    public static class KatalogkostenInvestitionSchema
    {
        /// <summary>
        /// Die Nummer des Schritts. Vorläufig, bis 209 (<c>KaeltemaschineTeillastSchema</c>) auf origin liegt — die
        /// Orchestrierung hängt um auf <c>KaeltemaschineTeillastSchema.SCHRITT + 1</c>.
        /// </summary>
        public const int SCHRITT = KatalogkostenUrsprungSchema.SCHRITT + 2;

        /// <summary>Die Spalte der Investitionsvorlage eines Katalogsatzes.</summary>
        public const string SPALTE_ID_KOSTENVORLAGE_INVESTITION = "ID_KostenVorlageInvestition";

        /// <summary>Die acht Spalten des Schritts: Tabelle, Spalte, Typ samt Verweis.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN =
            KatalogkostenUrsprungSchema.KATALOGE_MIT_KOSTEN
                .Select(t => (t, SPALTE_ID_KOSTENVORLAGE_INVESTITION,
                              "INTEGER REFERENCES \"" + KatalogkostenUrsprungSchema.TAB_KOSTENVORLAGE + "\" (\"ID\") ON DELETE SET NULL"))
                .ToArray();

        /// <summary>Die Tabellen, die stehen müssen, bevor der Schritt läuft.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => SPALTEN.Select(s => s.Tabelle).Append(KatalogkostenUrsprungSchema.TAB_KOSTENVORLAGE)
                      .Distinct(StringComparer.Ordinal).ToArray();

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Stehen alle Spalten des Schritts?</summary>
        public static bool Vollstaendig() => SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

        /// <summary>Führt der Katalog <paramref name="katalog"/> eine Investitionsvorlage des Satzes (Spalte vorhanden)?</summary>
        public static bool InvestitionsvorlageLesbar(string katalog)
            => DataRepository.SpalteVorhanden(katalog, SPALTE_ID_KOSTENVORLAGE_INVESTITION);

        /// <summary>
        /// Legt die fehlenden Spalten an — <b>wiederholbar</b>; ein zweiter Lauf ändert nichts. Gibt die Zahl der
        /// angelegten Spalten zurück.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            if (offen.Count == 0)
            {
                bericht?.Add("steht bereits - ID_KostenVorlageInvestition an den acht Katalogen; nichts zu tun");
                return 0;
            }

            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return offen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // PUFFERSPEICHER-AUSLEGUNG, ERGAENZUNGEN (Wellen P4c/P4d) - sechs nullbare Spalten und eine Saat:
    //
    // (1) Tab_PufferAuslegung: die Sitzungseingaben, die bisher nur in der Ansicht lebten -
    //     Kriterien_Aktiv (Bitmaske der Kriterienschalter in der Reihenfolge von
    //     PufferAuslegungVorgaben.VORLAGE_SCHALTER, Bit 0 = K1), Sperrzeit_Expertenweg (0/1),
    //     Auslegungsheizlast_kW, Wohneinheiten und Anzeigestufe. NULL = Vorgabe.
    // (2) Tab_Pufferspeicher.ID_Stamm: der Katalogsatz, aus dem die Pufferauslegung den Puffer
    //     uebernommen hat (Tab_Pufferspeicher_STAMM, ON DELETE SET NULL).
    // (3) Die Saat der Vorgaben des Aufheizkriteriums K12 (Welle P4d) in
    //     Tab_PufferAuslegungParameter_STAMM: INSERT ... WHERE NOT EXISTS jeder Zeile aus
    //     PufferAuslegungVorgaben.EINTRAEGE, die noch fehlt - dieselbe Quelle wie Schritt 169.
    //
    // KEIN VERWEIS AM GEBAEUDE (Anwenderentscheid 03.10.2026): Die Pufferauslegung liest die Nutzung
    // als Kopie aus Tab_Konditionierungskalender.Nutzung; ein Fremdschluessel auf die Vorlagentabelle
    // liesse Katalogabgleich und Vorlagenloeschen auf Projektdaten durchschlagen.
    //
    // ERGEBNISNEUTRAL: Alle Spalten entstehen leer; kein Referenzpuffer bekommt einen Wert, die
    // Simulation liest weder die Spalten noch die Vorgaben. Der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Ergänzungen der Pufferspeicher-Auslegung (Sitzungseingaben, Katalogverweis am Projektpuffer,
    /// Saat des Aufheizkriteriums) — EINE Quelle für Migration, Werkzeug, Testkopie und
    /// Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class PufferAuslegungErgaenzungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.
        /// </summary>
        public const int SCHRITT = ProzessNutzungSchema.SCHRITT + 1;

        /// <summary>Der Pufferkatalog.</summary>
        public const string TAB_PUFFER_STAMM = "Tab_Pufferspeicher_STAMM";

        /// <summary>Die Bitmaske der Kriterienschalter.</summary>
        public const string SPALTE_KRITERIEN = "Kriterien_Aktiv";

        /// <summary>Der Expertenweg der Sperrzeit (0/1).</summary>
        public const string SPALTE_EXPERTENWEG = "Sperrzeit_Expertenweg";

        /// <summary>Die Auslegungsheizlast der Sitzung [kW].</summary>
        public const string SPALTE_HEIZLAST = "Auslegungsheizlast_kW";

        /// <summary>Die Wohneinheiten der Sitzung.</summary>
        public const string SPALTE_WOHNEINHEITEN = "Wohneinheiten";

        /// <summary>Die Anzeigestufe der Ansicht.</summary>
        public const string SPALTE_ANZEIGESTUFE = "Anzeigestufe";

        /// <summary>Der Katalogverweis am Projektpuffer.</summary>
        public const string SPALTE_PUFFER_STAMM = "ID_Stamm";

        /// <summary>Die Werteliste der Anzeigestufe.</summary>
        public static readonly IReadOnlyList<string> ANZEIGESTUFEN = new[] { "SCHNELL", "STANDARD", "EXPERTE" };

        /// <summary>Die größte zulässige Bitmaske (alle Kriterienschalter an).</summary>
        public static int MASKE_MAX => (1 << PufferAuslegungVorgaben.VORLAGE_SCHALTER.Count) - 1;

        /// <summary>Die sechs Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel bzw. Fremdschlüssel.</summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (PufferAuslegungSchema.TAB, SPALTE_KRITERIEN,
             "INTEGER CHECK (\"" + SPALTE_KRITERIEN + "\" IS NULL OR (\"" + SPALTE_KRITERIEN + "\" >= 0 AND \"" +
             SPALTE_KRITERIEN + "\" <= " + ((1 << PufferAuslegungVorgaben.VORLAGE_SCHALTER.Count) - 1) + "))"),
            (PufferAuslegungSchema.TAB, SPALTE_EXPERTENWEG,
             "INTEGER CHECK (\"" + SPALTE_EXPERTENWEG + "\" IS NULL OR \"" + SPALTE_EXPERTENWEG + "\" IN (0,1))"),
            (PufferAuslegungSchema.TAB, SPALTE_HEIZLAST,
             "REAL CHECK (\"" + SPALTE_HEIZLAST + "\" IS NULL OR \"" + SPALTE_HEIZLAST + "\" >= 0)"),
            (PufferAuslegungSchema.TAB, SPALTE_WOHNEINHEITEN,
             "INTEGER CHECK (\"" + SPALTE_WOHNEINHEITEN + "\" IS NULL OR \"" + SPALTE_WOHNEINHEITEN + "\" >= 0)"),
            (PufferAuslegungSchema.TAB, SPALTE_ANZEIGESTUFE,
             "TEXT CHECK (\"" + SPALTE_ANZEIGESTUFE + "\" IS NULL OR \"" + SPALTE_ANZEIGESTUFE +
             "\" IN ('SCHNELL','STANDARD','EXPERTE'))"),
            (SchemaKatalog.TAB_PUFFERSPEICHER, SPALTE_PUFFER_STAMM,
             "INTEGER REFERENCES \"" + TAB_PUFFER_STAMM + "\" (\"ID\") ON DELETE SET NULL")
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return PufferAuslegungSchema.TAB;
            yield return PufferAuslegungSchema.TAB_PARAMETER;
            yield return SchemaKatalog.TAB_PUFFERSPEICHER;
            yield return TAB_PUFFER_STAMM;
        }

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht jede Spalte des Schritts und jede Saatzeile der Vorgabetabelle?</summary>
        public static bool Vollstaendig() =>
            SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)) &&
            PufferAuslegungSchema.SaatOffen() == 0;

        /// <summary>Steht die Spalte (für die Leser, die auf einem älteren Stand weiterlaufen)?</summary>
        public static bool Vorhanden(string tabelle, string spalte) => DataRepository.SpalteVorhanden(tabelle, spalte);

        /// <summary>
        /// Die DDL-Anweisungen des Schritts — je fehlende Spalte eine; leer, wenn alle stehen
        /// (<b>wiederholbar</b>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen =>
            SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))
                   .Select(s => new KeyValuePair<string, string>(s.Tabelle + "." + s.Spalte + " anlegen (leer)", Anlegen(s)))
                   .ToList();

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus: fehlende Spalten anlegen, dann die fehlenden Vorgaben
        /// säen (INSERT … WHERE NOT EXISTS) — für <c>SchemaMigration</c>, <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. <b>Wiederholbar.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Handgriffe: angelegte Spalten plus gesäte Vorgaben (0, wenn alles stand).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + SCHRITT + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            List<KeyValuePair<string, string>> ddl = Anweisungen.ToList();
            HashSet<string> da = PufferAuslegungSchema.GesaeteSchluessel();
            List<PufferVorgabe> offen = PufferAuslegungVorgaben.EINTRAEGE.Where(p => !da.Contains(p.Schluessel)).ToList();
            if (ddl.Count == 0 && offen.Count == 0)
            {
                bericht?.Add("Ergaenzungsspalten und Vorgaben der Pufferauslegung stehen bereits");
                return 0;
            }
            int eingefuegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                foreach (KeyValuePair<string, string> a in ddl)
                {
                    v.Ausfuehren(a.Value);
                    bericht?.Add(a.Key);
                }
                foreach (PufferVorgabe p in offen)
                    eingefuegt += v.Ausfuehren(PufferAuslegungSchema.SQL_SAAT,
                        new DbParam("@s", p.Schluessel),
                        new DbParam("@w", p.Wert),
                        new DbParam("@e", (object)p.Einheit ?? DBNull.Value),
                        new DbParam("@q", p.Quelle),
                        new DbParam("@h", p.Herkunftsart),
                        new DbParam("@s2", p.Schluessel));
                v.Commit();
            }
            if (eingefuegt > 0)
                bericht?.Add(PufferAuslegungSchema.TAB_PARAMETER + ": " +
                             eingefuegt.ToString(System.Globalization.CultureInfo.InvariantCulture) + " Vorgabe(n) gesät");
            return ddl.Count + eingefuegt;
        }
    }
}

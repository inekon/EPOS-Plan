using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // PUFFERSPEICHER-AUSLEGUNG, ERGAENZUNGEN (Welle P4c) - reines DDL, sieben nullbare Spalten:
    //
    // (1) Tab_PufferAuslegung: die Sitzungseingaben, die bisher nur in der Ansicht lebten -
    //     Kriterien_Aktiv (Bitmaske der Kriterienschalter in der Reihenfolge von
    //     PufferAuslegungVorgaben.VORLAGE_SCHALTER, Bit 0 = K1), Sperrzeit_Expertenweg (0/1),
    //     Auslegungsheizlast_kW, Wohneinheiten und Anzeigestufe. NULL = Vorgabe.
    // (2) Tab_Gebaeude.ID_Konditionierungsvorlage: die zuletzt uebernommene Konditionierungsvorlage
    //     (Katalog Tab_Konditionierungsvorlage_STAMM, ON DELETE SET NULL). Die Nutzungsprofil-
    //     Ableitung der Pufferauslegung liest zuerst sie, dann als Rueckfall die Bemerkung.
    // (3) Tab_Pufferspeicher.ID_Stamm: der Katalogsatz, aus dem die Pufferauslegung den Puffer
    //     uebernommen hat (Tab_Pufferspeicher_STAMM, ON DELETE SET NULL).
    //
    // ERGEBNISNEUTRAL: Alle Spalten entstehen leer; kein Referenzgebaeude und kein Referenzpuffer
    // bekommt einen Wert, die Simulation liest keine von ihnen. Der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Die Ergänzungsspalten der Pufferspeicher-Auslegung (Sitzungseingaben, Konditionierungsvorlage am
    /// Gebäude, Katalogverweis am Projektpuffer) — EINE Quelle für Migration, Werkzeug, Testkopie und
    /// Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class PufferAuslegungErgaenzungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.
        /// </summary>
        public const int SCHRITT = ProzessNutzungSchema.SCHRITT + 1;

        /// <summary>Die Gebäudetabelle (Projektkopie).</summary>
        public const string TAB_GEBAEUDE = "Tab_Gebaeude";

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

        /// <summary>Die zuletzt übernommene Konditionierungsvorlage am Projektgebäude.</summary>
        public const string SPALTE_KONDITIONIERUNGSVORLAGE = "ID_Konditionierungsvorlage";

        /// <summary>Der Katalogverweis am Projektpuffer.</summary>
        public const string SPALTE_PUFFER_STAMM = "ID_Stamm";

        /// <summary>Die Werteliste der Anzeigestufe.</summary>
        public static readonly IReadOnlyList<string> ANZEIGESTUFEN = new[] { "SCHNELL", "STANDARD", "EXPERTE" };

        /// <summary>Die größte zulässige Bitmaske (alle Kriterienschalter an).</summary>
        public static int MASKE_MAX => (1 << PufferAuslegungVorgaben.VORLAGE_SCHALTER.Count) - 1;

        /// <summary>Die sieben Spalten des Schritts: Tabelle, Spalte, Typ samt Prüfklausel bzw. Fremdschlüssel.</summary>
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
            (TAB_GEBAEUDE, SPALTE_KONDITIONIERUNGSVORLAGE,
             "INTEGER REFERENCES \"" + SchemaKatalog.TAB_KONDITIONIERUNGSVORLAGE_STAMM + "\" (\"ID\") ON DELETE SET NULL"),
            (SchemaKatalog.TAB_PUFFERSPEICHER, SPALTE_PUFFER_STAMM,
             "INTEGER REFERENCES \"" + TAB_PUFFER_STAMM + "\" (\"ID\") ON DELETE SET NULL")
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return PufferAuslegungSchema.TAB;
            yield return TAB_GEBAEUDE;
            yield return SchemaKatalog.TAB_KONDITIONIERUNGSVORLAGE_STAMM;
            yield return SchemaKatalog.TAB_PUFFERSPEICHER;
            yield return TAB_PUFFER_STAMM;
        }

        /// <summary>Die <c>ALTER TABLE … ADD COLUMN</c>-Anweisung einer Spalte.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        /// <summary>Steht jede Spalte des Schritts?</summary>
        public static bool Vollstaendig() =>
            SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte));

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
        /// Führt den Schritt in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b>, kein DML.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 7).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + SCHRITT + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            List<KeyValuePair<string, string>> ddl = Anweisungen.ToList();
            if (ddl.Count == 0)
            {
                bericht?.Add("Ergaenzungsspalten der Pufferauslegung stehen bereits");
                return 0;
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                foreach (KeyValuePair<string, string> a in ddl)
                {
                    v.Ausfuehren(a.Value);
                    bericht?.Add(a.Key);
                }
                v.Commit();
            }
            return ddl.Count;
        }
    }
}

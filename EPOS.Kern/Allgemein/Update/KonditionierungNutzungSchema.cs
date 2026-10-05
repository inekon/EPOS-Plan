using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KONDITIONIERUNGSNUTZUNG AN DER KALENDERKOPIE - Nachzug zu den Projektkopien der Kataloge
    // (ProjektkopienKatalogeSchema, Konzept Simulationsablauf Abschnitt 22).
    //
    // WARUM. „Vorlage übernehmen" kopiert den Inhalt einer Konditionierungsvorlage in den Kalender
    // des Projektgebäudes bzw. der Zone; die Herkunft steht nur als Text in Bemerkung. Die
    // Pufferauslegung belegte ihr Nutzungsprofil aber mit der Nutzung der VORLAGE vor - aufgelöst über
    // den Vorlagennamen aus Bemerkung gegen Tab_Konditionierungsvorlage_STAMM. Wurde die Vorlage
    // umbenannt, gelöscht oder ihre Nutzung vom Katalogabgleich geändert, änderte sich die Vorbelegung
    // eines Projekts. Die Kopie trägt die Nutzung nun selbst.
    //
    // WAS DER SCHRITT ANLEGT
    //   Tab_Konditionierungskalender  Nutzung  TEXT, nullbar, CHECK auf DbWerte.KOND_NUTZUNGEN
    //   (dieselbe Prüfklausel wie an Tab_Konditionierungsvorlage_STAMM). ADD COLUMN, kein Neubau:
    //   SQLite nimmt eine Prüfklausel in ADD COLUMN auch an einer STRICT-Tabelle an.
    //
    // DIE SAAT (einmalig, wiederholbar): je Kalenderzeile ohne Nutzung, deren Bemerkung eine Herkunft
    // nennt (Kalenderherkunft.AusBemerkung), die Nutzung der Vorlage gleichen Namens und gleicher
    // Größe; ohne Treffer bleibt die Zeile leer. Eine gesetzte Nutzung bleibt, wie sie ist.
    //
    // DANACH SCHREIBEN: „Vorlage übernehmen" setzt die Nutzung der Vorlage an der Kopie;
    // KonditionierungCtrl.KalenderSchreiben behält sie, solange die Herkunft dieselbe Vorlage nennt.
    // Duplizieren, Projektpaket und Katalogbau-Übernahme tragen die Spalte über ihre generischen
    // Spaltenlisten mit.
    //
    // ERGEBNISNEUTRAL. Der Lauf liest die Nutzung nicht; nur die Vorbelegung der Pufferauslegung.
    //
    // VIER LESER: die Schalenmigration, Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl: ein älteres Paket bringt die Nutzung nicht mit;
    // die Kalender bleiben leer, bis „Vorlage übernehmen" sie setzt).
    // ====================================================================================

    /// <summary>
    /// Der Schemaschritt der Konditionierungsnutzung an der Kalenderkopie — EINE Quelle für Migration,
    /// Werkzeug, Testkopie und Nachweis. Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KonditionierungNutzungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste Schritt
        /// hinter den Projektkopien der Kataloge (<see cref="ProjektkopienKatalogeSchema"/>, 175), also 176.
        /// </summary>
        public const int SCHRITT = ProjektkopienKatalogeSchema.SCHRITT + 1;

        /// <summary>Die Kalendertabelle der Konditionierung.</summary>
        public const string TAB_KALENDER = KonditionierungSchema.TAB_KALENDER;

        /// <summary>Der Auslieferungskatalog der Vorlagen (nur für die Saat).</summary>
        public const string TAB_VORLAGE = KonditionierungVorlagenSchema.TAB_VORLAGE;

        /// <summary>Die neue Spalte: die Nutzung der Vorlage, aus der der Kalender stammt; NULL = ohne.</summary>
        public const string SPALTE_NUTZUNG = KonditionierungVorlagenSchema.SPALTE_NUTZUNG;

        /// <summary>Spaltenzahl von <c>Tab_Konditionierungskalender</c> nach diesem Schritt (11 + 1).</summary>
        public const int SPALTENZAHL_KALENDER = KonditionierungSchema.SPALTENZAHL_KALENDER + 1;

        /// <summary>Typ samt Prüfklausel der Spalte — dieselbe Wertliste wie an der Vorlage.</summary>
        public static readonly string TYP =
            "TEXT CHECK (\"" + SPALTE_NUTZUNG + "\" IS NULL OR \"" + SPALTE_NUTZUNG + "\" IN (" +
            KonditionierungVorlagenSchema.WERTE_NUTZUNG + "))";

        /// <summary>Die Anweisung, die die Spalte anlegt.</summary>
        public static readonly string SQL_ANLEGEN =
            "ALTER TABLE \"" + TAB_KALENDER + "\" ADD COLUMN \"" + SPALTE_NUTZUNG + "\" " + TYP;

        /// <summary>Die Kalender ohne Nutzung, deren Bemerkung eine Herkunft nennen kann.</summary>
        internal const string SQL_OFFEN =
            "SELECT \"ID\", \"Groesse\", \"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
            "\" WHERE \"Nutzung\" IS NULL AND \"Bemerkung\" IS NOT NULL ORDER BY \"ID\"";

        /// <summary>Dieselben Kalender, beschränkt auf die Gebäude und Zonen der Projekte ab einer ID (Paketimport).</summary>
        internal const string SQL_OFFEN_AB_PROJEKT =
            "SELECT k.\"ID\", k.\"Groesse\", k.\"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
            "\" k JOIN \"Tab_Gebaeude\" g ON g.\"ID\" = k.\"ID_Gebaeude\" WHERE g.\"ID_Projekt\" > ? AND " +
            "k.\"Nutzung\" IS NULL AND k.\"Bemerkung\" IS NOT NULL ORDER BY k.\"ID\"";

        /// <summary>Die Nutzung einer Vorlage nach Name und Größe.</summary>
        internal const string SQL_VORLAGE_NUTZUNG =
            "SELECT \"Nutzung\" FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
            "\" WHERE \"Bezeichner\" = ? AND \"Groesse\" = ? AND \"Nutzung\" IS NOT NULL ORDER BY \"ID\" LIMIT 1";

        /// <summary>Setzt die Nutzung einer Kalenderzeile.</summary>
        internal const string SQL_SETZEN =
            "UPDATE \"" + KonditionierungSchema.TAB_KALENDER + "\" SET \"Nutzung\" = ? WHERE \"ID\" = ?";

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return TAB_KALENDER;
        }

        /// <summary>Steht die Spalte?</summary>
        public static bool SchemaVollstaendig() => DataRepository.SpalteVorhanden(TAB_KALENDER, SPALTE_NUTZUNG);

        /// <summary>Spalte vorhanden und jede auflösbare Herkunft gesät?</summary>
        public static bool Vollstaendig() => SchemaVollstaendig() && OffeneSaat() == 0;

        /// <summary>Steht die Spalte — gefragt im laufenden Vorgang (dieselbe Verbindung).</summary>
        internal static bool SpalteDa(DbVorgang v)
        {
            object n = v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = ?",
                                new DbParam("@t", TAB_KALENDER), new DbParam("@s", SPALTE_NUTZUNG));
            return n != null && n != DBNull.Value && Convert.ToInt64(n, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Die DDL des Schritts — leer, wenn die Spalte steht (<b>wiederholbar</b>).</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (DataRepository.TabelleVorhanden(TAB_KALENDER) && !SchemaVollstaendig())
                    yield return new KeyValuePair<string, string>(TAB_KALENDER + "." + SPALTE_NUTZUNG + " anlegen", SQL_ANLEGEN);
            }
        }

        /// <summary>
        /// Die Zuordnung der Saat: je Kalenderzeile ohne Nutzung die Nutzung der Vorlage, die ihre
        /// Bemerkung nennt (gleicher Name, gleiche Größe); Zeilen ohne Treffer fehlen.
        /// </summary>
        /// <param name="v">Der laufende Vorgang.</param>
        /// <param name="nachProjekt">Nur die Kalender der Projekte mit größerer ID; <c>null</c> = alle Kalender.</param>
        internal static List<KeyValuePair<long, string>> Zuordnung(DbVorgang v, long? nachProjekt = null)
        {
            var l = new List<KeyValuePair<long, string>>();
            if (!SpalteDa(v)) return l;
            object vorlagen = v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                                       new DbParam("@t", TAB_VORLAGE));
            if (vorlagen == null || vorlagen == DBNull.Value ||
                Convert.ToInt64(vorlagen, CultureInfo.InvariantCulture) == 0) return l;
            DataTable t = nachProjekt.HasValue
                ? v.Lese(SQL_OFFEN_AB_PROJEKT, new DbParam("@p", nachProjekt.Value))
                : v.Lese(SQL_OFFEN);
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
            {
                string nutzung = NutzungDerHerkunft(v, Convert.ToString(r["Bemerkung"], CultureInfo.InvariantCulture),
                                                    Convert.ToString(r["Groesse"], CultureInfo.InvariantCulture));
                if (nutzung == null) continue;
                l.Add(new KeyValuePair<long, string>(Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture), nutzung));
            }
            return l;
        }

        /// <summary>
        /// Die Nutzung der Vorlage, die die <paramref name="bemerkung"/> eines Kalenders der Größe
        /// <paramref name="groesse"/> als Herkunft nennt — dieselbe Regel wie die Saat dieses Schritts, damit
        /// ein geschriebener Kalender keinen Stand hinterlässt, den die Saat danach noch füllen müsste.
        /// <c>null</c> ohne Herkunft, ohne Vorlagentabelle oder ohne gültige Nutzung an der Vorlage.
        /// </summary>
        internal static string NutzungDerHerkunft(DbVorgang v, string bemerkung, string groesse)
        {
            string name = Kalenderherkunft.AusBemerkung(bemerkung).Vorlage;
            if (string.IsNullOrEmpty(name)) return null;
            object vorlagen = v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                                       new DbParam("@t", TAB_VORLAGE));
            if (vorlagen == null || vorlagen == DBNull.Value ||
                Convert.ToInt64(vorlagen, CultureInfo.InvariantCulture) == 0) return null;
            object n = v.Skalar(SQL_VORLAGE_NUTZUNG, new DbParam("@b", name), new DbParam("@g", groesse ?? ""));
            string nutzung = n == null || n == DBNull.Value ? null : Convert.ToString(n, CultureInfo.InvariantCulture);
            return Nutzungstext(nutzung);
        }

        /// <summary>
        /// <b>Die Nutzung als freier Text</b> (Konzept Nutzungsprofile NP-F15): getrimmt, 1 bis
        /// <see cref="RaumnutzungSchema.NUTZUNG_MAX_ZEICHEN"/> Zeichen — der Profilname oder eine der vier alten
        /// Kennungen (<see cref="DbWerte.KOND_NUTZUNGEN"/>), dieselbe Regel wie der <c>CHECK</c> der Spalte seit
        /// Schritt <see cref="RaumnutzungSchema.SCHRITT"/>. <c>null</c> für leer oder zu lang.
        /// </summary>
        public static string Nutzungstext(string nutzung)
        {
            string n = nutzung?.Trim();
            return string.IsNullOrEmpty(n) || n.Length > RaumnutzungSchema.NUTZUNG_MAX_ZEICHEN ? null : n;
        }

        /// <summary>Wie viele Kalenderzeilen die Saat noch füllen würde.</summary>
        public static int OffeneSaat()
        {
            if (!SchemaVollstaendig()) return 0;
            using (DbVorgang v = DataRepository.Vorgang())
                return Zuordnung(v).Count;
        }

        /// <summary>
        /// Die Saat des Schritts: die Nutzung der Herkunftsvorlage an jeder Kalenderzeile, die sie noch
        /// nicht trägt. Wiederholbar.
        /// </summary>
        /// <param name="bericht">Nimmt eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der gefüllten Kalenderzeilen.</returns>
        public static int Saat(IList<string> bericht) => Saat(bericht, null);

        /// <summary>
        /// Die Saat für die Projekte, die ein Paketimport angelegt hat (IDs größer als
        /// <paramref name="nachProjekt"/>): Ein Paket mit älterem Schemastand bringt die Nutzung nicht mit
        /// und bekommt sie aus der Herkunft in <c>Bemerkung</c>. Stehende Werte bleiben.
        /// </summary>
        public static int SaatNachImport(long nachProjekt) => Saat(null, nachProjekt);

        private static int Saat(IList<string> bericht, long? nachProjekt)
        {
            if (!SchemaVollstaendig()) return 0;
            int n = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                foreach (KeyValuePair<long, string> z in Zuordnung(v, nachProjekt))
                    n += v.Ausfuehren(SQL_SETZEN, new DbParam("@n", z.Value), new DbParam("@id", z.Key));
                v.Commit();
            }
            bericht?.Add(TAB_KALENDER + "." + SPALTE_NUTZUNG + ": " + n.ToString(CultureInfo.InvariantCulture) +
                         " Kalender aus der Herkunftsvorlage gefüllt");
            return n;
        }

        /// <summary>
        /// Führt den Schritt aus — DDL, dann die Saat — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen Helfer.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der DDL-Handgriffe.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in Anweisungen.ToList())
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_KALENDER + "." + SPALTE_NUTZUNG + " vorhanden");
            Saat(bericht);
            return n;
        }
    }
}

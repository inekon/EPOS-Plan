using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KATALOGFASSUNG UND ERDREICHPRÜFUNG IM ERGEBNIS - Welle M6 „Katalog-Update" der
    // Entscheidungsvorlage Modellgrenzen (KU1 Stufe 1, EQ1).
    //
    // WAS DER SCHRITT ANLEGT
    //   je Katalogtabelle der Stufe 1 (Katalogfassung.Stufe1):
    //     Katalog_Schluessel   TEXT     nullbar, Länge ≤ 120, eindeutig (Teilindex WHERE NOT NULL)
    //     Katalog_Pruefsumme   TEXT     nullbar, 64 Hexzeichen (SHA-256)
    //     Katalog_Ausgelaufen  INTEGER  NOT NULL DEFAULT 0 CHECK IN (0,1)
    //   Tab_Applikation.Katalogfassung  INTEGER  nullbar (leer = noch nie abgeglichen)
    //   Tab_Katalogabgleich   STRICT    Protokoll des Abgleichs (Zeitpunkt, Fassung, Tabelle,
    //                                   Schlüssel, Aktion, Hinweis)
    //   Tab_ErgebnisErdreich  STRICT    die Prüfung nach VDI 4640 je Lauf und Anlage (EQ1)
    //
    // DIE SAAT belegt Schlüssel und Prüfsumme der heute ausgelieferten Sätze (ReadOnly = 1, noch
    // ohne Schlüssel) - KEIN Fachwert ändert sich. Wiederholbar: Ein Satz mit Schlüssel wird
    // übergangen. Ein Anwendersatz (ReadOnly = 0) bekommt keinen Schlüssel.
    //
    // ERGEBNISNEUTRAL. Der Lauf liest Projektkopien, und die bleiben unberührt; die Erdreichtabelle
    // entsteht leer. Der Referenzlauf bleibt byte-gleich.
    //
    // DIE NAMEN STEHEN NUR ALS ARGUMENT (Muster ErzeugerTeillastSchema): Die Anweisungen entstehen
    // aus Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen ALTER-Text, der
    // gegen eine migrierte Datenbank „duplicate column" wäre.
    //
    // VIER LESER: die Schalenmigration (WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs),
    // Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die Paketanhebung.
    // ====================================================================================

    /// <summary>
    /// Der Schemaschritt der Katalogfassung (KU1 Stufe 1) und der gespeicherten Erdreichprüfung
    /// (EQ1) — EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis.
    /// </summary>
    public static class KatalogfassungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter den Teillastfeldern von Wärmepumpe und BHKW.
        /// </summary>
        public const int SCHRITT = ErzeugerTeillastSchema.SCHRITT + 1;

        /// <summary>Das Protokoll des Katalogabgleichs.</summary>
        public const string TAB_ABGLEICH = "Tab_Katalogabgleich";

        /// <summary>Die gespeicherte Erdreichprüfung je Lauf und Anlage (EQ1).</summary>
        public const string TAB_ERGEBNIS_ERDREICH = "Tab_ErgebnisErdreich";

        /// <summary>Die Aktionen des Protokolls (Prüfklausel von <c>Tab_Katalogabgleich.Aktion</c>).</summary>
        public static readonly IReadOnlyList<string> AKTIONEN = new[]
        {
            Katalogabgleich.AKTION_EINGEFUEGT, Katalogabgleich.AKTION_AKTUALISIERT,
            Katalogabgleich.AKTION_BEHALTEN, Katalogabgleich.AKTION_AUSGELAUFEN,
            Katalogabgleich.AKTION_WIEDERHERGESTELLT, Katalogabgleich.AKTION_KEIN_PAKET,
            Katalogabgleich.AKTION_BERICHT
        };

        private static string Aktionsliste() => string.Join(",", AKTIONEN.Select(a => "'" + a + "'"));

        /// <summary>Die drei Katalogspalten — Name und Typdefinition.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Katalogspalten()
        {
            yield return new KeyValuePair<string, string>(Katalogfassung.SPALTE_SCHLUESSEL,
                "TEXT CHECK (\"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NULL OR length(\"" +
                Katalogfassung.SPALTE_SCHLUESSEL + "\") BETWEEN 1 AND " +
                Katalogfassung.SCHLUESSEL_MAX.ToString(CultureInfo.InvariantCulture) + ")");
            yield return new KeyValuePair<string, string>(Katalogfassung.SPALTE_PRUEFSUMME,
                "TEXT CHECK (\"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" IS NULL OR length(\"" +
                Katalogfassung.SPALTE_PRUEFSUMME + "\") = 64)");
            yield return new KeyValuePair<string, string>(Katalogfassung.SPALTE_AUSGELAUFEN,
                "INTEGER NOT NULL DEFAULT 0 CHECK (\"" + Katalogfassung.SPALTE_AUSGELAUFEN + "\" IN (0,1))");
        }

        /// <summary>Der Name des Teilindex über den Schlüssel einer Tabelle.</summary>
        public static string Indexname(string tabelle) => "UX_" + tabelle + "_Katalog_Schluessel";

        /// <summary>Die Spalte der Programmfassung an <c>Tab_Applikation</c>.</summary>
        private static string FassungTyp() =>
            "INTEGER CHECK (\"" + Katalogfassung.SPALTE_FASSUNG + "\" IS NULL OR \"" +
            Katalogfassung.SPALTE_FASSUNG + "\" >= 0)";

        /// <summary>Das Protokoll des Abgleichs (STRICT).</summary>
        private static string AbgleichTabelle() =>
            "CREATE TABLE IF NOT EXISTS \"" + TAB_ABGLEICH + "\" (" +
            "\"ID\" INTEGER PRIMARY KEY, " +
            "\"Zeitpunkt\" TEXT NOT NULL, " +
            "\"Fassung\" INTEGER NOT NULL CHECK (\"Fassung\" >= 0), " +
            "\"Tabelle\" TEXT NOT NULL, " +
            "\"Schluessel\" TEXT, " +
            "\"Aktion\" TEXT NOT NULL CHECK (\"Aktion\" IN (" + Aktionsliste() + ")), " +
            "\"Hinweis\" TEXT) STRICT";

        /// <summary>Die gespeicherte Erdreichprüfung (STRICT), am Projekt und an der Anlage hängend.</summary>
        private static string ErdreichTabelle() =>
            "CREATE TABLE IF NOT EXISTS \"" + TAB_ERGEBNIS_ERDREICH + "\" (" +
            "\"ID\" INTEGER PRIMARY KEY, " +
            "\"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE, " +
            "\"ID_Anlage\" INTEGER NOT NULL REFERENCES \"Tab_Energieanlagen\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE, " +
            "\"Pruefzeile\" TEXT NOT NULL CHECK (length(\"Pruefzeile\") BETWEEN 1 AND 60), " +
            "\"Istwert\" REAL, " +
            "\"Grenzwert\" REAL, " +
            "\"Einheit\" TEXT, " +
            "\"Grundlage\" TEXT, " +
            "\"Hinweis\" TEXT, " +
            "\"Laufstempel\" TEXT NOT NULL) STRICT";

        private static string ErdreichIndex() =>
            "CREATE INDEX IF NOT EXISTS \"IX_" + TAB_ERGEBNIS_ERDREICH + "_Projekt\" ON \"" +
            TAB_ERGEBNIS_ERDREICH + "\" (\"ID_Projekt\", \"ID_Anlage\")";

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            foreach (Katalogtabelle t in Katalogfassung.Stufe1) yield return t.Tabelle;
            yield return Katalogfassung.TAB_APPLIKATION;
            yield return "Tab_Projekt";
            yield return "Tab_Energieanlagen";
        }

        /// <summary>Steht der Teilindex einer Tabelle?</summary>
        private static bool IndexVorhanden(string tabelle)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                new DbParam("@n", Indexname(tabelle)));
            return o != null && o != DBNull.Value && Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Steht das Schema des Schritts vollständig (ohne die Saat)?</summary>
        public static bool SchemaVollstaendig()
        {
            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                if (!Katalogfassung.SpaltenVorhanden(t.Tabelle)) return false;
                if (!IndexVorhanden(t.Tabelle)) return false;
            }
            return DataRepository.SpalteVorhanden(Katalogfassung.TAB_APPLIKATION, Katalogfassung.SPALTE_FASSUNG) &&
                   DataRepository.TabelleVorhanden(TAB_ABGLEICH) &&
                   DataRepository.TabelleVorhanden(TAB_ERGEBNIS_ERDREICH);
        }

        /// <summary>Schema vollständig und jeder ausgelieferte Satz mit Schlüssel und Prüfsumme?</summary>
        public static bool Vollstaendig() => SchemaVollstaendig() && KatalogSchluesselSaat.OffeneSaetze() == 0;

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL, je fehlende Spalte, fehlenden Index
        /// und fehlende Tabelle eine; leer, wenn alles steht (<b>wiederholbar</b>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                foreach (Katalogtabelle t in Katalogfassung.Stufe1)
                {
                    if (!DataRepository.TabelleVorhanden(t.Tabelle)) continue;
                    foreach (KeyValuePair<string, string> s in Katalogspalten())
                    {
                        if (DataRepository.SpalteVorhanden(t.Tabelle, s.Key)) continue;
                        yield return new KeyValuePair<string, string>(
                            t.Tabelle + "." + s.Key + " anlegen",
                            "ALTER TABLE \"" + t.Tabelle + "\" ADD COLUMN \"" + s.Key + "\" " + s.Value);
                    }
                }
                // Die Indizes nach den Spalten: Ein Index ueber eine fehlende Spalte schluege fehl.
                foreach (Katalogtabelle t in Katalogfassung.Stufe1)
                {
                    if (!DataRepository.TabelleVorhanden(t.Tabelle)) continue;
                    if (IndexVorhanden(t.Tabelle) && DataRepository.SpalteVorhanden(t.Tabelle, Katalogfassung.SPALTE_SCHLUESSEL))
                        continue;
                    yield return new KeyValuePair<string, string>(
                        Indexname(t.Tabelle) + " anlegen",
                        "CREATE UNIQUE INDEX IF NOT EXISTS \"" + Indexname(t.Tabelle) + "\" ON \"" + t.Tabelle +
                        "\" (\"" + Katalogfassung.SPALTE_SCHLUESSEL + "\") WHERE \"" +
                        Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NOT NULL");
                }
                if (!DataRepository.SpalteVorhanden(Katalogfassung.TAB_APPLIKATION, Katalogfassung.SPALTE_FASSUNG))
                    yield return new KeyValuePair<string, string>(
                        Katalogfassung.TAB_APPLIKATION + "." + Katalogfassung.SPALTE_FASSUNG + " anlegen",
                        "ALTER TABLE \"" + Katalogfassung.TAB_APPLIKATION + "\" ADD COLUMN \"" +
                        Katalogfassung.SPALTE_FASSUNG + "\" " + FassungTyp());
                if (!DataRepository.TabelleVorhanden(TAB_ABGLEICH))
                    yield return new KeyValuePair<string, string>(TAB_ABGLEICH + " anlegen", AbgleichTabelle());
                if (!DataRepository.TabelleVorhanden(TAB_ERGEBNIS_ERDREICH))
                {
                    yield return new KeyValuePair<string, string>(TAB_ERGEBNIS_ERDREICH + " anlegen", ErdreichTabelle());
                    yield return new KeyValuePair<string, string>(TAB_ERGEBNIS_ERDREICH + ": Index anlegen", ErdreichIndex());
                }
            }
        }

        /// <summary>
        /// Führt den Schritt aus — DDL, dann die Saat — für <c>Werkzeuge/Testdatenbankschema</c>,
        /// <c>Werkzeuge/Auslieferungsvorlage</c> und <c>EPOS.Kern.Tests</c>; die Migration der
        /// Schale geht denselben Weg über ihre eigenen Helfer, aus derselben <see cref="Anweisungen"/>
        /// und mit derselben <see cref="KatalogSchluesselSaat"/>.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der DDL-Handgriffe.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in new List<KeyValuePair<string, string>>(Anweisungen))
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add("Katalogspalten, Tab_Katalogabgleich und Tab_ErgebnisErdreich vorhanden");
            KatalogSchluesselSaat.Ausfuehren(bericht);
            return n;
        }
    }

    /// <summary>
    /// <b>Die Saat des Schemaschritts</b>: Schlüssel und Prüfsumme je ausgelieferten Satz der Stufe 1,
    /// der noch keinen trägt. Kein Fachwert ändert sich; ein Anwendersatz bleibt ohne Schlüssel.
    /// </summary>
    public static class KatalogSchluesselSaat
    {
        /// <summary>Zahl der ausgelieferten Sätze ohne Schlüssel oder Prüfsumme (0 = Saat vollständig).</summary>
        public static int OffeneSaetze()
        {
            int n = 0;
            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                if (!DataRepository.TabelleVorhanden(t.Tabelle)) continue;
                if (!Katalogfassung.SpaltenVorhanden(t.Tabelle)) return int.MaxValue;
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + t.Tabelle + "\" WHERE \"ReadOnly\" = 1 AND (\"" +
                    Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NULL OR \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" IS NULL)");
                n += o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return n;
        }

        /// <summary>
        /// Belegt je Tabelle in EINEM Vorgang Schlüssel und Prüfsumme der ausgelieferten Sätze ohne
        /// Schlüssel, in der Folge ihrer ID. <b>Wiederholbar.</b> Fehler werfen.
        /// </summary>
        /// <returns>Die Zahl der belegten Sätze.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int gesamt = 0;
            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                if (!DataRepository.TabelleVorhanden(t.Tabelle) || !Katalogfassung.SpaltenVorhanden(t.Tabelle)) continue;
                List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
                int n = 0;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        var belegt = new HashSet<string>(StringComparer.Ordinal);
                        DataTable vorhanden = v.Lese("SELECT \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" FROM \"" + t.Tabelle +
                                                    "\" WHERE \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NOT NULL");
                        if (vorhanden != null)
                            foreach (DataRow r in vorhanden.Rows) belegt.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));

                        DataTable offen = v.Lese("SELECT ID, \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\", " +
                                                 Katalogfassung.Spaltentext(spalten) + " FROM \"" + t.Tabelle +
                                                 "\" WHERE \"ReadOnly\" = 1 AND (\"" + Katalogfassung.SPALTE_SCHLUESSEL +
                                                 "\" IS NULL OR \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" IS NULL) ORDER BY ID");
                        if (offen != null)
                        {
                            foreach (DataRow r in offen.Rows)
                            {
                                string schluessel = r[Katalogfassung.SPALTE_SCHLUESSEL] as string;
                                if (string.IsNullOrEmpty(schluessel))
                                {
                                    schluessel = Katalogfassung.Schluessel(t, Convert.ToString(r[Katalogfassung.SPALTE_BEZEICHNER],
                                                                           CultureInfo.InvariantCulture), belegt.Contains);
                                    belegt.Add(schluessel);
                                }
                                string summe = Katalogfassung.PruefsummeDerZeile(t, spalten, r, (sql, p) => v.Lese(sql, p));
                                v.Ausfuehren("UPDATE \"" + t.Tabelle + "\" SET \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\" = ?, \"" +
                                             Katalogfassung.SPALTE_PRUEFSUMME + "\" = ? WHERE ID = ?",
                                             new DbParam("@s", schluessel), new DbParam("@p", summe),
                                             new DbParam("@id", Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture)));
                                n++;
                            }
                        }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                gesamt += n;
                if (n > 0) bericht?.Add(t.Tabelle + ": " + n.ToString(CultureInfo.InvariantCulture) +
                                        " ausgelieferte(r) Satz/Saetze mit Schluessel und Pruefsumme");
            }
            if (gesamt == 0) bericht?.Add("Schluessel und Pruefsummen der ausgelieferten Saetze stehen");
            return gesamt;
        }
    }
}

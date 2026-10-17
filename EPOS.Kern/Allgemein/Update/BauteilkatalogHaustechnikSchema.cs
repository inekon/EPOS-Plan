using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BK-3c - KLASSE HAUSTECHNIK IM BAUTEILKATALOG (Entscheid E134, Anwender 12.10.2026).
    //
    // WAS. Die Klassenlisten der Baustoffe und der Bauteilaufbauten bekommen in der Gruppe "Sonstige" die Klasse HAUSTECHNIK
    // (Werkstoffe und Zubehoer der Haustechnik, Basis-Codes 14, 19, 29, 30 der Fremdquelle; ueber den Klassenfilter
    // ausblendbar). Die Klasse steht als Wert in der Pruefklausel von Klasse_Code an Tab_Baustoff(_STAMM) und
    // Tab_Bauteilaufbau(_STAMM); SQLite kann einer CHECK-Klausel nichts nachtragen -> Neubau der vier Tabellen nach dem Rezept
    // des Hauses (TwwBezugsartSchema.Neubau: IDs, AUTOINCREMENT-Stand, Indizes, Trigger, Fremdschluessel der Kinder bleiben,
    // STRICT bleibt). Die Zeilen bleiben unveraendert; keine Pruefsumme aendert sich.
    //
    // EIN WEG FUER ALLE STAENDE. Die Listen von BauteilkatalogSchema fuehren die Klasse; ein Stand vor 221 bekommt sie deshalb
    // schon mit Schritt 221 (ADD COLUMN mit der neuen Klausel), und dieser Schritt findet nichts zu tun. Ein Stand 221 traegt
    // die Klausel ohne HAUSTECHNIK; sie wird getauscht.
    //
    // ERGEBNISNEUTRAL. Der Bauteilweg liest keine Gliederung.
    // ====================================================================================

    /// <summary>
    /// <b>BK-3c</b> — Klasse Haustechnik in der Prüfklausel von <c>Klasse_Code</c> an Baustoff- und Aufbaukatalog samt
    /// Projektkopien (E134). EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C).
    /// </summary>
    public static class BauteilkatalogHaustechnikSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (222, hängt an 221).</summary>
        public const int SCHRITT = BauteilkatalogSchema.SCHRITT + 1;

        /// <summary>Die neue Klasse (Persistenzwert, eingefroren) in der Gruppe „Sonstige“ beider Kataloge.</summary>
        public const string KLASSE = "HAUSTECHNIK";

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die vier Tabellen mit Klassenspalte — Katalog vor Kopie, Stoffe vor Aufbauten.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => BauteilkatalogSchema.KOPFTABELLEN;

        private static bool Baustoff(string tabelle) =>
            tabelle == BauteilkatalogSchema.TAB_BAUSTOFF_STAMM || tabelle == BauteilkatalogSchema.TAB_BAUSTOFF;

        private static IEnumerable<string> Klassen(string tabelle) =>
            (Baustoff(tabelle) ? BauteilkatalogSchema.BAUSTOFF_KLASSEN : BauteilkatalogSchema.AUFBAU_KLASSEN).Select(k => k.Klasse);

        /// <summary>Die Klassenklausel einer Tabelle nach dem Schritt (mit Haustechnik).</summary>
        public static string KlauselNeu(string tabelle) =>
            BauteilkatalogSchema.GliederungKlausel(BauteilkatalogSchema.SPALTE_KLASSE, Klassen(tabelle));

        /// <summary>Die Klassenklausel einer Tabelle vor dem Schritt (Stand 221, ohne Haustechnik).</summary>
        public static string KlauselAlt(string tabelle) =>
            BauteilkatalogSchema.GliederungKlausel(BauteilkatalogSchema.SPALTE_KLASSE, Klassen(tabelle).Where(k => k != KLASSE));

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        public static string Hilfsname(string tabelle) => tabelle + "_vor_Haustechnik";

        /// <summary>
        /// <b>Der Zieltext einer Tabelle</b>: der GELTENDE <c>sqlite_master</c>-Text mit getauschter Klassenklausel; <c>null</c>,
        /// wenn sie die neue Klausel schon trägt. Fehlt die Klassenspalte (Schritt 221 nicht gelaufen), trägt sie eine
        /// unbekannte Klausel oder ist sie nicht <c>STRICT</c>, bricht der Aufruf ab, statt eine falsche Tabelle anzulegen.
        /// </summary>
        public static string Zieltext(string tabelle, string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            string neu = KlauselNeu(tabelle), alt = KlauselAlt(tabelle);
            if (bestand.Contains(neu, StringComparison.Ordinal)) return null;
            if (!bestand.TrimEnd().EndsWith(") STRICT", StringComparison.Ordinal))
                throw new InvalidOperationException(tabelle + " ist keine STRICT-Tabelle - der Schritt " + Nr + " baut sie nicht um.");
            int stelle = bestand.IndexOf(alt, StringComparison.Ordinal);
            if (stelle < 0)
                throw new InvalidOperationException(tabelle + " traegt keine bekannte Klassenklausel (Schritt " +
                                                    BauteilkatalogSchema.SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                    " nicht gelaufen?) - der Schritt " + Nr + " baut die Tabelle deshalb NICHT um.");
            if (bestand.IndexOf(alt, stelle + alt.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(tabelle + " traegt die Klassenklausel zweimal.");
            return bestand.Substring(0, stelle) + neu + bestand.Substring(stelle + alt.Length);
        }

        private static string CreateText(Func<string, DbParam[], DataTable> lese, string tabelle)
        {
            DataTable dt = lese("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new[] { new DbParam("p1", tabelle) });
            return dt == null || dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value
                ? null : Convert.ToString(dt.Rows[0][0], CultureInfo.InvariantCulture);
        }

        /// <summary>Was noch offen ist: je umzubauende Tabelle 1 (−1: eine Tabelle fehlt).</summary>
        public static int Offen()
        {
            if (!Voraussetzungen().All(DataRepository.TabelleVorhanden)) return -1;
            int offen = 0;
            foreach (string t in Voraussetzungen())
            {
                string bestand = CreateText((sql, p) => DataRepository.GetDataTable(sql, p), t);
                if (bestand == null || !bestand.Contains(KlauselNeu(t), StringComparison.Ordinal)) offen++;
            }
            return offen;
        }

        /// <summary>Steht der Schritt?</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus (wiederholbar); Rückgabe = Zahl der
        /// neu gebauten Tabellen. Steht alles, geschieht nichts. Fehlt eine vorausgesetzte Tabelle, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (Offen() == 0)
            {
                bericht?.Add("steht bereits - Klasse Haustechnik am Bauteilkatalog; nichts zu tun");
                return 0;
            }
            int handgriffe = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    Func<string, DbParam[], DataTable> lese = (sql, p) => v.Lese(sql, p);
                    foreach (string t in Voraussetzungen())
                    {
                        string ziel = Zieltext(t, CreateText(lese, t));
                        if (ziel == null) continue;
                        long vorher = Zahl(v, "SELECT COUNT(*) FROM \"" + t + "\"");
                        int objekte = TwwBezugsartSchema.Neubau(v, t, ziel, Hilfsname(t));
                        long nachher = Zahl(v, "SELECT COUNT(*) FROM \"" + t + "\"");
                        if (vorher != nachher)
                            throw new InvalidOperationException(t + ": " + vorher.ToString(CultureInfo.InvariantCulture) +
                                " Zeile(n) vor dem Neubau, " + nachher.ToString(CultureInfo.InvariantCulture) + " danach.");
                        handgriffe++;
                        zeilen.Add(t + ": neu gebaut (Klasse " + KLASSE + " in der Pruefklausel); Zeilen " +
                                   nachher.ToString(CultureInfo.InvariantCulture) + ", Index/Trigger wieder " +
                                   objekte.ToString(CultureInfo.InvariantCulture));
                    }
                    foreach (string t in Voraussetzungen())
                    {
                        long verletzt = TwwBezugsartSchema.Verletzt(v, t);
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Schritt meldet foreign_key_check fuer " + t +
                                " oder eine Kindtabelle " + verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN (Muster aus Schritt 96).
                    try { if (v.Offen) v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }
            if (bericht != null)
            {
                foreach (string z in zeilen) bericht.Add(z);
                bericht.Add("foreign_key_check leer; IDs, Zeilen und Pruefsummen erhalten; der Bauteilweg liest keine Gliederung, " +
                            "der Referenzlauf bleibt gleich");
            }
            return handgriffe;
        }

        private static long Zahl(DbVorgang v, string sql) =>
            Convert.ToInt64(v.Skalar(sql) ?? 0L, CultureInfo.InvariantCulture);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // IMPORT AUS DER PROJEKTDATEI (Schritt 200; Gebaeudeimport nur aus der Projektdatei .sqproj).
    //
    // WOZU. Der Gebaeudeimport bekommt eine dritte Quelle: die Projektdatei eines Gebaeudemodells
    // (.sqproj). Format und Herkunft bekommen dafuer einen eigenen Wert SQPROJ, damit die Herkunft wahr
    // bleibt - keine Zeile traegt IFC, wenn es keine IFC-Datei gab. Beide Wertlisten stehen heute als
    // Pruefklausel im Schema, und SQLite kann einem CHECK keinen Wert nachtragen; also werden die
    // Tabellen neu gebaut:
    //
    //   Tab_Importquelle         "Format"   IN ('IFC','GBXML')                               + 'SQPROJ'
    //   Tab_Baustoff(_STAMM)     "Herkunft" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE') + 'SQPROJ'
    //   Tab_Bauteilaufbau(_STAMM), Tab_Zone, Tab_Bauteil       dieselbe Herkunftsliste      + 'SQPROJ'
    //
    // Weitere Tabellen fuehren keine dieser Listen (geprueft an sqlite_master der Testdatenbank).
    //
    // DAS REZEPT des Hauses fuer STRICT-Tabellen (Schritte 96, 100, TwwBezugsartSchema.Neubau): Fremdschluessel
    // AUS vor der Transaktion, die alte Tabelle weicht unter legacy_alter_table auf einen Hilfsnamen aus, die
    // neue entsteht unter dem echten Namen aus dem GELTENDEN sqlite_master-Text, in dem allein die
    // Pruefklausel getauscht ist (Spaltenfolge, STRICT, Fremdschluessel samt Kaskaden bleiben Zeichen fuer
    // Zeichen), die Zeilen ziehen namentlich um, der AUTOINCREMENT-Stand reist mit, Indizes und Trigger
    // stehen wieder, und foreign_key_check haelt jede Tabelle samt Kindtabellen noch IN der Transaktion.
    // Sichten verweisen ueber den Namen und ueberstehen den Neubau ohne eigenen Handgriff (legacy_alter_table).
    //
    // GRUNDSCHEMA = SCHRITT. Das Grundschema (BaustoffSchema.WERTE_HERKUNFT, ImportzuordnungSchema.WERTE_FORMAT)
    // fuehrt dieselben Wertlisten; eine neu angelegte und eine nachgezogene Datenbank tragen danach denselben
    // CREATE-Text.
    //
    // ERGEBNISNEUTRAL. Keine Zeile aendert sich, der Rechenweg liest weder Format noch Herkunft.
    // Geschrieben wird SQPROJ erst vom Import der Projektdatei.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>Import aus der Projektdatei</b> — Schemaschritt (Nummer <see cref="SCHRITT"/>): Neubau der sieben Tabellen aus
    /// <see cref="TABELLEN"/> mit den Prüfklauseln, die den Wert <c>SQPROJ</c> annehmen. EINE Quelle für Migration,
    /// Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ProjektdateiImportSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 200): der Schritt
        /// hinter <see cref="NordrichtungSchema.SCHRITT"/>.
        /// </summary>
        public const int SCHRITT = NordrichtungSchema.SCHRITT + 1;

        /// <summary>Die Herkunftsklausel vor dem Schritt (fünf Werte).</summary>
        public const string CHECK_HERKUNFT_ALT = "CHECK (\"Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE'))";

        /// <summary>Die Herkunftsklausel nach dem Schritt — dieselbe Wertliste wie das Grundschema.</summary>
        public const string CHECK_HERKUNFT_NEU = "CHECK (\"Herkunft\" IN (" + BaustoffSchema.WERTE_HERKUNFT + "))";

        /// <summary>Die Formatklausel vor dem Schritt (zwei Werte).</summary>
        public const string CHECK_FORMAT_ALT = "CHECK (\"Format\" IN ('IFC','GBXML'))";

        /// <summary>Die Formatklausel nach dem Schritt — dieselbe Wertliste wie das Grundschema.</summary>
        public const string CHECK_FORMAT_NEU = "CHECK (\"Format\" IN (" + ImportzuordnungSchema.WERTE_FORMAT + "))";

        /// <summary>Der Zusatz des Hilfsnamens, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        private const string HILFSZUSATZ = "_vor_Projektdatei";

        /// <summary>Eine umzubauende Tabelle: Name, Spalte, Prüfklausel davor und danach.</summary>
        public sealed class Umbau
        {
            internal Umbau(string tabelle, string spalte, string alt, string neu)
            {
                Tabelle = tabelle;
                Spalte = spalte;
                Alt = alt;
                Neu = neu;
            }

            /// <summary>Die Tabelle.</summary>
            public string Tabelle { get; }

            /// <summary>Die Spalte mit der Wertliste.</summary>
            public string Spalte { get; }

            /// <summary>Die Prüfklausel vor dem Schritt.</summary>
            public string Alt { get; }

            /// <summary>Die Prüfklausel nach dem Schritt.</summary>
            public string Neu { get; }
        }

        private static Umbau Herkunft(string tabelle) => new Umbau(tabelle, "Herkunft", CHECK_HERKUNFT_ALT, CHECK_HERKUNFT_NEU);

        /// <summary>Die sieben Tabellen samt Prüfklausel — erst die Quelle, dann die Herkunftstabellen, Eltern vor Kindern.</summary>
        public static readonly IReadOnlyList<Umbau> TABELLEN = new[]
        {
            new Umbau(SchemaKatalog.TAB_IMPORTQUELLE, "Format", CHECK_FORMAT_ALT, CHECK_FORMAT_NEU),
            Herkunft(SchemaKatalog.TAB_BAUSTOFF_STAMM),
            Herkunft(SchemaKatalog.TAB_BAUSTOFF),
            Herkunft(SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM),
            Herkunft(SchemaKatalog.TAB_BAUTEILAUFBAU),
            Herkunft(SchemaKatalog.TAB_ZONE),
            Herkunft(SchemaKatalog.TAB_BAUTEIL)
        };

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
        {
            var t = new List<string>();
            foreach (Umbau u in TABELLEN) t.Add(u.Tabelle);
            return t;
        }

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        public static string Hilfsname(string tabelle) => tabelle + HILFSZUSATZ;

        /// <summary>
        /// <b>Der Zieltext einer Tabelle</b>: der GELTENDE <c>sqlite_master</c>-Text, in dem allein <see cref="Umbau.Alt"/>
        /// durch <see cref="Umbau.Neu"/> ersetzt ist. <c>null</c>, wenn nichts zu tun ist (die Tabelle trägt schon die neue
        /// Klausel). Trägt sie die Spalte mit einer anderen Prüfklausel, die Klausel zweimal oder ist sie nicht
        /// <c>STRICT</c>, bricht der Aufruf ab, statt eine falsche Tabelle anzulegen.
        /// </summary>
        public static string Zieltext(Umbau u, string bestand)
        {
            if (u == null) throw new ArgumentNullException(nameof(u));
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + u.Tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            if (bestand.Contains(u.Neu, StringComparison.Ordinal)) return null;
            int stelle = bestand.IndexOf(u.Alt, StringComparison.Ordinal);
            if (stelle < 0)
                throw new InvalidOperationException(
                    u.Tabelle + " traegt die Spalte " + u.Spalte + " mit einer unbekannten Pruefklausel - der Schritt " + Nr +
                    " baut die Tabelle deshalb NICHT um.");
            if (bestand.IndexOf(u.Alt, stelle + u.Alt.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(u.Tabelle + " traegt die Pruefklausel der Spalte " + u.Spalte + " zweimal.");
            if (!bestand.TrimEnd().EndsWith(") STRICT", StringComparison.Ordinal))
                throw new InvalidOperationException(u.Tabelle + " ist keine STRICT-Tabelle - der Schritt baut sie nicht um.");
            return bestand.Substring(0, stelle) + u.Neu + bestand.Substring(stelle + u.Alt.Length);
        }

        /// <summary>Muss die Tabelle noch neu gebaut werden? <c>false</c> ohne die Tabelle.</summary>
        public static bool UmbauNoetig(Umbau u)
        {
            string bestand = Text(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("p1", u.Tabelle)));
            return bestand != null && Zieltext(u, bestand) != null;
        }

        /// <summary>Wie viele der Tabellen müssen noch neu gebaut werden?</summary>
        public static int Offen()
        {
            int offen = 0;
            foreach (Umbau u in TABELLEN)
                if (UmbauNoetig(u)) offen++;
            return offen;
        }

        /// <summary>Steht der Schritt? Jede Tabelle da und keine offen.</summary>
        public static bool Vollstaendig()
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t)) return false;
            return Offen() == 0;
        }

        /// <summary>
        /// <b>Führt den Schritt aus</b> — alle Neubauten in EINEM Vorgang mit abgeschalteten Fremdschlüsseln. Scheitert
        /// etwas, rollt der Vorgang zurück und die Ausnahme geht an den Aufrufer (Migration, Werkzeug). <b>Wiederholbar</b>:
        /// Eine fertige Tabelle wird übersprungen. Fehlt eine Tabelle, wirft er benannt. Liefert die Zahl der neu gebauten
        /// Tabellen.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            int umgebaut = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach (Umbau u in TABELLEN)
                    {
                        string bestand = Text(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                       new DbParam("p1", u.Tabelle)));
                        string ziel = Zieltext(u, bestand);
                        if (ziel == null)
                        {
                            zeilen.Add("steht bereits - " + u.Tabelle + "." + u.Spalte);
                            continue;
                        }

                        long vorher = Zahl(v, "SELECT COUNT(*) FROM \"" + u.Tabelle + "\"");
                        int objekte = TwwBezugsartSchema.Neubau(v, u.Tabelle, ziel, Hilfsname(u.Tabelle));
                        long nachher = Zahl(v, "SELECT COUNT(*) FROM \"" + u.Tabelle + "\"");
                        if (vorher != nachher)
                            throw new InvalidOperationException(u.Tabelle + ": " + vorher.ToString(CultureInfo.InvariantCulture) +
                                " Zeile(n) vor dem Neubau, " + nachher.ToString(CultureInfo.InvariantCulture) + " danach.");
                        long verletzt = TwwBezugsartSchema.Verletzt(v, u.Tabelle);
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Neubau meldet foreign_key_check fuer " + u.Tabelle +
                                " oder eine Kindtabelle " + verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");
                        umgebaut++;
                        zeilen.Add(u.Tabelle + "." + u.Spalte + ": neu gebaut mit " + u.Neu + "; Zeilen " +
                                   vorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                                   nachher.ToString(CultureInfo.InvariantCulture) + ", Index/Trigger wieder " +
                                   objekte.ToString(CultureInfo.InvariantCulture));
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
                bericht.Add(umgebaut.ToString(CultureInfo.InvariantCulture) + " Tabelle(n) neu gebaut, foreign_key_check leer; " +
                            "KEIN DML an Bestandsdaten, der Referenzlauf bleibt gleich");
            }
            return umgebaut;
        }

        private static long Zahl(DbVorgang v, string sql)
        {
            object wert = v.Skalar(sql);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}

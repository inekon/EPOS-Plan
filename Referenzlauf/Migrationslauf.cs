using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Modus "migration" der Referenzlauf-Suite (ADR-001, Aufgabe 8).
    ///
    /// Legt eine Kopie der uebergebenen Datenbank an, biegt den DB-Pfad der App darauf
    /// um (und prueft das hart nach), laesst <see cref="SchemaMigration"/> laufen und
    /// weist das Ergebnis am Schema nach - Spalten, neue Tabelle, Beziehungen.
    ///
    /// HARTE REGEL wie bei allen anderen Modi: es wird niemals in die produktive
    /// Kenndaten.accdb geschrieben. DbUmgebung.AufArbeitskopieUmschaltenUndPruefen
    /// bricht ab, wenn der Zielpfad eine bekannte produktive Ablage ist.
    ///
    /// Aufruf:
    ///   Referenzlauf.exe migration &lt;quellDb&gt; &lt;zielOrdner&gt; [--nokopie] [--schreibschutz]
    ///
    ///   --nokopie        vorhandene Kopie im Zielordner weiterverwenden (No-op-Lauf)
    ///   --schreibschutz  die Kopie vor dem Lauf schreibgeschuetzt setzen
    /// </summary>
    internal static class Migrationslauf
    {
        public static int Ausfuehren(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("  Referenzlauf.exe migration <quellDb> <zielOrdner> [--nokopie] [--schreibschutz]");
                return 2;
            }

            string quelle = args[0];
            string zielOrdner = Path.GetFullPath(args[1]);
            bool nokopie = args.Any(a => string.Equals(a, "--nokopie", StringComparison.OrdinalIgnoreCase));
            bool schreibschutz = args.Any(a => string.Equals(a, "--schreibschutz", StringComparison.OrdinalIgnoreCase));

            var log = new Protokoll();
            log.Zeile("Migrations-Testlauf");
            log.Zeile("Quelle:      " + quelle);
            log.Zeile("Zielordner:  " + zielOrdner);
            log.Zeile("Optionen:    " + (nokopie ? "--nokopie " : "") + (schreibschutz ? "--schreibschutz" : ""));
            log.Leerzeile();

            string ziel = Path.Combine(zielOrdner, DbUmgebung.DB_DATEINAME);

            // --- 1. Kopie -----------------------------------------------------------
            if (nokopie)
            {
                if (!File.Exists(ziel))
                {
                    log.FehlerZeile("--nokopie verlangt eine vorhandene Kopie: " + ziel);
                    return 2;
                }
                log.Zeile("Vorhandene Kopie wird weiterverwendet: " + ziel);
            }
            else
            {
                if (!File.Exists(quelle))
                {
                    log.FehlerZeile("Quelldatenbank nicht gefunden: " + quelle);
                    return 2;
                }
                DbUmgebung.ArbeitskopieAnlegen(quelle, zielOrdner, log);
            }

            var info = new FileInfo(ziel);
            if (schreibschutz)
            {
                if (!info.IsReadOnly) info.IsReadOnly = true;
                log.Zeile("Kopie schreibgeschuetzt gesetzt.");
            }
            else if (info.IsReadOnly)
            {
                info.IsReadOnly = false;
            }

            // --- 2. DB-Pfad der App umbiegen und hart pruefen ------------------------
            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(zielOrdner, log);
            log.Leerzeile();

            // --- 3. Migration -------------------------------------------------------
            int standVorher = SchemaVersionLesen(ziel);
            log.Zeile("SchemaVersion vorher: " + Anzeige(standVorher));

            string bericht;
            bool ok;
            using (new DialogWaechter())
            {
                ok = SchemaMigration.Ausfuehren(out bericht);
            }

            log.Leerzeile();
            log.Roh("----- Bericht der SchemaMigration -----");
            foreach (string z in (bericht ?? "").Replace("\r\n", "\n").Split('\n')) log.Roh(z);
            log.Roh("---------------------------------------");
            log.Leerzeile();

            int standNachher = SchemaVersionLesen(ziel);
            log.Zeile("Ergebnis: " + (ok ? "ERFOLG" : "FEHLGESCHLAGEN") +
                      "   MigrationOk=" + SchemaMigration.MigrationOk);
            log.Zeile("SchemaVersion nachher: " + Anzeige(standNachher) +
                      "   (Zielstand " + SchemaMigration.ZIEL_VERSION + ")");
            log.Zeile("ID_PUFFER-Bereinigung: " + SchemaMigration.IdPufferGemappt + " gemappt, " +
                      SchemaMigration.IdPufferGenullt + " genullt");
            log.Zeile("Datenmigration 5.5: " +
                      SchemaMigration.DatenPufferVerwendung + " Puffer mit Verwendung, " +
                      SchemaMigration.DatenAnlagenPuffersenke + " Anlagen auf Puffer, " +
                      SchemaMigration.DatenAnlagenHeizkreis + " Anlagen auf Heizkreis, " +
                      SchemaMigration.DatenQuellPuffer + " Quell-Puffer, " +
                      SchemaMigration.DatenAnlagenzeilenNeu + " Anlagenzeilen neu, " +
                      SchemaMigration.DatenAnlagenzeilenRepariert + " Anlagenzeilen mit ID_PUFFER repariert, " +
                      SchemaMigration.DatenPendelspeicherNeu + " Pendelspeicher (davon " +
                      SchemaMigration.DatenPendelspeicherTemperaturen + " mit Systemtemperaturen), " +
                      SchemaMigration.DatenHinweise + " Hinweise");
            log.Leerzeile();

            // --- 4. Schema-Nachweis --------------------------------------------------
            // Bei schreibgeschuetzter Datei ist der Nachweis sinnlos - die DB ist unveraendert.
            int abweichungen = 0;
            if (!schreibschutz) abweichungen = SchemaNachweis(ziel, log);

            log.Leerzeile();
            if (schreibschutz)
            {
                // Erwartetes Verhalten: sauberer Fehlerbericht, Marker NICHT angehoben.
                bool markerUnveraendert = standNachher == standVorher;
                log.Zeile("Schreibschutz-Fall: Marker unveraendert = " + markerUnveraendert +
                          ", MigrationOk = " + SchemaMigration.MigrationOk);
                bool erwartet = !ok && !SchemaMigration.MigrationOk && markerUnveraendert;
                log.Zeile(erwartet ? "ERGEBNIS: wie erwartet." : "ERGEBNIS: NICHT wie erwartet!");
                info.IsReadOnly = false; // Aufraeumen, damit der Ordner loeschbar bleibt
                return erwartet ? 0 : 1;
            }

            log.Zeile("Abweichungen im Schema-Nachweis: " + abweichungen);
            return (ok && abweichungen == 0) ? 0 : 1;
        }

        private static string Anzeige(int v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        private static int SchemaVersionLesen(string dbDatei)
        {
            try
            {
                using (var conn = Verbindung(dbDatei))
                {
                    conn.Open();
                    var dt = new DataTable();
                    using (var cmd = new OleDbCommand("SELECT TOP 1 * FROM Tab_Applikation", conn))
                    using (var ad = new OleDbDataAdapter(cmd)) ad.Fill(dt);

                    if (!dt.Columns.Contains("SchemaVersion") || dt.Rows.Count == 0) return 0;
                    object v = dt.Rows[0]["SchemaVersion"];
                    return v == DBNull.Value ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture);
                }
            }
            catch { return -1; }
        }

        private static OleDbConnection Verbindung(string dbDatei)
        {
            return new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + dbDatei + ";");
        }

        // =================================================================================
        // Schema-Nachweis ueber GetOleDbSchemaTable
        // =================================================================================

        private sealed class FkErwartung
        {
            public string FkTabelle, FkSpalte, PkTabelle, DeleteRegel;
            public FkErwartung(string ft, string fs, string pt, string del)
            { FkTabelle = ft; FkSpalte = fs; PkTabelle = pt; DeleteRegel = del; }
        }

        private static readonly FkErwartung[] ERWARTETE_FKS =
        {
            // Schritt 3 - Ergebnistabelle haengt wie ihre Geschwister an Tab_Ergebnis
            new FkErwartung("Tab_ErgebnisPufferspeicher", "ID_Ergebnis", "Tab_Ergebnis", "CASCADE"),

            // Schritt 4 - die drei neuen plus ID_PUFFER, alle RESTRIKTIV (kein CASCADE)
            new FkErwartung("Tab_Energieanlagen", "WS_ID_Puffer",  "Tab_Pufferspeicher", "NO ACTION"),
            new FkErwartung("Tab_Energieanlagen", "WS_ID_Puffer2", "Tab_Pufferspeicher", "NO ACTION"),
            new FkErwartung("Tab_Energieanlagen", "WQ_ID_Puffer",  "Tab_Pufferspeicher", "NO ACTION"),
            new FkErwartung("Tab_Energieanlagen", "ID_PUFFER",     "Tab_Pufferspeicher", "NO ACTION"),

            // B0-6b - Projektloeschung raeumt die Puffer-Kopien ab
            new FkErwartung("Tab_Pufferspeicher", "ID_Projekt", "Tab_Projekt", "CASCADE"),
        };

        private static readonly string[] ERGEBNISPUFFER_SPALTEN =
        {
            "ID", "ID_Ergebnis", "ID_Pufferspeicher", "Bezeichner", "Verwendung", "Q_max",
            "Ladung_gesamt", "Entladung_gesamt", "Verluste_gesamt", "SOC_Ende", "SOC_Mittel",
            "SOC_Max", "Vollzyklen"
        };

        private static int SchemaNachweis(string dbDatei, Protokoll log)
        {
            int fehlend = 0;

            using (var conn = Verbindung(dbDatei))
            {
                conn.Open();

                // --- Spalten aus dem gemeinsamen Katalog ----------------------------
                log.Roh("--- Spalten (Katalog SchemaKatalog.Alle) ---");
                foreach (var gruppe in SchemaKatalog.Alle.GroupBy(s => s.Tabelle, StringComparer.OrdinalIgnoreCase))
                {
                    HashSet<string> vorhanden = SpaltenLesen(conn, gruppe.Key);
                    var fehlt = gruppe.Where(s => !vorhanden.Contains(s.Name)).Select(s => s.Name).ToList();
                    log.Roh(string.Format(CultureInfo.InvariantCulture, "  {0,-22} {1,2}/{2,2} vorhanden{3}",
                        gruppe.Key, gruppe.Count() - fehlt.Count, gruppe.Count(),
                        fehlt.Count == 0 ? "" : "   FEHLT: " + string.Join(", ", fehlt)));
                    fehlend += fehlt.Count;
                }

                // --- Position von Extrapolation_erlaubt (row[0..22] darf unberuehrt bleiben)
                List<string> einstellungen = SpaltenInReihenfolge(conn, "Tab_Einstellungen");
                int posExtra = einstellungen.FindIndex(c =>
                    string.Equals(c, "Extrapolation_erlaubt", StringComparison.OrdinalIgnoreCase));
                log.Roh("  Tab_Einstellungen: " + einstellungen.Count + " Spalten, " +
                        "Extrapolation_erlaubt an Position " + posExtra +
                        (posExtra == einstellungen.Count - 1 && posExtra >= 23
                            ? "  (angehaengt - row[0..22] unberuehrt)"
                            : "  ACHTUNG: nicht am Ende!"));
                if (posExtra < 23) fehlend++;

                // --- Neue Tabelle ---------------------------------------------------
                log.Roh("--- Tabelle Tab_ErgebnisPufferspeicher ---");
                HashSet<string> ergSpalten = SpaltenLesen(conn, "Tab_ErgebnisPufferspeicher");
                if (ergSpalten.Count == 0)
                {
                    log.Roh("  FEHLT vollstaendig!");
                    fehlend++;
                }
                else
                {
                    var fehlt = ERGEBNISPUFFER_SPALTEN.Where(c => !ergSpalten.Contains(c)).ToList();
                    log.Roh("  " + (ERGEBNISPUFFER_SPALTEN.Length - fehlt.Count) + "/" +
                            ERGEBNISPUFFER_SPALTEN.Length + " Spalten" +
                            (fehlt.Count == 0 ? "" : "   FEHLT: " + string.Join(", ", fehlt)));
                    fehlend += fehlt.Count;

                    bool index = IndexVorhanden(conn, "Tab_ErgebnisPufferspeicher", "idx_ErgPuffer");
                    log.Roh("  Index idx_ErgPuffer: " + (index ? "vorhanden" : "FEHLT"));
                    if (!index) fehlend++;
                }

                // --- Beziehungen ----------------------------------------------------
                log.Roh("--- Beziehungen ---");
                DataTable fks = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, null);
                foreach (FkErwartung e in ERWARTETE_FKS)
                {
                    DataRow treffer = fks.Rows.Cast<DataRow>().FirstOrDefault(r =>
                        string.Equals(Txt(r, "FK_TABLE_NAME"), e.FkTabelle, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Txt(r, "FK_COLUMN_NAME"), e.FkSpalte, StringComparison.OrdinalIgnoreCase));

                    if (treffer == null)
                    {
                        log.Roh("  FEHLT: " + e.FkTabelle + "." + e.FkSpalte + " -> " + e.PkTabelle);
                        fehlend++;
                        continue;
                    }

                    string pk = Txt(treffer, "PK_TABLE_NAME");
                    string del = Txt(treffer, "DELETE_RULE");
                    bool passt = string.Equals(pk, e.PkTabelle, StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(del, e.DeleteRegel, StringComparison.OrdinalIgnoreCase);
                    log.Roh(string.Format(CultureInfo.InvariantCulture,
                        "  {0} {1}.{2} -> {3}.{4}  DELETE={5} (erwartet {6})",
                        passt ? "OK  " : "ABW.", e.FkTabelle, e.FkSpalte, pk,
                        Txt(treffer, "PK_COLUMN_NAME"), del, e.DeleteRegel));
                    if (!passt) fehlend++;
                }

                // --- Restbestand ungueltiger ID_PUFFER-Werte ------------------------
                object rest = Skalar(conn,
                    "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_PUFFER IS NOT NULL " +
                    "AND ID_PUFFER NOT IN (SELECT ID FROM Tab_Pufferspeicher)");
                log.Roh("--- Datenbestand ---");
                log.Roh("  ID_PUFFER ohne gueltigen Puffer: " + (rest ?? "?"));
                object waisen = Skalar(conn,
                    "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt IS NOT NULL " +
                    "AND ID_Projekt NOT IN (SELECT ID FROM Tab_Projekt)");
                log.Roh("  Tab_Pufferspeicher ohne Projekt:  " + (waisen ?? "?"));

                fehlend += DatenNachweis(conn, log);
            }

            return fehlend;
        }

        /// <summary>
        /// Nachweis der Datenmigration aus Schritt 5 (Konzept 5.5). Geprueft wird der
        /// Zustand NACH dem Lauf, unabhaengig von den Zaehlern der Migration selbst -
        /// beim No-op-Zweitlauf muessen dieselben Zahlen herauskommen.
        /// </summary>
        private static int DatenNachweis(OleDbConnection conn, Protokoll log)
        {
            int fehler = 0;
            log.Roh("--- Datenmigration Quellen/Senken (Konzept 5.5) ---");

            log.Roh("  Puffer mit Verwendung gesetzt:      " +
                    Skalar(conn, "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE Verwendung IS NOT NULL"));
            log.Roh("  Anlagen WS_Ziel = PufferHeizung:    " +
                    Skalar(conn, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WS_Ziel = 'PufferHeizung'"));
            log.Roh("  Anlagen WS_Ziel = Heizkreis:        " +
                    Skalar(conn, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WS_Ziel = 'Heizkreis'"));
            log.Roh("  Anlagen mit WQ_ID_Puffer:           " +
                    Skalar(conn, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WQ_ID_Puffer IS NOT NULL"));
            log.Roh("  Anlagenzeilen ID_Type = 12:         " +
                    Skalar(conn, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Type = 12"));
            log.Roh("  Puffer 'BHKW-Pendelspeicher':       " +
                    Skalar(conn, "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE Bezeichner = 'BHKW-Pendelspeicher'"));

            // Verhaltensneutralitaet: die Alt-Zuordnung ist unangetastet (Konzept 5.4).
            log.Roh("  Z_ProjektPufferSp Zeilen:           " +
                    Skalar(conn, "SELECT COUNT(*) FROM Z_ProjektPufferSp"));

            // Invarianten
            object senkeOhneId = Skalar(conn,
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WS_Ziel = 'PufferHeizung' AND WS_ID_Puffer IS NULL");
            log.Roh("  PufferHeizung ohne WS_ID_Puffer:    " + senkeOhneId + "   (erwartet 0)");
            if (Zahl(senkeOhneId) != 0) fehler++;

            object nullwerte = Skalar(conn,
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WS_ID_Puffer = 0 OR WS_ID_Puffer2 = 0 OR WQ_ID_Puffer = 0");
            log.Roh("  ID-Spalten mit 0 statt NULL:        " + nullwerte + "   (erwartet 0)");
            if (Zahl(nullwerte) != 0) fehler++;

            object prioOffen = Skalar(conn,
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE WS_Ladeprio IS NULL OR WS_Ladeprio2 IS NULL " +
                "OR WS_Ladeprio_PV IS NULL OR WS_Ladegrenze IS NULL OR WS_Ladegrenze2 IS NULL");
            log.Roh("  Anlagen ohne Ladeprio-Vorgabe:      " + prioOffen + "   (erwartet 0)");
            if (Zahl(prioOffen) != 0) fehler++;

            object pufferOhneZeile = Skalar(conn,
                "SELECT COUNT(*) FROM (SELECT DISTINCT ID_Projekt, Bezeichner FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt IS NOT NULL) AS p WHERE NOT EXISTS " +
                "(SELECT 1 FROM Tab_Energieanlagen a WHERE a.ID_Type = 12 " +
                " AND a.ID_Projekt = p.ID_Projekt AND a.Bezeichner = p.Bezeichner)");
            log.Roh("  Projekt-Puffer ohne Anlagenzeile:   " + pufferOhneZeile + "   (erwartet 0)");
            if (Zahl(pufferOhneZeile) != 0) fehler++;

            return fehler;
        }

        private static int Zahl(object o)
        {
            if (o == null || o == DBNull.Value) return -1;
            try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
            catch { return -1; }
        }

        private static string Txt(DataRow r, string spalte)
        {
            return r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? r[spalte].ToString() : "";
        }

        private static HashSet<string> SpaltenLesen(OleDbConnection conn, string tabelle)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tabelle, null });
                foreach (DataRow r in dt.Rows) set.Add(r["COLUMN_NAME"].ToString());
            }
            catch { }
            return set;
        }

        private static List<string> SpaltenInReihenfolge(OleDbConnection conn, string tabelle)
        {
            var liste = new List<string>();
            try
            {
                DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tabelle, null });
                foreach (DataRow r in dt.Select("", "ORDINAL_POSITION")) liste.Add(r["COLUMN_NAME"].ToString());
            }
            catch { }
            return liste;
        }

        private static bool IndexVorhanden(OleDbConnection conn, string tabelle, string index)
        {
            try
            {
                DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Indexes,
                    new object[] { null, null, null, null, tabelle });
                foreach (DataRow r in dt.Rows)
                    if (string.Equals(r["INDEX_NAME"].ToString(), index, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            catch { }
            return false;
        }

        private static object Skalar(OleDbConnection conn, string sql)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, conn)) return cmd.ExecuteScalar();
            }
            catch { return null; }
        }
    }
}

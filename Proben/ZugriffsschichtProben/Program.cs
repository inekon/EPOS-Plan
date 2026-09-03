using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Text;
using WindowsFormsApplication1;

namespace ZugriffsschichtProben
{
    /// <summary>
    /// Proben zur umgebauten Zugriffsschicht (Arbeitspaket S4a), zur Schemapflege-
    /// Gabelung (Arbeitspaket S6, Faelle 13 bis 15) und zum Erststart-Assistenten
    /// (Arbeitspaket S8, Fall 16).
    ///
    /// AUFRUF:
    ///   ZugriffsschichtProben.exe --quelle=&lt;Pfad zur SQLite-Datei&gt; [--arbeit=&lt;Ordner&gt;]
    ///                             [--altbestand=&lt;Pfad zur .accdb&gt;]
    /// oder ueber die Umgebungsvariable EPOS_PROBEN_QUELLE. Es wird IMMER auf einer
    /// KOPIE gearbeitet - die Quelldatei wird nur gelesen und nie geoeffnet, waehrend
    /// geschrieben wird. Das gilt auch fuer den Altbestand aus Fall 15.
    ///
    /// Rueckgabewert = Anzahl der fehlgeschlagenen Faelle (0 = alles bestanden).
    /// Uebersprungene Faelle sind KEIN Fehlschlag; sie nennen ihren Grund.
    /// </summary>
    internal static class Program
    {
        private const string PROBE_INSERT = "S4a-Probe-Insert";
        private const string PROBE_ROLLBACK = "S4a-Probe-Rollback";
        private const string PROBE_COMMIT = "S4a-Probe-Commit";
        private const string PROBE_DISPOSE = "S4a-Probe-Dispose";
        private const string PROBE_FK = "S4a-Probe-FK";

        /// <summary>Wegwerfspalte des synthetischen SQLite-Schritts aus Fall 14.</summary>
        private const string PROBE_SPALTE = "S6_Probespalte";

        /// <summary>Vorgabe fuer Fall 15, ueberschreibbar mit --altbestand=.</summary>
        private const string ALTBESTAND_VORGABE = @"C:\ProgramData\EPOS_PLAN\Kenndaten.accdb";

        private static int _faelle;
        private static int _fehlschlaege;
        private static int _uebersprungen;
        private static readonly List<string> _stilleMeldungen = new List<string>();

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch (Exception) { }

            string quelle = Argument(args, "--quelle") ?? Environment.GetEnvironmentVariable("EPOS_PROBEN_QUELLE");
            string arbeitsordner = Argument(args, "--arbeit")
                                   ?? Path.Combine(Path.GetTempPath(), "ZugriffsschichtProben");

            if (string.IsNullOrWhiteSpace(quelle) || !File.Exists(quelle))
            {
                Console.Error.WriteLine("ABBRUCH: Quelldatenbank nicht angegeben oder nicht vorhanden.");
                Console.Error.WriteLine("         --quelle=<Pfad> setzen oder EPOS_PROBEN_QUELLE belegen.");
                return 1;
            }

            string kopie;
            try
            {
                kopie = ArbeitskopieAnlegen(quelle, arbeitsordner);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ABBRUCH: Arbeitskopie liess sich nicht anlegen: " + ex.Message);
                return 1;
            }

            Console.WriteLine("Quelle       : " + quelle);
            Console.WriteLine("Arbeitskopie : " + kopie);
            Console.WriteLine();

            // Die Zugriffsschicht auf die KOPIE richten - ohne die Einstellungen des
            // Anwenders anzufassen (Haken aus S4a).
            DataRepository.PfadUeberschreibung = kopie;

            // Durchgaengig dialogfrei: sonst blockierte die erste FehlerMelden-MessageBox
            // den unbeaufsichtigten Lauf bis zum Timeout.
            using (DataRepository.EngineModus())
            {
                AufraeumenVorlauf();

                Fall01Uebersetzer();
                Fall02Normalisierung();
                Fall03Lesen();
                Fall04Wahrheitswerte();
                Fall05JoinDubletten();
                Fall06Skalar();
                Fall07EinfuegenUndId();
                Fall08Vorgang();
                Fall09SchemaAuskunft();
                Fall10Fremdschluessel();
                Fall11KeinBeginTransaction();
                Fall12DatenbankVorhanden(kopie);

                // --- ARBEITSPAKET S6: die Schemapflege-Gabelung -----------------------
                // Jeder der drei Faelle arbeitet auf einer EIGENEN Wegwerf-Kopie und
                // setzt PfadUeberschreibung selbst; am Ende steht sie wieder auf der
                // gemeinsamen Arbeitskopie, damit der Nachlauf dort aufraeumt.
                Fall13SqliteZweig(quelle, arbeitsordner);
                Fall14SqliteSchritt(quelle, arbeitsordner);
                Fall15Altbestand(args, arbeitsordner);

                // --- ARBEITSPAKET S8: der Erststart-Assistent -------------------------
                Fall16Erststart(args, arbeitsordner);

                DataRepository.PfadUeberschreibung = kopie;

                AufraeumenNachlauf();
                StilleMeldungenEinsammeln();
            }

            Console.WriteLine();
            Console.WriteLine("Still gesammelte Datenbankmeldungen (" + _stilleMeldungen.Count + "):");
            if (_stilleMeldungen.Count == 0) Console.WriteLine("  (keine)");
            foreach (string m in _stilleMeldungen) Console.WriteLine("  * " + m);

            Console.WriteLine();
            Console.WriteLine("Ergebnis: " + (_faelle - _fehlschlaege - _uebersprungen) + "/" + _faelle +
                              " bestanden" +
                              (_uebersprungen > 0 ? ", " + _uebersprungen + " uebersprungen" : "") + ".");
            return _fehlschlaege;
        }


        // =============================================================================
        // Die Faelle 1 bis 16
        // =============================================================================

        private static void Fall01Uebersetzer()
        {
            Fuehre("1  ?->@pN-Uebersetzung", fall =>
            {
                Gleich(fall, "einfach", "a=@p0 AND b=@p1",
                       DataRepository.UebersetzeParameterzeichen("a=? AND b=?"));
                Gleich(fall, "Textliteral bleibt", "x='?' AND y=@p0",
                       DataRepository.UebersetzeParameterzeichen("x='?' AND y=?"));
                Gleich(fall, "eckiger Bezeichner bleibt", "[a?b]=@p0",
                       DataRepository.UebersetzeParameterzeichen("[a?b]=?"));
                Gleich(fall, "''-Escape bleibt", "z='it''s?' AND w=@p0",
                       DataRepository.UebersetzeParameterzeichen("z='it''s?' AND w=?"));
                Gleich(fall, "ohne ? unveraendert", "SELECT ID FROM Tab_Projekt ORDER BY ID",
                       DataRepository.UebersetzeParameterzeichen("SELECT ID FROM Tab_Projekt ORDER BY ID"));
            });
        }

        private static void Fall02Normalisierung()
        {
            Fuehre("2  NormalisiereWert", fall =>
            {
                Gleich(fall, "true", 1, DataRepository.NormalisiereWert(true));
                Gleich(fall, "false", 0, DataRepository.NormalisiereWert(false));

                object datum = DataRepository.NormalisiereWert(new DateTime(2026, 9, 2, 13, 4, 5));
                Gleich(fall, "DateTime", "2026-09-02 13:04:05", datum);

                Gleich(fall, "DBNull", DBNull.Value, DataRepository.NormalisiereWert(DBNull.Value));
                Gleich(fall, "null", DBNull.Value, DataRepository.NormalisiereWert(null));

                Guid g = Guid.NewGuid();
                Gleich(fall, "Guid", g.ToString(), DataRepository.NormalisiereWert(g));

                object dez = DataRepository.NormalisiereWert(1.5m);
                fall.Muss(dez is double, "decimal wurde nicht zu double, sondern zu " + Typname(dez));
                fall.Muss(dez is double && Math.Abs((double)dez - 1.5d) < 1e-12, "decimal-Wert falsch: " + dez);

                object text = DataRepository.NormalisiereWert("unveraendert");
                Gleich(fall, "string bleibt", "unveraendert", text);
            });
        }

        private static void Fall03Lesen()
        {
            Fuehre("3  GetDataTable Tab_Projekt (Zeilen, Int32, Umlaut, Datum)", fall =>
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Projektname, Aenderungsdatum FROM Tab_Projekt ORDER BY ID");

                fall.Muss(dt.Rows.Count == 26, "Zeilenzahl " + dt.Rows.Count + " statt 26");
                if (dt.Rows.Count == 0) return;

                fall.Muss(dt.Columns["ID"].DataType == typeof(int),
                          "Spaltentyp ID ist " + dt.Columns["ID"].DataType.Name + " statt Int32");
                fall.Muss(dt.Rows[0]["ID"].GetType() == typeof(int),
                          "Wertetyp ID ist " + Typname(dt.Rows[0]["ID"]) + " statt Int32");

                DataRow[] gefunden = dt.Select("ID = 19");
                fall.Muss(gefunden.Length == 1, "ID 19 nicht genau einmal gefunden (" + gefunden.Length + ")");
                if (gefunden.Length == 1)
                {
                    string name = Convert.ToString(gefunden[0]["Projektname"]);
                    fall.Muss(name == "Wöhler WP", "Projektname zu ID 19 ist \"" + name + "\" statt \"Wöhler WP\"");
                }

                fall.Muss(dt.Columns["Aenderungsdatum"].DataType == typeof(DateTime),
                          "Spaltentyp Aenderungsdatum ist " + dt.Columns["Aenderungsdatum"].DataType.Name +
                          " statt DateTime");
                foreach (DataRow zeile in dt.Rows)
                {
                    object w = zeile["Aenderungsdatum"];
                    if (w == DBNull.Value || w is DateTime) continue;
                    fall.Muss(false, "Aenderungsdatum in ID " + zeile["ID"] + " ist " + Typname(w));
                    break;
                }

                // LEERES Ergebnis - der haeufigste Fall im Bestand. Die Spaltentypen
                // muessen auch dann stehen, denn der Bestand fragt DataColumn.DataType ab,
                // bevor er die Zeilenzahl prueft.
                DataTable leer = DataRepository.GetDataTable(
                    "SELECT ID, Projektname, Aenderungsdatum FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("?", -1));
                fall.Muss(leer.Rows.Count == 0, "leeres Ergebnis lieferte " + leer.Rows.Count + " Zeilen");
                fall.Muss(leer.Columns.Count == 3, "leeres Ergebnis hat " + leer.Columns.Count + " Spalten statt 3");
                if (leer.Columns.Count == 3)
                {
                    fall.Muss(leer.Columns["ID"].DataType == typeof(int),
                              "leeres Ergebnis: ID ist " + leer.Columns["ID"].DataType.Name + " statt Int32");
                    fall.Muss(leer.Columns["Projektname"].DataType == typeof(string),
                              "leeres Ergebnis: Projektname ist " + leer.Columns["Projektname"].DataType.Name +
                              " statt String");
                    fall.Muss(leer.Columns["Aenderungsdatum"].DataType == typeof(DateTime),
                              "leeres Ergebnis: Aenderungsdatum ist " +
                              leer.Columns["Aenderungsdatum"].DataType.Name + " statt DateTime");
                }
            });
        }

        private static void Fall04Wahrheitswerte()
        {
            Fuehre("4  Boolean-Spalte kommt als bool an (Tab_Energieanlagen.Bivalenter_Betrieb)", fall =>
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bivalenter_Betrieb FROM Tab_Energieanlagen ORDER BY ID");

                fall.Muss(dt.Rows.Count > 0, "keine Zeilen gelesen");
                if (dt.Rows.Count == 0) return;

                fall.Muss(dt.Columns["Bivalenter_Betrieb"].DataType == typeof(bool),
                          "Spaltentyp ist " + dt.Columns["Bivalenter_Betrieb"].DataType.Name + " statt Boolean");
                fall.Muss(dt.Rows[0]["Bivalenter_Betrieb"].GetType() == typeof(bool),
                          "Wertetyp ist " + Typname(dt.Rows[0]["Bivalenter_Betrieb"]) + " statt Boolean");

                // Der Bestand traegt beide Auspraegungen - beide muessen ankommen.
                int wahr = 0, falsch = 0;
                foreach (DataRow zeile in dt.Rows)
                {
                    if (zeile["Bivalenter_Betrieb"] == DBNull.Value) continue;
                    if ((bool)zeile["Bivalenter_Betrieb"]) wahr++; else falsch++;
                }
                fall.Muss(wahr > 0 && falsch > 0,
                          "erwartet wurden beide Auspraegungen, gezaehlt true=" + wahr + " false=" + falsch);
            });
        }

        private static void Fall05JoinDubletten()
        {
            Fuehre("5  Namensdubletten aus Joins entdoppelt (ID, ID1)", fall =>
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT a.ID, b.ID FROM Tab_Projekt a INNER JOIN Tab_Projekt b ON a.ID=b.ID");

                fall.Muss(dt.Columns.Count == 2, "Spaltenzahl " + dt.Columns.Count + " statt 2");
                if (dt.Columns.Count != 2) return;

                Gleich(fall, "Spalte 0", "ID", dt.Columns[0].ColumnName);
                Gleich(fall, "Spalte 1", "ID1", dt.Columns[1].ColumnName);
                fall.Muss(dt.Rows.Count == 26, "Zeilenzahl " + dt.Rows.Count + " statt 26");
            });
        }

        private static void Fall06Skalar()
        {
            Fuehre("6  ExecuteScalar und GetIdByName", fall =>
            {
                object anzahl = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Projekt");
                fall.Muss(anzahl != null, "ExecuteScalar lieferte null");
                if (anzahl != null)
                    fall.Muss(Convert.ToInt32(anzahl) == 26, "COUNT(*) = " + anzahl + " statt 26");

                int id = DataRepository.GetIdByName("Tab_Projekt", "Projektname", "Wöhler WP");
                fall.Muss(id == 19, "GetIdByName lieferte " + id + " statt 19");

                object leer = DataRepository.ExecuteScalar(
                    "SELECT Beschreibung FROM Tab_Projekt WHERE ID = ?", new DbParam("?", -1));
                fall.Muss(leer == null, "leeres Ergebnis kam als " + Typname(leer) + " statt null");
            });
        }

        private static void Fall07EinfuegenUndId()
        {
            Fuehre("7  ExecuteInsertAndGetId auf energy_carrier (Schreiben auf der Kopie)", fall =>
            {
                int maxVorher = Ganzzahl("SELECT MAX(id) FROM energy_carrier");

                int neu = DataRepository.ExecuteInsertAndGetId(
                    "INSERT INTO energy_carrier (name, code, group_code, billing_unit, is_active) VALUES (?, ?, ?, ?, ?)",
                    new DbParam[]
                    {
                        new DbParam("?", PROBE_INSERT),
                        new DbParam("?", "S4A_INS"),
                        new DbParam("?", "PROBE"),
                        new DbParam("?", "kWh"),
                        new DbParam("?", true),      // bool -> INTEGER 1
                    });

                fall.Muss(neu > maxVorher, "neue ID " + neu + " ist nicht groesser als das bisherige MAX(id) " + maxVorher);
                fall.Muss(TraegerAnzahl(PROBE_INSERT) == 1, "eingefuegte Zeile nicht auffindbar");

                // is_active war ein bool - er muss als bool zurueckkommen (Typkatalog).
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT id, is_active FROM energy_carrier WHERE name = ?", new DbParam("?", PROBE_INSERT));
                if (dt.Rows.Count == 1)
                {
                    fall.Muss(dt.Columns["is_active"].DataType == typeof(bool),
                              "is_active kam als " + dt.Columns["is_active"].DataType.Name + " statt Boolean");
                    fall.Muss(dt.Rows[0]["is_active"] is bool && (bool)dt.Rows[0]["is_active"],
                              "is_active ist nicht true");
                    fall.Muss(Convert.ToInt32(dt.Rows[0]["id"]) == neu, "gelesene id passt nicht zur Rueckgabe");
                }

                bool geloescht = DataRepository.ExecuteSQL(
                    "DELETE FROM energy_carrier WHERE id = ?", new DbParam("?", neu));
                fall.Muss(geloescht, "ExecuteSQL (DELETE) lieferte false");
                fall.Muss(TraegerAnzahl(PROBE_INSERT) == 0, "Zeile nach dem Loeschen noch vorhanden");
            });
        }

        private static void Fall08Vorgang()
        {
            Fuehre("8  DbVorgang: Rollback / Commit / Dispose ohne Commit", fall =>
            {
                // --- Rollback ---
                using (DbVorgang vorgang = DataRepository.Vorgang())
                {
                    vorgang.Ausfuehren(EinfuegenSql(),
                                       new DbParam("?", PROBE_ROLLBACK), new DbParam("?", "S4A_RB"));
                    object innen = vorgang.Skalar("SELECT COUNT(*) FROM energy_carrier WHERE name = ?",
                                                  new DbParam("?", PROBE_ROLLBACK));
                    fall.Muss(innen != null && Convert.ToInt32(innen) == 1,
                              "Zeile war im laufenden Vorgang nicht sichtbar");
                    vorgang.Rollback();
                }
                fall.Muss(TraegerAnzahl(PROBE_ROLLBACK) == 0, "Rollback: Zeile ist trotzdem da");

                // --- Commit ---
                int neueId;
                using (DbVorgang vorgang = DataRepository.Vorgang())
                {
                    neueId = vorgang.EinfuegenUndId(EinfuegenSql(), new DbParam[]
                    {
                        new DbParam("?", PROBE_COMMIT), new DbParam("?", "S4A_CM"),
                    });
                    fall.Muss(neueId > 0, "EinfuegenUndId lieferte " + neueId);

                    DataTable innen = vorgang.Lese("SELECT id, name FROM energy_carrier WHERE name = ?",
                                                   new DbParam("?", PROBE_COMMIT));
                    fall.Muss(innen.Rows.Count == 1, "Lese im Vorgang fand " + innen.Rows.Count + " Zeilen statt 1");
                    if (innen.Rows.Count == 1)
                        fall.Muss(Convert.ToInt32(innen.Rows[0]["id"]) == neueId,
                                  "last_insert_rowid passt nicht zur gelesenen id");

                    vorgang.Commit();
                }
                fall.Muss(TraegerAnzahl(PROBE_COMMIT) == 1, "Commit: Zeile fehlt");
                DataRepository.ExecuteSQL("DELETE FROM energy_carrier WHERE id = ?", new DbParam("?", neueId));
                fall.Muss(TraegerAnzahl(PROBE_COMMIT) == 0, "Aufraeumen nach Commit misslungen");

                // --- Dispose ohne Commit ---
                using (DbVorgang vorgang = DataRepository.Vorgang())
                {
                    vorgang.Ausfuehren(EinfuegenSql(),
                                       new DbParam("?", PROBE_DISPOSE), new DbParam("?", "S4A_DP"));
                }
                fall.Muss(TraegerAnzahl(PROBE_DISPOSE) == 0, "Dispose ohne Commit: Zeile ist trotzdem da");
            });
        }

        private static void Fall09SchemaAuskunft()
        {
            Fuehre("9  Schema-Auskunft", fall =>
            {
                fall.Muss(DataRepository.TabelleVorhanden("Tab_Projekt"), "Tab_Projekt gilt als nicht vorhanden");
                fall.Muss(!DataRepository.TabelleVorhanden("Tab_GibtsNicht"), "Tab_GibtsNicht gilt als vorhanden");

                fall.Muss(DataRepository.SpalteVorhanden("Tab_Projekt", "Projektname"),
                          "Spalte Projektname gilt als nicht vorhanden");
                fall.Muss(!DataRepository.SpalteVorhanden("Tab_Projekt", "GibtsNicht"),
                          "Spalte GibtsNicht gilt als vorhanden");

                int erwartet = Ganzzahl("SELECT COUNT(*) FROM pragma_table_info('Tab_Projekt')");
                List<string> spalten = DataRepository.SpaltenVonTabelle("Tab_Projekt");
                fall.Muss(spalten.Count == erwartet,
                          "SpaltenVonTabelle zaehlt " + spalten.Count + ", PRAGMA table_info " + erwartet);
                fall.Muss(spalten.Count > 0 && spalten[0] == "ID",
                          "erste Spalte ist " + (spalten.Count > 0 ? spalten[0] : "(keine)") + " statt ID");

                DataTable indizes = DataRepository.IndexListe("Tab_Projekt");
                fall.Muss(indizes.Rows.Count > 0, "IndexListe lieferte keine Zeile");

                DataTable fks = DataRepository.FremdschluesselListe("Tab_Gebaeude");
                fall.Muss(fks.Rows.Count > 0, "FremdschluesselListe zu Tab_Gebaeude lieferte keine Zeile");
                if (fks.Rows.Count > 0)
                    fall.Muss(Convert.ToString(fks.Rows[0]["Zieltabelle"]) == "Z_ProjektGebaeude",
                              "Zieltabelle ist " + fks.Rows[0]["Zieltabelle"] + " statt Z_ProjektGebaeude");
            });
        }

        private static void Fall10Fremdschluessel()
        {
            Fuehre("10 Fremdschluessel greifen (INSERT mit unbekanntem Elternwert)", fall =>
            {
                StilleMeldungenEinsammeln();   // Sammlung leeren, damit der naechste Fehler eindeutig ist

                bool ok = DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Gebaeude (ID_ProjektGebaeude, ID_Projekt, Gebaeudename, Typ) VALUES (?, ?, ?, ?)",
                    new DbParam("?", 999999),
                    new DbParam("?", 999999),
                    new DbParam("?", PROBE_FK),
                    new DbParam("?", "Probe"));

                string[] gesammelt = DataRepository.StilleFehlerAbholen();
                _stilleMeldungen.AddRange(gesammelt);

                fall.Muss(!ok, "ExecuteSQL lieferte true - der Fremdschluessel greift nicht");
                fall.Muss(gesammelt.Length > 0, "kein stiller Fehler gesammelt");

                int da = Ganzzahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Gebaeudename = '" + PROBE_FK + "'");
                fall.Muss(da == 0, "Zeile trotz Fremdschluesselverletzung vorhanden (" + da + ")");
            });
        }

        /// <summary>
        /// ARBEITSPAKET S4e: Bis dahin pruefte dieser Fall, dass der werfende Stub
        /// <c>DataRepository.BeginTransaction()</c> eine NotSupportedException liefert.
        /// Mit S4e ist der Stub ERSATZLOS geloescht - der Fall pruefte damit nichts mehr.
        /// An seine Stelle tritt die Reflexionspruefung: Die Zugriffsschicht darf ueberhaupt
        /// keine Methode dieses Namens mehr besitzen (kein Weg mehr an <c>Vorgang()</c>
        /// vorbei), und <c>Vorgang()</c> muss vorhanden sein und einen <c>DbVorgang</c>
        /// liefern.
        /// </summary>
        private static void Fall11KeinBeginTransaction()
        {
            Fuehre("11 DataRepository besitzt keine Methode BeginTransaction mehr", fall =>
            {
                Type t = typeof(DataRepository);

                System.Reflection.MethodInfo[] alle = t.GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.DeclaredOnly);

                var treffer = new List<string>();
                foreach (System.Reflection.MethodInfo m in alle)
                    if (m.Name.IndexOf("BeginTransaction", StringComparison.OrdinalIgnoreCase) >= 0)
                        treffer.Add(m.Name);

                fall.Muss(treffer.Count == 0,
                          "BeginTransaction ist noch vorhanden: " + string.Join(", ", treffer.ToArray()));

                System.Reflection.MethodInfo vorgang = t.GetMethod("Vorgang",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                fall.Muss(vorgang != null, "Vorgang() fehlt - es gibt keinen Weg in eine Transaktion");
                if (vorgang != null)
                    fall.Muss(vorgang.ReturnType == typeof(DbVorgang),
                              "Vorgang() liefert " + vorgang.ReturnType.Name + " statt DbVorgang");
            });
        }

        private static void Fall12DatenbankVorhanden(string kopie)
        {
            Fuehre("12 DatenbankVorhanden", fall =>
            {
                fall.Muss(DataRepository.DatenbankVorhanden(), "Arbeitskopie gilt als nicht vorhanden: " + kopie);

                string sicherung = DataRepository.PfadUeberschreibung;
                try
                {
                    DataRepository.PfadUeberschreibung = Path.Combine(
                        Path.GetTempPath(), "gibt-es-nicht-" + Guid.NewGuid().ToString("N") + ".sqlite");
                    fall.Muss(!DataRepository.DatenbankVorhanden(), "Fantasiepfad gilt als vorhanden");
                }
                finally
                {
                    DataRepository.PfadUeberschreibung = sicherung;
                }

                fall.Muss(DataRepository.DatenbankVorhanden(), "nach der Ruecknahme wieder falsch");
            });
        }


        // =============================================================================
        // ARBEITSPAKET S6 - die Schemapflege-Gabelung (Faelle 13 bis 15)
        // =============================================================================

        /// <summary>
        /// FALL 13 - der SQLite-Zweig des Normalstarts.
        ///
        /// Drei Laeufe auf EINER Wegwerf-Kopie, in dieser Reihenfolge, damit die
        /// prozessweiten Statuswerte (MigrationOk, SimulationGesperrt) am Ende im
        /// gutartigen Zustand stehen:
        ///
        ///   a) Stand 0   -> false, Bericht nennt die fehlende Schemaversion, gesperrt
        ///   b) Stand 60  -> false, Bericht nennt den Freeze-Stand 61, gesperrt
        ///   c) Stand 61  -> true,  keine Fehlerzeile, Stand bleibt 61, NICHT gesperrt
        ///
        /// Der eigentliche Nachweis steckt in (c): Der Bericht darf KEINE Spur eines
        /// Access-Schritts tragen - kein "Bootstrap", kein "Schritt n", keine
        /// Abschlusspruefung. Alles davon liefe ueber Lauf.Conn, und die ist im
        /// SQLite-Zweig gar nicht erst offen.
        /// </summary>
        private static void Fall13SqliteZweig(string quelle, string arbeitsordner)
        {
            Fuehre("13 SchemaMigration.Ausfuehren faehrt NUR den SQLite-Zweig", fall =>
            {
                // Eigener Unterordner je Fall: SchemaMigration legt neben der Datenbank
                // ein migration_protokoll.txt ab. Gemeinsamer Ordner hiesse, dass die
                // Faelle sich gegenseitig das Protokoll ueberschreiben - und genau das
                // ist der Beleg, den man nach einem Fehlschlag lesen will.
                string kopie = KopieAnlegen(quelle, Path.Combine(arbeitsordner, "fall13"),
                                            "Kenndaten_S6_Fall13.sqlite");
                string sicherung = DataRepository.PfadUeberschreibung;
                DataRepository.PfadUeberschreibung = kopie;
                try
                {
                    string bericht;
                    string grund;

                    // --- a) ohne Schemaversion --------------------------------------
                    SchemaVersionSetzen(0);
                    fall.Muss(!SchemaMigration.Ausfuehren(out bericht),
                              "Stand 0 wurde als erfolgreiche Migration gewertet");
                    fall.Muss(bericht.IndexOf("keine Schemaversion", StringComparison.OrdinalIgnoreCase) >= 0,
                              "Stand 0: der Bericht nennt die fehlende Schemaversion nicht: " + Erste(bericht));
                    fall.Muss(SchemaMigration.SimulationGesperrt(out grund),
                              "Stand 0: die Simulation ist NICHT gesperrt");

                    // --- b) unterhalb des Freeze-Stands ------------------------------
                    SchemaVersionSetzen(60);
                    fall.Muss(!SchemaMigration.Ausfuehren(out bericht),
                              "Stand 60 wurde als erfolgreiche Migration gewertet");
                    fall.Muss(bericht.IndexOf("Freeze-Stand 61", StringComparison.Ordinal) >= 0,
                              "Stand 60: der Bericht nennt den Freeze-Stand nicht: " + Erste(bericht));
                    fall.Muss(bericht.IndexOf("EposSqliteMigrator", StringComparison.Ordinal) >= 0,
                              "Stand 60: der Bericht nennt den Weg zur Erstmigration nicht");
                    fall.Muss(SchemaMigration.SimulationGesperrt(out grund),
                              "Stand 60: die Simulation ist NICHT gesperrt");

                    // --- c) auf Freeze-Stand ----------------------------------------
                    // Ab hier laeuft der SQLite-Zweig seine EIGENEN Schritte (ab 62,
                    // erster Eintrag: die PV-Anlagenparameter des Pakets A). Die
                    // Erwartung ist deshalb nicht mehr "Stand bleibt 61", sondern
                    // "Stand steht danach auf ZIEL_VERSION" - relativiert, damit der
                    // Fall den naechsten Schritt ueberlebt.
                    SchemaVersionSetzen(SchemaMigration.FREEZE_VERSION_ACCESS);
                    fall.Muss(SchemaMigration.Ausfuehren(out bericht),
                              "Stand " + SchemaMigration.FREEZE_VERSION_ACCESS +
                              " lief NICHT durch. Bericht: " + Erste(bericht));

                    fall.Muss(bericht.IndexOf("FEHLGESCHLAGEN", StringComparison.Ordinal) < 0,
                              "der Bericht enthaelt eine Fehlerzeile");
                    fall.Muss(bericht.IndexOf("ABBRUCH", StringComparison.Ordinal) < 0,
                              "der Bericht enthaelt eine Abbruchzeile");

                    // Kein Access-Schritt gefahren - das ist der Kern dieses Falls.
                    fall.Muss(bericht.IndexOf("Bootstrap", StringComparison.OrdinalIgnoreCase) < 0,
                              "der Bericht nennt den Bootstrap des Access-Zweigs");
                    // Frueher: "gar kein Schritt" - das ging, solange SCHRITTE_SQLITE leer
                    // war. Der Access-Zweig listet JEDEN seiner Schritte auf (mindestens
                    // als "bereits erledigt"); "Schritt 61" ist deshalb der Nachweis, dass
                    // er NICHT gelaufen ist - die SQLite-Liste beginnt bei 62.
                    fall.Muss(bericht.IndexOf("Schritt 61", StringComparison.Ordinal) < 0,
                              "der Bericht nennt einen Schritt des Access-Zweigs");
                    fall.Muss(bericht.IndexOf("Abschlusspr", StringComparison.OrdinalIgnoreCase) < 0,
                              "der Bericht nennt eine Abschlusspruefung des Access-Zweigs");

                    fall.Muss(bericht.IndexOf("Schemastand nachher: " + SchemaMigration.ZIEL_VERSION,
                                              StringComparison.Ordinal) >= 0,
                              "der Bericht meldet nicht den Schemastand " +
                              SchemaMigration.ZIEL_VERSION + ": " + Erste(bericht));

                    Gleich(fall, "StandVorher", SchemaMigration.FREEZE_VERSION_ACCESS, SchemaMigration.StandVorher);
                    Gleich(fall, "StandNachher", SchemaMigration.ZIEL_VERSION, SchemaMigration.StandNachher);
                    Gleich(fall, "SchemaVersion in der Datei", SchemaMigration.ZIEL_VERSION, SchemaVersionLesen());

                    fall.Muss(!SchemaMigration.SimulationGesperrt(out grund),
                              "die Simulation ist auf Zielstand gesperrt: " + grund);
                }
                finally
                {
                    DataRepository.PfadUeberschreibung = sicherung;
                }
            });
        }

        /// <summary>
        /// FALL 14 - ein synthetischer SQLite-Schritt ueber den Test-Seam.
        ///
        /// Der Seam (SchemaMigration.ProbeSchritt*, per Reflexion befuellt - Muster wie
        /// Fall 11) haengt einen Wegwerf-Schritt ein, der ueber den DDL-Rueckruf - und
        /// damit ueber SqliteDdl - eine Spalte anlegt.
        ///
        ///   Lauf 1: Schritt laeuft, Spalte da, Marker auf ZIEL_VERSION + 1
        ///   Lauf 2: "bereits erledigt", nichts passiert, Marker bleibt stehen
        ///
        /// DIE NUMMER IST RELATIV (ZIEL_VERSION + 1), nicht fest 62. Seit Paket A des
        /// PV-Ertragsmodells ist 62 ein ECHTER Schritt; eine feste 62 im Seam waere nach
        /// dessen Lauf "bereits erledigt" und der Wegwerf-Schritt liefe nie. Die Probe
        /// setzt den Marker deshalb auf ZIEL_VERSION (alle echten Schritte erledigt) und
        /// haengt ihren Schritt eine Nummer darueber ein.
        ///
        /// Der Seam wird danach wieder geleert; im Programmbetrieb ist er unbesetzt.
        /// </summary>
        private static void Fall14SqliteSchritt(string quelle, string arbeitsordner)
        {
            int probeNr = SchemaMigration.ZIEL_VERSION + 1;

            Fuehre("14 synthetischer SQLite-Schritt " + probeNr +
                   " ueber SqliteDdl (Marker + Idempotenz)", fall =>
            {
                string kopie = KopieAnlegen(quelle, Path.Combine(arbeitsordner, "fall14"),
                                            "Kenndaten_S6_Fall14.sqlite");
                string sicherung = DataRepository.PfadUeberschreibung;
                DataRepository.PfadUeberschreibung = kopie;
                try
                {
                    // Auf ZIEL_VERSION, nicht auf den Freeze-Stand: Die echten Schritte
                    // gelten damit als erledigt, und was der Bericht meldet, ist allein
                    // der Wegwerf-Schritt.
                    SchemaVersionSetzen(SchemaMigration.ZIEL_VERSION);

                    bool gesetzt = SeamSetzen(
                        probeNr,
                        "Wegwerfspalte " + PROBE_SPALTE + " (S6-Probe)",
                        ddl => ddl("ALTER TABLE [Tab_Applikation] ADD COLUMN [" + PROBE_SPALTE +
                                   "] INTEGER DEFAULT 0",
                                   "Tab_Applikation." + PROBE_SPALTE));
                    fall.Muss(gesetzt, "der Test-Seam liess sich nicht befuellen - Felder umbenannt?");
                    if (!gesetzt) return;

                    string schrittWort = "Schritt " + probeNr;

                    // --- Lauf 1 -----------------------------------------------------
                    string bericht1;
                    fall.Muss(SchemaMigration.Ausfuehren(out bericht1),
                              "Lauf 1 lief nicht durch. Bericht: " + Erste(bericht1));
                    fall.Muss(bericht1.IndexOf(schrittWort, StringComparison.Ordinal) >= 0,
                              "Lauf 1: der Bericht nennt " + schrittWort + " nicht");
                    fall.Muss(bericht1.IndexOf(schrittWort, StringComparison.Ordinal) >= 0 &&
                              bericht1.IndexOf(": OK", StringComparison.Ordinal) >= 0,
                              "Lauf 1: " + schrittWort + " wurde nicht als OK gemeldet");
                    fall.Muss(bericht1.IndexOf("angelegt", StringComparison.Ordinal) >= 0,
                              "Lauf 1: SqliteDdl notierte kein \"angelegt\"");

                    fall.Muss(DataRepository.SpalteVorhanden("Tab_Applikation", PROBE_SPALTE),
                              "Lauf 1: die Testspalte wurde nicht angelegt");
                    Gleich(fall, "Lauf 1 SchemaVersion", probeNr, SchemaVersionLesen());
                    Gleich(fall, "Lauf 1 StandNachher", probeNr, SchemaMigration.StandNachher);

                    // --- Lauf 2 (idempotent) ----------------------------------------
                    string bericht2;
                    fall.Muss(SchemaMigration.Ausfuehren(out bericht2),
                              "Lauf 2 lief nicht durch. Bericht: " + Erste(bericht2));
                    fall.Muss(bericht2.IndexOf("bereits erledigt", StringComparison.Ordinal) >= 0,
                              "Lauf 2: " + schrittWort + " wurde nicht als \"bereits erledigt\" gemeldet");
                    fall.Muss(bericht2.IndexOf("angelegt", StringComparison.Ordinal) < 0,
                              "Lauf 2: es wurde erneut DDL gefahren");
                    fall.Muss(bericht2.IndexOf("FEHLGESCHLAGEN", StringComparison.Ordinal) < 0,
                              "Lauf 2: der Bericht enthaelt eine Fehlerzeile");

                    Gleich(fall, "Lauf 2 SchemaVersion", probeNr, SchemaVersionLesen());
                    Gleich(fall, "Lauf 2 StandVorher", probeNr, SchemaMigration.StandVorher);
                    Gleich(fall, "Lauf 2 StandNachher", probeNr, SchemaMigration.StandNachher);

                    // Die Spalte genau EINMAL - eine zweite haette SQLite ohnehin
                    // abgelehnt, aber der Nachweis gehoert hierher.
                    int spalten = 0;
                    foreach (string s in DataRepository.SpaltenVonTabelle("Tab_Applikation"))
                        if (string.Equals(s, PROBE_SPALTE, StringComparison.OrdinalIgnoreCase)) spalten++;
                    Gleich(fall, "Anzahl Testspalten", 1, spalten);
                }
                finally
                {
                    SeamLeeren();
                    DataRepository.PfadUeberschreibung = sicherung;
                }
            });
        }

        /// <summary>
        /// FALL 15 - der eingefrorene Access-Zweig ueber HebeAltbestand.
        ///
        /// Faehrt gegen eine KOPIE der Live-.accdb (Vorgabe C:\ProgramData\EPOS_PLAN\
        /// Kenndaten.accdb, ueberschreibbar mit --altbestand=). Deren Stand ist 61, der
        /// Lauf muss also durchkommen und ausschliesslich "bereits erledigt" melden - der
        /// Nachweis, dass der Zweig nach der Gabelung noch faehrt und dass er seine
        /// Verbindung NICHT mehr aus DataRepository zieht (die zeigt hier auf eine
        /// SQLite-Datei; mit ihr waere kein einziger Schritt lesbar).
        ///
        /// Ist ACE/OleDb nicht verfuegbar oder fehlt die Datei, wird der Fall MIT GRUND
        /// uebersprungen statt als Fehlschlag gewertet.
        /// </summary>
        private static void Fall15Altbestand(string[] args, string arbeitsordner)
        {
            const string BEZEICHNUNG = "15 HebeAltbestand auf einer Kopie der Live-.accdb (Access-Zweig)";

            string quelle = Argument(args, "--altbestand") ?? ALTBESTAND_VORGABE;
            if (!File.Exists(quelle))
            {
                Ueberspringe(BEZEICHNUNG, "Altbestand nicht vorhanden: " + quelle +
                                          " (mit --altbestand=<Pfad> setzen).");
                return;
            }

            if (!AceVerfuegbar())
            {
                Ueberspringe(BEZEICHNUNG,
                             "Microsoft.ACE.OLEDB.12.0 ist im Probenkontext nicht verfuegbar " +
                             "(Provider nicht registriert oder Bitness passt nicht).");
                return;
            }

            string ordner = Path.Combine(arbeitsordner, "fall15");
            string kopie = null;
            try
            {
                Directory.CreateDirectory(ordner);
                kopie = Path.Combine(ordner, "Kenndaten_S6_Fall15.accdb");
                Console.WriteLine("       (Fall 15 kopiert " +
                                  (new FileInfo(quelle).Length / (1024 * 1024)) + " MB - das dauert.)");
                DateiEntfernen(kopie);
                File.Copy(quelle, kopie, true);
                new FileInfo(kopie).IsReadOnly = false;
            }
            catch (Exception ex)
            {
                Ueberspringe(BEZEICHNUNG, "Kopie des Altbestands misslang: " + ex.Message);
                DateiEntfernen(kopie);
                return;
            }

            string wegwerf = kopie;
            Fuehre(BEZEICHNUNG, fall =>
            {
                string bericht;
                bool ok = SchemaMigration.HebeAltbestand(wegwerf, out bericht);

                fall.Muss(ok, "HebeAltbestand lieferte false. Bericht: " + Erste(bericht));
                fall.Muss(bericht.IndexOf("bereits erledigt", StringComparison.Ordinal) >= 0,
                          "der Bericht enthaelt keine \"bereits erledigt\"-Zeile: " + Erste(bericht));
                fall.Muss(bericht.IndexOf("FEHLGESCHLAGEN", StringComparison.Ordinal) < 0,
                          "der Bericht enthaelt eine Fehlerzeile: " + Erste(bericht));
                fall.Muss(bericht.IndexOf("Schemastand nachher: 61", StringComparison.Ordinal) >= 0,
                          "der Bericht meldet nicht den Schemastand 61: " + Erste(bericht));
                fall.Muss(bericht.IndexOf("Bootstrap Schemamarker", StringComparison.Ordinal) >= 0,
                          "der Bericht meldet keinen Bootstrap - der Access-Zweig lief also nicht");

                Gleich(fall, "StandVorher", 61, SchemaMigration.StandVorher);
                Gleich(fall, "StandNachher", 61, SchemaMigration.StandNachher);
            });

            // Die 144-MB-Kopie geht sofort wieder weg; das Protokoll des Laufs bleibt
            // als Beleg liegen.
            DateiEntfernen(kopie);
        }

        /// <summary>
        /// FALL 16 - der ERSTSTART-ASSISTENT (Arbeitspaket S8), kopfueber und ohne
        /// Oberflaeche.
        ///
        /// Baut einen Wegwerf-Ordner mit einer KOPIE der Live-.accdb und faehrt darauf
        /// den vollstaendigen Ablauf aus <see cref="ErststartMigration"/>:
        /// Lagebild -> Alt-Hebung -> Migration -> Umbenennung. Geprueft wird
        ///   * Pruefe() vorher   = NurAccdbVorhanden,
        ///   * Fuehredurch()     = true, Kenndaten.sqlite entstanden, Bericht daneben,
        ///                         Datenbeweis vollstaendig (alle Tabellen gleich),
        ///   * die .accdb heisst danach Kenndaten.vor-sqlite.accdb,
        ///   * Pruefe() nachher  = SqliteVorhanden,
        ///   * ein ZWEITER Aufruf verweigert mit "Nichts zu tun" und fasst nichts an.
        ///
        /// <c>settingsFixup</c> ist hier IMMER <c>false</c> - die Einstellungen des
        /// Anwenders werden von einer Probe nicht angefasst. Dass das eingehalten wurde,
        /// wird am Ende gegen den gespeicherten <c>DBName</c> nachgemessen.
        ///
        /// Der Ordner geht danach vollstaendig weg (Kopie + SQLite + Bericht); die
        /// Kennzahlen des Migrators stehen vorher in der Konsolenausgabe.
        /// </summary>
        private static void Fall16Erststart(string[] args, string arbeitsordner)
        {
            const string BEZEICHNUNG = "16 Erststart-Assistent auf einer Kopie der Live-.accdb (S8)";

            string quelle = Argument(args, "--altbestand") ?? ALTBESTAND_VORGABE;
            if (!File.Exists(quelle))
            {
                Ueberspringe(BEZEICHNUNG, "Altbestand nicht vorhanden: " + quelle +
                                          " (mit --altbestand=<Pfad> setzen).");
                return;
            }

            if (!AceVerfuegbar())
            {
                Ueberspringe(BEZEICHNUNG,
                             "Microsoft.ACE.OLEDB.12.0 ist im Probenkontext nicht verfuegbar " +
                             "(Provider nicht registriert oder Bitness passt nicht).");
                return;
            }

            // Waechter: eine .laccdb neben der Quelle heisst, dass der Bestand gerade
            // offen ist - eine Kopie davon waere ein halber Stand.
            string sperre = Path.ChangeExtension(quelle, ".laccdb");
            if (File.Exists(sperre))
            {
                Ueberspringe(BEZEICHNUNG,
                             "Neben dem Altbestand liegt die Sperrdatei " + sperre +
                             " - der Bestand ist geoeffnet. EPOS-Plan und Access schliessen.");
                return;
            }

            string ordner = Path.Combine(arbeitsordner, "fall16");
            string accdb = Path.Combine(ordner, "Kenndaten.accdb");
            string sqlite = Path.Combine(ordner, "Kenndaten.sqlite");
            string rueckfall = Path.Combine(ordner, "Kenndaten.vor-sqlite.accdb");

            try
            {
                OrdnerLeeren(ordner);
                Directory.CreateDirectory(ordner);
                Console.WriteLine("       (Fall 16 kopiert " +
                                  (new FileInfo(quelle).Length / (1024 * 1024)) +
                                  " MB und migriert sie - das dauert einige Minuten.)");
                File.Copy(quelle, accdb, true);
                new FileInfo(accdb).IsReadOnly = false;
            }
            catch (Exception ex)
            {
                Ueberspringe(BEZEICHNUNG, "Wegwerf-Ordner liess sich nicht herrichten: " + ex.Message);
                OrdnerLeeren(ordner);
                return;
            }

            string dbNameVorher = GespeicherterDbName();

            Fuehre(BEZEICHNUNG, fall =>
            {
                Gleich(fall, "Pruefe vor dem Lauf",
                       ErststartLage.NurAccdbVorhanden, ErststartMigration.Pruefe(ordner));

                Sammler sammler = new Sammler();
                string bericht;
                bool ok = ErststartMigration.Fuehredurch(ordner, sammler, false, out bericht);

                fall.Muss(ok, "Fuehredurch lieferte false: " + ErststartMigration.LetzteMeldung);
                fall.Muss(sammler.Zeilen.Count > 0, "der Assistent meldete keinen einzigen Fortschritt");

                fall.Muss(File.Exists(sqlite), "Kenndaten.sqlite ist nicht entstanden: " + sqlite);
                fall.Muss(!File.Exists(accdb), "Kenndaten.accdb liegt noch da - nicht umbenannt");
                fall.Muss(File.Exists(rueckfall),
                          "die Rueckfallebene Kenndaten.vor-sqlite.accdb fehlt: " + rueckfall);
                fall.Muss(!string.IsNullOrEmpty(bericht) && File.Exists(bericht),
                          "der Migrationsbericht fehlt: " + (bericht ?? "(kein Pfad)"));

                fall.Muss(ErststartMigration.LetzteTabellen > 0, "kein Datenbeweis im Ergebnis");
                Gleich(fall, "Datenbeweis (Tabellen mit gleicher Zeilenzahl und Pruefsumme)",
                       ErststartMigration.LetzteTabellen, ErststartMigration.LetzteTabellenOk);

                if (!string.IsNullOrEmpty(bericht) && File.Exists(bericht))
                {
                    string text = File.ReadAllText(bericht);
                    fall.Muss(text.IndexOf("Datenbeweis bestanden", StringComparison.Ordinal) >= 0,
                              "der Bericht meldet keinen bestandenen Datenbeweis");
                    BerichtKennzahlen(bericht, text);
                }

                Console.WriteLine("        Migrator: " + ErststartMigration.LetzteTabellenOk + "/" +
                                  ErststartMigration.LetzteTabellen +
                                  " Tabellen bewiesen, " +
                                  ErststartMigration.LetzteZeilen.ToString("N0", new CultureInfo("de-DE")) +
                                  " Zeilen, Zieldatei " +
                                  (new FileInfo(sqlite).Length / (1024 * 1024)) + " MB.");

                Gleich(fall, "Pruefe nach dem Lauf",
                       ErststartLage.SqliteVorhanden, ErststartMigration.Pruefe(ordner));

                // --- der ZWEITE Aufruf muss verweigern und nichts anfassen ------------
                long groesseVorher = new FileInfo(sqlite).Length;
                DateTime standVorher = new FileInfo(sqlite).LastWriteTimeUtc;

                string bericht2;
                bool ok2 = ErststartMigration.Fuehredurch(ordner, null, false, out bericht2);

                fall.Muss(!ok2, "der zweite Aufruf lief durch, statt zu verweigern");
                fall.Muss(bericht2 == null, "der zweite Aufruf lieferte einen Berichtspfad: " + bericht2);
                fall.Muss(ErststartMigration.LetzteMeldung
                              .IndexOf("Nichts zu tun", StringComparison.Ordinal) >= 0,
                          "die Verweigerung sagt nicht \"Nichts zu tun\": " +
                          ErststartMigration.LetzteMeldung);
                fall.Muss(new FileInfo(sqlite).Length == groesseVorher &&
                          new FileInfo(sqlite).LastWriteTimeUtc == standVorher,
                          "der zweite Aufruf hat die SQLite-Datei angefasst");
                fall.Muss(File.Exists(rueckfall) && !File.Exists(accdb),
                          "der zweite Aufruf hat an den Dateinamen gedreht");

                // --- settingsFixup war false: der gespeicherte DBName ist unberuehrt ---
                string dbNameNachher = GespeicherterDbName();
                fall.Muss(dbNameVorher == dbNameNachher,
                          "der gespeicherte DBName hat sich geaendert (\"" + dbNameVorher +
                          "\" -> \"" + dbNameNachher + "\") - settingsFixup war false");
                Console.WriteLine("        Settings.DBName unveraendert: " + (dbNameVorher ?? "(nicht lesbar)"));
            });

            OrdnerLeeren(ordner);
        }

        /// <summary>Fortschrittsempfaenger, der synchron in eine Liste schreibt.</summary>
        /// <remarks>
        /// Bewusst KEIN <see cref="Progress{T}"/>: Ohne Oberflaechen-Kontext wuerde der
        /// seine Meldungen ueber den Threadpool zustellen - die Liste waere beim Pruefen
        /// noch nicht fertig.
        /// </remarks>
        private sealed class Sammler : IProgress<string>
        {
            internal readonly List<string> Zeilen = new List<string>();

            public void Report(string wert)
            {
                Zeilen.Add(wert ?? "");
            }
        }

        /// <summary>
        /// Zitiert die Kennzahlen aus dem Migrationsbericht in die Konsole - der Ordner
        /// wird danach geloescht, das Protokoll des Probenlaufs bleibt der Beleg.
        /// </summary>
        private static void BerichtKennzahlen(string pfad, string text)
        {
            Console.WriteLine("        Bericht: " + pfad);
            foreach (string z in text.Replace("\r\n", "\n").Split('\n'))
            {
                string t = z.Trim();
                if (t.StartsWith("| Tabellen migriert", StringComparison.Ordinal) ||
                    t.StartsWith("| Zeilen gesamt", StringComparison.Ordinal) ||
                    t.StartsWith("| Schemastand", StringComparison.Ordinal) ||
                    t.StartsWith("| Exit-Code", StringComparison.Ordinal) ||
                    t.StartsWith("**Datenbeweis bestanden", StringComparison.Ordinal) ||
                    t.StartsWith("- `PRAGMA", StringComparison.Ordinal))
                    Console.WriteLine("          " + t);
            }
        }

        /// <summary>
        /// Liest den gespeicherten <c>DBName</c> per Reflexion aus den Einstellungen der
        /// Anwendung (<c>Properties.Settings</c> ist <c>internal</c>). Nur LESEND - die
        /// Proben schreiben nie in die Einstellungen des Anwenders.
        /// Liefert <c>null</c>, wenn sich der Wert nicht lesen laesst; das ist kein
        /// Fehlschlag, sondern macht den Vergleich lediglich wirkungslos.
        /// </summary>
        private static string GespeicherterDbName()
        {
            try
            {
                Type t = typeof(DataRepository).Assembly
                    .GetType("WindowsFormsApplication1.Properties.Settings");
                if (t == null) return null;

                System.Reflection.PropertyInfo pDefault = t.GetProperty(
                    "Default",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static);
                if (pDefault == null) return null;

                object inst = pDefault.GetValue(null);
                System.Reflection.PropertyInfo pName = t.GetProperty("DBName");
                if (inst == null || pName == null) return null;

                return Convert.ToString(pName.GetValue(inst));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Loescht einen Wegwerf-Ordner samt Inhalt, ohne je zu stoeren.</summary>
        private static void OrdnerLeeren(string ordner)
        {
            try
            {
                if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("       (Hinweis: " + ordner + " liess sich nicht raeumen: " +
                                  ex.Message + ")");
            }
        }

        /// <summary>Laesst sich der ACE-Provider ueberhaupt laden?</summary>
        private static bool AceVerfuegbar()
        {
            try
            {
                using (OleDbConnection probe =
                           new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=;"))
                {
                    try { probe.Open(); }
                    catch (Exception ex)
                    {
                        // "Provider nicht registriert" ist das Aus; jeder andere Fehler
                        // (leerer Pfad, Datei nicht gefunden) beweist gerade, dass der
                        // Provider da ist.
                        string m = (ex.Message ?? "").ToLowerInvariant();
                        if (m.Contains("nicht registriert") || m.Contains("not registered") ||
                            m.Contains("provider cannot be found") ||
                            m.Contains("provider konnte nicht gefunden"))
                            return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Befuellt den Test-Seam der SchemaMigration per Reflexion (Muster Fall 11).</summary>
        private static bool SeamSetzen(int nr, string name, Func<Func<string, string, bool>, bool> aktion)
        {
            System.Reflection.FieldInfo fNr = SeamFeld("ProbeSchrittNr");
            System.Reflection.FieldInfo fName = SeamFeld("ProbeSchrittName");
            System.Reflection.FieldInfo fAktion = SeamFeld("ProbeSchrittAktion");
            if (fNr == null || fName == null || fAktion == null) return false;

            fNr.SetValue(null, nr);
            fName.SetValue(null, name);
            fAktion.SetValue(null, aktion);
            return true;
        }

        private static void SeamLeeren()
        {
            System.Reflection.FieldInfo fNr = SeamFeld("ProbeSchrittNr");
            System.Reflection.FieldInfo fName = SeamFeld("ProbeSchrittName");
            System.Reflection.FieldInfo fAktion = SeamFeld("ProbeSchrittAktion");
            if (fNr != null) fNr.SetValue(null, 0);
            if (fName != null) fName.SetValue(null, null);
            if (fAktion != null) fAktion.SetValue(null, null);
        }

        private static System.Reflection.FieldInfo SeamFeld(string name)
        {
            return typeof(SchemaMigration).GetField(
                name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static);
        }

        /// <summary>Schemastand der DATEI, auf die PfadUeberschreibung gerade zeigt.</summary>
        private static int SchemaVersionLesen()
        {
            return Ganzzahl("SELECT SchemaVersion FROM Tab_Applikation LIMIT 1");
        }

        private static void SchemaVersionSetzen(int version)
        {
            DataRepository.ExecuteSQL("UPDATE Tab_Applikation SET SchemaVersion = ?",
                                      new DbParam("?", version));
        }

        /// <summary>Die ersten Zeilen eines Berichts - fuer sprechende Mangelmeldungen.</summary>
        private static string Erste(string bericht)
        {
            if (string.IsNullOrEmpty(bericht)) return "(leer)";
            string[] zeilen = bericht.Replace("\r\n", "\n").Split('\n');
            var kopf = new List<string>();
            foreach (string z in zeilen)
            {
                kopf.Add(z.Trim());
                if (kopf.Count >= 6) break;
            }
            return " >> " + string.Join(" / ", kopf.ToArray());
        }

        private static void DateiEntfernen(string pfad)
        {
            if (string.IsNullOrEmpty(pfad)) return;
            try { if (File.Exists(pfad)) File.Delete(pfad); } catch (Exception) { }
        }


        // =============================================================================
        // Hilfsmittel
        // =============================================================================

        private sealed class Fall
        {
            internal readonly List<string> Maengel = new List<string>();
            internal void Muss(bool bedingung, string beschreibung)
            {
                if (!bedingung) Maengel.Add(beschreibung);
            }
        }

        private static void Fuehre(string bezeichnung, Action<Fall> koerper)
        {
            _faelle++;
            Fall fall = new Fall();
            try
            {
                koerper(fall);
            }
            catch (Exception ex)
            {
                fall.Muss(false, "Ausnahme " + ex.GetType().Name + ": " + ex.Message);
            }

            if (fall.Maengel.Count == 0)
            {
                Console.WriteLine("PASS  " + bezeichnung);
                return;
            }

            _fehlschlaege++;
            Console.WriteLine("FAIL  " + bezeichnung);
            foreach (string m in fall.Maengel) Console.WriteLine("        - " + m);
        }

        /// <summary>
        /// Zaehlt einen Fall als NICHT durchgefuehrt - weder bestanden noch
        /// fehlgeschlagen. Der Grund gehoert immer dazu: Ein stiller Skip liest sich in
        /// der Ergebniszeile wie ein bestandener Fall.
        /// </summary>
        private static void Ueberspringe(string bezeichnung, string grund)
        {
            _faelle++;
            _uebersprungen++;
            Console.WriteLine("SKIP  " + bezeichnung);
            Console.WriteLine("        Grund: " + grund);
        }

        private static void Gleich(Fall fall, string was, object erwartet, object tatsaechlich)
        {
            bool gleich = erwartet == null ? tatsaechlich == null : erwartet.Equals(tatsaechlich);
            fall.Muss(gleich, was + ": erwartet <" + Anzeige(erwartet) + ">, erhalten <" + Anzeige(tatsaechlich) + ">");
        }

        private static string Anzeige(object w)
        {
            if (w == null) return "null";
            if (w == DBNull.Value) return "DBNull";
            return Convert.ToString(w, CultureInfo.InvariantCulture) + " (" + Typname(w) + ")";
        }

        private static string Typname(object w)
        {
            if (w == null) return "null";
            if (w == DBNull.Value) return "DBNull";
            return w.GetType().Name;
        }

        private static string EinfuegenSql()
        {
            return "INSERT INTO energy_carrier (name, code, group_code, is_active) VALUES (?, ?, 'PROBE', 0)";
        }

        private static int Ganzzahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null ? 0 : Convert.ToInt32(o);
        }

        private static int TraegerAnzahl(string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_carrier WHERE name = ?", new DbParam("?", name));
            return o == null ? -1 : Convert.ToInt32(o);
        }

        private static void AufraeumenVorlauf()
        {
            // Reste eines abgebrochenen Vorlaufs entfernen - energy_carrier.name ist eindeutig.
            foreach (string name in new[] { PROBE_INSERT, PROBE_ROLLBACK, PROBE_COMMIT, PROBE_DISPOSE })
                DataRepository.ExecuteSQL("DELETE FROM energy_carrier WHERE name = ?", new DbParam("?", name));
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE Gebaeudename = ?", new DbParam("?", PROBE_FK));
            StilleMeldungenEinsammeln();
        }

        private static void AufraeumenNachlauf()
        {
            AufraeumenVorlauf();
        }

        private static void StilleMeldungenEinsammeln()
        {
            _stilleMeldungen.AddRange(DataRepository.StilleFehlerAbholen());
        }

        private static string ArbeitskopieAnlegen(string quelle, string ordner)
        {
            string quellWal = quelle + "-wal";
            if (File.Exists(quellWal) && new FileInfo(quellWal).Length > 0)
            {
                Console.WriteLine("HINWEIS: Neben der Quelle liegt ein nicht leeres -wal. Die Kopie koennte");
                Console.WriteLine("         unvollstaendig sein; Quelle vorher sauber schliessen lassen.");
            }

            return KopieAnlegen(quelle, ordner, "Kenndaten_Probe.sqlite");
        }

        /// <summary>
        /// Frische Wegwerf-Kopie unter dem gewuenschten Namen - samt der beiden
        /// WAL-Beidateien, die eine alte Kopie sonst wiederbeleben wuerden.
        /// Herausgeloest in S6: Die Faelle 13 und 14 brauchen je eine EIGENE Kopie,
        /// weil sie den Schemamarker verstellen.
        /// </summary>
        private static string KopieAnlegen(string quelle, string ordner, string zielName)
        {
            Directory.CreateDirectory(ordner);
            string ziel = Path.Combine(ordner, zielName);

            foreach (string anhang in new[] { "", "-wal", "-shm" })
                DateiEntfernen(ziel + anhang);

            File.Copy(quelle, ziel, true);
            new FileInfo(ziel).IsReadOnly = false;
            return ziel;
        }

        private static string Argument(string[] args, string name)
        {
            if (args == null) return null;
            foreach (string a in args)
            {
                if (a != null && a.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                    return a.Substring(name.Length + 1).Trim('"');
            }
            return null;
        }
    }
}

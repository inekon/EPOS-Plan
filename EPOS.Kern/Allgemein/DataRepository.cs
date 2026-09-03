using System.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1.Allgemein;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET S4a (Implementierungskonzept DB-Migration SQLite, Abschnitt 2):
    // Die Zugriffsschicht spricht ab hier Microsoft.Data.Sqlite. Uebersetzt wird INNEN -
    // die oeffentlichen Signaturen bleiben, und der reine DATENTRAEGER an den ~2.300
    // Aufrufstellen im Bestand bleibt es auch. OleDb band rein nach POSITION; die
    // Parameternamen im Bestand sind beliebig ("?", "@p", "@id"). Der Uebersetzer wertet
    // die Namen deshalb NICHT aus, sondern nummeriert strikt nach Reihenfolge
    // (? -> @p0 ... @pN, Parameterarray -> @p0 ... @pN).
    //
    // NICHT geaendert wurden: Engine-Modus, FehlerMelden, StilleFehlerAbholen, KurzSql,
    // saemtliche Fehlermeldungs-Wortlaute und die Rueckgabewerte im Fehlerfall.
    //
    // ARBEITSPAKET iU6 (Umsetzungskonzept iOS 1.4, iF10): Der Datentraeger ist nicht mehr
    // OleDbParameter, sondern DbParam - "new OleDbParameter(...)" wirft auf Linux/macOS
    // schon im Konstruktor (Entscheidungsregister 2.2, Messung B). Der Uebersetzer
    // arbeitet unveraendert: dieselbe Positionsnummerierung, dieselbe Normalisierung.
    // Mit iU6-T3b ist die Uebergangsbruecke aus DbParam ausgezogen: Sie steht als
    // DbParamOleDb in der Anwendung, und EPOS.Kern nennt System.Data.OleDb nirgends
    // mehr - weder im Quelltext noch als PackageReference.
    // =====================================================================================

    public static class DataRepository
    {
        // Dateiname der SQLite-Datenbank (liegt im konfigurierten Datenbank-Ordner).
        // Bis zur Umstellung war das "Kenndaten.accdb"; siehe N7-Vorgriff in GetDBPath().
        private const string DB_DATEINAME = "Kenndaten.sqlite";

        // =================================================================================
        // ARBEITSPAKET iU6-T4: DIE FASSADE UND IHRE UMSETZUNG
        // =================================================================================
        //
        // Ab hier ist DataRepository eine FASSADE. Die eigentliche Arbeit macht
        // SqliteDatenzugriff hinter IDatenzugriff (Umsetzungskonzept iOS § 1.4, iF10);
        // die zwoelf Vertragsmethoden dieser Klasse leiten nur noch weiter. Die rund 160
        // Aufruferdateien bleiben dabei UNVERAENDERT - Signaturen, Fehlerwortlaute und
        // Rueckgabewerte im Fehlerfall sind dieselben.
        //
        // Warum ueberhaupt eine Schnittstelle, wenn es nur EINE Umsetzung gibt: Der Kern
        // muss auf iOS ohne Windows-Anbieter laufen, und der Referenzlauf und die Proben
        // muessen die Zugriffsschicht auf eine Kopie der Datenbank richten koennen. Beides
        // braucht eine Naht - genau die ist IDatenzugriff. Einen zweiten Dialekt gibt es
        // ausdruecklich NICHT (Umsetzungskonzept § 1.5, Praezisierung zu iL2).
        //
        // Das Feld ist absichtlich ein FELD und nicht readonly: In iU5 wird es an den
        // Dienste-Halter gehaengt (Dienste.Daten); bis dahin ist es die
        // Standardinstanz. Tests koennen es fuer die Dauer eines Falls tauschen.
        internal static IDatenzugriff Zugriff = new SqliteDatenzugriff();

        // =================================================================================
        // Engine-Modus: Datenbankfehler ohne MessageBox (Paket 8, Konzept 13.4)
        // =================================================================================
        //
        // AUSGANGSLAGE. Jede Zugriffsmethode dieser Klasse meldete einen Datenbankfehler
        // selbst per MessageBox. Weil die Simulationsklassen durchgängig hierüber
        // zugreifen, blockierte ein einziger DB-Fehler jeden unbeaufsichtigten Lauf bis
        // zum Timeout - Konzept 13.4 nennt das ausdrücklich als Teil des Pakets
        // ("DataRepository-Fehlerpfad ohne MessageBox im Engine-Kontext"). Die
        // Umgehung war bisher StilleDb, die aber nur für NEUEN Code galt; der Altbestand
        // im Rechenpfad blieb bei DataRepository.
        //
        // LÖSUNG. Ein zählender Schalter, den ausschließlich die Einstiegspunkte eines
        // Simulationslaufs setzen. Das sind (berichtigt in der Nacharbeit, Befund N14c):
        //   - SimulationRunner.Simuliere              - der ganze headless-Lauf
        //   - SimulationRunner.SimuliereUndSpeichere  - zusätzlich Ergebnisaufbau + Save
        //   - SimulationControl.Do_Simulation         - innere Absicherung, falls die
        //                                               Engine ohne Runner gerufen wird
        // Form_Simulation_Detail setzt den Modus NICHT: Der Lauf aus der Detailansicht
        // läuft auf dem UI-Thread, dort ist ein Dialog die richtige Meldung, und die
        // innere Absicherung in Do_Simulation deckt den Rechenteil trotzdem ab.
        // Solange der Schalter steht, wandert der Fehlertext in eine Sammelliste und auf
        // die Konsole statt in einen Dialog; nach dem Lauf holt der Aufrufer ihn ab und
        // legt ihn in den Protokollkanal.
        //
        // FÜR ALLE ÜBRIGEN AUFRUFER ÄNDERT SICH NICHTS: Ohne gesetzten Schalter - also
        // in jeder Bedienung der Oberfläche - kommt die MessageBox wie bisher, mit
        // demselben Wortlaut.
        //
        // Der Schalter ZÄHLT (statt bool), weil sich die Bereiche schachteln:
        // SimulationRunner.Simuliere setzt ihn um den ganzen Lauf, SimulationControl noch
        // einmal um Do_Simulation.
        //
        // PROZESSWEIT, NICHT THREADGEBUNDEN (Nacharbeit, Befund N7). _stillTiefe und
        // _stilleFehler gelten für den ganzen Prozess. Das trägt nur, solange HÖCHSTENS
        // EIN Simulationslauf gleichzeitig läuft - und die Anwendung ist dafür NICHT
        // einläufig: Der Berichtspfad rechnet in Task.Run auf einem ThreadPool-Thread
        // (BerichtsDatenSammler.Sammle, gerufen aus Form_Bericht,
        // Form_Wirtschaftlichkeit, Form_WirtschaftlichkeitVerlauf). Getragen wird die
        // Annahme von der MODALITÄT dieser drei Formulare: Alle drei werden ausschließlich
        // über ShowDialog() geöffnet, der MDI-Thread kann währenddessen keinen zweiten
        // Lauf starten. Wer eines davon je nicht-modal öffnet, bricht diese Invariante -
        // dann gehören Schalter und Sammelliste threadgebunden (dieselbe Vormerkung wie
        // in SimulationProtokoll).

        private static readonly object _stillSperre = new object();
        private static int _stillTiefe;
        private static readonly List<string> _stilleFehler = new List<string>();

        /// <summary>Höchstzahl gesammelter Meldungen je Lauf - gegen Meldungsfluten in Schleifen.</summary>
        private const int MAX_STILLE_FEHLER = 50;

        /// <summary>true, solange ein Simulationslauf den dialogfreien Modus hält.</summary>
        public static bool EngineModusAktiv
        {
            get { lock (_stillSperre) { return _stillTiefe > 0; } }
        }

        /// <summary>
        /// Öffnet den dialogfreien Modus für die Dauer eines <c>using</c>-Blocks.
        /// Verschachtelung ist zulässig; erst der äußerste Block gibt ihn wieder frei.
        /// </summary>
        public static IDisposable EngineModus()
        {
            return new EngineModusBereich();
        }

        /// <summary>
        /// Liefert die im dialogfreien Modus aufgelaufenen Meldungen und leert die
        /// Sammlung. Aufzurufen unmittelbar nach dem Lauf.
        /// </summary>
        public static string[] StilleFehlerAbholen()
        {
            lock (_stillSperre)
            {
                string[] kopie = _stilleFehler.ToArray();
                _stilleFehler.Clear();
                return kopie;
            }
        }

        private sealed class EngineModusBereich : IDisposable
        {
            private bool _offen = true;

            public EngineModusBereich()
            {
                lock (_stillSperre)
                {
                    // Der ÄUSSERSTE Bereich beginnt mit leerer Sammlung; ein
                    // verschachtelter erbt die des äußeren.
                    if (_stillTiefe == 0) _stilleFehler.Clear();
                    _stillTiefe++;
                }
            }

            public void Dispose()
            {
                if (!_offen) return;
                _offen = false;
                lock (_stillSperre) { if (_stillTiefe > 0) _stillTiefe--; }
            }
        }

        /// <summary>
        /// Meldet einen Datenbankfehler - als Dialog (Bedienung) oder als Protokolleintrag
        /// (Engine-Modus). EINE Entscheidungsstelle für alle sechs Fehlerpfade dieser
        /// Klasse UND für die datenbanknahen Meldungen der Controller, die aus dem
        /// Rechenpfad heraus erreichbar sind (<c>ErgebnisCtrl.Save</c> beim Speichern des
        /// Laufergebnisses, <c>PufferSpCtrl.CopyFromStamm</c>).
        ///
        /// Öffentlich, damit es bei EINER Entscheidung bleibt: Jede zweite Fassung des
        /// „Dialog oder Protokoll"-Musters wäre die Stelle, an der der nächste
        /// headless-Lauf wieder hängen bleibt.
        /// </summary>
        public static void FehlerMelden(string meldung)
        {
            bool still;
            lock (_stillSperre)
            {
                still = _stillTiefe > 0;
                if (still)
                {
                    // NACHARBEIT PAKET 8, BEFUND N12a: Beim Überlauf EINMAL sagen, dass
                    // gekappt wurde. Sonst liest sich eine abgeschnittene Liste wie eine
                    // vollständige - und gerade bei einer Meldungsflut ist die Zahl der
                    // Fehler die eigentliche Information.
                    if (_stilleFehler.Count < MAX_STILLE_FEHLER) _stilleFehler.Add(meldung);
                    else if (_stilleFehler.Count == MAX_STILLE_FEHLER)
                        _stilleFehler.Add("… weitere Meldungen unterdrückt (Grenze " +
                                          MAX_STILLE_FEHLER + " je Abholung).");
                }
            }

            if (!still)
            {
                Meldung.Zeigen(meldung);
                return;
            }

            try { Console.WriteLine("Datenbankfehler im Simulationslauf (ohne Dialog): " + meldung); }
            catch { }
        }


        // =================================================================================
        // Verbindung (Konzept 2.1)
        // =================================================================================

        // Zentraler Ort für den Pfad - einfach anzupassen
        public static string GetConnectionString()
        {
            // UEBERGANGSZUSTAND BIS S4b: 36 Dateien im Bestand bauen aus diesem String noch
            // eine EIGENE OleDbConnection. Das KOMPILIERT weiterhin (der Konstruktor nimmt
            // einen beliebigen string), traegt zur Laufzeit aber nicht mehr - genau diese
            // Eigenverbindungen fuehrt S4b auf die Zugriffsschicht zurueck. Bis dahin
            // liefert die Methode bewusst schon den SQLite-Verbindungsstring, damit es
            // ab S4b nur noch EINE Wahrheit gibt.
            return $"Data Source={GetDBPath()};Foreign Keys=True";
        }

        // =================================================================================
        // Verbindung (Konzept 2.1) - Weiterleitungen
        // =================================================================================

        /// <summary>
        /// Oeffnet eine Verbindung und setzt die verbindungsgebundenen PRAGMAs.
        ///
        /// Weiterleitung an <see cref="SqliteDatenzugriff.OeffneVerbindung"/>, wo der
        /// Rumpf seit iU6-T4 steht. Bleibt hier stehen, weil <c>StilleDb</c>,
        /// <c>RecordSet</c>, <c>DbVorgang</c>, <c>WaermequelleClass</c>,
        /// <c>PufferSpCtrl</c> und <c>GeraeteWaisen</c> sie unter diesem Namen rufen -
        /// es gibt weiterhin GENAU EINEN Verbindungsaufbau.
        /// </summary>
        internal static SqliteConnection OeffneVerbindung()
        {
            return SqliteDatenzugriff.OeffneVerbindung();
        }

        /// <summary>
        /// Prüft, ob die Datenbankdatei vorhanden und lesbar ist (Startprüfung,
        /// Konzept 2.8).
        /// </summary>
        public static bool DatenbankVorhanden()
        {
            return Zugriff.DatenbankVorhanden();
        }


        // =================================================================================
        // Uebersetzung und Typ-Rueckweg - Weiterleitungen
        // =================================================================================

        /// <summary>
        /// Ersetzt die Platzhalter <c>?</c> strikt in Reihenfolge durch <c>@p0 … @pN</c>.
        /// Weiterleitung; der Rumpf steht in <see cref="SqliteDatenzugriff"/>. Bleibt
        /// oeffentlich, weil die Zugriffsschichtproben sie unmittelbar pruefen.
        /// </summary>
        public static string UebersetzeParameterzeichen(string sql)
        {
            return SqliteDatenzugriff.UebersetzeParameterzeichen(sql);
        }

        /// <summary>
        /// Hebt einen Parameterwert auf die Speicherform der SQLite-Datei.
        /// Weiterleitung; der Rumpf steht in <see cref="SqliteDatenzugriff"/>. Bleibt
        /// oeffentlich, weil die Zugriffsschichtproben sie unmittelbar pruefen.
        /// </summary>
        public static object NormalisiereWert(object w)
        {
            return SqliteDatenzugriff.NormalisiereWert(w);
        }

        /// <summary>
        /// Baut ein fertiges Kommando (SQL uebersetzt, Parameter uebersetzt und gebunden).
        /// Weiterleitung; genutzt von <see cref="DbVorgang"/>, <c>RecordSet</c>,
        /// <c>StilleDb</c> und den Eigenverbindungen im Rechenpfad.
        /// </summary>
        internal static SqliteCommand ErzeugeKommando(SqliteConnection verbindung, SqliteTransaction transaktion,
                                                      string sql, DbParam[] parameter)
        {
            return SqliteDatenzugriff.ErzeugeKommando(verbindung, transaktion, sql, parameter);
        }

        /// <summary>
        /// Laedt einen offenen Reader in eine DataTable und stellt dabei die heutigen
        /// CLR-Typen wieder her (Typ-Rueckweg D9). Weiterleitung.
        /// </summary>
        internal static DataTable LadeTabelle(SqliteDataReader leser)
        {
            return SqliteDatenzugriff.LadeTabelle(leser);
        }


        // =================================================================================
        // Die sechs Zugriffsmethoden (Konzept 2.4 / 2.5) - Weiterleitungen
        // =================================================================================

        // Für SELECT-Abfragen: Liefert Daten in den Arbeitsspeicher
        public static DataTable GetDataTable(string sql, params DbParam[] parameters)
        {
            return Zugriff.GetDataTable(sql, parameters);
        }

        // Für INSERT, UPDATE, DELETE
        public static bool ExecuteSQL(string sql, params DbParam[] parameters)
        {
            return Zugriff.ExecuteSQL(sql, parameters);
        }

        // Für INSERT, UPDATE, DELETE – gibt die Anzahl der betroffenen Zeilen zurück
        public static int ExecuteNonQuery(string sql, params DbParam[] parameters)
        {
            return Zugriff.ExecuteNonQuery(sql, parameters);
        }

        // Signatur bewusst OHNE params - 7 Aufrufstellen verlassen sich darauf.
        public static int ExecuteInsertAndGetId(string insertSql, DbParam[] parameters)
        {
            return Zugriff.ExecuteInsertAndGetId(insertSql, parameters);
        }

        public static object ExecuteScalar(string sql, params DbParam[] parameters)
        {
            return Zugriff.ExecuteScalar(sql, parameters);
        }
        /// <summary>
        /// Diagnosezusatz der Fehlermeldungen (26.08.2026): Die Box „Für mindestens
        /// einen erforderlichen Parameter …“ nennt ohne die Abfrage weder Ort noch
        /// Ursache — der Anfang des SQL macht jede Meldung selbstverortend.
        /// </summary>
        internal static string KurzSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";
            string s = sql.Replace("\r", " ").Replace("\n", " ").Trim();
            while (s.IndexOf("  ", StringComparison.Ordinal) >= 0) s = s.Replace("  ", " ");
            if (s.Length > 160) s = s.Substring(0, 160) + "…";
            return Environment.NewLine + Environment.NewLine + "Abfrage: " + s;
        }


        // =================================================================================
        // Transaktionen (Konzept 2.6) - Weiterleitung
        // =================================================================================

        /// <summary>
        /// Oeffnet einen Datenbankvorgang (Verbindung + Transaktion) fuer einen
        /// <c>using</c>-Block. Ohne <c>Commit()</c> wird beim Verlassen zurueckgerollt.
        ///
        /// EINZIGER WEG IN EINE TRANSAKTION. Bis Arbeitspaket S4e gab es daneben ein
        /// <c>BeginTransaction()</c>, das ein <c>(OleDbConnection, OleDbTransaction)</c>-Tupel
        /// an 18 Dateien herausgab; seit S4e ist es ersatzlos geloescht - Verbindung und
        /// Transaktion verlassen die Zugriffsschicht nicht mehr.
        /// </summary>
        public static DbVorgang Vorgang()
        {
            return Zugriff.Vorgang();
        }

        // =================================================================================
        // Pfad und Startprüfung
        // =================================================================================

        /// <summary>
        /// Uebersteuert den Datenbankpfad vollstaendig, wenn gesetzt (null = normaler
        /// Weg ueber die Einstellungen).
        ///
        /// HAKEN FUER PROBEN UND REFERENZLAUF-SUITE: Damit laesst sich die Zugriffsschicht
        /// auf eine KOPIE der Datenbank richten, ohne die Einstellungen des Anwenders
        /// anzufassen. Im Programmbetrieb bleibt die Eigenschaft unbesetzt.
        /// </summary>
        public static string PfadUeberschreibung { get; set; }

        // Vollstaendiger Pfad zur SQLite-Datenbank.
        // Ordner: konfigurierter Standard-Datenbankpfad aus den Einstellungen (Form_AdminSettings),
        // sonst Fallback %ProgramData%\EPOS_PLAN. Darunter liegt die Datei DB_DATEINAME.
        public static string GetDBPath()
        {
            // Proben-/Referenzlauf-Haken: schlaegt alles andere.
            if (!string.IsNullOrWhiteSpace(PfadUeberschreibung)) return PfadUeberschreibung;

            string ordner = Properties.Settings.Default.DBPath;
            if (string.IsNullOrWhiteSpace(ordner))
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                ordner = Path.Combine(programData, "EPOS_PLAN");
            }

            string datei = Properties.Settings.Default.DBName;

            // VORGRIFF AUF N7 (endgueltiger Settings-Fixup ist Arbeitspaket S8):
            // In den gespeicherten Einstellungen steht auf jedem Bestandsrechner noch
            // "Kenndaten.accdb". Solange der Fixup nicht gelaufen ist, zeigt der
            // konfigurierte Name auf eine Access-Datei, die es nach der Migration nicht
            // mehr gibt - deshalb wird ein .accdb-Name hier auf die SQLite-Datei
            // umgebogen, statt den Start an einem Altwert scheitern zu lassen.
            if (string.IsNullOrWhiteSpace(datei) ||
                datei.EndsWith(".accdb", StringComparison.OrdinalIgnoreCase))
            {
                datei = DB_DATEINAME;
            }

            return Path.Combine(ordner, datei);
        }


        // =================================================================================
        // Schema-Auskunft - Ersatz fuer GetOleDbSchemaTable (Konzept 2.7, Befund N2)
        // =================================================================================
        //
        // Weiterleitungen; die Rumpfe stehen seit iU6-T4 in SqliteDatenzugriff.

        /// <summary>Gibt es eine Tabelle (oder Sicht) dieses Namens?</summary>
        public static bool TabelleVorhanden(string name)
        {
            return Zugriff.TabelleVorhanden(name);
        }

        /// <summary>Gibt es diese Spalte in dieser Tabelle? (Namensvergleich ohne Gross-/Kleinschreibung)</summary>
        public static bool SpalteVorhanden(string tabelle, string spalte)
        {
            return Zugriff.SpalteVorhanden(tabelle, spalte);
        }

        /// <summary>Spaltennamen einer Tabelle in Schemareihenfolge (leer, wenn es sie nicht gibt).</summary>
        public static List<string> SpaltenVonTabelle(string tabelle)
        {
            return Zugriff.SpaltenVonTabelle(tabelle);
        }

        /// <summary>
        /// Indizes einer Tabelle, eine Zeile je Index-Spalte
        /// (Indexname, Eindeutig, Position, Spaltenname) - deckt das frühere
        /// <c>Indexes</c>-Rowset ab.
        /// </summary>
        public static DataTable IndexListe(string tabelle)
        {
            return Zugriff.IndexListe(tabelle);
        }

        /// <summary>
        /// Fremdschlüssel einer Tabelle - deckt das frühere <c>Foreign_Keys</c>-Rowset ab.
        /// </summary>
        public static DataTable FremdschluesselListe(string tabelle)
        {
            return Zugriff.FremdschluesselListe(tabelle);
        }

        // =================================================================================
        // Bequemlichkeiten (unveraendert im Verhalten)
        // =================================================================================

        public static int GetMaxID(string tableName, string fieldName = "ID")
        {
            // Wir nutzen string.Format, da Tabellen- und Spaltennamen nicht als ? Parameter übergeben werden können
            string sql = string.Format("SELECT MAX({0}) FROM {1}", fieldName, tableName);

            DataTable dt = GetDataTable(sql);

            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                return Convert.ToInt32(dt.Rows[0][0]);
            }

            return 0;
        }

        public static bool DeleteWithDependencies(string masterTable, string detailTable, string detailForeignKey, int masterId)
        {
            using (DbVorgang vorgang = Vorgang())
            {
                try
                {
                    // 1. Details löschen (z.B. project_settings)
                    vorgang.Ausfuehren($"DELETE FROM {detailTable} WHERE {detailForeignKey} = ?",
                                       new DbParam("?", masterId));

                    // 2. Master löschen (z.B. energy_carrier)
                    vorgang.Ausfuehren($"DELETE FROM {masterTable} WHERE ID = ?",
                                       new DbParam("?", masterId));

                    vorgang.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Ein bereits von SQLite selbst zurueckgerollter Vorgang darf die
                    // eigentliche Meldung nicht verdraengen.
                    try { vorgang.Rollback(); } catch (Exception) { }
                    FehlerMelden($"Fehler beim Löschen in {masterTable}: " + ex.Message);
                    return false;
                }
            }
        }

        public static int GetIdByName(string tableName, string nameField, string nameValue)
        {
            string sql = $"SELECT ID FROM {tableName} WHERE {nameField} = ?";
            object result = ExecuteScalar(sql, new DbParam("?", nameValue));
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public static object GetValueById(string tableName, string nameField, int id)
        {
            string sql = $"SELECT {nameField} FROM {tableName} WHERE id = ?";
            object result = ExecuteScalar(sql, new DbParam("?", id));
            return result;
        }
    }
}

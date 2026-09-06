using System;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Threading;
using EposSqliteMigrator.Kern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lagebild des Datenbankordners beim Programmstart (Implementierungskonzept
    /// DB-Migration SQLite, Abschnitt 8).
    /// </summary>
    public enum ErststartLage
    {
        /// <summary>Die SQLite-Datei liegt bereits da - der Normalfall nach dem Cutover.</summary>
        SqliteVorhanden,

        /// <summary>Nur der Access-Altbestand liegt da - die Erstmigration steht an.</summary>
        NurAccdbVorhanden,

        /// <summary>Weder das eine noch das andere - keine benutzbare Datenbank.</summary>
        BeidesFehlt,
    }

    /// <summary>
    /// Der ERSTSTART-ASSISTENT (Arbeitspaket S8): Entscheidungs- und Ablauflogik der
    /// einmaligen Umstellung eines Bestands von <c>Kenndaten.accdb</c> auf
    /// <c>Kenndaten.sqlite</c>.
    ///
    /// <para><b>Bewusst OHNE Oberfläche.</b> Die Klasse kennt weder <c>Form</c> noch
    /// <c>MessageBox</c>; Fortschritt geht über <see cref="IProgress{T}"/> hinaus, das
    /// Ergebnis über den Rückgabewert und <see cref="LetzteMeldung"/>. So lässt sich der
    /// gesamte Ablauf kopfüber aus den Proben fahren (Fall 16) - das Fortschrittsfenster
    /// <see cref="Form_Erststart"/> ist nur eine dünne Hülle darum.</para>
    ///
    /// <para>Der Ablauf ist die Umsetzung der Tabellenzeile „Erststart" aus Abschnitt 8
    /// des Implementierungskonzepts:</para>
    /// <list type="number">
    ///   <item><description><see cref="SchemaMigration.HebeAltbestand"/> hebt die
    ///     <c>.accdb</c> in-place auf den Freeze-Stand 61 - genau das, was die
    ///     Access-Fassung von EPOS-Plan bei jedem Start ohnehin tat.</description></item>
    ///   <item><description><see cref="Migrator"/> aus <c>EposSqliteMigrator.Kern</c>
    ///     überträgt die Daten nach <c>Kenndaten.sqlite</c> und legt den
    ///     Migrationsbericht daneben. Bei JEDEM Fehler löscht der Kern die Zieldatei;
    ///     die <c>.accdb</c> bleibt unangetastet und ist damit das Rollback.</description></item>
    ///   <item><description>Erst NACH nachgewiesenem Erfolg wird die <c>.accdb</c> in
    ///     <c>Kenndaten.vor-sqlite.accdb</c> umbenannt (Rev. 2: Rückfallebene und
    ///     Beleg, dass dieser Bestand umgestellt wurde).</description></item>
    ///   <item><description>Optional der Settings-Fixup N7: der gespeicherte
    ///     <c>DBName</c> wird auf <c>Kenndaten.sqlite</c> gestellt und gespeichert.
    ///     In Proben IMMER <c>false</c> - die Einstellungen des Anwenders werden dort
    ///     nicht angefasst.</description></item>
    /// </list>
    ///
    /// <para><b>Der Fixup ist eine Aufräumarbeit, kein Riegel.</b> Der Vorgriff in
    /// <see cref="DataRepository.GetDBPath"/> biegt einen gespeicherten
    /// <c>*.accdb</c>-Namen ohnehin auf die SQLite-Datei um. Er bleibt als Netz
    /// bestehen, damit ein Bestand auch dann startet, wenn der Fixup nicht durchkam
    /// (schreibgeschütztes Profil, abgebrochener Lauf).</para>
    /// </summary>
    public static class ErststartMigration
    {
        /// <summary>Dateiname der SQLite-Datenbank - gleichlautend mit
        /// <c>DataRepository.DB_DATEINAME</c> (dort <c>private</c>).</summary>
        public const string SQLITE_DATEI = "Kenndaten.sqlite";

        /// <summary>Dateiname des Access-Altbestands.</summary>
        public const string ACCDB_DATEI = "Kenndaten.accdb";

        /// <summary>Name, den der Altbestand nach erfolgreicher Migration trägt.</summary>
        public const string ACCDB_UMBENANNT = "Kenndaten.vor-sqlite.accdb";

        /// <summary>
        /// Klartext zum letzten <see cref="Fuehredurch"/>-Aufruf - bei Erfolg die
        /// Bilanz, bei Fehlschlag der Grund, bei Verweigerung „Nichts zu tun".
        /// Immer gefüllt.
        /// </summary>
        public static string LetzteMeldung { get; private set; }

        /// <summary>Tabellen im Datenbeweis des letzten Migrationslaufs (0 = kein Lauf).</summary>
        public static int LetzteTabellen { get; private set; }

        /// <summary>Davon mit gleicher Zeilenzahl UND gleicher Prüfsumme.</summary>
        public static int LetzteTabellenOk { get; private set; }

        /// <summary>Zeilen im Ziel über alle Tabellen des letzten Laufs.</summary>
        public static long LetzteZeilen { get; private set; }


        // =================================================================================
        // Pfade und Lagebild
        // =================================================================================

        /// <summary>Der Ordner, in dem die Anwendung ihre Datenbank erwartet.</summary>
        /// <remarks>
        /// Leitet sich aus <see cref="DataRepository.GetDBPath"/> ab und trägt damit
        /// sowohl die Einstellung <c>DBPath</c> als auch den Proben-Haken
        /// <c>PfadUeberschreibung</c> mit.
        /// </remarks>
        public static string StandardOrdner()
        {
            try
            {
                string ordner = Path.GetDirectoryName(DataRepository.GetDBPath());
                return string.IsNullOrEmpty(ordner) ? "" : ordner;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string SqlitePfad(string dbOrdner)
        {
            return Path.Combine(dbOrdner ?? "", SQLITE_DATEI);
        }

        public static string AccdbPfad(string dbOrdner)
        {
            return Path.Combine(dbOrdner ?? "", ACCDB_DATEI);
        }

        public static string RueckfallPfad(string dbOrdner)
        {
            return Path.Combine(dbOrdner ?? "", ACCDB_UMBENANNT);
        }

        /// <summary>
        /// Das Lagebild des Datenbankordners. Reine Dateiprüfung, öffnet nichts und
        /// verändert nichts.
        /// </summary>
        /// <remarks>
        /// Der bereits umbenannte <c>Kenndaten.vor-sqlite.accdb</c> zählt bewusst NICHT
        /// als Altbestand: Er ist die Rückfallebene eines umgestellten Bestands. Wer ihn
        /// erneut migrieren will, benennt ihn von Hand zurück - das ist eine bewusste
        /// Entscheidung und keine, die der Assistent stillschweigend trifft.
        /// </remarks>
        public static ErststartLage Pruefe(string dbOrdner)
        {
            if (string.IsNullOrWhiteSpace(dbOrdner)) return ErststartLage.BeidesFehlt;

            if (File.Exists(SqlitePfad(dbOrdner))) return ErststartLage.SqliteVorhanden;
            if (File.Exists(AccdbPfad(dbOrdner))) return ErststartLage.NurAccdbVorhanden;

            return ErststartLage.BeidesFehlt;
        }


        // =================================================================================
        // Der Ablauf
        // =================================================================================

        /// <summary>
        /// Führt die einmalige Umstellung durch: Alt-Hebung, Migration, Umbenennung und
        /// wahlweise den Settings-Fixup.
        /// </summary>
        /// <param name="dbOrdner">Ordner mit <c>Kenndaten.accdb</c>.</param>
        /// <param name="fortschritt">Empfänger der Fortschrittszeilen; darf <c>null</c> sein.</param>
        /// <param name="settingsFixup">
        /// <c>true</c> = gespeicherten <c>DBName</c> auf <c>Kenndaten.sqlite</c> stellen und
        /// speichern. In Proben IMMER <c>false</c>.
        /// </param>
        /// <param name="berichtPfad">
        /// Pfad des Migrationsberichts, sobald der Kern gelaufen ist - auch im Fehlerfall
        /// (dort steht die Begründung drin). <c>null</c>, solange es keinen gibt.
        /// </param>
        /// <returns><c>true</c> = die SQLite-Datei steht und ist bewiesen.</returns>
        public static bool Fuehredurch(string dbOrdner, IProgress<string> fortschritt,
                                       bool settingsFixup, out string berichtPfad)
        {
            berichtPfad = null;
            LetzteMeldung = "";
            LetzteTabellen = 0;
            LetzteTabellenOk = 0;
            LetzteZeilen = 0;

            // AUSNAHME DER SCHREIBNAHT (Welle iF30, Anwenderentscheid 04.09.2026).
            // Die Erststart-Migration hebt einen .accdb-Bestand nach SQLite. Sie laeuft
            // genau einmal je Bestand, vor jeder Fachmaske, und muss auch im Lesemodus
            // laufen duerfen: Ohne sie gaebe es keine Datenbank, die man ansehen koennte.
            // Der Access-Zweig selbst geht ohnehin ueber eigene OleDb-Verbindungen an der
            // Naht vorbei - die Freigabe deckt die SQLite-Seite (Schemamarker,
            // Nachmigration) und macht die Ausnahme an dieser Stelle LESBAR.
            using IDisposable freigabe = Schreibnaht.Freigabe(Schreibnaht.GRUND_MIGRATION);


            // --- Vorprüfung: nur die Lage "nur .accdb" ist ein Auftrag ------------------
            ErststartLage lage = Pruefe(dbOrdner);
            if (lage == ErststartLage.SqliteVorhanden)
            {
                LetzteMeldung = "Nichts zu tun: " + SqlitePfad(dbOrdner) +
                                " ist bereits vorhanden. Die Umstellung läuft genau einmal je Bestand.";
                Melde(fortschritt, LetzteMeldung);
                return false;
            }
            if (lage == ErststartLage.BeidesFehlt)
            {
                LetzteMeldung = "Abbruch: Im Ordner " + (string.IsNullOrWhiteSpace(dbOrdner) ? "(leer)" : dbOrdner) +
                                " liegt weder " + SQLITE_DATEI + " noch " + ACCDB_DATEI + ".";
                Melde(fortschritt, LetzteMeldung);
                return false;
            }

            string accdb = AccdbPfad(dbOrdner);
            string sqlite = SqlitePfad(dbOrdner);
            string rueckfall = RueckfallPfad(dbOrdner);

            if (File.Exists(rueckfall))
            {
                LetzteMeldung = "Abbruch: " + rueckfall + " ist bereits vorhanden. " +
                                "Diese Datei ist die Rückfallebene einer früheren Umstellung und wird " +
                                "niemals überschrieben. Bitte den Bestand von Hand klären.";
                Melde(fortschritt, LetzteMeldung);
                return false;
            }

            // --- (a) Alt-Hebung auf den Freeze-Stand 61 ---------------------------------
            Melde(fortschritt, "Schritt 1 von 3: Altbestand auf den letzten Access-Stand heben …");
            Melde(fortschritt, "  " + accdb);

            string hebungsBericht;
            bool gehoben;
            try
            {
                gehoben = SchemaMigration.HebeAltbestand(accdb, out hebungsBericht);
            }
            catch (Exception ex)
            {
                LetzteMeldung = "Die Alt-Hebung des Access-Bestands brach mit einem unerwarteten Fehler ab: " +
                                ex.Message + Environment.NewLine +
                                "Die Datenbank wurde nicht umgestellt.";
                Melde(fortschritt, LetzteMeldung);
                return false;
            }

            if (!gehoben)
            {
                LetzteMeldung = "Die Alt-Hebung des Access-Bestands ist fehlgeschlagen - die Umstellung " +
                                "wurde nicht begonnen. Die Datenbank ist unverändert." + Environment.NewLine +
                                "Protokoll: " + Path.Combine(dbOrdner, SchemaMigration.PROTOKOLL_DATEI) +
                                Environment.NewLine + Environment.NewLine + Kopfzeilen(hebungsBericht);
                Melde(fortschritt, "FEHLGESCHLAGEN: Alt-Hebung. " + Kopfzeilen(hebungsBericht));
                return false;
            }

            Melde(fortschritt, "  Alt-Hebung abgeschlossen: Schemastand " +
                               SchemaMigration.StandVorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                               SchemaMigration.StandNachher.ToString(CultureInfo.InvariantCulture) + ".");

            // Der Access-Zweig hatte die Datei offen. Bevor der Migrator sie mit
            // Mode=Read anfasst, muss die ACE-Sitzung wirklich weg sein - sonst liegt
            // noch die Sperrdatei daneben und der Kern bricht (zu Recht) mit
            // ExitCode.SitzungOffen ab.
            AceSitzungFreigeben(accdb);

            // --- (b) Migration nach SQLite ---------------------------------------------
            Melde(fortschritt, "Schritt 2 von 3: Daten nach SQLite übertragen …");
            Melde(fortschritt, "  Ziel: " + sqlite);

            MigrationsErgebnis erg;
            try
            {
                var opt = new MigrationsOptionen
                {
                    Quelle = accdb,
                    Ziel = sqlite,
                    OrphanPolicy = OrphanPolicy.Abbruch,
                };

                // Der Kern nimmt seinen Fortschrittsempfaenger als Action<string> entgegen -
                // ein IProgress<string> wird hier schlicht daran gehaengt. Ein eigener
                // IProgress-Haken im Kern waere ein zweiter Weg fuer dieselbe Sache.
                var migrator = new Migrator(zeile => Melde(fortschritt, "  " + zeile));
                erg = migrator.Ausfuehren(opt);
            }
            catch (Exception ex)
            {
                LetzteMeldung = "Die Datenübernahme brach mit einem unerwarteten Fehler ab: " + ex.Message +
                                Environment.NewLine +
                                "Der Access-Bestand ist unverändert und bleibt die gültige Datenbank.";
                Melde(fortschritt, LetzteMeldung);
                return false;
            }

            berichtPfad = erg.BerichtPfad;
            LetzteTabellen = erg.Tabellen.Count;
            LetzteZeilen = erg.ZeilenGesamt;
            int ok = 0;
            foreach (TabellenErgebnis t in erg.Tabellen) if (t.Ok) ok++;
            LetzteTabellenOk = ok;

            if (erg.Code != ExitCode.Erfolg)
            {
                LetzteMeldung = "Die Datenübernahme nach SQLite ist fehlgeschlagen." + Environment.NewLine +
                                (string.IsNullOrEmpty(erg.Fehlermeldung) ? "(ohne Fehlertext)" : erg.Fehlermeldung) +
                                Environment.NewLine + Environment.NewLine +
                                "Die Zieldatei wurde gelöscht; " + accdb + " ist unverändert geblieben und " +
                                "bleibt die gültige Datenbank.";
                Melde(fortschritt, "FEHLGESCHLAGEN: " + erg.Fehlermeldung);
                return false;
            }

            Melde(fortschritt, "  Datenbeweis: " + LetzteTabellenOk + "/" + LetzteTabellen +
                               " Tabellen mit gleicher Zeilenzahl und gleicher Prüfsumme, " +
                               LetzteZeilen.ToString("N0", CultureInfo.GetCultureInfo("de-DE")) + " Zeilen.");

            // --- (c) Altbestand als Rückfallebene wegbenennen ---------------------------
            Melde(fortschritt, "Schritt 3 von 3: Altbestand als Rückfallebene sichern …");

            string umbenennFehler;
            bool umbenannt = UmbenennenMitGeduld(accdb, rueckfall, out umbenennFehler);

            string warnung = "";
            if (umbenannt)
            {
                Melde(fortschritt, "  " + ACCDB_DATEI + " heißt jetzt " + ACCDB_UMBENANNT + ".");
            }
            else
            {
                // Die Migration ist NACHGEWIESEN gelungen - daran ändert eine misslungene
                // Umbenennung nichts. Sie ist Beleg und Rueckfallebene, nicht Voraussetzung:
                // gemeldet, nicht zum Abbruch erhoben. Beim naechsten Start meldet Pruefe()
                // ohnehin SqliteVorhanden.
                warnung = "Hinweis: " + ACCDB_DATEI + " ließ sich nicht in " + ACCDB_UMBENANNT +
                          " umbenennen (" + umbenennFehler + "). Die Umstellung ist trotzdem gültig; " +
                          "die Altdatei kann von Hand umbenannt oder gesichert werden.";
                Melde(fortschritt, "  " + warnung);
            }

            // --- (d) Settings-Fixup N7 --------------------------------------------------
            if (settingsFixup)
            {
                try
                {
                    Properties.Settings.Default.DBName = SQLITE_DATEI;
                    Properties.Settings.Default.Save();
                    Melde(fortschritt, "  Einstellung „Datenbankdatei“ auf " + SQLITE_DATEI + " gestellt.");
                }
                catch (Exception ex)
                {
                    Melde(fortschritt, "  Hinweis: Die Einstellung „Datenbankdatei“ ließ sich nicht " +
                                       "speichern (" + ex.Message + "). Der Start greift trotzdem - " +
                                       "DataRepository.GetDBPath biegt einen .accdb-Namen auf " +
                                       SQLITE_DATEI + " um.");
                }
            }

            LetzteMeldung = "Die Datenbank wurde nach SQLite übernommen: " +
                            LetzteTabellenOk + " von " + LetzteTabellen + " Tabellen bewiesen, " +
                            LetzteZeilen.ToString("N0", CultureInfo.GetCultureInfo("de-DE")) + " Zeilen." +
                            Environment.NewLine + "Bericht: " + berichtPfad +
                            (warnung.Length == 0 ? "" : Environment.NewLine + warnung);
            Melde(fortschritt, "Fertig.");
            return true;
        }


        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        private static void Melde(IProgress<string> fortschritt, string zeile)
        {
            if (fortschritt == null) return;
            try { fortschritt.Report(zeile); }
            catch (Exception) { /* ein Fortschrittsempfaenger darf den Ablauf nie kippen */ }
        }

        /// <summary>
        /// Gibt die ACE-Sitzung auf der <c>.accdb</c> wirklich frei und wartet, bis die
        /// Sperrdatei <c>.laccdb</c> verschwunden ist (höchstens ~4 s).
        /// </summary>
        /// <remarks>
        /// <c>OleDbConnection.Close()</c> gibt die Verbindung nur in den Pool zurück - die
        /// Datei bleibt geöffnet und die Sperrdatei liegen. Der Migrator prüft genau diese
        /// Sperrdatei und bräche sonst mit „Die Quelldatenbank ist geoeffnet" ab.
        /// Bleibt sie liegen, weil ein ANDERER Prozess die Datei offen hat, ist der
        /// Abbruch des Kerns richtig - deshalb wird hier nur gewartet, nie gelöscht.
        /// </remarks>
        private static void AceSitzungFreigeben(string accdbPfad)
        {
            try { OleDbConnection.ReleaseObjectPool(); } catch (Exception) { }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            try
            {
                string sperre = Path.ChangeExtension(accdbPfad, ".laccdb");
                for (int i = 0; i < 40 && File.Exists(sperre); i++) Thread.Sleep(100);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Benennt um und lässt sich dabei Zeit: Virenscanner und Suchindex halten eine
        /// frisch geschlossene 150-MB-Datei gern noch einen Moment fest.
        /// </summary>
        private static bool UmbenennenMitGeduld(string von, string nach, out string fehler)
        {
            fehler = "";
            for (int versuch = 0; versuch < 10; versuch++)
            {
                try
                {
                    File.Move(von, nach);
                    return true;
                }
                catch (Exception ex)
                {
                    fehler = ex.Message;
                    try { OleDbConnection.ReleaseObjectPool(); } catch (Exception) { }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(200);
                }
            }
            return false;
        }

        /// <summary>Die ersten Zeilen eines Berichts - für sprechende Meldungen.</summary>
        private static string Kopfzeilen(string bericht)
        {
            if (string.IsNullOrEmpty(bericht)) return "(kein Bericht vorhanden)";

            string[] zeilen = bericht.Replace("\r\n", "\n").Split('\n');
            int n = Math.Min(8, zeilen.Length);
            var teile = new string[n];
            for (int i = 0; i < n; i++) teile[i] = zeilen[i];
            return string.Join(Environment.NewLine, teile).TrimEnd();
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    /// <summary>Das Ergebnis einer <see cref="Erstbereitstellung"/>.</summary>
    public enum Erstbereitstellungslage
    {
        /// <summary>Am Ziel lag bereits eine Datenbank — es wurde NICHTS angefasst.</summary>
        VorhandenBelassen,

        /// <summary>Die Vorlage wurde kopiert und ist geprüft.</summary>
        Kopiert,

        /// <summary>Es gibt keine Vorlage — am Ziel liegt weiterhin nichts.</summary>
        VorlageFehlt,

        /// <summary>
        /// Das Kopieren oder die Prüfung danach ist fehlgeschlagen. Eine halb
        /// entstandene Zieldatei wurde wieder entfernt.
        /// </summary>
        Fehler,
    }

    /// <summary>Was <see cref="Erstbereitstellung.Sicherstellen"/> getan hat.</summary>
    public sealed class Erstbereitstellungsergebnis
    {
        /// <summary>Der Ausgang.</summary>
        public Erstbereitstellungslage Lage { get; internal set; }

        /// <summary>Der Ablageort der Arbeitsdatenbank.</summary>
        public string Zielpfad { get; internal set; }

        /// <summary>Der Ort, an dem die Auslieferungsvorlage erwartet wurde.</summary>
        public string Vorlagepfad { get; internal set; }

        /// <summary>
        /// Klartext zum Ausgang — bei <see cref="Erstbereitstellungslage.Fehler"/> der
        /// Grund, sonst die Bilanz. Immer gefüllt.
        /// </summary>
        public string Meldung { get; internal set; }

        /// <summary>
        /// Der Schemastand der bereitgestellten Datenbank (<c>Tab_Applikation.SchemaVersion</c>),
        /// <c>-1</c>, wenn er nicht gelesen werden konnte oder nicht kopiert wurde.
        ///
        /// <para>Er darf ÄLTER sein als <see cref="SchemaStand.Zielversion"/> — die
        /// Schemapflege hebt ihn beim selben Start an. Er darf nur nicht fehlen: Eine
        /// Datei ohne <c>Tab_Applikation</c> ist keine EPOS-Plan-Datenbank.</para>
        /// </summary>
        public int Schemastand { get; internal set; } = -1;

        /// <summary>Wahr, wenn danach eine benutzbare Datenbank am Ziel liegt.</summary>
        public bool Bereit
        {
            get
            {
                return Lage == Erstbereitstellungslage.Kopiert ||
                       Lage == Erstbereitstellungslage.VorhandenBelassen;
            }
        }
    }

    // =====================================================================================
    // ANWENDERENTSCHEID #157-E-1, Weg W3 (09.09.2026): DIE VORLAGE IST DER ERSTSTART.
    //
    // Bis hierher startete eine Neuinstallation auf einem frischen Rechner GAR NICHT: Im
    // Datenbankordner lag weder Kenndaten.sqlite noch Kenndaten.accdb, DatenbankVorhanden()
    // war falsch, und der Start endete mit START_DB_FEHLT (Befund #157). Das Setup legte
    // zwar seit jeher eine Vorlage nach {app}\Vorlage - kein Pfad im Quelltext las diesen
    // Ordner. Genau diese Lücke schliesst diese Klasse.
    //
    // Sie ist der WINDOWS-ZWILLING von EPOS.iOS/Datenbankbereitstellung.Sicherstellen: dort
    // wird die Seed-Datenbank aus dem (schreibgeschuetzten) Anwendungspaket in die Sandbox
    // kopiert, hier aus dem (schreibgeschuetzten) Programmordner in den Datenordner. Der
    // Gedanke ist derselbe - liegt die Datei schon da, ist nichts zu tun -, und der Ordner
    // heisst auf beiden Seiten EPOS_PLAN.
    //
    // WAS SIE NICHT IST: ein Ersatz fuer die Uebernahme eines Access-Altbestands. Die ist
    // seit W3 ein HAUSWERKZEUG (EposSqliteMigrator) und kein Kundenweg mehr.
    // =====================================================================================

    /// <summary>
    /// Legt beim Zustand „keine Datenbank" die Arbeitsdatenbank aus der ausgelieferten
    /// Vorlage an — einmal je Rechner, nie überschreibend, nie halb.
    /// </summary>
    /// <remarks>
    /// <para><b>Drei Zusicherungen.</b>
    /// <list type="number">
    ///   <item><description><b>Nie überschreiben.</b> Liegt am Ziel eine Datei, wird sie
    ///     nicht angefasst — auch dann nicht, wenn sie kaputt ist. Der Anwender hat dort
    ///     seine Projekte; eine „Reparatur" durch Überschreiben wäre Datenverlust.</description></item>
    ///   <item><description><b>Nie halb liegen lassen.</b> Bricht das Kopieren ab oder
    ///     hält die Kopie der Prüfung nicht stand, wird die Zieldatei wieder entfernt.
    ///     Sonst stünde beim nächsten Start eine Ruine da, die als „vorhanden" gälte und
    ///     nie wieder ersetzt würde.</description></item>
    ///   <item><description><b>Erst prüfen, dann melden.</b> Nach dem Kopieren laufen
    ///     <c>PRAGMA integrity_check</c> und die Abfrage von
    ///     <c>Tab_Applikation.SchemaVersion</c>. Erst beides zusammen beweist, dass die
    ///     Vorlage eine vollständige EPOS-Plan-Datenbank ist und nicht bloß eine Datei
    ///     mit dem richtigen Namen.</description></item>
    /// </list></para>
    ///
    /// <para><b>Ohne die Zugriffsschicht.</b> Geprüft wird über eine eigene, NICHT
    /// gepoolte Verbindung im Modus <c>ReadOnly</c> — aus zwei Gründen: Zum einen ist die
    /// Zugriffsschicht zu diesem Zeitpunkt noch auf nichts gerichtet (der Start fragt vor
    /// jeder Fachmaske), zum anderen hielte eine gepoolte Verbindung die Datei fest und
    /// das Aufräumen im Fehlerfall (<c>File.Delete</c>) schlüge fehl. <c>Pooling=False</c>
    /// steht deshalb ausdrücklich im Verbindungstext.</para>
    ///
    /// <para><b>Der Schemastand darf älter sein.</b> Die Vorlage wird beim Bauen des
    /// Setups eingefroren; bis zur Auslieferung können Schemaschritte dazugekommen sein.
    /// Angehoben wird sie beim selben Start von <c>SchemaMigration.Ausfuehren</c>, das
    /// unmittelbar danach läuft. Diese Klasse prüft deshalb nur, dass ÜBERHAUPT ein
    /// Schemamarker da ist, und nicht, welche Zahl er trägt.</para>
    /// </remarks>
    public static class Erstbereitstellung
    {
        /// <summary>Die Statustabelle mit dem Schemamarker — eine Einzelzeile.</summary>
        private const string TABELLE_APPLIKATION = "Tab_Applikation";

        /// <summary>Die Beidateien des WAL-Modus, die vor einer Kopie weg müssen.</summary>
        private static readonly string[] Beidateien = { "-wal", "-shm" };

        /// <summary>
        /// Stellt sicher, dass unter <paramref name="zielpfad"/> eine benutzbare
        /// Datenbank liegt.
        /// </summary>
        /// <param name="zielpfad">
        /// Der Ablageort der Arbeitsdatenbank — im Programmbetrieb
        /// <c>DataRepository.GetDBPath()</c>.
        /// </param>
        /// <param name="vorlagepfad">
        /// Die ausgelieferte Vorlage — im Programmbetrieb
        /// <c>Dienste.Pfade.Auslieferungsvorlage</c>.
        /// </param>
        /// <param name="protokoll">Nimmt die Zeilen des Laufs auf; <c>null</c> = still.</param>
        public static Erstbereitstellungsergebnis Sicherstellen(
            string zielpfad, string vorlagepfad, Action<string> protokoll = null)
        {
            var ergebnis = new Erstbereitstellungsergebnis
            {
                Zielpfad = zielpfad ?? "",
                Vorlagepfad = vorlagepfad ?? "",
                Meldung = "",
            };

            if (string.IsNullOrWhiteSpace(zielpfad))
            {
                ergebnis.Lage = Erstbereitstellungslage.Fehler;
                ergebnis.Meldung = "Es wurde kein Ablageort für die Datenbank angegeben.";
                Melde(protokoll, ergebnis.Meldung);
                return ergebnis;
            }

            // --- Am Ziel liegt schon etwas: Finger weg. --------------------------------
            if (DateiDa(zielpfad))
            {
                ergebnis.Lage = Erstbereitstellungslage.VorhandenBelassen;
                ergebnis.Meldung = "Datenbank vorhanden: " + zielpfad + ". Die Auslieferungsvorlage " +
                                   "wird nicht angefasst.";
                Melde(protokoll, ergebnis.Meldung);
                return ergebnis;
            }

            // --- Ohne Vorlage gibt es nichts zu kopieren. ------------------------------
            if (string.IsNullOrWhiteSpace(vorlagepfad) || !DateiDa(vorlagepfad))
            {
                ergebnis.Lage = Erstbereitstellungslage.VorlageFehlt;
                ergebnis.Meldung = "Es liegt keine Datenbank unter " + zielpfad +
                                   ", und die Auslieferungsvorlage fehlt ebenfalls: " +
                                   (string.IsNullOrWhiteSpace(vorlagepfad) ? "(kein Pfad)" : vorlagepfad);
                Melde(protokoll, ergebnis.Meldung);
                return ergebnis;
            }

            Melde(protokoll, "Erststart - die Datenbank wird aus der Auslieferungsvorlage angelegt.");
            Melde(protokoll, "  Vorlage: " + vorlagepfad);
            Melde(protokoll, "  Ziel   : " + zielpfad);

            // --- Kopieren --------------------------------------------------------------
            if (!Kopiere(vorlagepfad, zielpfad, ergebnis, protokoll)) return ergebnis;

            // --- Prüfen ----------------------------------------------------------------
            string grund;
            int stand;
            if (!Pruefe(zielpfad, out stand, out grund))
            {
                Aufraeumen(zielpfad);
                ergebnis.Lage = Erstbereitstellungslage.Fehler;
                ergebnis.Meldung = "Die Auslieferungsvorlage hielt der Prüfung nicht stand: " + grund +
                                   " Die angelegte Datei wurde wieder entfernt; " + vorlagepfad +
                                   " ist unverändert geblieben.";
                Melde(protokoll, "FEHLGESCHLAGEN: " + ergebnis.Meldung);
                return ergebnis;
            }

            ergebnis.Lage = Erstbereitstellungslage.Kopiert;
            ergebnis.Schemastand = stand;
            ergebnis.Meldung = "Die Datenbank wurde aus der Auslieferungsvorlage angelegt: " + zielpfad +
                               " (" + Groesse(zielpfad) + " MB, Schemastand " +
                               stand.ToString(CultureInfo.InvariantCulture) + ").";
            Melde(protokoll, ergebnis.Meldung);
            return ergebnis;
        }


        // =================================================================================
        // Die zwei Schritte
        // =================================================================================

        /// <summary>
        /// Kopiert die Vorlage an das Ziel. <c>false</c> = fehlgeschlagen; das Ergebnis
        /// trägt dann Lage und Grund, und am Ziel liegt nichts mehr.
        /// </summary>
        private static bool Kopiere(string vorlagepfad, string zielpfad,
                                    Erstbereitstellungsergebnis ergebnis, Action<string> protokoll)
        {
            try
            {
                string ordner = Path.GetDirectoryName(zielpfad);
                if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);

                // Reste eines abgebrochenen Vorlaufs. Ein liegengebliebenes -wal wuerde
                // beim ersten Oeffnen in die frisch kopierte Datei eingespielt - sie waere
                // danach weder der Auslieferungsstand noch ein gueltiger Stand. Dieselbe
                // Vorsorge trifft EPOS.iOS/Datenbankbereitstellung.Kopiere.
                foreach (string anhang in Beidateien)
                {
                    try { if (File.Exists(zielpfad + anhang)) File.Delete(zielpfad + anhang); }
                    catch { }
                }

                // overwrite: false - die zweite Haelfte der Zusicherung "nie ueberschreiben".
                // Der Fall ist oben schon abgefangen; hier steht er gegen ein Wettrennen.
                File.Copy(vorlagepfad, zielpfad, false);

                // Die Vorlage liegt beim Anwender unter %ProgramFiles% und ist deshalb
                // schreibgeschuetzt - das Kopieren nimmt dieses Merkmal mit. Die
                // Arbeitsdatenbank muss beschreibbar sein.
                try { new FileInfo(zielpfad).IsReadOnly = false; } catch { }

                return true;
            }
            catch (Exception ex)
            {
                Aufraeumen(zielpfad);
                ergebnis.Lage = Erstbereitstellungslage.Fehler;
                ergebnis.Meldung = "Die Datenbank ließ sich nicht aus der Auslieferungsvorlage anlegen: " +
                                   ex.Message + " Ziel: " + zielpfad + ".";
                Melde(protokoll, "FEHLGESCHLAGEN: " + ergebnis.Meldung);
                return false;
            }
        }

        /// <summary>
        /// Die zwei Prüfungen auf der frisch kopierten Datei: <c>PRAGMA integrity_check</c>
        /// und der Schemamarker <c>Tab_Applikation.SchemaVersion</c>.
        /// </summary>
        /// <remarks>
        /// <c>Mode=ReadOnly</c>, damit nichts entsteht, und <c>Pooling=False</c>, damit die
        /// Datei nach dem <c>using</c> wirklich frei ist — sonst schlüge das Aufräumen im
        /// Fehlerfall fehl.
        /// </remarks>
        private static bool Pruefe(string zielpfad, out int schemastand, out string grund)
        {
            schemastand = -1;
            grund = "";

            try
            {
                using (var verbindung = new SqliteConnection(
                           "Data Source=" + zielpfad + ";Mode=ReadOnly;Pooling=False"))
                {
                    verbindung.Open();

                    using (SqliteCommand cmd = verbindung.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA integrity_check";
                        object wert = cmd.ExecuteScalar();
                        string antwort = wert == null ? "" : Convert.ToString(wert, CultureInfo.InvariantCulture);
                        if (!string.Equals(antwort, "ok", StringComparison.OrdinalIgnoreCase))
                        {
                            grund = "PRAGMA integrity_check meldet \"" +
                                    (string.IsNullOrEmpty(antwort) ? "(keine Antwort)" : antwort) + "\".";
                            return false;
                        }
                    }

                    using (SqliteCommand cmd = verbindung.CreateCommand())
                    {
                        cmd.CommandText = "SELECT " + ApplikationCtrl.SPALTE_SCHEMAVERSION +
                                          " FROM " + TABELLE_APPLIKATION + " LIMIT 1";
                        object wert = cmd.ExecuteScalar();
                        if (wert == null || wert == DBNull.Value)
                        {
                            grund = "In " + TABELLE_APPLIKATION + " steht kein Schemamarker (" +
                                    ApplikationCtrl.SPALTE_SCHEMAVERSION + ").";
                            return false;
                        }
                        schemastand = Convert.ToInt32(wert, CultureInfo.InvariantCulture);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                grund = ex.Message;
                return false;
            }
        }


        // =================================================================================
        // Kleinkram
        // =================================================================================

        /// <summary>Entfernt eine halb entstandene Zieldatei samt WAL-Beidateien.</summary>
        private static void Aufraeumen(string zielpfad)
        {
            try { if (File.Exists(zielpfad)) File.Delete(zielpfad); } catch { }
            foreach (string anhang in Beidateien)
            {
                try { if (File.Exists(zielpfad + anhang)) File.Delete(zielpfad + anhang); } catch { }
            }
        }

        private static bool DateiDa(string pfad)
        {
            try { return File.Exists(pfad); } catch { return false; }
        }

        private static string Groesse(string datei)
        {
            try { return (new FileInfo(datei).Length / 1024 / 1024).ToString(CultureInfo.InvariantCulture); }
            catch { return "?"; }
        }

        private static void Melde(Action<string> protokoll, string zeile)
        {
            if (protokoll == null) return;
            try { protokoll(zeile); } catch { /* ein Empfaenger darf den Ablauf nie kippen */ }
        }
    }
}

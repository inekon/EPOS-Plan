using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // AUFTRAG #158 (Befund des Wiki-Agenten #155, 09.09.2026): EINE SICHERUNGSWAHRHEIT.
    //
    // Bis hierher gab es ZWEI Schreibweisen derselben Aufgabe: KiSicherungspunkt.cs prüfte
    // auf eine ".laccdb"-Sperrdatei (Access-Konzept, unter SQLite bedeutungslos - der
    // Hinweis konnte nie mehr auftreten) und kopierte nur die Hauptdatei; MenueCtrl.cs kopierte
    // Hauptdatei UND "-wal"/"-shm" von Hand. Beides ist der wörtliche Nachbau von
    // Referenzlauf\DbUmgebung.ArbeitskopieAnlegen aus der Access-Zeit und trägt unter SQLite
    // im WAL-Modus nicht mehr: Der aktuelle Datenstand ist die SUMME aus Hauptdatei und
    // "-wal" (BETRIEB_SQLITE.md § 2), eine reine Dateikopie der Hauptdatei greift nur den
    // letzten Checkpoint ab, und bei geöffneter Datenbank ist sie im schlimmsten Fall sogar
    // mitten im Schreiben kopiert - inkonsistent.
    // =====================================================================================

    /// <summary>
    /// Die EINE Sicherungswahrheit des Kerns: eine in sich geschlossene, konsistente Kopie
    /// einer SQLite-Datenbank - unabhängig davon, ob sie gerade geöffnet ist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der gewählte Weg: <c>VACUUM INTO</c> über eine frisch geöffnete Verbindung.</b>
    /// <c>BETRIEB_SQLITE.md</c> § 3.2 nennt genau das als DIE Sicherung im laufenden
    /// Betrieb ("schreibt eine in sich geschlossene, defragmentierte Kopie, ohne die
    /// laufende Anwendung zu stören und ohne WAL-Beidateien"), und
    /// <c>EPOS.iOS/Datenbankbereitstellung.SicherungAnlegen</c> setzt exakt das für die
    /// iOS-Seite schon um. Diese Klasse zieht die Windows-Seite (Hilfe-Assistent UND
    /// Projektverwaltung) auf denselben, jetzt EINEN Weg.
    /// </para>
    /// <para>
    /// <b>Warum eine geöffnete Verbindung mehr sieht als eine Dateikopie.</b> WAL-Modus
    /// bedeutet: Jede Verbindung, die die Datei über SQLite ÖFFNET (statt ihre Bytes zu
    /// kopieren), liest automatisch durch die "-wal" hindurch und sieht den zuletzt
    /// committeten Stand - ob er schon eingecheckpointet ist oder nicht ist für einen
    /// SQLite-Leser unsichtbar, genau das ist der Zweck von WAL. <c>VACUUM INTO</c> schreibt
    /// aus dieser konsistenten Sicht heraus EINE neue, defragmentierte Datei - die Zieldatei
    /// entsteht frisch im Standard-Journalmodus (nicht im WAL-Modus der Quelle) und hat
    /// deshalb von sich aus KEINE eigene "-wal"/"-shm". Die Quelle bleibt dabei unverändert;
    /// es entsteht nur eine zweite Datei daneben.
    /// </para>
    /// <para>
    /// <b>Warum nicht <c>SqliteConnection.BackupDatabase</c>.</b> Die Online-Backup-API von
    /// <c>Microsoft.Data.Sqlite</c> wäre ebenfalls ohne ein neues Paket erreichbar, bräuchte
    /// aber eine ZWEITE, selbst geöffnete Zielverbindung und kopiert seitenweise über
    /// die native Backup-Schnittstelle - ein eigener Mechanismus neben der SQL-Ausführung,
    /// den <see cref="IDatenzugriff"/> mangels SQLite-Typen in seiner Signatur gar nicht
    /// abbilden könnte ("alles mit SQLite-Typen in der Signatur ... ist Innenleben der
    /// Umsetzung", <c>IDatenzugriff.cs</c>). <c>VACUUM INTO</c> ist dagegen ein gewöhnliches
    /// SQL-Kommando: Es läuft über <see cref="SqliteDatenzugriff.ErzeugeKommando"/> - denselben
    /// Weg, über den jede andere Anweisung des Kerns geht (Parameterbindung per
    /// <see cref="DbParam"/>, Schreibnaht-Prüfung) - und braucht dafür keine neue Öffnung der
    /// Zugriffsschicht nach außen. Das ist "der Weg, den SqliteDatenzugriff ohne neue
    /// Abhängigkeit hergibt".
    /// </para>
    /// <para>
    /// <b>Die Schreibnaht (Welle iF30).</b> <c>VACUUM INTO</c> zählt für
    /// <see cref="Schreibnaht.IstSchreibend"/> als schreibende Anweisung, obwohl sie die
    /// QUELLE nicht verändert - sie schreibt nur eine zweite Datei daneben. Genau dafür gibt
    /// es bereits <see cref="Schreibnaht.GRUND_SICHERUNG"/> ("Sicherung und Export"): Eine
    /// Sicherung bleibt auch im Lesemodus (abgelaufene Lizenz) ausdrücklich erlaubt
    /// (Lizenzierungskonzept § 6) - derselbe Grund, den
    /// <c>Datenbankbereitstellung.SicherungAnlegen</c> unter iOS bereits setzt.
    /// </para>
    /// <para>
    /// <b>Ergebnis.</b> Eine einzelne, in sich geschlossene Datei ohne Begleitdateien. Das
    /// Ziel darf noch nicht existieren - SQLite überschreibt bei <c>VACUUM INTO</c> nichts
    /// (BETRIEB_SQLITE.md § 3.2); der sekundengenaue Zeitstempel im Namen macht eine
    /// Kollision praktisch ausgeschlossen. Schlägt das Kommando dennoch fehl (Ziel nicht
    /// beschreibbar, Platte voll, Quelle mit einer anderen Verbindung exklusiv gesperrt -&gt;
    /// <c>SQLITE_BUSY</c>), wird eine angebrochene Zieldatei entfernt und die ursprüngliche
    /// Ausnahme UNVERÄNDERT weitergereicht - nie still.
    /// </para>
    /// </remarks>
    internal static class Datenbanksicherung
    {
        /// <summary>
        /// Haltezeit, bevor eine gesperrte Quelle <c>VACUUM INTO</c> mit
        /// <c>SQLITE_BUSY</c> aufgibt - derselbe Wert wie am Verbindungsaufbau des
        /// laufenden Betriebs (<see cref="SqliteDatenzugriff.OeffneVerbindung"/>).
        /// </summary>
        private const int BUSY_TIMEOUT_MS = 5000;

        /// <summary>
        /// Legt eine in sich geschlossene Sicherungskopie der SQLite-Datenbank
        /// <paramref name="quellpfad"/> im Ordner <paramref name="zielordner"/> an und
        /// liefert den vollständigen Zielpfad.
        /// </summary>
        /// <param name="quellpfad">Pfad der Quelldatenbank (<c>.sqlite</c>); muss vorhanden sein.</param>
        /// <param name="zielordner">Zielordner; wird angelegt, falls er fehlt.</param>
        /// <param name="praefix">
        /// Namensbestandteil vor dem Zeitstempel (z. B. <c>"Kenndaten_KI"</c> oder
        /// <c>"Kenndaten_Loeschen"</c>). Der Aufrufer trägt damit seine eigene
        /// Namensgebung bei - dieser Helfer kennt weder „KI" noch einen „Zweck", das bleibt
        /// Sache der zwei Aufrufer (<c>KiSicherungspunkt</c>, <c>MenueCtrl</c>). Leer oder
        /// <c>null</c> fällt auf den Dateinamen der Quelle ohne Endung zurück.
        /// </param>
        /// <returns>Der vollständige Pfad der neu angelegten Kopie.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="quellpfad"/> oder <paramref name="zielordner"/> ist leer.
        /// </exception>
        /// <exception cref="FileNotFoundException">Die Quelldatenbank existiert nicht.</exception>
        /// <exception cref="IOException">
        /// Der Zielordner lässt sich nicht anlegen, oder <c>VACUUM INTO</c> scheitert
        /// (z. B. volle Platte, Ziel existiert schon).
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">Der Zielordner ist nicht beschreibbar.</exception>
        /// <exception cref="SqliteException">
        /// Die Quelle ist mit einer anderen Verbindung exklusiv gesperrt (<c>SQLITE_BUSY</c>)
        /// oder das Ziel existiert bereits.
        /// </exception>
        internal static string KopieAnlegen(string quellpfad, string zielordner, string praefix)
        {
            if (string.IsNullOrWhiteSpace(quellpfad))
                throw new ArgumentException("Der Quellpfad ist leer.", nameof(quellpfad));
            if (!File.Exists(quellpfad))
                throw new FileNotFoundException("Die Quelldatenbank wurde nicht gefunden.", quellpfad);
            if (string.IsNullOrWhiteSpace(zielordner))
                throw new ArgumentException("Der Zielordner ist leer.", nameof(zielordner));

            // Wirft von sich aus IOException/UnauthorizedAccessException, wenn der Ordner
            // nicht anzulegen ist (z. B. weil ein gleichnamiger Pfadteil eine DATEI ist,
            // oder weil der übergeordnete Ordner keine Schreibrechte gibt) - eine klare
            // .NET-Meldung, an der nichts zu übersetzen ist.
            Directory.CreateDirectory(zielordner);

            string stamm = string.IsNullOrEmpty(praefix) ? Path.GetFileNameWithoutExtension(quellpfad) : praefix;
            string dateiname = stamm + "_" +
                                DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture) +
                                Path.GetExtension(quellpfad);
            string zielpfad = Path.Combine(zielordner, dateiname);

            // Eigene, kurzlebige Verbindung auf GENAU den übergebenen Quellpfad - bewusst
            // NICHT SqliteDatenzugriff.OeffneVerbindung(), die fest an
            // DataRepository.GetDBPath() hängt. Dieser Helfer ist ein reiner Pfad-zu-Pfad-
            // Vorgang und kennt "die aktuell konfigurierte Datenbank" nicht - das macht ihn
            // unabhängig testbar und für beide Aufrufer gleich nutzbar, auch wenn
            // Quelle und laufende Anwendungsdatenbank einmal auseinanderfallen sollten.
            using (SqliteConnection verbindung = new SqliteConnection($"Data Source={quellpfad}"))
            {
                verbindung.Open();
                using (SqliteCommand pragma = verbindung.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA busy_timeout = " +
                                          BUSY_TIMEOUT_MS.ToString(CultureInfo.InvariantCulture) + ";";
                    pragma.ExecuteNonQuery();
                }

                // GRUND_SICHERUNG (Welle iF30): VACUUM INTO fängt die Schreibnaht sonst als
                // schreibende Anweisung ab, obwohl es die QUELLE nicht anfasst - eine
                // Sicherung bleibt auch im Lesemodus ausdrücklich erlaubt.
                using (Schreibnaht.Freigabe(Schreibnaht.GRUND_SICHERUNG))
                using (SqliteCommand kommando = SqliteDatenzugriff.ErzeugeKommando(
                           verbindung, null, "VACUUM INTO ?", new[] { new DbParam("@ziel", zielpfad) }))
                {
                    try
                    {
                        kommando.ExecuteNonQuery();
                    }
                    catch
                    {
                        // VACUUM INTO schreibt nicht atomar (kein Rename-bei-Erfolg) - ein
                        // Fehlschlag mitten im Kopiervorgang (z. B. Platte voll) kann eine
                        // angebrochene Zieldatei hinterlassen. Sie darf nicht als Sicherung
                        // durchgehen; die ursprüngliche Ausnahme bleibt unverändert.
                        try { if (File.Exists(zielpfad)) File.Delete(zielpfad); }
                        catch { /* Aufräumen darf den echten Fehler nicht verdecken */ }
                        throw;
                    }
                }
            }

            return zielpfad;
        }
    }
}

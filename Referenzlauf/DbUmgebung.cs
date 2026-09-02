using System;
using System.IO;
using System.Reflection;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Umgang mit der Datenbank fuer die Referenzlauf-Suite.
    ///
    /// HARTE REGEL: Es wird NIEMALS in die produktive Kenndaten.accdb geschrieben.
    /// Die Suite legt eine Arbeitskopie an und biegt den DB-Pfad der App darauf um.
    /// Erst danach darf ueberhaupt ein Controller der App angefasst werden, denn
    /// jeder Zugriff laeuft ueber DataRepository.GetConnectionString().
    /// </summary>
    internal static class DbUmgebung
    {
        /// <summary>Dateiname des Access-Zweigs (DataRepository.DB_DATEINAME bis zur Umstellung).</summary>
        public const string DB_DATEINAME = "Kenndaten.accdb";

        /// <summary>Dateiname des SQLite-Zweigs (DataRepository.DB_DATEINAME seit Paket S4).</summary>
        public const string DB_DATEINAME_SQLITE = "Kenndaten.sqlite";

        /// <summary>Fallback-Quelle, falls unter %ProgramData% nichts liegt.</summary>
        private const string QUELLE_FALLBACK = @"C:\Waermeplan\Kenndaten.accdb";

        /// <summary>Endungsprobe - sie entscheidet ueber Access- oder SQLite-Zweig.</summary>
        public static bool IstSqlite(string pfad)
        {
            return pfad != null && pfad.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Voller Pfad der Arbeitskopie in diesem Ordner. Liegt dort eine
        /// <c>Kenndaten.sqlite</c>, gilt der SQLite-Zweig, sonst der Access-Zweig.
        ///
        /// Die Probe laeuft ueber das DATEISYSTEM und nicht ueber ein gemerktes Kennzeichen,
        /// weil der Kindprozess (Modus "projekt") nur den ORDNER uebergeben bekommt und
        /// jede statische Merkung dort wieder auf ihrem Anfangswert stuende.
        /// </summary>
        public static string ArbeitskopieDatei(string ordner)
        {
            string sqlite = Path.Combine(ordner, DB_DATEINAME_SQLITE);
            return File.Exists(sqlite) ? sqlite : Path.Combine(ordner, DB_DATEINAME);
        }

        /// <summary>
        /// Sucht die produktive Datenbank: erst %ProgramData%\EPOS_PLAN (SQLite vor Access,
        /// weil die Umstellung die .accdb als Altbestand liegen laesst), dann der Fallback.
        /// Liefert null, wenn nichts davon existiert.
        /// </summary>
        public static string ProduktivQuelleFinden(Protokoll log)
        {
            return ProduktivQuelleFinden(log, null);
        }

        /// <summary>
        /// Wie <see cref="ProduktivQuelleFinden(Protokoll)"/>, aber mit ausdruecklicher
        /// Vorgabe (Schalter <c>--quelle</c>).
        ///
        /// ZWECK: Der Verhaltensbeweis der Umstellung (Paket S7) laesst beide Backends auf
        /// EINEM eingefrorenen Datenstand rechnen - der Access-Datei und der daraus
        /// migrierten SQLite-Datei. Beide liegen ausserhalb der produktiven Ablage; ohne
        /// Vorgabe waere jeder Lauf auf den jeweils aktuellen Produktivstand angewiesen und
        /// damit auf Datendrift zwischen den beiden Laeufen.
        /// </summary>
        public static string ProduktivQuelleFinden(Protokoll log, string vorgabe)
        {
            if (!string.IsNullOrWhiteSpace(vorgabe))
            {
                string voll = Path.GetFullPath(vorgabe.Trim());
                if (!File.Exists(voll))
                {
                    log.FehlerZeile("Vorgegebene Quelle (--quelle) nicht vorhanden: " + voll);
                    return null;
                }
                log.Zeile("Quelle vorgegeben (--quelle): " + voll);
                return voll;
            }

            string ordner = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EPOS_PLAN");

            foreach (string name in new[] { DB_DATEINAME_SQLITE, DB_DATEINAME })
            {
                string kandidat = Path.Combine(ordner, name);
                if (File.Exists(kandidat))
                {
                    log.Zeile("Quelle gefunden (ProgramData): " + kandidat);
                    return kandidat;
                }
                log.Zeile("Quelle NICHT vorhanden: " + kandidat);
            }

            if (File.Exists(QUELLE_FALLBACK))
            {
                log.Zeile("Quelle gefunden (Fallback): " + QUELLE_FALLBACK);
                return QUELLE_FALLBACK;
            }
            log.Zeile("Quelle NICHT vorhanden: " + QUELLE_FALLBACK);
            return null;
        }

        /// <summary>
        /// Kopiert die Quelldatenbank in den Arbeitskopie-Ordner.
        /// Eine danebenliegende .laccdb bedeutet "DB gerade geoeffnet" - lesendes
        /// Kopieren ist trotzdem zulaessig, wird aber protokolliert, weil die Kopie
        /// dann einen Zwischenstand mit nicht eingespielten Aenderungen zeigen kann.
        /// </summary>
        public static string ArbeitskopieAnlegen(string quelle, string zielOrdner, Protokoll log)
        {
            bool sqlite = IstSqlite(quelle);

            if (sqlite)
            {
                // Gegenstueck zur .laccdb-Probe: -wal/-shm neben der Quelle heissen, dass die
                // Datenbank nicht sauber geschlossen wurde. Eine reine Dateikopie ohne diese
                // Begleitdateien zeigt dann nur den eingecheckpointeten Stand.
                foreach (string anhang in new[] { "-wal", "-shm" })
                    if (File.Exists(quelle + anhang))
                        log.Warnung("Neben der Quelldatenbank liegt " +
                                    Path.GetFileName(quelle) + anhang + ". Die Datenbank wurde " +
                                    "nicht sauber geschlossen; die Kopie enthaelt nur den " +
                                    "eingecheckpointeten Stand.");
            }
            else
            {
                string sperrdatei = Path.ChangeExtension(quelle, ".laccdb");
                if (File.Exists(sperrdatei))
                {
                    log.Warnung("Die Quelldatenbank ist geoeffnet (" + Path.GetFileName(sperrdatei) +
                                " vorhanden). Es wird trotzdem lesend kopiert; die Kopie kann " +
                                "noch nicht geschriebene Aenderungen der laufenden Sitzung nicht enthalten.");
                }
            }

            Directory.CreateDirectory(zielOrdner);
            string ziel = Path.Combine(zielOrdner, sqlite ? DB_DATEINAME_SQLITE : DB_DATEINAME);

            // Reste eines fruehen Laufs entfernen, damit nicht mit Altdaten gerechnet wird.
            foreach (string alt in Directory.GetFiles(zielOrdner, "*.laccdb"))
            {
                try { File.Delete(alt); } catch { /* egal, wird gleich ueberschrieben */ }
            }

            // Dasselbe fuer SQLite - und hier ist es KEINE Kosmetik: ein liegengebliebenes
            // -wal des Vorlaufs wuerde beim ersten Oeffnen in die frisch kopierte Datei
            // eingespielt. Die Arbeitskopie waere dann weder der Quellstand noch ein
            // gueltiger Stand.
            foreach (string alt in new[] { ziel + "-wal", ziel + "-shm" })
            {
                try { if (File.Exists(alt)) File.Delete(alt); } catch { }
            }

            File.Copy(quelle, ziel, true);
            // Der Installer legt die Produktiv-DB schreibgeschuetzt ab - das Attribut
            // wandert beim Kopieren mit und wuerde jeden Schreibzugriff blockieren.
            var info = new FileInfo(ziel);
            if (info.IsReadOnly) info.IsReadOnly = false;

            log.Zeile("Arbeitskopie angelegt: " + ziel +
                      " (" + (info.Length / 1024 / 1024) + " MB)");
            return ziel;
        }

        /// <summary>
        /// Biegt den DB-Pfad der App auf den uebergebenen ORDNER um.
        /// DataRepository.GetDBPath() haengt DB_DATEINAME selbst an, deshalb wird der
        /// Ordner gesetzt, nicht die Datei.
        ///
        /// WindowsFormsApplication1.Properties.Settings ist internal - der Zugriff laeuft
        /// bewusst ueber Reflection, damit im App-Projekt kein InternalsVisibleTo noetig ist.
        /// </summary>
        public static void DbPfadSetzen(string ordner)
        {
            Assembly app = typeof(DataRepository).Assembly;

            Type tSettings = app.GetType("WindowsFormsApplication1.Properties.Settings", false);
            if (tSettings == null)
                throw new InvalidOperationException(
                    "Typ WindowsFormsApplication1.Properties.Settings nicht gefunden.");

            PropertyInfo piDefault = tSettings.GetProperty("Default",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (piDefault == null)
                throw new InvalidOperationException("Settings.Default nicht gefunden.");

            object settings = piDefault.GetValue(null);

            PropertyInfo piDbPath = tSettings.GetProperty("DBPath",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (piDbPath != null && piDbPath.CanWrite)
            {
                piDbPath.SetValue(settings, ordner);
                return;
            }

            // Fallback: ueber den Indexer der ApplicationSettingsBase.
            PropertyInfo indexer = tSettings.GetProperty("Item",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (indexer == null)
                throw new InvalidOperationException("Weder DBPath-Property noch Indexer gefunden.");
            indexer.SetValue(settings, ordner, new object[] { "DBPath" });
        }

        /// <summary>Fragt die App, mit welcher Datei sie gerade arbeiten wuerde.</summary>
        public static string AktuellerDbPfad()
        {
            return DataRepository.GetDBPath();
        }

        /// <summary>
        /// Setzt den Pfad und prueft hart nach, dass die App wirklich auf die Arbeitskopie
        /// zeigt. Schlaegt die Pruefung fehl, wird abgebrochen - sonst bestuende die Gefahr,
        /// dass der Lauf die produktive Datenbank beschreibt.
        /// </summary>
        public static void AufArbeitskopieUmschaltenUndPruefen(string arbeitskopieOrdner, Protokoll log)
        {
            string erwartet = Path.GetFullPath(ArbeitskopieDatei(arbeitskopieOrdner));

            if (IstSqlite(erwartet))
            {
                // SQLite-Zweig: DataRepository.PfadUeberschreibung (Haken aus Paket S4a)
                // schlaegt jede Einstellung und laesst die Einstellungen des Anwenders
                // unangetastet.
                DataRepository.PfadUeberschreibung = erwartet;
            }
            else
            {
                // Access-Zweig (eingefrorener Altstand): Umbiegung ueber Settings.DBPath.
                DbPfadSetzen(arbeitskopieOrdner);
            }

            string tatsaechlich = Path.GetFullPath(AktuellerDbPfad());

            if (!string.Equals(erwartet, tatsaechlich, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "ABBRUCH: DataRepository.GetDBPath() liefert '" + tatsaechlich +
                    "', erwartet war '" + erwartet + "'. Der Lauf wuerde auf einer fremden " +
                    "Datenbank arbeiten - moeglicherweise der produktiven.");
            }

            // Zweiter Riegel: selbst wenn jemand den Arbeitskopie-Ordner falsch setzt,
            // darf das Ziel niemals eine der bekannten produktiven Ablagen sein - seit der
            // Umstellung gehoert die SQLite-Datei jeder dieser Ablagen mit dazu.
            string produktivOrdner = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EPOS_PLAN");
            foreach (string verboten in new[]
                     {
                         Path.Combine(produktivOrdner, DB_DATEINAME),
                         Path.Combine(produktivOrdner, DB_DATEINAME_SQLITE),
                         QUELLE_FALLBACK,
                         Path.ChangeExtension(QUELLE_FALLBACK, ".sqlite")
                     })
            {
                if (string.Equals(Path.GetFullPath(verboten), tatsaechlich, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "ABBRUCH: Der DB-Pfad zeigt auf die produktive Datenbank '" + tatsaechlich +
                        "'. Die Suite schreibt ausschliesslich auf einer Arbeitskopie.");
            }

            log.Zeile("DB-Pfad der App verifiziert: " + tatsaechlich);
        }
    }
}

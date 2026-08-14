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
        /// <summary>Dateiname wie in DataRepository.DB_DATEINAME.</summary>
        public const string DB_DATEINAME = "Kenndaten.accdb";

        /// <summary>Fallback-Quelle, falls unter %ProgramData% nichts liegt.</summary>
        private const string QUELLE_FALLBACK = @"C:\Waermeplan\Kenndaten.accdb";

        /// <summary>
        /// Sucht die produktive Datenbank: erst %ProgramData%\EPOS_PLAN\Kenndaten.accdb,
        /// dann der Fallback. Liefert null, wenn beides fehlt.
        /// </summary>
        public static string ProduktivQuelleFinden(Protokoll log)
        {
            string programData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EPOS_PLAN", DB_DATEINAME);

            if (File.Exists(programData))
            {
                log.Zeile("Quelle gefunden (ProgramData): " + programData);
                return programData;
            }
            log.Zeile("Quelle NICHT vorhanden: " + programData);

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
            string sperrdatei = Path.ChangeExtension(quelle, ".laccdb");
            if (File.Exists(sperrdatei))
            {
                log.Warnung("Die Quelldatenbank ist geoeffnet (" + Path.GetFileName(sperrdatei) +
                            " vorhanden). Es wird trotzdem lesend kopiert; die Kopie kann " +
                            "noch nicht geschriebene Aenderungen der laufenden Sitzung nicht enthalten.");
            }

            Directory.CreateDirectory(zielOrdner);
            string ziel = Path.Combine(zielOrdner, DB_DATEINAME);

            // Reste eines fruehen Laufs entfernen, damit nicht mit Altdaten gerechnet wird.
            foreach (string alt in Directory.GetFiles(zielOrdner, "*.laccdb"))
            {
                try { File.Delete(alt); } catch { /* egal, wird gleich ueberschrieben */ }
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
            DbPfadSetzen(arbeitskopieOrdner);

            string erwartet = Path.GetFullPath(Path.Combine(arbeitskopieOrdner, DB_DATEINAME));
            string tatsaechlich = Path.GetFullPath(AktuellerDbPfad());

            if (!string.Equals(erwartet, tatsaechlich, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "ABBRUCH: DataRepository.GetDBPath() liefert '" + tatsaechlich +
                    "', erwartet war '" + erwartet + "'. Der Lauf wuerde auf einer fremden " +
                    "Datenbank arbeiten - moeglicherweise der produktiven.");
            }

            // Zweiter Riegel: selbst wenn jemand den Arbeitskopie-Ordner falsch setzt,
            // darf das Ziel niemals eine der bekannten produktiven Ablagen sein.
            foreach (string verboten in new[]
                     {
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                      "EPOS_PLAN", DB_DATEINAME),
                         QUELLE_FALLBACK
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

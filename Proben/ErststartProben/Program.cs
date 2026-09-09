using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using WindowsFormsApplication1;

namespace ErststartProben
{
    /// <summary>
    /// ErststartProben (Auftrag #157) — der Nachweis, WAS der Kern tut, wenn im
    /// Datenbankordner weder <c>Kenndaten.sqlite</c> noch <c>Kenndaten.accdb</c>
    /// liegt. Das ist die Lage einer Neuinstallation auf einem frischen Rechner:
    /// Das Setup legt die Datenbank NICHT in den Datenordner, sondern nur eine
    /// Vorlage nach <c>{app}\Vorlage</c>
    /// (<c>Setup/EPOS-Plan.iss:45</c>, <c>:279-282</c>), und kein Pfad im Quelltext
    /// liest diesen Ordner.
    ///
    /// <para><b>Was hier NICHT geprueft werden kann.</b> Der Startpfad selbst
    /// (<c>WindowsFormsApplication1/Program.cs:232</c> …<c>:429</c>) und
    /// <c>ErststartMigration</c> liegen in der WinForms-Anwendung
    /// (<c>net10.0-windows</c>) und laufen auf Linux nicht. Nachgestellt wird
    /// deshalb der KERNANTEIL: <c>DataRepository.DatenbankVorhanden()</c>, die
    /// Pfadaufloesung und der gewoehnliche Lesezugriff. Die reine Dateipruefung
    /// <c>ErststartMigration.Pruefe</c>
    /// (<c>WindowsFormsApplication1/Allgemein/Update/ErststartMigration.cs:139-147</c>)
    /// ist hier NACHGEBILDET und als solche gekennzeichnet.</para>
    ///
    /// <para><b>Die drei Faelle.</b>
    /// <list type="number">
    /// <item>Leerer Ordner: <c>DatenbankVorhanden()</c> ist false UND legt nichts an
    /// (die Probe oeffnet <c>Mode=ReadOnly</c>).</item>
    /// <item>Wuerde der Startpfad trotzdem weiterlaufen: der erste gewoehnliche
    /// Zugriff legt die Datei stillschweigend an — LEER, ohne eine einzige
    /// Tabelle und damit ohne jeden Auslieferungskatalog.</item>
    /// <item>Gegenprobe: liegt eine <c>Kenndaten.accdb</c> im Ordner, ist das
    /// Lagebild ein anderes (<c>NurAccdbVorhanden</c>) — nur dann bietet der
    /// Assistent ueberhaupt etwas an.</item>
    /// </list></para>
    ///
    /// <para>Rueckgabe 0, wenn alle Erwartungen zutreffen, sonst 1.</para>
    /// </summary>
    internal static class Program
    {
        private const string SQLITE_DATEI = "Kenndaten.sqlite";
        private const string ACCDB_DATEI = "Kenndaten.accdb";

        private static int _verstoesse;
        private static int _pruefungen;

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

            string ziel = Argument(args, "--ziel")
                          ?? Path.Combine(Path.GetTempPath(), "ErststartProben_" + Guid.NewGuid().ToString("N"));

            Console.WriteLine("ErststartProben - Erststart OHNE Altbestand (Auftrag #157)");
            Console.WriteLine("Laufordner: " + ziel);
            Console.WriteLine();

            try
            {
                // Dialogfreier Modus: DataRepository meldet Fehler sonst ueber
                // Meldung.Zeigen. Im Engine-Modus wandert jede Meldung in die stille
                // Sammlung - ein unbeaufsichtigter Lauf bleibt damit nicht stehen.
                using (DataRepository.EngineModus())
                {
                    Fall1_LeererOrdner(Path.Combine(ziel, "fall1"));
                    Fall2_ErsterZugriff(Path.Combine(ziel, "fall2"));
                    Fall3_GegenprobeAltbestand(Path.Combine(ziel, "fall3"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ABBRUCH: " + ex.GetType().Name + " - " + ex.Message);
                return 1;
            }
            finally
            {
                DataRepository.PfadUeberschreibung = null;
            }

            Console.WriteLine();
            Console.WriteLine("Pruefungen: " + _pruefungen + ", Verstoesse: " + _verstoesse);
            return _verstoesse == 0 ? 0 : 1;
        }


        // =============================================================================
        // Fall 1 - der leere Datenbankordner
        // =============================================================================

        /// <summary>
        /// Die Lage einer Neuinstallation: Der Datenordner ist da (das Setup legt ihn
        /// an, <c>EPOS-Plan.iss:263</c>), aber leer. Geprueft wird, was
        /// <c>Program.cs:232</c> als erstes fragt.
        /// </summary>
        private static void Fall1_LeererOrdner(string ordner)
        {
            Console.WriteLine("--- Fall 1: leerer Datenbankordner ------------------------------");
            Directory.CreateDirectory(ordner);

            DataRepository.PfadUeberschreibung = Path.Combine(ordner, SQLITE_DATEI);

            Muss(DataRepository.GetDBPath() == Path.Combine(ordner, SQLITE_DATEI),
                 "GetDBPath zeigt auf " + Path.Combine(ordner, SQLITE_DATEI));

            // Nachgebildet: ErststartMigration.Pruefe - reine Dateipruefung, sie
            // oeffnet nichts (ErststartMigration.cs:139-147).
            bool sqliteDa = File.Exists(Path.Combine(ordner, SQLITE_DATEI));
            bool accdbDa = File.Exists(Path.Combine(ordner, ACCDB_DATEI));
            Muss(!sqliteDa && !accdbDa,
                 "Lagebild (nachgebildet) = BeidesFehlt: weder " + SQLITE_DATEI + " noch " + ACCDB_DATEI);

            // Das ist die Frage aus Program.cs:232.
            bool vorhanden = DataRepository.DatenbankVorhanden();
            Muss(!vorhanden, "DatenbankVorhanden() liefert false");

            // UND: die Probe darf nichts anlegen (SqliteDatenzugriff.cs:103-121,
            // Mode=ReadOnly). Der Ordner muss danach unveraendert leer sein.
            string[] danach = Directory.GetFileSystemEntries(ordner);
            Muss(danach.Length == 0,
                 "Der Ordner ist danach immer noch leer (DatenbankVorhanden legt nichts an); gefunden: "
                 + danach.Length);

            Console.WriteLine("  Folge im Programm: Program.cs:232 ruft ErststartAnbieten(); dort ist");
            Console.WriteLine("  ErststartCtrl.UmstellungFaellig() false (ErststartCtrl.cs:36/37), es");
            Console.WriteLine("  erscheint START_DB_FEHLT und Main kehrt zurueck - das Programm startet NICHT.");
            Console.WriteLine();
        }


        // =============================================================================
        // Fall 2 - was geschaehe, wenn der Startpfad weiterliefe
        // =============================================================================

        /// <summary>
        /// Der Gegenbeweis zur Vermutung „dann legt der Kern eben eine leere Datenbank
        /// an": Er legt sie an — aber sie ist wirklich LEER. Kein Schema, keine
        /// <c>Tab_*_STAMM</c>, kein Auslieferungskatalog. Die Schemapflege
        /// (<c>SchemaMigration.Ausfuehren</c>, <c>Program.cs:281</c>) hebt ein
        /// vorhandenes Schema an, sie erzeugt keines.
        /// </summary>
        private static void Fall2_ErsterZugriff(string ordner)
        {
            Console.WriteLine("--- Fall 2: erster gewoehnlicher Zugriff auf den leeren Ordner ---");
            Directory.CreateDirectory(ordner);

            string pfad = Path.Combine(ordner, SQLITE_DATEI);
            DataRepository.PfadUeberschreibung = pfad;

            // Ein gewoehnlicher Lesezugriff - so, wie ihn jede Maske absetzt.
            bool tabelleDa = DataRepository.TabelleVorhanden("Tab_Applikation");

            Muss(File.Exists(pfad),
                 "Der erste gewoehnliche Zugriff LEGT die Datei an (SQLite oeffnet im Standardmodus schreibend)");
            Muss(!tabelleDa, "Tab_Applikation ist NICHT vorhanden - es gibt kein Schema");

            DataTable inhalt = DataRepository.GetDataTable("SELECT name FROM sqlite_master");
            int objekte = inhalt == null ? -1 : inhalt.Rows.Count;
            Muss(objekte == 0, "sqlite_master ist leer: 0 Tabellen/Sichten/Indizes (gezaehlt: " + objekte + ")");

            long groesse = new FileInfo(pfad).Length;
            Console.WriteLine("  Dateigroesse der entstandenen Datenbank: " + groesse + " Byte");
            Console.WriteLine("  Bedeutung: Keine Kataloge, keine Tab_*_STAMM, keine Beispielprojekte.");
            Console.WriteLine("  Fuer einen Neukunden waere dieser Stand unbrauchbar - genau deshalb");
            Console.WriteLine("  bricht Program.ErststartAnbieten() vorher ab, statt ihn entstehen zu lassen.");
            Console.WriteLine();
        }


        // =============================================================================
        // Fall 3 - Gegenprobe: mit Altbestand sieht das Lagebild anders aus
        // =============================================================================

        /// <summary>
        /// Belegt, dass Fall 1 nicht an der Probe liegt, sondern am fehlenden Bestand:
        /// Sobald eine <c>Kenndaten.accdb</c> im Ordner liegt, ist das Lagebild
        /// <c>NurAccdbVorhanden</c> und der Erststart-Assistent hat einen Auftrag.
        /// Die Umstellung selbst laeuft hier nicht - sie braucht die Access-Engine
        /// und damit Windows.
        /// </summary>
        private static void Fall3_GegenprobeAltbestand(string ordner)
        {
            Console.WriteLine("--- Fall 3: Gegenprobe mit Altbestand ----------------------------");
            Directory.CreateDirectory(ordner);

            File.WriteAllText(Path.Combine(ordner, ACCDB_DATEI), "Attrappe - kein echter Access-Bestand.");
            DataRepository.PfadUeberschreibung = Path.Combine(ordner, SQLITE_DATEI);

            bool sqliteDa = File.Exists(Path.Combine(ordner, SQLITE_DATEI));
            bool accdbDa = File.Exists(Path.Combine(ordner, ACCDB_DATEI));

            Muss(!sqliteDa && accdbDa,
                 "Lagebild (nachgebildet) = NurAccdbVorhanden - nur hier ist eine Umstellung faellig");
            Muss(!DataRepository.DatenbankVorhanden(),
                 "DatenbankVorhanden() ist auch hier false - die .accdb ist keine Datenbank des Kerns");

            Console.WriteLine("  Folge im Programm: ErststartCtrl.UmstellungFaellig() ist true, der");
            Console.WriteLine("  Assistent laeuft, danach steht Kenndaten.sqlite (Program.cs:406).");
            Console.WriteLine();
        }


        // =============================================================================
        // Kleinkram
        // =============================================================================

        private static void Muss(bool bedingung, string was)
        {
            _pruefungen++;
            if (bedingung)
            {
                Console.WriteLine("  [ok]     " + was);
                return;
            }
            _verstoesse++;
            Console.WriteLine("  [FEHLER] " + was);
        }

        private static string Argument(string[] args, string name)
        {
            if (args == null) return null;
            string kopf = name + "=";
            foreach (string a in args)
            {
                if (a != null && a.StartsWith(kopf, StringComparison.Ordinal))
                    return a.Substring(kopf.Length);
            }
            return null;
        }
    }
}

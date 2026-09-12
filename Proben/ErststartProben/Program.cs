using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;

namespace ErststartProben
{
    /// <summary>
    /// ErststartProben (Auftrag #157, mit <b>W3</b> vom 09.09.2026 fortgeschrieben) —
    /// der Nachweis, WAS beim Zustand „keine Datenbank" geschieht: der Lage jeder
    /// Neuinstallation auf einem frischen Rechner.
    ///
    /// <para><b>Was #157 fand.</b> Lag im Datenbankordner weder
    /// <c>Kenndaten.sqlite</c> noch <c>Kenndaten.accdb</c>, startete das Programm
    /// NICHT: Das Setup legte zwar eine Vorlage nach <c>{app}\Vorlage</c>, aber kein
    /// Pfad im Quelltext las diesen Ordner.</para>
    ///
    /// <para><b>Was W3 daraus gemacht hat</b> (Anwenderentscheid #157‑E‑1): Der Kern
    /// kopiert die ausgelieferte Vorlage beim ersten Start in den Datenordner —
    /// <c>EPOS.Kern/Allgemein/Datenbank/Erstbereitstellung.cs</c>, gerufen aus
    /// <c>Program.DatenbankBereitstellen()</c> vor der Meldung <c>START_DB_FEHLT</c>.
    /// Der Access-Weg (Übernahme-Assistent, ACE-Engine) ist damit gefallen.</para>
    ///
    /// <para><b>Die drei Fälle.</b>
    /// <list type="number">
    /// <item>Leerer Ordner: <c>DatenbankVorhanden()</c> ist false UND legt nichts an
    /// (die Probe öffnet <c>Mode=ReadOnly</c>). Das ist die Frage, die der Start
    /// zuerst stellt.</item>
    /// <item>Würde der Startpfad trotzdem weiterlaufen: der erste gewöhnliche
    /// Zugriff legt die Datei stillschweigend an — LEER, ohne eine einzige
    /// Tabelle und damit ohne jeden Auslieferungskatalog. Das ist der Grund,
    /// warum es die Erstbereitstellung überhaupt braucht.</item>
    /// <item>Die Erstbereitstellung selbst, in vier Lagen: Vorlage kopiert
    /// (byte-gleich), vorhandenes Ziel unberührt, fehlende Vorlage ohne Zieldatei,
    /// kaputte Vorlage ohne Zieldatei.</item>
    /// </list></para>
    ///
    /// <para>Rückgabe 0, wenn alle Erwartungen zutreffen, sonst 1.</para>
    /// </summary>
    internal static class Program
    {
        private const string SQLITE_DATEI = "Kenndaten.sqlite";
        private const string VORLAGE_DATEI = "Kenndaten.sqlite";

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
                    Fall3_Erstbereitstellung(Path.Combine(ziel, "fall3"));
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
        /// an, <c>EPOS-Plan.iss</c>, Abschnitt <c>[Dirs]</c>), aber leer. Geprueft wird,
        /// was der Start als erstes fragt.
        /// </summary>
        private static void Fall1_LeererOrdner(string ordner)
        {
            Console.WriteLine("--- Fall 1: leerer Datenbankordner ------------------------------");
            Directory.CreateDirectory(ordner);

            DataRepository.PfadUeberschreibung = Path.Combine(ordner, SQLITE_DATEI);

            Muss(DataRepository.GetDBPath() == Path.Combine(ordner, SQLITE_DATEI),
                 "GetDBPath zeigt auf " + Path.Combine(ordner, SQLITE_DATEI));

            // Reine Dateipruefung - sie oeffnet nichts.
            bool sqliteDa = File.Exists(Path.Combine(ordner, SQLITE_DATEI));
            Muss(!sqliteDa, "Im Ordner liegt keine " + SQLITE_DATEI);

            // Das ist die Frage, die der Start zuerst stellt (Program.cs).
            bool vorhanden = DataRepository.DatenbankVorhanden();
            Muss(!vorhanden, "DatenbankVorhanden() liefert false");

            // UND: die Probe darf nichts anlegen (SqliteDatenzugriff.cs:103-121,
            // Mode=ReadOnly). Der Ordner muss danach unveraendert leer sein.
            string[] danach = Directory.GetFileSystemEntries(ordner);
            Muss(danach.Length == 0,
                 "Der Ordner ist danach immer noch leer (DatenbankVorhanden legt nichts an); gefunden: "
                 + danach.Length);

            Console.WriteLine("  Folge im Programm (seit W3): Program.DatenbankBereitstellen() ruft");
            Console.WriteLine("  Erstbereitstellung.Sicherstellen(Zielpfad, Dienste.Pfade.Auslieferungsvorlage).");
            Console.WriteLine("  Gibt es die Vorlage, entsteht die Datenbank hier; gibt es sie nicht,");
            Console.WriteLine("  erscheint START_DB_FEHLT samt erwartetem Vorlagenpfad und Main kehrt zurueck.");
            Console.WriteLine();
        }


        // =============================================================================
        // Fall 2 - was geschaehe, wenn der Startpfad weiterliefe
        // =============================================================================

        /// <summary>
        /// Der Gegenbeweis zur Vermutung „dann legt der Kern eben eine leere Datenbank
        /// an": Er legt sie an — aber sie ist wirklich LEER. Kein Schema, keine
        /// <c>Tab_*_STAMM</c>, kein Auslieferungskatalog. Die Schemapflege
        /// (<c>SchemaMigration.Ausfuehren</c>) hebt ein vorhandenes Schema an, sie
        /// erzeugt keines — GENAU deshalb muss die Erstbereitstellung eine gefuellte
        /// Vorlage kopieren und nicht eine leere Datei anlegen.
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
            Console.WriteLine("  kopiert die Erstbereitstellung die AUSGELIEFERTE VORLAGE (Fall 3).");
            Console.WriteLine();
        }


        // =============================================================================
        // Fall 3 - die Erstbereitstellung aus der Auslieferungsvorlage (W3)
        // =============================================================================

        /// <summary>
        /// Der Nachweis des Wegs, den <b>W3</b> gebaut hat: vier Lagen von
        /// <c>Erstbereitstellung.Sicherstellen</c> auf jeweils eigenen Wegwerf-Ordnern.
        ///
        /// <list type="number">
        /// <item><b>Vorlage kopiert</b> — die Zieldatei entsteht und ist BYTE-GLEICH zur
        /// Vorlage; der gelesene Schemastand ist der der Vorlage.</item>
        /// <item><b>Ziel vorhanden</b> — die Vorlage wird nicht angefasst, die vorhandene
        /// Datei bleibt Byte für Byte, wie sie war.</item>
        /// <item><b>Vorlage fehlt</b> — Lage <c>VorlageFehlt</c>, und es entsteht KEINE
        /// Zieldatei (der Startpfad meldet dann <c>START_DB_FEHLT</c>).</item>
        /// <item><b>Vorlage kaputt</b> — Lage <c>Fehler</c>, und die halb angelegte
        /// Zieldatei ist wieder weg. Sonst stünde beim nächsten Start eine Ruine da, die
        /// als „vorhanden" gälte.</item>
        /// </list>
        /// </summary>
        private static void Fall3_Erstbereitstellung(string ordner)
        {
            Console.WriteLine("--- Fall 3: Erstbereitstellung aus der Auslieferungsvorlage (W3) --");
            Directory.CreateDirectory(ordner);

            // (1) Vorlage vorhanden -> kopiert und byte-gleich ------------------------
            string vorlage = Path.Combine(ordner, "Vorlage", VORLAGE_DATEI);
            VorlageBauen(vorlage, 71);

            string ziel1 = Path.Combine(ordner, "leer", SQLITE_DATEI);
            Erstbereitstellungsergebnis e1 = Erstbereitstellung.Sicherstellen(ziel1, vorlage);

            Muss(e1.Lage == Erstbereitstellungslage.Kopiert,
                 "Lage = Kopiert (gemeldet: " + e1.Lage + " - " + e1.Meldung + ")");
            Muss(e1.Bereit, "Bereit = true");
            Muss(File.Exists(ziel1), "die Datenbank ist entstanden: " + ziel1);
            Muss(Gleich(vorlage, ziel1), "die Kopie ist BYTE-GLEICH zur Vorlage");
            Muss(e1.Schemastand == 71,
                 "der gelesene Schemastand ist der der Vorlage (71, gemeldet: " + e1.Schemastand + ")");

            // Und danach ist die Datenbank auch wirklich zu oeffnen - genau das
            // fragt der Startpfad ein zweites Mal.
            DataRepository.PfadUeberschreibung = ziel1;
            Muss(DataRepository.DatenbankVorhanden(),
                 "DatenbankVorhanden() liefert danach true - das Programm startet");

            // (2) Ziel vorhanden -> nichts wird angefasst -----------------------------
            string abdruck = Pruefsumme(ziel1);
            Erstbereitstellungsergebnis e2 = Erstbereitstellung.Sicherstellen(ziel1, vorlage);

            Muss(e2.Lage == Erstbereitstellungslage.VorhandenBelassen,
                 "zweiter Aufruf: Lage = VorhandenBelassen (gemeldet: " + e2.Lage + ")");
            Muss(Pruefsumme(ziel1) == abdruck,
                 "die vorhandene Datenbank ist Byte fuer Byte unveraendert");

            // (3) Vorlage fehlt -> keine Zieldatei ------------------------------------
            string ziel3 = Path.Combine(ordner, "ohnevorlage", SQLITE_DATEI);
            string fehlt = Path.Combine(ordner, "gibtsnicht", VORLAGE_DATEI);
            Erstbereitstellungsergebnis e3 = Erstbereitstellung.Sicherstellen(ziel3, fehlt);

            Muss(e3.Lage == Erstbereitstellungslage.VorlageFehlt,
                 "ohne Vorlage: Lage = VorlageFehlt (gemeldet: " + e3.Lage + ")");
            Muss(!e3.Bereit, "Bereit = false");
            Muss(!File.Exists(ziel3), "es ist KEINE Zieldatei entstanden: " + ziel3);
            Muss(e3.Vorlagepfad == fehlt,
                 "das Ergebnis nennt den erwarteten Vorlagenpfad (fuer die Startmeldung)");

            // (4) Vorlage kaputt -> Fehler, und das Ziel ist wieder weg ---------------
            string kaputt = Path.Combine(ordner, "kaputt", VORLAGE_DATEI);
            Directory.CreateDirectory(Path.GetDirectoryName(kaputt));
            File.WriteAllText(kaputt, "Das ist keine SQLite-Datei, sondern Text.");

            string ziel4 = Path.Combine(ordner, "kaputtziel", SQLITE_DATEI);
            Erstbereitstellungsergebnis e4 = Erstbereitstellung.Sicherstellen(ziel4, kaputt);

            Muss(e4.Lage == Erstbereitstellungslage.Fehler,
                 "kaputte Vorlage: Lage = Fehler (gemeldet: " + e4.Lage + ")");
            Muss(!File.Exists(ziel4),
                 "die halb angelegte Zieldatei ist wieder entfernt: " + ziel4);

            DataRepository.PfadUeberschreibung = null;
            Console.WriteLine();
        }

        /// <summary>
        /// Baut eine winzige, gueltige Auslieferungsvorlage: eine <c>Tab_Applikation</c>
        /// mit dem Schemamarker. Mehr braucht <c>Erstbereitstellung</c> nicht — sie prueft
        /// <c>PRAGMA integrity_check</c> und das VORHANDENSEIN des Markers, nicht seine
        /// Zahl (die Schemapflege hebt eine aeltere Vorlage beim selben Start an).
        /// </summary>
        private static void VorlageBauen(string pfad, int schemastand)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pfad));
            if (File.Exists(pfad)) File.Delete(pfad);

            using (var verbindung = new SqliteConnection("Data Source=" + pfad + ";Pooling=False"))
            {
                verbindung.Open();
                using (SqliteCommand cmd = verbindung.CreateCommand())
                {
                    cmd.CommandText =
                        "CREATE TABLE Tab_Applikation (ID INTEGER PRIMARY KEY, SchemaVersion INTEGER)";
                    cmd.ExecuteNonQuery();
                }
                using (SqliteCommand cmd = verbindung.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Tab_Applikation (ID, SchemaVersion) VALUES (1, $v)";
                    cmd.Parameters.AddWithValue("$v", schemastand);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Sind zwei Dateien byte-gleich?</summary>
        private static bool Gleich(string a, string b)
        {
            try
            {
                byte[] x = File.ReadAllBytes(a);
                byte[] y = File.ReadAllBytes(b);
                if (x.Length != y.Length) return false;
                for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
                return true;
            }
            catch { return false; }
        }

        /// <summary>Laenge und Zeitstempel als billiger Abdruck einer Datei.</summary>
        private static string Pruefsumme(string pfad)
        {
            try
            {
                var f = new FileInfo(pfad);
                return f.Length.ToString(CultureInfo.InvariantCulture) + "|" +
                       f.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture);
            }
            catch { return "?"; }
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

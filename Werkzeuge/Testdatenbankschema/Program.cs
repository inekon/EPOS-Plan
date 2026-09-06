using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using WindowsFormsApplication1;

namespace Testdatenbankschema
{
    /// <summary>
    /// Zieht eine SQLite-Datenbank auf <see cref="SchemaStand.Zielversion"/> nach —
    /// gedacht fuer <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>, die Messlatte des
    /// SQL-Dialektpruefers und die Quelle jedes Referenzlaufs.
    ///
    /// <para><b>Der Weg ist der der Migration, nicht ein zweiter.</b> Die Spalten kommen
    /// aus <see cref="SchemaKatalog"/>, die Typen aus <c>StilleDb.SqliteSpaltenTyp</c>,
    /// die zwei DELETE-Texte des Schritts 62 aus <c>KlimaWaisenBereinigung</c> — also
    /// aus genau den Quellen, aus denen sich auch
    /// <c>SchemaMigration.Schritt_62_KlimaWaisen</c>,
    /// <c>Schritt_63_PvAnlagenparameter</c>, <c>Schritt_64_PvModellwahl</c> und
    /// <c>Schritt_65_Wechselrichterkatalog</c> bedienen (dessen zwei CREATE TABLE stehen
    /// in <c>WechselrichterSchema</c>). Hier steht keine abgeschriebene DDL.</para>
    ///
    /// <para><b>Idempotent.</b> Eine vorhandene Spalte wird uebergangen, ein zweiter Lauf
    /// aendert nichts mehr. Rueckgabe 0 = Datei steht auf dem Zielstand.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Die Schritte 63 und 64 legen ausschliesslich Spalten
    /// an, Schritt 65 zwei LEERE Tabellen, und keiner von ihnen schreibt einen Wert (NULL heisst im Rechenweg genau die bisher fest
    /// verdrahtete Vorbelegung); Schritt 62 loescht nur Zeilen ohne Kopfsatz, die ueber
    /// keine Abfrage des Programms erreichbar sind. Der Referenzlauf muss vor und nach
    /// dem Nachziehen byte-gleiche CSV liefern — das ist die Abnahme.</para>
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1 || args[0] == "--hilfe" || args[0] == "-h")
            {
                Console.WriteLine("Aufruf: Testdatenbankschema <pfad-zur.sqlite> [--trocken]");
                Console.WriteLine();
                Console.WriteLine("  Zieht die Datei auf Schemastand " + SchemaStand.Zielversion +
                                  " nach (Schritte 62 bis 65) und fuehrt danach VACUUM aus.");
                Console.WriteLine("  --trocken  nur berichten, nichts aendern.");
                return 2;
            }

            string pfad = Path.GetFullPath(args[0]);
            bool trocken = Array.IndexOf(args, "--trocken") >= 0;

            if (!File.Exists(pfad))
            {
                Console.Error.WriteLine("Datei nicht gefunden: " + pfad);
                return 2;
            }

            DataRepository.PfadUeberschreibung = pfad;

            // DIE WERKZEUG-FREIGABE DER SCHREIBNAHT (Welle iF30) - EINE benannte Zeile,
            // ausdruecklich und nicht durch Auslassen. Dieses Werkzeug legt Spalten an und
            // schreibt den Schemamarker; eine Lizenz hat es nicht und braucht es nicht.
            Schreibnaht.WerkzeugFreigabe("Werkzeug Testdatenbankschema");

            Console.WriteLine("Datei:  " + pfad);
            Console.WriteLine("Groesse vorher: " + Mb(pfad));

            int vorher = SchemaVersionLesen();
            Console.WriteLine("Schemastand vorher: " + vorher + "   (Zielstand " + SchemaStand.Zielversion + ")");
            Console.WriteLine();

            if (trocken) Console.WriteLine("--trocken: es wird nichts geschrieben.");
            Console.WriteLine();

            int angelegt = 0;

            // ---- Schritt 62: die verwaisten Klimadaten-Zeilen (kein DDL, zwei DELETE) ----
            long waisen = 0;
            foreach (string tabelle in KlimaWaisenBereinigung.Datenblocktabellen())
            {
                long z = Zahl(KlimaWaisenBereinigung.ZaehlungZu(tabelle));
                waisen += Math.Max(0, z);
                Console.WriteLine("Schritt 62 - " + tabelle + ": Waisen " + z + ".");
                if (!trocken && z > 0)
                    DataRepository.ExecuteNonQuery(KlimaWaisenBereinigung.LoeschungZu(tabelle));
            }
            Console.WriteLine("Schritt 62: " + (waisen == 0
                ? "nichts zu tun - kein Datenblock ohne Kopfsatz."
                : waisen + " verwaiste Zeile(n) abgeraeumt."));
            Console.WriteLine();

            // ---- Schritt 63: zwei PV-Anlagenparameter. Der Katalog fuehrt DOUBLE, die
            //      STRICT-Tabelle nimmt REAL - wortgleich zu Schritt_63_PvAnlagenparameter.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt63_PvAnlagenparameter)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name, "REAL", 63, trocken);

            // ---- Schritt 64: sechs Spalten der Modellwahl, dazu Stammtabelle und
            //      Degradation. Typ ueber dieselbe Uebersetzung wie die Rueckfallebene.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvModellwahl)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 64, trocken);
            foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvStammUndDegradation)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 64, trocken);

            // ---- Schritt 65: der Wechselrichterkatalog und seine Projektkopie.
            //      Die DDL kommt aus WechselrichterSchema - DIESELBE Quelle, aus der
            //      sich SchemaMigration.Schritt_65_Wechselrichterkatalog bedient.
            //      CREATE TABLE IF NOT EXISTS ist selbst idempotent.
            int tabellen = 0;
            foreach (KeyValuePair<string, string> a in WechselrichterSchema.Anweisungen)
                tabellen += TabelleSicherstellen(a.Key, a.Value, 65, trocken);

            Console.WriteLine();
            Console.WriteLine(angelegt + " Spalte(n) angelegt, " + tabellen + " Tabelle(n) angelegt.");

            if (trocken)
            {
                Console.WriteLine("--trocken: Marker und VACUUM uebersprungen.");
                return 0;
            }

            // ---- Marker und Verdichtung ----
            DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET SchemaVersion = " +
                                           SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture));
            DataRepository.ExecuteNonQuery("VACUUM");

            int nachher = SchemaVersionLesen();
            Console.WriteLine();
            Console.WriteLine("Schemastand nachher: " + nachher + "   (Zielstand " + SchemaStand.Zielversion + ")");
            Console.WriteLine("Groesse nachher: " + Mb(pfad));
            return nachher >= SchemaStand.Zielversion ? 0 : 1;
        }

        /// <summary>
        /// Legt eine Spalte an, wenn sie fehlt — dieselbe Vorpruefung wie
        /// <c>SchemaMigration.SqliteSpalteAnlegen</c>: vorhandene Spalte = nichts zu tun.
        /// Rueckgabe 1, wenn angelegt wurde, sonst 0.
        ///
        /// <para><b>Warum <c>pragma_table_info</c> und nicht <c>PRAGMA table_info</c>.</b>
        /// Der PRAGMA liefert die Spalte <c>dflt_value</c> ohne festen Typ; das Fuellen
        /// einer <see cref="DataTable"/> daraus scheitert an der ersten Zeile mit einer
        /// Vorgabe ("Couldn't store &lt;0&gt; in dflt_value Column"). Die Tabellenfunktion
        /// beantwortet dieselbe Frage als Zahl und ist damit unabhaengig vom Typraten -
        /// wichtig, weil sonst der zweite Lauf die Spalte fuer fehlend hielte und das
        /// Werkzeug seine Idempotenzzusage braeche.</para>
        /// </summary>
        private static int SpalteSicherstellen(string tabelle, string spalte, string typ, int schritt, bool trocken)
        {
            object da = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM pragma_table_info('" + tabelle.Replace("'", "''") + "') " +
                "WHERE lower(name) = lower('" + spalte.Replace("'", "''") + "')");
            if (da != null && da != DBNull.Value && Convert.ToInt64(da) > 0)
            {
                Console.WriteLine("Schritt " + schritt + " - " + tabelle + "." + spalte + ": vorhanden.");
                return 0;
            }

            Console.WriteLine("Schritt " + schritt + " - " + tabelle + "." + spalte + ": anlegen als " + typ + ".");
            if (!trocken)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + spalte + "\" " + typ);
            return 1;
        }

        /// <summary>
        /// Legt eine Tabelle an, wenn sie fehlt. Die Anweisung traegt ihr
        /// <c>IF NOT EXISTS</c> selbst; die Vorabfrage dient allein der Zaehlung im
        /// Bericht. Rueckgabe 1, wenn angelegt wurde, sonst 0.
        /// </summary>
        private static int TabelleSicherstellen(string tabelle, string ddl, int schritt, bool trocken)
        {
            object da = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" +
                tabelle.Replace("'", "''") + "'");
            bool vorhanden = da != null && da != DBNull.Value && Convert.ToInt64(da) > 0;

            if (vorhanden)
            {
                Console.WriteLine("Schritt " + schritt + " - " + tabelle + ": vorhanden.");
                return 0;
            }

            Console.WriteLine("Schritt " + schritt + " - " + tabelle + ": anlegen.");
            if (!trocken) DataRepository.ExecuteNonQuery(ddl);
            return 1;
        }

        /// <summary>Der Schemamarker aus <c>Tab_Applikation</c>; -1, wenn nicht lesbar.</summary>
        private static int SchemaVersionLesen()
        {
            try
            {
                object o = DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation");
                return o == null || o == DBNull.Value ? -1 : Convert.ToInt32(o);
            }
            catch { return -1; }
        }

        private static long Zahl(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o);
            }
            catch { return -1; }
        }

        private static string Mb(string pfad)
        {
            long b = new FileInfo(pfad).Length;
            return b.ToString("N0", CultureInfo.InvariantCulture) + " Byte (" +
                   (b / 1024.0 / 1024.0).ToString("N1", CultureInfo.InvariantCulture) + " MB)";
        }
    }
}

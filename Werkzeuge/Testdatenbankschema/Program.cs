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
    /// <c>Schritt_63_PvAnlagenparameter</c>, <c>Schritt_64_PvModellwahl</c>,
    /// <c>Schritt_65_Wechselrichterkatalog</c> (dessen zwei CREATE TABLE stehen in
    /// <c>WechselrichterSchema</c>), <c>Schritt_66_Strangzuordnung</c> (Tabelle in
    /// <c>AnlageStrangSchema</c>, Spalte in
    /// <c>SchemaKatalog.Schritt66_PvWechselrichterweg</c>) und
    /// <c>Schritt_67_BhkwLeistungsgrenze</c> (das eine UPDATE aus
    /// <c>BhkwLeistungsgrenzeVorgabe</c>) und <c>Schritt_68_StromspeicherFirma</c>
    /// (Spalten aus <c>SchemaKatalog.Schritt68_StromspeicherFirma</c>, Nachtrag aus
    /// <c>StromspeicherFirmaNachtrag</c>) bedienen. Hier steht keine
    /// abgeschriebene DDL und kein abgeschriebenes DML.</para>
    ///
    /// <para><b>Idempotent.</b> Eine vorhandene Spalte wird uebergangen, ein zweiter Lauf
    /// aendert nichts mehr. Rueckgabe 0 = Datei steht auf dem Zielstand.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Die Schritte 63 und 64 legen ausschliesslich Spalten
    /// an, Schritt 65 zwei LEERE Tabellen, Schritt 66 eine LEERE Tabelle und eine
    /// NULL-Spalte, und keiner von ihnen schreibt einen Wert (NULL heisst im Rechenweg genau die bisher fest
    /// verdrahtete Vorbelegung); Schritt 62 loescht nur Zeilen ohne Kopfsatz, die ueber
    /// keine Abfrage des Programms erreichbar sind. <b>Schritt 67 schreibt als einziger
    /// einen FACHWERT</b> (<c>Tab_Einstellungen.Leistungsgrenze</c> NULL → 30) — und ist
    /// gerade dadurch ergebnisneutral: Er setzt an die Stelle des stillen Fallbacks in
    /// <c>SimulationBHKW</c>, der mit dem Anwenderentscheid W6-E-7 gefallen ist, genau
    /// den Wert, mit dem diese Saetze bisher schon gerechnet haben. <b>Schritt 68</b>
    /// legt zwei Spalten an und traegt in eine davon nach, was der Bezeichner schon
    /// sagt — auch das ergebnisneutral, weil kein Rechenweg den Hersteller liest. Der
    /// Referenzlauf muss vor und nach dem Nachziehen byte-gleiche CSV liefern — das ist
    /// die Abnahme.</para>
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
                                  " nach (Schritte 62 bis 68) und fuehrt danach VACUUM aus.");
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

            // ---- Schritt 66: die Strangzuordnung und der sichtbare Wechselrichterweg.
            //      Zwei Quellen, beide im Kern - die TABELLE aus AnlageStrangSchema, die
            //      SPALTE aus SchemaKatalog.Schritt66_PvWechselrichterweg. DIESELBEN,
            //      aus denen sich SchemaMigration.Schritt_66_Strangzuordnung bedient.
            foreach (KeyValuePair<string, string> a in AnlageStrangSchema.Anweisungen)
                tabellen += TabelleSicherstellen(a.Key, a.Value, 66, trocken);

            foreach (SchemaSpalte s in SchemaKatalog.Schritt66_PvWechselrichterweg)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 66, trocken);

            // ---- Schritt 67: die sichtbare BHKW-Leistungsuntergrenze (W6-E-7).
            //      DAS ERSTE DML DIESES WERKZEUGS mit einem FACHWERT - anders als die
            //      Schritte 63 bis 66, die nur Spalten und leere Tabellen anlegen. Es
            //      ist trotzdem ergebnisneutral: Genau die Saetze, die es anfasst,
            //      rechneten bisher ueber den stillen Fallback in SimulationBHKW mit
            //      denselben 30 %. Die Anweisung kommt aus BhkwLeistungsgrenzeVorgabe -
            //      DIESELBE Quelle, aus der sich SchemaMigration.Schritt_67_
            //      BhkwLeistungsgrenze bedient. Idempotent ueber ihr eigenes IS NULL.
            long ohneWert = Zahl(BhkwLeistungsgrenzeVorgabe.Zaehlung());
            Console.WriteLine("Schritt 67 - " + BhkwLeistungsgrenzeVorgabe.TABELLE + "." +
                              BhkwLeistungsgrenzeVorgabe.SPALTE + ": ohne gepflegten Wert " +
                              ohneWert + ".");
            if (!trocken && ohneWert > 0)
                DataRepository.ExecuteNonQuery(BhkwLeistungsgrenzeVorgabe.Anhebung());
            Console.WriteLine("Schritt 67: " + (ohneWert == 0
                ? "nichts zu tun - jeder Satz fuehrt einen gepflegten Wert."
                : ohneWert + " Satz/Saetze auf " +
                  BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT + " % gehoben."));
            Console.WriteLine();

            // ---- Schritt 68: der Hersteller des Stromspeicherkatalogs (W14a-E-10-Q7).
            //      ERST die zwei Spalten, DANN der Nachtrag - das UPDATE nennt Firma
            //      und liefe auf einer Datenbank ohne die Spalte in einen Fehler. Die
            //      Quellen sind dieselben, aus denen sich
            //      SchemaMigration.Schritt_68_StromspeicherFirma bedient:
            //      SchemaKatalog.Schritt68_StromspeicherFirma und
            //      StromspeicherFirmaNachtrag. Ergebnisneutral - kein Rechenweg liest
            //      den Hersteller.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt68_StromspeicherFirma)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 68, trocken);

            if (!trocken)
            {
                long ohnePraefix = Zahl(StromspeicherFirmaNachtrag.Zaehlung());
                Console.WriteLine("Schritt 68 - " + StromspeicherFirmaNachtrag.TABELLE + "." +
                                  StromspeicherFirmaNachtrag.SPALTE +
                                  ": nachzutragen " + ohnePraefix + " von " +
                                  Zahl(StromspeicherFirmaNachtrag.Gesamtzahl()) + ".");
                if (ohnePraefix > 0)
                    DataRepository.ExecuteNonQuery(StromspeicherFirmaNachtrag.Nachtrag());
                Console.WriteLine("Schritt 68: " + (ohnePraefix == 0
                    ? "nichts zu tun - kein Satz traegt ein Bezeichnerpraefix."
                    : ohnePraefix + " Satz/Saetze aus dem Bezeichnerpraefix nachgetragen."));
            }
            Console.WriteLine();

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

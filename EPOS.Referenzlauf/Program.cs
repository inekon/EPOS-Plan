using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Referenzlauf OHNE Windows (Umsetzungskonzept iOS, Paket iU3).
    ///
    /// <para>Modi:</para>
    /// <code>
    ///   lauf      --quelle &lt;sqlite&gt; [--projekte 1030,1007] [--ziel &lt;ordner&gt;]
    ///   projekt   &lt;id&gt; &lt;zielordner&gt;
    ///   vergleich &lt;refOrdner&gt; &lt;neuOrdner&gt; [--ohne &lt;a,b&gt;]
    ///   pruefen   &lt;ordner&gt;
    /// </code>
    ///
    /// <para><b>Was hier fehlt und warum.</b> Kein Kindprozess je Projekt, kein
    /// <c>DialogWaechter</c>, kein <c>Migrationslauf</c>. Der Kindprozess war die einzige
    /// Handhabe gegen eine haengende <c>MessageBox</c> der Engine; seit iU3-2 setzt der
    /// Rechenkern keine mehr ab (<c>Allgemein\Meldung.cs</c>), und der Waechter, der sie
    /// wegklickte, ist reine WinForms-Vorsorge. Die Migration wiederum gehoert zum
    /// eingefrorenen Access-Zweig — der Kern rechnet auf einer bereits migrierten
    /// SQLite-Datei. Alles laeuft deshalb IN DIESEM Prozess.</para>
    ///
    /// <para><b>Die Kultur wird ausdruecklich gesetzt.</b> Unter Windows lief die Suite
    /// mit der Systemkultur des Anwenders, praktisch immer de-DE. Auf einem Linux-Laeufer
    /// ist die invariante Kultur voreingestellt — dieselbe Rechnung liesse dort andere
    /// Zahlen entstehen, wo irgendwo <c>Convert.ToDouble</c> ohne Formatangabe steht.
    /// Damit ein Vergleich Plattformdrift und nicht Kulturdrift misst, wird de-DE
    /// gesetzt und protokolliert.</para>
    /// </summary>
    internal static class Program
    {
        private const string ORDNER_REFERENZLAEUFE = "Referenzlaeufe";
        private const string ORDNER_ARBEITSKOPIE = "Arbeitskopie";

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            KulturSetzen();
            WerkzeugFreigabe();

            if (args.Length == 0) { Hilfe(); return 2; }

            try
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "lauf":
                        return ModusLauf(args.Skip(1).ToArray());

                    case "projekt":
                        if (args.Length < 3) { Hilfe(); return 2; }
                        return ModusProjekt(args[1], args[2]);

                    case "vergleich":
                        if (args.Length < 3) { Hilfe(); return 2; }
                        return Vergleich.Ausfuehren(args[1], args[2],
                                                    (Argument(args, "--ohne") ?? "").Split(','));

                    case "pruefen":
                        if (args.Length < 2) { Hilfe(); return 2; }
                        return Plausibilitaet.Pruefen(args[1]);

                    default:
                        Hilfe();
                        return 2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ABBRUCH: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return 2;
            }
        }

        /// <summary>
        /// Setzt Rechen- UND Anzeigekultur fest auf de-DE und schreibt sie ins Protokoll.
        /// Siehe die Begruendung im Klassenkommentar.
        /// </summary>
        private static void KulturSetzen()
        {
            var kultur = new CultureInfo("de-DE");
            CultureInfo.DefaultThreadCurrentCulture = kultur;
            CultureInfo.DefaultThreadCurrentUICulture = kultur;
            System.Threading.Thread.CurrentThread.CurrentCulture = kultur;
            System.Threading.Thread.CurrentThread.CurrentUICulture = kultur;
            Console.WriteLine("Kultur (Rechnen und Anzeige): " + kultur.Name);
        }

        /// <summary>
        /// <b>Die Werkzeug-Freigabe der Schreibnaht</b> (Welle iF30) — EINE benannte
        /// Zeile, ausdrücklich und nicht durch Auslassen.
        ///
        /// <para>Seit iF30 sperrt <c>Schreibnaht</c> jeden schreibenden Datenbankzugriff,
        /// solange die Lizenz keinen erlaubt. Dieser Lauf hat keine Lizenz — er läuft auf
        /// einem Linux-Läufer der CI, auf einer ARBEITSKOPIE der Testdatenbank — und er
        /// SCHREIBT: <c>SimuliereUndSpeichere</c> legt je Projekt einen Ergebniskopf an.
        /// Ohne diese Zeile fiele der Rechennachweis rot aus, und zwar aus einem Grund,
        /// der mit dem Rechenweg nichts zu tun hat.</para>
        /// </summary>
        private static void WerkzeugFreigabe()
        {
            Schreibnaht.WerkzeugFreigabe("EPOS.Referenzlauf (Rechennachweis ohne Lizenz)");
            Console.WriteLine("Schreibnaht: freigegeben für " + Schreibnaht.WerkzeugGrund);
        }

        private static void Hilfe()
        {
            Console.WriteLine("EPOS.Referenzlauf - Referenzlauf ohne Windows (iU3)");
            Console.WriteLine();
            Console.WriteLine("  EPOS.Referenzlauf lauf --quelle <sqlite> [--projekte 1030,1007] [--ziel <ordner>]");
            Console.WriteLine("  EPOS.Referenzlauf projekt <id> <zielordner>");
            Console.WriteLine("  EPOS.Referenzlauf vergleich <refOrdner> <neuOrdner> [--ohne <a,b>]");
            Console.WriteLine("  EPOS.Referenzlauf pruefen <ordner>");
        }

        // =================================================================================
        // Modus "lauf"
        // =================================================================================

        private static int ModusLauf(string[] args)
        {
            var log = new Protokoll();
            var start = DateTime.Now;

            string wurzel = ProjektWurzelFinden();
            string basis = Path.Combine(wurzel, ORDNER_REFERENZLAEUFE);
            string zielWurzel = Argument(args, "--ziel") ??
                                Path.Combine(basis, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "_Kern");
            string arbeitskopieOrdner = Path.Combine(basis, ORDNER_ARBEITSKOPIE);

            log.Zeile("Referenzlauf (Kern) gestartet.");
            log.Zeile("Projektwurzel: " + wurzel);
            log.Zeile("Zielordner:    " + zielWurzel);
            log.Leerzeile();

            // --- 1. Arbeitskopie ---------------------------------------------------------
            string quelle = DbUmgebung.ProduktivQuelleFinden(log, Argument(args, "--quelle"));
            if (quelle == null)
            {
                log.FehlerZeile("Keine Datenbank gefunden - Abbruch.");
                return 2;
            }
            if (!DbUmgebung.IstSqlite(quelle))
            {
                log.FehlerZeile("Der Kernlauf rechnet ausschliesslich auf SQLite. " +
                                "Ein .accdb-Bestand muss zuvor migriert werden.");
                return 2;
            }
            DbUmgebung.ArbeitskopieAnlegen(quelle, arbeitskopieOrdner, log);

            // --- 2. DB-Pfad umbiegen und hart pruefen ------------------------------------
            // Der SQLite-Zweig geht ueber DataRepository.PfadUeberschreibung; die
            // Einstellungen des Anwenders bleiben unangetastet.
            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(arbeitskopieOrdner, log);
            log.Leerzeile();

            // --- 3. Projektauswahl -------------------------------------------------------
            log.Zeile("Projektlandschaft wird gelesen ...");
            List<Projektprofil> profile = Projektauswahl.ProfileLesen();
            log.Zeile(profile.Count + " Projekte in Tab_Projekt.");

            List<Tuple<Projektprofil, string>> auswahl;
            string vorgabe = Argument(args, "--projekte");
            if (vorgabe != null)
            {
                var ids = vorgabe.Split(',')
                                 .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                                 .ToList();
                auswahl = profile.Where(p => ids.Contains(p.ID))
                                 .Select(p => Tuple.Create(p, "per --projekte vorgegeben"))
                                 .ToList();

                foreach (int id in ids)
                    if (!profile.Any(p => p.ID == id))
                        log.Warnung("Projekt " + id + " steht nicht in Tab_Projekt.");
            }
            else
            {
                auswahl = Projektauswahl.Waehlen(profile, log);
            }

            if (auswahl.Count == 0)
            {
                log.FehlerZeile("Kein simulierbares Projekt gefunden - Abbruch.");
                return 2;
            }

            log.Leerzeile();
            log.Zeile("Gewaehlte Referenzprojekte (" + auswahl.Count + "):");
            foreach (var a in auswahl)
            {
                log.Roh("  - Projekt " + a.Item1.ID + " \"" + a.Item1.Name + "\"");
                log.Roh("      Ausstattung: " + a.Item1.Ausstattung);
                log.Roh("      Grund:       " + a.Item2);
            }
            log.Leerzeile();

            // --- 4. Laeufe - IN DIESEM Prozess -------------------------------------------
            Directory.CreateDirectory(zielWurzel);
            int erfolge = 0;

            foreach (var a in auswahl)
            {
                string ziel = Path.Combine(zielWurzel, "Projekt_" + a.Item1.ID);
                log.Zeile("Projekt " + a.Item1.ID + " \"" + a.Item1.Name + "\" wird gerechnet ...");

                var t0 = DateTime.Now;
                int dateien;
                try
                {
                    dateien = Ergebnisexport.ProjektAusfuehren(a.Item1.ID, ziel, log);
                }
                catch (Exception ex)
                {
                    log.FehlerZeile("Projekt " + a.Item1.ID + ": " + ex.Message);
                    dateien = 0;
                }

                var dauer = DateTime.Now - t0;
                if (dateien > 0)
                {
                    erfolge++;
                    log.Zeile("  OK - " + dateien + " Dateien in " + dauer.ToString(@"mm\:ss"));
                }
                else
                {
                    log.FehlerZeile("  FEHLGESCHLAGEN nach " + dauer.ToString(@"mm\:ss"));
                }
            }

            // --- 5. Protokoll ------------------------------------------------------------
            var gesamt = DateTime.Now - start;
            log.Leerzeile();
            log.Zeile("Fertig. Gesamtdauer " + gesamt.ToString(@"hh\:mm\:ss"));
            log.Zeile("Erfolgreich: " + erfolge + " von " + auswahl.Count);

            log.Speichern(Path.Combine(zielWurzel, "protokoll.txt"),
                          "Referenzlauf (Kern) vom " + start.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
                          new[]
                          {
                              "Quelle:        " + quelle,
                              "Arbeitskopie:  " + DbUmgebung.ArbeitskopieDatei(arbeitskopieOrdner),
                              "Kultur:        " + CultureInfo.CurrentCulture.Name,
                              "Zielordner:    " + zielWurzel,
                              "Projekte:      " + string.Join(", ", auswahl.Select(a => a.Item1.ID.ToString(CultureInfo.InvariantCulture))),
                              "Dauer:         " + gesamt.ToString(@"hh\:mm\:ss")
                          });

            return erfolge == auswahl.Count ? 0 : 1;
        }

        // =================================================================================
        // Modus "projekt" - EIN Projekt auf der bereits umgebogenen Arbeitskopie
        // =================================================================================

        private static int ModusProjekt(string idText, string zielOrdner)
        {
            var log = new Protokoll();
            int id = int.Parse(idText.Trim(), CultureInfo.InvariantCulture);

            string basis = Path.Combine(ProjektWurzelFinden(), ORDNER_REFERENZLAEUFE);
            string arbeitskopieOrdner = Path.Combine(basis, ORDNER_ARBEITSKOPIE);

            if (!File.Exists(DbUmgebung.ArbeitskopieDatei(arbeitskopieOrdner)))
            {
                log.FehlerZeile("Keine Arbeitskopie unter " + arbeitskopieOrdner +
                                " - zuerst \"lauf --quelle <sqlite>\" ausfuehren.");
                return 2;
            }

            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(arbeitskopieOrdner, log);
            int dateien = Ergebnisexport.ProjektAusfuehren(id, zielOrdner, log);
            return dateien > 0 ? 0 : 1;
        }

        // =================================================================================
        // Kleinigkeiten
        // =================================================================================

        private static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        /// <summary>
        /// Der Ordner, in dem <c>Referenzlaeufe\</c> liegt: vom Programmverzeichnis aus
        /// nach oben, bis der Ordner auftaucht; ersatzweise das aktuelle Verzeichnis.
        /// </summary>
        private static string ProjektWurzelFinden()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (Directory.Exists(Path.Combine(d.FullName, ORDNER_REFERENZLAEUFE)))
                    return d.FullName;
                d = d.Parent;
            }

            d = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (d != null)
            {
                if (Directory.Exists(Path.Combine(d.FullName, ORDNER_REFERENZLAEUFE)))
                    return d.FullName;
                d = d.Parent;
            }

            return Directory.GetCurrentDirectory();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Referenzlauf-Suite (Paket B1, Kapitel 9 des Simulationskonzepts).
    ///
    /// Modi:
    ///   lauf       [--ziel &lt;ordner&gt;] [--projekte 1007,1009] [--timeout &lt;sek&gt;] [--quelle &lt;db&gt;]
    ///   vergleich  &lt;refOrdner&gt; &lt;neuOrdner&gt; [--ohne &lt;schluessel,schluessel&gt;]
    ///   pruefen    &lt;ordner&gt;
    ///   liste      [&lt;dbOrdner&gt;]
    ///   migration  &lt;quellDb&gt; &lt;zielOrdner&gt; [--nokopie] [--schreibschutz]
    ///   projekt    &lt;id&gt; &lt;zielordner&gt; &lt;dbordner&gt;     (intern: ein Projekt im Kindprozess)
    ///
    /// Jedes Projekt laeuft in einem EIGENEN Kindprozess. Das ist der einzige zuverlaessige
    /// Weg, einen haengenden Lauf (Endlosschleife oder eine nicht wegklickbare MessageBox
    /// der Engine) nach Ablauf des Timeouts abzubrechen, ohne die restlichen Projekte zu
    /// verlieren.
    /// </summary>
    internal static class Program
    {
        private const string ORDNER_REFERENZLAEUFE = "Referenzlaeufe";
        private const string ORDNER_ARBEITSKOPIE = "Arbeitskopie";
        private const string FALLBACK_WURZEL = @"C:\Waermeplan\WP_Plan";
        private const int TIMEOUT_STANDARD_SEKUNDEN = 300;

        [STAThread]
        private static int Main(string[] args)
        {
            // Die Kultur bleibt bewusst unveraendert (de-DE): der Rechenkern soll sich exakt
            // so verhalten wie beim Lauf aus der Anwendung heraus. Nur die CSV-Ausgabe
            // formatiert explizit invariant.
            // Der Elternprozess liest die Ausgabe der Kindprozesse als UTF-8. Ohne diese
            // Zeile schreibt die Konsole in der OEM-Codepage und Umlaute kommen als
            // Ersatzzeichen im Protokoll an.
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            OberflaechenspracheSetzen();

            // DIE WERKZEUG-FREIGABE DER SCHREIBNAHT (Welle iF30) - EINE benannte Zeile,
            // ausdruecklich und nicht durch Auslassen. Die Suite laeuft ohne Lizenz und
            // SCHREIBT (SimuliereUndSpeichere legt je Projekt einen Ergebniskopf an, der
            // Migrationsmodus hebt eine Datenbank). Ohne sie fiele der Rechennachweis rot
            // aus - aus einem Grund, der mit dem Rechenweg nichts zu tun hat.
            Schreibnaht.WerkzeugFreigabe("Referenzlauf-Suite (Rechennachweis ohne Lizenz)");
            Console.WriteLine("Schreibnaht: freigegeben für " + Schreibnaht.WerkzeugGrund);

            if (args.Length == 0) { Hilfe(); return 2; }

            try
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "lauf": return ModusLauf(args.Skip(1).ToArray());
                    case "projekt": return ModusProjekt(args.Skip(1).ToArray());
                    case "vergleich":
                        if (args.Length < 3) { Hilfe(); return 2; }
                        // --ohne <a,b,c>: benannte Schluessel vom Vergleich ausnehmen
                        // (Etappe D4 - eine neue Ergebnisspalte erweitert aggregate.csv;
                        //  siehe Vergleich._ausgenommen).
                        return Vergleich.Ausfuehren(args[1], args[2],
                                                    (Argument(args, "--ohne") ?? "").Split(','));
                    case "pruefen":
                        if (args.Length < 2) { Hilfe(); return 2; }
                        return Plausibilitaet.Pruefen(args[1]);
                    case "liste": return ModusListe(args.Skip(1).ToArray());
                    case "migration": return Migrationslauf.Ausfuehren(args.Skip(1).ToArray());
                    default: Hilfe(); return 2;
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
        /// Setzt die ANZEIGESPRACHE des Laufs aus der Umgebungsvariablen
        /// <c>EPOS_REFLAUF_UICULTURE</c> (Paket 9, Sprachgleichheitsprobe / L7-Vorstufe).
        ///
        /// <para>
        /// Ohne die Variable aendert sich nichts — dann gilt die Systemkultur, genau wie
        /// bisher. Mit <c>EPOS_REFLAUF_UICULTURE=en-US</c> rechnet dieselbe Suite mit
        /// englischer Oberflaechensprache; die Ergebnisdateien muessen byte-identisch
        /// bleiben. Genau das weist nach, dass kein lokalisierter Text als Steuerwert
        /// dient (Drei-Schichten-Regel).
        /// </para>
        ///
        /// <para>
        /// <b>Nur CurrentUICulture.</b> <c>CurrentCulture</c> (Zahlen- und Datumsformat)
        /// bleibt unangetastet — sie zu aendern waere eine Rechenaenderung und ist laut
        /// Konzept 13.6 ausdruecklich nicht Teil des Lokalisierungspakets.
        /// </para>
        ///
        /// <para>
        /// <b>Warum eine Umgebungsvariable und kein Argument.</b> Jedes Projekt rechnet in
        /// einem eigenen Kindprozess (siehe <c>ProjektImKindprozess</c>). Die Umgebung
        /// wird vererbt, ein Argument muesste durchgereicht werden — eine Stelle statt
        /// zweier, und die Registry des Anwenders wird nicht angefasst.
        /// </para>
        /// </summary>
        private static void OberflaechenspracheSetzen()
        {
            string sprache = Environment.GetEnvironmentVariable("EPOS_REFLAUF_UICULTURE");
            if (string.IsNullOrWhiteSpace(sprache)) return;

            try
            {
                CultureInfo kultur = CultureInfo.GetCultureInfo(sprache.Trim());
                System.Threading.Thread.CurrentThread.CurrentUICulture = kultur;
                CultureInfo.DefaultThreadCurrentUICulture = kultur;
                Console.WriteLine("Oberflaechensprache (nur Anzeige): " + kultur.Name);
            }
            catch (CultureNotFoundException)
            {
                Console.WriteLine("Unbekannte Oberflaechensprache '" + sprache + "' - Systemkultur bleibt.");
            }
        }

        private static void Hilfe()
        {
            Console.WriteLine("Referenzlauf-Suite EPOS-Plan (Paket B1)");
            Console.WriteLine();
            Console.WriteLine("  Referenzlauf.exe lauf [--ziel <ordner>] [--projekte 1007,1009] [--timeout <sek>] [--quelle <db>]");
            Console.WriteLine("  Referenzlauf.exe vergleich <refOrdner> <neuOrdner> [--ohne <schluessel,schluessel>]");
            Console.WriteLine("  Referenzlauf.exe pruefen <ordner>");
            Console.WriteLine("  Referenzlauf.exe liste [<dbOrdner>]");
            Console.WriteLine("  Referenzlauf.exe migration <quellDb> <zielOrdner> [--nokopie] [--schreibschutz]");
        }

        // =================================================================================
        // Modus "lauf" - Orchestrierung
        // =================================================================================

        private static int ModusLauf(string[] args)
        {
            var log = new Protokoll();
            var start = DateTime.Now;

            string wurzel = ProjektWurzelFinden();
            string basis = Path.Combine(wurzel, ORDNER_REFERENZLAEUFE);
            string zielWurzel = Argument(args, "--ziel") ??
                                Path.Combine(basis, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "_B0");
            string arbeitskopieOrdner = Path.Combine(basis, ORDNER_ARBEITSKOPIE);

            int timeoutSekunden = TIMEOUT_STANDARD_SEKUNDEN;
            string tArg = Argument(args, "--timeout");
            if (tArg != null) int.TryParse(tArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeoutSekunden);

            log.Zeile("Referenzlauf gestartet.");
            log.Zeile("Projektwurzel: " + wurzel);
            log.Zeile("Zielordner:    " + zielWurzel);
            log.Zeile("Timeout je Projekt: " + timeoutSekunden + " s");
            log.Leerzeile();

            // --- 1. Arbeitskopie ---------------------------------------------------------
            // --quelle richtet den Lauf auf eine ausdruecklich benannte Datenbank statt auf
            // die produktive Ablage. Die Endung entscheidet ueber den Zweig: .accdb oder
            // .sqlite (Paket S7, Verhaltensbeweis auf EINEM eingefrorenen Datenstand).
            string quelle = DbUmgebung.ProduktivQuelleFinden(log, Argument(args, "--quelle"));
            if (quelle == null)
            {
                log.FehlerZeile("Keine Datenbank gefunden - Abbruch.");
                return 2;
            }
            DbUmgebung.ArbeitskopieAnlegen(quelle, arbeitskopieOrdner, log);

            // --- 2. DB-Pfad umbiegen und hart pruefen -----------------------------------
            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(arbeitskopieOrdner, log);
            log.Leerzeile();

            // --- 2b. Arbeitskopie migrieren ---------------------------------------------
            // Ohne diesen Schritt rechnete "lauf" auf einer Kopie im Stand der Quelle:
            // fehlende Spalten und eine fehlende Tab_ErgebnisPufferspeicher wurden dann
            // nur von den Rueckfallebenen im Anwendungscode notduerftig ausgeglichen -
            // das Ergebnis war nicht mit einem Lauf auf einer migrierten Datenbank
            // vergleichbar. Die Migration ist idempotent; auf einer bereits aktuellen
            // Kopie ist sie ein No-op.
            MigrationAusfuehren(log);
            log.Leerzeile();

            // --- 3. Projektauswahl -------------------------------------------------------
            using (new DialogWaechter())
            {
                log.Zeile("Projektlandschaft wird gelesen ...");
                var profile = Projektauswahl.ProfileLesen();
                log.Zeile(profile.Count + " Projekte in Tab_Projekt.");

                List<Tuple<Projektprofil, string>> auswahl;
                string vorgabe = Argument(args, "--projekte");
                if (vorgabe != null)
                {
                    var ids = vorgabe.Split(',').Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToList();
                    auswahl = profile.Where(p => ids.Contains(p.ID))
                                     .Select(p => Tuple.Create(p, "per --projekte vorgegeben"))
                                     .ToList();
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

                // --- 4. Laeufe ------------------------------------------------------------
                Directory.CreateDirectory(zielWurzel);
                var ergebnisse = new List<Laufergebnis>();

                foreach (var a in auswahl)
                {
                    ergebnisse.Add(ProjektImKindprozess(a.Item1, zielWurzel, arbeitskopieOrdner,
                                                        timeoutSekunden, log));
                }

                // --- 5. Protokoll ---------------------------------------------------------
                var dauer = DateTime.Now - start;
                log.Leerzeile();
                log.Zeile("Fertig. Gesamtdauer " + dauer.ToString(@"hh\:mm\:ss"));
                log.Zeile("Erfolgreich: " + ergebnisse.Count(e => e.Erfolg) + " von " + ergebnisse.Count);

                ProtokollSchreiben(log, zielWurzel, quelle, arbeitskopieOrdner, auswahl, ergebnisse, dauer, timeoutSekunden);

                return ergebnisse.All(e => e.Erfolg) ? 0 : 1;
            }
        }

        /// <summary>
        /// Bringt die Arbeitskopie auf den Zielstand des Schemas. Der DB-Pfad muss
        /// vorher umgebogen und geprueft sein - SchemaMigration arbeitet auf der
        /// Verbindung der Anwendung.
        /// </summary>
        private static void MigrationAusfuehren(Protokoll log)
        {
            log.Zeile("Schema-Migration der Arbeitskopie ...");
            try
            {
                string bericht;
                bool ok;
                using (new DialogWaechter())
                {
                    ok = SchemaMigration.Ausfuehren(out bericht);
                }

                foreach (string z in (bericht ?? "").Replace("\r\n", "\n").Split('\n'))
                    if (z.Trim().Length > 0) log.Roh("  " + z);

                if (ok) log.Zeile("Migration: ERFOLG (Zielstand " + SchemaMigration.ZIEL_VERSION + ").");
                else log.Warnung("Migration FEHLGESCHLAGEN - der Lauf rechnet auf einem unvollstaendigen Schema.");
            }
            catch (Exception ex)
            {
                log.Warnung("Migration nicht ausfuehrbar: " + ex.Message);
            }
        }

        private sealed class Laufergebnis
        {
            public int ID;
            public string Name;
            public bool Erfolg;
            public string Anmerkung = "";
            public TimeSpan Dauer;
            public int Dateien;

            /// <summary>Vom Dialogwaechter automatisch beantwortete MessageBoxen der Engine.</summary>
            public readonly List<string> Dialoge = new List<string>();
        }

        private static Laufergebnis ProjektImKindprozess(Projektprofil profil, string zielWurzel,
                                                         string dbOrdner, int timeoutSekunden,
                                                         Protokoll log)
        {
            var erg = new Laufergebnis { ID = profil.ID, Name = profil.Name };
            string zielOrdner = Path.Combine(zielWurzel, "Projekt_" + profil.ID);
            var start = DateTime.Now;

            log.Zeile("--- Projekt " + profil.ID + " (" + profil.Name + ") ---");

            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.ArgumentList.Add("projekt");
            psi.ArgumentList.Add(profil.ID.ToString(CultureInfo.InvariantCulture));
            psi.ArgumentList.Add(zielOrdner);
            psi.ArgumentList.Add(dbOrdner);

            var ausgabe = new List<string>();
            using (var p = new Process { StartInfo = psi })
            {
                p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (ausgabe) ausgabe.Add(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (ausgabe) ausgabe.Add("stderr: " + e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(timeoutSekunden * 1000))
                {
                    try { p.Kill(true); } catch { }
                    try { p.WaitForExit(5000); } catch { }
                    erg.Erfolg = false;
                    erg.Anmerkung = "UEBERSPRUNGEN - Timeout nach " + timeoutSekunden +
                                    " s (haengender Lauf oder nicht schliessbarer Dialog).";
                    erg.Dauer = DateTime.Now - start;
                    AusgabeUebernehmen(log, ausgabe, erg);
                    log.Warnung("Projekt " + profil.ID + ": " + erg.Anmerkung);
                    // Halbfertige Ausgabe entfernen, damit sie nicht als Referenz gilt.
                    try { if (Directory.Exists(zielOrdner)) Directory.Delete(zielOrdner, true); } catch { }
                    return erg;
                }

                erg.Dauer = DateTime.Now - start;
                AusgabeUebernehmen(log, ausgabe, erg);

                if (p.ExitCode != 0)
                {
                    erg.Erfolg = false;
                    erg.Anmerkung = "UEBERSPRUNGEN - Kindprozess endete mit Code " + p.ExitCode + ".";
                    log.Warnung("Projekt " + profil.ID + ": " + erg.Anmerkung);
                    try { if (Directory.Exists(zielOrdner)) Directory.Delete(zielOrdner, true); } catch { }
                    return erg;
                }
            }

            erg.Erfolg = true;
            erg.Dateien = Directory.Exists(zielOrdner) ? Directory.GetFiles(zielOrdner, "*.csv").Length : 0;
            erg.Anmerkung = "OK";
            log.Zeile("Projekt " + profil.ID + ": OK, " + erg.Dateien + " CSV-Dateien, " +
                      erg.Dauer.ToString(@"mm\:ss"));
            return erg;
        }

        private const string MARKER_DIALOG = "MessageBox der Anwendung automatisch geschlossen: ";

        private static void AusgabeUebernehmen(Protokoll log, List<string> ausgabe, Laufergebnis erg)
        {
            lock (ausgabe)
                foreach (string z in ausgabe)
                {
                    log.AusKindprozess(z);

                    int pos = z.IndexOf(MARKER_DIALOG, StringComparison.Ordinal);
                    if (pos >= 0) erg.Dialoge.Add(z.Substring(pos + MARKER_DIALOG.Length));
                }
        }

        // =================================================================================
        // Modus "projekt" - ein Projekt, im Kindprozess
        // =================================================================================

        private static int ModusProjekt(string[] args)
        {
            if (args.Length < 3) { Hilfe(); return 2; }

            int idProjekt = int.Parse(args[0], CultureInfo.InvariantCulture);
            string zielOrdner = args[1];
            string dbOrdner = args[2];

            var log = new Protokoll();

            // Sicherheitsnetz: der Kindprozess arbeitet ausschliesslich auf der Arbeitskopie.
            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(dbOrdner, log);

            using (var waechter = new DialogWaechter())
            {
                int dateien = Ergebnisexport.ProjektAusfuehren(idProjekt, zielOrdner, log);

                string[] dialoge = waechter.GeschlosseneDialoge;
                foreach (string d in dialoge)
                    log.Warnung("MessageBox der Anwendung automatisch geschlossen: " + d);

                return dateien > 0 ? 0 : 3;
            }
        }

        // =================================================================================
        // Modus "liste"
        // =================================================================================

        /// <summary>
        /// Zeigt Projektlandschaft und Auswahl. Ohne Argument wird dafuer die
        /// Arbeitskopie neu aus der produktiven Datenbank angelegt; mit einem
        /// Ordnerargument wird eine VORHANDENE Kopie benutzt und nichts kopiert -
        /// so laesst sich die Auswahl auf einer eigenen, migrierten Kopie ausserhalb
        /// des Repos nachpruefen, ohne die Arbeitskopie eines laufenden Vergleichs
        /// zu ueberschreiben.
        /// </summary>
        private static int ModusListe(string[] args)
        {
            var log = new Protokoll();
            string wurzel = ProjektWurzelFinden();
            string arbeitskopieOrdner = (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
                ? Path.GetFullPath(args[0])
                : null;

            if (arbeitskopieOrdner == null)
            {
                arbeitskopieOrdner = Path.Combine(wurzel, ORDNER_REFERENZLAEUFE, ORDNER_ARBEITSKOPIE);
                string quelle = DbUmgebung.ProduktivQuelleFinden(log, Argument(args, "--quelle"));
                if (quelle == null) return 2;
                DbUmgebung.ArbeitskopieAnlegen(quelle, arbeitskopieOrdner, log);
            }
            else
            {
                log.Zeile("Vorhandene Kopie wird gelesen: " + arbeitskopieOrdner);
            }

            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(arbeitskopieOrdner, log);

            using (new DialogWaechter())
            {
                var profile = Projektauswahl.ProfileLesen();
                log.Leerzeile();
                foreach (var p in profile)
                    log.Roh(string.Format(CultureInfo.InvariantCulture,
                        "{0,-6} {1,-32} {2}", p.ID, p.Name, p.Ausstattung));

                log.Leerzeile();
                log.Zeile("Automatische Auswahl:");
                foreach (var a in Projektauswahl.Waehlen(profile, log))
                    log.Roh("  " + a.Item1.ID + " - " + a.Item2);
            }
            return 0;
        }

        // =================================================================================
        // Hilfsfunktionen
        // =================================================================================

        /// <summary>Sucht vom Programmverzeichnis aufwaerts das Verzeichnis mit WP-Plan.sln.</summary>
        private static string ProjektWurzelFinden()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "WP-Plan.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return FALLBACK_WURZEL;
        }

        private static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        private static void ProtokollSchreiben(Protokoll log, string zielWurzel, string quelle,
                                               string arbeitskopie,
                                               List<Tuple<Projektprofil, string>> auswahl,
                                               List<Laufergebnis> ergebnisse, TimeSpan dauer,
                                               int timeoutSekunden)
        {
            var kopf = new List<string>();
            kopf.Add("**Zeitpunkt:** " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));
            kopf.Add("");
            kopf.Add("**Quelle (produktiv, nur gelesen):** `" + quelle + "`");
            kopf.Add("");
            kopf.Add("**Arbeitskopie (beschrieben):** `" + DbUmgebung.ArbeitskopieDatei(arbeitskopie) + "`");
            kopf.Add("");
            kopf.Add("**Zielordner:** `" + zielWurzel + "`");
            kopf.Add("");
            kopf.Add("**Gesamtdauer:** " + dauer.ToString(@"hh\:mm\:ss") +
                     "  |  **Timeout je Projekt:** " + timeoutSekunden + " s");
            kopf.Add("");
            kopf.Add("**Warnungen:** " + log.Warnungen + "  |  **Fehler:** " + log.Fehler);
            kopf.Add("");
            kopf.Add("## Projekte");
            kopf.Add("");
            kopf.Add("| ID | Projekt | Ausstattung | Auswahlgrund | Dauer | CSV | Status |");
            kopf.Add("|---|---|---|---|---|---|---|");

            foreach (var a in auswahl)
            {
                var e = ergebnisse.FirstOrDefault(x => x.ID == a.Item1.ID);
                kopf.Add("| " + a.Item1.ID +
                         " | " + a.Item1.Name +
                         " | " + Zelle(a.Item1.Ausstattung) +
                         " | " + Zelle(a.Item2) +
                         " | " + (e == null ? "-" : e.Dauer.ToString(@"mm\:ss")) +
                         " | " + (e == null ? "-" : e.Dateien.ToString(CultureInfo.InvariantCulture)) +
                         " | " + (e == null ? "nicht ausgefuehrt" : Zelle(e.Anmerkung)) + " |");
            }

            var mitDialogen = ergebnisse.Where(e => e.Dialoge.Count > 0).ToList();
            if (mitDialogen.Count > 0)
            {
                kopf.Add("");
                kopf.Add("## Automatisch beantwortete Dialoge");
                kopf.Add("");
                kopf.Add("Die Engine stellt im Grenzfall Rueckfragen per MessageBox. Der Dialogwaechter");
                kopf.Add("drueckt den bejahenden Knopf, damit der Lauf denselben Weg geht wie bei einem");
                kopf.Add("Anwender. Jede Rueckfrage ist hier dokumentiert:");
                kopf.Add("");
                foreach (var e in mitDialogen)
                    foreach (string d in e.Dialoge)
                        kopf.Add("- Projekt " + e.ID + ": " + d);
            }

            log.Speichern(Path.Combine(zielWurzel, "lauf_protokoll.md"),
                          "Referenzlauf-Protokoll", kopf);
        }

        /// <summary>Macht einen Text tabellentauglich - ein rohes "|" wuerde die Spalte brechen.</summary>
        private static string Zelle(string text)
        {
            return (text ?? "").Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WindowsFormsApplication1;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Erzeugt den AUSLIEFERUNGSSTAND der Datenbank: eine Kopie der produktiven
    /// <c>Kenndaten.sqlite</c>, aus der jedes Projekt entfernt ist, deren Kataloge auf den
    /// Auslieferungsstand gebracht sind und in die anschliessend die Beispielprojekte
    /// eingespielt werden.
    ///
    /// <para><b>Warum als Werkzeug und nicht von Hand.</b>
    /// <c>Dokumentation/aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md</c> 6.1 beschrieb bis hierher vier
    /// HANDgriffe und vermerkte „noch nicht automatisiert"; offener Punkt S5 desselben
    /// Dokuments. Anwenderentscheid <b>#157-E-2</b> (09.09.2026, „Empfehlung"
    /// angenommen): automatisieren, und die Vorlage enthaelt Beispielprojekte. Der Grund
    /// ist nicht Bequemlichkeit: Die Entwicklungsdatenbank enthaelt reale Kunden- und
    /// Objektdaten, ein vergessenes DELETE ist eine Datenpanne, und Handarbeit laesst sich
    /// nicht gegenpruefen. Hier entsteht neben der Datei ein Prueflauf, den man liest,
    /// bevor man sie in ein Setup packt.</para>
    ///
    /// <para><b>Der Rahmen (Anwender, 09.09.2026):</b> Access wurde nie produktiv
    /// eingesetzt; die Vorlage ist eine <c>.sqlite</c>-Datei. Entscheid <b>#157-E-1</b>
    /// ist inzwischen auf <b>W3</b> gefallen — Vorlage als <c>.sqlite</c>, der Access-Weg
    /// faellt. Das Werkzeug kennt <c>.accdb</c> deshalb gar nicht.</para>
    ///
    /// <para><b>Die Quelle wird nie angefasst.</b> Nicht einmal lesend ueber die
    /// Zugriffsschicht: Ein Oeffnen im WAL-Modus legte <c>-wal</c> und <c>-shm</c> neben
    /// die produktive Datei. Der einzige Zugriff ist
    /// <see cref="Datenbanksicherung"/><c>.KopieAnlegen</c> — die EINE Sicherungswahrheit
    /// des Kerns seit Auftrag #158, ein <c>VACUUM INTO</c> ueber eine frisch geoeffnete
    /// Verbindung. Warum nicht <c>File.Copy</c>: Im WAL-Modus ist der aktuelle Datenstand
    /// die SUMME aus Hauptdatei und <c>-wal</c> (BETRIEB_SQLITE.md § 2); eine Bytekopie
    /// der Hauptdatei greift nur den letzten Checkpoint ab und ist bei geoeffneter
    /// Datenbank im schlimmsten Fall mitten im Schreiben entstanden. <c>VACUUM INTO</c>
    /// liest durch das WAL hindurch, schreibt eine in sich geschlossene Datei und laesst
    /// die Quelle byte-gleich — auch dann, wenn EPOS-Plan gerade laeuft.</para>
    ///
    /// <para><b>Rueckgabe.</b> 0 = erzeugt und abgenommen; alles andere ist ein Abbruch
    /// mit Grund auf stderr (2 Aufruf, 3 Schreibort, 4 Katalogwaechter, 5 fachlich,
    /// 1 unerwartet). Die Zieldatei entsteht ausschliesslich bei 0 — ein Abbruch laesst
    /// keine halbe Vorlage liegen, die beim naechsten Setup-Lauf als gueltig durchginge.</para>
    /// </summary>
    internal static class Program
    {
        private const int OK = 0;
        private const int UNERWARTET = 1;
        private const int AUFRUF = 2;
        private const int SCHREIBORT = 3;
        private const int KATALOGWAECHTER = 4;
        private const int FACHLICH = 5;

        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--hilfe" || args[0] == "-h" || args[0] == "--help")
            {
                Argumente.HilfeAusgeben();
                return AUFRUF;
            }

            Argumente arg = Argumente.Lesen(args);
            if (arg.Fehler != null)
            {
                Console.Error.WriteLine("Abbruch: " + arg.Fehler);
                Console.Error.WriteLine("Aufrufhilfe: --hilfe");
                return AUFRUF;
            }

            if (!arg.Trocken)
            {
                string verweigert = Schreibort.Pruefen(arg.Ziel);
                if (verweigert != null)
                {
                    Console.Error.WriteLine("Abbruch: " + verweigert);
                    return SCHREIBORT;
                }
            }

            string arbeitsordner = Path.Combine(Path.GetTempPath(),
                "auslieferungsvorlage-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            string kopie = null;

            try
            {
                return Ausfuehren(arg, arbeitsordner, ref kopie);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Abbruch: " + ex.GetType().Name + " — " + ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return UNERWARTET;
            }
            finally
            {
                Aufraeumen(arbeitsordner);
            }
        }

        private static int Ausfuehren(Argumente arg, string arbeitsordner, ref string kopie)
        {
            var bericht = new Bericht(aufKonsole: true);
            bericht.Zeile("Auslieferungsvorlage — Auslieferungsstand der Datenbank");
            bericht.Zeile("erzeugt am  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            bericht.Zeile("Quelle      " + arg.Quelle + "   (" + Vorlagenbau.Mb(new FileInfo(arg.Quelle).Length) + ")");
            bericht.Zeile("Ziel        " + (arg.Trocken ? "(--trocken: keine Datei)" : arg.Ziel));
            bericht.Zeile("Beispiele   " + (arg.Beispiele.Count == 0 ? "keine" : arg.Beispiele.Count + " Paket(e)"));
            foreach (string b in arg.Beispiele) bericht.Zeile("            " + b);

            // ---- Schritt 1: Arbeitskopie ------------------------------------------
            bericht.Abschnitt("Schritt 1 — Arbeitskopie");
            long quelleVorher = new FileInfo(arg.Quelle).Length;
            DateTime quelleZeit = File.GetLastWriteTimeUtc(arg.Quelle);

            kopie = Datenbanksicherung.KopieAnlegen(arg.Quelle, arbeitsordner, "Auslieferungsvorlage");
            bericht.Zeile("VACUUM INTO -> " + kopie);
            bericht.Zeile("Groesse " + Vorlagenbau.Mb(quelleVorher) + " -> " + Vorlagenbau.Mb(new FileInfo(kopie).Length));
            bericht.Zeile("Die Quelle wurde nur gelesen; sie bleibt byte-gleich.");

            // Ab hier arbeitet der ganze Kern auf der Kopie.
            DataRepository.PfadUeberschreibung = kopie;

            // DIE WERKZEUG-FREIGABE DER SCHREIBNAHT (Welle iF30) — EINE benannte Zeile,
            // ausdruecklich und nicht durch Auslassen. Dieses Werkzeug loescht, spielt ein
            // und verdichtet; eine Lizenz hat es nicht und braucht es nicht.
            Schreibnaht.WerkzeugFreigabe("Werkzeug Auslieferungsvorlage");

            // Die Meldungen des Kerns (Import, Datenbankfehler) gehen sonst still auf die
            // Konsole und fehlen im Prueflauf — hier werden sie mitgeschrieben.
            var mitschrieb = new MitschriebDialoge();
            Dienste.Dialog = mitschrieb;

            Projektsicht sicht = Projektsicht.Lesen();
            int strictVorher = Prueflauf.StrictTabellenDerKopie();

            // ---- Schritte 2 bis 4 --------------------------------------------------
            var bau = new Vorlagenbau(arg, bericht);
            bau.ProjektdatenEntfernen(sicht);
            bau.KatalogeBereinigen(sicht);

            if (bau.GeleerteKataloge.Count > 0 && !arg.KatalogleerungZulassen && !arg.KatalogeVollstaendig)
            {
                Console.Error.WriteLine("Abbruch: Die ReadOnly-Regel leert " + bau.GeleerteKataloge.Count +
                                        " Katalogtabelle(n) vollstaendig (" +
                                        string.Join(", ", bau.GeleerteKataloge) + "). Eine Vorlage mit leerem " +
                                        "Katalog ist unbrauchbar und faellt erst beim Kunden auf. Entweder die " +
                                        "Marke ReadOnly im Bestand pflegen, oder --kataloge weglassen (Vorgabe " +
                                        "seit #160-E-1a: alle), oder --katalogleerung-zulassen.");
                return KATALOGWAECHTER;
            }

            bau.PersonenbezugAbraeumen(sicht);

            if (!bau.BeispieleEinspielen(arg.Beispiele, out string fehler))
            {
                Console.Error.WriteLine("Abbruch: " + fehler);
                return FACHLICH;
            }

            // ---- Schritt 5: verdichten und pruefen ---------------------------------
            bau.Verdichten();

            var pruefung = new Prueflauf(bericht, sicht);
            bool abgenommen = pruefung.Ausfuehren(strictVorher);

            // Erst JETZT die Verbindungen schliessen: Solange die Zugriffsschicht die
            // Datei im WAL-Modus offen haelt, liegen -wal und -shm daneben; die Frage
            // „nur EINE Datei?" ist erst danach zu beantworten.
            Verbindungenschliessen();
            abgenommen &= pruefung.Dateipruefung(kopie);

            if (mitschrieb.Meldungen.Count > 0)
            {
                bericht.Abschnitt("Meldungen des Kerns");
                foreach (string m in mitschrieb.Meldungen) bericht.Zeile(m);
            }

            // Gegenprobe: die Quelle darf sich nicht geruehrt haben.
            bericht.Abschnitt("Quelle");
            bool quelleUnberuehrt = new FileInfo(arg.Quelle).Length == quelleVorher &&
                                    File.GetLastWriteTimeUtc(arg.Quelle) == quelleZeit;
            bericht.Zeile((quelleUnberuehrt ? "ok      " : "FEHLER  ") + arg.Quelle +
                          " — Groesse und Aenderungszeitpunkt unveraendert");
            foreach (string bei in new[] { arg.Quelle + "-wal", arg.Quelle + "-shm" })
                if (File.Exists(bei)) bericht.Zeile("WARNUNG neben der Quelle liegt " + Path.GetFileName(bei));
            abgenommen &= quelleUnberuehrt;

            if (!abgenommen)
            {
                Console.Error.WriteLine("Abbruch: Die Abnahme der erzeugten Vorlage ist rot — siehe Abschnitt " +
                                        "„Prüfung“. Es wurde keine Zieldatei geschrieben.");
                return FACHLICH;
            }

            // ---- Ergebnis ablegen ---------------------------------------------------
            bericht.Abschnitt("Ergebnis");
            // Die eine Zeile, die vor der Auslieferung zaehlt: Steht hier etwas anderes als
            // 0, ist im Bericht oben eine WARNUNG- oder FEHLER-Zeile zu lesen und zu erklaeren.
            // Ueber eine KOPIE: Jede geschriebene Zeile, die mit WARNUNG oder FEHLER
            // beginnt, waechst die Liste - eine Aufzaehlung ueber das Original wuerde sich
            // selbst verlaengern.
            string[] auffaellig = bericht.Auffaelligkeiten.ToArray();
            bericht.Zeile("Auffaelligkeiten im Bericht: " + auffaellig.Length);
            foreach (string a in auffaellig)
                bericht.Zeile("    - " + (a.Length > 110 ? a.Substring(0, 110) + " …" : a));
            if (arg.Trocken)
            {
                bericht.Zeile("--trocken: die Zieldatei " + arg.Ziel + " wurde NICHT geschrieben.");
                Console.WriteLine();
                Console.WriteLine("Trockenlauf beendet — nichts geschrieben.");
                return OK;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(arg.Ziel));
            foreach (string alt in new[] { arg.Ziel, arg.Ziel + "-wal", arg.Ziel + "-shm" })
                if (File.Exists(alt)) File.Delete(alt);
            File.Move(kopie, arg.Ziel);
            kopie = null;

            string berichtsdatei = arg.Ziel + ".bericht.txt";
            bericht.Zeile("Vorlage:     " + arg.Ziel + "   (" + Vorlagenbau.Mb(new FileInfo(arg.Ziel).Length) + ")");
            bericht.Zeile("Prüfbericht: " + berichtsdatei);
            bericht.Speichern(berichtsdatei);

            Console.WriteLine();
            Console.WriteLine("Fertig. " + arg.Ziel);
            return OK;
        }

        /// <summary>
        /// Gibt die Arbeitskopie frei. <c>Microsoft.Data.Sqlite</c> haelt geschlossene
        /// Verbindungen im Pool offen; ohne dieses Leeren blieben <c>-wal</c> und
        /// <c>-shm</c> liegen und die Datei liesse sich nicht verschieben.
        /// </summary>
        private static void Verbindungenschliessen()
        {
            try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); }
            catch { /* Aufraeumen darf den Lauf nicht kippen */ }
        }

        private static void Aufraeumen(string arbeitsordner)
        {
            DataRepository.PfadUeberschreibung = null;
            Verbindungenschliessen();
            try { if (Directory.Exists(arbeitsordner)) Directory.Delete(arbeitsordner, true); }
            catch { /* der Ordner liegt im Temp-Bereich und stoert dort niemanden */ }
        }
    }

    /// <summary>
    /// Nimmt die Meldungen des Kerns auf, statt sie auf der Konsole verschwinden zu lassen.
    /// Der Import eines Projektpakets meldet ueber <c>Meldung.Zeigen</c>, warum er nicht
    /// weiterkam; im Prueflauf einer Auslieferungsdatei will man das lesen koennen.
    /// </summary>
    internal sealed class MitschriebDialoge : IDialogDienst
    {
        internal List<string> Meldungen { get; } = new List<string>();

        public void Meldung(string text, string titel = null) => Notieren("Meldung", text, titel);
        public void Warnung(string text, string titel = null) => Notieren("Warnung", text, titel);
        public void Fehler(string text, string titel = null) => Notieren("Fehler", text, titel);

        /// <summary>
        /// Eine Rueckfrage im Stapelbetrieb wird VERNEINT — dieselbe Regel wie in
        /// <see cref="StilleDialoge"/>: Ohne Bedienung gilt die Antwort mit dem kleineren
        /// Schaden, und „Nein" heisst, der angebotene Vorgang unterbleibt.
        /// </summary>
        public bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false)
        {
            Notieren("Frage (ohne Bedienung: nein)", text, titel);
            return false;
        }

        /// <inheritdoc/>
        public JaNeinAbbruch Wahl(string text, string titel = null)
        {
            Notieren("Wahl (ohne Bedienung: Abbruch)", text, titel);
            return JaNeinAbbruch.Abbruch;
        }

        /// <summary>Ohne Oberflaeche folgenlos.</summary>
        public void Warten(bool an) { }

        private void Notieren(string art, string text, string titel) =>
            Meldungen.Add(art + ": " + (string.IsNullOrEmpty(titel) ? "" : "[" + titel + "] ") + text);
    }
}

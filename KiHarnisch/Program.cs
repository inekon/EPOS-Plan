using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KiKern;
using WindowsFormsApplication1;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Aktionsharnisch des KI-Assistenten (Fachkonzept 8/Etappe 1, Abnahme).
    ///
    /// <para>
    /// Ruft JEDE registrierte Aktion mindestens einmal gegen eine ARBEITSKOPIE der
    /// produktiven Datenbank auf. Geprueft werden drei Zusagen:
    /// </para>
    /// <list type="number">
    /// <item><description>Die Werte sind plausibel (Projektzahl, Varianten, Ganglinien …).</description></item>
    /// <item><description>Je Ausfuehrung entsteht GENAU EINE Protokollzeile.</description></item>
    /// <item><description>Es erscheint kein Dialog - eine unerwartete MessageBox ist ein TESTFEHLER.</description></item>
    /// </list>
    /// <para>
    /// Zusaetzlich laeuft eine kleine Reihe von Gegenproben: unbekannte Aktion, fehlender
    /// Pflichtparameter, unbekannte ID. Auch sie muessen je eine Protokollzeile erzeugen
    /// und duerfen keine Ausnahme nach aussen lassen.
    /// </para>
    /// </summary>
    internal static class Program
    {
        private const string ORDNER_ARBEITSKOPIE = "Arbeitskopie_KI";
        private const string BEISPIELPROJEKT = "Beispiel WP WG 1";

        private static Protokoll _log;

        [STAThread]
        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            // Die Kultur bleibt unveraendert: der Harnisch soll sehen, was der Anwender sieht.
            _log = new Protokoll();

            string zielWurzel = Argument(args, "--ziel") ??
                Path.Combine(Path.GetTempPath(), "EPOS_KiHarnisch");

            // Der Rechtshinweis wird an echten Registry-Werten des angemeldeten Benutzers
            // geprueft. Sie werden hier gesichert und in JEDEM Fall wiederhergestellt.
            Einwilligung.Sichern(_log);

            try
            {
                return Lauf(zielWurzel);
            }
            catch (Exception ex)
            {
                _log.FehlerZeile("ABBRUCH: " + ex);
                Speichern(zielWurzel);
                return 2;
            }
            finally
            {
                Einwilligung.Wiederherstellen(_log);
            }
        }

        private static int Lauf(string zielWurzel)
        {
            _log.Zeile("Aktionsharnisch KI-Assistent (Etappen 1 bis 3)");
            _log.Zeile("Start: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            _log.Leerzeile();

            // ---------------------------------------------------------- Arbeitskopie
            string quelle = DbUmgebung.ProduktivQuelleFinden(_log);
            if (quelle == null) { _log.FehlerZeile("Keine Quelldatenbank gefunden."); Speichern(zielWurzel); return 2; }

            string hashVorher = Hash(quelle);
            _log.Zeile("SHA-256 der Produktiv-DB vorher: " + hashVorher);

            string kopieOrdner = Path.Combine(zielWurzel, ORDNER_ARBEITSKOPIE);
            DbUmgebung.ArbeitskopieAnlegen(quelle, kopieOrdner, _log);
            DbUmgebung.AufArbeitskopieUmschaltenUndPruefen(kopieOrdner, _log);

            // Protokolldatei liegt neben der DATENBANK - also neben der Arbeitskopie.
            string protokollDatei = KiAusfuehrer.ProtokollPfad();
            _log.Zeile("Aktionsprotokoll: " + protokollDatei);
            if (File.Exists(protokollDatei)) File.Delete(protokollDatei);
            _log.Leerzeile();

            using (var waechter = new DialogWaechter())
            {
                // ------------------------------------------------------ Register
                KiRegister register = KiAusfuehrer.Register;
                _log.Zeile("Registrierte Aktionen: " + register.Anzahl);
                foreach (KiAktion a in register.Alle)
                    _log.Roh("      · " + a.Name + "  [" + SchutzstufeText.Schluessel(a.Stufe) + "]  -> " + a.Andockpunkt);
                _log.Leerzeile();

                // Seit Etappe 3 sind auch Schreibaktionen registriert. Was hier zaehlt,
                // ist nicht mehr „alles Stufe 1", sondern: nichts oberhalb der Grenze,
                // die der Riegel ueberhaupt freigibt (Fachkonzept 4.1).
                foreach (KiAktion a in register.Alle)
                    if (a.Stufe > KiRiegel.HoechsteStufe)
                        _log.FehlerZeile("Aktion " + a.Name + " liegt oberhalb von KiRiegel.HoechsteStufe.");

                // ------------------------------------------------------ Eckdaten
                Eckdaten eck = EckdatenLesen();
                _log.Zeile("Eckdaten der Arbeitskopie: " + eck);
                _log.Leerzeile();

                // ------------------------------------------------------ Aufrufe
                var gerufen = new HashSet<string>(StringComparer.Ordinal);
                int zeilenVorher = Protokollzeilen(protokollDatei);

                _log.Zeile("--- Aufrufe der registrierten Aktionen ---");
                foreach (var fall in Faelle(eck))
                {
                    gerufen.Add(fall.Aktion);
                    Rufe(fall.Aktion, fall.Werte, protokollDatei, ref zeilenVorher, fall.ErwarteErfolg);
                }

                _log.Leerzeile();
                _log.Zeile("--- Gegenproben (muessen sauber abgewiesen werden) ---");
                Rufe("datenbank_leeren", Werte(), protokollDatei, ref zeilenVorher, false);
                Rufe("projekt_lesen", Werte(), protokollDatei, ref zeilenVorher, false);
                Rufe("projekt_lesen", Werte("projekt_id", 999999), protokollDatei, ref zeilenVorher, false);
                Rufe("projekt_lesen", Werte("projekt_id", "kein Projekt"), protokollDatei, ref zeilenVorher, false);
                Rufe("kostenlage_pruefen", Werte("projekt_id", eck.IdBeispiel, "komponente", "Kernkraftwerk"),
                     protokollDatei, ref zeilenVorher, false);

                // ------------------------------------------------------ Einlaeufigkeit
                _log.Leerzeile();
                _log.Zeile("--- Einlaeufigkeit (zwei Aufrufe gleichzeitig) ---");
                EinlaeufigkeitPruefen(eck, protokollDatei, ref zeilenVorher);

                // ------------------------------------------------------ Rechtshinweis
                // Abschalter der Installation und versionierte Einwilligung. Steht VOR
                // der Werkzeugrunde: ohne erteilte Einwilligung darf dort nichts laufen.
                // Der Protokollzaehler wird danach neu gesetzt - die Faelle 3 und 4
                // fuehren echte Leseaktionen aus und schreiben dabei Protokollzeilen.
                Einwilligung.Pruefen(_log);
                zeilenVorher = Protokollzeilen(protokollDatei);

                // ------------------------------------------------------ Werkzeugrunde
                // Etappe 2: Absichtserkennung, Rundendeckel, Riegel, Cache-Umgehung und
                // Datenschutzschicht - mit EINGESPEISTER Modellantwort, ohne Netz.
                Werkzeugrunde.Pruefen(_log, protokollDatei, ref zeilenVorher);

                // ------------------------------------------------------ Schreibrunde
                // Etappe 3: Vorschau, Bestaetigung, Verfall, Sicherungspunkt,
                // DarfSchreiben() und Schreibschutz - mit Vorher-/Nachher-Werten aus
                // der ARBEITSKOPIE. Dass die produktive Datenbank dabei aussen vor
                // bleibt, belegt der SHA-256-Vergleich am Ende dieses Laufs.
                Schreibrunde.Pruefen(_log, protokollDatei, ref zeilenVorher);
                foreach (string name in Schreibrunde.Gerufen) gerufen.Add(name);

                // ------------------------------------------------------ Vollstaendigkeit
                _log.Leerzeile();
                foreach (KiAktion a in register.Alle)
                    if (!gerufen.Contains(a.Name))
                        _log.FehlerZeile("Aktion " + a.Name + " wurde NICHT aufgerufen.");
                _log.Zeile("Aufgerufene Registeraktionen: " + gerufen.Count + " von " + register.Anzahl);

                // ------------------------------------------------------ Dialoge
                string[] dialoge = waechter.GeschlosseneDialoge;
                if (dialoge.Length == 0) _log.Zeile("DialogWaechter: kein Dialog erschienen.");
                else
                    foreach (string d in dialoge)
                        _log.FehlerZeile("UNERWARTETER DIALOG: " + d);
            }

            // ---------------------------------------------------------- Protokollformat
            _log.Leerzeile();
            ProtokollPruefen(protokollDatei);

            // ---------------------------------------------------------- Produktiv-DB
            _log.Leerzeile();
            string hashNachher = Hash(quelle);
            _log.Zeile("SHA-256 der Produktiv-DB nachher: " + hashNachher);
            if (!string.Equals(hashVorher, hashNachher, StringComparison.Ordinal))
                _log.FehlerZeile("Die produktive Datenbank hat sich VERAENDERT.");
            else
                _log.Zeile("Die produktive Datenbank ist unveraendert.");

            // ---------------------------------------------------------- Registry
            _log.Leerzeile();
            Einwilligung.Wiederherstellen(_log);

            _log.Leerzeile();
            _log.Zeile("Warnungen: " + _log.Warnungen + ", Fehler: " + _log.Fehler);
            Speichern(zielWurzel);
            return _log.Fehler == 0 ? 0 : 1;
        }

        // =====================================================================
        // Ein Aufruf
        // =====================================================================

        private static void Rufe(string aktion, IReadOnlyDictionary<string, object> werte,
                                 string protokollDatei, ref int zeilenVorher, bool erwarteErfolg)
        {
            KiErgebnis e;
            try
            {
                e = KiAusfuehrer.AusfuehrenAsync(aktion, werte).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.FehlerZeile(aktion + ": AUSNAHME nach aussen durchgeschlagen - " + ex);
                return;
            }

            int zeilenNachher = Protokollzeilen(protokollDatei);
            int neu = zeilenNachher - zeilenVorher;
            zeilenVorher = zeilenNachher;

            string kopf = aktion.PadRight(34) + SchutzstufeText.Schluessel(e.Status).PadRight(15);
            _log.Roh("      " + kopf + e.Anzahl.ToString(CultureInfo.InvariantCulture).PadLeft(5) + "x  " +
                     ((long)e.Dauer.TotalMilliseconds).ToString(CultureInfo.InvariantCulture).PadLeft(6) + " ms  " +
                     Einzeilig(e.Text));

            foreach (string m in e.Meldungen) _log.Roh("            Meldung: " + Einzeilig(m));

            if (neu != 1)
                _log.FehlerZeile(aktion + ": " + neu + " Protokollzeilen statt genau einer.");

            if (erwarteErfolg && !e.Erfolg)
                _log.FehlerZeile(aktion + ": erwartet war ein Ergebnis, geliefert wurde " +
                                 SchutzstufeText.Schluessel(e.Status) + " (" + Einzeilig(e.Text) + ").");

            if (!erwarteErfolg && e.Erfolg)
                _log.FehlerZeile(aktion + ": erwartet war eine Abweisung, die Aktion lief aber durch.");
        }

        // =====================================================================
        // Einlaeufigkeit
        // =====================================================================

        /// <summary>
        /// Startet zwei Aktionen gleichzeitig. Genau eine muss laufen, die andere muss
        /// ABGEWIESEN werden (Fachkonzept 3.4, Pflicht 1) - eingereiht wird nichts.
        /// Gewaehlt ist die laengste Leseaktion, damit sich die Laeufe sicher ueberlappen.
        /// </summary>
        private static void EinlaeufigkeitPruefen(Eckdaten eck, string protokollDatei, ref int zeilenVorher)
        {
            IReadOnlyDictionary<string, object> werte = Werte(
                "ganglinie_id", eck.IdGanglinie, "kapazitaet_kwh", 300.0, "leistung_kw", 200.0,
                "projekt_id", eck.IdBeispiel);

            if (eck.IdGanglinie <= 0)
            { _log.Warnung("Keine Ganglinie vorhanden - Einlaeufigkeit nicht pruefbar."); return; }

            var start = new System.Threading.ManualResetEventSlim(false);
            var ergebnisse = new KiErgebnis[2];

            var laeufe = new System.Threading.Tasks.Task[2];
            for (int i = 0; i < 2; i++)
            {
                int n = i;
                laeufe[n] = System.Threading.Tasks.Task.Run(() =>
                {
                    start.Wait();
                    ergebnisse[n] = KiAusfuehrer.AusfuehrenAsync("minimale_spitze_ermitteln", werte)
                                                .GetAwaiter().GetResult();
                });
            }
            start.Set();
            System.Threading.Tasks.Task.WaitAll(laeufe);

            int gelaufen = ergebnisse.Count(e => e != null && e.Erfolg);
            int abgewiesen = ergebnisse.Count(e => e != null && e.Status == KiStatus.Abgelehnt);

            foreach (KiErgebnis e in ergebnisse)
                _log.Roh("      " + SchutzstufeText.Schluessel(e.Status).PadRight(15) + Einzeilig(e.Text));

            if (gelaufen != 1 || abgewiesen != 1)
                _log.FehlerZeile("Einlaeufigkeit verletzt: " + gelaufen + " gelaufen, " +
                                 abgewiesen + " abgewiesen (erwartet 1/1).");

            int neuZeilen = Protokollzeilen(protokollDatei) - zeilenVorher;
            zeilenVorher += neuZeilen;
            if (neuZeilen != 2)
                _log.FehlerZeile("Einlaeufigkeit: " + neuZeilen + " Protokollzeilen statt zwei.");

            if (KiAusfuehrer.Belegt) _log.FehlerZeile("Die Laufsperre ist nach dem Lauf noch belegt.");
        }

        // =====================================================================
        // Faelle
        // =====================================================================

        private sealed class Fall
        {
            public string Aktion;
            public IReadOnlyDictionary<string, object> Werte;
            public bool ErwarteErfolg = true;
        }

        private static IEnumerable<Fall> Faelle(Eckdaten eck)
        {
            yield return new Fall { Aktion = "projekte_auflisten", Werte = Werte() };
            yield return new Fall { Aktion = "projekt_lesen", Werte = Werte("projekt_id", eck.IdBeispiel) };
            yield return new Fall { Aktion = "varianten_auflisten", Werte = Werte("projekt_id", eck.IdBeispiel) };
            yield return new Fall
            {
                Aktion = "speichervarianten_auflisten",
                Werte = Werte("projekt_id", eck.IdSpeicher > 0 ? eck.IdSpeicher : eck.IdBeispiel)
            };
            yield return new Fall
            {
                Aktion = "ergebnisse_lesen",
                Werte = Werte("projekt_ids", new object[] { eck.IdBeispiel, eck.IdZweites })
            };
            yield return new Fall
            {
                Aktion = "wirtschaftlichkeit_parameter_lesen",
                Werte = Werte("projekt_id", eck.IdBeispiel)
            };
            yield return new Fall
            {
                Aktion = "kostenlage_pruefen",
                Werte = Werte("projekt_id", eck.IdKostenProjekt, "komponente", eck.Komponente),
                // Ohne verbaute Komponente ist die Abweisung das RICHTIGE Ergebnis.
                ErwarteErfolg = eck.IdKostenProjekt > 0
            };
            yield return new Fall
            {
                Aktion = "uebernahme_vorschau",
                Werte = Werte("von_projekt", eck.IdBeispiel, "nach_projekt", eck.IdZweites,
                              "gewerk", "Wärmepumpe")
            };
            yield return new Fall
            {
                Aktion = "merkmal_vorschau",
                Werte = Werte("von_projekt", eck.IdBeispiel, "nach_projekt", eck.IdZweites,
                              "merkmal", "Tab_Energieanlagen.Vorlauf")
            };
            yield return new Fall
            {
                Aktion = "lastgang_pruefen",
                Werte = Werte("dateipfad", BeispieldateiAnlegen())
            };
            yield return new Fall
            {
                Aktion = "ganglinien_auflisten",
                Werte = Werte("projekt_id", eck.IdBeispiel)
            };
            yield return new Fall
            {
                Aktion = "minimale_spitze_ermitteln",
                Werte = Werte("ganglinie_id", eck.IdGanglinie,
                              "kapazitaet_kwh", 300.0, "leistung_kw", 200.0,
                              "projekt_id", eck.IdBeispiel),
                ErwarteErfolg = eck.IdGanglinie > 0
            };
            yield return new Fall { Aktion = "letzte_aktionen", Werte = Werte("anzahl", 5) };
        }

        // =====================================================================
        // Eckdaten der Arbeitskopie
        // =====================================================================

        private sealed class Eckdaten
        {
            public int Projekte;
            public int IdBeispiel;
            public string NameBeispiel = "";
            public int IdZweites;
            public int IdSpeicher;
            public int IdGanglinie;
            public int IdKostenProjekt;
            public string Komponente = DbWerte.ERZEUGER_WAERMEPUMPE;

            public override string ToString()
            {
                return "Projekte=" + Projekte +
                       ", Beispiel=" + IdBeispiel + " (" + NameBeispiel + ")" +
                       ", zweites=" + IdZweites +
                       ", Speicherprojekt=" + IdSpeicher +
                       ", Ganglinie=" + IdGanglinie +
                       ", Kostenfall=" + IdKostenProjekt + "/" + Komponente;
            }
        }

        private static Eckdaten EckdatenLesen()
        {
            var e = new Eckdaten();

            DataTable dt = DataRepository.GetDataTable("SELECT ID, Projektname FROM Tab_Projekt ORDER BY ID");
            e.Projekte = dt == null ? 0 : dt.Rows.Count;

            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                    string name = Convert.ToString(r["Projektname"]) ?? "";
                    if (e.IdBeispiel == 0 && string.Equals(name, BEISPIELPROJEKT, StringComparison.Ordinal))
                    { e.IdBeispiel = id; e.NameBeispiel = name; }
                }
                if (e.IdBeispiel == 0 && dt.Rows.Count > 0)
                {
                    e.IdBeispiel = Convert.ToInt32(dt.Rows[0]["ID"], CultureInfo.InvariantCulture);
                    e.NameBeispiel = Convert.ToString(dt.Rows[0]["Projektname"]) ?? "";
                }
                foreach (DataRow r in dt.Rows)
                {
                    int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                    if (id != e.IdBeispiel) { e.IdZweites = id; break; }
                }
            }

            e.IdSpeicher = Skalar("SELECT MIN(a.ID_Projekt) FROM Tab_Energieanlagen AS a " +
                                  "WHERE a.ID_SP IS NOT NULL");
            e.IdGanglinie = Skalar("SELECT MIN(ID) FROM Tab_Stromganglinie_STAMM");

            // Ein Projekt, das die geprüfte Komponente wirklich fuehrt - sonst waere die
            // Vorbedingung (zu Recht) die einzige Antwort.
            foreach (string spalte in new[] { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" })
            {
                int id = Skalar("SELECT MIN(ID_Projekt) FROM Tab_Energieanlagen WHERE [" + spalte + "] IS NOT NULL");
                if (id <= 0) continue;
                e.IdKostenProjekt = id;
                e.Komponente = KomponenteZu(spalte);
                break;
            }

            return e;
        }

        private static string KomponenteZu(string spalte)
        {
            switch (spalte)
            {
                case "ID_WP": return DbWerte.ERZEUGER_WAERMEPUMPE;
                case "ID_Kessel": return DbWerte.ERZEUGER_HEIZKESSEL;
                case "ID_BHKW": return DbWerte.ERZEUGER_BHKW;
                case "ID_PV": return DbWerte.ERZEUGER_PHOTOVOLTAIK;
                case "ID_Solar": return DbWerte.ERZEUGER_SOLARTHERMIE;
                case "ID_SP": return DbWerte.ERZEUGER_STROMSPEICHER;
                default: return DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER;
            }
        }

        private static int Skalar(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        // =====================================================================
        // Protokollpruefung
        // =====================================================================

        private static void ProtokollPruefen(string datei)
        {
            if (!File.Exists(datei)) { _log.FehlerZeile("Es wurde kein Aktionsprotokoll geschrieben."); return; }

            string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8);
            int vorspann = zeilen.Count(z => z.StartsWith("#", StringComparison.Ordinal));
            IReadOnlyList<KiProtokollEintrag> eintraege = KiProtokoll.LiesAlle(zeilen);
            int nutz = zeilen.Length - vorspann;

            _log.Zeile("Protokoll: " + zeilen.Length + " Zeilen (" + vorspann + " Vorspann, " +
                       nutz + " Eintraege), davon lesbar: " + eintraege.Count);

            if (eintraege.Count != nutz)
                _log.FehlerZeile("Nicht jede Protokollzeile liess sich zurueck lesen.");

            foreach (KiProtokollEintrag e in eintraege)
            {
                if (string.IsNullOrWhiteSpace(e.Aktion)) _log.FehlerZeile("Protokollzeile ohne Aktionsnamen.");
                if (e.Parameter == null || !e.Parameter.StartsWith("{", StringComparison.Ordinal))
                    _log.FehlerZeile("Protokollzeile " + e.Aktion + ": Parameter sind kein JSON-Objekt.");
            }

            _log.Roh("");
            _log.Roh("      Auszug:");
            foreach (string z in zeilen.Take(Math.Min(zeilen.Length, 12))) _log.Roh("      " + z);
        }

        private static int Protokollzeilen(string datei)
        {
            if (!File.Exists(datei)) return 0;
            try
            {
                int n = 0;
                foreach (string z in File.ReadLines(datei, Encoding.UTF8))
                    if (z.Length > 0 && !z.StartsWith("#", StringComparison.Ordinal)) n++;
                return n;
            }
            catch { return 0; }
        }

        // =====================================================================
        // Hilfen
        // =====================================================================

        private static IReadOnlyDictionary<string, object> Werte(params object[] paare)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < paare.Length; i += 2) d[(string)paare[i]] = paare[i + 1];
            return d;
        }

        /// <summary>Legt eine kleine, gueltige Lastgangdatei fuer lastgang_pruefen an.</summary>
        private static string BeispieldateiAnlegen()
        {
            string pfad = Path.Combine(Path.GetTempPath(), "EPOS_KiHarnisch_lastgang.csv");
            var sb = new StringBuilder();
            sb.AppendLine("Zeit;Leistung");
            for (int i = 0; i < 96; i++)
                sb.AppendLine(new DateTime(2025, 1, 1).AddMinutes(15 * i).ToString("dd.MM.yyyy HH:mm",
                              CultureInfo.InvariantCulture) + ";" +
                              (100.0 + 50.0 * Math.Sin(i / 6.0)).ToString("0.000", CultureInfo.InvariantCulture));
            File.WriteAllText(pfad, sb.ToString(), new UTF8Encoding(false));
            return pfad;
        }

        private static string Einzeilig(string text)
        {
            return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string Hash(string datei)
        {
            using (var sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(datei))
                return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
        }

        private static string Argument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private static void Speichern(string zielWurzel)
        {
            try
            {
                _log.Speichern(Path.Combine(zielWurzel, "ki_harnisch_protokoll.md"),
                               "Aktionsharnisch KI-Assistent (Etappen 1 bis 3)",
                               new[] { "Erzeugt von KiHarnisch.exe.", "" });
            }
            catch (Exception ex) { Console.WriteLine("Protokoll nicht speicherbar: " + ex.Message); }
        }
    }
}

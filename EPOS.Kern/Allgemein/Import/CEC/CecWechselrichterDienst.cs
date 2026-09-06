using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Abruf der CEC-Wechselrichterliste</b> (NREL/SAM) — Stufe S1.5 des
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c> (Anwenderentscheid <b>W6‑E‑2‑Q2</b>
    /// vom 06.09.2026: „Alle drei, gestaffelt. CEC und Handpflege in S1").
    ///
    /// <para><b>Zwilling zu <see cref="CECDataService"/>, wörtlich.</b> Die Liste liegt
    /// im SELBEN Verzeichnis wie die Modulliste, die EPOS-Plan schon lädt; damit ist der
    /// ganze Apparat vorhanden und wiederverwendbar: eine Rückfallkette aus zwei URLs,
    /// 45 Sekunden Zeitgrenze je Versuch, ein 30-Tage-Zwischenspeicher, ein
    /// Fortschrittsmelder mit Abbruch (<see cref="CecFortschritt"/>) und die mehrzeilige
    /// Kopf-/Einheitenzeile, die der CSV-Leser bereits kennt.</para>
    ///
    /// <para><b>Der Zwischenspeicher ist ein EIGENER.</b> Er liegt neben dem der Module
    /// (<c>LocalApplicationData\CECModuleImporter\cec_inverters.csv</c>), nicht in
    /// derselben Datei: Es sind zwei Kataloge mit zwei Kopfzeilen, und ein gemeinsamer
    /// Speicher entwertete beim ersten Abruf den jeweils anderen.</para>
    ///
    /// <para><b>Gemessen am 06.09.2026:</b> 2 343 Geräte von 152 Herstellern in
    /// 2 346 Zeilen (Kopf-, Einheiten- und <c>[0]</c>-Zeile). Die Liste liegt damit in
    /// derselben Größenordnung wie die Modulliste (20 746) und braucht dieselbe
    /// Behandlung — ein virtualisiertes Raster.</para>
    ///
    /// <para><b>Der Kern kennt keine Anzeigetexte.</b> Jede Rückmeldung ist ein
    /// SCHLÜSSEL mit Platzhalterwerten; der Wirt übersetzt (dieselbe Regel wie bei
    /// <see cref="CECDataService"/>, Abweichung A-17 der Welle 13).</para>
    /// </summary>
    public class CecWechselrichterDienst
    {
        private const string Url1 =
            "https://raw.githubusercontent.com/NREL/SAM/develop/deploy/libraries/CEC%20Inverters.csv";
        private const string Url2 =
            "https://raw.githubusercontent.com/NREL/SAM/refs/heads/develop/deploy/libraries/CEC%20Inverters.csv";

        /// <summary>Die Pflichtspalten der Kopfzeile — ohne sie stünde im Katalog stillschweigend 0.</summary>
        internal static readonly string[] PFLICHTSPALTEN =
        {
            "name", "paco", "pdco", "pso", "c0", "vdcmax", "idcmax", "mppt_low", "mppt_high"
        };

        private readonly string _zwischenspeicher;
        private List<CecWechselrichter> _geraete = new List<CecWechselrichter>();

        /// <summary>Die gelesenen Geräte.</summary>
        public IReadOnlyList<CecWechselrichter> AlleGeraete => _geraete;

        public CecWechselrichterDienst()
        {
            _zwischenspeicher = Dienste.Pfade.Verbinde(
                Dienste.Pfade.BenutzerLokalBasis, "CECModuleImporter", "cec_inverters.csv");
        }

        /// <summary>
        /// Holt die Liste — aus dem Zwischenspeicher, wenn er jünger als 30 Tage ist,
        /// sonst über HTTP von einer der beiden Quellen.
        /// </summary>
        public async Task<(bool Erfolg, CecFortschritt Meldung)> LadenAsync(
            IProgress<CecFortschritt> melder = null,
            CancellationToken abbruch = default)
        {
            melder?.Report(new CecFortschritt("CEC_PROT_CACHE_SUCHEN"));

            if (File.Exists(_zwischenspeicher))
            {
                TimeSpan alter = DateTime.Now - File.GetLastWriteTime(_zwischenspeicher);
                if (alter.TotalDays < 30)
                {
                    melder?.Report(new CecFortschritt("CEC_PROT_CACHE_LADEN"));
                    return AusDatei(_zwischenspeicher);
                }
            }

            melder?.Report(new CecFortschritt("CEC_PROT_VERBINDEN"));

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(45);
                http.DefaultRequestHeaders.Add("User-Agent", "CECModuleImporter/1.0");

                foreach (string url in new[] { Url1, Url2 })
                {
                    abbruch.ThrowIfCancellationRequested();
                    try
                    {
                        melder?.Report(new CecFortschritt("CEC_PROT_VERSUCHE", new Uri(url).Host));
                        HttpResponseMessage antwort = await http.GetAsync(url, abbruch).ConfigureAwait(false);
                        if (!antwort.IsSuccessStatusCode) continue;

                        string csv = await antwort.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (csv.Length < 1000) continue;

                        string ordner = Path.GetDirectoryName(_zwischenspeicher);
                        if (!Directory.Exists(ordner)) Directory.CreateDirectory(ordner);

                        using (var sw = new StreamWriter(_zwischenspeicher, false, Encoding.UTF8))
                            await sw.WriteAsync(csv).ConfigureAwait(false);

                        melder?.Report(new CecFortschritt("CEC_PROT_GESPEICHERT"));
                        return AusDatei(_zwischenspeicher);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        melder?.Report(new CecFortschritt("CEC_PROT_FEHLER", ex.Message));
                    }
                }
            }

            if (File.Exists(_zwischenspeicher))
            {
                melder?.Report(new CecFortschritt("CEC_PROT_CACHE_ALT"));
                return AusDatei(_zwischenspeicher);
            }

            return (false, new CecFortschritt("CEC_MSG_KEINE_QUELLE"));
        }

        /// <summary>
        /// Liest eine Liste aus einer Datei — der Prüfweg ohne Netz und der Weg, auf dem
        /// die Importprobe <c>Referenzlaeufe/Importproben/cec_wechselrichter_21.csv</c>
        /// gelesen wird.
        /// </summary>
        public (bool Erfolg, CecFortschritt Meldung) AusDatei(string pfad)
        {
            if (!File.Exists(pfad)) return (false, new CecFortschritt("CEC_MSG_DATEI_FEHLT"));

            try
            {
                List<string> zeilen = Nutzzeilen(File.ReadAllLines(pfad));
                if (zeilen.Count <= 1) return (false, new CecFortschritt("CEC_MSG_LEER"));

                // Trennzeichen an der Kopfzeile bestimmen - dieselbe Regel wie beim
                // Modulimport: das haeufigere von ';' und ','. Die Originaldatei des
                // NREL nimmt das Komma, eine Excel-Ausleitung das Semikolon.
                char trenner = zeilen[0].Count(c => c == ';') > zeilen[0].Count(c => c == ',') ? ';' : ',';

                string[] kopf = Zerlege(zeilen[0], trenner);
                var spalte = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < kopf.Length; i++)
                    spalte[kopf[i].Trim().ToLowerInvariant().Replace(" ", "_")] = i;

                int Spalte(string name) => spalte.TryGetValue(name, out int idx) ? idx : -1;

                List<string> fehlend = PFLICHTSPALTEN.Where(s => Spalte(s) < 0).ToList();
                if (fehlend.Count > 0)
                    return (false, new CecFortschritt("CEC_MSG_KOPFZEILE", string.Join(", ", fehlend)));

                var geraete = new List<CecWechselrichter>();
                for (int z = 1; z < zeilen.Count; z++)
                {
                    string[] felder = Zerlege(zeilen[z], trenner);
                    if (felder.Length < 3) continue;

                    string Feld(int idx) => (idx >= 0 && idx < felder.Length) ? felder[idx].Trim() : "";

                    var g = new CecWechselrichter
                    {
                        Name = Feld(Spalte("name")),
                        Vac = Feld(Spalte("vac")),
                        Pso = Zahl(Feld(Spalte("pso"))),
                        Paco = Zahl(Feld(Spalte("paco"))),
                        Pdco = Zahl(Feld(Spalte("pdco"))),
                        Vdco = Zahl(Feld(Spalte("vdco"))),
                        C0 = Zahl(Feld(Spalte("c0"))),
                        C1 = Zahl(Feld(Spalte("c1"))),
                        C2 = Zahl(Feld(Spalte("c2"))),
                        C3 = Zahl(Feld(Spalte("c3"))),
                        Pnt = Zahl(Feld(Spalte("pnt"))),
                        Vdcmax = Zahl(Feld(Spalte("vdcmax"))),
                        Idcmax = Zahl(Feld(Spalte("idcmax"))),
                        MpptLow = Zahl(Feld(Spalte("mppt_low"))),
                        MpptHigh = Zahl(Feld(Spalte("mppt_high"))),
                        CecDatum = Feld(Spalte("cec_date"))
                    };

                    // Eine Zeile ohne Namen ist kein Geraet - sie waere ein
                    // Katalogsatz ohne Bezeichner und damit nicht adressierbar.
                    if (g.Name.Length == 0) continue;

                    geraete.Add(g);
                }

                _geraete = geraete;
                return (true, new CecFortschritt("CEC_MSG_GELADEN",
                    _geraete.Count.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                return (false, new CecFortschritt("CEC_MSG_FEHLER", ex.Message));
            }
        }

        /// <summary>Die Hersteller der geladenen Liste, aufsteigend und ohne Dubletten.</summary>
        public IEnumerable<string> Hersteller()
        {
            return _geraete.Select(g => g.Hersteller)
                           .Where(h => !string.IsNullOrEmpty(h))
                           .Distinct()
                           .OrderBy(h => h, StringComparer.CurrentCulture);
        }

        // =================================================================
        //  Der Zerleger
        // =================================================================

        /// <summary>
        /// Die Nutzzeilen einer CEC-Datei: ohne Leer- und Kommentarzeilen, ohne die
        /// Einheitenzeile (<c>Units</c>) und ohne die Variablennamenzeile
        /// (<c>[0]</c>). Die erste verbleibende Zeile ist die Kopfzeile.
        /// </summary>
        /// <remarks>
        /// Der Modulleser sucht die Einheitenzeile zusätzlich über Markerzeichen
        /// (<c>IsUnitsRow</c>). Das braucht es hier nicht: Die Einheitenzeile der
        /// Wechselrichterliste beginnt IMMER mit „Units", die Variablenzeile mit
        /// „[0]" — beide sind an ihrer ersten Spalte eindeutig, und ein Gerätename
        /// beginnt nie so.
        /// </remarks>
        internal static List<string> Nutzzeilen(IEnumerable<string> zeilen)
        {
            var raus = new List<string>();
            foreach (string zeile in zeilen)
            {
                string t = (zeile ?? "").Trim();
                if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal)) continue;
                if (t.StartsWith("Units", StringComparison.OrdinalIgnoreCase)) continue;
                if (t.StartsWith("[0]", StringComparison.Ordinal)) continue;
                raus.Add(zeile);
            }
            return raus;
        }

        /// <summary>
        /// Zerlegt eine CSV-Zeile; ein Trennzeichen IM Anführungszeichenfeld trennt
        /// nicht. Wörtlich <c>CECDataService.SplitCsvLine</c>.
        /// </summary>
        internal static string[] Zerlege(string zeile, char trenner)
        {
            var raus = new List<string>();
            bool imFeld = false;
            var puffer = new StringBuilder();

            foreach (char c in zeile ?? "")
            {
                if (c == '"') imFeld = !imFeld;
                else if (c == trenner && !imFeld) { raus.Add(puffer.ToString()); puffer.Clear(); }
                else puffer.Append(c);
            }
            raus.Add(puffer.ToString());
            return raus.ToArray();
        }

        /// <summary>
        /// Eine Zahl aus der Datei — invariant, mit Komma als zweitem Dezimalzeichen
        /// (Excel-Ausleitung). Wörtlich <c>CECDataService.SafeD</c>.
        /// </summary>
        internal static double Zahl(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "N/A" || s == "-") return 0.0;
            return double.TryParse(s.Replace(",", "."), NumberStyles.Any,
                                   CultureInfo.InvariantCulture, out double d) ? d : 0.0;
        }
    }
}

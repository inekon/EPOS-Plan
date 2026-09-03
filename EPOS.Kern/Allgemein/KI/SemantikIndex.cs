using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Paket H10: der oertliche Einbettungs-Index ueber die Online-Dokumentation.
    /// Zerlegt jede echte Wiki-Seite in Abschnitte, bettet sie mit
    /// <see cref="SemantikModell"/> ein und beantwortet damit die zweite Stufe
    /// der Hybridsuche in <see cref="WikiWissen"/>. Oberflaechenfrei.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ueberhaupt.</b> Die Stichwortsuche des Wikis verknuepft mit UND
    /// und kennt keine Synonyme: wer nach „Akku" fragt, findet die Seite
    /// „Stromspeicher" nicht. Der Index beantwortet genau diese Faelle - er
    /// ERSETZT die Stichwortsuche nicht, er fuellt die Luecken (H9 bleibt
    /// fuehrend, siehe <see cref="WikiWissen.SucheAsync(string,string,CancellationToken)"/>).
    /// </para>
    /// <para>
    /// <b>Nichts verlaesst den Rechner.</b> Eingebettet wird oertlich; die
    /// Seitentexte kommen ueber denselben Weg und denselben Tagescache wie in
    /// H4 (<see cref="WikiWissen"/>), es entsteht KEIN zusaetzlicher Empfaenger.
    /// Der Index selbst liegt unter
    /// <c>%APPDATA%\wp-plan\semantik\index\</c>, eine Datei je Seite.
    /// </para>
    /// <para>
    /// <b>Der Aufbau haelt niemanden auf.</b> <see cref="Anstossen"/> kehrt
    /// sofort zurueck; gearbeitet wird auf einem Hintergrund-Faden. Solange
    /// nichts fertig ist, liefert <see cref="Suche"/> eine leere Liste und die
    /// Suche verhaelt sich exakt wie vor H10. Ein Aufbau, der scheitert,
    /// hinterlaesst nichts ausser einer Zeile im Debug-Fenster.
    /// </para>
    /// <para>
    /// <b>Weiterleitungen bleiben draussen.</b> Aufgenommen werden nur echte
    /// Seiten (<c>apfilterredir=nonredirects</c>). Die 37 Synonym-Weiterleitungen
    /// aus H9 haben keinen eigenen Text - sie zu indizieren hiesse, dieselbe
    /// Seite mehrfach zu fuehren.
    /// </para>
    /// </remarks>
    public static class SemantikIndex
    {
        /// <summary>Kuerzester Abschnitt, der allein stehen darf.</summary>
        private const int CHUNK_MIN = 200;

        /// <summary>
        /// Laengster Abschnitt. Bewusst am unteren Rand der Konzeptspanne
        /// (200-1200): das Modell ist auf 128 Stuecke trainiert, und rund 900
        /// deutsche Zeichen liegen bei etwa 250 Stuecken - laengere Abschnitte
        /// wuerden bei <c>MAX_STUECKE</c> abgeschnitten und der Rest ginge
        /// verloren.
        /// </summary>
        private const int CHUNK_MAX = 900;

        /// <summary>Gueltigkeit einer Indexdatei - dieselbe Spanne wie der Textcache.</summary>
        private static readonly TimeSpan Gueltigkeit = TimeSpan.FromHours(24);

        /// <summary>
        /// Ab diesem Kosinus gilt ein Abschnitt als Treffer. Gemessen am
        /// 29.08.2026: eine passende Frage liegt bei 0,70-0,80 gegen ihren
        /// Abschnitt, eine unpassende bei 0,25-0,30 - dazwischen ist viel Luft.
        /// </summary>
        public const double SCHWELLE = 0.40;

        /// <summary>Ein eingebetteter Abschnitt.</summary>
        internal sealed class Abschnitt
        {
            public string Titel = "";
            public string Ueberschrift = "";
            public string Text = "";
            public float[] Vektor;

            /// <summary>
            /// Kein Textabschnitt, sondern der KURZNAME der Seite als eigener
            /// Eintrag - siehe <see cref="Titelabschnitt"/>.
            /// </summary>
            public bool IstTitel;
        }

        /// <summary>Ein Fund der semantischen Stufe.</summary>
        internal sealed class Fund
        {
            public string Titel = "";
            public string Ueberschrift = "";
            public double Punkte;
        }

        private static readonly object _riegel = new object();

        /// <summary>Der fertige Index. <c>null</c>, solange noch keiner steht.</summary>
        private static Abschnitt[] _abschnitte;

        /// <summary>Zeitpunkt, zu dem <see cref="_abschnitte"/> gebaut wurde.</summary>
        private static DateTime _gebautUtc = DateTime.MinValue;

        /// <summary>0 = kein Aufbau, 1 = einer laeuft (<see cref="Interlocked"/>).</summary>
        private static int _aufbau;

        /// <summary>
        /// Wann ein Aufbau zuletzt OHNE Ergebnis endete. Ohne diesen Merker
        /// stiesse JEDE Frage einen neuen Versuch an, solange das Netz weg ist -
        /// im Hintergrund zwar, aber jedes Mal mit vier Sekunden Wartezeit.
        /// </summary>
        private static DateTime _fehlversuchUtc = DateTime.MinValue;

        /// <summary>Sperrfrist nach einem erfolglosen Aufbau.</summary>
        private static readonly TimeSpan Sperrfrist = TimeSpan.FromMinutes(5);

        /// <summary>Zaehler fuer das Protokoll: Seiten und Abschnitte des letzten Aufbaus.</summary>
        public static int Seitenzahl { get; private set; }

        /// <summary>Anzahl der Abschnitte im aktuellen Index.</summary>
        public static int Abschnittszahl
        {
            get { lock (_riegel) return _abschnitte == null ? 0 : _abschnitte.Length; }
        }

        /// <summary>Dauer des letzten Aufbaus in Millisekunden.</summary>
        public static long AufbauzeitMs { get; private set; }

        /// <summary>Steht ein benutzbarer Index?</summary>
        public static bool Bereit
        {
            get { lock (_riegel) return _abschnitte != null && _abschnitte.Length > 0; }
        }

        // ==================================================================
        //  Anstoss
        // ==================================================================

        /// <summary>
        /// Stoesst Modell und Indexaufbau an und kehrt SOFORT zurueck. Mehrfach
        /// aufzurufen ist ausdruecklich vorgesehen - jeder Aufruf, der einen
        /// laufenden Aufbau oder einen frischen Index vorfindet, tut nichts.
        /// </summary>
        public static void Anstossen(string basis)
        {
            if (string.IsNullOrWhiteSpace(basis)) return;
            if (SemantikModell.Zustand == SemantikModell.Lage.Nichtverfuegbar) return;

            lock (_riegel)
            {
                if (_abschnitte != null && DateTime.UtcNow - _gebautUtc < Gueltigkeit) return;
                if (DateTime.UtcNow - _fehlversuchUtc < Sperrfrist) return;
            }

            if (Interlocked.CompareExchange(ref _aufbau, 1, 0) != 0) return;

            Task.Run(async () =>
            {
                try { await AufbauenAsync(basis, CancellationToken.None).ConfigureAwait(false); }
                catch (Exception ex) { Debug.WriteLine("[Semantik] Indexaufbau: " + ex.Message); }
                finally
                {
                    lock (_riegel) { if (_abschnitte == null) _fehlversuchUtc = DateTime.UtcNow; }
                    Interlocked.Exchange(ref _aufbau, 0);
                }
            });
        }

        /// <summary>Laeuft gerade ein Aufbau?</summary>
        public static bool AufbauLaeuft { get { return Volatile.Read(ref _aufbau) != 0; } }

        // ==================================================================
        //  Aufbau
        // ==================================================================

        /// <summary>
        /// Baut den Index: Modell aufwaermen, Seitenliste holen, je Seite die
        /// Indexdatei lesen oder neu rechnen, alles in den Speicher stellen.
        /// Der Weg des Pruefharnischs ist derselbe, nur wartbar.
        /// </summary>
        internal static async Task AufbauenAsync(string basis, CancellationToken abbruch)
        {
            Stopwatch uhr = Stopwatch.StartNew();

            await SemantikModell.AnstossenUndWarten().ConfigureAwait(false);
            if (!SemantikModell.Bereit)
            {
                Debug.WriteLine("[Semantik] Kein Modell - Index wird nicht gebaut.");
                return;
            }

            Directory.CreateDirectory(Ordner());

            List<string> seiten = await WikiWissen.SeitenlisteAsync(basis, abbruch).ConfigureAwait(false);
            if (seiten.Count == 0)
            {
                Debug.WriteLine("[Semantik] Keine Seitenliste - Index wird nicht gebaut.");
                return;
            }

            List<Abschnitt> alle = new List<Abschnitt>(seiten.Count * 8);

            foreach (string titel in seiten)
            {
                if (abbruch.IsCancellationRequested) return;

                List<Abschnitt> ausDatei = DateiLesen(basis, titel);
                if (ausDatei != null) { alle.AddRange(ausDatei); continue; }

                string text = await WikiWissen.SeitentextAsync(basis, titel, abbruch).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(text)) continue;

                List<Abschnitt> neu = new List<Abschnitt>();
                foreach (Abschnitt a in Zerlegen(titel, text))
                {
                    a.Vektor = SemantikModell.Einbetten(Einbettungstext(a));
                    if (a.Vektor != null) neu.Add(a);
                }

                if (neu.Count == 0) continue;

                DateiSchreiben(basis, titel, neu);
                alle.AddRange(neu);
            }

            lock (_riegel)
            {
                _abschnitte = alle.ToArray();
                _gebautUtc = DateTime.UtcNow;
            }

            Seitenzahl = seiten.Count;
            AufbauzeitMs = uhr.ElapsedMilliseconds;
            Debug.WriteLine("[Semantik] Index fertig: " + alle.Count + " Abschnitte aus " +
                            seiten.Count + " Seiten in " + AufbauzeitMs + " ms.");
        }

        /// <summary>
        /// Was tatsaechlich eingebettet wird: Seitentitel, Abschnittsueberschrift
        /// und Text. Der Titel gehoert dazu - „Eingaben" allein sagt nichts, „Programm
        /// Dokumentation/Brauchwasser - Eingaben" sagt alles.
        /// </summary>
        private static string Einbettungstext(Abschnitt a)
        {
            if (a.IstTitel) return a.Text;

            StringBuilder sb = new StringBuilder();
            sb.Append(a.Titel);
            if (a.Ueberschrift.Length > 0) sb.Append(" - ").Append(a.Ueberschrift);
            sb.Append('\n').Append(a.Text);
            return sb.ToString();
        }

        // ==================================================================
        //  Zerlegen in Abschnitte
        // ==================================================================

        /// <summary>Eine Ueberschriftszeile eines Klartext-Auszugs: <c>== Titel ==</c>.</summary>
        private static readonly Regex UEBERSCHRIFT =
            new Regex(@"^\s*(={2,6})\s*(.+?)\s*\1\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Zerlegt einen Klartext-Auszug in Abschnitte: an jeder Ueberschrift
        /// beginnt einer, zu lange werden an Absatzgrenzen geteilt, zu kurze an
        /// den vorigen gehaengt.
        /// </summary>
        internal static List<Abschnitt> Zerlegen(string titel, string text)
        {
            List<Abschnitt> ergebnis = new List<Abschnitt>();
            if (string.IsNullOrWhiteSpace(text)) return ergebnis;

            ergebnis.Add(Titelabschnitt(titel));

            // ---- 1) An den Ueberschriften trennen.
            List<KeyValuePair<string, StringBuilder>> teile =
                new List<KeyValuePair<string, StringBuilder>>();
            teile.Add(new KeyValuePair<string, StringBuilder>("", new StringBuilder()));

            foreach (string zeile in text.Replace("\r\n", "\n").Split('\n'))
            {
                Match m = UEBERSCHRIFT.Match(zeile);
                if (m.Success)
                {
                    teile.Add(new KeyValuePair<string, StringBuilder>(
                        m.Groups[2].Value.Trim(), new StringBuilder()));
                    continue;
                }

                if (zeile.Trim().Length == 0) teile[teile.Count - 1].Value.Append('\n');
                else teile[teile.Count - 1].Value.Append(zeile.Trim()).Append('\n');
            }

            // ---- 2) Je Teil auf Laenge bringen.
            foreach (KeyValuePair<string, StringBuilder> teil in teile)
            {
                string rumpf = teil.Value.ToString().Trim();
                if (rumpf.Length == 0) continue;

                foreach (string stueck in Stueckeln(rumpf))
                {
                    // Zu kurz und es gibt einen Vorgaenger, in den es passt? Dann
                    // dranhaengen - ein Zweizeiler traegt allein keine Bedeutung.
                    // Der Titelabschnitt nimmt nie etwas auf: er lebt davon, KURZ
                    // zu bleiben.
                    if (stueck.Length < CHUNK_MIN && ergebnis.Count > 0 &&
                        !ergebnis[ergebnis.Count - 1].IstTitel &&
                        ergebnis[ergebnis.Count - 1].Text.Length + stueck.Length + 2 <= CHUNK_MAX)
                    {
                        Abschnitt letzter = ergebnis[ergebnis.Count - 1];
                        letzter.Text = letzter.Text + "\n" + stueck;
                        continue;
                    }

                    ergebnis.Add(new Abschnitt
                    {
                        Titel = titel,
                        Ueberschrift = teil.Key,
                        Text = stueck
                    });
                }
            }

            return ergebnis;
        }

        /// <summary>
        /// Der Kurzname der Seite als eigener, ganz kurzer Eintrag.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum das noetig ist.</b> Das Modell trennt KURZE Texte scharf
        /// (gemessen: „Warmwasserbedarf" gegen „Brauchwasserbedarf" 0,85, gegen
        /// „Photovoltaikmodul" 0,27) und lange Fliesstexte nur noch matt - dort
        /// liegt alles zwischen 0,45 und 0,73, weil jeder laengere Absatz zum
        /// Fachgebiet hin mittelt. Ohne diesen Eintrag gewinnt bei „wie kann der
        /// Warmwasserbedarf angelegt werden" die Prosa ueber das Thema
        /// (Hydraulikschemata, Beispiele) und die BEDIENSEITE „Brauchwasser"
        /// landete gemessen auf Rang 129. Mit ihm steht sie vorn: der Kurzname
        /// ist genau der Kurztext, in dem das Modell stark ist.
        /// </para>
        /// <para>
        /// Eingebettet wird nur das letzte Glied des Titels - das Praefix
        /// „Programm Dokumentation/" steht auf 30 Seiten gleich und traegt
        /// nichts bei.
        /// </para>
        /// </remarks>
        private static Abschnitt Titelabschnitt(string titel)
        {
            string kurz = (titel ?? "").Trim();
            int p = kurz.LastIndexOf('/');
            if (p >= 0 && p + 1 < kurz.Length) kurz = kurz.Substring(p + 1).Trim();

            return new Abschnitt
            {
                Titel = titel,
                Ueberschrift = "",
                Text = kurz,
                IstTitel = true
            };
        }

        /// <summary>
        /// Teilt einen Rumpf an Absatz-, sonst an Satzgrenzen in Stuecke von
        /// hoechstens <see cref="CHUNK_MAX"/> Zeichen.
        /// </summary>
        private static List<string> Stueckeln(string rumpf)
        {
            List<string> stuecke = new List<string>();
            if (rumpf.Length <= CHUNK_MAX) { stuecke.Add(rumpf); return stuecke; }

            StringBuilder aktuell = new StringBuilder();
            foreach (string zeile in rumpf.Split('\n'))
            {
                string z = zeile.Trim();
                if (z.Length == 0) continue;

                foreach (string satz in Saetze(z))
                {
                    if (aktuell.Length > 0 && aktuell.Length + satz.Length + 1 > CHUNK_MAX)
                    {
                        stuecke.Add(aktuell.ToString().Trim());
                        aktuell.Clear();
                    }
                    aktuell.Append(satz).Append(' ');
                }

                if (aktuell.Length > 0) aktuell.Append('\n');
            }

            if (aktuell.ToString().Trim().Length > 0) stuecke.Add(aktuell.ToString().Trim());
            return stuecke;
        }

        /// <summary>
        /// Grobe Satzteilung. Reicht: sie dient nur der Laengenbegrenzung, nicht
        /// der Sprachanalyse - ein falsch getrennter Satz kostet nichts.
        /// </summary>
        private static IEnumerable<string> Saetze(string zeile)
        {
            if (zeile.Length <= CHUNK_MAX) { yield return zeile; yield break; }

            int start = 0;
            for (int i = 0; i < zeile.Length; i++)
            {
                bool ende = (zeile[i] == '.' || zeile[i] == '!' || zeile[i] == '?' ||
                             zeile[i] == ';') &&
                            (i + 1 >= zeile.Length || zeile[i + 1] == ' ');

                if (!ende && i - start < CHUNK_MAX) continue;

                yield return zeile.Substring(start, i - start + 1).Trim();
                start = i + 1;
            }

            if (start < zeile.Length) yield return zeile.Substring(start).Trim();
        }

        // ==================================================================
        //  Suche
        // ==================================================================

        /// <summary>
        /// Die besten Seiten zu einer Frage - je Seite ihr staerkster Abschnitt,
        /// absteigend, nur ueber <see cref="SCHWELLE"/>. Leere Liste, solange
        /// kein Index steht: dann verhaelt sich alles wie vor H10.
        /// </summary>
        /// <remarks>
        /// <b>Zwei Lesarten derselben Frage.</b> Gewertet wird der GROESSERE der
        /// beiden Kosinus: einmal gegen die Frage im Wortlaut, einmal gegen ihre
        /// Stichwortliste nach H9 (<see cref="WikiWissen.Stichwortliste"/>). Die
        /// Fuellwoerter stoeren naemlich auch hier: gemessen an „wie kann der
        /// Warmwasserbedarf angelegt werden" steht die Bedienseite
        /// „Brauchwasser" im Wortlaut auf Rang 24 (0,66), mit der Stichwortkette
        /// „warmwasserbedarf angelegt" auf Rang 1 (0,72). Umgekehrt tragen
        /// laengere Fragen mehr Zusammenhang - deshalb das Maximum und nicht die
        /// eine oder die andere Lesart. Kosten: eine zweite Einbettung, rund
        /// drei Millisekunden.
        /// </remarks>
        internal static List<Fund> Suche(string frage, int hoechstens)
        {
            List<Fund> ergebnis = new List<Fund>();
            if (string.IsNullOrWhiteSpace(frage) || hoechstens <= 0) return ergebnis;

            Abschnitt[] index;
            lock (_riegel) index = _abschnitte;
            if (index == null || index.Length == 0) return ergebnis;

            float[] fragevektor = SemantikModell.Einbetten(frage);
            if (fragevektor == null) return ergebnis;

            string stichworte = WikiWissen.Stichwortliste(frage);
            float[] stichwortvektor =
                string.Equals(stichworte, frage.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? null : SemantikModell.Einbetten(stichworte);

            Dictionary<string, Fund> beste =
                new Dictionary<string, Fund>(StringComparer.OrdinalIgnoreCase);

            foreach (Abschnitt a in index)
            {
                double punkte = SemantikModell.Kosinus(fragevektor, a.Vektor);
                if (stichwortvektor != null)
                    punkte = Math.Max(punkte, SemantikModell.Kosinus(stichwortvektor, a.Vektor));

                if (punkte < SCHWELLE) continue;

                Fund vorhanden;
                if (beste.TryGetValue(a.Titel, out vorhanden))
                {
                    if (punkte <= vorhanden.Punkte) continue;
                    vorhanden.Punkte = punkte;
                    vorhanden.Ueberschrift = a.Ueberschrift;
                    continue;
                }

                beste[a.Titel] = new Fund
                {
                    Titel = a.Titel,
                    Ueberschrift = a.Ueberschrift,
                    Punkte = punkte
                };
            }

            ergebnis.AddRange(beste.Values.OrderByDescending(f => f.Punkte).Take(hoechstens));
            return ergebnis;
        }

        // ==================================================================
        //  Ablage: eine Datei je Seite
        // ==================================================================

        /// <summary>Ablageordner der Indexdateien.</summary>
        public static string Ordner()
        {
            return Dienste.Pfade.Verbinde(Dienste.Pfade.Anwendungsdaten, "semantik", "index");
        }

        /// <summary>Belegter Plattenplatz des Index in Byte.</summary>
        public static long Plattenbedarf()
        {
            try
            {
                if (!Directory.Exists(Ordner())) return 0;
                return Directory.GetFiles(Ordner(), "*.json").Sum(d => new FileInfo(d).Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Semantik] Plattenbedarf: " + ex.Message);
                return 0;
            }
        }

        /// <summary>Dateiname einer Seite - Streuwert ueber Basis-URL und Titel, wie im Textcache.</summary>
        private static string Datei(string basis, string titel)
        {
            using (System.Security.Cryptography.SHA256 hasch =
                       System.Security.Cryptography.SHA256.Create())
            {
                byte[] streu = hasch.ComputeHash(
                    Encoding.UTF8.GetBytes((basis ?? "") + "|" + (titel ?? "")));

                StringBuilder sb = new StringBuilder(32);
                for (int i = 0; i < 16; i++)
                    sb.Append(streu[i].ToString("x2", CultureInfo.InvariantCulture));
                return Path.Combine(Ordner(), sb.ToString() + ".json");
            }
        }

        /// <summary>
        /// Liest die Indexdatei einer Seite; <c>null</c>, wenn es keine gibt, sie
        /// zu alt ist oder von einem anderen Modell stammt.
        /// </summary>
        private static List<Abschnitt> DateiLesen(string basis, string titel)
        {
            try
            {
                string datei = Datei(basis, titel);
                if (!File.Exists(datei)) return null;

                using (FileStream fs = File.OpenRead(datei))
                using (JsonDocument doc = JsonDocument.Parse(fs))
                {
                    JsonElement wurzel = doc.RootElement;

                    if (Text(wurzel, "modell") != Modellmarke()) return null;

                    DateTime abgerufen;
                    if (!DateTime.TryParse(Text(wurzel, "abgerufen"), CultureInfo.InvariantCulture,
                                           DateTimeStyles.AdjustToUniversal |
                                           DateTimeStyles.AssumeUniversal, out abgerufen))
                        return null;

                    if (DateTime.UtcNow - abgerufen > Gueltigkeit) return null;

                    JsonElement liste;
                    if (!wurzel.TryGetProperty("abschnitte", out liste) ||
                        liste.ValueKind != JsonValueKind.Array) return null;

                    List<Abschnitt> ergebnis = new List<Abschnitt>();
                    foreach (JsonElement e in liste.EnumerateArray())
                    {
                        float[] vektor = VektorLesen(Text(e, "vektor"));
                        if (vektor == null) continue;

                        JsonElement marke;
                        bool istTitel = e.TryGetProperty("titelabschnitt", out marke) &&
                                        marke.ValueKind == JsonValueKind.True;

                        ergebnis.Add(new Abschnitt
                        {
                            Titel = titel,
                            Ueberschrift = Text(e, "ueberschrift"),
                            Text = Text(e, "text"),
                            IstTitel = istTitel,
                            Vektor = vektor
                        });
                    }

                    return ergebnis.Count > 0 ? ergebnis : null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Semantik] Indexdatei nicht lesbar: " + ex.Message);
                return null;
            }
        }

        /// <summary>Legt die Abschnitte einer Seite ab. Scheitert das, faellt nichts aus.</summary>
        private static void DateiSchreiben(string basis, string titel, List<Abschnitt> abschnitte)
        {
            try
            {
                Directory.CreateDirectory(Ordner());

                using (FileStream fs = File.Create(Datei(basis, titel)))
                using (Utf8JsonWriter schreiber = new Utf8JsonWriter(fs))
                {
                    schreiber.WriteStartObject();
                    schreiber.WriteString("titel", titel);
                    schreiber.WriteString("quelle", basis);
                    schreiber.WriteString("abgerufen",
                        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                    schreiber.WriteString("modell", Modellmarke());

                    schreiber.WriteStartArray("abschnitte");
                    foreach (Abschnitt a in abschnitte)
                    {
                        schreiber.WriteStartObject();
                        schreiber.WriteString("ueberschrift", a.Ueberschrift);
                        schreiber.WriteString("text", a.Text);
                        if (a.IstTitel) schreiber.WriteBoolean("titelabschnitt", true);
                        schreiber.WriteString("vektor", VektorSchreiben(a.Vektor));
                        schreiber.WriteEndObject();
                    }
                    schreiber.WriteEndArray();

                    schreiber.WriteEndObject();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Semantik] Indexdatei nicht schreibbar: " + ex.Message);
            }
        }

        /// <summary>
        /// Modell und Stand als eine Zeichenkette. Sie steht in jeder Indexdatei;
        /// wechselt das Modell, sind alle Vektoren wertlos und die Dateien werden
        /// verworfen, statt Unsinn zu vergleichen.
        /// </summary>
        private static string Modellmarke()
        {
            return SemantikModell.NAME + "@" + SemantikModell.STAND;
        }

        private static string VektorSchreiben(float[] vektor)
        {
            byte[] rohdaten = new byte[vektor.Length * 4];
            Buffer.BlockCopy(vektor, 0, rohdaten, 0, rohdaten.Length);
            return Convert.ToBase64String(rohdaten);
        }

        private static float[] VektorLesen(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            try
            {
                byte[] rohdaten = Convert.FromBase64String(text);
                if (rohdaten.Length == 0 || rohdaten.Length % 4 != 0) return null;

                float[] vektor = new float[rohdaten.Length / 4];
                Buffer.BlockCopy(rohdaten, 0, vektor, 0, rohdaten.Length);
                return vektor;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Semantik] Vektor nicht lesbar: " + ex.Message);
                return null;
            }
        }

        private static string Text(JsonElement knoten, string name)
        {
            JsonElement wert;
            if (!knoten.TryGetProperty(name, out wert)) return "";
            return wert.ValueKind == JsonValueKind.String ? (wert.GetString() ?? "") : "";
        }

        /// <summary>
        /// Wirft den Index im Speicher weg - ausschliesslich fuer den
        /// Pruefharnisch, der mehrere Aufbauten hintereinander misst.
        /// </summary>
        internal static void Zuruecksetzen()
        {
            lock (_riegel)
            {
                _abschnitte = null;
                _gebautUtc = DateTime.MinValue;
                _fehlversuchUtc = DateTime.MinValue;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Paket H10: das oertliche Einbettungsmodell hinter der semantischen
    /// Doku-Suche - Bezug, Pruefung, Aufwaermen und die eine Rechenoperation
    /// <see cref="Einbetten"/>. Oberflaechenfrei.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nichts verlaesst den Rechner.</b> Eingebettet wird ausschliesslich
    /// hier, in diesem Prozess, auf der CPU. Weder die Frage noch ein Wiki-Text
    /// noch ein Vektor geht irgendwohin - der EINZIGE Netzverkehr dieser Klasse
    /// ist der einmalige Download der beiden Modelldateien von Hugging Face
    /// (ein reiner Dateiabruf ohne Nutzerdaten, siehe <see cref="QUELLE"/>).
    /// An den Modellanbieter geht unveraendert nur, was <see cref="KiChatService"/>
    /// schon vorher gesendet hat.
    /// </para>
    /// <para>
    /// <b>Modellwahl (empirisch, 29.08.2026).</b> Gegeneinander gemessen wurden
    /// <c>intfloat/multilingual-e5-small</c> und
    /// <c>sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2</c>, beide
    /// als int8-ONNX und beide mit demselben XLM-R-Tokenizer. Entschieden hat der
    /// Abstand der Belegpaare: E5 trennte („Akku", „Stromspeicher") von
    /// („Akku", „Heizkessel") nur um 0,008 - viel zu wenig fuer einen Schwellwert -,
    /// das Paraphrase-Modell um 0,42. E5 ist auf Frage-gegen-Absatz trainiert und
    /// legt alle Kurztexte dicht beieinander; gebraucht wird hier aber genau die
    /// SYMMETRISCHE Aehnlichkeit kurzer Fachbegriffe.
    /// </para>
    /// <para>
    /// <b>Tokenizer-Weg (die eigentliche Huerde).</b> XLM-R nummeriert seine
    /// Stuecke NICHT wie die SentencePiece-Datei: fairseq schiebt alles um eins
    /// nach hinten und legt <c>&lt;s&gt;=0 &lt;pad&gt;=1 &lt;/s&gt;=2
    /// &lt;unk&gt;=3</c> davor. <see cref="SentencePieceTokenizer"/> liefert die
    /// ROHEN Nummern; ohne den Versatz rechnet das Modell mit falschen Stuecken -
    /// gemessen sackt der Abstand des ersten Belegpaars von 0,58 auf 0,02.
    /// Der Versatz steht in <see cref="Kennungen"/> und ist gegen
    /// <c>tokenizer.json</c> Stueck fuer Stueck nachgeprueft (53 von 53).
    /// </para>
    /// <para>
    /// <b>Scheitern ist erlaubt.</b> Kein Download, kein Modell, keine
    /// Sitzung - dann bleibt <see cref="Zustand"/> auf
    /// <see cref="Lage.Nichtverfuegbar"/> und alles faellt still auf die
    /// Stichwortsuche zurueck. Ein zweiter Versuch findet in derselben
    /// Programmsitzung NICHT statt (sonst haengt jede Frage an einem
    /// vergeblichen Netzabruf); beim naechsten Start faengt es von vorn an.
    /// </para>
    /// </remarks>
    public static class SemantikModell
    {
        // ==================================================================
        //  Herkunft - versionsgepinnt, mit Pruefsumme
        // ==================================================================

        /// <summary>Der Stand (Commit) im Modell-Verzeichnis, auf den die Adressen zeigen.</summary>
        public const string STAND = "e8f8c211226b894fcb81acc59f3b34ba3efd5f42";

        /// <summary>Name des Modells - erscheint in der Komponentenliste und im Tooltip.</summary>
        public const string NAME = "paraphrase-multilingual-MiniLM-L12-v2";

        /// <summary>Lizenz des Modells.</summary>
        public const string LIZENZ = "Apache-2.0";

        /// <summary>
        /// Menschenlesbare Herkunftsangabe fuer Tooltip und Protokoll. Die Lizenz
        /// steht hier NICHT noch einmal - die Anzeigezeile
        /// (<c>KI_SEMANTIK_HERKUNFT</c>) fuehrt sie als eigene Angabe.
        /// </summary>
        public const string QUELLE =
            "huggingface.co/sentence-transformers/" + NAME + ", Stand " + STAND;

        /// <summary>
        /// Verzeichnisadresse der beiden Dateien. Kein Schreibzugriff aus dem
        /// Programm heraus - <c>internal</c> allein fuer den Pruefharnisch, der
        /// hier eine unbrauchbare Adresse einsetzt, um den Rueckfall zu messen.
        /// </summary>
        internal static string Quellbasis =
            "https://huggingface.co/sentence-transformers/" + NAME + "/resolve/" + STAND + "/";

        /// <summary>Die beiden Dateien: Adresszusatz, oertlicher Name, Groesse, SHA-256.</summary>
        private static readonly Bezugsdatei[] DATEIEN =
        {
            new Bezugsdatei("onnx/model_quint8_avx2.onnx", "modell.onnx", 118453870L,
                "98a01d88b7de996cdea58c32ca71208c09968d143798814b2ea09d3439dc334f"),
            new Bezugsdatei("sentencepiece.bpe.model", "tokenizer.model", 5069051L,
                "cfc8146abe2a0488e9e2a0c56de7952f7c11ab059eca145a0a727afce0db2865")
        };

        private sealed class Bezugsdatei
        {
            public readonly string Zusatz, Ortsname, Streuwert;
            public readonly long Groesse;

            public Bezugsdatei(string zusatz, string ortsname, long groesse, string streuwert)
            {
                Zusatz = zusatz; Ortsname = ortsname; Groesse = groesse; Streuwert = streuwert;
            }
        }

        // ==================================================================
        //  Zustand
        // ==================================================================

        /// <summary>Die vier Zustaende, in denen das Modell sein kann.</summary>
        public enum Lage
        {
            /// <summary>Noch nicht angefasst.</summary>
            Aus,
            /// <summary>Wird geholt oder geladen.</summary>
            Laedt,
            /// <summary>Einsatzbereit.</summary>
            Bereit,
            /// <summary>In dieser Programmsitzung nicht mehr zu haben.</summary>
            Nichtverfuegbar
        }

        private static readonly object _riegel = new object();
        private static Lage _zustand = Lage.Aus;
        private static Task _vorbereitung;
        private static InferenceSession _sitzung;
        private static SentencePieceTokenizer _tokenizer;

        /// <summary>Der aktuelle Zustand - die Grundlage der Statuszeile im Chatfenster.</summary>
        public static Lage Zustand { get { lock (_riegel) return _zustand; } }

        /// <summary>Kurzform: kann <see cref="Einbetten"/> gerade rechnen?</summary>
        public static bool Bereit { get { return Zustand == Lage.Bereit; } }

        /// <summary>Grund des Scheiterns - reine Diagnose, nie in der Oberflaeche.</summary>
        public static string LetzterFehler { get; private set; } = "";

        /// <summary>Millisekunden vom Anstoss bis zur fertigen Sitzung - fuer das Protokoll.</summary>
        public static long AufwaermzeitMs { get; private set; }

        // ==================================================================
        //  Vorbereitung
        // ==================================================================

        /// <summary>
        /// Stoesst Bezug und Aufwaermen an, wenn beides noch nicht laeuft, und
        /// kehrt SOFORT zurueck. Der Aufrufer wartet nie - was fertig ist, wird
        /// benutzt, was nicht fertig ist, gibt es eben noch nicht.
        /// </summary>
        public static void Anstossen()
        {
            lock (_riegel)
            {
                if (_zustand == Lage.Bereit || _zustand == Lage.Laedt ||
                    _zustand == Lage.Nichtverfuegbar) return;

                _zustand = Lage.Laedt;
                _vorbereitung = Task.Run((Action)Vorbereiten);
            }
        }

        /// <summary>
        /// Dasselbe, aber wartbar - der Weg des Indexaufbaus und des
        /// Pruefharnischs. Gibt es nichts vorzubereiten, ist die Aufgabe sofort
        /// fertig.
        /// </summary>
        internal static Task AnstossenUndWarten()
        {
            Anstossen();
            lock (_riegel) return _vorbereitung ?? Task.CompletedTask;
        }

        private static void Vorbereiten()
        {
            Stopwatch uhr = Stopwatch.StartNew();
            try
            {
                Directory.CreateDirectory(Ordner());

                foreach (Bezugsdatei d in DATEIEN) Sicherstellen(d);

                SessionOptions einstellungen = new SessionOptions();
                // Bewusst genuegsam: der Indexaufbau laeuft im Hintergrund, waehrend
                // der Anwender weiterarbeitet - er darf die Maschine nicht besetzen.
                einstellungen.IntraOpNumThreads = 2;
                einstellungen.InterOpNumThreads = 1;

                InferenceSession sitzung =
                    new InferenceSession(Path.Combine(Ordner(), DATEIEN[0].Ortsname), einstellungen);

                SentencePieceTokenizer tokenizer;
                using (FileStream fs = File.OpenRead(Path.Combine(Ordner(), DATEIEN[1].Ortsname)))
                    tokenizer = SentencePieceTokenizer.Create(fs, false, false);

                lock (_riegel)
                {
                    _sitzung = sitzung;
                    _tokenizer = tokenizer;
                    _zustand = Lage.Bereit;
                }

                AufwaermzeitMs = uhr.ElapsedMilliseconds;
                Debug.WriteLine("[Semantik] Modell bereit nach " + AufwaermzeitMs + " ms.");
            }
            catch (Exception ex)
            {
                LetzterFehler = ex.Message;
                lock (_riegel) _zustand = Lage.Nichtverfuegbar;
                Debug.WriteLine("[Semantik] Modell nicht verfuegbar: " + ex.Message);
            }
        }

        /// <summary>
        /// Legt eine Datei bereit: vorhanden und pruefsummengleich -&gt; nichts zu
        /// tun; sonst holen, pruefen, umbenennen. Eine Datei falscher Pruefsumme
        /// wird verworfen, nicht benutzt.
        /// </summary>
        private static void Sicherstellen(Bezugsdatei d)
        {
            string ziel = Path.Combine(Ordner(), d.Ortsname);

            if (File.Exists(ziel))
            {
                if (Streuwert(ziel) == d.Streuwert) return;
                Debug.WriteLine("[Semantik] " + d.Ortsname + " hat die falsche Pruefsumme - neu holen.");
                File.Delete(ziel);
            }

            string zwischen = ziel + ".teil";
            try
            {
                using (HttpClient klient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
                using (Stream quelle = klient.GetStreamAsync(Quellbasis + d.Zusatz).GetAwaiter().GetResult())
                using (FileStream senke = File.Create(zwischen))
                    quelle.CopyTo(senke, 1 << 20);

                long ist = new FileInfo(zwischen).Length;
                if (ist != d.Groesse)
                    throw new InvalidDataException(d.Ortsname + ": " + ist + " statt " + d.Groesse + " Byte.");

                string streu = Streuwert(zwischen);
                if (!string.Equals(streu, d.Streuwert, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(d.Ortsname + ": SHA-256 " + streu + " statt " + d.Streuwert + ".");

                File.Move(zwischen, ziel);
                Debug.WriteLine("[Semantik] " + d.Ortsname + " geholt und geprueft (" + ist + " Byte).");
            }
            finally
            {
                try { if (File.Exists(zwischen)) File.Delete(zwischen); }
                catch (Exception ex) { Debug.WriteLine("[Semantik] Teildatei bleibt liegen: " + ex.Message); }
            }
        }

        private static string Streuwert(string datei)
        {
            using (SHA256 hasch = SHA256.Create())
            using (FileStream fs = File.OpenRead(datei))
                return BitConverter.ToString(hasch.ComputeHash(fs))
                                   .Replace("-", "").ToLowerInvariant();
        }

        /// <summary>Ablageordner der beiden Modelldateien.</summary>
        public static string Ordner()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "wp-plan", "semantik", "modell");
        }

        /// <summary>Belegter Plattenplatz der Modelldateien in Byte; 0, wenn nichts liegt.</summary>
        public static long Plattenbedarf()
        {
            long summe = 0;
            try
            {
                foreach (Bezugsdatei d in DATEIEN)
                {
                    string p = Path.Combine(Ordner(), d.Ortsname);
                    if (File.Exists(p)) summe += new FileInfo(p).Length;
                }
            }
            catch (Exception ex) { Debug.WriteLine("[Semantik] Plattenbedarf: " + ex.Message); }
            return summe;
        }

        // ==================================================================
        //  Einbetten
        // ==================================================================

        /// <summary>So viele Stuecke gehen hoechstens ins Modell.</summary>
        private const int MAX_STUECKE = 256;

        /// <summary>
        /// Der Einbettungsvektor eines Textes, auf Laenge 1 gebracht (dadurch ist
        /// das Skalarprodukt zweier Vektoren unmittelbar der Kosinus).
        /// <c>null</c>, solange das Modell nicht bereit ist oder etwas schiefgeht -
        /// der Aufrufer prueft auf <c>null</c> und macht ohne Semantik weiter.
        /// </summary>
        public static float[] Einbetten(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            InferenceSession sitzung;
            SentencePieceTokenizer tokenizer;
            lock (_riegel)
            {
                if (_zustand != Lage.Bereit) return null;
                sitzung = _sitzung;
                tokenizer = _tokenizer;
            }

            try
            {
                long[] kennungen = Kennungen(tokenizer, text);
                int n = kennungen.Length;

                long[] maske = new long[n];
                for (int i = 0; i < n; i++) maske[i] = 1L;

                List<NamedOnnxValue> eingaben = new List<NamedOnnxValue>(3)
                {
                    NamedOnnxValue.CreateFromTensor("input_ids",
                        new DenseTensor<long>(kennungen, new[] { 1, n })),
                    NamedOnnxValue.CreateFromTensor("attention_mask",
                        new DenseTensor<long>(maske, new[] { 1, n }))
                };
                if (sitzung.InputMetadata.ContainsKey("token_type_ids"))
                    eingaben.Add(NamedOnnxValue.CreateFromTensor("token_type_ids",
                        new DenseTensor<long>(new long[n], new[] { 1, n })));

                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> ergebnis =
                           sitzung.Run(eingaben))
                {
                    Tensor<float> t = ergebnis.First().AsTensor<float>();
                    int breite = t.Dimensions[2];

                    // Mittelwert ueber alle Stuecke (die Maske ist ueberall 1, es
                    // wird ohne Fuellstellen gerechnet) - die Pooling-Vorschrift
                    // dieses Modells (1_Pooling/config.json: mean).
                    float[] vektor = new float[breite];
                    for (int i = 0; i < n; i++)
                        for (int j = 0; j < breite; j++) vektor[j] += t[0, i, j];

                    double laenge = 0;
                    for (int j = 0; j < breite; j++)
                    {
                        vektor[j] /= n;
                        laenge += vektor[j] * (double)vektor[j];
                    }

                    laenge = Math.Sqrt(laenge);
                    if (laenge <= 0) return null;
                    for (int j = 0; j < breite; j++) vektor[j] = (float)(vektor[j] / laenge);
                    return vektor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Semantik] Einbetten fehlgeschlagen: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Die Stueck-Nummern eines Textes in der Zaehlweise des Modells.
        /// </summary>
        /// <remarks>
        /// <b>Der Versatz ist der springende Punkt.</b> Die SentencePiece-Datei
        /// zaehlt <c>&lt;unk&gt;=0 &lt;s&gt;=1 &lt;/s&gt;=2</c> und dann die
        /// Stuecke; XLM-R/fairseq zaehlt <c>&lt;s&gt;=0 &lt;pad&gt;=1
        /// &lt;/s&gt;=2 &lt;unk&gt;=3</c> und dann die Stuecke - jedes gewoehnliche
        /// Stueck liegt also um genau eins hoeher. Nachgeprueft gegen die
        /// <c>tokenizer.json</c> desselben Standes: 53 von 53 Stuecken eines
        /// deutschen Probesatzes, kein Ausreisser.
        /// </remarks>
        private static long[] Kennungen(SentencePieceTokenizer tokenizer, string text)
        {
            IReadOnlyList<int> roh = tokenizer.EncodeToIds(text);

            int anzahl = Math.Min(roh.Count, MAX_STUECKE - 2);
            long[] kennungen = new long[anzahl + 2];

            kennungen[0] = 0L;                                   // <s>
            for (int i = 0; i < anzahl; i++)
                kennungen[i + 1] = roh[i] == 0 ? 3L : roh[i] + 1L;   // <unk> bzw. Versatz
            kennungen[anzahl + 1] = 2L;                          // </s>

            return kennungen;
        }

        /// <summary>Der Kosinus zweier bereits normierter Vektoren.</summary>
        public static double Kosinus(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0;

            double summe = 0;
            for (int i = 0; i < a.Length; i++) summe += a[i] * (double)b[i];
            return summe;
        }

        /// <summary>
        /// Setzt den Zustand zurueck - ausschliesslich fuer den Pruefharnisch,
        /// der Bezug und Rueckfall mehrfach hintereinander messen muss.
        /// </summary>
        internal static void Zuruecksetzen()
        {
            lock (_riegel)
            {
                try { if (_sitzung != null) _sitzung.Dispose(); }
                catch (Exception ex) { Debug.WriteLine("[Semantik] Sitzung schliessen: " + ex.Message); }

                _sitzung = null;
                _tokenizer = null;
                _vorbereitung = null;
                _zustand = Lage.Aus;
                LetzterFehler = "";
            }
        }

        /// <summary>Anzahl der Merkmale eines Vektors, sobald einer vorliegt.</summary>
        internal static int Breite
        {
            get
            {
                lock (_riegel)
                {
                    if (_zustand != Lage.Bereit || _sitzung == null) return 0;
                    int[] mass = _sitzung.OutputMetadata.Values.First().Dimensions;
                    return mass[mass.Length - 1];
                }
            }
        }
    }
}

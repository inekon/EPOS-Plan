using System;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Ausnahmeprotokoll</b> — jede ausgelöste .NET-Ausnahme mit Aufrufstapel, SOFORT
    /// in eine Textdatei geschrieben (Anwenderauftrag 30.09.2026).
    ///
    /// <para><b>Wozu.</b> Eine Ausnahme, die aus einem Rückruf der WebView2 herausläuft,
    /// beendet den Prozess mit <c>0xc000041d</c> (STATUS_FATAL_USER_CALLBACK_EXCEPTION)
    /// über <c>KERNELBASE.dll</c> — ohne Meldung, ohne Eintrag „.NET Runtime“, ohne
    /// Aufrufstapel (der WinForms-BlazorWebView führt kein <c>UnhandledException</c>-Ereignis,
    /// siehe <c>BlazorDialogForm</c>). Kein Abschlusscode läuft mehr. Deshalb schreibt dieses
    /// Protokoll schon beim AUSLÖSEN (<see cref="AppDomain.FirstChanceException"/>): Was den
    /// Prozess gleich darauf beendet, steht dann bereits in der Datei.</para>
    ///
    /// <para><b>Was es schreibt.</b> Je Ausnahme Zeit, Art, Faden, Typ, Meldung, die inneren
    /// Ausnahmen und den Aufrufstapel am Auslöseort. Auch abgefangene Ausnahmen erscheinen —
    /// die letzte vor einem Absturz ist die gesuchte. Dazu unbehandelte Ausnahmen der
    /// Anwendungsdomäne und unbeobachtete Aufgaben; deren übliche Behandlung bleibt
    /// unverändert (kein <c>SetObserved</c>, kein <c>Application.ThreadException</c>, das den
    /// Fehlerdialog von WinForms ersetzen würde).</para>
    ///
    /// <para><b>Grenzen.</b> Höchstens <see cref="HOECHSTE_JE_SEKUNDE"/> Einträge je Sekunde,
    /// der Rest wird gezählt und beim nächsten Eintrag genannt; über
    /// <see cref="HOECHSTGROESSE"/> Byte wandert die Datei nach <see cref="DATEI_ALT"/>
    /// (die vorige Fassung wird ersetzt). Ein Fehler beim Schreiben ist nie der Grund eines
    /// Fehlers: Alles hier ist still, und eine Ausnahme im Protokoll selbst löst keinen
    /// zweiten Eintrag aus.</para>
    ///
    /// <para><b>Plattformfrei.</b> Den Ordner reicht die Schale herein (Windows: <c>Logs</c>
    /// neben der Datenbank); der Kern kennt keinen Sonderordner.</para>
    /// </summary>
    public sealed class Ausnahmeprotokoll
    {
        /// <summary>Dateiname des Protokolls im Ordner.</summary>
        public const string DATEI = "Ausnahmen.txt";

        /// <summary>Dateiname der vorigen Fassung nach dem Umlauf.</summary>
        public const string DATEI_ALT = "Ausnahmen.alt.txt";

        /// <summary>Größe [Byte], ab der die Datei umläuft.</summary>
        public const long HOECHSTGROESSE = 200 * 1024;

        /// <summary>Einträge je Sekunde, darüber wird nur gezählt.</summary>
        public const int HOECHSTE_JE_SEKUNDE = 20;

        private static readonly object EinschaltSperre = new object();
        private static Ausnahmeprotokoll _eingeschaltet;

        [ThreadStatic] private static bool _imSchreiben;

        private readonly object _sperre = new object();
        private readonly Func<string> _ordnerQuelle;
        private readonly Func<DateTime> _uhr;
        private string _ordner;
        private long _sekunde = long.MinValue;
        private int _inSekunde;
        private int _gedrosselt;

        /// <summary>Ein Protokoll in den Ordner, den <paramref name="ordnerQuelle"/> beim ersten Eintrag nennt.</summary>
        /// <param name="ordnerQuelle">Der Zielordner; erst beim ersten Eintrag gefragt, damit ihn die Schale aus den Einstellungen bilden kann. Leer oder nicht beschreibbar: der Temp-Ordner.</param>
        /// <param name="uhr">Die Uhr (Tests); Vorgabe <see cref="DateTime.Now"/>.</param>
        public Ausnahmeprotokoll(Func<string> ordnerQuelle, Func<DateTime> uhr = null)
        {
            _ordnerQuelle = ordnerQuelle;
            _uhr = uhr ?? (() => DateTime.Now);
        }

        /// <summary>Der Pfad der Protokolldatei — gebildet beim ersten Zugriff.</summary>
        public string Datei => Path.Combine(Ordner(), DATEI);

        /// <summary>
        /// Schaltet das Protokoll für den Prozess ein — einmal; ein zweiter Aufruf gibt das
        /// bestehende zurück. Schreibt eine Kopfzeile, damit die Läufe in der Datei getrennt stehen.
        /// </summary>
        public static Ausnahmeprotokoll Einschalten(Func<string> ordnerQuelle, string kopfzeile)
        {
            lock (EinschaltSperre)
            {
                if (_eingeschaltet != null) return _eingeschaltet;

                var p = new Ausnahmeprotokoll(ordnerQuelle);
                AppDomain.CurrentDomain.FirstChanceException += p.BeiAusnahme;
                AppDomain.CurrentDomain.UnhandledException +=
                    (s, e) => p.Schreiben("UNBEHANDELT", e.ExceptionObject as Exception, null);
                TaskScheduler.UnobservedTaskException +=
                    (s, e) => p.Schreiben("UNBEOBACHTETE AUFGABE", e.Exception, null);
                p.Kopf(kopfzeile);
                _eingeschaltet = p;
                return p;
            }
        }

        private void BeiAusnahme(object sender, FirstChanceExceptionEventArgs e)
        {
            // Der Stapel am Auslöseort: Der Handler läuft auf dem Faden und über dem Stapel
            // der Stelle, die gerade wirft.
            if (_imSchreiben) return;
            string stapel;
            try { stapel = Environment.StackTrace; }
            catch { stapel = null; }
            Schreiben("AUSGELOEST", e.Exception, stapel);
        }

        /// <summary>Schreibt die Kopfzeile eines Laufs.</summary>
        public void Kopf(string zeile)
        {
            bool vorher = _imSchreiben;
            _imSchreiben = true;
            try
            {
                Anhaengen(Environment.NewLine + "=== " + Zeit() + " " + (zeile ?? "") + " ===" + Environment.NewLine,
                          gedrosselt: false);
            }
            catch { /* still */ }
            finally { _imSchreiben = vorher; }
        }

        /// <summary>Ein Eintrag — gedrosselt, still, ohne Rückkopplung.</summary>
        /// <param name="art">„AUSGELOEST“, „UNBEHANDELT“, „UNBEOBACHTETE AUFGABE“.</param>
        /// <param name="ausnahme">Die Ausnahme; <c>null</c> = nichts zu schreiben.</param>
        /// <param name="stapel">Der Aufrufstapel am Auslöseort; <c>null</c> = der der Ausnahme.</param>
        public void Schreiben(string art, Exception ausnahme, string stapel)
        {
            if (ausnahme == null || _imSchreiben) return;
            _imSchreiben = true;
            try
            {
                Anhaengen(Text(art, ausnahme, stapel), gedrosselt: true);
            }
            catch { /* ein Protokoll darf nie der Grund eines Fehlers sein */ }
            finally { _imSchreiben = false; }
        }

        /// <summary>Der Text eines Eintrags — ohne Zeitstempel-Kopf der Drosselung.</summary>
        internal string Text(string art, Exception ausnahme, string stapel)
        {
            var sb = new StringBuilder();
            sb.Append(Zeit()).Append(" [").Append(art).Append("] Faden ")
              .Append(Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture))
              .Append(": ").Append(ausnahme.GetType().FullName).Append(": ").AppendLine(ausnahme.Message);

            int tiefe = 0;
            for (Exception innen = ausnahme.InnerException; innen != null && tiefe < 5; innen = innen.InnerException, tiefe++)
                sb.Append("  innen: ").Append(innen.GetType().FullName).Append(": ").AppendLine(innen.Message);

            string s = string.IsNullOrEmpty(stapel) ? ausnahme.StackTrace : stapel;
            if (!string.IsNullOrEmpty(s)) sb.AppendLine("  Stapel:").AppendLine(s);
            return sb.ToString();
        }

        private void Anhaengen(string text, bool gedrosselt)
        {
            lock (_sperre)
            {
                if (gedrosselt)
                {
                    long sekunde = _uhr().Ticks / TimeSpan.TicksPerSecond;
                    if (sekunde != _sekunde) { _sekunde = sekunde; _inSekunde = 0; }
                    if (++_inSekunde > HOECHSTE_JE_SEKUNDE) { _gedrosselt++; return; }
                    if (_gedrosselt > 0)
                    {
                        text = "  (" + _gedrosselt.ToString(CultureInfo.InvariantCulture) +
                               " weitere Ausnahmen gedrosselt)" + Environment.NewLine + text;
                        _gedrosselt = 0;
                    }
                }

                try
                {
                    string datei = Datei;
                    var info = new FileInfo(datei);
                    if (info.Exists && info.Length > HOECHSTGROESSE)
                        File.Move(datei, Path.Combine(Ordner(), DATEI_ALT), overwrite: true);
                    File.AppendAllText(datei, text, new UTF8Encoding(false));
                }
                catch { /* still */ }
            }
        }

        private string Ordner()
        {
            if (_ordner != null) return _ordner;

            // Auch das Bilden des Ordners darf keinen Eintrag auslösen: Eine Ausnahme hier
            // (Einstellungen, Rechte) käme sonst über BeiAusnahme wieder hierher.
            bool vorher = _imSchreiben;
            _imSchreiben = true;
            try { return OrdnerBilden(); }
            finally { _imSchreiben = vorher; }
        }

        private string OrdnerBilden()
        {
            string ordner = null;
            try { ordner = _ordnerQuelle?.Invoke(); } catch { ordner = null; }

            foreach (string kandidat in new[] { ordner, Path.Combine(Path.GetTempPath(), "EPOS_PLAN", "Logs") })
            {
                if (string.IsNullOrWhiteSpace(kandidat)) continue;
                try
                {
                    Directory.CreateDirectory(kandidat);
                    _ordner = kandidat;
                    return _ordner;
                }
                catch { /* nächster Kandidat */ }
            }
            _ordner = Path.GetTempPath();
            return _ordner;
        }

        private string Zeit() => _uhr().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}

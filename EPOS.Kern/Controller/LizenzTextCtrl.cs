using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Woher der Vertragstext kommt (iU9-W15c.8) — die kernfähige Hälfte von
    /// <c>Form_Lizenz</c>.
    ///
    /// <para><b>Drei Quellen, in dieser Reihenfolge:</b> eine örtliche Vertragsdatei,
    /// sonst der Zwischenspeicher der zuletzt geholten Fassung, sonst die
    /// Online-Fassung von <c>epos-plan.de</c>. Der Vorläufer entschied das in
    /// <c>LizenzLaden</c> (<c>:307-358</c>) mitten in der Oberfläche; hier ist es eine
    /// Frage an den Kern.</para>
    ///
    /// <para><b>Die Quelle ist EINE Zeile</b> (<see cref="ONLINE_QUELLE"/>, Auflage
    /// E-17). Heute ist es bitgleich die AGB-<b>Seite</b> über die
    /// WordPress-Schnittstelle. Der Lizenzserver bietet seit <c>epos-lizenz</c> 1.4.0
    /// zusätzlich <c>GET epos/v1/vertrag</c> — je Tarif Stand, SHA-256 und URL des
    /// Dokuments, das der Kunde im Checkout akzeptiert hat (Befund W15c-B27). Das ist
    /// die fachlich richtigere Quelle; die Umstellung ist eine Anwenderentscheidung und
    /// nicht Teil dieser Welle. Sie kostet dann genau diese eine Zeile.</para>
    ///
    /// <para><b>Nichts hier kennt eine Oberfläche.</b> Die Dateiwahl gehört der
    /// Plattform (<c>Dienste.Datei</c>), der gemerkte Pfad den Einstellungen
    /// (<c>Dienste.Einstellungen</c> — unter Windows derselbe Registry-Zweig
    /// <c>HKCU\Software\wp-plan</c> wie im Bestand, Befund W15c-B17), und der
    /// Zwischenspeicher liegt neben den übrigen Lizenzdaten
    /// (<c>Dienste.Pfade.Anwendungsdaten</c>).</para>
    /// </summary>
    internal static class LizenzTextCtrl
    {
        /// <summary>Dateinamen, nach denen gesucht wird (in dieser Reihenfolge).</summary>
        private static readonly string[] DATEINAMEN =
        {
            "LIZENZ-INEKON.rtf",
            "LIZENZVEREINBARUNG UND ALLGEMEINE GESCHÄFTSBEDINGUNGEN- Wärmeplan.docx"
        };

        /// <summary>Der Einstellungsname des vom Anwender gewählten Pfades.</summary>
        internal const string EINSTELLUNG_LIZENZDATEI = "LizenzDatei";

        /// <summary>Die jeweils geltende Fassung steht online; die App zeigt eine Kopie.</summary>
        internal const string ONLINE_FASSUNG = "https://epos-plan.de/agb/";

        /// <summary>
        /// Dieselbe Seite über die WordPress-Schnittstelle — sie liefert den reinen
        /// Vertragstext ohne Menü und Fußbereich, dazu das Änderungsdatum.
        /// <b>Die eine Zeile, die E-17 betrifft.</b>
        /// </summary>
        internal const string ONLINE_QUELLE =
            "https://epos-plan.de/wp-json/wp/v2/pages?slug=agb&_fields=modified,content";

        /// <summary>
        /// Kürzer als so viele Zeichen ist kein Vertragstext — dann lieber den
        /// vorhandenen Stand behalten als ihn durch Bruchstücke ersetzen
        /// (<c>Form_Lizenz.cs:585-587</c>).
        /// </summary>
        internal const int MINDESTLAENGE = 2000;

        private const string ZWISCHEN_TEXT = "lizenztext.txt";
        private const string ZWISCHEN_STAND = "lizenztext-stand.txt";

        // ==================================================================
        //  Der gewählte Pfad
        // ==================================================================

        /// <summary>Zuletzt gewählter Pfad der Lizenzdatei; leer, wenn keiner gemerkt ist.</summary>
        internal static string GewaehltenPfadLesen()
        {
            try { return Dienste.Einstellungen.Lies(EINSTELLUNG_LIZENZDATEI) ?? ""; }
            catch { return ""; }
        }

        /// <summary>Merkt den gewählten Pfad, damit die Datei beim nächsten Öffnen sofort da ist.</summary>
        internal static void GewaehltenPfadSpeichern(string pfad)
        {
            try { Dienste.Einstellungen.Schreib(EINSTELLUNG_LIZENZDATEI, pfad ?? ""); }
            catch { }
        }

        // ==================================================================
        //  Die Dateisuche
        // ==================================================================

        /// <summary>
        /// Durchsucht die üblichen Ablageorte nach der Vertragsdatei; <c>null</c>, wenn
        /// keine gefunden wurde.
        /// </summary>
        /// <remarks>
        /// Die Reihenfolge ist unverändert (<c>Form_Lizenz.DateiSuchen</c>,
        /// <c>:760-818</c>): 1. der ausdrücklich gewählte Pfad — er hat Vorrang, weil
        /// er irgendwo liegen kann; 2. das Programmverzeichnis und bis zu SECHS Ebenen
        /// darüber (<c>bin\x64\Release\net…</c> → Projektstamm); 3. der gemeinsame und
        /// der benutzereigene Anwendungsordner. Je Ordner die zwei Namen.
        /// </remarks>
        internal static string DateiSuchen()
        {
            string gewaehlt = GewaehltenPfadLesen();
            if (!string.IsNullOrEmpty(gewaehlt))
            {
                try { if (File.Exists(gewaehlt)) return gewaehlt; }
                catch { }
            }

            var ordner = new List<string>();
            try
            {
                string basis = AppDomain.CurrentDomain.BaseDirectory;
                ordner.Add(basis);

                DirectoryInfo di = new DirectoryInfo(basis);
                for (int i = 0; i < 6 && di.Parent != null; i++)
                {
                    di = di.Parent;
                    ordner.Add(di.FullName);
                }
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(Dienste.Pfade.Gemeinsam)) ordner.Add(Dienste.Pfade.Gemeinsam);
                if (!string.IsNullOrEmpty(Dienste.Pfade.BenutzerLokal)) ordner.Add(Dienste.Pfade.BenutzerLokal);
            }
            catch { }

            foreach (string o in ordner)
            {
                if (string.IsNullOrEmpty(o)) continue;
                foreach (string name in DATEINAMEN)
                {
                    try
                    {
                        string pfad = Path.Combine(o, name);
                        if (File.Exists(pfad)) return pfad;
                    }
                    catch { }
                }
            }

            return null;
        }

        // ==================================================================
        //  Der Zwischenspeicher
        // ==================================================================

        /// <summary>Ablage neben den übrigen Lizenzdaten (<c>%APPDATA%\wp-plan</c>).</summary>
        private static string ZwischenspeicherDatei(string name)
            => Path.Combine(Dienste.Pfade.Unterordner(Dienste.Pfade.Anwendungsdaten), name);

        /// <summary>Der zuletzt geholte Text samt Stand; leer, wenn noch keiner da ist.</summary>
        internal static string ZwischenspeicherLesen(out string stand)
        {
            stand = null;
            try
            {
                string textdatei = ZwischenspeicherDatei(ZWISCHEN_TEXT);
                if (!File.Exists(textdatei)) return "";

                string standdatei = ZwischenspeicherDatei(ZWISCHEN_STAND);
                if (File.Exists(standdatei)) stand = File.ReadAllText(standdatei).Trim();

                return File.ReadAllText(textdatei);
            }
            catch { return ""; }
        }

        /// <summary>Legt den geholten Text ab. Ein Fehlschlag bleibt folgenlos.</summary>
        internal static void ZwischenspeicherSchreiben(string text, string stand)
        {
            try
            {
                File.WriteAllText(ZwischenspeicherDatei(ZWISCHEN_TEXT), text);
                File.WriteAllText(ZwischenspeicherDatei(ZWISCHEN_STAND), stand ?? "");
            }
            catch { }
        }

        // ==================================================================
        //  Die Online-Fassung
        // ==================================================================

        /// <summary>
        /// Holt die geltende Fassung von <c>epos-plan.de</c> und legt sie örtlich ab.
        /// Scheitert der Abruf, bleibt der zuletzt geholte Stand stehen — der Dialog
        /// soll auch ohne Netz etwas zeigen. Übertragen wird nichts außer dem
        /// Seitenabruf.
        /// </summary>
        /// <returns>
        /// Text und Stand; <c>Text == ""</c> heißt „nichts Brauchbares geholt", und
        /// dann bleibt alles, wie es war.
        /// </returns>
        internal static async Task<(string Text, string Stand)> OnlineFassungHolen()
        {
            try
            {
                string json;
                using (HttpClient http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(20);
                    http.DefaultRequestHeaders.Add("User-Agent", "EPOS-Plan");
                    json = await http.GetStringAsync(ONLINE_QUELLE).ConfigureAwait(false);
                }

                (string text, string stand) = AntwortLesen(json);

                // Ein paar Zeilen waeren kein Vertragstext.
                if (text.Length < MINDESTLAENGE) return ("", null);

                ZwischenspeicherSchreiben(text, stand);
                return (text, stand);
            }
            catch
            {
                // ohne Netz bleibt der Zwischenspeicher stehen
                return ("", null);
            }
        }

        /// <summary>
        /// Die Antwort der WordPress-Schnittstelle auswerten — getrennt vom Abruf,
        /// damit sie ohne Netz prüfbar ist.
        /// </summary>
        internal static (string Text, string Stand) AntwortLesen(string json)
        {
            string text = "";
            string stand = null;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement wurzel = doc.RootElement;
                if (wurzel.ValueKind != JsonValueKind.Array || wurzel.GetArrayLength() == 0)
                    return ("", null);

                JsonElement seite = wurzel[0];
                if (seite.TryGetProperty("content", out JsonElement inhalt) &&
                    inhalt.TryGetProperty("rendered", out JsonElement gerendert))
                {
                    text = HtmlZuText(gerendert.GetString());
                }
                if (seite.TryGetProperty("modified", out JsonElement geaendert))
                {
                    stand = StandFormatieren(geaendert.GetString());
                }
            }
            catch { return ("", null); }

            return (text, stand);
        }

        // ==================================================================
        //  Zwei reine Funktionen
        // ==================================================================

        /// <summary>HTML der Vertragsseite in lesbaren Fließtext umsetzen.</summary>
        internal static string HtmlZuText(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            string s = html;
            s = Regex.Replace(s, @"(?is)<(script|style)[^>]*>.*?</\1>", "");
            s = Regex.Replace(s, @"(?i)<br\s*/?>", "\n");
            s = Regex.Replace(s, @"(?i)<li[^>]*>", "  - ");
            s = Regex.Replace(s, @"(?i)<h[1-6][^>]*>", "\n");
            s = Regex.Replace(s, @"(?i)</(p|div|li|tr|h[1-6])>", "\n");
            s = Regex.Replace(s, @"<[^>]+>", "");
            s = System.Net.WebUtility.HtmlDecode(s);

            s = s.Replace("\r\n", "\n").Replace('\r', '\n');
            s = Regex.Replace(s, @"[ \t]+\n", "\n");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            return s.Trim().Replace("\n", Environment.NewLine);
        }

        /// <summary>„2026-08-13T22:08:02" wird zu „13.08.2026".</summary>
        internal static string StandFormatieren(string roh)
        {
            if (string.IsNullOrEmpty(roh)) return null;
            return DateTime.TryParse(roh, out DateTime wert) ? wert.ToString("dd.MM.yyyy") : roh;
        }
    }
}

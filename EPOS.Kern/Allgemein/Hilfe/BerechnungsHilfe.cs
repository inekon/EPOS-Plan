using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Seite der Wiki-Rubrik <c>Programm Dokumentation/Berechnung</c> — der
    /// RECHENWEG einer Komponente (H13, Anwenderwunsch vom 06.09.2026).
    /// </summary>
    /// <remarks>
    /// Die Seiten liegen als <c>.wiki</c>-Dateien in
    /// <c>EPOS.Kern/Allgemein/Hilfe/Berechnung/</c> und sind in die Assembly
    /// eingebettet. Sie sind MediaWiki-Markup und damit unverändert in die
    /// Wikiseite kopierbar; dasselbe Markup speist das Wissen des Assistenten
    /// (<see cref="HilfeWissen"/>).
    /// </remarks>
    public sealed class BerechnungsSeite
    {
        internal BerechnungsSeite(string seitenname, string stand, string rechenkern, string markup)
        {
            Seitenname = seitenname ?? "";
            Stand = stand ?? "";
            Rechenkern = rechenkern ?? "";
            Markup = markup ?? "";
            Klartext = BerechnungsHilfe.AlsKlartext(Markup);
        }

        /// <summary>Der Name der Wikiseite ohne Rubrik, z. B. <c>Photovoltaik</c>.</summary>
        public string Seitenname { get; }

        /// <summary>Die Überschrift für Anzeige und Wissensabschnitt: <c>Berechnung: Photovoltaik</c>.</summary>
        public string Titel => BerechnungsHilfe.RUBRIK_KURZ + ": " + Seitenname;

        /// <summary>Der volle Wiki-Seitentitel: <c>Programm Dokumentation/Berechnung/Photovoltaik</c>.</summary>
        public string WikiTitel => BerechnungsHilfe.RUBRIK_TITEL + "/" + Seitenname;

        /// <summary>
        /// Der Kurzname, den <c>help_mapping.txt</c> als Ziel trägt:
        /// <c>Berechnung/Photovoltaik</c>.
        /// </summary>
        public string Ziel => BerechnungsHilfe.RUBRIK_KURZ + "/" + Seitenname;

        /// <summary>Der Stand aus dem Kopfblock, Form <c>JJJJ-MM-TT</c>.</summary>
        public string Stand { get; }

        /// <summary>
        /// Die Dateien des Rechenkerns, gegen die der Text belegt ist — aus dem
        /// Kopfblock, durch Komma getrennt.
        /// </summary>
        public string Rechenkern { get; }

        /// <summary>Der Seitentext als MediaWiki-Markup, einschließlich Kopfblock.</summary>
        public string Markup { get; }

        /// <summary>
        /// Derselbe Text ohne Auszeichnung und ohne Kopfblock — das, was der
        /// Assistent liest.
        /// </summary>
        public string Klartext { get; }

        /// <inheritdoc />
        public override string ToString() => Titel;
    }

    /// <summary>
    /// Der Leser der Hilferubrik „Berechnung" (H13).
    ///
    /// <para><b>Wozu.</b> Der Anwenderwunsch vom 06.09.2026 trennt zwei Dinge, die
    /// bis dahin dieselbe Wikiseite teilten: die BEDIENUNG einer Maske und den
    /// RECHENWEG dahinter. Die Rechenwege stehen seither in einer eigenen Rubrik
    /// <c>Programm Dokumentation/Berechnung</c>, und die allgemeinen Seiten
    /// verweisen mit einem Abschnitt „Berechnung" dorthin.</para>
    ///
    /// <para><b>Warum die Texte im Kern liegen und nicht nur im Wiki.</b> Drei
    /// Verbraucher wollen dieselben Sätze:
    /// <list type="number">
    /// <item>das Wiki selbst — der Anwender kopiert die Datei unverändert in die
    ///       Seite; deshalb ist der Inhalt MediaWiki-Markup und kein eigenes
    ///       Format,</item>
    /// <item>der KI-Assistent — <see cref="HilfeWissen"/> hängt je Seite einen
    ///       Wissensabschnitt an, und der wirkt auch OHNE Netz,</item>
    /// <item>die Prüfung — ein Text, der im Wiki steht, altert unbemerkt; einer,
    ///       der im Quellbaum steht, wird von den Tests auf Kopfblock und
    ///       Gliederung geprüft.</item>
    /// </list></para>
    ///
    /// <para><b>Der Bestand.</b> Eine Datei je Wikiseite, eingebettet über
    /// <c>EPOS.Kern.csproj</c> mit dem logischen Namen
    /// <c>EPOS.Kern.Hilfe.Berechnung.&lt;Dateiname&gt;</c>. Eine Datei mit
    /// führendem Unterstrich ist KEINE Seite, sondern Beiwerk für den Anwender
    /// (<c>_Index.wiki</c> — die Rubrik-Startseite, <c>_Bezuege.wiki</c> — die
    /// Abschnitte, die er in die allgemeinen Seiten einfügt).</para>
    ///
    /// <para>Der Leser ist plattformfrei: nur <see cref="Assembly"/> und
    /// Zeichenketten, keine Datei neben der EXE, kein Netz, kein
    /// <c>Dienste.*</c>. Er läuft damit unter Windows, auf iOS und im
    /// Prüfstand gleich.</para>
    /// </summary>
    public static class BerechnungsHilfe
    {
        /// <summary>Der Kurzname der Unterrubrik — so heißt sie im Wiki und im Mapping.</summary>
        public const string RUBRIK_KURZ = "Berechnung";

        /// <summary>Der volle Wiki-Titel der Rubrik-Startseite.</summary>
        public const string RUBRIK_TITEL = "Programm Dokumentation/" + RUBRIK_KURZ;

        /// <summary>
        /// Der Bereich, unter dem die Seiten im Wissen des Assistenten stehen
        /// (<c>WissensAbschnitt.Bereich</c>).
        /// </summary>
        public const string BEREICH = RUBRIK_KURZ;

        /// <summary>
        /// Namensvorsatz der eingebetteten Dateien. Er steht wörtlich so in
        /// <c>EPOS.Kern.csproj</c> und hängt deshalb NICHT am Ordnerpfad.
        /// </summary>
        internal const string RESSOURCE_VORSATZ = "EPOS.Kern.Hilfe.Berechnung.";

        /// <summary>Die Endung der Seitendateien.</summary>
        internal const string ENDUNG = ".wiki";

        private static IReadOnlyList<BerechnungsSeite> _seiten;

        /// <summary>
        /// Alle Seiten der Rubrik, nach Seitenname sortiert. Wird beim ersten
        /// Zugriff einmal gelesen; die Liste ist danach unveränderlich.
        /// </summary>
        public static IReadOnlyList<BerechnungsSeite> Seiten
        {
            get { return _seiten ?? (_seiten = Lesen(typeof(BerechnungsHilfe).Assembly)); }
        }

        /// <summary>Die Seite zu einem Seitennamen; <c>null</c>, wenn es sie nicht gibt.</summary>
        /// <remarks>Der Vergleich ist schreibungsunabhängig — „photovoltaik" trifft „Photovoltaik".</remarks>
        public static BerechnungsSeite Seite(string seitenname)
        {
            if (string.IsNullOrWhiteSpace(seitenname)) return null;

            string gesucht = seitenname.Trim();

            // Ein vollstaendiges Ziel ("Berechnung/Photovoltaik") ist ebenfalls
            // zulaessig - so, wie es in help_mapping.txt steht.
            if (gesucht.StartsWith(RUBRIK_KURZ + "/", StringComparison.OrdinalIgnoreCase))
                gesucht = gesucht.Substring(RUBRIK_KURZ.Length + 1).Trim();

            foreach (BerechnungsSeite seite in Seiten)
                if (string.Equals(seite.Seitenname, gesucht, StringComparison.OrdinalIgnoreCase)) return seite;

            return null;
        }

        // ===================================================================
        //  Lesen
        // ===================================================================

        /// <summary>
        /// Liest alle eingebetteten Seiten einer Assembly. Öffentlich für den
        /// Prüfstand; im Betrieb genügt <see cref="Seiten"/>.
        /// </summary>
        internal static IReadOnlyList<BerechnungsSeite> Lesen(Assembly assembly)
        {
            var seiten = new List<BerechnungsSeite>();
            if (assembly == null) return seiten;

            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name == null) continue;
                if (!name.StartsWith(RESSOURCE_VORSATZ, StringComparison.Ordinal)) continue;
                if (!name.EndsWith(ENDUNG, StringComparison.OrdinalIgnoreCase)) continue;

                string dateiname = name.Substring(RESSOURCE_VORSATZ.Length);

                // Beiwerk fuer den Anwender, keine Wikiseite (_Index, _Bezuege).
                if (dateiname.StartsWith("_", StringComparison.Ordinal)) continue;

                string markup = Inhalt(assembly, name);
                if (markup.Length == 0) continue;

                string ausDateiname = dateiname.Substring(0, dateiname.Length - ENDUNG.Length);

                Kopfblock(markup, out string seitenname, out string stand, out string rechenkern);
                if (seitenname.Length == 0) seitenname = ausDateiname;

                seiten.Add(new BerechnungsSeite(seitenname, stand, rechenkern, markup));
            }

            seiten.Sort((a, b) => string.Compare(a.Seitenname, b.Seitenname, StringComparison.Ordinal));
            return seiten;
        }

        private static string Inhalt(Assembly assembly, string ressource)
        {
            try
            {
                using (Stream strom = assembly.GetManifestResourceStream(ressource))
                {
                    if (strom == null) return "";

                    using (var leser = new StreamReader(strom, Encoding.UTF8,
                                                        detectEncodingFromByteOrderMarks: true))
                    {
                        return leser.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Berechnungsseite '" + ressource + "' nicht lesbar: " + ex.Message);
                return "";
            }
        }

        // ===================================================================
        //  Kopfblock
        // ===================================================================

        /// <summary>
        /// Der Kopfblock steht als Wiki-Kommentar in den ersten Zeilen jeder
        /// Datei und ist die Beleglage des Textes:
        /// <code>
        /// &lt;!-- EPOS-Plan Hilferubrik Berechnung | Seite: Photovoltaik | Stand: 2026-09-06 | Rechenkern:
        ///      EPOS.Kern/Allgemein/Simulation/SimulationPV.cs --&gt;
        /// </code>
        /// </summary>
        /// <remarks>
        /// Der Wiki-Kommentar wird beim Kopieren in die Seite mitgenommen und ist
        /// dort unsichtbar — der Leser der Wikiseite sieht ihn nicht, wer den
        /// Quelltext der Seite öffnet, findet die Herkunft. Im
        /// <see cref="BerechnungsSeite.Klartext"/> ist er entfernt.
        /// </remarks>
        internal static void Kopfblock(string markup, out string seite, out string stand,
                                       out string rechenkern)
        {
            seite = "";
            stand = "";
            rechenkern = "";
            if (string.IsNullOrEmpty(markup)) return;

            Match kommentar = Regex.Match(markup, @"<!--(.*?)-->", RegexOptions.Singleline);
            if (!kommentar.Success) return;

            string kopf = Regex.Replace(kommentar.Groups[1].Value, @"\s+", " ").Trim();

            seite = Feld(kopf, "Seite");
            stand = Feld(kopf, "Stand");
            rechenkern = Feld(kopf, "Rechenkern");
        }

        /// <summary>
        /// Ein Feld des Kopfblocks. Die Felder sind mit '|' getrennt; das letzte
        /// (<c>Rechenkern</c>) reicht bis zum Ende.
        /// </summary>
        private static string Feld(string kopf, string name)
        {
            Match treffer = Regex.Match(kopf, @"\b" + Regex.Escape(name) + @"\s*:\s*([^|]*)");
            return treffer.Success ? treffer.Groups[1].Value.Trim() : "";
        }

        // ===================================================================
        //  Klartext
        // ===================================================================

        /// <summary>
        /// Macht aus dem MediaWiki-Markup lesbaren Fließtext — das, was der
        /// Assistent bekommt.
        /// </summary>
        /// <remarks>
        /// <para><b>Was verschwindet:</b> der Kopfblock und jeder andere
        /// Wiki-Kommentar, die Auszeichnung von Überschriften, Listen, Fett- und
        /// Kursivsatz, die Gerüstzeilen einer Tabelle und die Zielhälfte eines
        /// Verweises.</para>
        /// <para><b>Was bleibt:</b> jedes Wort und jede Zahl. Die Formelzeilen
        /// stehen im Markup als vorformatierte Zeilen (führendes Leerzeichen) —
        /// sie bleiben Zeile für Zeile stehen, denn sie sind der Kern der
        /// Auskunft. Tabellenzellen werden zu einer Zeile mit
        /// „&#160;|&#160;" zwischen den Feldern.</para>
        /// </remarks>
        internal static string AlsKlartext(string markup)
        {
            if (string.IsNullOrWhiteSpace(markup)) return "";

            // Kommentare (darunter der Kopfblock) fallen ganz weg.
            string text = Regex.Replace(markup, @"<!--.*?-->", "", RegexOptions.Singleline);

            var ausgabe = new List<string>();
            var zelle = new List<string>();

            foreach (string rohzeile in text.Replace("\r\n", "\n").Split('\n'))
            {
                string zeile = rohzeile.TrimEnd();
                string beschnitten = zeile.Trim();

                // Tabellengeruest: Anfang, Ende und Zeilentrenner.
                if (beschnitten.StartsWith("{|", StringComparison.Ordinal) ||
                    beschnitten.StartsWith("|}", StringComparison.Ordinal) ||
                    beschnitten.StartsWith("|-", StringComparison.Ordinal) ||
                    beschnitten.StartsWith("|+", StringComparison.Ordinal))
                {
                    ZeileSchliessen(ausgabe, zelle);
                    continue;
                }

                // Kopf- und Datenzellen einer Tabelle sammeln sich zu EINER Zeile.
                if (beschnitten.StartsWith("!", StringComparison.Ordinal) ||
                    beschnitten.StartsWith("|", StringComparison.Ordinal))
                {
                    foreach (string teil in Zellen(beschnitten))
                    {
                        string wert = Saeubern(teil);
                        if (wert.Length > 0) zelle.Add(wert);
                    }
                    continue;
                }

                ZeileSchliessen(ausgabe, zelle);

                if (beschnitten.Length == 0)
                {
                    if (ausgabe.Count > 0 && ausgabe[ausgabe.Count - 1].Length > 0) ausgabe.Add("");
                    continue;
                }

                // Alles Uebrige - Fliesstext, Listenpunkte und die vorformatierten
                // Formelzeilen (fuehrendes Leerzeichen) - verliert nur seine
                // Auszeichnung. Die Formel selbst bleibt Zeichen fuer Zeichen stehen.
                ausgabe.Add(Saeubern(beschnitten));
            }

            ZeileSchliessen(ausgabe, zelle);

            string ergebnis = string.Join("\n", ausgabe).Trim();
            return Regex.Replace(ergebnis, @"\n{3,}", "\n\n");
        }

        private static void ZeileSchliessen(List<string> ausgabe, List<string> zelle)
        {
            if (zelle.Count == 0) return;

            ausgabe.Add(string.Join(" | ", zelle));
            zelle.Clear();
        }

        /// <summary>
        /// Zerlegt eine Tabellenzeile in ihre Zellen. MediaWiki trennt Zellen
        /// EINER Zeile mit „||" (Daten) bzw. „!!" (Kopf); das führende Zeichen
        /// gehört zur ersten Zelle.
        /// </summary>
        private static IEnumerable<string> Zellen(string zeile)
        {
            string rumpf = zeile.Substring(1);
            string[] teile = zeile.StartsWith("!", StringComparison.Ordinal)
                ? rumpf.Split(new[] { "!!" }, StringSplitOptions.None)
                : rumpf.Split(new[] { "||" }, StringSplitOptions.None);

            return teile.Select(t => t.Trim());
        }

        /// <summary>Nimmt einer Zeile die Auszeichnung, ohne ihr Wort zu ändern.</summary>
        private static string Saeubern(string zeile)
        {
            string s = zeile ?? "";

            // Ueberschriften: "== Rechenweg ==" -> "Rechenweg"
            Match ueber = Regex.Match(s.Trim(), @"^(={2,6})\s*(.*?)\s*\1$");
            if (ueber.Success) s = ueber.Groups[2].Value;

            // Verweise: [[Ziel|Text]] -> Text, [[Ziel]] -> Ziel
            s = Regex.Replace(s, @"\[\[([^\]\|]*)\|([^\]]*)\]\]", "$2");
            s = Regex.Replace(s, @"\[\[([^\]]*)\]\]", "$1");

            // Aeussere Verweise: [http://... Text] -> Text
            s = Regex.Replace(s, @"\[(?:https?|mailto):[^\s\]]+\s+([^\]]*)\]", "$1");

            // Fett und kursiv
            s = s.Replace("'''''", "").Replace("'''", "").Replace("''", "");

            // Formeln, falls die Wikiinstallation sie kann
            s = Regex.Replace(s, @"</?math>", "");

            // Einfache HTML-Reste (<br>, <ref>, <code>). Bewusst eng gefasst:
            // Ein Muster wie "<[^>]+>" verschluckte in einer Formelzeile alles
            // zwischen einem "<" und dem naechsten ">" - aus "P < 0 und Q > 1"
            // wuerde "P 1".
            s = Regex.Replace(s, @"</?[A-Za-z][A-Za-z0-9]{0,15}\s*/?>", "");

            // Listenzeichen am Anfang
            s = Regex.Replace(s, @"^[\*\#:;]+\s*", "");

            return s.TrimEnd();
        }
    }
}

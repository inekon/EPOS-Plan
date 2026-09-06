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
        /// stehen im Markup als vorformatierte Zeilen (führendes Leerzeichen), seit
        /// H13 Fassung 2 als eingerückte Anzeige-Zeile
        /// <c>: &lt;big&gt;…&lt;/big&gt;  (3)</c> und seit Fassung 3 als
        /// <c>: &lt;math&gt;…&lt;/math&gt;  (3)</c> mit ihren Legendezeilen
        /// <c>:: &lt;math&gt;…&lt;/math&gt; – Bedeutung [Einheit]</c>; alle bleiben
        /// Zeile für Zeile stehen, denn sie sind der Kern der Auskunft.
        /// Tabellenzellen werden zu einer Zeile mit „&#160;|&#160;" zwischen den
        /// Feldern.</para>
        /// <para><b>Was umgesetzt wird:</b> die Formelschreibweise der Rubrik. Sie
        /// hat seit H13 <b>Fassung 3</b> zwei Formen, und beide führen auf dieselbe
        /// lesbare Zeile:
        /// <list type="bullet">
        /// <item><b>LaTeX in <c>&lt;math&gt;</c></b> (Fassung 3) — das Wiki setzt sie
        ///       über seine Math-Erweiterung, der Assistent kann das nicht;
        ///       <see cref="LatexKlartext"/> macht daraus
        ///       <c>SKZ = (P_el)/(P_th)</c>.</item>
        /// <item><b>HTML-Indizes</b> (Fassung 2, bis die letzten Seiten nachziehen) —
        ///       <c>P&lt;sub&gt;AC,nenn&lt;/sub&gt;</c> wird zu <c>P_AC,nenn</c>,
        ///       <c>T&lt;sup&gt;2&lt;/sup&gt;</c> zu <c>T^2</c>, und die
        ///       <c>&lt;big&gt;</c>-Klammer der Anzeige-Formel fällt weg.</item>
        /// </list></para>
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

            // H13 Fassung 3 - die Formeln stehen als LaTeX in <math>. Der Assistent
            // bekommt keinen Formelsetzer; die Zeile wird deshalb HIER in lesbare
            // Zeichen umgesetzt ("\frac{a}{b}" -> "(a)/(b)", "\eta" -> "η"). Das
            // muss vor der Tag-Entfernung weiter unten geschehen: Die naehme nur die
            // zwei Klammern <math> und </math> und liesse den Backslash-Salat stehen.
            s = Regex.Replace(s, @"<math>(.*?)</math>",
                              treffer => LatexKlartext(treffer.Groups[1].Value),
                              RegexOptions.Singleline);

            // Eine unpaarige Klammer faellt trotzdem weg.
            s = Regex.Replace(s, @"</?math>", "");

            // H13 Fassung 2 - die Formelschreibweise der Rubrik. Indizes stehen im
            // Markup als HTML ("P<sub>AC,nenn</sub>", "T<sup>2</sup>"), weil diese
            // Wikiinstallation keine Math-Erweiterung hat. Sie werden HIER in die
            // Schreibweise umgesetzt, die der Assistent lesen kann - "P_AC,nenn" und
            // "T^2". Die Zeile MUSS vor der HTML-Zeile darunter stehen: Die fraesse
            // die Auszeichnung samt Trennzeichen weg, und aus "P<sub>AC</sub>" wuerde
            // das stumme "PAC".
            s = Regex.Replace(s, @"<sub>\s*(.*?)\s*</sub>", "_$1",
                              RegexOptions.IgnoreCase | RegexOptions.Singleline);
            s = Regex.Replace(s, @"<sup>\s*(.*?)\s*</sup>", "^$1",
                              RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Geschuetzte Leerzeichen der Anzeige-Formeln ("</big> &nbsp;&nbsp;(3)") und
            // der Zahlen ("8&nbsp;760") - fuer den Assistenten ein gewoehnliches Leerzeichen.
            s = s.Replace("&nbsp;", " ");

            // Einfache HTML-Reste (<br>, <ref>, <code>, <big> der Anzeige-Formeln).
            // Bewusst eng gefasst:
            // Ein Muster wie "<[^>]+>" verschluckte in einer Formelzeile alles
            // zwischen einem "<" und dem naechsten ">" - aus "P < 0 und Q > 1"
            // wuerde "P 1".
            s = Regex.Replace(s, @"</?[A-Za-z][A-Za-z0-9]{0,15}\s*/?>", "");

            // Listenzeichen am Anfang
            s = Regex.Replace(s, @"^[\*\#:;]+\s*", "");

            return s.TrimEnd();
        }

        // ===================================================================
        //  LaTeX -> lesbare Zeile (H13 Fassung 3)
        // ===================================================================

        /// <summary>
        /// Die Zeichen, die einen LaTeX-Befehl der Rubrik im Klartext vertreten.
        /// </summary>
        /// <remarks>
        /// Ein Befehlsname endet in LaTeX am ersten Zeichen, das kein Buchstabe ist —
        /// deshalb steht hinter jedem Muster <c>(?![A-Za-z])</c> und NICHT das
        /// bequemere <c>\b</c>: Der Wortanschluss läge zwischen „sum" und dem
        /// Unterstrich von <c>\sum_{t=1}</c> gar nicht vor (beides sind für ihn
        /// Wortzeichen), und die Summe bliebe stehen. Die Vorschau hindert zugleich
        /// <c>\in</c> daran, in <c>\infty</c> hineinzugreifen.
        /// </remarks>
        private static readonly (string Befehl, string Zeichen)[] BefehlsZeichen =
        {
            // Aufbau
            ("cdot", "·"), ("sum", "Σ"), ("prod", "Π"), ("int", "∫"),
            ("lvert", "|"), ("rvert", "|"),
            // Vergleich, Pfeil, Menge
            ("le", "≤"), ("ge", "≥"), ("ne", "≠"), ("approx", "≈"), ("pm", "±"),
            ("to", "→"), ("infty", "∞"), ("in", "∈"), ("dots", "…"),
            // Griechisch
            ("vartheta", "ϑ"), ("varepsilon", "ε"), ("varphi", "φ"),
            ("eta", "η"), ("rho", "ρ"), ("lambda", "λ"), ("alpha", "α"),
            ("beta", "β"), ("gamma", "γ"), ("tau", "τ"), ("kappa", "κ"),
            ("omega", "ω"), ("pi", "π"), ("ell", "ℓ"),
            ("Delta", "Δ"), ("Sigma", "Σ"), ("Psi", "Ψ")
        };

        /// <summary>
        /// Macht aus dem LaTeX einer Formel eine Zeile, die der Assistent lesen und
        /// wiedergeben kann.
        /// </summary>
        /// <remarks>
        /// <para><b>Warum das sein muss.</b> Seit H13 Fassung 3 stehen die Formeln der
        /// Rubrik als LaTeX in <c>&lt;math&gt;</c> — das Wiki setzt sie über seine
        /// Math-Erweiterung. Der Assistent hat keine solche Erweiterung: Ohne
        /// Umsetzung bekäme er
        /// <c>\displaystyle \mathrm{SKZ} = \frac{P_{\mathrm{el}}}{P_{\mathrm{th}}}</c>
        /// und gäbe genau das zurück. Nach der Umsetzung liest er
        /// <c>SKZ = (P_el)/(P_th)</c> — dieselbe Aussage in der Schreibweise, die
        /// der Anwender aus den Antworten kennt.</para>
        /// <para><b>Was geschieht.</b> Satz- und Abstandsbefehle
        /// (<c>\displaystyle</c>, <c>\left</c>, <c>\right</c>, <c>\,</c>, <c>\;</c>,
        /// <c>\quad</c>) fallen weg; <c>\mathrm{…}</c>, <c>\text{…}</c> und
        /// <c>\operatorname{…}</c> geben ihren Inhalt frei; ein Bruch wird zu
        /// <c>(Zähler)/(Nenner)</c>, eine Wurzel zu <c>√(…)</c>, eine
        /// Fallunterscheidung zu <c>{ a wenn b; c wenn d }</c>; Summen-, Vergleichs-
        /// und griechische Befehle werden zu ihrem Zeichen; Indizes und Hochzahlen
        /// verlieren ihre Klammern (<c>P_{\mathrm{AC,nenn}}</c> → <c>P_AC,nenn</c>),
        /// und <c>0{,}95</c> wird wieder <c>0,95</c>.</para>
        /// </remarks>
        internal static string LatexKlartext(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return "";

            string s = latex;

            // 1) Fallunterscheidung zuerst - sie traegt die Klammern, die weiter
            //    unten fallen, und ihr Trenner "\\" ist kein Befehl.
            s = Regex.Replace(s, @"\\begin\{cases\}(.*?)\\end\{cases\}",
                              treffer => Faelle(treffer.Groups[1].Value),
                              RegexOptions.Singleline);

            // 2) Satz- und Abstandsbefehle
            s = Regex.Replace(s, @"\\(?:displaystyle|left|right)(?![A-Za-z])", "");
            s = Regex.Replace(s, @"\\quad(?![A-Za-z])", " ");
            s = s.Replace("\\,", "").Replace("\\;", "").Replace("\\ ", " ");

            // 3) Klammernde Befehle geben ihren Inhalt frei
            s = Entfalten(s, "mathrm", 1, teile => teile[0]);
            s = Entfalten(s, "operatorname", 1, teile => teile[0]);
            s = Entfalten(s, "text", 1, teile => teile[0]);

            // 4) Bruch und Wurzel
            s = Entfalten(s, "frac", 2, teile => "(" + teile[0] + ")/(" + teile[1] + ")");
            s = Entfalten(s, "sqrt", 1, teile => "√(" + teile[0] + ")");

            // 5) Zeichenbefehle
            foreach ((string befehl, string zeichen) in BefehlsZeichen)
                s = Regex.Replace(s, @"\\" + befehl + @"(?![A-Za-z])", zeichen);

            // 6) Dezimaltrenner: "0{,}95" ist LaTeX fuer "0,95"
            s = s.Replace("{,}", ",");

            // 7) Indizes und Hochzahlen verlieren ihre Klammern
            for (int runde = 0; runde < 6; runde++)
            {
                string vorher = s;
                s = Regex.Replace(s, @"([_^])\{([^{}]*)\}", "$1$2");
                if (s == vorher) break;
            }

            // 8) Was an Klammern und Befehlen uebrig ist
            s = s.Replace("{", "").Replace("}", "");
            s = Regex.Replace(s, @"\\([A-Za-z]+)", "$1");
            s = s.Replace("\\", "");

            // 9) Die zwei Klammern der Fallunterscheidung waren vor Schritt 8 in
            //    Merkzeichen verwahrt - sie SOLLEN stehen bleiben.
            s = s.Replace(FALL_AUF, "{").Replace(FALL_ZU, "}");

            // Doppelte Leerzeichen, die beim Wegfall der Befehle entstehen
            return Regex.Replace(s, @"[ \t]{2,}", " ").Trim();
        }

        /// <summary>
        /// Der Rumpf einer <c>cases</c>-Umgebung wird zu
        /// <c>{ Wert wenn Bedingung; Wert wenn Bedingung }</c>. Die Zeilen trennt
        /// <c>\\</c>, Wert und Bedingung trennt <c>&amp;</c>.
        /// </summary>
        private static string Faelle(string rumpf)
        {
            var saetze = new List<string>();


            foreach (string zeile in Regex.Split(rumpf ?? "", @"\\\\"))
            {
                string[] spalten = zeile.Split('&');
                string wert = spalten[0].Trim();
                if (wert.Length == 0 && spalten.Length < 2) continue;

                saetze.Add(spalten.Length >= 2 && spalten[1].Trim().Length > 0
                    ? wert + " wenn " + spalten[1].Trim()
                    : wert);
            }

            return FALL_AUF + " " + string.Join("; ", saetze) + " " + FALL_ZU;
        }

        /// <summary>
        /// Merkzeichen für die geschweiften Klammern einer Fallunterscheidung. Sie
        /// stehen dort, wo Schritt 8 gleich JEDE Klammer wegnimmt — und werden danach
        /// zurückgesetzt. Zwei Steuerzeichen, die in keinem Seitentext vorkommen.
        /// </summary>
        private const string FALL_AUF = "\u0001";

        /// <inheritdoc cref="FALL_AUF" />
        private const string FALL_ZU = "\u0002";

        /// <summary>
        /// Ersetzt jedes <c>\befehl{…}</c> (mit <paramref name="argumente"/> Klammern,
        /// geschachtelte mitgezählt) durch das Ergebnis von <paramref name="bau"/>.
        /// </summary>
        /// <remarks>
        /// Ein Muster wie <c>\\frac\{([^{}]*)\}\{([^{}]*)\}</c> genügt hier NICHT: In
        /// <c>\frac{P_{\mathrm{el}}}{P_{\mathrm{th}}}</c> steht in jedem Argument
        /// wieder eine Klammer. Deshalb wird ausgezählt.
        /// </remarks>
        private static string Entfalten(string s, string befehl, int argumente,
                                        Func<string[], string> bau)
        {
            string marke = "\\" + befehl;
            int ab = 0;

            while (true)
            {
                int start = s.IndexOf(marke, ab, StringComparison.Ordinal);
                if (start < 0) return s;

                int stelle = start + marke.Length;

                // "\text" darf nicht in "\textrm" hineingreifen.
                if (stelle < s.Length && char.IsLetter(s[stelle])) { ab = start + 1; continue; }

                var teile = new string[argumente];
                bool vollstaendig = true;

                for (int i = 0; i < argumente; i++)
                {
                    while (stelle < s.Length && s[stelle] == ' ') stelle++;
                    if (stelle >= s.Length || s[stelle] != '{') { vollstaendig = false; break; }

                    int tiefe = 0;
                    int j = stelle;
                    for (; j < s.Length; j++)
                    {
                        if (s[j] == '{') tiefe++;
                        else if (s[j] == '}' && --tiefe == 0) break;
                    }
                    if (j >= s.Length) { vollstaendig = false; break; }

                    teile[i] = s.Substring(stelle + 1, j - stelle - 1);
                    stelle = j + 1;
                }

                if (!vollstaendig) { ab = start + 1; continue; }

                s = s.Substring(0, start) + bau(teile) + s.Substring(stelle);
                ab = start;   // das Eingesetzte kann selbst wieder einen Befehl tragen
            }
        }
    }
}

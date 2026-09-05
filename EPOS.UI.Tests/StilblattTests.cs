using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache über die Stilblätter in <c>EPOS.UI/wwwroot</c> — Befund
/// <b>W6‑B‑1</b> der Windows-Abnahme vom 04.09.2026.
///
/// <para>Der Regel <c>.epos-mehrzeilig { white-space: pre-line;</c> fehlte seit
/// dem Merge <c>7e8e341</c> (03.09.2026, Welle 5 in Welle 6) die schließende
/// Klammer. Chromium liest die folgenden Regeln dann nicht als Nachbarn,
/// sondern als <b>verschachtelte</b> Regeln (CSS Nesting): Sie greifen nur noch
/// INNERHALB eines <c>.epos-mehrzeilig</c>-Elements. Betroffen waren <b>414</b>
/// der 569 Blöcke des Hausblatts — darunter Menüband, Kopfband, Reiterleiste
/// und Kachelraster; das Hauptfenster erschien am Gerät als ungestyltes
/// HTML.</para>
///
/// <para>Keine bunit-Probe kann das sehen: Das Markup war die ganze Zeit
/// richtig, und bunit rechnet keine Stilblätter aus. Deshalb liest dieser Test
/// das Stilblatt selbst und prüft seine STRUKTUR — denselben Weg zum Blatt
/// geht die Regressionswache zu W5‑B‑1
/// (<c>Seiten/KostenSeiteTests.Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex</c>),
/// die den INHALT einer einzelnen Regel prüft.</para>
///
/// <para>Drei Fälle: (a) jede öffnende Klammer wird geschlossen und keine
/// schließende ist überzählig, (b) keine Stilregel steht in einer Stilregel,
/// (c) kein <c>&amp;</c>-Selektor. Das Haus benutzt <b>kein</b> CSS-Nesting —
/// wo verschachtelt aussieht, ist eine Klammer verlorengegangen.</para>
///
/// <para>Keine Sprachbindung: Der Fall prüft ausschließlich Zeichenketten,
/// keine Anzeigetexte.</para>
/// </summary>
public sealed class StilblattTests
{
    // ---------------------------------------------------------------------
    //  Die Fälle
    // ---------------------------------------------------------------------

    /// <summary>Alle <c>.css</c> unter <c>EPOS.UI/wwwroot</c>, je ein Fall.</summary>
    public static TheoryData<string> AlleStilblaetter
    {
        get
        {
            var daten = new TheoryData<string>();
            foreach (string p in Stilblaetter()) daten.Add(Path.GetFileName(p)!);
            return daten;
        }
    }

    /// <summary>
    /// Die Wurzel der Sache: Eine fehlende Klammer schaltet alles ab, was
    /// dahinter steht. Die Meldung nennt Zeile und Selektor des Blocks, der
    /// offen geblieben ist — sonst sucht man in 4 000 Zeilen.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleStilblaetter))]
    public void Jede_geoeffnete_Klammer_wird_geschlossen(string dateiname)
    {
        IReadOnlyList<Fund> funde = PruefeDatei(dateiname);

        Fund[] klammern = funde.Where(f => f.Art is Fund.OffenerBlock or Fund.UeberzaehligeKlammer)
                               .ToArray();

        Assert.True(klammern.Length == 0, dateiname + ":\n" + Bericht(klammern));
    }

    /// <summary>
    /// Das Haus schreibt flaches CSS. Eine Regel IN einer Regel ist deshalb
    /// nie Absicht, sondern die Folge einer verlorenen Klammer — und sie
    /// bleibt still: Der Browser meldet nichts, die Regeln greifen nur eben
    /// woanders. Innerhalb einer At-Regel (<c>@media</c>, <c>@supports</c>,
    /// <c>@keyframes</c>, <c>@font-face</c>, <c>@layer</c>) ist ein Block
    /// normal und erlaubt.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleStilblaetter))]
    public void Keine_Stilregel_steht_in_einer_Stilregel(string dateiname)
    {
        Fund[] geschachtelt = PruefeDatei(dateiname)
                              .Where(f => f.Art == Fund.Verschachtelt)
                              .ToArray();

        Assert.True(geschachtelt.Length == 0, dateiname + ":\n" + Bericht(geschachtelt));
    }

    /// <summary>
    /// Der Verstärker zu Fall (b): <c>&amp;</c> ist die Nesting-Syntax selbst.
    /// Wer sie benutzt, schreibt verschachteltes CSS mit Absicht — im Haus ist
    /// das keine erlaubte Bauweise, weil sie genau den Fehler unsichtbar
    /// macht, den W6‑B‑1 gekostet hat. In Kommentaren (&amp;nbsp;, „Berichte
    /// &amp; Kosten") ist das Zeichen erlaubt; der Prüfer sieht Kommentare
    /// nicht.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleStilblaetter))]
    public void Kein_kaufmaennisches_Und_als_Nesting_Selektor(string dateiname)
    {
        Fund[] ampersand = PruefeDatei(dateiname)
                           .Where(f => f.Art == Fund.NestingZeichen)
                           .ToArray();

        Assert.True(ampersand.Length == 0, dateiname + ":\n" + Bericht(ampersand));
    }

    /// <summary>
    /// Die Wache muss auch etwas finden können. Der Fall nimmt das ECHTE
    /// Stilblatt, entfernt die schließende Klammer von <c>.epos-mehrzeilig</c>
    /// wieder — der Stand vom 03.09.2026 — und verlangt beide Meldungen:
    /// den offenen Block mit seiner Zeile und seinem Selektor, und die erste
    /// Regel, die dadurch in ihm landet.
    ///
    /// <para>Am 05.09.2026 ist das Zeile 1384 (<c>.epos-mehrzeilig {</c>), die
    /// fehlende Klammer gehört auf Zeile 1386, und die erste hineingeratene
    /// Regel ist <c>.epos-reiter</c> (Zeile 1393). Die Zeilennummer wird hier
    /// NICHT festgeschrieben, sondern im Text gesucht — sonst bräche der Fall
    /// bei jeder Regel, die jemand weiter oben einfügt.</para>
    /// </summary>
    [Fact]
    public void Die_Wache_findet_die_fehlende_Klammer_von_epos_mehrzeilig()
    {
        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css")).Replace("\r\n", "\n");

        // Die Regel steht genau einmal und ist heil — sonst prüft der Fall
        // nicht das, was er zu prüfen vorgibt.
        const string heil = ".epos-mehrzeilig {\n    white-space: pre-line;\n}";
        Assert.Equal(1, ZaehleVorkommen(css, heil));

        string kaputt = css.Replace(heil, ".epos-mehrzeilig {\n    white-space: pre-line;");
        int zeileDerRegel = ZeileVon(kaputt, ".epos-mehrzeilig {");

        IReadOnlyList<Fund> funde = Pruefe(kaputt);

        Fund offen = Assert.Single(funde, f => f.Art == Fund.OffenerBlock);
        Assert.Equal(zeileDerRegel, offen.Zeile);
        Assert.Equal(".epos-mehrzeilig", offen.Selektor);

        // Die Meldung nennt beides — Zeile UND Selektor. Ohne das sucht man
        // die Stelle in 4 000 Zeilen von Hand.
        Assert.Contains(zeileDerRegel.ToString(), offen.ToString());
        Assert.Contains(".epos-mehrzeilig", offen.ToString());

        // Und die Folge: die naechste Regel steht jetzt DRIN.
        Fund erste = funde.First(f => f.Art == Fund.Verschachtelt);
        Assert.Equal(".epos-mehrzeilig", erste.Umgebung);
        Assert.Equal(zeileDerRegel, erste.UmgebungZeile);
        Assert.Equal(".epos-reiter", erste.Selektor);

        // Das heile Blatt meldet nichts — der Gegenbeweis zur Manipulation.
        Assert.Empty(Pruefe(css));
    }

    /// <summary>
    /// Die Wache darf nicht ins Leere greifen: Findet der Pfadweg kein Blatt,
    /// wären alle Theorien oben leer und trotzdem grün.
    /// </summary>
    [Fact]
    public void Das_Hausblatt_liegt_unter_der_Wache()
    {
        string[] blaetter = Stilblaetter().Select(p => Path.GetFileName(p)!).ToArray();

        Assert.NotEmpty(blaetter);
        Assert.Contains("epos-ui.css", blaetter);
    }

    // ---------------------------------------------------------------------
    //  Der Strukturprüfer
    // ---------------------------------------------------------------------

    /// <summary>Ein Fund des Strukturprüfers.</summary>
    public sealed class Fund
    {
        internal const string OffenerBlock = "Block nicht geschlossen";
        internal const string UeberzaehligeKlammer = "Schliessende Klammer ohne Block";
        internal const string Verschachtelt = "Stilregel in Stilregel";
        internal const string NestingZeichen = "&-Selektor";

        internal Fund(string art, int zeile, string selektor,
                      string umgebung = "", int umgebungZeile = 0)
        {
            Art = art;
            Zeile = zeile;
            Selektor = selektor;
            Umgebung = umgebung;
            UmgebungZeile = umgebungZeile;
        }

        /// <summary>Welche der vier Arten.</summary>
        public string Art { get; }

        /// <summary>Zeilennummer, 1-basiert.</summary>
        public int Zeile { get; }

        /// <summary>Der Selektor an dieser Stelle, auf 80 Zeichen gekürzt.</summary>
        public string Selektor { get; }

        /// <summary>Bei <see cref="Verschachtelt"/>: der umgebende Selektor.</summary>
        public string Umgebung { get; }

        /// <summary>Bei <see cref="Verschachtelt"/>: dessen Zeile.</summary>
        public int UmgebungZeile { get; }

        public override string ToString()
        {
            string s = "Zeile " + Zeile + ": " + Art + " — \"" + Selektor + "\"";
            if (UmgebungZeile > 0) s += " steht in \"" + Umgebung + "\" (Zeile " + UmgebungZeile + ")";
            return s;
        }
    }

    /// <summary>
    /// Der Strukturparser. Er versteht CSS nicht, er zählt nur Blöcke — und
    /// genau das reicht: Kommentare <c>/* … */</c>, Zeichenketten
    /// <c>"…"</c>/<c>'…'</c> und <c>url(…)</c> werden übersprungen, alles
    /// Übrige ist Selektorvorspann, Deklaration oder Klammer. Über die
    /// geöffneten Blöcke läuft ein Stapel mit Zeile und Selektor mit, damit
    /// jede Meldung sagen kann, WO man nachsehen muss.
    /// </summary>
    public static IReadOnlyList<Fund> Pruefe(string css)
    {
        var funde = new List<Fund>();
        var stapel = new Stack<(int Zeile, string Selektor)>();
        var vorspann = new StringBuilder();
        int zeile = 1;

        for (int i = 0; i < css.Length; i++)
        {
            char c = css[i];

            if (c == '\n')
            {
                zeile++;
                vorspann.Append(' ');
                continue;
            }

            // Kommentar — überspringen, die Zeilen darin trotzdem zählen.
            if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
            {
                int ende = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                if (ende < 0) ende = css.Length - 2;      // unbeendet: bis zum Schluss
                for (int k = i; k < ende; k++)
                    if (css[k] == '\n') zeile++;
                i = ende + 1;
                continue;
            }

            // Zeichenkette — eine Klammer darin ist Inhalt, keine Struktur.
            if (c == '"' || c == '\'')
            {
                i = UeberliesZeichenkette(css, i, ref zeile);
                continue;
            }

            // url(…) ohne Anführungszeichen — bis zur schließenden Klammer.
            if ((c == 'u' || c == 'U') && BeginntUrl(css, i))
            {
                i = UeberliesUrl(css, i, ref zeile);
                continue;
            }

            if (c == '{')
            {
                string selektor = Gekuerzt(vorspann.ToString());

                // Ein Block IN einem Block ist nur unter einer At-Regel normal.
                if (stapel.Count > 0 && !stapel.Peek().Selektor.StartsWith("@", StringComparison.Ordinal))
                {
                    (int uZeile, string uSelektor) = stapel.Peek();
                    funde.Add(new Fund(Fund.Verschachtelt, zeile, selektor, uSelektor, uZeile));
                }

                stapel.Push((zeile, selektor));
                vorspann.Clear();
                continue;
            }

            if (c == '}')
            {
                if (stapel.Count == 0)
                    funde.Add(new Fund(Fund.UeberzaehligeKlammer, zeile, Gekuerzt(vorspann.ToString())));
                else
                    stapel.Pop();

                vorspann.Clear();
                continue;
            }

            if (c == ';')
            {
                vorspann.Clear();
                continue;
            }

            // Ausserhalb von Kommentar, Zeichenkette und url(): Nesting-Syntax.
            if (c == '&')
                funde.Add(new Fund(Fund.NestingZeichen, zeile, Gekuerzt(vorspann.ToString() + "&")));

            vorspann.Append(c);
        }

        // Was am Ende offen ist, war nie geschlossen — innerste Blöcke zuerst.
        foreach ((int z, string s) in stapel)
            funde.Add(new Fund(Fund.OffenerBlock, z, s));

        return funde;
    }

    /// <summary>Liest <paramref name="dateiname"/> aus <c>EPOS.UI/wwwroot</c> und prüft ihn.</summary>
    private static IReadOnlyList<Fund> PruefeDatei(string dateiname)
        => Pruefe(File.ReadAllText(Path.Combine(Wwwroot(), dateiname)));

    /// <summary>Ab dem Anführungszeichen bis hinter das schließende; liefert dessen Stelle.</summary>
    private static int UeberliesZeichenkette(string css, int i, ref int zeile)
    {
        char anfuehrung = css[i];
        i++;

        while (i < css.Length && css[i] != anfuehrung)
        {
            if (css[i] == '\\') i++;                       // maskiertes Zeichen mitnehmen
            else if (css[i] == '\n') zeile++;              // (in CSS unerlaubt, aber zählbar)
            i++;
        }

        return i;
    }

    /// <summary>Steht an dieser Stelle das Wort <c>url(</c>?</summary>
    private static bool BeginntUrl(string css, int i)
        => i + 4 <= css.Length
           && string.Compare(css, i, "url(", 0, 4, StringComparison.OrdinalIgnoreCase) == 0;

    /// <summary>
    /// Über <c>url(…)</c> hinweg. Steht der Inhalt in Anführungszeichen,
    /// übernimmt ihn der gewöhnliche Weg — hier wird nur das Wort selbst
    /// übersprungen; sonst geht es bis zur schließenden runden Klammer.
    /// </summary>
    private static int UeberliesUrl(string css, int i, ref int zeile)
    {
        int j = i + 4;                                     // hinter "url("
        while (j < css.Length && (css[j] == ' ' || css[j] == '\t')) j++;

        if (j < css.Length && (css[j] == '"' || css[j] == '\'')) return j - 1;

        while (j < css.Length && css[j] != ')')
        {
            if (css[j] == '\n') zeile++;
            j++;
        }

        return j;
    }

    /// <summary>Mehrfache Leerzeichen weg, auf 80 Zeichen gekürzt.</summary>
    private static string Gekuerzt(string vorspann)
    {
        string s = string.Join(" ", vorspann.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return s.Length <= 80 ? s : s.Substring(0, 77) + "...";
    }

    /// <summary>Die Funde als lesbare Liste für die Fehlermeldung.</summary>
    private static string Bericht(IEnumerable<Fund> funde)
        => string.Join("\n", funde.Select(f => "  " + f));

    // ---------------------------------------------------------------------
    //  Der Weg zum Stilblatt
    // ---------------------------------------------------------------------

    /// <summary>
    /// <c>EPOS.UI/wwwroot</c>, gefunden über denselben Aufstieg wie in
    /// <c>KostenSeiteTests.Stilblock</c>: vom Ausgabeverzeichnis so lange
    /// aufwärts, bis das Hausblatt dasteht.
    /// </summary>
    private static string Wwwroot()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);   // das Stilblatt muss im Baum stehen
        return Path.Combine(d!.FullName, "EPOS.UI", "wwwroot");
    }

    /// <summary>Alle Stilblätter unter <c>EPOS.UI/wwwroot</c>, nach Namen sortiert.</summary>
    private static string[] Stilblaetter()
        => Directory.GetFiles(Wwwroot(), "*.css", SearchOption.AllDirectories)
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .ToArray();

    /// <summary>Zeilennummer (1-basiert) des ersten Vorkommens von <paramref name="text"/>.</summary>
    private static int ZeileVon(string css, string text)
    {
        int a = css.IndexOf(text, StringComparison.Ordinal);
        Assert.True(a >= 0, "\"" + text + "\" steht nicht im Stilblatt");
        return css.Take(a).Count(z => z == '\n') + 1;
    }

    /// <summary>Wie oft <paramref name="text"/> vorkommt.</summary>
    private static int ZaehleVorkommen(string css, string text)
    {
        int n = 0;
        for (int i = css.IndexOf(text, StringComparison.Ordinal); i >= 0;
             i = css.IndexOf(text, i + text.Length, StringComparison.Ordinal))
            n++;
        return n;
    }
}

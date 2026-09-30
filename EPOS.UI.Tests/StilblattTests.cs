using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    // ---------------------------------------------------------------------
    //  W6-B-4: die Regeln, die die Strangtabelle in den Dialog passen lassen
    // ---------------------------------------------------------------------

    /// <summary>
    /// <b>Die zwei Klapplisten der Strangtabelle sind gedeckelt und lassen aus</b>
    /// (Befund <b>W6‑B‑4</b>, Windows-Abnahme 07.09.2026). Ein Gerätename wie
    /// „SMA America: SB30-1SP-US-40 {240V}" trieb seine Spalte auf 302 px und die
    /// Tabelle auf 1 974 px — bei 1 046 px Platz lagen sechs Zahlenspalten ausserhalb
    /// des sichtbaren Bereichs. Die Regel deckelt beide Listen und schneidet den
    /// Namen mit Auslassungspunkten ab; der volle Name steht im Werkzeugtipp.
    ///
    /// <para>Denselben Weg geht die Wache zu W5‑B‑1: bunit rechnet keine Stilblätter
    /// aus (Lehre W6‑B‑1), also liest der Fall die REGEL. Ob sie wirkt, misst die
    /// Playwright-Probe im Laufordner.</para>
    /// </summary>
    [Fact]
    public void W6B4_Die_Klapplisten_der_Strangtabelle_sind_gedeckelt()
    {
        string block = Regelblock(".epos-strangtabelle-liste .epos-eingabe");

        Assert.Contains("max-width:", block, StringComparison.Ordinal);
        Assert.Contains("text-overflow: ellipsis", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die fünf Zahlenspalten bleiben schmal und dürfen ihren Kopf umbrechen</b>
    /// (W6‑B‑4). „Module in Reihe" und „Stränge parallel" stehen unter
    /// <c>.epos-raster th { white-space: nowrap }</c> in EINER Zeile und belegten
    /// damit je 196 px — mehr als das Zahlenfeld darunter je braucht.
    /// </summary>
    [Fact]
    public void W6B4_Die_Zahlenspalten_der_Strangtabelle_duerfen_umbrechen()
    {
        // Der Selektor nennt th UND td - eine blosse Klasse (0,1,0) verloere gegen
        // die Hausregel .epos-raster th, .epos-raster td { white-space: nowrap }
        // (0,1,1). Gemessen: ohne th/td blieben "Module in Reihe" und "Straenge
        // parallel" einzeilig und hielten je 126 statt 72 px offen.
        string block = Regelblock(".epos-strangtabelle th.epos-strangtabelle-zahl");

        Assert.Contains("white-space: normal", block, StringComparison.Ordinal);
        Assert.Contains("width: 1%", block, StringComparison.Ordinal);

        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css"));
        Assert.Contains(".epos-strangtabelle td.epos-strangtabelle-zahl", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die Zahlenspalten der Ergebnistabellen stehen rechts</b> (W11b‑B‑23,
    /// 09.09.2026). Elf Tabellen der Detaillierten Simulation setzen
    /// <c>.epos-simerg-zahl</c> an ihre Zahlenzellen — die einzige Regel dazu
    /// stand aber unter <c>.epos-simerg-kennzahlen td.epos-simerg-zahl</c> und
    /// galt damit NUR für die Kennzahlentabelle des Stromspeichers. In
    /// <c>.epos-raster</c> erbten die Zellen <c>text-align: left</c> der
    /// Hausregel: Die Klasse behauptete eine Ausrichtung, die es nicht gab.
    ///
    /// <para>Der Selektor nennt <c>td</c> UND die Tabellenklasse — eine blosse
    /// Klasse (0,1,0) verlöre gegen die Hausregel <c>.epos-raster th,
    /// .epos-raster td</c> (0,1,1). Denselben Weg geht W6‑B‑4.</para>
    /// </summary>
    [Fact]
    public void W11bB23_Die_Zahlenspalten_der_Ergebnistabellen_stehen_rechts()
    {
        string block = Regelblock(".epos-raster td.epos-simerg-zahl");

        Assert.Contains("text-align: right", block, StringComparison.Ordinal);
        Assert.Contains("font-variant-numeric: tabular-nums", block, StringComparison.Ordinal);

        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css"));
        Assert.Contains(".epos-raster th.epos-simerg-zahl", css, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    //  #186: Gruppenkopf-Balken und Kopfzelle des Zeilenrasters
    // ---------------------------------------------------------------------

    /// <summary>
    /// <b>Befund 3 der Abnahmeliste vom 11.09.2026:</b> weisser Text im dunklen
    /// Balken ohne Einrichtung — <c>.epos-gruppenkopf-balken</c> trug kein
    /// <c>padding</c>, nur <c>.epos-gruppenkopf-titel { margin-left: 8px }</c>,
    /// unabhaengig davon ob ein Symbol stand. Mit Symbol begann der Text daher
    /// spaeter als ohne — zwei Textbeginn-Stellen desselben Balkens, bei ueber
    /// 140 Einsaetzen ohne Symbol sichtbar als „keine Einrueckung". Der Balken
    /// traegt jetzt <c>padding-inline</c> und eine FESTE Symbolspur
    /// (<c>grid-template-columns</c>): Die Spur steht auch ohne
    /// <c>&lt;span class="epos-gruppenkopf-symbol"&gt;</c>, der Titel liegt
    /// immer in derselben Spalte — der Textbeginn ist damit fuer jeden Einsatz
    /// derselbe, mit und ohne Symbol.
    /// </summary>
    [Fact]
    public void W186_Der_Gruppenkopf_Balken_traegt_padding_inline()
    {
        string block = Regelblock(".epos-gruppenkopf-balken");

        Assert.Contains("padding-inline:", block, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns:", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Befund 1a der Abnahmeliste vom 11.09.2026:</b> Die Spaltenkoepfe
    /// „Nutzungs-⏎dauer [a]" und „Worst/Best" liefen zusammen. Ursache war ein
    /// echter Zeilenumbruch in <c>KDLG_SP_NUTZUNG</c> (wirkte in WinForms,
    /// kollabierte in HTML zu einem Leerzeichen) UND
    /// <c>.epos-zr-kopfzelle { white-space: nowrap }</c> ohne Overflow — die
    /// schmalste Spur des Positionsrasters ist 60 px (SPALTENMASS), das reicht
    /// fuer keinen Kopftext einzeilig. Die Kopfzelle darf jetzt zweizeilig
    /// umbrechen, auch mitten im Wort.
    /// </summary>
    [Fact]
    public void W186_Die_Kopfzelle_des_Zeilenrasters_bricht_um()
    {
        string block = Regelblock(".epos-zr-kopfzelle");

        Assert.DoesNotContain("nowrap", block, StringComparison.Ordinal);
        Assert.Contains("white-space: normal", block, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Auftrag #273:</b> Der Kopfblock der Station „Optimierung" steht EINSPALTIG —
    /// erst die Suche, darunter der Bedienblock in voller Breite. Bis dahin war er eine
    /// Flexzeile mit dem Bedienblock als zweiter Spalte fester Breite (26 rem); der
    /// Anwender hat ihn am 14.09.2026 unter die Suche gestellt. Mit der zweiten Spalte
    /// fällt auch die Umbruchregel — ohne sie gibt es nichts umzubrechen.
    /// </summary>
    [Fact]
    public void W273_Der_Kopf_der_Station_Optimierung_ist_einspaltig()
    {
        string kopf = Regelblock(".epos-flotte-optimierung-kopf", "epos-flotte.css");

        Assert.Contains("display: grid", kopf, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1fr)", kopf, StringComparison.Ordinal);
        Assert.DoesNotContain("flex", kopf, StringComparison.Ordinal);

        // Und der Bedienblock traegt keine feste Spaltenbreite mehr.
        string block = Regelblock(".epos-flotte-bedienblock", "epos-flotte.css");
        Assert.DoesNotContain("flex:", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Stufe G4c, Welle 2:</b> Der Gebäudeimport markiert seine Zeilen gelb und rot aus den
    /// TOKENS des Hauses (keine Farbliterale), und im Kontrastmodus bleiben die zwei Zustände
    /// sichtbar verschieden — gestrichelt gegen durchgezogen. Die Herkunft ist je Schlüssel
    /// eine Regel; die beiden Dateiformate teilen eine.
    /// </summary>
    [Fact]
    public void G4c_Die_Zeilen_des_Gebaeudeimports_markieren_mit_Tokens()
    {
        Assert.Contains("var(--epos-warn-flaeche)", Regelblock(".epos-gebimport-zeile--gelb > td"), StringComparison.Ordinal);
        Assert.Contains("var(--epos-ampel-rot-flaeche)", Regelblock(".epos-gebimport-zeile--rot > td"), StringComparison.Ordinal);
        Assert.Contains("dashed var(--epos-warn-rahmen)", Regelblock(".epos-gebimport-zeile--gelb > td:first-child"), StringComparison.Ordinal);
        Assert.Contains("solid var(--epos-stufe-fehler)", Regelblock(".epos-gebimport-zeile--rot > td:first-child"), StringComparison.Ordinal);
        Assert.Contains("var(--epos-quelle-text)", Regelblock(".epos-gebimport-herkunft--gbxml,"), StringComparison.Ordinal);
        Assert.Contains("font-weight: 600", Regelblock(".epos-gebimport-herkunft--manuell"), StringComparison.Ordinal);

        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css"));
        int kontrast = css.IndexOf(".epos-gebimport-zeile--gelb > td:first-child { border-left: 4px dashed CanvasText; }", StringComparison.Ordinal);
        Assert.True(kontrast > css.LastIndexOf("@media (forced-colors: active)", kontrast, StringComparison.Ordinal),
                    "Der Kontrastmodus des Gebäudeimports fehlt");
        Assert.Contains(".epos-gebimport-zeile--rot > td:first-child { border-left: 4px solid CanvasText; }", css, StringComparison.Ordinal);

        // Die Beschriftung der Zeilenfelder ist nur für die Sprachausgabe da, das Zahlenfeld schmal.
        Assert.Contains("clip-path: inset(50%)", Regelblock(".epos-gebimport-zeilen .epos-feld-text,"), StringComparison.Ordinal);
        Assert.Contains("width: 7em", Regelblock(".epos-gebimport-zeilen input.epos-eingabe"), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Befund 26.09.2026 (Gebäudedialog, WebView2):</b> Ein Zug über ein Diagramm
    /// markierte Titel, Legende und Achsen, statt zu zoomen — die Zeichenfläche trug
    /// kein <c>user-select: none</c>, und das Modul fing <c>selectstart</c> nicht ab.
    /// Ein zweiter Zug auf markiertem Text wird im Browser zum Ziehen der Auswahl.
    /// Gemessen im Chromium (Playwright): vorher 67 markierte Zeichen nach einem
    /// Bereichszug, nachher keins. Das <c>pointerdown</c> bleibt ohne
    /// <c>preventDefault</c>, sonst verlöre die Fläche den Fokus für + − 0.
    /// </summary>
    [Fact]
    public void Diagrammflaeche_markiert_beim_Ziehen_keinen_Text()
    {
        string block = Regelblock(".epos-diagramm-svg-flaeche {");
        Assert.Contains("user-select: none", block, StringComparison.Ordinal);
        Assert.Contains("-webkit-user-select: none", block, StringComparison.Ordinal);
        Assert.Contains("touch-action: none", block, StringComparison.Ordinal);

        // Das Hexfeld des Farbwaehlers bleibt markierbar.
        Assert.Contains("user-select: text", Regelblock(".epos-diagramm-svg-flaeche .epos-farbwahl"), StringComparison.Ordinal);

        string js = File.ReadAllText(Path.Combine(Wwwroot(), "epos-diagramm.js"));
        int a = js.IndexOf("an(flaeche, \"selectstart\"", StringComparison.Ordinal);
        Assert.True(a >= 0, "epos-diagramm.js faengt selectstart nicht ab");
        Assert.Contains("e.preventDefault()", js.Substring(a, Math.Min(200, js.Length - a)), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Stufe G6c, Welle D2: der Grundriss des Gebäudeimports.</b> Die zehn Zonenfarben stehen als Token in
    /// <c>:root</c> und kommen über die Klasse der Stelle an Fläche und Legende; ohne Zone grau, schematisch
    /// gestrichelt, der Rand bei jedem Maßstab 1 px (<c>vector-effect</c>). Im Kontrastmodus Canvas mit
    /// CanvasText-Rand. Die Andockung neben der Zonenliste bricht mit <c>flex-wrap</c> um, ohne Medienabfrage.
    /// </summary>
    [Fact]
    public void G6c_Der_Grundriss_faerbt_mit_Tokens_und_die_Andockung_bricht_um()
    {
        string wurzel = Regelblock(":root {");
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains("--epos-grundriss-zone-" + i + ": #", wurzel, StringComparison.Ordinal);
            Assert.Contains("var(--epos-grundriss-zone-" + i + ")", Regelblock(".epos-gebansicht-zone--" + i + " {"), StringComparison.Ordinal);
        }
        Assert.Contains("--epos-grundriss-ohnezone: #", wurzel, StringComparison.Ordinal);
        Assert.Contains("var(--epos-grundriss-ohnezone)", Regelblock(".epos-gebansicht-zone--ohne {"), StringComparison.Ordinal);

        string flaeche = Regelblock(".epos-gebansicht-raum polygon {");
        Assert.Contains("fill: var(--epos-gebansicht-farbe)", flaeche, StringComparison.Ordinal);
        Assert.Contains("vector-effect: non-scaling-stroke", flaeche, StringComparison.Ordinal);
        Assert.Contains("stroke-dasharray", Regelblock(".epos-gebansicht-raum--schematisch polygon {"), StringComparison.Ordinal);
        Assert.Contains("background: var(--epos-gebansicht-farbe)", Regelblock(".epos-gebansicht-farbfeld {"), StringComparison.Ordinal);
        Assert.Contains("fill: var(--epos-text)", Regelblock(".epos-gebansicht-raum text {"), StringComparison.Ordinal);

        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css"));
        int kontrast = css.IndexOf(".epos-gebansicht-raum polygon { fill: Canvas; stroke: CanvasText; }", StringComparison.Ordinal);
        Assert.True(kontrast > 0 && kontrast > css.LastIndexOf("@media (forced-colors: active)", kontrast, StringComparison.Ordinal),
                    "Der Kontrastmodus des Grundrisses fehlt");

        Assert.Contains("flex-wrap: wrap", Regelblock(".epos-gebimport-zonenblock {"), StringComparison.Ordinal);
        Assert.Contains("min-width: 0", Regelblock(".epos-gebimport-grundrissspalte {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Auftrag #572 (Befund 26.09.2026):</b> Die Zapfprofil-Überlagerung IN der
    /// Überlagerung „Brauchwasser…" stand nicht im Fenster, sondern im Kasten der äußeren —
    /// links und rechts abgeschnitten, mit Querrollbalken, die Wirtsliste schien unten durch.
    /// Ursache war <c>transform: translate(-50%, -50%)</c> an <c>.epos-ueberlagerung</c>: Ein
    /// transform macht das Element zum umschließenden Block jedes <c>position: fixed</c>-
    /// Nachfahren. Zentriert wird jetzt über <c>inset: 0</c>, <c>margin: auto</c> und
    /// <c>height: fit-content</c>; quer rollt die Überlagerung nie, und der Wirt rollt nicht,
    /// solange sie steht. Gemessen (Chromium, 1 280 × 800 und 1 024 × 700): vorher
    /// Querüberlauf der äußeren 165 bzw. 43 px und keine Ecke der inneren obenauf, nachher
    /// 0 px und alle vier Ecken sichtbar.
    /// </summary>
    [Fact]
    public void U572_Die_Ueberlagerung_steht_ohne_transform_im_Fenster()
    {
        string block = Regelblock(".epos-ueberlagerung {");

        Assert.DoesNotContain("transform", block, StringComparison.Ordinal);
        Assert.Contains("position: fixed", block, StringComparison.Ordinal);
        Assert.Contains("inset: 0", block, StringComparison.Ordinal);
        Assert.Contains("margin: auto", block, StringComparison.Ordinal);
        Assert.Contains("height: fit-content", block, StringComparison.Ordinal);
        Assert.Contains("overflow-x: hidden", block, StringComparison.Ordinal);
        Assert.Contains("overscroll-behavior: contain", block, StringComparison.Ordinal);

        // Kein anderes Hausblatt setzt einen transform auf die Ueberlagerung.
        foreach (string datei in Stilblaetter())
        {
            string css = File.ReadAllText(datei);
            foreach (Match m in Regex.Matches(css, @"[^{}]*\.epos-ueberlagerung[^{}]*\{([^}]*)\}"))
                Assert.DoesNotContain("transform:", m.Groups[1].Value, StringComparison.Ordinal);
        }

        Assert.Contains("min-width: 0", Regelblock(".epos-ueberlagerung-inhalt {"), StringComparison.Ordinal);
        Assert.Contains("overflow: hidden", Regelblock("html:has(.epos-ueberlagerung) {"), StringComparison.Ordinal);
        Assert.Contains("overflow-y: hidden", Regelblock(".epos-ueberlagerung:has(.epos-ueberlagerung) {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Auftrag #572:</b> In einer langen Überlagerung haftet die Fußleiste des
    /// eingebetteten Dialogs (die <c>SpeichernLeiste</c>, erkannt an ihrer Statusspanne) am
    /// unteren Rand des Rollbereichs, und die Zeigerzeile eines Diagramms haftet darüber — im
    /// Gebäudebedarf stand der Wert am Zeiger unter dem Raumtemperaturbild sonst unter dem
    /// unteren Rand. Gemessen: Zeigerzeile vorher 2 px unter dem sichtbaren Rand, nachher
    /// direkt über der Fußleiste; Fußleiste bei Rollstand 0 sichtbar, am Ende bündig mit dem
    /// Rand der Überlagerung.
    /// </summary>
    [Fact]
    public void U572_Fussleiste_und_Zeigerzeile_haften_in_der_Ueberlagerung()
    {
        string fuss = Regelblock(".epos-ueberlagerung-inhalt > .epos-dialog > .epos-leiste:has(> .epos-status) {");
        Assert.Contains("position: sticky", fuss, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(-1 * var(--epos-karte-rand))", fuss, StringComparison.Ordinal);
        Assert.Contains("background: var(--epos-karte-flaeche)", fuss, StringComparison.Ordinal);

        string zeiger = Regelblock(".epos-ueberlagerung-inhalt .epos-diagramm-zeigerzeile {");
        Assert.Contains("position: sticky", zeiger, StringComparison.Ordinal);
        Assert.Contains("bottom: var(--epos-ueberlagerung-fuss)", zeiger, StringComparison.Ordinal);

        Assert.Contains("--epos-ueberlagerung-fuss: 0px", Regelblock(".epos-ueberlagerung {"), StringComparison.Ordinal);
        Assert.Contains("--epos-ueberlagerung-fuss: calc(", Regelblock(
            ".epos-ueberlagerung:has(> .epos-ueberlagerung-inhalt > .epos-dialog > .epos-leiste > .epos-status) {"),
            StringComparison.Ordinal);

        // Die SpeichernLeiste traegt ihre Statusspanne als direktes Kind - daran haengt die Regel.
        string leiste = File.ReadAllText(Path.Combine(Wwwroot(), "..", "Bausteine", "SpeichernLeiste.razor"));
        Assert.Matches(@"<div class=""epos-leiste"">[\s\S]*?<span class=""epos-status ", leiste);
    }

    /// <summary>
    /// <b>Nachtrag N35 zu #572 (Blattwechsel):</b> Das Zapfprofil steht als BLATT im Dialog
    /// „Brauchwasser…" statt als Überlagerung in der Überlagerung. Damit es nichts von seinem Wirt
    /// verliert, tragen drei Regeln, was die Überlagerung ihm gab: Das Blatt weitet die tragende
    /// Überlagerung auf das breite Maß (der Gebäudekatalog weiß nichts vom Blatt), seine Fußleiste
    /// haftet wie die einer eingebetteten Komponente, und der eingebettete Dialog trägt keinen
    /// zweiten Rand. Gemessen (Chromium, 1 280 × 800 und 1 024 × 700): Überlagerung 1 229 bzw.
    /// 983 px breit, Querüberlauf 0, eine Überlagerung statt zwei, zwei Hilfepillen.
    /// </summary>
    [Fact]
    public void N35_Das_Blatt_weitet_die_Ueberlagerung_und_haftet_mit_seiner_Fussleiste()
    {
        string breit = Regelblock(".epos-ueberlagerung:has(.epos-blatt--breit) {");
        Assert.Contains("width: min(96vw, 1400px)", breit, StringComparison.Ordinal);
        Assert.Contains("max-height: 94vh", breit, StringComparison.Ordinal);

        // Dasselbe Maß wie die ausdrückliche Zusatzklasse - eine Zahl, zwei Wege.
        string ausdruecklich = Regelblock(".epos-ueberlagerung--breit {");
        Assert.Contains("width: min(96vw, 1400px)", ausdruecklich, StringComparison.Ordinal);

        string fuss = Regelblock(".epos-blatt-inhalt > .epos-dialog > .epos-leiste:has(> .epos-status) {");
        Assert.Contains("position: sticky", fuss, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(-1 * var(--epos-karte-rand))", fuss, StringComparison.Ordinal);
        Assert.Contains("background: var(--epos-karte-flaeche)", fuss, StringComparison.Ordinal);

        Assert.Contains("--epos-ueberlagerung-fuss: calc(", Regelblock(
            ".epos-ueberlagerung:has(.epos-blatt-inhalt > .epos-dialog > .epos-leiste > .epos-status) {"),
            StringComparison.Ordinal);

        string dialog = Regelblock(".epos-blatt-inhalt > .epos-dialog {");
        Assert.Contains("padding: 0", dialog, StringComparison.Ordinal);

        // Der Baustein traegt die Klassen, an denen die Regeln haengen.
        string blatt = File.ReadAllText(Path.Combine(Wwwroot(), "..", "Bausteine", "Blattwechsel.razor"));
        Assert.Contains("class=\"epos-blatt-inhalt\"", blatt, StringComparison.Ordinal);
        Assert.Contains("class=\"epos-blatt @ZusatzKlasse\"", blatt, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Konzept Navigation Berichte &amp; Kosten, Variante A (A1/A2):</b> Die dunkle
    /// Seitennavigation mit ihren sechs Festfarben ist fort; die Statuszeile der Reiter warnt
    /// mit dem TOKEN <c>--epos-warn-text</c> und im Kontrastmodus mit einer Systemfarbe, die
    /// Kurzform steht erst unter 900 px, und das Leistenende bricht dort in eine eigene Zeile.
    /// </summary>
    [Fact]
    public void BN_A_Die_Statuszeile_der_Reiter_warnt_mit_Token_und_kuerzt_unter_900px()
    {
        // Zeilenenden normalisieren: Der Windows-Arbeitsbaum haelt das Blatt mit CRLF,
        // die mehrzeiligen Suchtexte unten stehen mit LF.
        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css")).Replace("\r\n", "\n");
        Assert.DoesNotContain(".epos-navigation", css, StringComparison.Ordinal);
        Assert.DoesNotContain("#23282d", css, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("color: var(--epos-warn-text)", Regelblock(".epos-reiter-status--warnung"), StringComparison.Ordinal);
        Assert.Contains("color: var(--epos-text-leise)", Regelblock(".epos-reiter-status {"), StringComparison.Ordinal);
        Assert.Contains("display: none", Regelblock(".epos-reiter-status-kurz"), StringComparison.Ordinal);
        Assert.Contains("min-height: 52px", Regelblock(".epos-reiter-knopf--status"), StringComparison.Ordinal);

        int schmal = css.IndexOf(
            "@media (max-width: 900px) {\n    .epos-reiter-status-lang {\n        display: none;\n    }\n\n"
            + "    .epos-reiter-status-kurz {\n        display: inline;\n    }", StringComparison.Ordinal);
        Assert.True(schmal > 0, "Die Kurzform unter 900 px fehlt");
        int ende = css.IndexOf(".epos-reiter-kopfzeile > .epos-reiter-leistenende {\n        flex: 1 1 100%;", schmal,
                               StringComparison.Ordinal);
        Assert.True(ende > schmal && ende < css.IndexOf("\n}\n", schmal, StringComparison.Ordinal),
                    "Das Leistenende bricht unter 900 px nicht in eine eigene Zeile");

        int kontrast = css.IndexOf(".epos-reiter-status--warnung { color: LinkText; }", StringComparison.Ordinal);
        Assert.True(kontrast > 0 && kontrast > css.LastIndexOf("@media (forced-colors: active)", kontrast, StringComparison.Ordinal)
                    && css.LastIndexOf("@media (forced-colors: active)", kontrast, StringComparison.Ordinal) > schmal,
                    "Der Kontrastmodus der Warnung fehlt");
    }

    /// <summary>
    /// <b>Stufe KP2, Welle U0b (Entwurf KP2 Festlegung 7):</b> Das Wochenraster der Kalenderkarten
    /// ordnet sich nach der Breite seines BEHÄLTERS an — Container-Abfrage wie der Katalograhmen,
    /// keine Medienabfrage —: je Tag 4 × 6 als Vorgabe, 2 × 12 ab 600 px, 7 × 24 als feste Tabelle ab
    /// 1 150 px; jede Zelle behält das Berührungsmaß. „aus" ist ein eigener Zellzustand, nicht nur
    /// eine Farbe. Ohne die Zusätze bleibt das Raster des Bestands (AK1) stehen, wie es war. Die
    /// Maße im Browser misst die Wirtseite <c>/konditionierungsprobe</c> der Rasterprobe.
    /// </summary>
    [Fact]
    public void KP2_Das_Wochenraster_bricht_am_Behaelter_um_und_aus_ist_ein_eigener_Zustand()
    {
        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css")).Replace("\r\n", "\n");

        string wurzel = Regelblock(".epos-wochenraster--umbrechend {");
        Assert.Contains("container-type: inline-size", wurzel, StringComparison.Ordinal);
        Assert.Contains("container-name: epos-wochenraster", wurzel, StringComparison.Ordinal);

        Assert.Contains("grid-template-columns: repeat(6, minmax(var(--epos-touchziel), 1fr))",
                        Regelblock(".epos-wochenraster--umbrechend .epos-wochenraster-tabelle tbody tr {"),
                        StringComparison.Ordinal);
        Assert.Contains("content: attr(data-stunde)",
                        Regelblock(".epos-wochenraster--umbrechend .epos-wochenraster-tabelle td::before {"),
                        StringComparison.Ordinal);

        string mittel = Abfrageblock(css, "@container epos-wochenraster (min-width: 600px) {");
        Assert.Contains("grid-template-columns: 3rem repeat(12, minmax(var(--epos-touchziel), 1fr))", mittel,
                        StringComparison.Ordinal);
        Assert.Contains("grid-row: span 2", mittel, StringComparison.Ordinal);

        string breit = Abfrageblock(css, "@container epos-wochenraster (min-width: 1150px) {");
        Assert.Contains("table-layout: fixed", breit, StringComparison.Ordinal);
        // 46 px Tagesspalte + 24 × (44 px Zelle + 2 px Polster) = 1 150 px: An der Schwelle hat
        // jede Zelle ihr Berührungsmaß (mit 3rem waren es 43,9 px, gemessen im Browser).
        Assert.Contains("width: 46px", breit, StringComparison.Ordinal);
        Assert.Contains("display: table-row;", breit, StringComparison.Ordinal);
        Assert.Contains("content: none", breit, StringComparison.Ordinal);

        // Der Bestand: ohne Zusatz rollt das Raster quer in seiner Hülle, die Zelle hat ihr Maß.
        Assert.Contains("overflow-x: auto", Regelblock(".epos-wochenraster-huelle {"), StringComparison.Ordinal);
        Assert.Contains("width: 3.6em", Regelblock(".epos-wochenraster-tabelle .epos-eingabe {"), StringComparison.Ordinal);

        string aus = Regelblock(".epos-eingabe--aus {");
        Assert.Contains("border-style: dashed", aus, StringComparison.Ordinal);
        Assert.Contains("font-style: italic", aus, StringComparison.Ordinal);
        Assert.DoesNotContain("#", aus, StringComparison.Ordinal);   // Farben nur als Token
    }

    /// <summary>
    /// <b>Stufe KP2, Welle U1 (Teilkonzept 7.1, Entwurf KP2 Festlegung 7):</b> Der Reiter
    /// „Konditionierung" bricht am BEHÄLTER um, nicht am Fenster — in der Überlagerung griffe eine
    /// Medienabfrage nie. Schmal (Vorgabe) steht nur die Spalte und die Karte der gewählten Größe, die
    /// fünf Reiter darüber; ab 900 px Behälterbreite die ganze Matrix als Tabelle (feste Spaltenanteile,
    /// jedes Feld nimmt die Breite seiner Zelle) und die Karten darunter, die Reiter fallen. Die
    /// Beschriftung einer Zelle wird nur vorgelesen (die Tabelle trägt Kopf und Zeile), und die weich
    /// gesperrte Kühlspalte ist ein Knopf in der Zellenbreite. Farben nur als Token.
    /// </summary>
    [Fact]
    public void KP2_Der_Reiter_Konditionierung_bricht_am_Behaelter_bei_900_px_um()
    {
        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css")).Replace("\r\n", "\n");

        string wurzel = Regelblock(".epos-kond {");
        Assert.Contains("container-type: inline-size", wurzel, StringComparison.Ordinal);
        Assert.Contains("container-name: epos-kond", wurzel, StringComparison.Ordinal);
        Assert.Contains("min-width: 0", wurzel, StringComparison.Ordinal);

        string tabelle = Regelblock(".epos-kond-matrix {");
        Assert.Contains("table-layout: fixed", tabelle, StringComparison.Ordinal);
        Assert.Contains("width: 100%", tabelle, StringComparison.Ordinal);
        Assert.Contains("width: 100%", Regelblock(".epos-kond-zelle .epos-eingabe {"), StringComparison.Ordinal);
        Assert.Contains("min-width: 0", Regelblock(".epos-kond-zelle .epos-eingabe {"), StringComparison.Ordinal);

        // Schmal: die übrigen Spalten und Karten sind aus; kein display:flex an einer Zelle.
        int schmal = css.IndexOf("\n.epos-kond-matrix th[scope=col]:not(.epos-kond--aktiv),\n" +
                                 ".epos-kond-matrix td.epos-kond-zelle:not(.epos-kond--aktiv),\n" +
                                 ".epos-kond-karte:not(.epos-kond--aktiv) {\n    display: none;", StringComparison.Ordinal);
        Assert.True(schmal > 0, "Die schmale Anordnung (nur die gewählte Spalte) fehlt");
        Assert.DoesNotContain("display: flex", Regelblock(".epos-kond-zelle {"), StringComparison.Ordinal);

        string breit = Abfrageblock(css, "@container epos-kond (min-width: 900px) {");
        Assert.Contains(".epos-kond-groessen {\n        display: none;", breit, StringComparison.Ordinal);
        Assert.Contains("display: table-cell;", breit, StringComparison.Ordinal);
        Assert.Contains(".epos-kond-karte:not(.epos-kond--aktiv) {\n        display: block;", breit, StringComparison.Ordinal);
        Assert.DoesNotContain("@media", css.Substring(css.IndexOf("\n.epos-kond {", StringComparison.Ordinal),
                                                     css.IndexOf("/* FORMULARRASTER (Anwenderwunsch", StringComparison.Ordinal)
                                                     - css.IndexOf("\n.epos-kond {", StringComparison.Ordinal)),
                              StringComparison.Ordinal);

        // Die Beschriftung der Zelle ist nur vorgelesen - sichtbar versteckt, nicht display:none.
        string text = Regelblock(".epos-kond-zelle .epos-feld-text {");
        Assert.Contains("clip-path: inset(50%)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("display: none", text, StringComparison.Ordinal);

        Assert.Contains("width: 100%", Regelblock(".epos-kond-gesperrt {"), StringComparison.Ordinal);

        // Die Knopfzeilen (Kopf und Karte) brechen zwischen den Knöpfen um, nie im Wort — gemessen in der
        // Konditionierungsprobe: „Zurückneh|men“ bei 390 px, „Verwerfe|n“ in der Karte bei 1 180 px.
        Assert.Contains("flex-wrap: wrap", Regelblock(".epos-kond-kopf,"), StringComparison.Ordinal);
        // Fünf Karten in einer Reihe, sobald die Matrix als Tabelle steht (Behälter ab 900 px).
        Assert.Contains("repeat(auto-fill, minmax(200px, 1fr))", breit, StringComparison.Ordinal);

        foreach (string regel in new[] { ".epos-kond-matrix th,", ".epos-kond-leer {", ".epos-kond-karte {",
                                         ".epos-kond-karte-zustand {" })
            Assert.DoesNotContain("#", Regelblock(regel), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die Hilfepille hält das Berührungsmaß</b> (Stufe KP2, Welle U1, Nebenbefund aus U0b): Beide
    /// Felder — Fragezeichen und Assistent — und der Ring des KI-Knopfs sind mindestens
    /// <c>--epos-touchziel</c> (44 px) hoch und breit; das Hausmaß 28 px des WinForms-Vorbilds
    /// (<c>--epos-infoknopf</c>) gilt in der Oberfläche nicht mehr — gemessen waren 28 × 26 px.
    /// </summary>
    [Fact]
    public void KP2_Die_Hilfepille_und_der_KI_Ring_halten_das_Beruehrungsmass()
    {
        // Die Felder bestimmen die Höhe; eine feste Höhe der Pille schnitte mit overflow: hidden die
        // Trefferfläche ab.
        Assert.DoesNotContain("height:", Regelblock(".epos-hilfepille {"), StringComparison.Ordinal);
        string feld = Regelblock(".epos-hilfepille__feld {");
        Assert.Contains("min-width: var(--epos-touchziel)", feld, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--epos-touchziel)", feld, StringComparison.Ordinal);

        string ring = Regelblock(".epos-kiknopf {");
        Assert.Contains("width: var(--epos-touchziel)", ring, StringComparison.Ordinal);
        Assert.Contains("height: var(--epos-touchziel)", ring, StringComparison.Ordinal);

        string css = File.ReadAllText(Path.Combine(Wwwroot(), "epos-ui.css"));
        Assert.DoesNotContain("var(--epos-infoknopf)", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Gebäude-Katalogeditor ist nicht auf 1 160 px gedeckelt</b> (Stufe KP2, Welle U1,
    /// Nebenbefund aus U0b; Vorbild <c>.epos-wp-anlage</c>): Die Matrix aus fünf Spalten ist keine
    /// Bandwurmzeile — im breiten Fenster und in der breiten Überlagerung nimmt der Editor die Breite,
    /// und der Behälter der Konditionierung bricht an SEINER Breite um.
    /// </summary>
    [Fact]
    public void KP2_Der_Gebaeudeeditor_nimmt_die_Breite_ohne_Deckel()
    {
        Assert.Contains("max-width: 1160px", Regelblock(".epos-dialog {"), StringComparison.Ordinal);
        Assert.Contains("max-width: none", Regelblock(".epos-dialog.epos-gebk-editor {"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Rumpf einer At-Regel (<c>@container …</c>, <c>@media …</c>) samt ihrer Regeln — bis zur
    /// Klammer, die sie schließt.
    /// </summary>
    private static string Abfrageblock(string css, string kopf)
    {
        int a = css.IndexOf("\n" + kopf, StringComparison.Ordinal);
        Assert.True(a >= 0, "Die Abfrage \"" + kopf + "\" steht nicht im Hausblatt");
        int auf = css.IndexOf('{', a);
        int tiefe = 0;
        for (int i = auf; i < css.Length; i++)
        {
            if (css[i] == '{') tiefe++;
            else if (css[i] == '}' && --tiefe == 0) return css.Substring(auf + 1, i - auf - 1);
        }
        Assert.Fail("Die Abfrage \"" + kopf + "\" wird nicht geschlossen");
        return "";
    }

    /// <summary>Der Rumpf der Regel zu <paramref name="selektor"/> im Hausblatt.</summary>
    private static string Regelblock(string selektor) => Regelblock(selektor, "epos-ui.css");

    /// <summary>Der Rumpf der Regel zu <paramref name="selektor"/> in diesem Stilblatt.</summary>
    private static string Regelblock(string selektor, string datei)
    {
        string css = File.ReadAllText(Path.Combine(Wwwroot(), datei));

        int a = css.IndexOf("\n" + selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, "Die Regel \"" + selektor + "\" steht nicht in " + datei);

        int auf = css.IndexOf('{', a);
        int zu = css.IndexOf('}', auf);
        Assert.True(auf > 0 && zu > auf, "Die Regel \"" + selektor + "\" hat keinen Rumpf");
        return css.Substring(auf + 1, zu - auf - 1);
    }
}

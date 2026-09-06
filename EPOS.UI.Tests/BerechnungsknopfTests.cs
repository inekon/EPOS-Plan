using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Der WÄCHTER über die Infoknöpfe der Hilferubrik „Berechnung" (H13,
/// Anwenderwunsch vom 06.09.2026).
///
/// <para><b>Worum es geht.</b> Das Paket legt je Rechenwegseite einen ZWEITEN
/// Hilfeschlüssel an — <c>&lt;Formname&gt;.Berechnung</c> neben dem bestehenden
/// <c>&lt;Formname&gt;.btn_Help</c> —, und im Dialog sitzt dazu ein
/// <c>InfoKnopf</c> am Kopf des Abschnitts, der die Rechnung parametriert. Beide
/// Hälften gehören zusammen, und beide altern für sich:</para>
///
/// <list type="bullet">
/// <item>Eine Zeile in <c>help_mapping.txt</c> ohne Knopf ist eine Zusage an
///       niemanden — sie fällt keinem auf, weil nichts sie aufruft.</item>
/// <item>Ein Knopf ohne Zeile ist unter Windows ABGESCHALTET (F3) und im
///       Blazor-Dialog folgenlos: sichtbar, aber tot.</item>
/// </list>
///
/// <para><b>Warum der Fall den Quelltext liest.</b> Die Zuordnungsdatei liegt im
/// WinForms-Projekt (<c>net10.0-windows</c>); ein Test, der es referenziert, liefe
/// weder auf dem ubuntu-Läufer noch auf macOS. Derselbe Weg wie in
/// <see cref="HuellenwegTests"/> und <c>StilblattTests</c>.</para>
///
/// <para><b>Wo ein Schlüssel stehen darf.</b> In der Razor-Komponente selbst (der
/// Regelfall — der Vorgabewert des Parameters) oder in der Windows-Hülle, wenn EINE
/// Komponente mehrere Ausprägungen bedient und die Hülle je Ausprägung einen anderen
/// Schlüssel hereinreicht (so bei den drei Bedarfsprofil-Dialogen Prozesswärme,
/// Stromverbraucher und Brauchwasser).</para>
///
/// <para><b>Fassung 2 (06.09.2026).</b> Dazu kommt die dritte Hälfte des Wegs: Wohin der
/// Knopf führt, muss auch etwas taugen. Der Anwender wollte „die Definition der Parameter
/// und Variablen" und die Formeln „in mathematischer Schreibweise"; die letzten zwei Fälle
/// halten deshalb fest, dass jede Seite, auf die ein Knopf dieses Pakets zeigt, den
/// Abschnitt „Formelzeichen und Parameter" mit beiden Tabellen und mindestens eine
/// nummerierte Anzeige-Formel trägt — und dass die Rubrikstartseite die Schreibweise
/// erklärt, die alle 13 Seiten teilen.</para>
///
/// <para><b>Fassung 3 (06.09.2026).</b> Der Anwender lässt die Math-Erweiterung
/// installieren und wünschte die Formeln „wie LaTeX" samt der Definition jeder
/// Variablen „unter der verwendeten Formel". Für die sechs Seiten dieses Teils gilt
/// deshalb zusätzlich: jede Anzeige-Gleichung als <c>&lt;math&gt;</c> mit ihrer
/// Legende darunter. Der LaTeX-Riegel der Fassung 2 wird dabei zur ALLOWLIST — nicht
/// mehr „kein Backslash", sondern „nur was WikiTexVC kennt".</para>
/// </summary>
public sealed class BerechnungsknopfTests
{
    /// <summary>Das Schlüsselmuster des Pakets: <c>Form_Irgendwas.Berechnung</c>.</summary>
    private static readonly Regex Schluesselmuster =
        new(@"\bForm_[A-Za-z0-9_]+\.Berechnung\b", RegexOptions.Compiled);

    /// <summary>Eine Zuordnungszeile <c>Schlüssel = Ziel</c>.</summary>
    private static readonly Regex Zuordnungszeile =
        new(@"^\s*([A-Za-z0-9_.]+)\s*=\s*(\S.*?)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Eine Anzeige-Formel: eingerückte Zeile mit der laufenden Nummer am Zeilenende,
    /// gesetzt in <c>&lt;math&gt;</c> (Fassung 3) ODER in <c>&lt;big&gt;</c> (Fassung 2).
    /// Bis beide Teile zusammengeführt sind, liegen beide Formen im selben Ordner.
    /// </summary>
    // TODO Zusammenführung Fassung 3: auf "<math>" allein verengen.
    private static readonly Regex Anzeigeformel =
        new(@"^:\s*(?:<math>.+</math>|<big>.+</big>)(?:\s|&nbsp;)*\(\d+\)\s*$",
            RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Eine Anzeige-Gleichung der Fassung 3 — LaTeX in <c>&lt;math&gt;</c>.</summary>
    private static readonly Regex Anzeigegleichung =
        new(@"^:\s*<math>.+</math>(?:\s|&nbsp;)*\(\d+\)\s*$",
            RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Eine Legendezeile der Fassung 3 — doppelt eingerückt, Zeichen zuerst.</summary>
    private static readonly Regex Legendezeile =
        new(@"^::\s*<math>", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Ein LaTeX-Befehl — <c>\frac</c>, <c>\sum</c>, <c>\cdot</c> …</summary>
    private static readonly Regex LatexBefehl = new(@"\\([A-Za-z]+)", RegexOptions.Compiled);

    /// <summary>
    /// Die LaTeX-Teilmenge der Rubrik (Fassung 3) — nur was WikiTexVC sicher kennt.
    /// Sie steht wortgleich in <c>EPOS.Kern.Tests/BerechnungsHilfeTests.cs</c>; die
    /// zwei Wächter sehen dieselbe Seite von zwei Seiten (Markup im Kern, Datei im
    /// Arbeitsbaum) und dürfen sich dabei nicht widersprechen.
    /// </summary>
    private static readonly HashSet<string> ErlaubteBefehle = new(StringComparer.Ordinal)
    {
        "frac", "sqrt", "sum", "int", "prod", "min", "max", "cdot",
        "left", "right", "lvert", "rvert",
        "mathrm", "text", "operatorname", "displaystyle", "begin", "end", "quad",
        "le", "ge", "ne", "approx", "pm", "to", "infty", "in", "dots",
        "eta", "vartheta", "rho", "lambda", "alpha", "beta", "gamma",
        "varepsilon", "tau", "varphi", "Delta", "Sigma",
        "pi", "omega", "kappa", "Psi", "ell", "dot"
    };

    /// <summary>
    /// Alle dreizehn Seiten der Rubrik — seit der Zusammenführung von Teil A und Teil B
    /// (06.09.2026) gelten die Fassung-2-Fälle für jede von ihnen.
    /// </summary>
    private static readonly string[] SeitenDerRubrik =
    {
        "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme",
        "Strombedarf", "Wärmequelle Erdreich", "Heizkessel", "BHKW", "Wärmepumpe",
        "Pufferspeicher", "Solarthermie", "Photovoltaik", "Stromspeicher"
    };

    /// <summary>
    /// Die SECHS Seiten, die Teil A der Fassung 3 auf LaTeX umgestellt hat. Nur für sie
    /// gilt die Bauform „Gleichung in <c>&lt;math&gt;</c> mit Legende darunter".
    /// </summary>
    // TODO Zusammenführung Fassung 3: auf SeitenDerRubrik umstellen
    private static readonly string[] SeitenDiesesTeils =
    {
        "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme",
        "Strombedarf", "Wärmequelle Erdreich"
    };

    // =====================================================================
    //  Die zwei Richtungen
    // =====================================================================

    /// <summary>
    /// Jeder <c>*.Berechnung</c>-Schlüssel der Zuordnungsdatei wird auch benutzt.
    /// </summary>
    [Fact]
    public void Jeder_Berechnungsschluessel_hat_einen_Infoknopf()
    {
        IReadOnlyDictionary<string, string> zuordnung = Berechnungszuordnungen();
        IReadOnlyDictionary<string, List<string>> imQuelltext = SchluesselImQuelltext();

        Assert.True(zuordnung.Count >= 1,
            "help_mapping.txt führt keine einzige Zeile '<Form>.Berechnung = Berechnung/<Seite>' " +
            "(Abschnitt 'H13 - Rubrik Berechnung' am Dateiende).");

        var funde = zuordnung.Keys.Where(k => !imQuelltext.ContainsKey(k)).ToList();

        Assert.True(funde.Count == 0,
            "Diese Schlüssel stehen in help_mapping.txt, aber in keinem Dialog:\n" +
            string.Join("\n", funde));
    }

    /// <summary>
    /// Umgekehrt: Jeder Schlüssel, der im Quelltext steht, hat auch ein Ziel. Ohne
    /// Zeile bleibt der Knopf unter Windows abgeschaltet.
    /// </summary>
    [Fact]
    public void Jeder_Infoknopf_hat_eine_Zeile_in_der_Zuordnung()
    {
        IReadOnlyDictionary<string, string> zuordnung = Berechnungszuordnungen();
        IReadOnlyDictionary<string, List<string>> imQuelltext = SchluesselImQuelltext();

        var funde = imQuelltext
            .Where(p => !zuordnung.ContainsKey(p.Key))
            .Select(p => p.Key + "  (" + string.Join(", ", p.Value) + ")")
            .ToList();

        Assert.True(funde.Count == 0,
            "Diese Schlüssel stehen im Quelltext, aber nicht in help_mapping.txt — " +
            "der Knopf bliebe wirkungslos:\n" + string.Join("\n", funde));
    }

    /// <summary>
    /// Ein Schlüssel gehört GENAU EINEM Dialog. Zwei Komponenten mit demselben
    /// Berechnungsschlüssel wären zwei Wege auf dieselbe Seite, ohne dass die
    /// Zuordnungsdatei das noch zeigte.
    ///
    /// <para><b>Gezählt werden Razor-Dateien.</b> Dass ein Schlüssel ZUSÄTZLICH in
    /// einer Hülle steht, ist der Regelfall bei einer Komponente mit mehreren
    /// Ausprägungen: <c>BedarfsProfileDialog</c> trägt den Vorgabewert für die
    /// Prozesswärme, und <c>BedarfsProfileHuelle</c> reicht je Ausprägung den
    /// passenden Schlüssel herein. Beides ist derselbe Dialog.</para>
    /// </summary>
    [Fact]
    public void Jeder_Schluessel_gehoert_genau_einem_Dialog()
    {
        var mehrfach = SchluesselImQuelltext()
            .Select(p => new
            {
                p.Key,
                Razor = p.Value.Where(d => d.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                                .Distinct(StringComparer.Ordinal).ToList()
            })
            .Where(p => p.Razor.Count > 1)
            .Select(p => p.Key + ": " + string.Join(", ", p.Razor))
            .ToList();

        Assert.True(mehrfach.Count == 0,
            "Diese Schlüssel stehen in mehr als einer Razor-Komponente:\n" +
            string.Join("\n", mehrfach));
    }

    /// <summary>
    /// Das Ziel einer H13-Zeile zeigt in die Rubrik — <c>Berechnung/&lt;Seite&gt;</c>.
    /// Ein Tippfehler landete sonst auf einer allgemeinen Seite, und niemand sähe es.
    /// </summary>
    [Fact]
    public void Jedes_Ziel_zeigt_in_die_Rubrik_Berechnung()
    {
        foreach (var paar in Berechnungszuordnungen())
        {
            Assert.True(paar.Value.StartsWith("Berechnung/", StringComparison.Ordinal),
                paar.Key + " zeigt auf '" + paar.Value + "' statt auf 'Berechnung/<Seite>'.");
        }
    }

    /// <summary>
    /// Eine Razor-Datei, die einen Berechnungsschlüssel führt, führt auch einen
    /// <c>InfoKnopf</c>. Ein Schlüssel als bloßer Parameter ohne Knopf wäre eine
    /// Zeichenkette ohne Wirkung.
    /// </summary>
    [Fact]
    public void Jede_Razor_Datei_mit_Schluessel_traegt_einen_Infoknopf()
    {
        var funde = new List<string>();

        foreach (string pfad in Quelldateien())
        {
            if (!pfad.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)) continue;

            string quelltext = File.ReadAllText(pfad);
            if (!Schluesselmuster.IsMatch(quelltext)) continue;
            if (quelltext.Contains("<InfoKnopf", StringComparison.Ordinal)) continue;

            funde.Add(Path.GetFileName(pfad));
        }

        Assert.True(funde.Count == 0,
            "Diese Razor-Dateien führen einen Berechnungsschlüssel, aber keinen " +
            "<InfoKnopf>:\n" + string.Join("\n", funde));
    }

    // =====================================================================
    //  Fassung 2 — wohin der Knopf führt
    // =====================================================================

    /// <summary>
    /// Jede Seite, auf die ein Knopf dieses Pakets zeigt, trägt die Bauform der
    /// Fassung 2: den Abschnitt „Formelzeichen und Parameter" mit BEIDEN Tabellen,
    /// mindestens eine nummerierte Anzeige-Formel — und keine LaTeX-Auszeichnung.
    ///
    /// <para><b>Warum das hier steht und nicht nur im Kern-Prüfstand.</b> Die zwei
    /// Fälle oben sichern, dass Knopf und Zuordnungszeile zusammenpassen. Dieser
    /// sichert, dass das ZIEL der Zuordnung etwas taugt: Ein Knopf, der auf eine
    /// Seite ohne Zeichenerklärung führt, hält die Zusage des Anwenderwunsches
    /// nicht ein, obwohl beide Hälften der Verdrahtung stimmen.</para>
    ///
    /// <para>Das Wiki hat KEINE Math-Erweiterung (gemessen am 06.09.2026) — ein
    /// <c>math</c>-Block oder ein Backslash-Befehl erschiene dem Anwender als
    /// Klartext mitten im Satz. Deshalb der Riegel.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleSeitenDerRubrik))]
    public void Jeder_Knopf_fuehrt_auf_eine_Seite_der_Fassung_2(string seitenname)
    {
        // Der Knopf führt wirklich dorthin - sonst prüfte der Fall eine Seite, die
        // niemand aufruft.
        Assert.Contains(Berechnungszuordnungen().Values,
                        ziel => string.Equals(ziel, "Berechnung/" + seitenname, StringComparison.Ordinal));

        string pfad = Seitenpfad(seitenname);
        Assert.True(File.Exists(pfad), "Die Seite '" + seitenname + "' fehlt: " + pfad);

        string markup = File.ReadAllText(pfad);

        int zeichen = markup.IndexOf("== Formelzeichen und Parameter ==", StringComparison.Ordinal);
        int rechenweg = markup.IndexOf("== Rechenweg ==", StringComparison.Ordinal);

        Assert.True(zeichen >= 0, seitenname + ": der Abschnitt 'Formelzeichen und Parameter' fehlt.");
        Assert.True(rechenweg > zeichen,
            seitenname + ": 'Formelzeichen und Parameter' muss VOR dem Rechenweg stehen — " +
            "der Leser soll die Zeichen kennen, bevor er die erste Formel sieht.");

        Assert.Contains("! Symbol !! Bedeutung !! Einheit !! Herkunft", markup, StringComparison.Ordinal);
        Assert.Contains("! Symbol !! Bedeutung !! Einheit !! berechnet in", markup, StringComparison.Ordinal);

        Assert.True(Anzeigeformel.IsMatch(markup),
            seitenname + ": keine nummerierte Anzeige-Formel. Muster: " +
            "': <math>\\displaystyle Q_{\\mathrm{a}} = \\frac{\\sum … }{1\\,000}</math>  (4)'.");

        var fremd = LatexBefehl.Matches(markup)
            .Select(m => m.Groups[1].Value)
            .Where(b => !ErlaubteBefehle.Contains(b))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(fremd.Count == 0,
            seitenname + ": Befehl(e) außerhalb der LaTeX-Teilmenge der Rubrik: \\" +
            string.Join(", \\", fremd) + ". WikiTexVC wiese sie ab, und der Anwender sähe " +
            "an der Stelle der Formel eine rote Fehlerzeile.");
    }

    /// <summary>
    /// <b>Fassung 3:</b> Die Seiten, die dieses Paket umgestellt hat, setzen jede
    /// Anzeige-Gleichung als LaTeX in <c>&lt;math&gt;</c> und tragen unmittelbar
    /// darunter ihre Legende. Der Knopf führt damit auf eine Seite, auf der jedes
    /// Zeichen erklärt ist, ohne dass der Leser zur Symboltabelle zurückspringen muss —
    /// genau das war der Anwenderwunsch vom 06.09.2026.
    /// </summary>
    [Theory]
    [MemberData(nameof(AlleSeitenDiesesTeils))]
    public void Jeder_Knopf_dieses_Teils_fuehrt_auf_eine_Seite_der_Fassung_3(string seitenname)
    {
        string pfad = Seitenpfad(seitenname);
        Assert.True(File.Exists(pfad), "Die Seite '" + seitenname + "' fehlt: " + pfad);

        string[] zeilen = File.ReadAllText(pfad).Replace("\r\n", "\n").Split('\n');

        Assert.DoesNotContain("<big>", string.Join("\n", zeilen), StringComparison.Ordinal);
        Assert.Contains("Fassung 3: LaTeX-Formeln und Legenden", zeilen[0], StringComparison.Ordinal);

        int gleichungen = 0;
        int legenden = 0;

        for (int i = 0; i < zeilen.Length; i++)
        {
            if (Legendezeile.IsMatch(zeilen[i])) legenden++;
            if (!Anzeigegleichung.IsMatch(zeilen[i])) continue;

            gleichungen++;
            Assert.True(i + 1 < zeilen.Length && Legendezeile.IsMatch(zeilen[i + 1]),
                seitenname + ": auf '" + zeilen[i].Trim() + "' folgt keine Legendezeile.");
        }

        Assert.True(gleichungen >= 1, seitenname + ": keine Anzeige-Gleichung in <math>.");
        Assert.True(legenden >= gleichungen,
            seitenname + ": " + gleichungen + " Gleichungen, aber nur " + legenden +
            " Legendezeilen — je Gleichung gehört mindestens eine Zeile darunter.");
    }

    /// <summary>
    /// Die Rubrikstartseite erklärt die Schreibweise, die alle 13 Seiten teilen: dass
    /// die Formeln seit Fassung 3 als LaTeX in <c>&lt;math&gt;</c> stehen, wie eine
    /// Anzeige-Gleichung samt ihrer Legende aussieht, und die gemeinsame Zeichentabelle.
    /// Ohne diesen Abschnitt stünde auf jeder Seite eine Notation, die nirgends erklärt
    /// ist.
    /// </summary>
    [Fact]
    public void Die_Rubrikstartseite_erklaert_die_Schreibweise()
    {
        string pfad = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung",
                                   "_Index.wiki");
        Assert.True(File.Exists(pfad), "_Index.wiki nicht gefunden: " + pfad);

        string markup = File.ReadAllText(pfad);

        Assert.Contains("== Schreibweise ==", markup, StringComparison.Ordinal);
        Assert.Contains("Math-Erweiterung", markup, StringComparison.Ordinal);
        Assert.Contains("! Groesse !! Symbol !! Einheit", markup, StringComparison.Ordinal);

        // Die Gliederung der Startseite nennt den neuen Abschnitt - sonst verspräche sie
        // sechs Abschnitte und die Seiten trügen sieben.
        Assert.Contains("Formelzeichen und Parameter", markup, StringComparison.Ordinal);

        // Fassung 3: die Startseite nennt die Auszeichnung und die Legende-Regel. Ein
        // Leser, der ":: <math>…" sieht, soll wissen, dass das kein Tippfehler ist.
        Assert.Contains("Legende", markup, StringComparison.Ordinal);
        Assert.Contains("\\displaystyle", markup, StringComparison.Ordinal);
        Assert.Contains("LaTeX", markup, StringComparison.Ordinal);
    }

    /// <summary>Alle dreizehn Seiten der Rubrik als Theoriedaten.</summary>
    public static TheoryData<string> AlleSeitenDerRubrik()
    {
        var daten = new TheoryData<string>();
        foreach (string name in SeitenDerRubrik) daten.Add(name);
        return daten;
    }

    /// <summary>Die sechs Seiten dieses Teils als Theoriedaten (Fassung 3).</summary>
    // TODO Zusammenführung Fassung 3: auf AlleSeitenDerRubrik umstellen
    public static TheoryData<string> AlleSeitenDiesesTeils()
    {
        var daten = new TheoryData<string>();
        foreach (string name in SeitenDiesesTeils) daten.Add(name);
        return daten;
    }

    /// <summary>Der Pfad einer Seitendatei der Rubrik.</summary>
    private static string Seitenpfad(string seitenname) =>
        Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung", seitenname + ".wiki");

    // =====================================================================
    //  Gegenproben
    // =====================================================================

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser findet das Muster wirklich — sonst liefe jeder
    /// Fall oben über eine leere Menge und niemand merkte es.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_das_Muster()
    {
        Assert.Matches(Schluesselmuster, "Schluessel=\"Form_PV.Berechnung\"");
        Assert.Matches(Schluesselmuster,
            "[Parameter] public string HilfeSchluesselBerechnung { get; set; } = \"Form_Gebaeude.Berechnung\";");
        Assert.Matches(Schluesselmuster, "case BedarfsArt.Brauchwasser: return \"Form_Brauchwasser.Berechnung\";");

        Assert.DoesNotMatch(Schluesselmuster, "Schluessel=\"Form_PV.btn_Help\"");
        Assert.DoesNotMatch(Schluesselmuster, "// Rubrik Berechnung, siehe Protokoll");
    }

    /// <summary>
    /// <b>Gegenprobe zur Anzeige-Formel:</b> Das Muster trifft die Bauform der Fassung 2
    /// und NICHT jede eingerückte Zeile. Ohne diese Probe liefe der Fall oben womöglich
    /// über eine Zeile, die gar keine Formel ist, oder über eine Formel ohne Nummer —
    /// und die Nummer ist das, worauf der Fließtext sich beruft.
    /// </summary>
    [Fact]
    public void Der_Waechter_erkennt_eine_Anzeigeformel()
    {
        Assert.Matches(Anzeigeformel,
            ": <math>\\displaystyle Q_{\\mathrm{a}} = \\frac{\\sum_{t=1}^{8\\,760} Q(t)}{1\\,000}</math> &nbsp;&nbsp;(4)");
        Assert.Matches(Anzeigeformel,
            ": <big>Q<sub>a</sub> = ( Σ<sub>t=1…8 760</sub> Q(t) ) / 1 000</big>  (4)");
        Assert.Matches(Anzeigeformel, ": <big>ϑ = ϑ<sub>m</sub> + 1,5 K</big> (7)");

        Assert.DoesNotMatch(Anzeigeformel, ": <math>P = U \\cdot I</math>");   // ohne Nummer
        Assert.DoesNotMatch(Anzeigeformel, ": <big>P = U · I</big>");          // ohne Nummer
        Assert.DoesNotMatch(Anzeigeformel, ": eine eingerueckte Zeile  (1)");  // ohne Formelsatz
        Assert.DoesNotMatch(Anzeigeformel, "; Zeitraster");                    // Definitionsliste

        // Die Fassung 3 verengt: nur <math> zählt, und die Legende steht darunter.
        Assert.Matches(Anzeigegleichung,
            ": <math>\\displaystyle w = (5 - i)\\ \\operatorname{mod}\\ 7</math> &nbsp;&nbsp;(3)");
        Assert.DoesNotMatch(Anzeigegleichung, ": <big>P = U · I</big> &nbsp;&nbsp;(1)");

        Assert.Matches(Legendezeile, ":: <math>P_{\\mathrm{el}}</math> – elektrische Leistung [kW]");
        Assert.DoesNotMatch(Legendezeile, ": <math>P</math> &nbsp;&nbsp;(1)");   // einfach eingerückt
        Assert.DoesNotMatch(Legendezeile, ":: P_el – elektrische Leistung [kW]"); // ohne <math>
    }

    /// <summary>
    /// <b>Gegenprobe zur LaTeX-Teilmenge:</b> Der Wächter trifft den Befehl und nicht
    /// den deutschen Satz, und er unterscheidet den ERLAUBTEN vom fremden. Ohne diese
    /// Probe liefe der Fall oben über eine leere Menge, und ein <c>\dfrac</c> käme
    /// unbemerkt durch — im Wiki stünde dann eine rote Fehlerzeile.
    /// </summary>
    [Fact]
    public void Der_Waechter_erkennt_einen_fremden_LaTeX_Befehl()
    {
        Assert.Matches(LatexBefehl, @"\frac{a}{b}");
        Assert.Matches(LatexBefehl, @"\sum_{t=1}^{8760}");

        Assert.DoesNotMatch(LatexBefehl, "Q_a = ( Σ Q(t) ) / 1 000");
        Assert.DoesNotMatch(LatexBefehl, "Der Faktor 0,83 gilt fuer Wand und Waermebruecken.");

        Assert.True(ErlaubteBefehle.Contains("frac"));
        Assert.True(ErlaubteBefehle.Contains("displaystyle"));
        Assert.True(ErlaubteBefehle.Contains("vartheta"));

        Assert.False(ErlaubteBefehle.Contains("dfrac"));
        Assert.False(ErlaubteBefehle.Contains("tag"));
        Assert.False(ErlaubteBefehle.Contains("label"));
        Assert.False(ErlaubteBefehle.Contains("newcommand"));
    }

    /// <summary>
    /// <b>Gegenprobe zum Bestand:</b> Der Wächter liest wirklich Dateien und findet
    /// darin wirklich Schlüssel.
    /// </summary>
    [Fact]
    public void Der_Waechter_sieht_den_Bestand()
    {
        string[] dateien = Quelldateien();
        Assert.True(dateien.Length > 100, "Nur " + dateien.Length + " Quelldateien gefunden.");
        Assert.True(SchluesselImQuelltext().Count >= 1, "Kein einziger Berechnungsschlüssel gefunden.");
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Die H13-Zeilen aus <c>help_mapping.txt</c>: Schlüssel → Ziel. Eine spätere
    /// Zeile schlägt eine frühere — dieselbe Regel wie in <c>HelpExtender.ZielFuer</c>.
    /// </summary>
    private static IReadOnlyDictionary<string, string> Berechnungszuordnungen()
    {
        var tabelle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string pfad = Path.Combine(Wurzel(), "WindowsFormsApplication1", "Allgemein", "Hilfe",
                                   "help_mapping.txt");
        Assert.True(File.Exists(pfad), "help_mapping.txt nicht gefunden: " + pfad);

        foreach (string rohzeile in File.ReadAllLines(pfad, System.Text.Encoding.UTF8))
        {
            string zeile = rohzeile.Trim('﻿', ' ', '\t');
            if (zeile.Length == 0 || zeile.StartsWith("#", StringComparison.Ordinal)) continue;

            Match m = Zuordnungszeile.Match(zeile);
            if (!m.Success) continue;

            string schluessel = m.Groups[1].Value;
            if (!schluessel.EndsWith(".Berechnung", StringComparison.Ordinal)) continue;

            tabelle[schluessel] = m.Groups[2].Value;
        }

        return tabelle;
    }

    /// <summary>
    /// Jeder <c>*.Berechnung</c>-Schlüssel des Quelltexts mit den Dateien, in denen
    /// er steht.
    /// </summary>
    private static IReadOnlyDictionary<string, List<string>> SchluesselImQuelltext()
    {
        var gefunden = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (string pfad in Quelldateien())
        {
            string quelltext = File.ReadAllText(pfad);
            string name = Path.GetFileName(pfad);

            foreach (Match treffer in Schluesselmuster.Matches(quelltext))
            {
                if (!gefunden.TryGetValue(treffer.Value, out List<string>? dateien))
                {
                    dateien = new List<string>();
                    gefunden[treffer.Value] = dateien;
                }
                if (!dateien.Contains(name, StringComparer.Ordinal)) dateien.Add(name);
            }
        }

        return gefunden;
    }

    /// <summary>
    /// Wo ein Berechnungsschlüssel stehen darf: die Razor-Komponenten von
    /// <c>EPOS.UI</c> und die Hüllen unter <c>WindowsFormsApplication1/Views</c>.
    /// </summary>
    private static string[] Quelldateien()
    {
        string wurzel = Wurzel();

        IEnumerable<string> ui = Directory
            .EnumerateFiles(Path.Combine(wurzel, "EPOS.UI"), "*.razor", SearchOption.AllDirectories);

        IEnumerable<string> huellen = Directory
            .EnumerateFiles(Path.Combine(wurzel, "WindowsFormsApplication1", "Views"), "*.cs",
                            SearchOption.AllDirectories);

        return ui.Concat(huellen)
                 .Where(p => p.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
                                       StringComparison.Ordinal) < 0
                          && p.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
                                       StringComparison.Ordinal) < 0)
                 .OrderBy(p => p, StringComparer.Ordinal)
                 .ToArray();
    }

    /// <summary>Die Wurzel des Arbeitsbaums, vom Testausgabeordner aus gesucht.</summary>
    private static string Wurzel()
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !Directory.Exists(Path.Combine(ordner.FullName, "WindowsFormsApplication1", "Views")))
            ordner = ordner.Parent;

        Assert.True(ordner is not null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!.FullName;
    }
}

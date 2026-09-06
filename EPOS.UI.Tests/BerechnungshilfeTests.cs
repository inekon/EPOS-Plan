using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die WACHE über die Hilferubrik „Berechnung" — Paket <b>H13</b>, Anwenderwunsch
/// vom 06.09.2026 („die Details der Berechnung sollten in einer Separaten
/// Hilferubrik auf der wiki sein … Die Erläuterung sollte aber aufrufbar sein aus
/// den allgemeinen Erklärungen mit Bezügen").
///
/// <para><b>Was hier gehalten wird — vier Zusagen.</b></para>
/// <list type="number">
///   <item><description>Jede Seite der Rubrik trägt ihren Kopfblock und die sieben
///     Abschnitte der Bauform. Eine Seite ohne „Grenzen und Annahmen" wäre eine
///     Erklärung, die verschweigt, was der Rechenkern NICHT tut.</description></item>
///   <item><description>Jede Seite setzt ihre Formeln in der NOTATION der Rubrik:
///     Anzeige-Gleichungen mit laufender Nummer und die zwei Tabellen, die jedes
///     Zeichen benennen. Seit der <b>Fassung 3</b> (Anwenderwunsch vom
///     06.09.2026) steht jede Anzeige-Gleichung als LaTeX in
///     <c>&lt;math&gt;</c>, und unter ihr die LEGENDE — je Zeichen eine
///     Zeile.</description></item>
///   <item><description>Jeder Schlüssel <c>*.Berechnung</c> der Zuordnungsdatei
///     zeigt auf eine Seite, die es als Datei WIRKLICH gibt — sonst öffnet der
///     Knopf beim Anwender ins Leere.</description></item>
///   <item><description>Jeder dieser Schlüssel steht in GENAU EINEM Razor-Dialog.
///     Keiner ist tote Zuordnung, keiner hängt an zwei Masken.</description></item>
/// </list>
///
/// <para><b>Warum jetzt doch <c>&lt;math&gt;</c>.</b> Die Fassung 2 setzte ihre
/// Formeln in Unicode-Notation, weil <c>wiki.epos-plan.de</c> keine
/// Math-Erweiterung führte — ein <c>&lt;math&gt;</c>-Block wäre dort Klartext
/// gewesen. Der Anwender lässt die Erweiterung am 06.09.2026 installieren
/// („mathe erweiterung soll für die Formeln installiert werden auf wiki"), und
/// damit tragen die Seiten LaTeX. Die Teilmenge ist eng gefasst
/// (<see cref="ErlaubteBefehle"/>): Was WikiTexVC nicht kennt, erschiene beim
/// Anwender als roter Fehlerkasten — deshalb hält dieser Wächter die Liste.</para>
///
/// <para><b>Solange nur ein Teil umgestellt ist.</b> Die Fassung 3 entsteht in
/// zwei Aufträgen. Die sieben Seiten dieses Teils stehen in
/// <see cref="SeitenDiesesTeils"/> und werden nach den Fassung-3-Regeln geprüft;
/// die übrigen tragen bis zur Zusammenführung ihre Fassung 2 und dürfen davon
/// nicht rot werden.</para>
///
/// <para><b>Warum QUELLTEXT und nicht der Katalog.</b> Der Hilfekatalog
/// (<c>HelpCatalog</c>, <c>help_mapping.txt</c>) liegt in der Windows-Anwendung
/// (<c>net10.0-windows</c>); ein Test, der sie referenziert, liefe weder auf dem
/// ubuntu-Läufer noch auf macOS. Dieser Fall liest deshalb die Dateien — derselbe
/// Weg, den <c>StilblattTests</c> zum Stilblatt und <c>ParametersatzTests</c> zu
/// den Hüllen geht.</para>
///
/// <para>Keine Sprachbindung: geprüft werden Abschnittsüberschriften der Seiten
/// (sie sind Teil des Wiki-Markups, nicht der Oberfläche) und Bezeichner.</para>
/// </summary>
public sealed class BerechnungshilfeTests : BunitContext
{
    /// <summary>Der Ordner der Seiten, relativ zur Repowurzel.</summary>
    private static readonly string[] SeitenOrdner = { "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung" };

    /// <summary>Die Zuordnungsdatei, relativ zur Repowurzel.</summary>
    private static readonly string[] Zuordnungsdatei =
        { "WindowsFormsApplication1", "Allgemein", "Hilfe", "help_mapping.txt" };

    /// <summary>
    /// Die Abschnitte, die die Bauform von JEDER Seite verlangt — in dieser
    /// Reihenfolge und als Überschrift zweiter Ebene.
    ///
    /// <para>Seit der Fassung 2 (06.09.2026) sind es SIEBEN: „Formelzeichen und
    /// Parameter" steht zwischen den Eingangsgrößen und dem Rechenweg — wer die
    /// Formeln liest, hat die Zeichen unmittelbar davor gelesen.</para>
    /// </summary>
    private static readonly string[] Pflichtabschnitte =
    {
        "== Was berechnet wird ==",
        "== Eingangsgrößen ==",
        "== Formelzeichen und Parameter ==",
        "== Rechenweg ==",
        "== Grenzen und Annahmen ==",
        "== Ergebnisse und wo sie stehen ==",
        "== Bezüge =="
    };

    /// <summary>
    /// Was auf einer Seite der FASSUNG 2 nichts zu suchen hat: die Auszeichnung
    /// einer Math-Erweiterung, die es damals nicht gab, und die LaTeX-Befehle,
    /// die ohne sie als Backslash-Text erschienen.
    ///
    /// <para>Für die Seiten der Fassung 3 gilt das Verbot nicht mehr — dort tritt
    /// <see cref="ErlaubteBefehle"/> an seine Stelle.</para>
    /// </summary>
    private static readonly string[] VerboteneAuszeichnung =
    {
        "<math", "\\frac", "\\sum", "\\cdot", "\\eta", "\\begin", "\\text", "\\sqrt"
    };

    /// <summary>
    /// Die erlaubte LaTeX-Teilmenge der Fassung 3 — texvc/WikiTexVC-sicher. Was
    /// hier nicht steht, gehört nicht in eine Formel dieser Rubrik: Die
    /// Math-Erweiterung des Wikis zeichnet einen unbekannten Befehl als roten
    /// Fehlerkasten, und der Anwender liest statt der Gleichung eine Fehlermeldung.
    ///
    /// <para>Acht Befehle stehen über der Liste der Bauform, alle texvc-sicher:
    /// <c>\pi</c> (der Ersatzbehälter des Pufferspeichers ist ein Zylinder — eine
    /// Konstante, keine Schreibweise), <c>\kappa</c> (die Kappung seines
    /// vertikalen Ausgleichs; dasselbe Zeichen führt die Brauchwasserseite für den
    /// Kaltwasserfaktor), <c>\theta</c> (der Einfallswinkel der Sonnengeometrie),
    /// <c>\cos</c>, <c>\sin</c> (dieselbe Geometrie), <c>\ln</c> (das
    /// logarithmische Wechselrichtermodell der Photovoltaikseite) und
    /// <c>\circ</c> (das Gradzeichen eines Winkels, <c>85^{\circ}</c>) und
    /// <c>\chi</c> (die Zulässigkeit einer Ladequelle des Stromspeichers). Ohne
    /// sie ließe sich keine dieser vier Seiten schreiben.</para>
    ///
    /// <para><b>Zwei Befehle der Bauform fehlen hier mit Absicht:</b>
    /// <c>\lvert</c> und <c>\rvert</c>. Gemessen am 06.09.2026 gegen die frisch
    /// installierte Math-Erweiterung von <c>wiki.epos-plan.de</c>: „Unbekannte
    /// Funktion \lvert" — WikiTexVC kennt sie nicht. Der Betragsstrich ist
    /// <c>\left| … \right|</c> oder schlicht <c>|</c>.</para>
    /// </summary>
    private static readonly string[] ErlaubteBefehle =
    {
        "\\frac", "\\sqrt", "\\sum", "\\int", "\\prod", "\\min", "\\max",
        "\\cdot", "\\left", "\\right",
        "\\mathrm", "\\text", "\\operatorname", "\\displaystyle", "\\begin", "\\end",
        "\\eta", "\\vartheta", "\\rho", "\\lambda", "\\alpha", "\\beta", "\\gamma",
        "\\varepsilon", "\\tau", "\\varphi", "\\Delta", "\\Sigma", "\\pi", "\\kappa", "\\theta",
        "\\cos", "\\sin", "\\ln", "\\circ", "\\chi",
        "\\le", "\\ge", "\\ne", "\\approx", "\\pm", "\\to", "\\infty", "\\in", "\\dots",
        "\\quad", "\\,", "\\;", "\\ ", "\\\\"
    };

    /// <summary>
    /// Alle dreizehn Seiten der Rubrik (seit der Zusammenführung von Teil A und
    /// Teil B am 06.09.2026). Sie stehen hier AUSDRÜCKLICH und nicht als
    /// Verzeichnisinhalt: Eine gelöschte Datei soll rot ausfallen, nicht still
    /// durchgehen.
    /// </summary>
    private static readonly string[] SeitenDerRubrik =
    {
        "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme",
        "Strombedarf", "Wärmequelle Erdreich", "Heizkessel", "BHKW", "Wärmepumpe",
        "Pufferspeicher", "Solarthermie", "Photovoltaik", "Stromspeicher"
    };

    /// <summary>
    /// Die sieben Seiten, die dieser Auftrag auf die FASSUNG 3 umstellt (Erzeuger
    /// und Speicher). Nur für sie gelten LaTeX, Legende und das Verbot von
    /// <c>&lt;big&gt;</c>.
    ///
    /// <para>Die Liste ist eine Übergangsliste, keine Bauform.</para>
    /// </summary>
    // TODO Zusammenführung Fassung 3: auf SeitenDerRubrik umstellen
    private static readonly string[] SeitenDiesesTeils =
    {
        "Heizkessel", "BHKW", "Wärmepumpe", "Pufferspeicher", "Solarthermie",
        "Photovoltaik", "Stromspeicher"
    };

    /// <summary>Trägt die Seite schon die Fassung 3?</summary>
    private static bool Fassung3(string seite)
        => SeitenDiesesTeils.Contains(seite, StringComparer.Ordinal);

    public BerechnungshilfeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  1. Die Seiten
    // =====================================================================

    /// <summary>Jede Seite dieses Teils liegt als Datei im Kern.</summary>
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Jede_Seite_liegt_als_Datei_im_Kern(string seite)
    {
        Assert.True(File.Exists(Seitendatei(seite)),
                    "Die Seite " + seite + " fehlt in " + string.Join("/", SeitenOrdner) + ".");
    }

    /// <summary>
    /// Der Kopfblock steht in den ersten vier Zeilen und nennt Seite, Stand und
    /// die Fundstellen im Rechenkern. Er ist ein Wiki-KOMMENTAR und damit auf der
    /// Wikiseite unsichtbar — er gehört dem Entwickler, nicht dem Leser.
    /// </summary>
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Jede_Seite_traegt_ihren_Kopfblock(string seite)
    {
        string[] zeilen = File.ReadAllLines(Seitendatei(seite));
        Assert.True(zeilen.Length > 4, seite + " ist zu kurz für einen Kopfblock.");

        string kopf = string.Join("\n", zeilen.Take(4));

        Assert.StartsWith("<!--", zeilen[0].TrimStart(), StringComparison.Ordinal);
        Assert.Contains("EPOS-Plan Hilferubrik Berechnung", kopf, StringComparison.Ordinal);
        Assert.Contains("Seite: " + seite, kopf, StringComparison.Ordinal);
        Assert.Contains("Stand: 2026-", kopf, StringComparison.Ordinal);
        Assert.Contains("Rechenkern:", kopf, StringComparison.Ordinal);
        Assert.Contains("-->", string.Join("\n", zeilen.Take(6)), StringComparison.Ordinal);
    }

    /// <summary>
    /// Die sieben Abschnitte der Bauform stehen auf jeder Seite, und zwar in der
    /// vorgegebenen REIHENFOLGE. Zusätzliche Abschnitte sind erlaubt — die
    /// Photovoltaikseite führt einen eigenen über den Wechselrichter.
    /// </summary>
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Jede_Seite_traegt_die_sieben_Abschnitte_der_Bauform(string seite)
    {
        string text = File.ReadAllText(Seitendatei(seite)).Replace("\r\n", "\n");

        int vorher = -1;
        foreach (string abschnitt in Pflichtabschnitte)
        {
            int stelle = text.IndexOf("\n" + abschnitt, StringComparison.Ordinal);
            Assert.True(stelle >= 0, seite + " fehlt der Abschnitt " + abschnitt + ".");
            Assert.True(stelle > vorher,
                        seite + ": Der Abschnitt " + abschnitt + " steht an der falschen Stelle.");
            vorher = stelle;
        }
    }

    /// <summary>
    /// Der sichtbare Text nennt KEINE Quelltextpfade (Bauform, Punkt 2). Sie
    /// gehören in den Kopfkommentar — eine Wikiseite, die auf <c>.cs</c>-Dateien
    /// zeigt, altert mit dem nächsten Umbau und hilft dem Anwender nie.
    /// </summary>
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Der_sichtbare_Text_nennt_keine_Quelltextpfade(string seite)
    {
        string[] zeilen = File.ReadAllLines(Seitendatei(seite));

        // Der Kopfblock endet mit dem ersten "-->"; alles danach ist sichtbar.
        int ende = Array.FindIndex(zeilen, z => z.Contains("-->", StringComparison.Ordinal));
        Assert.True(ende >= 0, seite + " hat keinen abgeschlossenen Kopfblock.");

        string sichtbar = string.Join("\n", zeilen.Skip(ende + 1));

        Assert.DoesNotContain(".cs", sichtbar, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".razor", sichtbar, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EPOS.Kern/", sichtbar, StringComparison.Ordinal);
    }

    // =====================================================================
    //  1b. Die Notation: Fassung 2 (Unicode) und Fassung 3 (LaTeX)
    // =====================================================================

    /// <summary>
    /// Eine Seite, die noch NICHT auf die Fassung 3 umgestellt ist, trägt weiter
    /// die Unicode-Notation der Fassung 2: keine Math-Auszeichnung, kein
    /// LaTeX-Befehl.
    ///
    /// <para>Für die sieben Seiten dieses Teils gilt das Gegenteil — sie tragen
    /// LaTeX, geprüft von <see cref="Jede_Seite_dieses_Teils_setzt_LaTeX_in_math"/>
    /// und <see cref="Jede_Formel_benutzt_nur_die_erlaubten_Befehle"/>.</para>
    /// </summary>
    // TODO Zusammenführung Fassung 3: auf SeitenDerRubrik umstellen
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Eine_Seite_der_Fassung_2_benutzt_kein_LaTeX(string seite)
    {
        if (Fassung3(seite)) return;   // Fassung 3: LaTeX ist dort gewollt

        string text = File.ReadAllText(Seitendatei(seite));

        var gefunden = VerboteneAuszeichnung
            .Where(v => text.Contains(v, StringComparison.Ordinal))
            .ToArray();

        Assert.True(gefunden.Length == 0,
                    seite + " benutzt eine Auszeichnung, die zur Fassung 2 nicht passt: " +
                    string.Join(", ", gefunden) +
                    ". Entweder Unicode-Notation, oder die Seite wird ganz auf die " +
                    "Fassung 3 umgestellt und in SeitenDiesesTeils eingetragen.");
    }

    /// <summary>
    /// Jede Seite trägt mindestens eine ANZEIGE-Gleichung, und jede trägt ihre
    /// laufende Nummer am Zeilenende. Ohne Nummer lässt sich im Text nicht auf
    /// eine Gleichung verweisen — und genau darauf zeigt die Spalte
    /// „berechnet in" der Variablentabelle.
    ///
    /// <para>Die Nummern laufen lückenlos von 1 an: Eine gestrichene Gleichung,
    /// deren Nummer stehen bleibt, macht jeden Verweis darauf falsch.</para>
    ///
    /// <para>Die Zeile beginnt mit <c>: &lt;big&gt;</c> (Fassung 2) ODER mit
    /// <c>: &lt;math&gt;</c> (Fassung 3) — solange beide Fassungen nebeneinander
    /// liegen, sind beide Formen richtig.</para>
    /// </summary>
    // TODO Zusammenführung Fassung 3: nur noch ': <math>' zulassen
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Jede_Anzeigegleichung_traegt_ihre_laufende_Nummer(string seite)
    {
        string[] formeln = Anzeigegleichungen(seite);

        Assert.True(formeln.Length > 0,
                    seite + " trägt keine Anzeige-Gleichung (Zeile ': <math>…</math> (n)').");

        var nummern = new List<int>();
        foreach (string zeile in formeln)
        {
            var treffer = System.Text.RegularExpressions.Regex.Match(zeile, @"\((\d+)\)\s*$");
            Assert.True(treffer.Success,
                        seite + ": Diese Anzeige-Gleichung trägt keine Nummer:\n  " + zeile);
            nummern.Add(int.Parse(treffer.Groups[1].Value, CultureInfo.InvariantCulture));
        }

        Assert.Equal(Enumerable.Range(1, nummern.Count).ToArray(), nummern.ToArray());
    }

    /// <summary>
    /// Fassung 3: Jede Anzeige-Gleichung dieses Teils steht als LaTeX in
    /// <c>&lt;math&gt;</c>, beginnt mit <c>\displaystyle</c> (Summenlimits über
    /// und unter dem Zeichen, echte Brüche) und behält ihre Nummer am Zeilenende.
    /// <c>&lt;big&gt;</c> — die Formelzeile der Fassung 2 — kommt auf der Seite
    /// nicht mehr vor.
    /// </summary>
    [Theory]
    [MemberData(nameof(SeitennamenDiesesTeils))]
    public void Jede_Seite_dieses_Teils_setzt_LaTeX_in_math(string seite)
    {
        string[] zeilen = File.ReadAllLines(Seitendatei(seite));

        var muster = new System.Text.RegularExpressions.Regex(
            @"^:\s*<math>.+</math>(?:\s|&nbsp;)*\(\d+\)\s*$");

        string[] gleichungen = zeilen
            .Where(z => z.TrimStart().StartsWith(": <math>", StringComparison.Ordinal))
            .ToArray();

        Assert.True(gleichungen.Length > 0,
                    seite + " trägt keine Anzeige-Gleichung in <math>.");

        foreach (string zeile in gleichungen)
        {
            Assert.True(muster.IsMatch(zeile),
                        seite + ": Diese Anzeige-Gleichung hält die Bauform nicht " +
                        "(': <math>…</math> &nbsp;&nbsp;(n)'):\n  " + zeile);
            Assert.Contains("<math>\\displaystyle ", zeile, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("<big>", string.Join("\n", zeilen), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Kern des Anwenderwunsches vom 06.09.2026:</b> Unter jeder
    /// Anzeige-Gleichung steht die LEGENDE — mindestens eine Zeile
    /// <c>:: &lt;math&gt;…&lt;/math&gt; – Bedeutung [Einheit]</c>.
    ///
    /// <para>„Die Definitionen der Parameter/Variablen ist nicht erläutert
    /// (sollte unter der verwendeten Formel … beschrieben werden)" — eine
    /// Gleichung ohne Legende ist genau der Zustand, den der Anwender
    /// beanstandet hat.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(SeitennamenDiesesTeils))]
    public void Auf_jede_Anzeigegleichung_folgt_ihre_Legende(string seite)
    {
        string[] zeilen = File.ReadAllLines(Seitendatei(seite));
        var ohne = new List<string>();

        for (int i = 0; i < zeilen.Length; i++)
        {
            if (!zeilen[i].TrimStart().StartsWith(": <math>", StringComparison.Ordinal)) continue;

            int folge = i + 1;
            while (folge < zeilen.Length && zeilen[folge].Trim().Length == 0) folge++;

            bool legende = folge < zeilen.Length &&
                           zeilen[folge].TrimStart().StartsWith(":: <math>", StringComparison.Ordinal);

            if (!legende) ohne.Add(zeilen[i]);
        }

        Assert.True(ohne.Count == 0,
                    seite + ": Auf diese Anzeige-Gleichung(en) folgt keine Legendezeile " +
                    "':: <math>…</math> – Bedeutung [Einheit]':\n  " + string.Join("\n  ", ohne));
    }

    /// <summary>
    /// Jeder LaTeX-Befehl der Seite steht in der erlaubten Teilmenge
    /// (<see cref="ErlaubteBefehle"/>). Geprüft wird NUR innerhalb von
    /// <c>&lt;math&gt;</c>: Außerhalb ist ein Backslash gewöhnlicher Text.
    /// </summary>
    [Theory]
    [MemberData(nameof(SeitennamenDiesesTeils))]
    public void Jede_Formel_benutzt_nur_die_erlaubten_Befehle(string seite)
    {
        string text = File.ReadAllText(Seitendatei(seite));

        var formeln = System.Text.RegularExpressions.Regex.Matches(text, @"<math>(.*?)</math>",
                          System.Text.RegularExpressions.RegexOptions.Singleline);

        Assert.True(formeln.Count > 0, seite + " trägt keine einzige Formel in <math>.");

        var fremd = new List<string>();

        foreach (System.Text.RegularExpressions.Match formel in formeln)
        {
            string latex = formel.Groups[1].Value;

            foreach (System.Text.RegularExpressions.Match befehl in
                     System.Text.RegularExpressions.Regex.Matches(latex, @"\\[a-zA-Z]+|\\\\|\\[,; ]"))
            {
                if (!ErlaubteBefehle.Contains(befehl.Value, StringComparer.Ordinal))
                    fremd.Add(befehl.Value + "  in:  " + latex);
            }

            // \begin{…}/\end{…} nur für Fallunterscheidungen
            foreach (System.Text.RegularExpressions.Match umgebung in
                     System.Text.RegularExpressions.Regex.Matches(latex, @"\\(?:begin|end)\{([a-zA-Z*]+)\}"))
            {
                if (!string.Equals(umgebung.Groups[1].Value, "cases", StringComparison.Ordinal))
                    fremd.Add("Umgebung " + umgebung.Groups[1].Value + "  in:  " + latex);
            }
        }

        Assert.True(fremd.Count == 0,
                    seite + ": Diese Befehle stehen nicht in der erlaubten Teilmenge — " +
                    "die Math-Erweiterung des Wikis zeichnete sie als Fehlerkasten:\n  " +
                    string.Join("\n  ", fremd.Distinct(StringComparer.Ordinal)));
    }

    /// <summary>
    /// Fassung 3, Punkt 3 der Bauform: Auch die Symbolspalte der zwei Tabellen
    /// steht in <c>&lt;math&gt;</c> — dieselbe Schreibweise wie in den
    /// Gleichungen. Ein Symbol, das in der Tabelle anders aussieht als in der
    /// Formel, ist für den Leser ein zweites Symbol.
    ///
    /// <para>Eine Zeile, deren Symbolzelle nur den Gedankenstrich trägt, ist
    /// ausgenommen: Die Solarthermieseite führt so ihre Annahmen OHNE Formelzeichen
    /// („keine Stagnation, keine Solarkreispumpe") — dafür gibt es kein Symbol, das
    /// man setzen könnte.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(SeitennamenDiesesTeils))]
    public void Die_Symbolspalte_beider_Tabellen_steht_in_math(string seite)
    {
        string text = File.ReadAllText(Seitendatei(seite)).Replace("\r\n", "\n");

        int anfang = text.IndexOf("\n== Formelzeichen und Parameter ==", StringComparison.Ordinal);
        int ende = text.IndexOf("\n== Rechenweg ==", anfang, StringComparison.Ordinal);
        string abschnitt = text.Substring(anfang, ende - anfang);

        var ohne = abschnitt.Split('\n')
            .Where(z => z.StartsWith("| ", StringComparison.Ordinal) &&
                        z.Contains("||", StringComparison.Ordinal))
            .Select(z => new { Zeile = z, Symbol = z.Substring(2, z.IndexOf("||", StringComparison.Ordinal) - 2).Trim() })
            .Where(x => !x.Symbol.Contains("<math>", StringComparison.Ordinal) &&
                        !string.Equals(x.Symbol, "—", StringComparison.Ordinal))
            .Select(x => x.Zeile)
            .ToArray();

        Assert.True(ohne.Length == 0,
                    seite + ": In diesen Tabellenzeilen steht das Symbol nicht in <math>:\n  " +
                    string.Join("\n  ", ohne));
    }

    /// <summary>
    /// Der Abschnitt „Formelzeichen und Parameter" trägt BEIDE Tabellen mit
    /// ihren Spaltenköpfen: die Parameter mit ihrer <b>Herkunft</b> (Dialog und
    /// Feld, Katalog und Spalte, Vorgabe oder Konstante) und die Variablen mit
    /// der <b>Gleichung</b>, in der sie entstehen.
    ///
    /// <para>Geprüft werden die Spaltenköpfe, nicht der Inhalt: Eine Tabelle mit
    /// drei Spalten hätte die Herkunft verloren — und die Herkunft ist der Grund,
    /// warum der Anwender die Tabelle überhaupt liest.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Der_Abschnitt_Formelzeichen_traegt_beide_Tabellen(string seite)
    {
        string text = File.ReadAllText(Seitendatei(seite)).Replace("\r\n", "\n");

        int anfang = text.IndexOf("\n== Formelzeichen und Parameter ==", StringComparison.Ordinal);
        Assert.True(anfang >= 0, seite + " fehlt der Abschnitt „Formelzeichen und Parameter“.");

        int ende = text.IndexOf("\n== Rechenweg ==", anfang, StringComparison.Ordinal);
        Assert.True(ende > anfang, seite + ": Der Rechenweg folgt nicht auf die Formelzeichen.");

        string abschnitt = text.Substring(anfang, ende - anfang);

        Assert.Contains("=== Parameter ===", abschnitt, StringComparison.Ordinal);
        Assert.Contains("=== Variablen ===", abschnitt, StringComparison.Ordinal);
        Assert.Contains("! Symbol !! Bedeutung !! Einheit !! Herkunft", abschnitt, StringComparison.Ordinal);
        Assert.Contains("! Symbol !! Bedeutung !! Einheit !! berechnet in", abschnitt, StringComparison.Ordinal);

        // Zwei Tabellen, nicht eine — und beide geschlossen.
        Assert.Equal(2, Zaehle(abschnitt, "{| class=\"wikitable\""));
        Assert.Equal(2, Zaehle(abschnitt, "\n|}"));
    }

    /// <summary>
    /// Der Kopfblock nennt die Fassung. Wer eine Seite anfasst, sieht in Zeile 1,
    /// ob sie die Notation schon trägt — und welche.
    /// </summary>
    // TODO Zusammenführung Fassung 3: nur noch „Fassung 3" zulassen
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Jede_Seite_nennt_ihre_Fassung_im_Kopfblock(string seite)
    {
        string kopf = string.Join("\n", File.ReadAllLines(Seitendatei(seite)).Take(4));

        Assert.Contains(Fassung3(seite) ? "Fassung 3" : "Fassung 2", kopf, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Anzeige-Gleichungen einer Seite — die Fassung 2 setzt sie in
    /// <c>&lt;big&gt;</c>, die Fassung 3 in <c>&lt;math&gt;</c>.
    /// </summary>
    // TODO Zusammenführung Fassung 3: ': <big>' streichen
    private static string[] Anzeigegleichungen(string seite)
        => File.ReadAllLines(Seitendatei(seite))
               .Where(z => z.TrimStart().StartsWith(": <big>", StringComparison.Ordinal) ||
                           z.TrimStart().StartsWith(": <math>", StringComparison.Ordinal))
               .ToArray();

    private static int Zaehle(string text, string teil)
    {
        int zahl = 0, stelle = 0;
        while ((stelle = text.IndexOf(teil, stelle, StringComparison.Ordinal)) >= 0)
        {
            zahl++;
            stelle += teil.Length;
        }
        return zahl;
    }

    // =====================================================================
    //  2. Die Zuordnungen
    // =====================================================================

    /// <summary>
    /// Jeder Schlüssel der Abschnitte „Teil A" und „Teil B" zeigt auf eine Seite, die es als
    /// Datei gibt — und der Leser findet überhaupt Zeilen.
    /// </summary>
    [Fact]
    public void Jede_Zuordnung_zeigt_auf_eine_vorhandene_Seite()
    {
        var zuordnungen = ZuordnungenDerRubrik();

        Assert.True(zuordnungen.Count >= 10,
                    "Nur " + zuordnungen.Count + " Zuordnungen gefunden — der Leser ist kaputt " +
                    "oder die Abschnitte # Teil A / # Teil B der Rubrik fehlen.");

        var fehlend = zuordnungen
            .Where(z => !File.Exists(Seitendatei(z.Value)))
            .Select(z => z.Key + " → " + z.Value)
            .ToArray();

        Assert.True(fehlend.Length == 0,
                    "Diese Zuordnungen zeigen auf eine Seite ohne Datei:\n  " +
                    string.Join("\n  ", fehlend));
    }

    /// <summary>
    /// Jeder Schlüssel steht in GENAU EINEM Razor-Dialog. Zwei Fundstellen wären
    /// zwei Masken mit demselben Hilfeziel — dann sagt die Zuordnung nicht mehr,
    /// wo der Knopf sitzt; null Fundstellen wären eine tote Zeile.
    /// </summary>
    [Fact]
    public void Jeder_Schluessel_steht_in_genau_einem_Razor_Dialog()
    {
        var quellen = Razorquellen();
        Assert.True(quellen.Length >= 40,
                    "Nur " + quellen.Length + " Razor-Dateien gefunden — der Leser ist kaputt.");

        var funde = new List<string>();

        // Nur die Schlüssel des Teils B: Die Schlüssel des Teils A wählt zum Teil die
        // Hülle (BedarfsProfileHuelle.BerechnungsSchluessel je BedarfsArt, dazu die
        // Razor-Vorgabe) — dort sind zwei Fundstellen richtig. Für sie gilt der
        // Wächter BerechnungsknopfTests, der Razor UND Hüllen liest.
        foreach (var z in ZuordnungenDerRubrik(nurTeilB: true))
        {
            string muster = "\"" + z.Key + "\"";
            string[] treffer = quellen
                .Where(q => File.ReadAllText(q).Contains(muster, StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .ToArray()!;

            if (treffer.Length != 1)
                funde.Add(z.Key + ": " + treffer.Length + " Fundstellen (" +
                          (treffer.Length == 0 ? "keine" : string.Join(", ", treffer)) + ")");
        }

        Assert.True(funde.Count == 0,
                    "Diese Schlüssel stehen nicht in genau einem Razor-Dialog:\n  " +
                    string.Join("\n  ", funde));
    }

    // =====================================================================
    //  3. Der Knopf im Dialog
    // =====================================================================

    /// <summary>
    /// Die zehn Wirte dieses Teils: Komponentenname und der Schlüssel, den ihr
    /// Berechnungsknopf tragen muss.
    /// </summary>
    public static TheoryData<string, string> Wirte => new()
    {
        { "HeizkesselDialog",        "Form_Heizkessel.Berechnung" },
        { "BhkwDialog",              "Form_BHKWEing.Berechnung" },
        { "WaermepumpeAnlageDialog", "Form_WP.Berechnung" },
        { "BetriebsmodusDialog",     "Form_Betriebsmodus.Berechnung" },
        { "PufferspeicherDialog",    "Form_PufferSp.Berechnung" },
        { "PufferSpProjektDialog",   "Form_PufferSp_Projekt.Berechnung" },
        { "SolarkollektorenDialog",  "Form_SolarKollektoren.Berechnung" },
        { "SolarganglinieDialog",    "Form_Solarganglinie.Berechnung" },
        { "PhotovoltaikDialog",      "Form_PV.Berechnung" },
        { "StromspeicherDialog",     "Form_Stromspeicher.Berechnung" }
    };

    /// <summary>
    /// Der Berechnungsknopf ist im gezeichneten Dialog wirklich da — nicht nur im
    /// Quelltext. Gezeichnet wird auf dem Weg der Windows-Hülle (Wörterbuch →
    /// Parametersatz, Muster <c>StartkachelDialogeTests</c>), also mit dem
    /// kleinstmöglichen Satz.
    ///
    /// <para>Geprüft wird über die KOMPONENTE und nicht über das Markup: Der
    /// <c>InfoKnopf</c> zeichnet seinen Schlüssel nirgends hin — er trägt ihn.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(Wirte))]
    public void Jeder_Dialog_traegt_seinen_Berechnungsknopf(string komponente, string schluessel)
    {
        var gezeichnet = AusHuelle(Komponente(komponente), Gaben(komponente));

        string[] schluesselImDialog = gezeichnet.FindComponents<InfoKnopf>()
                                                .Select(k => k.Instance.Schluessel)
                                                .ToArray();

        Assert.Contains(schluessel, schluesselImDialog);
    }

    /// <summary>
    /// Der Fensterknopf bleibt daneben stehen (Bauform, Punkt 5): Der
    /// Berechnungsknopf ist ein ZWEITER Einstieg, kein Ersatz.
    /// </summary>
    [Theory]
    [MemberData(nameof(Wirte))]
    public void Der_Fensterknopf_bleibt_neben_dem_Berechnungsknopf(string komponente, string schluessel)
    {
        var gezeichnet = AusHuelle(Komponente(komponente), Gaben(komponente));

        string[] schluesselImDialog = gezeichnet.FindComponents<InfoKnopf>()
                                                .Select(k => k.Instance.Schluessel)
                                                .ToArray();

        Assert.True(schluesselImDialog.Length >= 2,
                    komponente + " trägt nur " + schluesselImDialog.Length + " Infoknopf/-knöpfe.");
        Assert.Contains(schluesselImDialog, s => s != schluessel && s.EndsWith("btn_Help", StringComparison.Ordinal));
    }

    // =====================================================================
    //  Gaben je Wirt
    // =====================================================================

    /// <summary>
    /// Der kleinstmögliche Parametersatz. Zwei Dialoge zeigen ihren
    /// Anlagenabschnitt nur bei GEWÄHLTER Projektzeile — sie bekommen eine; die
    /// übrigen acht zeichnen ohne jede Gabe.
    /// </summary>
    private static Dictionary<string, object> Gaben(string komponente)
    {
        var gaben = new Dictionary<string, object>(StringComparer.Ordinal);

        if (komponente is "PhotovoltaikDialog" or "SolarkollektorenDialog")
            gaben["Zeilen"] = new List<ErzeugerZeile>
            {
                new() { Schluessel = 1, Bezeichner = "Probe", GeraetId = 1 }
            };

        return gaben;
    }

    // =====================================================================
    //  Lesen
    // =====================================================================

    /// <summary>Alle dreizehn Seitennamen der Rubrik als Theoriedaten.</summary>
    public static TheoryData<string> Seitennamen
    {
        get
        {
            var daten = new TheoryData<string>();
            foreach (string s in SeitenDerRubrik) daten.Add(s);
            return daten;
        }
    }

    /// <summary>
    /// Die sieben Seiten der Fassung 3 als Theoriedaten — Übergangsliste, bis
    /// beide Teile zusammengeführt sind.
    /// </summary>
    // TODO Zusammenführung Fassung 3: auf SeitenDerRubrik umstellen
    public static TheoryData<string> SeitennamenDiesesTeils
    {
        get
        {
            var daten = new TheoryData<string>();
            foreach (string s in SeitenDiesesTeils) daten.Add(s);
            return daten;
        }
    }

    private static string Seitendatei(string seite)
        => Path.Combine(new[] { Wurzel() }.Concat(SeitenOrdner).ToArray()) +
           Path.DirectorySeparatorChar + seite + ".wiki";

    /// <summary>
    /// Die Zuordnungen der Abschnitte „# Teil A (Ablauf und Bedarf)" und
    /// „# Teil B (Erzeuger und Speicher)" bis zum nächsten fremden
    /// Abschnittskommentar bzw. Dateiende: Schlüssel → Seitenname (der Teil hinter
    /// „Berechnung/"). Seit der Zusammenführung beider Teile (06.09.2026) beide.
    /// </summary>
    private static Dictionary<string, string> ZuordnungenDerRubrik(bool nurTeilB = false)
    {
        var ergebnis = new Dictionary<string, string>(StringComparer.Ordinal);

        string datei = Path.Combine(new[] { Wurzel() }.Concat(Zuordnungsdatei).ToArray());
        if (!File.Exists(datei)) return ergebnis;

        bool imAbschnitt = false;

        foreach (string roh in File.ReadAllLines(datei))
        {
            string zeile = roh.Trim('﻿', ' ', '\t');

            if (zeile.StartsWith("#", StringComparison.Ordinal))
            {
                if ((zeile.Contains("Teil A", StringComparison.Ordinal) && !nurTeilB) ||
                    zeile.Contains("Teil B", StringComparison.Ordinal)) imAbschnitt = true;
                else if (imAbschnitt) break;   // der nächste Abschnitt beginnt
                continue;
            }

            if (!imAbschnitt || zeile.Length == 0) continue;

            int gleich = zeile.IndexOf('=');
            if (gleich <= 0) continue;

            string schluessel = zeile.Substring(0, gleich).Trim();
            string ziel = zeile.Substring(gleich + 1).Trim();

            const string praefix = "Berechnung/";
            Assert.StartsWith(praefix, ziel, StringComparison.Ordinal);

            // Ein Anker hinter '#' gehört nicht zum Dateinamen.
            int anker = ziel.IndexOf('#');
            if (anker >= 0) ziel = ziel.Substring(0, anker);

            ergebnis[schluessel] = ziel.Substring(praefix.Length).Trim();
        }

        return ergebnis;
    }

    private static string[] Razorquellen()
    {
        string ui = Path.Combine(Wurzel(), "EPOS.UI");
        if (!Directory.Exists(ui)) return Array.Empty<string>();

        return Directory.GetFiles(ui, "*.razor", SearchOption.AllDirectories)
                        .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" +
                                                Path.DirectorySeparatorChar, StringComparison.Ordinal)
                                 && !p.Contains(Path.DirectorySeparatorChar + "obj" +
                                                Path.DirectorySeparatorChar, StringComparison.Ordinal))
                        .OrderBy(p => p, StringComparer.Ordinal)
                        .ToArray();
    }

    /// <summary>Derselbe Aufstieg wie in <c>StilblattTests</c> und <c>ParametersatzTests</c>.</summary>
    private static string Wurzel()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null &&
               !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    // =====================================================================
    //  Zeichnen (Muster StartkachelDialogeTests)
    // =====================================================================

    private IRenderedComponent<DynamicComponent> AusHuelle(
        Type komponente, IDictionary<string, object> gaben)
    {
        return Render<DynamicComponent>(builder =>
        {
            builder.OpenComponent<DynamicComponent>(0);
            builder.AddComponentParameter(1, nameof(DynamicComponent.Type), komponente);
            builder.AddComponentParameter(2, nameof(DynamicComponent.Parameters),
                                          (IDictionary<string, object?>)gaben!);
            builder.CloseComponent();
        });
    }

    private static Type Komponente(string name)
    {
        Type? t = typeof(InfoKnopf).Assembly.GetTypes().FirstOrDefault(x => x.Name == name);
        Assert.True(t is not null, "Die Komponente " + name + " gibt es in EPOS.UI nicht.");
        return t!;
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }
}

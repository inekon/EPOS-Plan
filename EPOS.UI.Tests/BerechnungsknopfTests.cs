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
/// </summary>
public sealed class BerechnungsknopfTests
{
    /// <summary>Das Schlüsselmuster des Pakets: <c>Form_Irgendwas.Berechnung</c>.</summary>
    private static readonly Regex Schluesselmuster =
        new(@"\bForm_[A-Za-z0-9_]+\.Berechnung\b", RegexOptions.Compiled);

    /// <summary>Eine Zuordnungszeile <c>Schlüssel = Ziel</c>.</summary>
    private static readonly Regex Zuordnungszeile =
        new(@"^\s*([A-Za-z0-9_.]+)\s*=\s*(\S.*?)\s*$", RegexOptions.Compiled);

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

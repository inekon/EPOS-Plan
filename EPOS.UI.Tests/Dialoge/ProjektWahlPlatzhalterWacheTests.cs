using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die WACHE über die FORMAT-PARAMETER von <c>ProjektWahlDialog</c> — Befund
/// <b>#217</b> (11.09.2026, Anwenderbefund „Absturz Projekt löschen"):
/// <c>ProjektWahlHuelle.Gaben</c> reicht als <c>FrageMehrereFormat</c> den
/// echten Ressourcentext <c>PDLG_RUECKFRAGE</c> herein — ZWEI Platzhalter
/// (<c>{0}</c> Anzahl, <c>{1}</c> Namensliste) —, während die Razor-Vorgabe des
/// Parameters bis dahin nur <c>{0}</c> führte und der Code mit genau EINEM
/// Argument formatierte. Jede Windows-Installation warf beim Löschen mehrerer
/// Projekte eine <see cref="FormatException"/>; die bunit-Fälle blieben grün,
/// weil ihre Vorgabe denselben Fehler trug wie der Code (Befund #216-artig,
/// „eine bunit-Probe sieht das nicht" — hier war es sogar die Vorgabe SELBST,
/// die den Fehler versteckte).
///
/// <para><b>Was die Wache prüft.</b> Für jeden <c>[Parameter] public string
/// …Format</c> von <c>ProjektWahlDialog.razor</c>, dessen RAZOR-VORGABE
/// mindestens einen Platzhalter <c>{n}</c> trägt, muss der zugeordnete
/// Ressourcentext (die Zuordnung Parametername → Schlüssel steht in
/// <c>ProjektWahlHuelle.Gaben</c>, z. B. <c>FrageMehrereFormat</c> →
/// <c>PDLG_RUECKFRAGE</c>) — in BEIDEN Sprachen — dieselbe HÖCHSTE
/// Platzhalternummer tragen wie die Vorgabe. Ein Parameter ohne Platzhalter in
/// der Vorgabe (<c>FrageFormat</c>, <c>MehrdeutigFormat</c> — beide standardmäßig
/// leer, weil die Hülle sie NUR im Löschmodus setzt) bleibt außen vor; ihre
/// Platzhalterzahl ist im Auftrag #217 von Hand geprüft (Argumentzahl im Code =
/// Platzhalterzahl der Ressource, siehe Abschlussbericht).</para>
///
/// <para><b>Warum Quelltext statt Reflexion.</b> Die Ressourcen-Designer-Klasse
/// steht in <c>EPOS.Kern</c> (net10.0, ohne Windows), aber die ZUORDNUNG
/// Parameter → Schlüssel steht in der WinForms-Hülle
/// (<c>WindowsFormsApplication1</c>, net10.0-windows) — ein Test hier kann sie
/// nicht referenzieren. Der Leser geht deshalb denselben Weg wie
/// <c>ParametersatzTests</c> und <c>StilblattTests</c>: Quelltext lesen, mit
/// Regex zerlegen.</para>
/// </summary>
public sealed class ProjektWahlPlatzhalterWacheTests
{
    // =====================================================================
    //  Die Fälle
    // =====================================================================

    [Fact]
    public void Jeder_Format_Parameter_mit_Platzhaltern_traegt_dieselbe_hoechste_Nummer_wie_seine_Ressource()
    {
        Dictionary<string, string> vorgaben = FormatVorgaben();
        Dictionary<string, string> zuordnung = ResourceZuordnung();

        var funde = new List<string>();

        foreach ((string parameter, string vorgabe) in vorgaben)
        {
            int hoechsteVorgabe = HoechstePlatzhalterNummer(vorgabe);
            if (hoechsteVorgabe < 0) continue;   // Vorgabe traegt keinen Platzhalter - nicht Sache dieser Wache

            if (!zuordnung.TryGetValue(parameter, out string? schluessel))
            {
                funde.Add(parameter + ": trägt Platzhalter ({" + hoechsteVorgabe +
                           "} höchste Nummer), aber ProjektWahlHuelle.Gaben ordnet ihm keinen " +
                           "Ressourcenschlüssel zu.");
                continue;
            }

            foreach ((string kultur, string text) in ResourceText(schluessel))
            {
                int hoechsteRessource = HoechstePlatzhalterNummer(text);
                if (hoechsteRessource != hoechsteVorgabe)
                    funde.Add($"{parameter} → {schluessel} ({kultur}): Vorgabe höchste Nummer " +
                              $"{{{hoechsteVorgabe}}}, Ressource höchste Nummer " +
                              (hoechsteRessource < 0 ? "(keine)" : $"{{{hoechsteRessource}}}") +
                              $" — Platzhalterzahl ≠ Argumentzahl, string.Format stürzt ab.");
            }
        }

        Assert.True(funde.Count == 0,
            "Format-Parameter und Ressource laufen auseinander:\n  " + string.Join("\n  ", funde));
    }

    /// <summary>
    /// SELBSTPROBE. Ein Leser, der nichts mehr findet, ist immer grün. Am
    /// 11.09.2026 (Auftrag #217) trugen fünf der sieben <c>…Format</c>-Parameter
    /// eine Vorgabe mit Platzhaltern (<c>AusgewaehltFormat</c>,
    /// <c>FrageMehrereFormat</c>, <c>WeitereFormat</c>, <c>AnzahlFormat</c>,
    /// <c>VarianteVonFormat</c>) — und für jeden davon fand die Hülle eine
    /// Zuordnung. <c>FrageFormat</c> und <c>MehrdeutigFormat</c> bleiben außen
    /// vor, weil ihre Vorgabe leer ist.
    /// </summary>
    [Fact]
    public void Die_Wache_findet_die_Format_Parameter_und_ihre_Ressourcenzuordnung()
    {
        Dictionary<string, string> vorgaben = FormatVorgaben();
        Assert.True(vorgaben.Count >= 7,
            "Nur " + vorgaben.Count + " „…Format\"-Parameter gefunden — stimmt der Weg zum Quelltext noch?");

        Dictionary<string, string> zuordnung = ResourceZuordnung();
        Assert.True(zuordnung.Count >= 5,
            "Nur " + zuordnung.Count + " Zuordnungen Parameter → Ressourcenschlüssel gefunden.");

        int mitPlatzhalter = vorgaben.Count(kv => HoechstePlatzhalterNummer(kv.Value) >= 0);
        Assert.True(mitPlatzhalter >= 5,
            "Nur " + mitPlatzhalter + " Vorgaben mit Platzhaltern gefunden (erwartet mindestens 5).");

        // Jeder Parameter mit Platzhaltern hat auch eine Zuordnung - sonst greift
        // der Hauptfall oben nicht am Text, sondern nur an der Fehlermeldung.
        foreach (string parameter in vorgaben.Where(kv => HoechstePlatzhalterNummer(kv.Value) >= 0)
                                             .Select(kv => kv.Key))
            Assert.True(zuordnung.ContainsKey(parameter), parameter + ": keine Zuordnung gefunden.");
    }

    /// <summary>
    /// GEGENPROBE des Zahlenlesers. Beweist, dass der Leser <c>{0}</c> von
    /// <c>{1}</c> unterscheidet, mehrfach genannte Nummern nicht doppelt zählt
    /// und escapte Klammern (<c>{{</c>/<c>}}</c>, kein Platzhalter) nicht als
    /// Nummer 0 liest — ohne dieses Rezept meldete der Fall der PDLG_RUECKFRAGE
    /// niemals eine Abweichung, egal was in der Vorgabe steht.
    /// </summary>
    [Theory]
    [InlineData("keine Platzhalter hier", -1)]
    [InlineData("{0}", 0)]
    [InlineData("{0} von {1} Projekten", 1)]
    [InlineData("{1}-mal, alle {1} geloescht, name {0}", 1)]
    [InlineData("literale {{Klammer}} ohne Platzhalter", -1)]
    [InlineData("{0} Projekt(e)…:\n\n{1}\nFortfahren?", 1)]
    public void Der_Zahlenleser_findet_die_hoechste_Platzhalternummer(string text, int erwartet)
        => Assert.Equal(erwartet, HoechstePlatzhalterNummer(text));

    // =====================================================================
    //  Der Leser: Razor-Vorgaben
    // =====================================================================

    private static readonly Regex FormatParameterRegex = new(
        @"\[Parameter\]\s*public\s+string\s+(\w+Format)\s*\{\s*get;\s*set;\s*\}\s*=\s*" +
        @"""((?:[^""\\]|\\.)*)""\s*;",
        RegexOptions.Compiled);

    /// <summary>Name → Razor-Vorgabe (die Escapes <c>\n</c> etc. bleiben als Zeichenkette stehen,
    /// das stört den Zahlenleser nicht — er sucht nur nach <c>{n}</c>).</summary>
    private static Dictionary<string, string> FormatVorgaben()
    {
        string quelltext = File.ReadAllText(DialogDatei());
        var vorgaben = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in FormatParameterRegex.Matches(quelltext))
            vorgaben[m.Groups[1].Value] = m.Groups[2].Value;
        return vorgaben;
    }

    // =====================================================================
    //  Der Leser: Zuordnung Parameter → Ressourcenschlüssel (aus der Hülle)
    // =====================================================================

    private static readonly Regex ZuordnungRegex = new(
        @"\[""(\w+Format)""\]\s*=.*?Text_\(\s*""([A-Za-z0-9_]+)""",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static Dictionary<string, string> ResourceZuordnung()
    {
        string quelltext = File.ReadAllText(HuelleDatei());
        var zuordnung = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in ZuordnungRegex.Matches(quelltext))
            zuordnung[m.Groups[1].Value] = m.Groups[2].Value;
        return zuordnung;
    }

    // =====================================================================
    //  Der Leser: Ressourcentexte (beide Sprachen)
    // =====================================================================

    private static Regex DataRegex(string schluessel) => new(
        @"<data name=""" + Regex.Escape(schluessel) + @"""[^>]*>\s*<value[^>]*>(.*?)</value>",
        RegexOptions.Singleline);

    /// <summary>Der Ressourcentext eines Schlüssels je Kultur ("de", "en-US").</summary>
    private static IEnumerable<(string Kultur, string Text)> ResourceText(string schluessel)
    {
        (string Kultur, string Datei)[] dateien =
        {
            ("de", Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", "Resource.resx")),
            ("en-US", Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", "Resource.en-US.resx"))
        };

        foreach ((string kultur, string datei) in dateien)
        {
            if (!File.Exists(datei)) continue;
            Match treffer = DataRegex(schluessel).Match(File.ReadAllText(datei));
            if (treffer.Success)
                yield return (kultur, System.Net.WebUtility.HtmlDecode(treffer.Groups[1].Value));
        }
    }

    // =====================================================================
    //  Der Zahlenleser
    // =====================================================================

    private static readonly Regex PlatzhalterRegex = new(@"\{(\d+)(?::[^{}]*)?\}", RegexOptions.Compiled);

    /// <summary>
    /// Die höchste in <paramref name="text"/> vorkommende <c>{n}</c>-Nummer, oder
    /// <c>-1</c>, wenn keine steht. <c>{{</c>/<c>}}</c> (escapte Klammern) werden
    /// zuerst entfernt, damit sie nicht als Platzhalter <c>{0}</c> "at Position 0"
    /// missverstanden werden — sie sind keine Ziffern und würden ohnehin nicht
    /// matchen, die Entfernung ist die saubere Variante.
    /// </summary>
    private static int HoechstePlatzhalterNummer(string text)
    {
        string bereinigt = text.Replace("{{", "").Replace("}}", "");
        int hoechste = -1;
        foreach (Match m in PlatzhalterRegex.Matches(bereinigt))
        {
            int n = int.Parse(m.Groups[1].Value);
            if (n > hoechste) hoechste = n;
        }
        return hoechste;
    }

    // =====================================================================
    //  Der Weg zum Quelltext (derselbe Aufstieg wie in ParametersatzTests/StilblattTests)
    // =====================================================================

    private static string Wurzel()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null &&
               !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string DialogDatei()
        => Path.Combine(Wurzel(), "EPOS.UI", "Dialoge", "Projekt", "ProjektWahlDialog.razor");

    private static string HuelleDatei()
        => Path.Combine(Wurzel(), "WindowsFormsApplication1", "Views", "Projekt", "ProjektWahlHuelle.cs");
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die WACHE über die PARAMETERSÄTZE der Windows-Hüllen — Befund
/// <b>W16b‑B‑1</b> der Windows-Abnahme vom 05.09.2026, Vorgeschichte
/// <b>W16c‑B12</b>.
///
/// <para><b>Worum es geht.</b> Eine Hülle in <c>WindowsFormsApplication1</c>
/// reicht ihre Gaben als <c>Dictionary&lt;string, object&gt;</c> an
/// <c>BlazorDialogForm&lt;T&gt;</c> bzw. <c>BlazorSeite&lt;T&gt;</c>. Das
/// Wörterbuch kennt keine Typen: Ein Schlüssel, den <c>T</c> nicht als
/// <c>[Parameter]</c> führt, fällt beim Übersetzen NICHT auf. Blazor bemerkt
/// ihn erst beim ersten Zeichnen — beim Anwender als Absturz an
/// <c>Application.Run</c> (so geschehen bei W16c‑B12) oder als stille leere
/// Fläche.</para>
///
/// <para><b>Warum der Fall HIER steht und nicht in einem Windows-Test.</b>
/// Die Hüllen liegen in einem <c>net10.0-windows</c>-Projekt; ein Test, der es
/// referenziert, liefe weder auf dem ubuntu-Läufer noch auf macOS. Dieser Fall
/// liest deshalb den QUELLTEXT der Hüllen (derselbe Weg, den
/// <c>StilblattTests</c> zum Stilblatt geht) und löst die Gegenseite per
/// REFLEXION über <c>EPOS.UI</c> auf — die Komponenten stehen ja hier.</para>
///
/// <para><b>Die Gegenwache steht am Gerät:</b>
/// <c>WindowsFormsApplication1/Allgemein/Blazor/Parametersatzwache.cs</c>
/// prüft dieselbe Regel beim Bauen der Hülle und nennt den Schlüssel im
/// Klartext.</para>
///
/// <para>Keine Sprachbindung: geprüft werden ausschließlich Bezeichner.</para>
/// </summary>
public sealed class ParametersatzTests
{
    /// <summary>
    /// Kleinste Zahl von Fundstellen, unter der der Leser als kaputt gilt.
    /// Am 05.09.2026 waren es 61; die Schranke lässt Luft nach unten, ohne
    /// einen stillen Totalausfall des Lesers durchgehen zu lassen.
    /// </summary>
    private const int MINDESTSTELLEN = 45;

    // =====================================================================
    //  Die Fälle
    // =====================================================================

    /// <summary>
    /// Jede Stelle <c>new BlazorDialogForm&lt;T&gt;(…)</c> /
    /// <c>new BlazorSeite&lt;T&gt;(…)</c> bekommt nur Schlüssel, die
    /// <c>T</c> als <c>[Parameter]</c> führt.
    /// </summary>
    [Fact]
    public void Jeder_Parametersatz_einer_Huelle_trifft_die_Parameter_seiner_Komponente()
    {
        var funde = new List<string>();

        foreach (Fundstelle stelle in Fundstellen())
        {
            Type? komponente = Komponente(stelle.Komponente);
            if (komponente is null)
            {
                funde.Add(stelle.Ort + ": Die Komponente " + stelle.Komponente +
                          " gibt es in EPOS.UI nicht.");
                continue;
            }

            string[] fremd = Fremdschluessel(komponente, stelle.Schluessel);
            if (fremd.Length > 0)
                funde.Add(stelle.Ort + " → " + stelle.Komponente + ": " +
                          string.Join(", ", fremd));
        }

        Assert.True(funde.Count == 0,
            "Diese Schlüssel treffen keinen [Parameter] ihrer Komponente:\n  " +
            string.Join("\n  ", funde));
    }

    /// <summary>
    /// Die DREIZEHN Assistentenseiten. Ihre Parametersätze gehen nicht durch
    /// eine Hülle, sondern über <c>AddMultipleAttributes</c> in
    /// <c>AssistentSeite.Seiteninhalt</c> — dieselbe Falle, nur ohne
    /// Konstruktor davor.
    /// </summary>
    [Fact]
    public void Jede_Assistentenseite_bekommt_nur_Parameter_ihrer_Komponente()
    {
        var funde = new List<string>();
        int geprueft = 0;

        foreach ((int nr, string huelle, string methode) in Assistentenseiten())
        {
            Type? komponente = Seitentyp(nr);
            Assert.NotNull(komponente);

            List<HashSet<string>> saetze = SchluesselAus(huelle, methode);
            if (saetze.All(s => s.Count == 0))
                continue;   // die Gaben stehen eine Ebene tiefer — Fall 1 fasst sie

            geprueft++;

            // Mehrere Rueckgaben heissen: UEBERLADUNGEN derselben Gaben-Methode
            // (HeizkesselHuelle.Gaben gibt es fuer den Dialog UND fuer den
            // Katalog). Welche der Uebersetzer waehlt, sieht ein Textleser nicht -
            // es gilt die, die passt.
            string[] fremd = saetze.Select(s => Fremdschluessel(komponente!, s))
                                   .OrderBy(f => f.Length).First();
            if (fremd.Length > 0)
                funde.Add("Assistentenseite " + nr + " (" + huelle + "." + methode + ") → " +
                          komponente!.Name + ": " + string.Join(", ", fremd));
        }

        Assert.True(geprueft >= 8,
            "Nur " + geprueft + " Assistentenseiten geprüft — der Leser findet ihre Gaben nicht mehr.");
        Assert.True(funde.Count == 0,
            "Diese Schlüssel treffen keinen [Parameter] ihrer Komponente:\n  " +
            string.Join("\n  ", funde));
    }

    /// <summary>
    /// SELBSTPROBE. Ein Leser, der nichts mehr findet, ist immer grün — und
    /// deshalb wertlos. Der Fall hält die Zahl der Fundstellen gegen eine
    /// Untergrenze.
    /// </summary>
    [Fact]
    public void Die_Wache_findet_die_Huellen_und_ihre_Stellen()
    {
        Assert.True(Huellen().Length >= 40,
            "Nur " + Huellen().Length + " *Huelle.cs gefunden — stimmt der Weg zum Quelltext noch?");

        List<Fundstelle> stellen = Fundstellen();
        Assert.True(stellen.Count >= MINDESTSTELLEN,
            "Nur " + stellen.Count + " Stellen mit BlazorDialogForm<T>/BlazorSeite<T> gefunden " +
            "(erwartet mindestens " + MINDESTSTELLEN + ").");

        // Jede Fundstelle traegt Schluessel - sonst liest der Zerleger die
        // Gaben-Methoden nicht mehr mit.
        Assert.True(stellen.Count(s => s.Schluessel.Count > 0) >= MINDESTSTELLEN / 2,
            "Zu wenige Fundstellen tragen ueberhaupt Schluessel.");
    }

    // =====================================================================
    //  Der Abgleich
    // =====================================================================

    /// <summary>
    /// Die Schlüssel, die kein <c>[Parameter]</c> von
    /// <paramref name="komponente"/> treffen. Eine Komponente mit
    /// <c>CaptureUnmatchedValues</c> nimmt jeden Namen — für sie ist die Liste
    /// immer leer.
    /// </summary>
    private static string[] Fremdschluessel(Type komponente, IEnumerable<string> schluessel)
    {
        PropertyInfo[] eigenschaften = komponente.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo p in eigenschaften)
        {
            var merkmal = p.GetCustomAttribute<ParameterAttribute>();
            if (merkmal is not null && merkmal.CaptureUnmatchedValues) return Array.Empty<string>();
        }

        var bekannt = new HashSet<string>(
            eigenschaften.Where(p => p.CanWrite && p.IsDefined(typeof(ParameterAttribute), true))
                         .Select(p => p.Name),
            StringComparer.Ordinal);

        return schluessel.Where(k => !bekannt.Contains(k))
                         .OrderBy(k => k, StringComparer.Ordinal)
                         .ToArray();
    }

    private static Type? Komponente(string name)
        => Uibibliothek.GetTypes().FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal));

    private static Assembly Uibibliothek => typeof(EPOS.UI.Bausteine.Kachel).Assembly;

    /// <summary>Der Seitentyp einer Assistentenseite — aus der Komponente selbst.</summary>
    private static Type? Seitentyp(int nr)
    {
        Type? seite = Komponente("AssistentSeite");
        MethodInfo? m = seite?.GetMethod("Seitentyp", BindingFlags.Public | BindingFlags.Static);
        return m?.Invoke(null, new object[] { nr }) as Type;
    }

    // =====================================================================
    //  Der Leser
    // =====================================================================

    /// <summary>Eine Stelle, an der ein Parametersatz auf eine Komponente trifft.</summary>
    private sealed record Fundstelle(string Ort, string Komponente, HashSet<string> Schluessel);

    private static readonly Regex TypRegex =
        new(@"Blazor(?:DialogForm|Seite|AssistentSeite)<\s*([A-Za-z0-9_.]+)\s*>", RegexOptions.Compiled);

    private static readonly Regex SchluesselRegex =
        new(@"\[""([A-Za-z_][A-Za-z0-9_]*)""\]\s*=", RegexOptions.Compiled);

    /// <summary>
    /// Ein Methodenaufruf <c>[Wirt.]Name(</c>. Gefiltert wird danach ueber den
    /// NAMEN: alles, was „Gaben" enthaelt — auch die Methode, die schlicht so
    /// heisst (deshalb kein Praefix im Muster).
    /// </summary>
    private static readonly Regex GabenRufRegex =
        new(@"(?:([A-Za-z_][A-Za-z0-9_]*)\.)?([A-Za-z_][A-Za-z0-9_]*)\s*\(",
            RegexOptions.Compiled);

    private static bool IstGabenRuf(Match ruf)
        => ruf.Groups[2].Value.Contains("Gaben", StringComparison.Ordinal);

    private static readonly Regex SignaturRegex = new(
        @"^\s{8}(?:(?:internal|private|public|protected|static|sealed|override|async)\s+)+" +
        @"[A-Za-z_][A-Za-z0-9_.<>,\[\]\s?]*?\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Alle Stellen aus allen Hüllen. Die Schlüssel einer Stelle sind die des
    /// umgebenden Methodenrumpfs PLUS die jeder <c>*Gaben*</c>-Methode, die er
    /// ruft. Gibt es davon mehrere Überladungen, gilt die Stelle als in
    /// Ordnung, sobald EINE passt — welche der Übersetzer wählt, sieht ein
    /// Textleser nicht.
    /// </summary>
    private static List<Fundstelle> Fundstellen()
    {
        Dictionary<(string, string), List<string>> alle = AlleMethoden();
        var stellen = new List<Fundstelle>();

        foreach (string pfad in Huellen())
        {
            string klasse = Path.GetFileNameWithoutExtension(pfad);
            foreach ((string name, string rumpf) in Methoden(File.ReadAllText(pfad)))
            {
                string[] typen = TypRegex.Matches(rumpf)
                                         .Select(m => m.Groups[1].Value.Split('.').Last())
                                         .Distinct(StringComparer.Ordinal)
                                         .ToArray();
                if (typen.Length != 1) continue;

                foreach (HashSet<string> satz in Saetze(alle, klasse, rumpf))
                    stellen.Add(new Fundstelle(klasse + "." + name, typen[0], satz));
            }
        }

        // Mehrere Saetze je Stelle heissen: Ueberladungen. Es gilt der beste.
        return stellen
            .GroupBy(s => s.Ort + "|" + s.Komponente, StringComparer.Ordinal)
            .Select(g => g.OrderBy(s => Fremdzahl(s)).First())
            .ToList();
    }

    private static int Fremdzahl(Fundstelle s)
    {
        Type? t = Komponente(s.Komponente);
        return t is null ? int.MaxValue : Fremdschluessel(t, s.Schluessel).Length;
    }

    /// <summary>
    /// Die möglichen Schlüsselsätze eines Rumpfs: seine eigenen Schlüssel,
    /// erweitert um je eine Überladung jeder gerufenen <c>*Gaben*</c>-Methode.
    /// </summary>
    private static List<HashSet<string>> Saetze(
        Dictionary<(string, string), List<string>> alle, string klasse, string rumpf)
    {
        var saetze = new List<HashSet<string>>
        {
            new(SchluesselRegex.Matches(rumpf).Select(m => m.Groups[1].Value), StringComparer.Ordinal)
        };

        foreach (Match ruf in GabenRufRegex.Matches(rumpf))
        {
            if (!IstGabenRuf(ruf)) continue;

            string wirt = ruf.Groups[1].Success && ruf.Groups[1].Value.Length > 0
                ? ruf.Groups[1].Value : klasse;
            string name = ruf.Groups[2].Value;

            if (!alle.TryGetValue((wirt, name), out List<string>? rumpfe) &&
                !alle.TryGetValue((klasse, name), out rumpfe))
                continue;

            var erweitert = new List<HashSet<string>>();
            foreach (HashSet<string> basis in saetze)
                foreach (string weiterer in rumpfe!)
                {
                    var neu = new HashSet<string>(basis, StringComparer.Ordinal);
                    foreach (Match m in SchluesselRegex.Matches(weiterer)) neu.Add(m.Groups[1].Value);
                    erweitert.Add(neu);
                }
            if (erweitert.Count > 0) saetze = erweitert;
        }

        return saetze;
    }

    /// <summary>Alle Methodenrümpfe aller Hüllen, nach Klasse und Name.</summary>
    private static Dictionary<(string, string), List<string>> AlleMethoden()
    {
        var alle = new Dictionary<(string, string), List<string>>();
        foreach (string pfad in Huellen())
        {
            string klasse = Path.GetFileNameWithoutExtension(pfad);
            foreach ((string name, string rumpf) in Methoden(File.ReadAllText(pfad)))
            {
                if (!alle.TryGetValue((klasse, name), out List<string>? liste))
                    alle[(klasse, name)] = liste = new List<string>();
                liste.Add(rumpf);
            }
        }
        return alle;
    }

    /// <summary>
    /// Die Schlüssel EINER benannten Methode einer benannten Hülle — je
    /// Überladung ein Satz.
    /// </summary>
    private static List<HashSet<string>> SchluesselAus(string klasse, string methode)
    {
        var saetze = new List<HashSet<string>>();
        if (AlleMethoden().TryGetValue((klasse, methode), out List<string>? rumpfe))
            foreach (string rumpf in rumpfe)
                saetze.Add(new HashSet<string>(
                    SchluesselRegex.Matches(rumpf).Select(m => m.Groups[1].Value),
                    StringComparer.Ordinal));

        if (saetze.Count == 0) saetze.Add(new HashSet<string>(StringComparer.Ordinal));
        return saetze;
    }

    /// <summary>
    /// Zerlegt eine Quelldatei grob in Methoden: Signatur suchen, ab der
    /// öffnenden Klammer die Bilanz zählen. Kein Übersetzer, aber genug für
    /// Wörterbuchliterale.
    /// </summary>
    private static IEnumerable<(string Name, string Rumpf)> Methoden(string quelltext)
    {
        foreach (Match m in SignaturRegex.Matches(quelltext))
        {
            int a = quelltext.IndexOf('{', m.Index + m.Length);
            if (a < 0) continue;

            int tiefe = 0, i = a;
            for (; i < quelltext.Length; i++)
            {
                if (quelltext[i] == '{') tiefe++;
                else if (quelltext[i] == '}' && --tiefe == 0) break;
            }
            if (i >= quelltext.Length) continue;

            yield return (m.Groups[1].Value, quelltext.Substring(a, i - a + 1));
        }
    }

    /// <summary>
    /// Die dreizehn Fälle aus <c>AssistentHuelle.Seitengaben</c> als
    /// (Nummer, Hülle, Gaben-Methode) — die Nummern kommen aus
    /// <c>WizardItemClass</c>.
    /// </summary>
    private static IEnumerable<(int Nr, string Huelle, string Methode)> Assistentenseiten()
    {
        string huelle = Path.Combine(Wurzel(), "WindowsFormsApplication1", "Views", "Wizard",
                                     "AssistentHuelle.cs");
        if (!File.Exists(huelle)) yield break;

        Dictionary<string, int> nummern = Itemnummern();
        string quelltext = File.ReadAllText(huelle);
        string rumpf = Methoden(quelltext).Where(m => m.Name == "Seitengaben")
                                          .Select(m => m.Rumpf).FirstOrDefault() ?? "";

        foreach (Match fall in Regex.Matches(
                     rumpf, @"case WizardItemClass\.([A-Z_]+):(.*?)(?=case WizardItemClass\.|default:)",
                     RegexOptions.Singleline))
        {
            if (!nummern.TryGetValue(fall.Groups[1].Value, out int nr)) continue;

            foreach (Match ruf in GabenRufRegex.Matches(fall.Groups[2].Value))
            {
                if (!IstGabenRuf(ruf)) continue;
                if (!ruf.Groups[1].Success || ruf.Groups[1].Value.Length == 0) continue;
                yield return (nr, ruf.Groups[1].Value, ruf.Groups[2].Value);
            }
        }
    }

    private static Dictionary<string, int> Itemnummern()
    {
        var nummern = new Dictionary<string, int>(StringComparer.Ordinal);
        string datei = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "WizardItemClass.cs");
        if (!File.Exists(datei)) return nummern;

        foreach (Match m in Regex.Matches(File.ReadAllText(datei),
                                          @"const\s+int\s+([A-Z_]+)\s*=\s*(\d+)"))
            nummern[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        return nummern;
    }

    // =====================================================================
    //  Der Weg zum Quelltext (derselbe Aufstieg wie in StilblattTests)
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

    private static string[] Huellen()
    {
        string views = Path.Combine(Wurzel(), "WindowsFormsApplication1", "Views");
        if (!Directory.Exists(views)) return Array.Empty<string>();

        return Directory.GetFiles(views, "*Huelle.cs", SearchOption.AllDirectories)
                        .OrderBy(p => p, StringComparer.Ordinal)
                        .ToArray();
    }
}

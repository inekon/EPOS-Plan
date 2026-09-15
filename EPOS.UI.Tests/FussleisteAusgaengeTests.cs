using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache zur Hausregel <b>„Geschrieben wird im OK-Weg"</b>
/// (<c>EPOS.UI/CLAUDE.md</c>, Abschnitt „Jeder Dialog trägt OK und Abbrechen").
///
/// <para><b>Der Befund, den sie verhindert.</b> Eine <c>SpeichernLeiste</c> mit
/// <c>MitSpeichern="true"</c> trägt einen Knopf, der den Dialog NICHT schließt.
/// Steht daneben kein Abbrechen (<c>MitAbbrechen="false"</c>), dann hat der
/// Anwender genau zwei Wege aus der Maske — und beide lassen stehen, was der
/// mittlere Knopf getan hat. Schreibt dieser Knopf in die Datenbank, gibt es kein
/// Zurück mehr; ein später nachgerüstetes „Abbrechen" wäre dann eine Behauptung,
/// die nicht stimmt. Diese Paarung ist deshalb verboten: Entweder der mittlere
/// Knopf übernimmt nur in einen Arbeitsstand — dann gehört Abbrechen dazu, das
/// ihn verwirft —, oder es gibt ihn nicht.</para>
///
/// <para><b>Warum sie nicht schlicht jedes <c>MitAbbrechen="false"</c> meldet.</b>
/// Eine Maske, die gar nichts aufnimmt — ein Bericht, eine Ergebnisansicht —,
/// trägt zu Recht nur „Schließen": Dort gibt es nichts zu verwerfen. Erst der
/// nicht schließende Knopf macht aus dem fehlenden Abbrechen einen Fehler.</para>
///
/// <para><b>Ausnahmeliste: leer.</b> Jede neue Fundstelle ist ein Fehler, keine
/// Ausnahme.</para>
/// </summary>
public sealed class FussleisteAusgaengeTests
{
    /// <summary>Eine Fundstelle: Datei, Zeile und der gefundene Leistenkopf.</summary>
    private readonly record struct Leistenfund(string Datei, int Zeile, string Tag);

    [Fact]
    public void Keine_Leiste_traegt_einen_nicht_schliessenden_Knopf_ohne_Abbrechen()
    {
        List<Leistenfund> funde = Funde(RazorDateien());

        Assert.True(funde.Count == 0,
            "SpeichernLeiste mit MitSpeichern=\"true\" und MitAbbrechen=\"false\" "
            + "(Hausregel „Geschrieben wird im OK-Weg\"):\n" + Bericht(funde));
    }

    /// <summary>
    /// Die Wache muss auch etwas finden können (Lehre W6‑B‑1: eine grüne Wache, die
    /// nie rot werden kann, prüft nichts). Der eingefrorene Bestand des Dialogs
    /// „BHKW-Wirtschaftlichkeit" VOR Auftrag #286 — nicht schließender
    /// „Speichern"-Knopf, kein Abbrechen — muss als Fund erscheinen, die heutige
    /// Fassung nicht mehr.
    /// </summary>
    [Fact]
    public void Die_Wache_findet_den_eingefrorenen_Befund_vor_286()
    {
        const string vorher =
            "    <SpeichernLeiste @ref=\"_leiste\" MitSpeichern=\"true\" MitAbbrechen=\"false\"\n"
            + "                     SatzMarkiert=\"true\" Geaendert=\"_geaendert\" OkText=\"@_t.Schliessen\"\n"
            + "                     Gespeichertwerden=\"Speichern_Klick\" Ergebnis=\"BeiErgebnis\" />\n";

        Leistenfund fund = Assert.Single(
            Funde(new Dictionary<string, string> { ["BhkwWirtschaftlichkeitDialog.razor"] = vorher }));
        Assert.Equal("BhkwWirtschaftlichkeitDialog.razor", fund.Datei);

        const string heute =
            "    <SpeichernLeiste @ref=\"_leiste\" MitSpeichern=\"false\" MitAbbrechen=\"true\"\n"
            + "                     OkText=\"@_t.Speichern\" AbbrechenText=\"@_t.Abbrechen\"\n"
            + "                     Ergebnis=\"BeiErgebnis\" />\n";
        Assert.Empty(Funde(new Dictionary<string, string> { ["BhkwWirtschaftlichkeitDialog.razor"] = heute }));

        // Und die zulaessigen Bauarten bleiben stumm: die Liste mit beiden Ausgaengen
        // (Pufferverwaltung) und die reine Ansicht mit nur einem.
        const string liste =
            "    <SpeichernLeiste OkText=\"@OkText\" AbbrechenText=\"@AbbrechenText\"\n"
            + "                     MitAbbrechen=\"true\"\n"
            + "                     MitSpeichern=\"true\" SpeichernText=\"@Uebernehmentext\"\n"
            + "                     Ergebnis=\"BeiErgebnis\" />\n";
        const string ansicht =
            "    <SpeichernLeiste OkText=\"@OkText\" MitAbbrechen=\"false\" Ergebnis=\"BeiErgebnis\" />\n";
        Assert.Empty(Funde(new Dictionary<string, string>
        {
            ["PufferSpProjektDialog.razor"] = liste,
            ["BedarfErgebnisDialog.razor"] = ansicht
        }));
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Wache_liest_tatsaechlich_razor_dateien()
    {
        Dictionary<string, string> dateien = RazorDateien();

        Assert.NotEmpty(dateien);
        Assert.Contains(dateien.Values, s => s.Contains("<SpeichernLeiste", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------
    //  Der Leser
    // ---------------------------------------------------------------------

    private static List<Leistenfund> Funde(IReadOnlyDictionary<string, string> dateien)
    {
        var funde = new List<Leistenfund>();

        foreach (KeyValuePair<string, string> datei in dateien)
        {
            string s = datei.Value;

            foreach (Match m in Regex.Matches(s, @"<SpeichernLeiste\b"))
            {
                int ende = TagEnde(s, m.Index);
                if (ende < 0) continue;
                string tag = s.Substring(m.Index, ende - m.Index + 1);

                if (!Regex.IsMatch(tag, "MitSpeichern=\"true\"")) continue;
                if (!Regex.IsMatch(tag, "MitAbbrechen=\"false\"")) continue;

                int zeile = s.Take(m.Index).Count(c => c == '\n') + 1;
                funde.Add(new Leistenfund(datei.Key, zeile, tag));
            }
        }

        return funde;
    }

    /// <summary>Das Ende des Oeffnungstags ab <paramref name="start"/>; -1 = keines.</summary>
    private static int TagEnde(string text, int start)
    {
        bool imText = false;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"') imText = !imText;
            else if (c == '>' && !imText) return i;
        }
        return -1;
    }

    private static string Bericht(IEnumerable<Leistenfund> funde)
        => string.Join("\n", funde.Select(f => "  " + f.Datei + ":" + f.Zeile));

    // ---------------------------------------------------------------------
    //  Der Weg zu den Quelldateien (dasselbe Verfahren wie UeberlagerungstitelTests)
    // ---------------------------------------------------------------------

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    private static Dictionary<string, string> RazorDateien()
    {
        string wurzel = Path.Combine(Wurzel(), "EPOS.UI");
        var ergebnis = new Dictionary<string, string>();
        foreach (string p in Directory.GetFiles(wurzel, "*.razor", SearchOption.AllDirectories))
            ergebnis[Path.GetFileName(p)] = File.ReadAllText(p);
        return ergebnis;
    }
}

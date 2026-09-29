using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Tests.Bausteine;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache zur Hausregel <b>„Ein KI-Knopf je Dialog"</b> (Anwenderbefund vom
/// 29.09.2026 mit Bildschirmfoto: „der KI-Assistent-Button ist doppelt vorhanden" — im Blatt
/// „Brauchwasser-Zapfprofil" standen die Pillen „Bedienung" und „Rechenweg" nebeneinander,
/// jede mit ihrem KI-Feld).
///
/// <para><b>Der Befund:</b> Der KI-Knopf steckt im <c>InfoKnopf</c> (Vorgabe
/// <c>MitAssistent = true</c>). 23 Dialoge und Seiten trugen mehr als eine Pille — Bedienung und
/// Rechenweg im Kopf, Berechnungshilfe, Import- und Formathinweis, Gruppen- und Feldhilfen —, und
/// jede zeichnete ihr KI-Feld. Den Bereich des Assistenten leitet der Kern aus dem Maskenpräfix
/// des Schlüssels ab (<c>KiChatKontext.BereichFuerHilfeschluessel</c>): Jede weitere Pille
/// öffnete ihn mit demselben Kontext noch einmal.</para>
///
/// <para><b>Die Regel:</b> Je Datei und je Überlagerung (<c>&lt;Ueberlagerung&gt;</c> und
/// <c>&lt;Blattwechsel&gt;</c> sind je ein eigener Dialog mit eigener Kopfpille) führt höchstens
/// EINE Pille den Assistenten — die Kopfpille; jede weitere setzt <c>MitAssistent="false"</c> und
/// bleibt der Weg zu IHRER Hilfeseite. Eine eingebettete Seite, deren Wirt die Pille trägt, führt
/// keine (<see cref="OHNE_ASSISTENT"/>).</para>
///
/// <para><b>Die Grenze der Wache:</b> Sie liest Markup (Razor-Kommentare ausgeblendet), keinen
/// kompilierten Baum. Zwei Pillen in einander ausschließenden Zweigen gälten ihr als zwei — das
/// ist die sichere Seite. Einbettungen über Dateigrenzen hinweg sieht sie nur, wo
/// <see cref="OHNE_ASSISTENT"/> sie nennt; den gezeichneten Baum prüft
/// <see cref="KiKnopfEinmalZapfprofilTests"/> am Blatt des Befunds.</para>
/// </summary>
public sealed class KiKnopfEinmalWacheTests
{
    /// <summary>
    /// Eingebettete Seiten, deren Wirt (oder das Kopfband des Hauptfensters) die Pille mit dem
    /// Assistenten trägt — hier führt KEINE Pille ihn.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> OHNE_ASSISTENT =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor"] =
                "steht nur in BerichteKostenSeite; deren Pille oder die des Kopfbands führt den Assistenten"
        };

    [Fact]
    public void Jeder_Dialog_traegt_hoechstens_einen_KI_Knopf()
    {
        string wurzel = Wurzel();
        var verstoesse = new List<string>();
        int dateien = 0;

        foreach (string voll in Directory.EnumerateFiles(Path.Combine(wurzel, "EPOS.UI"), "*.razor",
                                                         SearchOption.AllDirectories))
        {
            string repopfad = Path.GetRelativePath(wurzel, voll).Replace(Path.DirectorySeparatorChar, '/');
            if (repopfad.Contains("/bin/", StringComparison.Ordinal) ||
                repopfad.Contains("/obj/", StringComparison.Ordinal)) continue;

            List<Pille> pillen = Pillen(File.ReadAllText(voll));
            if (pillen.Count == 0) continue;
            dateien++;

            int erlaubt = OHNE_ASSISTENT.ContainsKey(repopfad) ? 0 : 1;
            foreach (IGrouping<string, Pille> gruppe in pillen.Where(p => p.MitAssistent).GroupBy(p => p.Kontext))
            {
                if (gruppe.Count() > erlaubt)
                    verstoesse.Add(repopfad + " (Zeilen " + string.Join(", ", gruppe.Select(p => p.Zeile)) + ")");
            }
        }

        Assert.True(dateien > 100, "Die Wache fand nur " + dateien + " Dateien mit Hilfepillen — Suchweg prüfen.");
        Assert.True(OHNE_ASSISTENT.Keys.All(k => File.Exists(Path.Combine(wurzel, k))),
                    "Ein Eintrag in OHNE_ASSISTENT nennt eine Datei, die es nicht mehr gibt.");
        Assert.True(verstoesse.Count == 0,
            "Mehr als ein KI-Knopf in einem Dialog — jede Pille außer der Kopfpille setzt " +
            "MitAssistent=\"false\": " + string.Join("; ", verstoesse));
    }

    [Fact]
    public void Gegenprobe_zwei_Pillen_im_Kopf_fallen_auf()
    {
        const string markup =
            "<div class=\"epos-dialog-kopf\">\n" +
            "    <InfoKnopf Schluessel=\"Form_X.btn_Help\" />\n" +
            "    <InfoKnopf Schluessel=\"Form_X.Berechnung\"\n" +
            "               StandardKurztext=\"Rechenweg\" />\n" +
            "</div>";

        List<Pille> pillen = Pillen(markup);

        Assert.Equal(2, pillen.Count);
        Assert.All(pillen, p => Assert.True(p.MitAssistent));
        Assert.Single(pillen.GroupBy(p => p.Kontext));
        Assert.Equal(new[] { 2, 3 }, pillen.Select(p => p.Zeile));
    }

    [Fact]
    public void Gegenprobe_die_Ueberlagerung_ist_ein_eigener_Dialog()
    {
        const string markup =
            "<InfoKnopf Schluessel=\"Form_X.btn_Help\" />\n" +
            "<Ueberlagerung Offen=\"@_offen\" Titel=\"Import\" Geschlossen=\"() => _offen = false\">\n" +
            "    <InfoKnopf Schluessel=\"Form_X_Import.btn_Help\" />\n" +
            "</Ueberlagerung>\n" +
            "<Ueberlagerung Offen=\"@_zwei\" />\n" +
            "<InfoKnopf Schluessel=\"Form_X.Berechnung\" MitAssistent=\"false\" />";

        List<Pille> pillen = Pillen(markup);

        Assert.Equal(3, pillen.Count);
        Assert.Equal(2, pillen.Where(p => p.MitAssistent).GroupBy(p => p.Kontext).Count());
        // Die selbstschließende Überlagerung öffnet keinen Kontext: Die dritte Pille steht
        // wieder im Kontext der ersten.
        Assert.Equal(pillen[0].Kontext, pillen[2].Kontext);
        Assert.False(pillen[2].MitAssistent);
    }

    [Fact]
    public void Gegenprobe_Kommentare_zaehlen_nicht()
    {
        const string markup =
            "@* Muster: <InfoKnopf Schluessel=\"Form_X.btn_Help\" /> *@\n" +
            "<InfoKnopf Schluessel=\"Form_X.btn_Help\" />";

        List<Pille> pillen = Pillen(markup);

        Assert.Single(pillen);
        Assert.Equal(2, pillen[0].Zeile);
    }

    // =====================================================================
    //  Der Leser
    // =====================================================================

    /// <summary>Eine Hilfepille: Zeile, Überlagerungskontext, führt sie den Assistenten?</summary>
    internal sealed record Pille(int Zeile, string Kontext, bool MitAssistent);

    /// <summary>Die Pillen eines Razor-Textes samt Kontext; Kommentare ausgeblendet.</summary>
    internal static List<Pille> Pillen(string text)
    {
        // Kommentare durch Leerzeichen ersetzen - Positionen und Zeilen bleiben stehen.
        string maske = Regex.Replace(text, @"@\*.*?\*@",
                                     m => Regex.Replace(m.Value, @"[^\n]", " "), RegexOptions.Singleline);

        var ereignisse = new List<(int Pos, char Art)>();
        foreach (Match m in Regex.Matches(maske, @"<(/?)(Ueberlagerung|Blattwechsel)\b"))
        {
            if (m.Groups[1].Value == "/")
            {
                ereignisse.Add((m.Index, 'z'));
                continue;
            }

            int ende = TagEnde(maske, m.Index);
            if (ende > 0 && maske[ende - 1] == '/') continue; // selbstschließend: kein Inhalt
            ereignisse.Add((m.Index, 'a'));
        }

        foreach (Match m in Regex.Matches(maske, @"<InfoKnopf\b"))
            ereignisse.Add((m.Index, 'p'));

        var stapel = new List<int>();
        var pillen = new List<Pille>();
        foreach ((int pos, char art) in ereignisse.OrderBy(e => e.Pos))
        {
            switch (art)
            {
                case 'a':
                    stapel.Add(pos);
                    break;
                case 'z':
                    if (stapel.Count > 0) stapel.RemoveAt(stapel.Count - 1);
                    break;
                default:
                    int ende = TagEnde(maske, pos);
                    string tag = ende < 0 ? maske.Substring(pos) : maske.Substring(pos, ende - pos);
                    bool ohne = Regex.IsMatch(tag, @"\bMitAssistent\s*=\s*""false""");
                    int zeile = 1 + maske.Take(pos).Count(c => c == '\n');
                    pillen.Add(new Pille(zeile, string.Join("/", stapel), !ohne));
                    break;
            }
        }

        return pillen;
    }

    /// <summary>
    /// Die Stelle des <c>&gt;</c>, das den Starttag ab <paramref name="start"/> schließt — außerhalb
    /// von Anführungszeichen, damit ein <c>=&gt;</c> in einem Attributwert nicht als Ende gilt;
    /// <c>-1</c>, wenn keines kommt.
    /// </summary>
    private static int TagEnde(string text, int start)
    {
        bool inWert = false;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"') inWert = !inWert;
            else if (c == '>' && !inWert) return i;
        }

        return -1;
    }

    /// <summary>Der Weg zur Repowurzel — dasselbe Verfahren wie <c>KiDialogkatalogTests.Wurzel</c>.</summary>
    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }
}

/// <summary>
/// Der gezeichnete Nachweis zur <see cref="KiKnopfEinmalWacheTests"/> am Blatt des Befunds: Das
/// „Brauchwasser-Zapfprofil" trägt im Kopf zwei Hilfepillen (Bedienung, Rechenweg), aber genau
/// EINEN KI-Knopf. In der Sammlung <c>KiDialogweg</c>, weil der Fall den prozessweiten Haken
/// <c>KiVerfuegbarkeit.Haken</c> setzt.
/// </summary>
[Collection("KiDialogweg")]
public sealed class KiKnopfEinmalZapfprofilTests : KiDialogwegBasis
{
    [Fact]
    public void Das_Zapfprofil_traegt_zwei_Hilfepillen_und_einen_KI_Knopf()
    {
        var cut = Render<ZapfprofilDialog>();

        Assert.Equal(2, cut.FindAll(".epos-dialog-kopf .epos-infoknopf").Count);
        Assert.Single(cut.FindAll(".epos-kiknopf"));
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-kiknopf"));
    }
}

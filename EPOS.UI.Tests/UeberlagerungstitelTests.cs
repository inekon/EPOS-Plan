using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache zur Hausregel <b>„Ein Titel, eine Stelle"</b>
/// (<b>W11b‑B‑9</b>, Abnahmeliste vom 11.09.2026, Auftrag <b>#187</b>).
///
/// <para><b>Der Befund:</b> Zeigt eine <c>&lt;Ueberlagerung Titel="@X"&gt;</c>
/// eine eingebettete Dialog-Komponente, trug diese ihren eigenen Kopf
/// (<c>&lt;h1 class="epos-dialog-titel"&gt;</c>) oft UNBEDINGT — mit demselben
/// Text wie die Überlagerung. Am Bildschirm stand der Titel dann zweimal
/// übereinander. Zwanzig Stellen im Haus trugen den Fehler, in zwei Bauarten:</para>
///
/// <para><b>Bauart A (elf Stellen, alle <c>NamensDialog</c>):</b> derselbe
/// Bezeichner steht WORTGLEICH als <c>Titel="@X"</c> der Überlagerung UND als
/// <c>TitelText="@X"</c> der eingebetteten Komponente — rein aus dem Markup
/// beweisbar, ohne eine Hülle zu lesen. Das prüft
/// <see cref="Jede_Ueberlagerung_und_ihr_Kind_tragen_verschiedene_Titelquellen"/>.</para>
///
/// <para><b>Bauart B (neun Stellen, die Kosten-, Gesetzes-, Klimazonen- und
/// Pufferspeicherdialoge):</b> die Überlagerung und die Komponente bekommen
/// ihren Text aus ZWEI verschiedenen Ausdrücken, die aber in derselben
/// <c>Huelle.cs</c>-Methode auf denselben Ressourcenschlüssel bzw. dieselbe
/// Literalzeichenkette zurückgehen (<c>["CaseTitel"] = T("KCASE_TITEL", …)</c>
/// UND, in derselben Methode, <c>["TitelText"] = T("KCASE_TITEL", …)</c>).
/// Das prüft <see cref="Keine_Huelle_Methode_setzt_TitelText_auf_denselben_Schluessel_wie_ihr_eigenes_Titel_Feld"/> —
/// bewusst NUR innerhalb EINER Methode (kein Sprung über Klassen oder
/// Delegatenketten hinweg): Die vier behobenen Fälle (<c>CaseGaben</c>,
/// <c>EditorGaben</c>, <c>KostenfaktorKatalogHuelle.Gaben</c>,
/// <c>GesetzeGaben</c>-Wrapper) liegen alle so; ein Sprung über mehrere
/// Klassen hinweg (wie einst <c>KatalogGaben</c> in <c>BhkwHuelle</c>) bleibt
/// Handarbeit — dafür stehen die 18 bunit-Fälle in
/// <c>Dialoge/KostenKomponenteDialogTests.cs</c> und die Sichtung im
/// Umsetzungsbericht zu #187.</para>
///
/// <para><b>Die Behebung, immer dieselbe Bauart</b> (Vorbild
/// <c>WaermepumpeAnlageDialog.razor:96</c>, seit #187 auch
/// <c>NamensDialog</c>, <c>CaseEingabeDialog</c>, <c>VorlagenPositionDialog</c>,
/// <c>KostenfaktorKatalogDialog</c>, <c>GesetzeskatalogDialog</c>):
/// <c>&lt;div class="epos-dialog-kopf @(string.IsNullOrEmpty(TitelText) ? "epos-dialog-kopf--ohnetitel" : "")"&gt;</c>,
/// der <c>&lt;h1&gt;</c> nur <c>@if (!string.IsNullOrEmpty(TitelText))</c>, der
/// Hilfeknopf bleibt immer. Trägt <c>TitelText</c> noch eine ZWEITE Aufgabe im
/// Rumpf der Komponente (<c>KlimazonenkarteDialog</c> speist daraus
/// <c>Bildkarte.Bildbeschreibung</c>, <c>PufferSpProjektDialog</c> seinen
/// <c>Gruppenkopf</c>), bleibt <c>TitelText</c> unverändert und ein zweiter
/// Parameter <c>[Parameter] public bool TitelAnzeigen { get; set; } = true;</c>
/// steuert stattdessen NUR den eigenen Kopf.</para>
///
/// <para><b>Ausnahmeliste: leer.</b> Beide Fälle unten müssen ohne
/// Einschränkung grün bleiben — jeder neue Doppel-Titel, gleich welcher
/// Bauart, ist ein Fehler, keine Ausnahme.</para>
/// </summary>
public sealed class UeberlagerungstitelTests
{
    // ---------------------------------------------------------------------
    //  Bauart A — reine Markup-Wache (kein Huelle-Zugriff nötig)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Ein Fund: eine Überlagerung, deren eingebettete Komponente denselben
    /// Bezeichner sowohl im eigenen <c>TitelText</c> als auch im
    /// <c>Titel</c> der umschließenden Überlagerung trägt — UND diese
    /// Komponente ihren Kopf dabei UNBEDINGT zeichnet.
    /// </summary>
    private readonly record struct MarkupFund(string Datei, int Zeile, string Bezeichner, string Kind);

    [Fact]
    public void Jede_Ueberlagerung_und_ihr_Kind_tragen_verschiedene_Titelquellen()
    {
        List<MarkupFund> funde = MarkupFunde(RazorDateien());

        Assert.True(funde.Count == 0,
            "Doppelter Ueberlagerungstitel (Bauart A, W11b-B-9):\n" + Bericht(funde));
    }

    /// <summary>
    /// Die Wache muss auch etwas finden können (Lehre W6‑B‑1: eine grüne
    /// Wache, die nie rot werden kann, prüft nichts). Der eingefrorene
    /// Bestand von <c>KatalogDublettenDialog.razor</c> VOR #187 — Titel und
    /// eingebettetes <c>TitelText</c> tragen wortgleich <c>@UmbenennenText</c>,
    /// der Kopf der Komponente zeichnet unbedingt — muss als Fund erscheinen.
    /// </summary>
    [Fact]
    public void Die_Markup_Wache_findet_den_eingefrorenen_Befund_vor_187()
    {
        const string wirt =
            "<Ueberlagerung Offen=\"@_umbenennenOffen\" Titel=\"@UmbenennenText\" Schliessbar=\"false\">\n" +
            "    <KindInhalt>\n" +
            "        <NamensDialog TitelText=\"@UmbenennenText\" FrageText=\"@LabelNameNeu\" />\n" +
            "    </KindInhalt>\n" +
            "</Ueberlagerung>\n";
        const string kind =
            "<div class=\"epos-dialog-kopf\">\n" +
            "    <h1 class=\"epos-dialog-titel\">@TitelText</h1>\n" +
            "    <InfoKnopf Schluessel=\"@HilfeSchluessel\" />\n" +
            "</div>\n";

        var dateien = new Dictionary<string, string>
        {
            ["Wirt.razor"] = wirt,
            ["NamensDialog.razor"] = kind
        };

        List<MarkupFund> funde = MarkupFunde(dateien);

        MarkupFund fund = Assert.Single(funde);
        Assert.Equal("UmbenennenText", fund.Bezeichner);
        Assert.Equal("NamensDialog", fund.Kind);

        // Die HEUTIGE Fassung von NamensDialog (Kopf bedingt) meldet nichts mehr.
        var dateienHeute = new Dictionary<string, string>
        {
            ["Wirt.razor"] = wirt,
            ["NamensDialog.razor"] =
                "<div class=\"epos-dialog-kopf @(string.IsNullOrEmpty(TitelText) ? \"epos-dialog-kopf--ohnetitel\" : \"\")\">\n" +
                "    @if (!string.IsNullOrEmpty(TitelText))\n" +
                "    {\n" +
                "        <h1 class=\"epos-dialog-titel\">@TitelText</h1>\n" +
                "    }\n" +
                "    <InfoKnopf Schluessel=\"@HilfeSchluessel\" />\n" +
                "</div>\n"
        };
        Assert.Empty(MarkupFunde(dateienHeute));
    }

    private static List<MarkupFund> MarkupFunde(IReadOnlyDictionary<string, string> dateien)
    {
        var funde = new List<MarkupFund>();

        foreach (KeyValuePair<string, string> datei in dateien)
        {
            string s = datei.Value;

            foreach (Match m in Regex.Matches(s, @"<Ueberlagerung\b"))
            {
                int tagEnde = FindeTagEnde(s, m.Index);
                if (tagEnde < 0) continue;
                string offnungstag = s.Substring(m.Index, tagEnde - m.Index + 1);

                Match titelM = Regex.Match(offnungstag, "Titel=\"@([A-Za-z_][A-Za-z0-9_]*)\"");
                if (!titelM.Success) continue;
                string bezeichner = titelM.Groups[1].Value;

                int blockEnde = s.IndexOf("</Ueberlagerung>", tagEnde, StringComparison.Ordinal);
                if (blockEnde < 0) blockEnde = s.Length;
                string block = s.Substring(tagEnde + 1, blockEnde - tagEnde - 1);

                foreach (Match kindM in Regex.Matches(block,
                             "TitelText=\"@" + Regex.Escape(bezeichner) + "\""))
                {
                    // welche Komponente traegt dieses TitelText? das naechste
                    // "<Name" davor im selben Block.
                    int kindTagStart = block.LastIndexOf('<', kindM.Index);
                    if (kindTagStart < 0) continue;
                    Match nameM = Regex.Match(block.Substring(kindTagStart),
                        @"^<(?:[A-Za-z0-9_.]*\.)?([A-Z][A-Za-z0-9]*)\b");
                    if (!nameM.Success) continue;
                    string kind = nameM.Groups[1].Value;

                    if (!dateien.TryGetValue(kind + ".razor", out string? kindDatei))
                        continue;                       // Kind nicht Teil dieses Laufs/Baums

                    if (ZeigtKopfUnbedingt(kindDatei))
                    {
                        int zeile = 1 + CountNewlines(s, m.Index);
                        funde.Add(new MarkupFund(datei.Key, zeile, bezeichner, kind));
                    }
                }
            }
        }

        return funde;
    }

    /// <summary>
    /// Zeichnet <paramref name="kindText"/> seinen eigenen
    /// <c>epos-dialog-titel</c> UNBEDINGT — ohne ein unmittelbar
    /// vorausgehendes <c>@if (...) {</c>?
    /// </summary>
    private static bool ZeigtKopfUnbedingt(string kindText)
    {
        Match h1 = Regex.Match(kindText, "<h1 class=\"epos-dialog-titel\">@TitelText</h1>");
        if (!h1.Success) return false;

        // Das naechste "@if" davor - seine Bedingung darf selbst Klammern
        // enthalten (z. B. "IsNullOrEmpty(TitelText)"), deshalb Klammerbilanz
        // statt "[^)]*" (das brach an der ERSTEN inneren Klammer ab).
        int ifIdx = kindText.LastIndexOf("@if", h1.Index, StringComparison.Ordinal);
        if (ifIdx < 0) return true;

        int offen = kindText.IndexOf('(', ifIdx);
        if (offen < 0 || offen > h1.Index) return true;

        int zu = FindeSchliessendeKlammer(kindText, offen);
        if (zu < 0 || zu > h1.Index) return true;

        // Zwischen der Bedingung und dem <h1> darf nur Leerraum und GENAU
        // EINE oeffnende geschweifte Klammer stehen - sonst ist das "@if"
        // ein fremder Block, der den Kopf nicht wirklich umschliesst.
        string dazwischen = kindText.Substring(zu + 1, h1.Index - zu - 1);
        return !Regex.IsMatch(dazwischen, @"^\s*\{\s*$");
    }

    private static int FindeSchliessendeKlammer(string text, int offenerIndex)
    {
        int depth = 0;
        for (int i = offenerIndex; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    // ---------------------------------------------------------------------
    //  Bauart B — eine Huelle-Methode setzt zwei Titelfelder gleich
    // ---------------------------------------------------------------------

    private readonly record struct HuelleFund(string Datei, string Methode, string Aussen, string Schluessel);

    [Fact]
    public void Keine_Huelle_Methode_setzt_TitelText_auf_denselben_Schluessel_wie_ihr_eigenes_Titel_Feld()
    {
        List<HuelleFund> funde = new();
        foreach (string datei in HuelleDateien())
            funde.AddRange(HuelleFunde(File.ReadAllText(datei), Path.GetFileName(datei)));

        Assert.True(funde.Count == 0,
            "Doppelter Ueberlagerungstitel ueber eine Huelle-Methode (Bauart B, W11b-B-9):\n"
            + string.Join("\n", funde.Select(f =>
                "  " + f.Datei + " " + f.Methode + "(): \"" + f.Aussen + "\" == TitelText (" + f.Schluessel + ")")));
    }

    /// <summary>
    /// Gegenprobe: der eingefrorene Bestand von <c>KostenKomponenteHuelle.CaseGaben</c>
    /// VOR #187 — <c>["TitelText"] = T("KCASE_TITEL", …)</c> in derselben
    /// Methode, die (über <c>KostenKomponenteDialog.CaseTitel</c>, hier nicht
    /// Teil des Rumpfs, deshalb als eigener Eintrag nachgebildet) denselben
    /// Schlüssel bereits als Überlagerungstitel führt — muss als Fund
    /// erscheinen; die HEUTIGE Fassung (<c>["TitelText"] = ""</c>) nicht mehr.
    /// </summary>
    [Fact]
    public void Die_Huelle_Wache_findet_den_eingefrorenen_Befund_vor_187()
    {
        const string vorher =
            "private IReadOnlyDictionary<string, object> CaseGaben(KostenPositionZeile z)\n" +
            "{\n" +
            "    return new Dictionary<string, object>\n" +
            "    {\n" +
            "        [\"CaseTitel\"] = T(\"KCASE_TITEL\", \"Eingabe Worst/Best Case\"),\n" +
            "        [\"TitelText\"] = T(\"KCASE_TITEL\", \"Eingabe Worst/Best Case\"),\n" +
            "        [\"OkText\"] = MyResource.Resource.ALLG_BTN_OK\n" +
            "    };\n" +
            "}\n";

        List<HuelleFund> funde = HuelleFunde(vorher, "Test.cs");
        HuelleFund fund = Assert.Single(funde);
        Assert.Equal("KCASE_TITEL", fund.Schluessel);

        const string heute =
            "private IReadOnlyDictionary<string, object> CaseGaben(KostenPositionZeile z)\n" +
            "{\n" +
            "    return new Dictionary<string, object>\n" +
            "    {\n" +
            "        [\"CaseTitel\"] = T(\"KCASE_TITEL\", \"Eingabe Worst/Best Case\"),\n" +
            "        [\"TitelText\"] = \"\",\n" +
            "        [\"OkText\"] = MyResource.Resource.ALLG_BTN_OK\n" +
            "    };\n" +
            "}\n";
        Assert.Empty(HuelleFunde(heute, "Test.cs"));
    }

    private static List<HuelleFund> HuelleFunde(string text, string dateiname)
    {
        var funde = new List<HuelleFund>();

        foreach ((string methodenname, string rumpf) in MethodenRuempfe(text))
        {
            var eintraege = new List<(string Schluessel, string Wert)>();
            foreach (Match em in Regex.Matches(rumpf, @"\[""([A-Za-z0-9_]+)""\]\s*=\s*"))
            {
                string wert = ScanAusdruck(rumpf, em.Index + em.Length);
                eintraege.Add((em.Groups[1].Value, wert));
            }

            string? titelTextWert = eintraege.FirstOrDefault(e => e.Schluessel == "TitelText").Wert;
            if (titelTextWert is null) continue;
            (string? art, string? wert) titelSchluessel = RessourcenSchluessel(titelTextWert);
            if (titelSchluessel.art is null || titelSchluessel.art == "LEER") continue;

            foreach ((string schluessel, string ausdruck) in eintraege)
            {
                if (schluessel == "TitelText") continue;
                if (!schluessel.EndsWith("Titel", StringComparison.Ordinal)) continue;

                (string? art, string? wert) aussen = RessourcenSchluessel(ausdruck);
                if (aussen.art is null || aussen.art == "LEER") continue;

                if (aussen.art == titelSchluessel.art && aussen.wert == titelSchluessel.wert)
                    funde.Add(new HuelleFund(dateiname, methodenname, schluessel, titelSchluessel.wert!));
            }
        }

        return funde;
    }

    /// <summary>Jede Methode der Datei als (Name, Rumpf-zwischen-den-Klammern).</summary>
    private static IEnumerable<(string Name, string Rumpf)> MethodenRuempfe(string text)
    {
        foreach (Match m in Regex.Matches(text,
                     @"(?:private|internal|public|static)[\w\s<>,\[\]]*?\b([A-Za-z_][A-Za-z0-9_]*)\s*\([^;{}]*\)\s*\{"))
        {
            int offen = m.Index + m.Length - 1;
            string rumpf = FindeKlammerRumpf(text, offen);
            if (rumpf.Length > 0) yield return (m.Groups[1].Value, rumpf);
        }
    }

    /// <summary>Ressourcenschlüssel bzw. Literaltext eines Ausdrucks wie
    /// <c>T("KEY", "Rueckfall")</c>, <c>Resource.KEY</c> oder <c>"Text"</c>.
    /// Liefert (Art, Wert); Art "LEER" fuer eine leere Zeichenkette.</summary>
    private static (string? Art, string? Wert) RessourcenSchluessel(string ausdruck)
    {
        string a = ausdruck.Trim().TrimEnd(',');
        if (a is "\"\"" or "''") return ("LEER", "");
        Match helfer = Regex.Match(a, @"^(?:T|Text_|TextEinfach|Text)\(\s*""([A-Z0-9_]+)""");
        if (helfer.Success) return ("KEY", helfer.Groups[1].Value);
        Match res = Regex.Match(a, @"^(?:MyResource\.)?Resource\.([A-Za-z0-9_]+)");
        if (res.Success && res.Groups[1].Value != "ResourceManager") return ("KEY", res.Groups[1].Value);
        Match lit = Regex.Match(a, "^\"([^\"]*)\"");
        if (lit.Success) return ("LITERAL", lit.Groups[1].Value);
        return (null, null);
    }

    // ---------------------------------------------------------------------
    //  Kleinwerkzeug (dieselbe Bauweise wie StilblattTests: kein echter
    //  Parser, nur Klammerbilanz und Zeichenketten ueberspringen)
    // ---------------------------------------------------------------------

    private static int FindeTagEnde(string s, int start)
    {
        int i = start;
        while (true)
        {
            i = s.IndexOf('>', i);
            if (i < 0) return -1;
            if (s[i - 1] == '=') { i++; continue; }   // "=>"-Pfeil, kein Tagende
            return i;
        }
    }

    private static string ScanAusdruck(string text, int start)
    {
        int depth = 0;
        char? inStr = null;
        int i = start;
        while (i < text.Length)
        {
            char c = text[i];
            if (inStr is not null)
            {
                if (c == '\\') { i += 2; continue; }
                if (c == inStr) inStr = null;
            }
            else if (c == '=' && i + 1 < text.Length && text[i + 1] == '>')
            {
                i += 2; continue;
            }
            else if (c is '"' or '\'')
            {
                inStr = c;
            }
            else if (c is '(' or '[' or '{' or '<')
            {
                depth++;
            }
            else if (c is ')' or ']' or '}' or '>')
            {
                if (depth > 0) depth--;
            }
            else if (depth == 0 && c is ',' or ';')
            {
                return text.Substring(start, i - start).Trim();
            }
            i++;
        }
        return text.Substring(start).Trim();
    }

    private static string FindeKlammerRumpf(string text, int offenerIndex)
    {
        int depth = 0;
        char? inStr = null;
        int i = offenerIndex;
        while (i < text.Length)
        {
            char c = text[i];
            if (inStr is not null)
            {
                if (c == '\\') { i += 2; continue; }
                if (c == inStr) inStr = null;
            }
            else if (c is '"' or '\'')
            {
                inStr = c;
            }
            else if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return text.Substring(offenerIndex, i - offenerIndex + 1);
            }
            i++;
        }
        return "";
    }

    private static int CountNewlines(string s, int bisIndex)
    {
        int n = 0;
        for (int i = 0; i < bisIndex && i < s.Length; i++)
            if (s[i] == '\n') n++;
        return n;
    }

    private static string Bericht(IEnumerable<MarkupFund> funde)
        => string.Join("\n", funde.Select(f =>
            "  " + f.Datei + ":" + f.Zeile + " Titel/TitelText=\"@" + f.Bezeichner
            + "\" -> " + f.Kind));

    // ---------------------------------------------------------------------
    //  Der Weg zu den Quelldateien (dasselbe Verfahren wie StilblattTests.Wwwroot)
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

    private static string[] HuelleDateien()
        => Directory.GetFiles(Path.Combine(Wurzel(), "WindowsFormsApplication1"), "*Huelle.cs",
                               SearchOption.AllDirectories);

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Wache_liest_tatsaechlich_razor_und_huelle_dateien()
    {
        Assert.NotEmpty(RazorDateien());
        Assert.NotEmpty(HuelleDateien());
    }
}

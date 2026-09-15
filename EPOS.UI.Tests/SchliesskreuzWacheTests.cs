using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache zur Hausregel <b>„Das Kreuz steht beim Titel"</b>
/// (Anwenderentscheid vom 15.09.2026: „Alle Dialoge sollten mit einem Kreuz zu
/// schließen sein und nicht erst ganz unten mit den Buttons. Sofern dem nichts
/// widerspricht, bei allen Dialogen einheitlich umsetzen.").
///
/// <para><b>Der Befund:</b> Die Oberdialoge laufen unter Windows in einem Fenster
/// mit Titelleiste und haben dort ihr Kreuz; die Unterdialoge liegen als
/// <c>&lt;Ueberlagerung&gt;</c> im selben Fenster und unterdrückten es fast überall
/// mit <c>Schliessbar="false"</c>, und die Dialogköpfe
/// (<c>.epos-dialog-kopf</c>) trugen selbst gar keins. Auf iOS gibt es gar keine
/// Titelleiste — der Weg nach draußen stand dort nur unten in der
/// <c>SpeichernLeiste</c>, nach dem Rollen.</para>
///
/// <para><b>Die Regel:</b> Jeder Dialogkopf trägt rechts außen das
/// <c>Schliesskreuz</c> (✕ = Esc = Abbrechen, <c>Geschlossen</c> bekommt die
/// Esc-Aktion des Dialogs). Zeigt ein Dialog seinen Titel NICHT (weil die
/// umschließende <c>Ueberlagerung</c> ihn trägt — Hausregel „Ein Titel, eine
/// Stelle", Wache <see cref="UeberlagerungstitelTests"/>), zeigt er auch kein
/// Kreuz; dann trägt die <c>Ueberlagerung</c> das Kreuz (<c>Schliessbar</c>,
/// Vorgabe <c>true</c>).</para>
///
/// <para><b>Daraus die drei Fälle:</b> (1) Jede
/// <c>EPOS.UI/Dialoge/**/*Dialog.razor</c>, die einen Dialogkopf zeichnet, enthält
/// auch ein <c>&lt;Schliesskreuz</c>. (2) Kein <c>&lt;Ueberlagerung&gt;</c>-Starttag
/// unter <c>EPOS.UI/Dialoge/</c> und <c>EPOS.UI/Seiten/</c> trägt zugleich einen
/// nicht-leeren <c>Titel</c> und <c>Schliessbar="false"</c> — denn dann hätte
/// dieser Bereich gar kein Kreuz: die Überlagerung zeigt keins, und die eingebettete
/// Komponente zeigt wegen des Titels oben auch keins. (3) Die GEGENPROBE dazu: Keine
/// betitelte, schließbare Überlagerung zeigt ZWEI Kreuze.</para>
///
/// <para><b>Fall 3 im Einzelnen</b> (Anwenderbefund vom 15.09.2026: „Doppeltes Kreuz
/// dürfen nicht sein!"): Trägt die Überlagerung Titel und ✕, muss jede darin
/// eingebettete Komponente, deren eigene Quelldatei ein <c>&lt;Schliesskreuz</c>
/// zeichnet, an IHRER Einbettungsstelle <c>TitelText=""</c> oder
/// <c>TitelAnzeigen="false"</c> tragen — sonst standen ✕ und Titel zweimal
/// übereinander. Das Attribut gehört AUSDRÜCKLICH in den Einbettungs-Starttag, und
/// zwar HINTER ein etwaiges <c>@@attributes="…"</c> (in Blazor gewinnt das rechts
/// stehende Attribut): Dann hängt die Regel nicht daran, was eine Hülle in ihren
/// Parametersatz legt. Die Titelseite derselben Sache prüft
/// <see cref="UeberlagerungstitelTests"/>; diese Wache hier prüft das KREUZ und
/// braucht deshalb keinen Blick in die Hüllen.</para>
///
/// <para><b>Die Grenze der Wache:</b> Sie liest Markup, keinen kompilierten Baum.
/// Sie findet den Starttag über die erste unmaskierte spitze Klammer (Pfeile
/// <c>=&gt;</c> in Attributwerten übersprungen, <see cref="FindeTagEnde"/>) und
/// prüft Zeichenketten. Ein Titel, der erst zur LAUFZEIT leer wird
/// (<c>Titel="@_titel"</c>), gilt ihr als Titel — das ist die sichere Seite: Wer
/// dort das Kreuz unterdrückt, muss es begründen.</para>
/// </summary>
public sealed class SchliesskreuzWacheTests
{
    // =====================================================================
    //  Die Ausnahmeliste
    // =====================================================================

    /// <summary>
    /// Eine begründete Stelle, an der eine betitelte <c>Ueberlagerung</c> ihr Kreuz
    /// unterdrücken darf. <paramref name="Datei"/> ist der repo-relative Pfad mit
    /// Schrägstrichen, <paramref name="Fragment"/> ein Attribut-Stück, das GENAU
    /// diesen einen Starttag der Datei trifft.
    /// </summary>
    private readonly record struct Ausnahme(string Datei, string Fragment, string Grund);

    /// <summary>
    /// Die Ausnahmen, je mit Grund. <b>Kurz halten:</b> Eine Überlagerung ohne
    /// Kreuz ist eine Sackgasse für die Maus — sie braucht einen Zustand, der von
    /// selbst endet, oder eine Antwort, ohne die es nicht weitergeht.
    /// </summary>
    private static readonly Ausnahme[] AUSNAHMEN =
    {
        new Ausnahme(
            "EPOS.UI/Dialoge/Simulation/QuelleErdreichDialog.razor",
            "Offen=\"@_laeuft\"",
            "Warte-Ueberlagerung waehrend eines laufenden Erdreich-Laufs: Sie darf nicht "
            + "zugehen, solange gerechnet wird - der Lauf schliesst sie selbst."),

        new Ausnahme(
            "EPOS.UI/Dialoge/Simulation/WertAbfrage.razor",
            "Offen=\"@Offen\"",
            "Eine Rueckfrage nach einem Wert: Sie muss beantwortet werden, sonst fehlt "
            + "dem weiteren Weg die Zahl. Abbrechen steht im Inhalt.")
    };

    /// <summary>
    /// Die Ausnahmen zu Fall 3 (zwei Kreuze in einem Bereich), je mit Grund.
    /// <paramref name="Ausnahme.Fragment"/> trifft hier den EINBETTUNGS-Starttag der
    /// Komponente, nicht den der Überlagerung. <b>Sie ist leer</b>: Ein zweites Kreuz
    /// im selben Bereich ist keine Geschmacksfrage — der Anwender hat es am
    /// 15.09.2026 ausdrücklich zurückgegeben („Doppeltes Kreuz dürfen nicht sein!").
    /// Wer hier etwas einträgt, braucht einen Grund, der über „passt gerade nicht"
    /// hinausgeht.
    /// </summary>
    private static readonly Ausnahme[] AUSNAHMEN_DOPPEL = Array.Empty<Ausnahme>();

    // =====================================================================
    //  Fall 1 — jeder Dialogkopf traegt das Kreuz
    // =====================================================================

    [Fact]
    public void Jeder_Dialogkopf_traegt_das_Schliesskreuz()
    {
        Dictionary<string, string> dateien = Dateien("Dialoge", "*Dialog.razor");

        List<string> fehlen = OhneKreuz(dateien);

        Assert.True(fehlen.Count == 0,
            "Diese Dialogkoepfe tragen kein <Schliesskreuz /> (Hausregel \"Das Kreuz steht "
            + "beim Titel\", Anwenderentscheid 15.09.2026). Es gehoert als LETZTES Kind in "
            + "den <div class=\"epos-dialog-kopf\">, unter derselben Bedingung wie der <h1>, "
            + "und bekommt die Esc-Aktion des Dialogs:\n"
            + string.Join("\n", fehlen.Select(d => "  " + d)));
    }

    /// <summary>
    /// Die Gegenprobe (Lehre W6‑B‑1: eine Wache, die nie rot werden kann, prüft
    /// nichts): Ein Kopf ohne Kreuz muss als Fund erscheinen, derselbe Kopf mit
    /// Kreuz nicht mehr — und eine Datei ganz ohne Dialogkopf bleibt außen vor.
    /// </summary>
    [Fact]
    public void Die_Kopf_Wache_findet_einen_Kopf_ohne_Kreuz()
    {
        const string ohneKreuz =
            "<div class=\"epos-dialog-kopf\">\n" +
            "    <h1 class=\"epos-dialog-titel\">@TitelText</h1>\n" +
            "    <InfoKnopf Schluessel=\"@HilfeSchluessel\" />\n" +
            "</div>\n";
        const string mitKreuz =
            "<div class=\"epos-dialog-kopf\">\n" +
            "    <h1 class=\"epos-dialog-titel\">@TitelText</h1>\n" +
            "    <InfoKnopf Schluessel=\"@HilfeSchluessel\" />\n" +
            "    <Schliesskreuz Geschlossen=\"Abbrechen\" />\n" +
            "</div>\n";
        const string ohneKopf = "<div class=\"epos-dialog\">\n    <p>nur Inhalt</p>\n</div>\n";

        Assert.Equal(
            new[] { "a/AltDialog.razor" },
            OhneKreuz(new Dictionary<string, string>
            {
                ["a/AltDialog.razor"] = ohneKreuz,
                ["a/NeuDialog.razor"] = mitKreuz,
                ["a/StillDialog.razor"] = ohneKopf
            }).ToArray());
    }

    /// <summary>Jede Datei mit Dialogkopf, aber ohne Schliesskreuz — nach Pfad geordnet.</summary>
    private static List<string> OhneKreuz(IReadOnlyDictionary<string, string> dateien)
        => dateien
           .Where(d => d.Value.Contains("class=\"epos-dialog-kopf", StringComparison.Ordinal))
           .Where(d => !d.Value.Contains("<Schliesskreuz", StringComparison.Ordinal))
           .Select(d => d.Key)
           .OrderBy(d => d, StringComparer.Ordinal)
           .ToList();

    // =====================================================================
    //  Fall 2 — keine betitelte Ueberlagerung ohne Kreuz
    // =====================================================================

    /// <summary>Ein Fund: Starttag mit Titel UND <c>Schliessbar="false"</c>.</summary>
    private readonly record struct Fund(string Datei, int Zeile, string Starttag);

    [Fact]
    public void Keine_betitelte_Ueberlagerung_unterdrueckt_ihr_Kreuz()
    {
        var dateien = new Dictionary<string, string>(Dateien("Dialoge", "*.razor"));
        foreach (KeyValuePair<string, string> d in Dateien("Seiten", "*.razor"))
            dateien[d.Key] = d.Value;

        List<Fund> funde = Funde(dateien, AUSNAHMEN);

        Assert.True(funde.Count == 0,
            "Diese Ueberlagerungen tragen einen Titel und unterdruecken trotzdem ihr Kreuz "
            + "(Hausregel \"Das Kreuz steht beim Titel\"): Die Ueberlagerung zeigt keins, und "
            + "die eingebettete Komponente zeigt wegen des Titels oben auch keins - der "
            + "Bereich hat dann GAR keins. Entweder Schliessbar streichen (Vorgabe true) oder "
            + "die Stelle mit Grund in AUSNAHMEN aufnehmen:\n" + Bericht(funde));
    }

    /// <summary>
    /// Die Gegenprobe: Ein betitelter Starttag mit <c>Schliessbar="false"</c> muss
    /// als Fund erscheinen — auch MEHRZEILIG und auch mit einem Lambda-Pfeil
    /// <c>=&gt;</c> im Attributwert (der frühere Parser hielt dessen <c>&gt;</c>
    /// für das Tagende). Ohne Titel, mit Kreuz oder in der Ausnahmeliste meldet
    /// dieselbe Methode nichts.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerungs_Wache_findet_den_betitelten_Bereich_ohne_Kreuz()
    {
        const string fund =
            "<Ueberlagerung Offen=\"@_offen\" Titel=\"@WarteTitel\"\n" +
            "               Geschlossen=\"() => _offen = false\" Schliessbar=\"false\">\n" +
            "    <KindInhalt><p>x</p></KindInhalt>\n" +
            "</Ueberlagerung>\n";
        const string ohneTitel =
            "<Ueberlagerung Offen=\"@_offen\" Titel=\"\" Schliessbar=\"false\">\n" +
            "    <KindInhalt><p>x</p></KindInhalt>\n" +
            "</Ueberlagerung>\n";
        const string mitKreuz =
            "<Ueberlagerung Offen=\"@_offen\" Titel=\"@WarteTitel\">\n" +
            "    <KindInhalt><p>x</p></KindInhalt>\n" +
            "</Ueberlagerung>\n";

        var dateien = new Dictionary<string, string>
        {
            ["EPOS.UI/Dialoge/X/ADialog.razor"] = fund,
            ["EPOS.UI/Dialoge/X/BDialog.razor"] = ohneTitel,
            ["EPOS.UI/Dialoge/X/CDialog.razor"] = mitKreuz
        };

        Fund treffer = Assert.Single(Funde(dateien, Array.Empty<Ausnahme>()));
        Assert.Equal("EPOS.UI/Dialoge/X/ADialog.razor", treffer.Datei);
        Assert.Equal(1, treffer.Zeile);

        // Und dieselbe Stelle, begruendet in der Ausnahmeliste, meldet nichts mehr.
        var ausnahme = new[]
        {
            new Ausnahme("EPOS.UI/Dialoge/X/ADialog.razor", "Offen=\"@_offen\"", "Probe")
        };
        Assert.Empty(Funde(dateien, ausnahme));
    }

    /// <summary>
    /// Eine Ausnahme wirkt nur in IHRER Datei und nur an IHRER Stelle: Dasselbe
    /// Fragment in einer anderen Datei deckt nichts zu.
    /// </summary>
    [Fact]
    public void Eine_Ausnahme_deckt_nur_ihre_eigene_Stelle()
    {
        const string fund =
            "<Ueberlagerung Offen=\"@_offen\" Titel=\"@WarteTitel\" Schliessbar=\"false\">\n" +
            "    <KindInhalt><p>x</p></KindInhalt>\n" +
            "</Ueberlagerung>\n";

        var dateien = new Dictionary<string, string>
        {
            ["EPOS.UI/Dialoge/X/ADialog.razor"] = fund,
            ["EPOS.UI/Dialoge/X/BDialog.razor"] = fund
        };
        var ausnahme = new[]
        {
            new Ausnahme("EPOS.UI/Dialoge/X/ADialog.razor", "Offen=\"@_offen\"", "Probe")
        };

        Fund treffer = Assert.Single(Funde(dateien, ausnahme));
        Assert.Equal("EPOS.UI/Dialoge/X/BDialog.razor", treffer.Datei);
    }

    private static List<Fund> Funde(IReadOnlyDictionary<string, string> dateien,
                                    IReadOnlyList<Ausnahme> ausnahmen)
    {
        var funde = new List<Fund>();

        foreach (KeyValuePair<string, string> datei in dateien.OrderBy(d => d.Key, StringComparer.Ordinal))
        {
            string s = datei.Value;

            foreach (Match m in Regex.Matches(s, @"<Ueberlagerung\b"))
            {
                int tagEnde = FindeTagEnde(s, m.Index);
                if (tagEnde < 0) continue;
                string starttag = s.Substring(m.Index, tagEnde - m.Index + 1);

                // Ein LEERES Titel="" ist kein Titel - dann traegt die eingebettete
                // Komponente ihren Kopf und damit ihr eigenes Kreuz.
                if (!Regex.IsMatch(starttag, "\\bTitel=\"[^\"]+\"")) continue;
                if (!Regex.IsMatch(starttag, "\\bSchliessbar=\"@?false\"")) continue;

                if (ausnahmen.Any(a => a.Datei == datei.Key
                                       && starttag.Contains(a.Fragment, StringComparison.Ordinal)))
                    continue;

                funde.Add(new Fund(datei.Key, 1 + Zeilenzahl(s, m.Index), Einzeilig(starttag)));
            }
        }

        return funde;
    }

    // =====================================================================
    //  Fall 3 — keine betitelte Ueberlagerung mit ZWEI Kreuzen
    // =====================================================================

    /// <summary>
    /// Ein Fund: eine betitelte, schließbare Überlagerung, in der eine Komponente
    /// steckt, die ihr eigenes <c>Schliesskreuz</c> zeichnet — ohne dass die
    /// Einbettung ihr den Kopf abschaltet.
    /// </summary>
    private readonly record struct Doppelfund(string Datei, int Zeile, string Komponente,
                                              string Einbettung);

    [Fact]
    public void Keine_betitelte_Ueberlagerung_zeigt_zwei_Kreuze()
    {
        List<Doppelfund> funde = Doppelfunde(Wirtsdateien(), Komponentendateien(), AUSNAHMEN_DOPPEL);

        Assert.True(funde.Count == 0,
            "In diesen betitelten Ueberlagerungen steht das Kreuz ZWEIMAL: Die "
            + "Ueberlagerung traegt Titel und ✕, und die eingebettete Komponente zeichnet "
            + "ihr eigenes <Schliesskreuz> dazu (Anwenderbefund 15.09.2026: \"Doppeltes "
            + "Kreuz duerfen nicht sein!\"). Die Einbettung braucht ein ausdrueckliches "
            + "TitelText=\"\" oder TitelAnzeigen=\"false\" - HINTER einem etwaigen "
            + "@attributes-Splat -, sonst gehoert die Stelle mit Grund in "
            + "AUSNAHMEN_DOPPEL:\n"
            + string.Join("\n", funde.Select(f =>
                "  " + f.Datei + ":" + f.Zeile + "  " + f.Komponente + "  " + f.Einbettung)));
    }

    /// <summary>
    /// Die Gegenprobe (Lehre W6‑B‑1): Eine ungeschützte Einbettung muss als Fund
    /// erscheinen; dieselbe Stelle mit <c>TitelText=""</c>, mit
    /// <c>TitelAnzeigen="false"</c>, hinter einem <c>@@attributes</c>-Splat, in einer
    /// Überlagerung OHNE Titel oder mit einer Komponente ohne eigenes Kreuz meldet
    /// nichts — und die Ausnahmeliste deckt genau ihre eine Stelle zu.
    /// </summary>
    [Fact]
    public void Die_Doppelkreuz_Wache_findet_die_ungeschuetzte_Einbettung()
    {
        const string mitKreuz = "<div class=\"epos-dialog-kopf\"><Schliesskreuz Geschlossen=\"Abbrechen\" /></div>\n";
        const string ohneKreuz = "<div class=\"epos-dialog\"><p>nur Inhalt</p></div>\n";

        string Wirt(string einbettung, string starttag = "<Ueberlagerung Offen=\"@_offen\" Titel=\"@Kopf\">")
            => starttag + "\n    <KindInhalt>\n        " + einbettung + "\n    </KindInhalt>\n</Ueberlagerung>\n";

        var komponenten = new Dictionary<string, string>
        {
            ["TiefDialog"] = mitKreuz,
            ["StillDialog"] = ohneKreuz
        };

        // (1) ungeschuetzt -> Fund
        var offen = new Dictionary<string, string>
        {
            ["EPOS.UI/Dialoge/X/AWirt.razor"] = Wirt("<TiefDialog Daten=\"@_d\" Geschlossen=\"Fertig\" />")
        };
        Doppelfund fund = Assert.Single(Doppelfunde(offen, komponenten, Array.Empty<Ausnahme>()));
        Assert.Equal("EPOS.UI/Dialoge/X/AWirt.razor", fund.Datei);
        Assert.Equal("TiefDialog", fund.Komponente);
        Assert.Equal(3, fund.Zeile);

        // (2) dieselbe Stelle, begruendet in der Ausnahmeliste -> nichts mehr
        Assert.Empty(Doppelfunde(offen, komponenten,
            new[] { new Ausnahme("EPOS.UI/Dialoge/X/AWirt.razor", "Daten=\"@_d\"", "Probe") }));

        // (3) die fuenf stillen Faelle
        var still = new Dictionary<string, string>
        {
            ["EPOS.UI/Dialoge/X/BWirt.razor"] = Wirt("<TiefDialog TitelText=\"\" Geschlossen=\"Fertig\" />"),
            ["EPOS.UI/Dialoge/X/CWirt.razor"] = Wirt("<TiefDialog TitelAnzeigen=\"false\" Geschlossen=\"Fertig\" />"),
            ["EPOS.UI/Dialoge/X/DWirt.razor"] = Wirt(
                "<EPOS.UI.Dialoge.X.TiefDialog @attributes=\"_gaben\" TitelText=\"\"\n"
                + "                                      Geschlossen=\"Fertig\" />"),
            ["EPOS.UI/Dialoge/X/EWirt.razor"] = Wirt("<TiefDialog Geschlossen=\"Fertig\" />",
                "<Ueberlagerung Offen=\"@_offen\" Titel=\"\">"),
            ["EPOS.UI/Dialoge/X/FWirt.razor"] = Wirt("<StillDialog Geschlossen=\"Fertig\" />")
        };
        Assert.Empty(Doppelfunde(still, komponenten, Array.Empty<Ausnahme>()));

        // (4) Schliessbar="false" gehoert Fall 2, nicht diesem hier
        var ohneUeberlagerungskreuz = new Dictionary<string, string>
        {
            ["EPOS.UI/Dialoge/X/GWirt.razor"] = Wirt("<TiefDialog Geschlossen=\"Fertig\" />",
                "<Ueberlagerung Offen=\"@_offen\" Titel=\"@Kopf\" Schliessbar=\"false\">")
        };
        Assert.Empty(Doppelfunde(ohneUeberlagerungskreuz, komponenten, Array.Empty<Ausnahme>()));
    }

    private static List<Doppelfund> Doppelfunde(IReadOnlyDictionary<string, string> wirte,
                                                IReadOnlyDictionary<string, string> komponenten,
                                                IReadOnlyList<Ausnahme> ausnahmen)
    {
        var funde = new List<Doppelfund>();

        foreach (KeyValuePair<string, string> datei in wirte.OrderBy(d => d.Key, StringComparer.Ordinal))
        {
            string s = datei.Value;

            foreach (Match m in Regex.Matches(s, @"<Ueberlagerung\b"))
            {
                int tagEnde = FindeTagEnde(s, m.Index);
                if (tagEnde < 0) continue;
                string starttag = s.Substring(m.Index, tagEnde - m.Index + 1);

                // Ohne Titel traegt die Komponente ihren Kopf (und damit ihr Kreuz)
                // zu Recht; ohne Kreuz oben ist es Fall 2, nicht dieser hier.
                if (!Regex.IsMatch(starttag, "\\bTitel=\"[^\"]+\"")) continue;
                if (Regex.IsMatch(starttag, "\\bSchliessbar=\"@?false\"")) continue;

                int blockEnde = s.IndexOf("</Ueberlagerung>", tagEnde, StringComparison.Ordinal);
                if (blockEnde < 0) blockEnde = s.Length;
                string block = s.Substring(tagEnde + 1, blockEnde - tagEnde - 1);

                foreach (Match km in Regex.Matches(block, @"<(?:[A-Za-z0-9_.]*\.)?([A-Z][A-Za-z0-9]*)\b"))
                {
                    string name = km.Groups[1].Value;
                    if (!komponenten.TryGetValue(name, out string? quelle)) continue;
                    if (!quelle.Contains("<Schliesskreuz", StringComparison.Ordinal)) continue;

                    int kindEnde = FindeTagEnde(block, km.Index);
                    if (kindEnde < 0) continue;
                    string einbettung = Einzeilig(block.Substring(km.Index, kindEnde - km.Index + 1));

                    if (Regex.IsMatch(einbettung, "TitelText=\"\"")) continue;
                    if (Regex.IsMatch(einbettung, "TitelAnzeigen=\"@?false\"")) continue;

                    if (ausnahmen.Any(a => a.Datei == datei.Key
                                           && einbettung.Contains(a.Fragment, StringComparison.Ordinal)))
                        continue;

                    funde.Add(new Doppelfund(datei.Key,
                                             1 + Zeilenzahl(s, tagEnde + 1 + km.Index),
                                             name, einbettung));
                }
            }
        }

        return funde;
    }

    // =====================================================================
    //  Kleinwerkzeug (dieselbe Bauweise wie UeberlagerungstitelTests)
    // =====================================================================

    /// <summary>
    /// Das Ende des Starttags — die erste spitze Klammer, die kein Lambda-Pfeil
    /// <c>=&gt;</c> ist. Ein Starttag darf ueber mehrere Zeilen laufen.
    /// </summary>
    private static int FindeTagEnde(string s, int start)
    {
        int i = start;
        while (true)
        {
            i = s.IndexOf('>', i);
            if (i < 0) return -1;
            if (i > 0 && s[i - 1] == '=') { i++; continue; }   // "=>"-Pfeil, kein Tagende
            return i;
        }
    }

    private static int Zeilenzahl(string s, int bisIndex)
    {
        int n = 0;
        for (int i = 0; i < bisIndex && i < s.Length; i++)
            if (s[i] == '\n') n++;
        return n;
    }

    /// <summary>Der Starttag als EINE Zeile — die Meldung soll les- und greppbar bleiben.</summary>
    private static string Einzeilig(string starttag)
        => Regex.Replace(starttag.Replace("\r", " ").Replace("\n", " "), @"\s+", " ").Trim();

    private static string Bericht(IEnumerable<Fund> funde)
        => string.Join("\n", funde.Select(f => "  " + f.Datei + ":" + f.Zeile + "  " + f.Starttag));

    // =====================================================================
    //  Der Weg zu den Quelldateien (dasselbe Verfahren wie UeberlagerungstitelTests)
    // =====================================================================

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    /// <summary>
    /// Alle Dateien eines Unterordners von <c>EPOS.UI</c>, Schlüssel ist der
    /// repo-relative Pfad mit Schrägstrichen — so steht er auch in
    /// <see cref="AUSNAHMEN"/> und in der Fehlermeldung.
    /// </summary>
    private static Dictionary<string, string> Dateien(string ordner, string muster)
    {
        string wurzel = Wurzel();
        string start = Path.Combine(wurzel, "EPOS.UI", ordner);
        var ergebnis = new Dictionary<string, string>();

        foreach (string p in Directory.GetFiles(start, muster, SearchOption.AllDirectories))
        {
            string relativ = Path.GetRelativePath(wurzel, p).Replace('\\', '/');
            ergebnis[relativ] = File.ReadAllText(p);
        }

        return ergebnis;
    }

    /// <summary>
    /// Die WIRTE: jede <c>.razor</c> unter <c>Dialoge/</c>, <c>Seiten/</c> und
    /// <c>Bausteine/</c> — dort steht jede <c>&lt;Ueberlagerung&gt;</c> des Hauses.
    /// Schlüssel ist der repo-relative Pfad mit Schrägstrichen.
    /// </summary>
    private static Dictionary<string, string> Wirtsdateien()
    {
        var ergebnis = new Dictionary<string, string>(Dateien("Dialoge", "*.razor"));
        foreach (string ordner in new[] { "Seiten", "Bausteine" })
            foreach (KeyValuePair<string, string> d in Dateien(ordner, "*.razor"))
                ergebnis[d.Key] = d.Value;
        return ergebnis;
    }

    /// <summary>
    /// Die KOMPONENTEN: jede <c>.razor</c> unter <c>EPOS.UI</c>, Schlüssel ist ihr
    /// blanker Dateiname ohne Endung — so heißt sie im Markup ihres Wirts (ein
    /// voll qualifizierter Aufruf trägt denselben letzten Namensteil).
    /// </summary>
    private static Dictionary<string, string> Komponentendateien()
    {
        string wurzel = Path.Combine(Wurzel(), "EPOS.UI");
        var ergebnis = new Dictionary<string, string>();
        foreach (string p in Directory.GetFiles(wurzel, "*.razor", SearchOption.AllDirectories))
            ergebnis[Path.GetFileNameWithoutExtension(p)] = File.ReadAllText(p);
        return ergebnis;
    }

    /// <summary>Die Wache darf nicht ins Leere greifen.</summary>
    [Fact]
    public void Die_Wache_liest_tatsaechlich_Dialoge_und_Seiten()
    {
        Assert.NotEmpty(Dateien("Dialoge", "*Dialog.razor"));
        Assert.NotEmpty(Dateien("Dialoge", "*.razor"));
        Assert.NotEmpty(Dateien("Seiten", "*.razor"));
        Assert.NotEmpty(Dateien("Bausteine", "*.razor"));
        Assert.NotEmpty(Wirtsdateien());

        // Die Komponentenseite muss die Traeger des Kreuzes wirklich kennen.
        Dictionary<string, string> komponenten = Komponentendateien();
        Assert.True(komponenten.ContainsKey("Schliesskreuz"));
        Assert.Contains("<Schliesskreuz", komponenten["NamensDialog"], StringComparison.Ordinal);
    }

    /// <summary>
    /// Jede Ausnahme muss GREIFEN — die aus <see cref="AUSNAHMEN"/> (Fall 2) wie die
    /// aus <see cref="AUSNAHMEN_DOPPEL"/> (Fall 3): Die Datei gibt es, und das
    /// Fragment trifft dort genau EINE Stelle, die ohne die Ausnahme ein Fund wäre.
    /// Eine Ausnahme, die ins Leere zeigt, ist entweder erledigt (dann gehört sie
    /// gestrichen) oder falsch geschrieben (dann deckt sie nichts).
    /// </summary>
    [Fact]
    public void Jede_Ausnahme_trifft_genau_eine_Stelle()
    {
        var dateien = new Dictionary<string, string>(Dateien("Dialoge", "*.razor"));
        foreach (KeyValuePair<string, string> d in Dateien("Seiten", "*.razor"))
            dateien[d.Key] = d.Value;

        var leerlauf = new List<string>();

        foreach (Ausnahme a in AUSNAHMEN)
        {
            int treffer = dateien.TryGetValue(a.Datei, out string? inhalt)
                ? Funde(new Dictionary<string, string> { [a.Datei] = inhalt },
                        Array.Empty<Ausnahme>())
                      .Count(f => f.Starttag.Contains(a.Fragment, StringComparison.Ordinal))
                : -1;

            if (treffer != 1)
                leerlauf.Add("  AUSNAHMEN        " + a.Datei + "  " + a.Fragment
                             + (treffer < 0 ? "  (Datei fehlt)" : "  (Treffer: " + treffer + ")"));
        }

        Dictionary<string, string> wirte = Wirtsdateien();
        Dictionary<string, string> komponenten = Komponentendateien();

        foreach (Ausnahme a in AUSNAHMEN_DOPPEL)
        {
            int treffer = wirte.TryGetValue(a.Datei, out string? inhalt)
                ? Doppelfunde(new Dictionary<string, string> { [a.Datei] = inhalt },
                              komponenten, Array.Empty<Ausnahme>())
                      .Count(f => f.Einbettung.Contains(a.Fragment, StringComparison.Ordinal))
                : -1;

            if (treffer != 1)
                leerlauf.Add("  AUSNAHMEN_DOPPEL " + a.Datei + "  " + a.Fragment
                             + (treffer < 0 ? "  (Datei fehlt)" : "  (Treffer: " + treffer + ")"));
        }

        Assert.True(leerlauf.Count == 0,
            "Diese Ausnahmen greifen nicht mehr genau einmal - streichen oder berichtigen:\n"
            + string.Join("\n", leerlauf));
    }
}

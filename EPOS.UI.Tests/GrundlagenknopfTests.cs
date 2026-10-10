using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Der WÄCHTER über die Knöpfe „Grundlagen" (Konzept Technikdokumentation, Abschnitt 7;
/// Entscheide TD‑E1 und TD‑E2) — gebaut wie <see cref="BerechnungsknopfTests"/>.
///
/// <para><b>Worum es geht.</b> Jeder Technikdialog trägt links neben seinem Knopf
/// <c>&lt;Formname&gt;.Berechnung</c> einen Knopf <c>&lt;Formname&gt;.Grundlagen</c>, dessen
/// Ziel in <c>help_mapping.txt</c> als SEITENPFAD der Wiki-Rubrik Grundlagen steht
/// (<c>/wiki/Grundlagen/Kessel_und_Spitzenlast</c>). Drei Abschnitte ohne eigene
/// Rechenwegseite (Wechselrichterwahl, Kühlung des Gebäudes, Kühlbetrieb der Wärmepumpe)
/// tragen ein Paar aus Anwendungsseite und Grundlagenseite. Alle Hälften altern für sich:
/// eine Zeile ohne Knopf, ein Knopf ohne Zeile, ein Ziel, das der Windows-Katalog nicht
/// kennt (dann bliebe der Knopf dort stumm), ein Technikdialog ohne Knopf.</para>
///
/// <para><b>Warum der Fall den Quelltext liest.</b> Zuordnungsdatei und Startbestand liegen
/// im WinForms-Projekt (<c>net10.0-windows</c>); ein Test, der es referenziert, liefe
/// weder auf dem ubuntu-Läufer noch auf macOS — derselbe Weg wie im Vorbild.</para>
/// </summary>
public sealed class GrundlagenknopfTests : EposBunitContext
{
    /// <summary>Der Anfang jedes Grundlagen-Ziels.</summary>
    private const string PFAD = "/wiki/Grundlagen/";

    /// <summary>Das Schlüsselmuster: <c>Form_Irgendwas.Grundlagen</c>.</summary>
    private static readonly Regex Schluesselmuster =
        new(@"\bForm_[A-Za-z0-9_]+\.Grundlagen\b", RegexOptions.Compiled);

    /// <summary>Eine Zuordnungszeile <c>Schlüssel = Ziel</c>.</summary>
    private static readonly Regex Zuordnungszeile =
        new(@"^\s*([A-Za-z0-9_.]+)\s*=\s*(\S.*?)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Die zehn Grundlagenseiten, die ein Knopf ansprechen darf — in der Schreibweise des
    /// Ziels (Unterstrich statt Leerzeichen). Genau diese trägt der Startbestand.
    /// </summary>
    private static readonly string[] Grundlagenseiten =
    {
        "Wärmepumpe", "Wärmequelle_Erdreich", "Kessel_und_Spitzenlast", "BHKW", "Solarkollektoren",
        "Pufferspeicher", "Photovoltaik", "Wechselrichter", "Stromspeicher", "Kühlung"
    };

    /// <summary>
    /// Rechenwegseite eines Berechnungsknopfs → Grundlagenseite ihrer Technik. Ein Anker steht
    /// nur dort im Schlüssel, wo er die Technik wechselt: Der Wechselrichter ist ein Abschnitt
    /// der Rechenwegseite Photovoltaik, hat aber eine eigene Grundlagenseite.
    /// </summary>
    private static readonly Dictionary<string, string> GrundlagenJeRechenweg = new(StringComparer.Ordinal)
    {
        { "Heizkessel", "Kessel_und_Spitzenlast" },
        { "BHKW", "BHKW" },
        { "Wärmepumpe", "Wärmepumpe" },
        { "Pufferspeicher", "Pufferspeicher" },
        { "Solarthermie", "Solarkollektoren" },
        { "Photovoltaik", "Photovoltaik" },
        { "Photovoltaik#wechselrichter", "Wechselrichter" },
        { "Stromspeicher", "Stromspeicher" },
        { "Wärmequelle Erdreich", "Wärmequelle_Erdreich" },
        { "Kühlung", "Kühlung" }
    };

    /// <summary>Rechenwegseiten OHNE Technik — ihre Dialoge bekommen keinen Grundlagenknopf.</summary>
    private static readonly string[] RechenwegeOhneTechnik =
        { "Simulationsablauf", "Wärmebedarf", "Brauchwasser", "Prozesswärme", "Strombedarf" };

    /// <summary>Die drei Abschnitte mit Knopfpaar: Name → (Anwendungsseite, Grundlagenseite).</summary>
    private static readonly Dictionary<string, (string Seite, string Grundlagen)> Abschnittspaare =
        new(StringComparer.Ordinal)
        {
            { "Form_PV_Wechselrichter", ("Wechselrichter", "Wechselrichter") },
            { "Form_Gebaeude_Kuehlung", ("Kühlung", "Kühlung") },
            { "Form_WP_Kuehlung", ("Kälteerzeugung", "Kühlung") }
        };

    public GrundlagenknopfTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Die zwei Richtungen
    // =====================================================================

    /// <summary>Jeder <c>*.Grundlagen</c>-Schlüssel der Zuordnungsdatei wird auch benutzt.</summary>
    [Fact]
    public void Jeder_Grundlagenschluessel_hat_einen_Knopf()
    {
        IReadOnlyDictionary<string, string> zuordnung = Grundlagenzuordnungen();
        IReadOnlyDictionary<string, List<string>> imQuelltext = SchluesselImQuelltext();

        Assert.True(zuordnung.Count >= 1,
            "help_mapping.txt führt keine einzige Zeile '<Form>.Grundlagen = /wiki/Grundlagen/<Titel>'.");

        var funde = zuordnung.Keys.Where(k => !imQuelltext.ContainsKey(k)).ToList();
        Assert.True(funde.Count == 0,
            "Diese Schlüssel stehen in help_mapping.txt, aber in keinem Dialog und keinem Profil:\n" +
            string.Join("\n", funde));
    }

    /// <summary>Umgekehrt: Jeder Schlüssel im Quelltext hat ein Ziel — sonst bliebe der Knopf stumm.</summary>
    [Fact]
    public void Jeder_Knopf_hat_eine_Zeile_in_der_Zuordnung()
    {
        IReadOnlyDictionary<string, string> zuordnung = Grundlagenzuordnungen();

        var funde = SchluesselImQuelltext()
            .Where(p => !zuordnung.ContainsKey(p.Key))
            .Select(p => p.Key + "  (" + string.Join(", ", p.Value) + ")")
            .ToList();

        Assert.True(funde.Count == 0,
            "Diese Schlüssel stehen im Quelltext, aber nicht in help_mapping.txt:\n" + string.Join("\n", funde));
    }

    // =====================================================================
    //  Das Ziel
    // =====================================================================

    /// <summary>
    /// Jedes Ziel ist ein Seitenpfad in die Rubrik Grundlagen, geschrieben wie MediaWiki den
    /// Titel in die Adresse schreibt (Unterstrich statt Leerzeichen), und nennt eine der zehn
    /// Grundlagenseiten. Ein Kurzname („Wärmepumpe") träfe die gleichnamige Seite der Rubrik
    /// „Programm Dokumentation" — genau das soll der Pfad verhindern.
    /// </summary>
    [Fact]
    public void Jedes_Ziel_zeigt_als_Seitenpfad_in_die_Rubrik_Grundlagen()
    {
        var falsch = new List<string>();

        foreach (var paar in Grundlagenzuordnungen())
        {
            string ziel = paar.Value;
            if (!ziel.StartsWith(PFAD, StringComparison.Ordinal))
                falsch.Add(paar.Key + " → '" + ziel + "' beginnt nicht mit " + PFAD);
            else if (ziel.Contains(' '))
                falsch.Add(paar.Key + " → '" + ziel + "' trägt ein Leerzeichen statt Unterstrich");
            else if (!Grundlagenseiten.Contains(Titel(ziel), StringComparer.Ordinal))
                falsch.Add(paar.Key + " → '" + ziel + "' nennt keine der zehn Grundlagenseiten");
        }

        Assert.True(falsch.Count == 0, "Diese Ziele stimmen nicht:\n  " + string.Join("\n  ", falsch));
    }

    /// <summary>
    /// Jedes Ziel steht im mitgelieferten Startbestand — ohne Eintrag dort kennt der
    /// Windows-Katalog die Seite nicht (er lädt die Rubrik Grundlagen nicht ab), und der Knopf
    /// bliebe unter Windows stumm. Der Eintrag trägt die URL-kodierte Adresse und keinen Slug.
    /// </summary>
    [Fact]
    public void Jedes_Ziel_steht_im_Startbestand()
    {
        using JsonDocument dok = JsonDocument.Parse(File.ReadAllText(Pfad(
            "WindowsFormsApplication1", "Allgemein", "Hilfe", "help_cache.json")).TrimStart('﻿'));

        var falsch = new List<string>();
        foreach (string titel in Grundlagenzuordnungen().Values.Select(Titel).Distinct(StringComparer.Ordinal))
        {
            string schluessel = PFAD.ToLowerInvariant() + titel.ToLowerInvariant() + "/";
            if (!dok.RootElement.TryGetProperty(schluessel, out JsonElement eintrag))
            {
                falsch.Add(schluessel + " fehlt");
                continue;
            }

            string url = eintrag.GetProperty("Url").GetString() ?? "";
            if (url != "https://wiki.epos-plan.de" + PFAD + Uri.EscapeDataString(titel))
                falsch.Add(schluessel + ": Adresse " + url);

            if (eintrag.TryGetProperty("Slug", out JsonElement slug) && (slug.GetString() ?? "").Length > 0)
                falsch.Add(schluessel + ": trägt den Slug '" + slug.GetString() + "' — er wäre mehrdeutig");
        }

        Assert.True(falsch.Count == 0, "Der Startbestand passt nicht zu den Zielen:\n  " + string.Join("\n  ", falsch));
    }

    // =====================================================================
    //  Wo ein Knopf stehen muss — und wo nicht
    // =====================================================================

    /// <summary>
    /// Neben JEDEM Berechnungsknopf einer Technik steht der Grundlagenknopf derselben Maske und
    /// führt auf die Grundlagenseite dieser Technik; die Dialoge des Bedarfs und der Simulation
    /// bekommen keinen („Nur die Technikdialoge"). Eine neue Rechenwegseite fällt hier auf,
    /// bis sie in einer der beiden Tafeln steht.
    /// </summary>
    [Fact]
    public void Neben_jedem_Berechnungsknopf_einer_Technik_steht_der_Grundlagenknopf()
    {
        IReadOnlyDictionary<string, string> grundlagen = Grundlagenzuordnungen();
        var falsch = new List<string>();
        int technik = 0;

        foreach (var paar in Zuordnungen().Where(p => p.Key.EndsWith(".Berechnung", StringComparison.Ordinal)))
        {
            string form = paar.Key.Substring(0, paar.Key.Length - ".Berechnung".Length);
            string rechenweg = paar.Value.StartsWith("Berechnung/", StringComparison.Ordinal)
                ? paar.Value.Substring("Berechnung/".Length)
                : paar.Value;
            string ohneAnker = rechenweg.Split('#')[0];

            string? seite = GrundlagenJeRechenweg.TryGetValue(rechenweg, out string? mitAnker) ? mitAnker
                          : GrundlagenJeRechenweg.TryGetValue(ohneAnker, out string? ohne) ? ohne
                          : null;

            grundlagen.TryGetValue(form + ".Grundlagen", out string? ziel);

            if (seite is not null)
            {
                technik++;
                if (ziel != PFAD + seite)
                    falsch.Add(form + ": erwartet " + form + ".Grundlagen = " + PFAD + seite + ", gefunden '" + ziel + "'");
            }
            else if (RechenwegeOhneTechnik.Contains(ohneAnker, StringComparer.Ordinal))
            {
                if (ziel is not null)
                    falsch.Add(form + ": zeigt den Rechenweg '" + ohneAnker + "' ohne Technik und trägt trotzdem einen Grundlagenknopf");
            }
            else
            {
                falsch.Add(form + ": Rechenwegseite '" + rechenweg + "' steht in keiner der beiden Tafeln dieses Wächters");
            }
        }

        Assert.True(technik >= 19, "Nur " + technik + " Berechnungsknöpfe einer Technik gefunden — Leser kaputt?");
        Assert.True(falsch.Count == 0, "Grundlagenknöpfe fehlen oder zeigen falsch:\n  " + string.Join("\n  ", falsch));
    }

    /// <summary>
    /// Umgekehrt: Ein Grundlagenknopf hat einen Nachbarn — den Berechnungsknopf seiner Maske
    /// oder, bei den drei Abschnitten ohne Rechenwegseite, den Knopf der Anwendungsseite.
    /// </summary>
    [Fact]
    public void Jeder_Grundlagenknopf_hat_seinen_Nachbarn()
    {
        IReadOnlyDictionary<string, string> alle = Zuordnungen();

        var ohneNachbar = Grundlagenzuordnungen().Keys
            .Select(k => k.Substring(0, k.Length - ".Grundlagen".Length))
            .Where(form => !alle.ContainsKey(form + ".Berechnung") && !Abschnittspaare.ContainsKey(form))
            .ToList();

        Assert.True(ohneNachbar.Count == 0,
            "Diese Grundlagenknöpfe stehen weder neben einem Berechnungsknopf noch in einem der drei " +
            "Abschnitte mit Knopfpaar:\n" + string.Join("\n", ohneNachbar));
    }

    /// <summary>
    /// Die drei Abschnitte mit Knopfpaar: Der Knopf der Anwendungsseite führt in die Rubrik
    /// „Programm Dokumentation" auf die Seite der Technik (ein Anker ist erlaubt), der
    /// Grundlagenknopf auf deren Grundlagenseite.
    /// </summary>
    [Fact]
    public void Die_Abschnittspaare_fuehren_auf_Anwendungs_und_Grundlagenseite()
    {
        IReadOnlyDictionary<string, string> alle = Zuordnungen();

        foreach (var paar in Abschnittspaare)
        {
            Assert.True(alle.TryGetValue(paar.Key + ".btn_Help", out string? anwendung),
                        paar.Key + ".btn_Help fehlt in help_mapping.txt.");
            Assert.Equal(paar.Value.Seite, anwendung!.Split('#')[0]);

            Assert.True(alle.TryGetValue(paar.Key + ".Grundlagen", out string? grundlagen),
                        paar.Key + ".Grundlagen fehlt in help_mapping.txt.");
            Assert.Equal(PFAD + paar.Value.Grundlagen, grundlagen);
        }
    }

    /// <summary>
    /// Im Markup steht der Grundlagenknopf in DERSELBEN Hülle wie sein Nachbar — in
    /// <c>.epos-berechnungshilfe</c> (bzw. der Herleitungszeile, in der der Rechenwegknopf der
    /// Wärmequelle Erdreich steht) und ohne schließendes Element dazwischen. Die Hülle ist
    /// keine Knopfleiste: Mehrere Masken zählen deren Knöpfe (Lehre aus H13b § 4).
    /// </summary>
    [Fact]
    public void Der_Grundlagenknopf_steht_in_der_Huelle_seines_Nachbarn()
    {
        var falsch = new List<string>();
        int gesehen = 0;

        foreach (string pfad in Quelldateien().Where(p => p.EndsWith(".razor", StringComparison.Ordinal)))
        {
            string text = File.ReadAllText(pfad);
            string name = Path.GetFileName(pfad);

            foreach (Match m in Regex.Matches(text,
                         @"<InfoKnopf\s+Schluessel=""(?<s>Form_[A-Za-z0-9_]+\.Grundlagen|@Profil\.GrundlagenSchluessel)"""))
            {
                gesehen++;
                int knopf = m.Index;

                int huelle = Math.Max(text.LastIndexOf("class=\"epos-berechnungshilfe\"", knopf, StringComparison.Ordinal),
                                      text.LastIndexOf("class=\"epos-herleitung\"", knopf, StringComparison.Ordinal));
                if (huelle < 0 || Schliesst(text, huelle, knopf))
                {
                    falsch.Add(name + ": " + m.Groups["s"].Value + " steht in keiner Hülle .epos-berechnungshilfe");
                    continue;
                }

                // Der Nachbar: der naechste InfoKnopf dahinter (Berechnung) oder der davor
                // (Anwendungsseite eines Abschnitts) - in derselben Huelle.
                int danach = text.IndexOf("<InfoKnopf", knopf + 1, StringComparison.Ordinal);
                int davor = text.LastIndexOf("<InfoKnopf", knopf - 1, StringComparison.Ordinal);
                bool nachbarDanach = danach > 0 && !Schliesst(text, knopf, danach) &&
                                     Regex.IsMatch(text.Substring(danach, Math.Min(120, text.Length - danach)),
                                                   @"Schluessel=""(?:[^""]*Berechnung[^""]*)""");
                bool nachbarDavor = davor > huelle && !Schliesst(text, davor, knopf) &&
                                    Regex.IsMatch(text.Substring(davor, Math.Min(120, text.Length - davor)),
                                                  @"Schluessel=""Form_[A-Za-z0-9_]+\.btn_Help""");

                if (!nachbarDanach && !nachbarDavor)
                    falsch.Add(name + ": " + m.Groups["s"].Value + " steht ohne Berechnungs- oder Anwendungsknopf in seiner Hülle");
            }
        }

        Assert.True(gesehen >= 15, "Nur " + gesehen + " Grundlagenknöpfe im Markup gefunden — Leser kaputt?");
        Assert.True(falsch.Count == 0, string.Join("\n", falsch));
    }

    // =====================================================================
    //  Gezeichnet: der Knopf ist im Dialog wirklich da
    // =====================================================================

    /// <summary>Die Projektdialoge und die Schlüssel, die ihr gezeichnetes Markup tragen muss.</summary>
    public static TheoryData<string, string> Wirte => new()
    {
        { "HeizkesselDialog",        "Form_Heizkessel.Grundlagen" },
        { "BhkwDialog",              "Form_BHKWEing.Grundlagen" },
        { "WaermepumpeAnlageDialog", "Form_WP.Grundlagen" },
        { "BetriebsmodusDialog",     "Form_Betriebsmodus.Grundlagen" },
        { "PufferspeicherDialog",    "Form_PufferSp.Grundlagen" },
        { "PufferSpProjektDialog",   "Form_PufferSp_Projekt.Grundlagen" },
        { "SolarkollektorenDialog",  "Form_SolarKollektoren.Grundlagen" },
        { "SolarganglinieDialog",    "Form_Solarganglinie.Grundlagen" },
        { "PhotovoltaikDialog",      "Form_PV.Grundlagen" },
        { "PhotovoltaikDialog",      "Form_PV_Wechselrichter.Grundlagen" },
        { "PhotovoltaikDialog",      "Form_PV_Wechselrichter.btn_Help" },
        { "StromspeicherDialog",     "Form_Stromspeicher.Grundlagen" },
        { "QuelleErdreichDialog",    "Form_QuelleErdreich.Grundlagen" }
    };

    /// <summary>
    /// Der Knopf ist im gezeichneten Dialog wirklich da — gezeichnet auf dem Weg der
    /// Windows-Hülle (Wörterbuch → Parametersatz), mit dem kleinstmöglichen Satz.
    /// </summary>
    [Theory]
    [MemberData(nameof(Wirte))]
    public void Jeder_Dialog_traegt_seinen_Grundlagenknopf(string komponente, string schluessel)
    {
        var gezeichnet = AusHuelle(Komponente(komponente), Gaben(komponente));
        // Katalogauswahl V1 (KA-E-11): Die Hilfe zum Wechselrichter steht in der Überlagerung
        // „Stränge und Wechselrichter…" des Photovoltaikdialogs - sie wird dafür geöffnet.
        if (komponente == "PhotovoltaikDialog" && schluessel.StartsWith("Form_PV_Wechselrichter", StringComparison.Ordinal))
            gezeichnet.Find(".epos-knopf--straenge").Click();

        string[] imDialog = gezeichnet.FindComponents<InfoKnopf>().Select(k => k.Instance.Schluessel).ToArray();

        Assert.Contains(schluessel, imDialog);
    }

    /// <summary>
    /// Die acht Katalogeditoren: Jedes Profil mit Rechenwegknopf trägt den Grundlagenschlüssel
    /// derselben Maske. (Welcher Schlüssel gilt, sagt das Profil — wie beim Rechenweg.)
    /// </summary>
    [Fact]
    public void Jedes_Katalogprofil_mit_Rechenweg_traegt_den_Grundlagenschluessel()
    {
        var paare = Enum.GetValues<KatalogBrowserArt>()
            .Select(a => KatalogBrowserProfil.Finde(a))
            .Select(p => (p.BerechnungsSchluessel, p.GrundlagenSchluessel))
            .Concat(Enum.GetValues<ModulKatalogArt>()
                .Select(a => ModulKatalogProfil.Finde(a))
                .Select(p => (p.BerechnungsSchluessel, p.GrundlagenSchluessel)))
            .Where(p => !string.IsNullOrEmpty(p.BerechnungsSchluessel))
            .ToList();

        Assert.Equal(7, paare.Count);
        foreach (var (berechnung, grundlagen) in paare)
            Assert.Equal(berechnung.Replace(".Berechnung", ".Grundlagen"), grundlagen);

        Assert.Contains("Form_WP_Stamm.Grundlagen",
                        File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Waermepumpe", "WaermepumpeStammDialog.razor")));
    }

    // =====================================================================
    //  Gegenproben
    // =====================================================================

    /// <summary><b>Gegenprobe:</b> Der Leser findet das Muster — und nur das.</summary>
    [Fact]
    public void Der_Leser_erkennt_das_Muster()
    {
        Assert.Matches(Schluesselmuster, "Schluessel=\"Form_PV.Grundlagen\"");
        Assert.Matches(Schluesselmuster, "GrundlagenSchluessel = \"Form_AdminPV.Grundlagen\",");
        Assert.DoesNotMatch(Schluesselmuster, "Schluessel=\"Form_PV.Berechnung\"");
        Assert.DoesNotMatch(Schluesselmuster, "// die Rubrik Grundlagen, siehe Konzept");

        Assert.Equal("Kessel_und_Spitzenlast", Titel("/wiki/Grundlagen/Kessel_und_Spitzenlast#kennzahlen"));
        const string zu = "<div class=\"x\"><a/></div><b/>";
        const string offen = "<div class=\"x\"><a/><b/>";
        Assert.True(Schliesst(zu, 0, zu.Length));
        Assert.False(Schliesst(offen, 0, offen.Length));
        Assert.True(Schliesst("<p class=\"epos-herleitung\"><i/></p><i/>", 0, 38));
    }

    /// <summary><b>Gegenprobe zum Bestand:</b> Der Wächter liest wirklich Zeilen und Dateien.</summary>
    [Fact]
    public void Der_Waechter_sieht_den_Bestand()
    {
        Assert.True(Grundlagenzuordnungen().Count >= 22,
                    "Nur " + Grundlagenzuordnungen().Count + " Grundlagenzeilen in help_mapping.txt.");
        Assert.True(Quelldateien().Length > 100, "Nur " + Quelldateien().Length + " Quelldateien gefunden.");
        Assert.True(SchluesselImQuelltext().Count >= 22, "Nur " + SchluesselImQuelltext().Count + " Grundlagenschlüssel im Quelltext.");
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Der Seitentitel eines Grundlagen-Ziels ohne Pfad und Anker.</summary>
    private static string Titel(string ziel)
    {
        string rest = ziel.StartsWith(PFAD, StringComparison.Ordinal) ? ziel.Substring(PFAD.Length) : ziel;
        return rest.Split('#')[0].Trim();
    }

    /// <summary>Schließt zwischen <paramref name="von"/> und <paramref name="bis"/> ein Element der Hülle?</summary>
    private static bool Schliesst(string text, int von, int bis)
    {
        string zwischen = text.Substring(von, bis - von);
        return zwischen.Contains("</div>", StringComparison.Ordinal) || zwischen.Contains("</p>", StringComparison.Ordinal);
    }

    /// <summary>Alle Zuordnungszeilen: Schlüssel → Ziel; eine spätere Zeile schlägt eine frühere.</summary>
    private static IReadOnlyDictionary<string, string> Zuordnungen()
    {
        var tabelle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rohzeile in File.ReadAllLines(Pfad("WindowsFormsApplication1", "Allgemein", "Hilfe", "help_mapping.txt"),
                                                      System.Text.Encoding.UTF8))
        {
            string zeile = rohzeile.Trim('﻿', ' ', '\t');
            if (zeile.Length == 0 || zeile.StartsWith("#", StringComparison.Ordinal)) continue;

            Match m = Zuordnungszeile.Match(zeile);
            if (m.Success) tabelle[m.Groups[1].Value] = m.Groups[2].Value;
        }

        return tabelle;
    }

    /// <summary>Die Zeilen <c>&lt;Form&gt;.Grundlagen = Ziel</c>.</summary>
    private static IReadOnlyDictionary<string, string> Grundlagenzuordnungen() =>
        Zuordnungen().Where(p => p.Key.EndsWith(".Grundlagen", StringComparison.Ordinal))
                     .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Jeder <c>*.Grundlagen</c>-Schlüssel des Quelltexts mit den Dateien, in denen er steht.</summary>
    private static IReadOnlyDictionary<string, List<string>> SchluesselImQuelltext()
    {
        var gefunden = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (string pfad in Quelldateien())
        {
            string name = Path.GetFileName(pfad);
            foreach (Match treffer in Schluesselmuster.Matches(File.ReadAllText(pfad)))
            {
                if (!gefunden.TryGetValue(treffer.Value, out List<string>? dateien))
                    gefunden[treffer.Value] = dateien = new List<string>();
                if (!dateien.Contains(name, StringComparer.Ordinal)) dateien.Add(name);
            }
        }

        return gefunden;
    }

    /// <summary>
    /// Wo ein Grundlagenschlüssel stehen darf: die Razor-Komponenten von <c>EPOS.UI</c>, die
    /// Hüllen unter <c>WindowsFormsApplication1/Views</c> und die Katalogprofile im Kern —
    /// dieselben Orte wie beim Berechnungsschlüssel.
    /// </summary>
    private static string[] Quelldateien()
    {
        IEnumerable<string> ui = Directory.EnumerateFiles(Pfad("EPOS.UI"), "*.razor", SearchOption.AllDirectories);
        IEnumerable<string> huellen = Directory.EnumerateFiles(Pfad("WindowsFormsApplication1", "Views"), "*.cs",
                                                               SearchOption.AllDirectories);
        IEnumerable<string> profile = Directory.EnumerateFiles(Pfad("EPOS.Kern", "Allgemein", "Katalog"), "*.cs",
                                                               SearchOption.AllDirectories);

        string bin = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;
        string obj = Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar;

        return ui.Concat(huellen).Concat(profile)
                 .Where(p => !p.Contains(bin, StringComparison.Ordinal) && !p.Contains(obj, StringComparison.Ordinal))
                 .OrderBy(p => p, StringComparer.Ordinal)
                 .ToArray();
    }

    /// <summary>Ein Pfad unter der Wurzel des Arbeitsbaums.</summary>
    private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

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

    /// <summary>
    /// Der kleinstmögliche Parametersatz. Zwei Dialoge zeigen ihren Anlagenabschnitt nur bei
    /// GEWÄHLTER Projektzeile — sie bekommen eine (Muster <c>BerechnungshilfeTests</c>).
    /// </summary>
    private static Dictionary<string, object> Gaben(string komponente)
    {
        var gaben = new Dictionary<string, object>(StringComparer.Ordinal);

        if (komponente is "PhotovoltaikDialog" or "SolarkollektorenDialog")
            gaben["Zeilen"] = new List<ErzeugerZeile> { new() { Schluessel = 1, Bezeichner = "Probe", GeraetId = 1 } };

        return gaben;
    }

    private IRenderedComponent<DynamicComponent> AusHuelle(Type komponente, IDictionary<string, object> gaben)
    {
        return Render<DynamicComponent>(builder =>
        {
            builder.OpenComponent<DynamicComponent>(0);
            builder.AddComponentParameter(1, nameof(DynamicComponent.Type), komponente);
            builder.AddComponentParameter(2, nameof(DynamicComponent.Parameters), (IDictionary<string, object?>)gaben!);
            builder.CloseComponent();
        });
    }

    private static Type Komponente(string name)
    {
        Type? t = typeof(InfoKnopf).Assembly.GetTypes().FirstOrDefault(x => x.Name == name);
        Assert.True(t is not null, "Die Komponente " + name + " gibt es in EPOS.UI nicht.");
        return t!;
    }
}

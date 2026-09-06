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
/// <para><b>Was hier gehalten wird — drei Zusagen.</b></para>
/// <list type="number">
///   <item><description>Jede Seite der Rubrik trägt ihren Kopfblock und die sechs
///     Abschnitte der Bauform. Eine Seite ohne „Grenzen und Annahmen" wäre eine
///     Erklärung, die verschweigt, was der Rechenkern NICHT tut.</description></item>
///   <item><description>Jeder Schlüssel <c>*.Berechnung</c> der Zuordnungsdatei
///     zeigt auf eine Seite, die es als Datei WIRKLICH gibt — sonst öffnet der
///     Knopf beim Anwender ins Leere.</description></item>
///   <item><description>Jeder dieser Schlüssel steht in GENAU EINEM Razor-Dialog.
///     Keiner ist tote Zuordnung, keiner hängt an zwei Masken.</description></item>
/// </list>
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
    /// </summary>
    private static readonly string[] Pflichtabschnitte =
    {
        "== Was berechnet wird ==",
        "== Eingangsgrößen ==",
        "== Rechenweg ==",
        "== Grenzen und Annahmen ==",
        "== Ergebnisse und wo sie stehen ==",
        "== Bezüge =="
    };

    /// <summary>
    /// Die Seiten dieses Teils (Erzeuger und Speicher). Sie stehen hier
    /// AUSDRÜCKLICH und nicht als Verzeichnisinhalt: Eine gelöschte Datei soll
    /// rot ausfallen, nicht still durchgehen.
    /// </summary>
    private static readonly string[] SeitenTeilB =
    {
        "Heizkessel", "BHKW", "Wärmepumpe", "Pufferspeicher",
        "Solarthermie", "Photovoltaik", "Stromspeicher"
    };

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
    /// Die sechs Abschnitte der Bauform stehen auf jeder Seite, und zwar in der
    /// vorgegebenen REIHENFOLGE. Zusätzliche Abschnitte sind erlaubt — die
    /// Photovoltaikseite führt einen eigenen über den Wechselrichter.
    /// </summary>
    [Theory]
    [MemberData(nameof(Seitennamen))]
    public void Jede_Seite_traegt_die_sechs_Abschnitte_der_Bauform(string seite)
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
    //  2. Die Zuordnungen
    // =====================================================================

    /// <summary>
    /// Jeder Schlüssel des Abschnitts „Teil B" zeigt auf eine Seite, die es als
    /// Datei gibt — und der Leser findet überhaupt Zeilen.
    /// </summary>
    [Fact]
    public void Jede_Zuordnung_zeigt_auf_eine_vorhandene_Seite()
    {
        var zuordnungen = ZuordnungenTeilB();

        Assert.True(zuordnungen.Count >= 10,
                    "Nur " + zuordnungen.Count + " Zuordnungen gefunden — der Leser ist kaputt " +
                    "oder der Abschnitt # Teil B (Erzeuger und Speicher) fehlt.");

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

        foreach (var z in ZuordnungenTeilB())
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

    /// <summary>Die Seitennamen dieses Teils als Theoriedaten.</summary>
    public static TheoryData<string> Seitennamen
    {
        get
        {
            var daten = new TheoryData<string>();
            foreach (string s in SeitenTeilB) daten.Add(s);
            return daten;
        }
    }

    private static string Seitendatei(string seite)
        => Path.Combine(new[] { Wurzel() }.Concat(SeitenOrdner).ToArray()) +
           Path.DirectorySeparatorChar + seite + ".wiki";

    /// <summary>
    /// Die Zuordnungen des Abschnitts „# Teil B (Erzeuger und Speicher)" bis zum
    /// nächsten Abschnittskommentar bzw. Dateiende: Schlüssel → Seitenname (der
    /// Teil hinter „Berechnung/").
    ///
    /// <para>Bewusst NUR dieser Abschnitt: Teil A der Rubrik hängt seine Zeilen an
    /// derselben Stelle an, und beide Teile sollen sich nicht gegenseitig rot
    /// färben.</para>
    /// </summary>
    private static Dictionary<string, string> ZuordnungenTeilB()
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
                if (zeile.Contains("Teil B", StringComparison.Ordinal)) imAbschnitt = true;
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

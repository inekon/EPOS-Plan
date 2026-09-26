using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Ansicht „Baustoff-Zuordnungen…"</b> (Nacharbeit G4b) — die Komponente mit synthetischen Gaben und
/// ihr Knopf im Gebäudedialog. Geprüft wird: die Liste (Materialname, Baustoff, Zeitpunkt), „Entfernen"
/// nimmt die Zeile heraus und schreibt erst mit OK, EINMAL, mit allen vorgemerkten Namen; Abbrechen, Esc
/// und ein leerer OK schreiben nichts; ein gescheiterter Schreibweg lässt den Dialog mit dem Grund offen;
/// ohne Schreibweg kein „Entfernen", ohne Zeilen der Leertext. Im Gebäudedialog: kein Delegat, kein Knopf;
/// der Knopf öffnet die Ansicht als Überlagerung, nach OK steht die Zahl der entfernten im Banner, und Esc
/// schließt den Gebäudedialog nicht, solange die Ansicht steht.
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class BaustoffzuordnungenDialogTests : EposBunitContext
{
    private const string KNOPF = "Baustoff-Zuordnungen…";

    public BaustoffzuordnungenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly IReadOnlyList<BaustoffzuordnungZeile> Drei = new[]
    {
        new BaustoffzuordnungZeile("estrich", "estrich", "Zementestrich", "26.09.2026 08:00"),
        new BaustoffzuordnungZeile("fussbodenaufbau", "fussbodenaufbau", "Zementestrich", "26.09.2026 10:15"),
        new BaustoffzuordnungZeile("gipsputz", "gipsputz", "Gipsputz", "26.09.2026 11:30"),
    };

    private IRenderedComponent<BaustoffzuordnungenDialog> Bauen(
        IReadOnlyList<BaustoffzuordnungZeile> zeilen, List<IReadOnlyList<string>>? geschrieben, List<int?> geschlossen,
        string? fehler = null, bool ohneSchreibweg = false)
        => Render<BaustoffzuordnungenDialog>(p =>
        {
            p.Add(x => x.Zeilen, zeilen)
             .Add(x => x.Geschlossen, (int? n) => geschlossen.Add(n));
            if (!ohneSchreibweg)
                p.Add(x => x.Entfernen, (Func<IReadOnlyList<string>, string?>)(namen =>
                {
                    geschrieben?.Add(namen);
                    return fehler;
                }));
        });

    private static IReadOnlyList<string> Spalte(IRenderedComponent<BaustoffzuordnungenDialog> cut, int spalte)
        => cut.FindAll(".epos-baustoffzuordnungen-liste tbody tr").Select(tr => tr.Children[spalte].TextContent.Trim()).ToList();

    private static void EntfernenIn(IRenderedComponent<BaustoffzuordnungenDialog> cut, string schluessel)
        => cut.Find("tr[data-schluessel=\"" + schluessel + "\"] button.epos-baustoffzuordnungen-entfernen").Click();

    private static void Ok(IRenderedComponent<BaustoffzuordnungenDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    // =====================================================================
    //  Die Komponente
    // =====================================================================

    [Fact]
    public void Die_Liste_zeigt_Materialname_Baustoff_und_Zeitpunkt()
    {
        var geschlossen = new List<int?>();
        IRenderedComponent<BaustoffzuordnungenDialog> cut = Bauen(Drei, null, geschlossen);

        Assert.Equal(new[] { "Materialname", "Baustoff", "Zugeordnet am", "" },
                     cut.FindAll(".epos-baustoffzuordnungen-liste thead th").Select(th => th.TextContent.Trim()));
        Assert.Equal(new[] { "estrich", "fussbodenaufbau", "gipsputz" }, Spalte(cut, 0));
        Assert.Equal(new[] { "Zementestrich", "Zementestrich", "Gipsputz" }, Spalte(cut, 1));
        Assert.Equal("26.09.2026 10:15", Spalte(cut, 2)[1]);
        Assert.Equal("Die Zuordnung „gipsputz“ entfernen",
                     cut.Find("tr[data-schluessel=\"gipsputz\"] button").GetAttribute("title"));
        Assert.Equal("Baustoff-Zuordnungen des Projekts", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Empty(geschlossen);
    }

    [Fact]
    public void Entfernen_nimmt_die_Zeile_heraus_und_OK_schreibt_alle_vorgemerkten_einmal()
    {
        var geschrieben = new List<IReadOnlyList<string>>();
        var geschlossen = new List<int?>();
        IRenderedComponent<BaustoffzuordnungenDialog> cut = Bauen(Drei, geschrieben, geschlossen);

        EntfernenIn(cut, "gipsputz");
        EntfernenIn(cut, "estrich");
        Assert.Equal(new[] { "fussbodenaufbau" }, Spalte(cut, 0));
        Assert.Empty(geschrieben);   // erst mit OK

        Ok(cut);
        IReadOnlyList<string> namen = Assert.Single(geschrieben);
        Assert.Equal(new[] { "gipsputz", "estrich" }, namen);
        Assert.Equal(new int?[] { 2 }, geschlossen);
    }

    [Fact]
    public void Abbrechen_Esc_und_Kreuz_schreiben_nichts()
    {
        var geschrieben = new List<IReadOnlyList<string>>();
        var geschlossen = new List<int?>();
        IRenderedComponent<BaustoffzuordnungenDialog> cut = Bauen(Drei, geschrieben, geschlossen);
        EntfernenIn(cut, "gipsputz");

        cut.FindAll(".epos-leiste button").First(k => k.TextContent.Trim() == "Abbrechen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.Find(".epos-dialog-kopf button.epos-dialog-zu").Click();

        Assert.Empty(geschrieben);
        Assert.Equal(new int?[] { null, null, null }, geschlossen);
    }

    [Fact]
    public void OK_ohne_Entfernen_schliesst_ohne_zu_schreiben()
    {
        var geschrieben = new List<IReadOnlyList<string>>();
        var geschlossen = new List<int?>();
        IRenderedComponent<BaustoffzuordnungenDialog> cut = Bauen(Drei, geschrieben, geschlossen);
        Ok(cut);
        Assert.Empty(geschrieben);
        Assert.Equal(new int?[] { 0 }, geschlossen);
    }

    [Fact]
    public void Ein_gescheiterter_Schreibweg_laesst_den_Dialog_mit_dem_Grund_offen()
    {
        var geschrieben = new List<IReadOnlyList<string>>();
        var geschlossen = new List<int?>();
        IRenderedComponent<BaustoffzuordnungenDialog> cut = Bauen(Drei, geschrieben, geschlossen, fehler: "Die Datenbank ist gesperrt.");
        EntfernenIn(cut, "estrich");
        Ok(cut);

        Assert.Single(geschrieben);
        Assert.Empty(geschlossen);
        Assert.Equal("Die Datenbank ist gesperrt.", cut.Instance.Meldung);
        Assert.Contains("Die Datenbank ist gesperrt.", cut.Markup);
        Assert.Equal(new[] { "fussbodenaufbau", "gipsputz" }, Spalte(cut, 0));   // die Zeile bleibt vorgemerkt
    }

    [Fact]
    public void Ohne_Schreibweg_gibt_es_kein_Entfernen_und_ohne_Zeilen_den_Leertext()
    {
        var geschlossen = new List<int?>();
        IRenderedComponent<BaustoffzuordnungenDialog> nurLesen = Bauen(Drei, null, geschlossen, ohneSchreibweg: true);
        Assert.Empty(nurLesen.FindAll("button.epos-baustoffzuordnungen-entfernen"));
        Assert.Equal(3, nurLesen.FindAll(".epos-baustoffzuordnungen-liste thead th").Count);

        IRenderedComponent<BaustoffzuordnungenDialog> leer = Bauen(Array.Empty<BaustoffzuordnungZeile>(), null, geschlossen);
        Assert.Empty(leer.FindAll(".epos-baustoffzuordnungen-liste"));
        Assert.Equal("Dieses Projekt hat keine eigenen Baustoff-Zuordnungen.", leer.Find(".epos-baustoffzuordnungen-leer").TextContent);
    }

    // =====================================================================
    //  Der Knopf im Gebäudedialog
    // =====================================================================

    private IRenderedComponent<GebaeudeDialog> Gebaeudedialog(
        Func<IReadOnlyDictionary<string, object>>? gaben, List<bool>? geschlossen = null)
        => Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Katalogzeilen, () => Array.Empty<Katalogfilterzeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.BaustoffzuordnungenGaben, gaben)
            .Add(x => x.Geschlossen, b => geschlossen?.Add(b)));

    private static IElement? Knopf(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == KNOPF);

    [Fact]
    public void Ohne_Delegat_traegt_der_Gebaeudedialog_keinen_Knopf()
        => Assert.Null(Knopf(Gebaeudedialog(null)));

    [Fact]
    public void Der_Knopf_oeffnet_die_Ansicht_und_nach_OK_steht_die_Zahl_im_Banner()
    {
        var geschrieben = new List<IReadOnlyList<string>>();
        int gelesen = 0;
        var geschlossen = new List<bool>();
        IRenderedComponent<GebaeudeDialog> cut = Gebaeudedialog(() =>
        {
            gelesen++;
            return new Dictionary<string, object>
            {
                ["Zeilen"] = Drei,
                ["Entfernen"] = new Func<IReadOnlyList<string>, string>(namen => { geschrieben.Add(namen); return null!; }),
            };
        }, geschlossen);

        IElement knopf = Knopf(cut)!;
        Assert.Equal("Die Zuordnungen von Materialnamen zu Baustoffen ansehen, die sich dieses Projekt beim Gebäudeimport gemerkt hat, und einzelne entfernen",
                     knopf.GetAttribute("title"));
        knopf.Click();
        Assert.Equal(1, gelesen);
        IRenderedComponent<BaustoffzuordnungenDialog> ansicht = cut.FindComponent<BaustoffzuordnungenDialog>();
        Assert.Empty(ansicht.FindAll("h1.epos-dialog-titel"));   // den Titel trägt die Überlagerung
        Assert.Contains("Baustoff-Zuordnungen des Projekts", cut.Markup);

        // Esc in der Ansicht schließt nur die Ansicht, nicht den Gebäudedialog.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(geschlossen);

        cut.FindComponent<BaustoffzuordnungenDialog>().Find("tr[data-schluessel=\"estrich\"] button").Click();
        cut.FindComponent<BaustoffzuordnungenDialog>().Find(".epos-leiste button.epos-knopf--primaer").Click();

        Assert.Equal(new[] { "estrich" }, Assert.Single(geschrieben));
        Assert.Empty(cut.FindComponents<BaustoffzuordnungenDialog>());
        Assert.Contains("Baustoff-Zuordnungen entfernt: 1.", cut.Markup);
        Assert.Empty(geschlossen);

        // Ein zweiter Klick liest neu.
        Knopf(cut)!.Click();
        Assert.Equal(2, gelesen);
    }
}

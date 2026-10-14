using System.Globalization;
using System.IO;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Zwei Reste der Spaltenwahl</b> (DZ1, Konzept Projektdialoge mit Katalogauswahl 4.10):
/// Die Trefferzahl kürzt nie mit Auslassung — sie steht als Zahl und Hauptwort, und nur das
/// Hauptwort weicht; neben einer angehakten Spalte mit Rang, die bei der aktuellen Breite
/// weicht, steht in der Auswahl „bei dieser Breite ausgeblendet“ (dieselbe Containerabfrage wie
/// die Spalte selbst).
/// </summary>
public class KataloglisteTrefferUndHinweisTests : EposBunitContext
{
    public KataloglisteTrefferUndHinweisTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<Katalogfilterzeile> Zeilen() => Enumerable.Range(1, 3).Select(i =>
        new Katalogfilterzeile(i, "Kessel mit langem Namen " + i)
            .MitText(Katalogfilterprofil.SpBezeichner, "Kessel mit langem Namen " + i)
            .MitText(Katalogfilterprofil.SpHersteller, "Herstellerwerk mit langem Namen " + i)
            .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
            .MitZahl(Katalogfilterprofil.SpPtherm, 10 * i)).ToList();

    private IRenderedComponent<Katalogliste> Aufbauen(Katalogfiltertexte? texte = null)
        => Render<Katalogliste>(p =>
        {
            p.Add(x => x.Profil, Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel, s => s))
             .Add(x => x.Zeilen, Zeilen())
             .Add(x => x.Filterstand, new Katalogfilterstand())
             .Add(x => x.Spaltenwahlname, "TrefferProbe");
            if (texte is not null) p.Add(x => x.Texte, texte);
        });

    [Fact]
    public void Die_Trefferzahl_steht_als_Zahl_und_Hauptwort()
    {
        var cut = Aufbauen();
        var treffer = cut.Find(".epos-katalog-treffer");
        string voll = string.Format(CultureInfo.CurrentCulture, Resource.KFLT_TREFFER, "3", "3");
        string kurz = string.Format(CultureInfo.CurrentCulture, Resource.KFLT_TREFFER_KURZ, "3", "3");

        Assert.Equal(voll, treffer.TextContent);
        Assert.Equal(voll, treffer.GetAttribute("title"));
        Assert.Equal(kurz, cut.Find(".epos-katalog-treffer-zahl").TextContent);
        Assert.Equal(voll.Substring(kurz.Length), cut.Find(".epos-katalog-treffer-wort").TextContent);
    }

    [Fact]
    public void Eine_eigene_Trefferzeile_des_Wirts_steht_ganz_im_Zahlteil()
    {
        var cut = Aufbauen(new Katalogfiltertexte { Treffer = "Treffer: {0}/{1}" });
        Assert.Equal("Treffer: 3/3", cut.Find(".epos-katalog-treffer-zahl").TextContent);
        Assert.Empty(cut.FindAll(".epos-katalog-treffer-wort"));
    }

    [Fact]
    public void Neben_einer_weichenden_angehakten_Spalte_steht_der_Hinweis()
    {
        var cut = Aufbauen();
        cut.Find("button.epos-katalog-spaltenknopf").Click();

        var weichend = cut.Instance.Stufen.Where(s => s.Value > 0).ToDictionary(s => s.Key, s => s.Value);
        Assert.NotEmpty(weichend);
        foreach (var eintrag in cut.FindAll(".epos-spaltenwahl-eintrag"))
        {
            string schluessel = eintrag.GetAttribute("data-spalte")!;
            bool an = eintrag.QuerySelector("input")!.HasAttribute("checked");
            var hinweis = eintrag.QuerySelector(".epos-spaltenwahl-weicht");
            if (an && weichend.TryGetValue(schluessel, out int ab))
            {
                Assert.NotNull(hinweis);
                Assert.Equal(Resource.KFLT_SPALTE_AUSGEBLENDET, hinweis!.TextContent);
                Assert.Contains("epos-weicht-ab-" + ab.ToString(CultureInfo.InvariantCulture), hinweis.ClassName);
            }
            else Assert.Null(hinweis);
        }
    }

    [Fact]
    public void Mit_gemerkter_Wahl_weicht_keine_Spalte_und_kein_Hinweis_steht()
    {
        IEinstellungen alt = WindowsFormsApplication1.Dienste.Einstellungen;
        WindowsFormsApplication1.Dienste.Einstellungen = new FluechtigeEinstellungen();
        try
        {
            var cut = Aufbauen();
            cut.Find("button.epos-katalog-spaltenknopf").Click();
            cut.FindAll(".epos-spaltenwahl-eintrag input")[0].Change(false);
            if (cut.FindAll(".epos-spaltenwahl-auswahl").Count == 0) cut.Find("button.epos-katalog-spaltenknopf").Click();
            Assert.Empty(cut.FindAll(".epos-spaltenwahl-weicht"));
        }
        finally { WindowsFormsApplication1.Dienste.Einstellungen = alt; }
    }

    [Fact]
    public void Das_Stilblatt_kuerzt_die_Trefferzahl_nicht_und_zeigt_den_Hinweis_je_Stufe()
    {
        string css = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"));
        int a = css.IndexOf(".epos-zweispalten-kopfleiste .epos-katalog-treffer {", StringComparison.Ordinal);
        Assert.True(a >= 0);
        string block = css.Substring(a, css.IndexOf('}', a) - a);
        Assert.DoesNotContain("ellipsis", block);
        Assert.Contains("min-width: min-content", block);
        Assert.Contains("flex: 0 0 auto !important", block);
        Assert.Contains("@container zweispaltenbereich (max-width: 699.98px) {\n    .epos-zweispalten-kopfleiste .epos-katalog-treffer-wort { display: none; }", css);
        for (int stufe = 400; stufe <= 1760; stufe += 80)
        {
            string g = (stufe - 0.02).ToString("0.00", CultureInfo.InvariantCulture);
            Assert.Contains($"@container epos-katalogliste (max-width: {g}px) {{ .epos-weicht-ab-{stufe} {{ display: inline; }} }}", css);
            Assert.Contains($"@container zweispaltenbereich (max-width: {g}px) {{ .epos-weicht-ab-{stufe} {{ display: inline; }} }}", css);
            Assert.Contains($"@container epos-katalogliste (max-width: {g}px) {{ .epos-spalte-ab-{stufe} {{ display: none; }} }}", css);
        }
    }

    private static string Wurzel()
    {
        string? d = AppContext.BaseDirectory;
        while (d is not null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
        return d ?? throw new InvalidOperationException("Repowurzel nicht gefunden");
    }
}

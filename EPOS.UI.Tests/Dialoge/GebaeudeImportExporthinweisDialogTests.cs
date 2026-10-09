using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Hinweis zur Exporteinstellung und Modellansicht (MVD) im Gebäudeimport</b> — mit der echten Hülle: An der Dateiwahl
/// steht der Hinweis, solange keine Quelle oder eine IFC-Quelle gewählt ist; nach dem Lesen einer IFC-Datei nennt der Kopf
/// die Modellansicht neben dem Schema, und das Protokoll trägt die Info-Zeile mit Schema und MVD.
/// </summary>
public class GebaeudeImportExporthinweisDialogTests : EposBunitContext
{
    private const string HINWEIS = "Empfohlene Exporteinstellung: IFC4 mit Basismengen und Raumgrenzen 2. Ebene.";

    public GebaeudeImportExporthinweisDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<GebaeudeImportDialog> Bauen(string datei, GebaeudeImportHuelle huelle)
    {
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        return Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(datei, Path.GetFileName(datei), new FileInfo(datei).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
            c.Add(x => x.NordrichtungDaten, (Func<GebaeudeNordrichtungDaten?>)g["NordrichtungDaten"]);
        });
    }

    private static string Probe(string name)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return Path.Combine(d!.FullName, "Referenzlaeufe", "Importproben", name);
    }

    private static IElement Quellwahl(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-gebimport-quellwahl select");

    private static string? Hinweis(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.FindAll(".epos-gebimport-exporthinweis").SingleOrDefault()?.TextContent.Trim();

    [Fact]
    public void Der_Hinweis_steht_an_der_Dateiwahl_fuer_IFC_und_ohne_Wahl()
    {
        var huelle = new GebaeudeImportHuelle(ios: false);
        var cut = Bauen(Probe("ifc4_haus.ifc"), huelle);

        Assert.StartsWith(HINWEIS, Hinweis(cut));   // ohne Wahl: der Hinweis des IFC-Wegs
        Assert.Contains("IFC2x3 (Coordination View 2.0)", Hinweis(cut));

        IReadOnlyList<GebaeudeImportQuellwahl> quellen = ((GebaeudeImportProfilDaten)huelle.Gaben()["Profil"]).Quellen!;
        for (int i = 0; i < quellen.Count; i++)
        {
            Quellwahl(cut).Change((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            bool ifc = quellen[i].Schluessel == GebaeudeImportHuelle.QUELLE_IFC
                       || quellen[i].Schluessel == GebaeudeImportHuelle.QUELLE_IFC_PROJEKTDATEI;
            if (ifc) Assert.StartsWith(HINWEIS, Hinweis(cut));
            else Assert.Null(Hinweis(cut));
        }
    }

    [Fact]
    public void Nach_dem_Lesen_nennen_Kopf_und_Protokoll_Schema_und_Modellansicht()
    {
        var cut = Bauen(Probe("ifc4_haus.ifc"), new GebaeudeImportHuelle(ios: false));
        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("dl.epos-gebimport-kopf")));

        IElement kopf = cut.Find("dl.epos-gebimport-kopf");
        Assert.Contains("Modellansicht (MVD)", kopf.TextContent);
        Assert.Equal("ReferenceView", cut.Find("dd.epos-gebimport-mvd").TextContent.Trim());
        Assert.Contains("Datei: IFC4, Modellansicht (MVD): ReferenceView", cut.Markup);
    }
}

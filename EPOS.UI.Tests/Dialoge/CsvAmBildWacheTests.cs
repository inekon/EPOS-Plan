using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„CSV…“ bei allen Zeitreihen</b> (Anwendergrundsatz 10.10.2026, CSV-3): Jeder Wirt der Liste
/// führt den Parameter <c>CsvSpeichern</c>, legt seine Naht als Kaskade um sein Markup — damit
/// erreicht sie jedes Bild darunter, auch in eingebetteten Bausteinen — und bekommt ihn von seiner
/// Hülle über <c>Diagrammexportnaht.Fuer</c>. Ein Wirt, der aus der Liste fällt, verliert den Knopf
/// still; diese Wache macht das laut. Den Klick selbst prüfen die bunit-Fälle der Dialoge.
/// </summary>
public class CsvAmBildWacheTests
{
    /// <summary>Wirt, Razor-Datei, Hülle(n), die den Parameter reichen.</summary>
    public static TheoryData<string, string, string> Wirte() => new()
    {
        { "QuelleErdreichDialog", "EPOS.UI/Dialoge/Simulation/QuelleErdreichDialog.razor", "EPOS.UI.Daten/Simulation/QuelleErdreichHuelle.cs" },
        { "WaermebedarfAdminDialog", "EPOS.UI/Dialoge/Bedarf/WaermebedarfAdminDialog.razor", "WindowsFormsApplication1/Views/Wärmebedarf/WaermebedarfAdminHuelle.cs" },
        { "StromganglinieAdminDialog", "EPOS.UI/Dialoge/Strom/StromganglinieAdminDialog.razor", "EPOS.UI.Daten/Strom/StromganglinieAdminHuelle.cs" },
        { "WaermebedarfExternDialog", "EPOS.UI/Dialoge/Bedarf/WaermebedarfExternDialog.razor", "WindowsFormsApplication1/Views/Wärmebedarf/WaermebedarfExternHuelle.cs" },
        { "StromganglinieDialog", "EPOS.UI/Dialoge/Strom/StromganglinieDialog.razor", "WindowsFormsApplication1/Views/Stromverbraucher/StromganglinieHuelle.cs" },
        { "GebaeudeDialog", "EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor", "EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs" },
        { "GebaeudeBedarfDialog", "EPOS.UI/Dialoge/Bedarf/GebaeudeBedarfDialog.razor", "EPOS.UI.Daten/Bedarf/GebaeudeBedarfHuelle.cs" },
        { "KlimadatenDialog", "EPOS.UI/Dialoge/Klimadaten/KlimadatenDialog.razor", "EPOS.UI.Daten/Klimadaten/KlimadatenHuelle.cs" },
        { "KostenprofilDialog", "EPOS.UI/Dialoge/Kosten/KostenprofilDialog.razor", "EPOS.UI.Daten/Kosten/KostenprofilHuelle.cs" },
        // Wochen- und Tagesprofile, Zapfprofile: Typprofil, Zapfprofil samt Auslegung (in den Bedarfsprofilen),
        // Gebäudetyp, Raumnutzung, Nutzungsprofil, Konditionierungsvorlagen (in Gebäude und Gebäudekatalog).
        { "BedarfsProfileDialog", "EPOS.UI/Dialoge/Bedarf/BedarfsProfileDialog.razor", "WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs" },
        { "BedarfAdminDialog", "EPOS.UI/Dialoge/Bedarf/BedarfAdminDialog.razor", "WindowsFormsApplication1/Views/Bedarf/BedarfAdminHuelle.cs" },
        { "TwwNutzungsartAdminDialog", "EPOS.UI/Dialoge/Bedarf/TwwNutzungsartAdminDialog.razor", "EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Katalogdialog.cs" },
        { "GebaeudeAdminDialog", "EPOS.UI/Dialoge/Bedarf/GebaeudeAdminDialog.razor", "EPOS.UI.Daten/Bedarf/GebaeudeAdminHuelle.cs" },
    };

    [Theory]
    [MemberData(nameof(Wirte))]
    public void Der_Wirt_fuehrt_Parameter_Kaskade_und_Huellengabe(string wirt, string razor, string huelle)
    {
        Type typ = typeof(DiagrammSvg).Assembly.GetTypes().Single(t => t.Name == wirt);
        PropertyInfo? p = typ.GetProperty("CsvSpeichern");
        Assert.True(p is not null, wirt + ": ohne Parameter CsvSpeichern.");
        Assert.NotNull(p!.GetCustomAttribute<ParameterAttribute>());
        Assert.Equal(typeof(Func<Zeichenmodell, string, Zeitraster, Task>), p.PropertyType);

        string quelltext = File.ReadAllText(Pfad(razor));
        Assert.Contains("<CascadingValue Value=\"@CsvNaht\">", quelltext);

        foreach (string h in huelle.Split('|'))
            Assert.Contains("[\"CsvSpeichern\"] = Diagrammexportnaht.Fuer(", File.ReadAllText(Pfad(h)));
    }

    /// <summary>
    /// Die Kalenderteppiche (CSV-4): Teppichbild der Kalenderkarte und der Raumnutzungsvorschau —
    /// Wirt mit Kaskade, Kette der Einbettung bis zum Bild, das Bild ohne eigenen <c>CsvExport</c>.
    /// </summary>
    public static TheoryData<string, string> Teppichketten() => new()
    {
        { "GebaeudeDialog>GebaeudeKatalogDialog>KonditionierungReiter>KalenderkarteInhalt", "Modell=\"@_teppich\"" },
        { "GebaeudeAdminDialog>KonditionierungReiter>KalenderkarteInhalt", "Modell=\"@_teppich\"" },
        { "GebaeudeDialog>GebaeudeKatalogDialog>RaumnutzungBlatt>RaumnutzungBildEditor", "Modell=\"@_vorschau.Teppich\"" },
    };

    [Theory]
    [MemberData(nameof(Teppichketten))]
    public void Der_Kalenderteppich_sitzt_unter_einem_Wirt_und_traegt_den_Knopf(string kette, string bild)
    {
        string[] glieder = kette.Split('>');
        Assert.Contains(Wirte(), w => (string)w[0] == glieder[0]);
        for (int i = 0; i < glieder.Length; i++)
        {
            string datei = Directory.GetFiles(Pfad("EPOS.UI/Dialoge"), glieder[i] + ".razor", SearchOption.AllDirectories).Single();
            string text = File.ReadAllText(datei);
            if (i + 1 < glieder.Length)
                Assert.True(text.Contains("<" + glieder[i + 1]), glieder[i] + " bettet " + glieder[i + 1] + " nicht ein.");
            else
            {
                int stelle = text.IndexOf(bild, StringComparison.Ordinal);
                Assert.True(stelle > 0, glieder[i] + ": Teppichbild nicht gefunden.");
                string tag = text.Substring(stelle, text.IndexOf("/>", stelle, StringComparison.Ordinal) - stelle);
                Assert.DoesNotContain("CsvExport", tag);
            }
        }

        // Das Teppichmodell selbst passt zur Naht: die Tafel 365 × 24, Raster Kalendertag.
        var naht = new Ganglinienexport((_, _, _) => Task.CompletedTask);
        Zeichenmodell teppich = ChartRenderer.KalenderteppichModell(Kalenderteppich.ImGemeinjahr(
            new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, null),
            new Gemeinjahrkalender(3)));
        Assert.True(naht.Passt(teppich));
        Assert.Equal(Zeitraster.Kalendertag, naht.RasterFuer(teppich));
    }

    private static string Pfad(string relativ)
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !Directory.Exists(Path.Combine(ordner.FullName, "WindowsFormsApplication1", "Views")))
            ordner = ordner.Parent;
        Assert.True(ordner is not null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return Path.Combine(ordner!.FullName, relativ.Replace('/', Path.DirectorySeparatorChar));
    }
}

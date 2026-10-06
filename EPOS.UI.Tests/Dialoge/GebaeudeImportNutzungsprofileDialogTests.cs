using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Nutzungsprofile…" am Zonenbaum des Imports</b> (Stufe NP3b; Konzept Nutzungsprofile 6.2, NP-F22) — gefahren mit
/// der echten Hülle und dem Zonenhaus wie der Zonenbaum: Der Knopf steht nur mit dem Weg des Katalogs („Kein Delegat,
/// kein Knopf"), öffnet den Katalog als Überlagerung, Esc schließt zuerst die Überlagerung und nicht den Dialog, und
/// nach dem Schließen ordnet der Plan neu zu (der Katalog hat sofort geschrieben, NP-F3).
/// </summary>
public partial class GebaeudeImportZonenDialogTests
{
    /// <summary>Ein Katalog ohne Datenbank: eine Kategorie mit einem eigenen Profil, keine Zuordnung.</summary>
    private static RaumnutzungWeg Nutzungskatalog() => new()
    {
        Kategorien = () => new[]
        {
            new RaumnutzungKategorieDaten(2, "Eigene Profile", RaumnutzungArt.Eigen, false, "", "")
        },
        Profile = _ => new[] { new RaumnutzungProfilDaten { Id = 12, IdKategorie = 2, Bezeichner = "Mein Büro" } },
        Zuordnungen = () => Array.Empty<RaumnutzungZuordnungDaten>(),
    };

    private IRenderedComponent<GebaeudeImportDialog> ZonenbaumMitKatalog(Zonenbaumprobe p, List<GebaeudeImportErgebnis?> zu)
    {
        string probe = Path.Combine(Wurzel(), "Referenzlaeufe", "Importproben", "ifc4_zonen.ifc");
        var huelle = new GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"];
        IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(probe, "ifc4_zonen.ifc", new FileInfo(probe).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return zuordnen(a); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(e => { p.Uebernommen.Add(e); return Task.FromResult<string?>(null); }));
            c.Add(x => x.Raumnutzung, Nutzungskatalog());
            c.Add(x => x.Geschlossen, (GebaeudeImportErgebnis? e) => zu.Add(e));
        });
        Einlesen(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum")));
        return cut;
    }

    /// <summary>Ohne den Weg des Katalogs trägt der Zonenbaum keinen Knopf „Nutzungsprofile…".</summary>
    [Fact]
    public void Ohne_Katalogweg_traegt_der_Zonenbaum_keinen_Knopf_Nutzungsprofile()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Zonenbaum(new Zonenbaumprobe());

        Assert.Empty(cut.FindAll(".epos-gebimport-nutzungsprofile-oeffnen"));
        Assert.False(cut.Instance.NutzungsprofileOffen);
    }

    /// <summary>
    /// Mit dem Weg steht der Knopf am Zonenbaum; er öffnet den Katalog als Überlagerung über dem Dialog, und das Blatt
    /// darin zeigt die Kategorien des Wegs.
    /// </summary>
    [Fact]
    public void Der_Knopf_Nutzungsprofile_oeffnet_den_Katalog_als_Ueberlagerung()
    {
        var zu = new List<GebaeudeImportErgebnis?>();
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumMitKatalog(new Zonenbaumprobe(), zu);

        IElement knopf = cut.Find(".epos-gebimport-nutzungsprofile-oeffnen");
        Assert.Empty(cut.FindAll(".epos-gebimport-nutzungsblatt"));
        knopf.Click();

        Assert.True(cut.Instance.NutzungsprofileOffen);
        IElement blatt = cut.Find(".epos-ueberlagerung.epos-gebimport-nutzungsblatt");
        Assert.NotNull(blatt.QuerySelector(".epos-raumnutzung"));
        Assert.Single(blatt.QuerySelectorAll(".epos-raumnutzung-kategorie"));
        // Der Dialog darunter bleibt stehen.
        Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum"));
        Assert.Empty(zu);
    }

    /// <summary>
    /// Esc schließt zuerst die Überlagerung — nicht den Dialog —, und nach dem Schließen ordnet der Plan neu zu; erst
    /// das nächste Esc gilt dem Dialog (Abbrechen, Ergebnis <c>null</c>).
    /// </summary>
    [Fact]
    public void Esc_schliesst_zuerst_die_Ueberlagerung_und_danach_wird_neu_zugeordnet()
    {
        var p = new Zonenbaumprobe();
        var zu = new List<GebaeudeImportErgebnis?>();
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumMitKatalog(p, zu);
        cut.Find(".epos-gebimport-nutzungsprofile-oeffnen").Click();
        int anfragen = p.Anfragen.Count;

        // Erreicht die Taste die Wurzel des Dialogs, solange die Überlagerung steht, schließt er nicht.
        cut.Find(".epos-dialog.epos-gebimport").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.True(cut.Instance.NutzungsprofileOffen);
        Assert.Empty(zu);
        Assert.Equal(anfragen, p.Anfragen.Count);

        // Esc in der Überlagerung schließt sie, der Dialog bleibt, und der Plan liest Katalog und Zuordnung neu.
        cut.Find(".epos-ueberlagerung.epos-gebimport-nutzungsblatt")
           .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.NutzungsprofileOffen);
        Assert.Empty(cut.FindAll(".epos-gebimport-nutzungsblatt"));
        Assert.Empty(zu);
        Assert.Equal(anfragen + 1, p.Anfragen.Count);
        Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonenbaum"));

        // Erst jetzt gilt Esc dem Dialog.
        cut.Find(".epos-dialog.epos-gebimport").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        cut.WaitForAssertion(() => Assert.Single(zu));
        Assert.Null(zu[0]);
    }

    /// <summary>Auch das Kreuz der Überlagerung schließt nur sie und ordnet neu zu.</summary>
    [Fact]
    public void Das_Kreuz_der_Ueberlagerung_schliesst_sie_und_ordnet_neu_zu()
    {
        var p = new Zonenbaumprobe();
        var zu = new List<GebaeudeImportErgebnis?>();
        IRenderedComponent<GebaeudeImportDialog> cut = ZonenbaumMitKatalog(p, zu);
        cut.Find(".epos-gebimport-nutzungsprofile-oeffnen").Click();
        int anfragen = p.Anfragen.Count;

        cut.Find(".epos-gebimport-nutzungsblatt .epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.NutzungsprofileOffen);
        Assert.Equal(anfragen + 1, p.Anfragen.Count);
        Assert.Empty(zu);
    }
}

using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// G5-3b: In der Liste „Flächen je Zone" steht nach der Fläche ihre Herkunft („Fläche aus": Mengensatz, Raumgrenze, Körper,
/// schematisch), und neben den vorhandenen Filtern schränkt „Nur Bauteile mit Befund" auf die Bauteile ein, die im Farbmodus
/// „Befund" orange oder rot stehen. Das Profil ist das der Hülle.
/// </summary>
public partial class GebaeudeImportZonenDialogTests
{
    private static readonly string[] Herkuenfte = { "Mengensatz", "Raumgrenze", "Körper", "schematisch", "" };

    /// <summary>Der Stand mit dem Profil der Hülle; jede vierte Fläche hat einen Bauteilbefund, die Herkunft wechselt reihum.</summary>
    private static GebaeudeImportStand StandMitHerkunft(GebaeudeZuordnungsanfrage a)
    {
        GebaeudeImportStand s = Stand(a);
        if (s.Zonierung is not { Flaechenprofil: not null } z) return s;
        var zeilen = z.Flaechen.Select((f, i) =>
        {
            f.Zeile.MitText(GebaeudeImportZonen.SP_FLAECHENHERKUNFT, Herkuenfte[i % Herkuenfte.Length]);
            return f with { MitBauteilbefund = i % 4 == 0 };
        }).ToList();
        return s with { Zonierung = z with { Flaechenprofil = GebaeudeImportZonen.Flaechenprofil(), Flaechen = zeilen } };
    }

    [Fact]
    public void G5_Die_Flaechenliste_nennt_die_Herkunft_der_Flaeche_und_filtert_nach_Bauteilbefund()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p, StandMitHerkunft);
        Einlesen(cut);

        // Die Spalte „Fläche aus" mit den kurzen Werten; die Erläuterung steht unter der Liste.
        IRenderedComponent<Katalogliste> liste = cut.FindComponent<Katalogliste>();
        Assert.Contains(liste.FindAll("th"), th => th.TextContent.Contains("Fläche aus", StringComparison.Ordinal));
        string text = liste.Markup;
        foreach (string h in Herkuenfte.Where(h => h.Length > 0)) Assert.Contains(h, text);
        Assert.Equal(new GebaeudeImportTexte().FlaechenherkunftHinweis,
                     cut.Find(".epos-gebimport-flaechenherkunft-hinweis").TextContent.Trim());

        // Der Filter steht neben den vorhandenen, mit Titeltext.
        IElement huelle = cut.Find(".epos-gebimport-flaechenfilter .epos-gebimport-filter-bauteilbefund");
        Assert.Equal(new GebaeudeImportTexte().FilterBauteilbefundHinweis, huelle.GetAttribute("title"));
        Assert.Equal(4, cut.FindAll(".epos-gebimport-flaechenfilter label.epos-schalter").Count);

        Assert.Equal(30, cut.Instance.Flaechenzeilen.Count);
        Schalter(cut, "Nur Bauteile mit Befund").Change(true);
        Assert.Equal(8, cut.Instance.Flaechenzeilen.Count);              // 0, 4, …, 28
        Assert.Equal(8, cut.FindComponent<Katalogliste>().Instance.Angezeigt.Count);
        // Zusammen mit „ohne Nachbarfläche" gilt „und": 0, 12, 24.
        Schalter(cut, "Nur Flächen ohne Nachbarfläche").Change(true);
        Assert.Equal(3, cut.Instance.Flaechenzeilen.Count);
        Schalter(cut, "Nur Flächen ohne Nachbarfläche").Change(false);
        Schalter(cut, "Nur Bauteile mit Befund").Change(false);
        Assert.Equal(30, cut.Instance.Flaechenzeilen.Count);
    }

    [Fact]
    public void G5_Die_Bauteilliste_einer_Zone_traegt_die_Spalte_Flaeche_aus()
    {
        // Zuletzt angehängt: Die übrigen Spalten behalten ihre Stellen.
        Katalogfilterprofil profil = GebaeudeImportHuelle.Bauteilprofil();
        Assert.Equal(GebaeudeImportZonen.SP_FLAECHENHERKUNFT, profil.Spalten[^1].Schluessel);
        Assert.Equal("Fläche aus", profil.Spalten[^1].Titel);
        Assert.Equal(GebaeudeImportZonen.SP_FLAECHENHERKUNFT, GebaeudeImportZonen.Flaechenprofil().Spalten[^1].Schluessel);
    }
}

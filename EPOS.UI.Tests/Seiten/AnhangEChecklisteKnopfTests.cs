using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// ETAPPE E8b (U43; Register Q18, Konzept § 2.11.2 V‑G12) — der Knopf
/// <b>„Anhang-E-Checkliste…"</b> im Fuß des Bewertungsblocks. Er öffnet eine Überlagerung
/// mit den 15 Punkten der DIN EN 17463, Anhang E: je Punkt Anforderung, Stelle im Bericht
/// und Stand in EPOS — dieselben Punkte wie auf der Abschlussseite der Berichte
/// (<see cref="AnhangECheckliste.Punkte"/>). Der Stand folgt der Lage der Seite.
///
/// <para><b>Kulturpinnung</b>: Die Texte kommen aus <c>MyResource</c>; die Hausvorrichtung
/// <see cref="EposBunitContext"/> pinnt de-DE.</para>
/// </summary>
public class AnhangEChecklisteKnopfTests : EposBunitContext
{
    public AnhangEChecklisteKnopfTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<AnhangEChecklisteKnopf> Zeige(WirtschaftlichkeitStand stand)
        => Render<AnhangEChecklisteKnopf>(p => p.Add(x => x.Stand, stand));

    /// <summary>Die Zeile eines Punktes (ohne die Gruppenzeilen), gesucht über die Nummer.</summary>
    private static IElement Punktzeile(IRenderedComponent<AnhangEChecklisteKnopf> cut, string nummer)
        => cut.FindAll("table.epos-wirt-checkliste tbody tr")
              .Where(z => !z.ClassList.Contains("epos-wirt-checkliste-gruppe"))
              .First(z => z.QuerySelector("td")!.TextContent == nummer);

    [Fact]
    public void Der_Knopf_zeigt_die_fuenfzehn_Punkte_mit_Stelle_und_Stand()
    {
        var cut = Zeige(new WirtschaftlichkeitStand());

        IElement knopf = cut.Find("button.epos-wirt-checklistenknopf");
        Assert.Equal(Resource.WIRT_AE_KNOPF, knopf.TextContent.Trim());
        Assert.Equal("Anhang-E-Checkliste…", knopf.TextContent.Trim());
        Assert.Empty(cut.FindAll("table.epos-wirt-checkliste"));   // zu, bis geklickt

        knopf.Click();

        Assert.Equal(Resource.WIRT_AE_TITEL, cut.Find(".epos-ueberlagerung-titel").TextContent);
        var zeilen = cut.FindAll("table.epos-wirt-checkliste tbody tr")
                        .Where(z => !z.ClassList.Contains("epos-wirt-checkliste-gruppe")).ToList();
        Assert.Equal(AnhangECheckliste.ANZAHL, zeilen.Count);
        Assert.Equal(new[] { "0.1", "0.2", "1", "2a", "2b", "3a", "3b", "4", "5", "6", "7", "8", "9", "10", "11" },
                     zeilen.Select(z => z.QuerySelector("td")!.TextContent).ToArray());
        Assert.Equal(5, cut.FindAll("tr.epos-wirt-checkliste-gruppe").Count);

        // Jeder Punkt nennt die Stelle im Bericht.
        Assert.All(zeilen, z => Assert.False(string.IsNullOrWhiteSpace(z.QuerySelectorAll("td")[3].TextContent)));

        // Ohne Rechnung: Punkt 1 offen, Punkt 0.1 erfüllt.
        Assert.StartsWith(Resource.WIRT_AE_STAND_OFFEN, Punktzeile(cut, "1").QuerySelectorAll("td")[4].TextContent);
        Assert.StartsWith(Resource.WIRT_AE_STAND_ERFUELLT, Punktzeile(cut, "0.1").QuerySelectorAll("td")[4].TextContent);

        // Das Kreuz schließt die Überlagerung.
        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.Empty(cut.FindAll("table.epos-wirt-checkliste"));
    }

    [Fact]
    public void Gepflegte_nicht_monetaere_Wirkungen_heben_2b_und_3b_auf_teilweise()
    {
        var ohne = Zeige(new WirtschaftlichkeitStand());
        ohne.Find("button.epos-wirt-checklistenknopf").Click();
        Assert.StartsWith(Resource.WIRT_AE_STAND_OFFEN, Punktzeile(ohne, "2b").QuerySelectorAll("td")[4].TextContent);

        var mit = Zeige(new WirtschaftlichkeitStand { NichtMonetaer = "Versorgungssicherheit" });
        mit.Find("button.epos-wirt-checklistenknopf").Click();
        Assert.StartsWith(Resource.WIRT_AE_STAND_TEILWEISE, Punktzeile(mit, "2b").QuerySelectorAll("td")[4].TextContent);
        Assert.StartsWith(Resource.WIRT_AE_STAND_TEILWEISE, Punktzeile(mit, "3b").QuerySelectorAll("td")[4].TextContent);
    }

    [Fact]
    public void Waehrend_eines_Laufs_ist_der_Knopf_gesperrt()
    {
        var cut = Render<AnhangEChecklisteKnopf>(p => p
            .Add(x => x.Stand, new WirtschaftlichkeitStand())
            .Add(x => x.Gesperrt, true));
        Assert.True(cut.Find("button.epos-wirt-checklistenknopf").HasAttribute("disabled"));
    }

    /// <summary>Punkt 9 verlangt Ungünstig UND Günstig mit Zahl: Eine Bandbreite, deren
    /// Zeilen nur „—" tragen, lässt ihn offen — dieselbe Bedingung wie im Bericht
    /// (<see cref="ChecklistenLage.SzenarienGerechnet"/>). Die Tafel ist gebaut wie in der
    /// Hülle: erste Spalte die Version, darunter zuerst die Referenzzeile.</summary>
    [Fact]
    public void Die_Szenarioanalyse_verlangt_Zahlen_in_Unguenstig_und_Guenstig()
    {
        static ErgebnisMatrix Tafel(string worst, string best) => new()
        {
            Spalten = new[] { Resource.WIRT_SZ_SP_VARIANTE, Resource.WIRT_SZEN_WORST, Resource.WIRT_SZEN_ERWARTET,
                              Resource.WIRT_SZEN_BEST, Resource.WIRT_SZ_SP_SPANNE, Resource.WIRT_EMPF_SPALTE },
            Zeilen = new[]
            {
                new MatrixZeile { Titel = "Stamm", Zellen = new[] { "Referenz", "Referenz", "Referenz", "—", "—" } },
                new MatrixZeile { Titel = "Variante A", Zellen = new[] { worst, "1.000", best, "—", "—" } }
            }
        };

        string Punkt9(ErgebnisMatrix tafel)
        {
            var stand = new WirtschaftlichkeitStand();
            stand.Ansicht.Bandbreite = tafel;
            var cut = Zeige(stand);
            cut.Find("button.epos-wirt-checklistenknopf").Click();
            return Punktzeile(cut, "9").QuerySelectorAll("td")[4].TextContent;
        }

        Assert.StartsWith(Resource.WIRT_AE_STAND_OFFEN, Punkt9(Tafel("—", "—")));
        Assert.StartsWith(Resource.WIRT_AE_STAND_OFFEN, Punkt9(Tafel("-500", "—")));
        Assert.StartsWith(Resource.WIRT_AE_STAND_TEILWEISE, Punkt9(Tafel("-500", "2.500")));
    }

    /// <summary>Der Knopf steht im Fuß des Bewertungsblocks — in beiden Darstellungen,
    /// vor „Bericht erzeugen" (Mockup-Folge), auch ohne Gaben.</summary>
    [Fact]
    public void Der_Knopf_steht_im_Fuss_des_Bewertungsblocks()
    {
        var cut = Render<WirtschaftlichkeitSeite>();
        Assert.Single(cut.FindAll(".epos-wirt-abschnitt-fuss button.epos-wirt-checklistenknopf"));
    }
}

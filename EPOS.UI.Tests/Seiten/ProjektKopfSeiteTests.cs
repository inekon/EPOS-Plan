using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Seiten.Assistent;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die erste Assistentenseite (iU9-W15a.6) — Soll ist die Feldkarte von
/// <c>Wizard_Projekt</c>: zehn Kartenzeilen, davon vier Textfelder, zwei GESPERRTE
/// Datumsfelder, ein Auswahlfeld und die Kopfzeile.
///
/// <para>Geprueft wird vor allem der RUECKWEG: Die Seite schreibt AN ORT UND STELLE
/// in das uebergebene <see cref="ProjektKopfDaten"/> — daraus liest
/// <c>WizardParent</c> (Weg (a), Befund W15a-B42).</para>
/// </summary>
public class ProjektKopfSeiteTests : BunitContext
{
    private static readonly (int Id, string Text)[] REGIONEN =
    {
        (12, "Region 12 Mannheim"),
        (5, "Region 05 Hamburg")
    };

    public ProjektKopfSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
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

    private static ProjektKopfDaten Satz() => new ProjektKopfDaten
    {
        Name = "Laurentiuskirche",
        Beschreibung = "Denkmalschutz",
        Kunde = "Kirchengemeinde",
        Bearbeiter = "M. Muster",
        Erstelldatum = new DateTime(2020, 1, 12),
        Aenderungsdatum = new DateTime(2026, 5, 4),
        IdKlimaregion = 12,
        Klimaname = "Region 12 Mannheim",
        NameAenderbar = false
    };

    private IRenderedComponent<ProjektKopfSeite> Aufbauen(ProjektKopfDaten daten)
        => Render<ProjektKopfSeite>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Klimaregionen, REGIONEN));

    [Fact]
    public void Die_Seite_zeigt_die_neun_Felder_des_Vorlaeufers()
    {
        var cut = Aufbauen(Satz());

        // Projektname, Kunde, Bearbeiter + die zwei gesperrten Datumsfelder;
        // Beschreibung ist ein textarea, Klimaregion ein select.
        Assert.Equal(5, cut.FindAll("input[type=text]").Count);
        Assert.Single(cut.FindAll("textarea"));
        Assert.Single(cut.FindAll("select"));

        Assert.Contains("Projektkonfiguration", cut.Find(".epos-gruppenkopf").TextContent);
        Assert.Contains("administrativen Projektdaten", cut.Markup);
    }

    [Fact]
    public void Die_beiden_Datumsfelder_sind_gesperrt_und_zeigen_die_Programmsprache()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;              // sonst waere der Name das dritte
        var cut = Aufbauen(daten);

        var gesperrt = cut.FindAll("input[type=text][readonly]");
        Assert.Equal(2, gesperrt.Count);

        // A-9: "d" der aktuellen Kultur - der Vorlaeufer nagelte de-DE fest (B32a).
        Assert.Equal(new DateTime(2026, 5, 4).ToString("d"), gesperrt[0].GetAttribute("value"));
        Assert.Equal(new DateTime(2020, 1, 12).ToString("d"), gesperrt[1].GetAttribute("value"));
    }

    [Fact]
    public void Im_Bearbeiten_Modus_ist_der_Projektname_gesperrt()
    {
        var cut = Aufbauen(Satz());

        // Name + die zwei Datumsfelder
        Assert.Equal(3, cut.FindAll("input[type=text][readonly]").Count);
    }

    [Fact]
    public void Im_Neu_Modus_ist_der_Projektname_aenderbar()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;
        var cut = Aufbauen(daten);

        // nur noch die zwei Datumsfelder
        Assert.Equal(2, cut.FindAll("input[type=text][readonly]").Count);
    }

    [Fact]
    public void Jede_Eingabe_landet_AN_ORT_UND_STELLE_im_uebergebenen_Satz()
    {
        ProjektKopfDaten daten = Satz();
        daten.NameAenderbar = true;
        var cut = Aufbauen(daten);

        var felder = cut.FindAll("input[type=text]:not([readonly])");
        felder[0].Input("Neuer Name");                   // Projektname
        felder[1].Input("Neuer Kunde");                  // Kunde
        felder[2].Input("Neuer Bearbeiter");             // Bearbeiter
        cut.Find("textarea").Input("Neue Beschreibung"); // Beschreibung

        Assert.Equal("Neuer Name", daten.Name);
        Assert.Equal("Neuer Kunde", daten.Kunde);
        Assert.Equal("Neuer Bearbeiter", daten.Bearbeiter);
        Assert.Equal("Neue Beschreibung", daten.Beschreibung);
    }

    [Fact]
    public void Die_Klimaregion_traegt_Id_UND_Namen_nach()
    {
        ProjektKopfDaten daten = Satz();
        var cut = Aufbauen(daten);

        cut.Find("select").Change("5");

        Assert.Equal(5, daten.IdKlimaregion);
        Assert.Equal("Region 05 Hamburg", daten.Klimaname);
    }

    [Fact]
    public void Ein_Altprojekt_ohne_passende_Id_wird_ueber_den_NAMEN_zugeordnet()
    {
        // Aeltere Projekte fuehren in Tab_Projekt.ID_Klimaregion die Id der
        // PROJEKTKOPIE; sie steht in keiner Stammliste.
        ProjektKopfDaten daten = Satz();
        daten.IdKlimaregion = 987;
        daten.Klimaname = "Region 05 Hamburg";

        var cut = Aufbauen(daten);

        var gewaehlt = cut.FindAll("select option")
                          .Cast<AngleSharp.Html.Dom.IHtmlOptionElement>()
                          .FirstOrDefault(o => o.IsSelected);
        Assert.NotNull(gewaehlt);
        Assert.Equal("5", gewaehlt!.Value);
    }

    [Fact]
    public void Ein_leerer_Satz_zeigt_leere_Felder_und_keine_Region()
    {
        var daten = new ProjektKopfDaten();
        var cut = Aufbauen(daten);

        Assert.Equal("", cut.FindAll("input[type=text]:not([readonly])")[0].GetAttribute("value"));
        Assert.DoesNotContain(cut.FindAll("select option")
                                 .Cast<AngleSharp.Html.Dom.IHtmlOptionElement>()
                                 .Where(o => o.IsSelected),
                              o => o.Value == "12" || o.Value == "5");
    }
}

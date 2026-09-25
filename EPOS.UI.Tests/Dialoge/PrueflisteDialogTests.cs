using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using EPOS.UI.Tests.Bausteine;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Überlagerung „Prüfliste" der Berichtsvorlagen (BV-E1, Konzept 6.8 und 9.7): die Meldungen
/// nach Stufe gruppiert mit Zeichen und Zahl, Fundort und „Was tun", die Kopfzeile, der Fall ohne
/// Befunde und ohne Gaben, die eine Fußleiste mit „Schließen" als primärem Knopf und der Rückweg
/// über Schließen, Esc und Kreuz.
///
/// <para>Kultur de-DE; alle Meldungen erfunden.</para>
/// </summary>
public class PrueflisteDialogTests : EposBunitContext
{
    public PrueflisteDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<Pruefmeldungszeile> Meldungen() => new[]
    {
        new Pruefmeldungszeile("hinweis", "Formatvorlage EPOS Hinweis ergänzt"),
        new Pruefmeldungszeile("FEHLER", "Unbekannter Platzhalter {{projekt.kundename}}",
                               "Tabelle 3, Zeile 2, Zelle beginnt mit ‚Wärme‘", "Vorschlag {{projekt.kunde}} übernehmen"),
        new Pruefmeldungszeile("warnung", "Überschriftenstile fehlen", WasTun: "Formatvorlage nutzen"),
        new Pruefmeldungszeile("fehler", "Block nicht geschlossen", "Absatz 12", "{{/je}} ergänzen"),
        new Pruefmeldungszeile("unbekannt", "Normalform: {{projekt.geaendert}}")
    };

    private IRenderedComponent<PrueflisteDialog> Zeige(
        Action<ComponentParameterCollectionBuilder<PrueflisteDialog>>? mehr = null)
        => Render<PrueflisteDialog>(p => mehr?.Invoke(p));

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_mit_Titel_Kreuz_und_ohne_Befunde()
    {
        var cut = Zeige();

        Assert.Equal("Prüfliste der Vorlage", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-dialog-zu"));
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-hilfepille"));
        Assert.Contains("Keine Befunde", cut.Find(".epos-vorlage-pruefleer").TextContent);
        Assert.Empty(cut.FindAll(".epos-vorlage-pruefkopf"));
        Assert.Empty(cut.FindAll(".epos-vorlage-meldung"));
        Assert.Equal("UcBericht.btn_Help_Pruefliste", cut.Instance.HilfeSchluessel);
    }

    [Fact]
    public void Die_Meldungen_stehen_nach_Stufe_gruppiert_mit_Zeichen_und_Zahl()
    {
        var cut = Zeige(p => p.Add(x => x.Meldungen, Meldungen()));

        var koepfe = cut.FindAll(".epos-vorlage-meldungskopf");
        Assert.Equal(new[] { "✖ Fehler (2)", "⚠ Warnungen (1)", "ℹ Hinweise (2)" },
                     koepfe.Select(k => string.Join(" ", k.Children.Select(c => c.TextContent.Trim()))));
        Assert.All(cut.FindAll(".epos-vorlage-meldungszeichen"), z => Assert.Equal("true", z.GetAttribute("aria-hidden")));

        var gruppen = cut.FindAll(".epos-vorlage-meldungsgruppe");
        Assert.Contains("epos-vorlage-meldungsgruppe--fehler", gruppen[0].ClassName);
        Assert.Contains("epos-vorlage-meldungsgruppe--warnung", gruppen[1].ClassName);
        Assert.Contains("epos-vorlage-meldungsgruppe--hinweis", gruppen[2].ClassName);

        // Eine unbekannte Stufe steht bei den Hinweisen, die Reihenfolge je Stufe bleibt.
        var fehler = gruppen[0].QuerySelectorAll(".epos-vorlage-meldungstext").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Unbekannter Platzhalter {{projekt.kundename}}", "Block nicht geschlossen" }, fehler);
        Assert.Equal(new Dictionary<string, int> { ["fehler"] = 2, ["warnung"] = 1, ["hinweis"] = 2 },
                     cut.Instance.Zahlen);

        // Fundort und „Was tun" nur, wo sie stehen.
        IElement erste = gruppen[0].QuerySelector(".epos-vorlage-meldung")!;
        Assert.Equal("Fundort: Tabelle 3, Zeile 2, Zelle beginnt mit ‚Wärme‘",
                     erste.QuerySelector(".epos-vorlage-fundort")!.TextContent);
        Assert.Equal("Was tun: Vorschlag {{projekt.kunde}} übernehmen",
                     erste.QuerySelector(".epos-vorlage-wastun")!.TextContent);
        IElement warnung = gruppen[1].QuerySelector(".epos-vorlage-meldung")!;
        Assert.Null(warnung.QuerySelector(".epos-vorlage-fundort"));
        Assert.NotNull(warnung.QuerySelector(".epos-vorlage-wastun"));

        // Die Liste steht im Rahmen mit Rollbalken und hat einen Namen.
        IElement rahmen = cut.Find(".epos-raster-huelle.epos-vorlage-meldungsrahmen");
        Assert.Equal("Meldungen der Prüfung", rahmen.GetAttribute("aria-label"));
    }

    [Fact]
    public void Die_Kopfzeile_nennt_Zahl_und_Vorlage()
    {
        var cut = Zeige(p => p.Add(x => x.Platzhalterzahl, 23).Add(x => x.Vorlagenname, "Kurzbericht Kunde"));
        Assert.Equal("Geprüft: 23 Platzhalter, Vorlage „Kurzbericht Kunde“", cut.Find(".epos-vorlage-pruefkopf").TextContent);

        var ohneName = Zeige(p => p.Add(x => x.Platzhalterzahl, 0));
        Assert.Equal("Geprüft: 0 Platzhalter", ohneName.Find(".epos-vorlage-pruefkopf").TextContent);
    }

    [Fact]
    public void Die_Fussleiste_traegt_nur_Schliessen_als_primaeren_Knopf()
    {
        var cut = Zeige(p => p.Add(x => x.Meldungen, Meldungen()));

        var leisten = cut.FindAll(".epos-leiste");
        Assert.Single(leisten);
        var knoepfe = leisten[0].QuerySelectorAll("button");
        Assert.Single(knoepfe);
        Assert.Equal("Schließen", knoepfe[0].TextContent.Trim());
        Assert.Contains("epos-knopf--primaer", knoepfe[0].ClassName);
    }

    [Fact]
    public void Schliessen_Esc_und_Kreuz_fuehren_zurueck()
    {
        int zu = 0;
        var cut = Zeige(p => p.Add(x => x.Meldungen, Meldungen()).Add(x => x.Geschlossen, () => zu++));

        cut.Find(".epos-vorlage-schliessen").Click();
        Assert.Equal(1, zu);

        cut.Find(".epos-vorlage-pruefliste").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, zu);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(3, zu);

        // Enter bleibt unbelegt.
        cut.Find(".epos-vorlage-pruefliste").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(3, zu);
    }

    [Fact]
    public void Eingebettet_zeigt_der_Dialog_weder_Titel_noch_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.Contains("epos-dialog-kopf--ohnetitel", cut.Find(".epos-dialog-kopf").ClassName);
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-hilfepille"));
    }
}

/// <summary>
/// „erklären lassen" an einer Meldung der Prüfliste — in der seriellen Sammlung, weil
/// <c>Dienste.Navigation</c> und <c>KiVerfuegbarkeit.Haken</c> prozessweit getauscht werden.
/// </summary>
[Collection("KiDialogweg")]
public class PrueflisteErklaerenTests : KiDialogwegBasis
{
    private IRenderedComponent<PrueflisteDialog> Zeige() => Render<PrueflisteDialog>(p => p
        .Add(x => x.Meldungen, new[]
        {
            new Pruefmeldungszeile("fehler", "Unbekannter Platzhalter {{projekt.kundename}}", Kennung: "VORLAGE_UNBEKANNT"),
            new Pruefmeldungszeile("warnung", "Bildrahmen unter 80 %")
        }));

    [Fact]
    public void Nur_eine_Meldung_mit_Kennung_traegt_den_Link_und_er_oeffnet_den_Assistenten()
    {
        var cut = Zeige();

        var links = cut.FindAll(".epos-vorlage-erklaeren");
        Assert.Single(links);
        Assert.Equal("erklären lassen", links[0].TextContent.Trim());
        links[0].Click();

        Assert.Equal(new[] { Masken.KiAssistent }, Navigation.Masken);
        Assert.Equal("VORLAGE_UNBEKANNT", Navigation.LetzterKontext!.Kennung);
        // Der Hilfeschlüssel der Berichtsseite führt in den Bereich Bericht.
        Assert.Equal(KiChatKontext.B_BERICHT, Navigation.LetzterKontext.Bereich);
    }

    [Fact]
    public void Ohne_moeglichen_Assistenten_steht_kein_Link()
    {
        AssistentAbschalten();
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-vorlage-erklaeren"));
    }
}

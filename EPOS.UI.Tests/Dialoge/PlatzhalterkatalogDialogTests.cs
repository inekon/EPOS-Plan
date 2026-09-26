using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Überlagerung „Platzhalterkatalog" der Berichtsvorlagen (BV-E1, Konzept 9.1 D und 9.7): die
/// Tabelle Schlüssel · Art · Kontext · Beschreibung, die Suche über Schlüssel und Beschreibung,
/// die Wahl einer Zeile mit der Schreibweise <c>{{schluessel}}</c> im nur lesbaren Feld, der
/// Leerzustand, die Suche beim Wirt und der Rückweg über Schließen, Esc und Kreuz.
///
/// <para>Kultur de-DE; alle Einträge erfunden.</para>
/// </summary>
public class PlatzhalterkatalogDialogTests : EposBunitContext
{
    public PlatzhalterkatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<Katalogzeile> Katalog() => new[]
    {
        new Katalogzeile("projekt.kunde", "Text", "Bericht", "Kunde des Projekts", "Musterfirma"),
        new Katalogzeile("projekt.name", "Text", "Bericht", "Name des Projekts"),
        new Katalogzeile("stamm.kennzahl.eff.jaz", "Zahl", "Stamm", "Jahresarbeitszahl der Wärmepumpe", "3,8"),
        new Katalogzeile("bericht.datum", "Datum", "Bericht", "Datum der Erstellung")
    };

    private IRenderedComponent<PlatzhalterkatalogDialog> Zeige(
        Action<ComponentParameterCollectionBuilder<PlatzhalterkatalogDialog>>? mehr = null)
        => Render<PlatzhalterkatalogDialog>(p => mehr?.Invoke(p));

    private static IReadOnlyList<string> Schluessel(IRenderedComponent<PlatzhalterkatalogDialog> cut)
        => cut.FindAll(".epos-vorlage-katalogtabelle tbody .epos-vorlage-katalogwahl")
              .Select(e => e.TextContent.Trim()).ToList();

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_den_leeren_Katalog()
    {
        var cut = Zeige();

        Assert.Equal("Platzhalterkatalog", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-dialog-zu"));
        Assert.Equal("Der Katalog ist leer.", cut.Find(".epos-vorlage-katalogleer").TextContent);
        Assert.Empty(cut.FindAll(".epos-vorlage-katalogtabelle"));
        Assert.Empty(cut.FindAll(".epos-vorlage-schreibweise"));
        Assert.Single(cut.FindAll(".epos-vorlage-katalogsuche input"));
        Assert.Equal("UcBericht.btn_Help_Platzhalterkatalog", cut.Instance.HilfeSchluessel);
    }

    [Fact]
    public void Die_Tabelle_fuehrt_Schluessel_Art_Kontext_und_Beschreibung()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()));

        Assert.Equal(new[] { "Schlüssel", "Art", "Kontext", "Beschreibung" },
                     cut.FindAll(".epos-vorlage-katalogtabelle thead th").Select(t => t.TextContent.Trim()));
        Assert.Equal(new[] { "projekt.kunde", "projekt.name", "stamm.kennzahl.eff.jaz", "bericht.datum" }, Schluessel(cut));

        var zellen = cut.FindAll(".epos-vorlage-katalogtabelle tbody tr")[2].QuerySelectorAll("td");
        Assert.Equal("Zahl", zellen[1].TextContent.Trim());
        Assert.Equal("Stamm", zellen[2].TextContent.Trim());
        Assert.Equal("Jahresarbeitszahl der Wärmepumpe", zellen[3].TextContent.Trim());

        Assert.Equal("4 Platzhalter", cut.Find(".epos-vorlage-katalogzahl").TextContent);
        Assert.NotNull(cut.Find(".epos-raster-huelle .epos-vorlage-katalogtabelle"));
    }

    [Theory]
    [InlineData("kunde", new[] { "projekt.kunde" })]
    [InlineData("PROJEKT", new[] { "projekt.kunde", "projekt.name" })]
    [InlineData("wärmepumpe", new[] { "stamm.kennzahl.eff.jaz" })]          // Beschreibung
    [InlineData("{{bericht.datum}}", new[] { "bericht.datum" })]             // aus einer Vorlage eingefügt
    [InlineData("projekt name", new[] { "projekt.name" })]                   // jedes Wort muss treffen
    public void Die_Suche_filtert_ueber_Schluessel_und_Beschreibung(string suche, string[] erwartet)
    {
        string? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()).Add(x => x.SucheChanged, (string s) => gemeldet = s));

        cut.Find(".epos-vorlage-katalogsuche input").Input(suche);

        Assert.Equal(erwartet, Schluessel(cut));
        Assert.Equal(suche, gemeldet);
        Assert.Equal(suche, cut.Instance.Suchtext);
        Assert.Equal(erwartet.Length + " von 4 Platzhaltern", cut.Find(".epos-vorlage-katalogzahl").TextContent);
    }

    [Fact]
    public void Ohne_Treffer_sagt_der_Katalog_es()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()));

        cut.Find(".epos-vorlage-katalogsuche input").Input("gibtesnicht");

        Assert.Equal("Kein Platzhalter passt zur Suche.", cut.Find(".epos-vorlage-katalogleer").TextContent);
        Assert.Empty(cut.FindAll(".epos-vorlage-katalogtabelle"));
        Assert.Equal("0 von 4 Platzhaltern", cut.Find(".epos-vorlage-katalogzahl").TextContent);
    }

    [Fact]
    public void Ein_Klick_auf_die_Zeile_zeigt_die_Schreibweise_nur_lesbar_mit_Beispiel()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()));

        cut.FindAll(".epos-vorlage-katalogtabelle tbody tr")[2].Click();

        Assert.Equal("stamm.kennzahl.eff.jaz", cut.Instance.GewaehlterSchluessel);
        IElement feld = cut.Find(".epos-vorlage-schreibweise input");
        Assert.Equal("{{stamm.kennzahl.eff.jaz}}", feld.GetAttribute("value"));
        Assert.True(feld.HasAttribute("readonly"));
        Assert.Contains("Beispiel: 3,8", cut.Markup);

        // Die gewählte Zeile ist sichtbar und für die Sprachausgabe gedrückt.
        IElement zeile = cut.FindAll(".epos-vorlage-katalogtabelle tbody tr")[2];
        Assert.Contains("epos-vorlage-katalogzeile--gewaehlt", zeile.ClassName);
        Assert.Equal("true", zeile.QuerySelector(".epos-vorlage-katalogwahl")!.GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.FindAll(".epos-vorlage-katalogwahl")[0].GetAttribute("aria-pressed"));

        // Der Schlüssel als Knopf wählt ebenso (Tastatur); ohne Beispiel keine Beispielzeile.
        cut.FindAll(".epos-vorlage-katalogwahl")[1].Click();
        Assert.Equal("{{projekt.name}}", cut.Find(".epos-vorlage-schreibweise input").GetAttribute("value"));
        Assert.DoesNotContain("Beispiel:", cut.Markup);
    }

    /// <summary>
    /// „markiert" (Konzept 9.6): Nach der Wahl sucht der Dialog das Eingabeelement unter der
    /// Kennung seiner Hülle über <c>document.querySelector</c> — ein Pfad über ein OBJEKT, wie
    /// ihn die JS-Brücke von .NET 10 allein auflöst — und ruft dessen <c>select()</c>.
    /// </summary>
    [Fact]
    public void Nach_der_Wahl_wird_die_Schreibweise_ueber_document_querySelector_markiert()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()));

        cut.FindAll(".epos-vorlage-katalogwahl")[0].Click();

        string kennung = cut.Find(".epos-vorlage-schreibweise").GetAttribute("id")!;
        Assert.StartsWith("epos-vorlage-schreibweise-", kennung);
        cut.WaitForAssertion(() =>
        {
            var suche = Assert.Single(JSInterop.Invocations, i => i.Identifier == "document.querySelector");
            Assert.Equal("#" + kennung + " input", suche.Arguments.Single());
            Assert.Single(JSInterop.Invocations, i => i.Identifier == "select");
        });
    }

    [Fact]
    public void Die_Wahl_bleibt_beim_Filtern_stehen()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()));

        cut.FindAll(".epos-vorlage-katalogwahl")[0].Click();
        cut.Find(".epos-vorlage-katalogsuche input").Input("datum");

        Assert.Equal("projekt.kunde", cut.Instance.GewaehlterSchluessel);
        Assert.Equal("{{projekt.kunde}}", cut.Find(".epos-vorlage-schreibweise input").GetAttribute("value"));
    }

    [Fact]
    public void Ein_neuer_Suchwert_des_Wirts_ersetzt_die_Suche()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()).Add(x => x.Suche, "kunde"));
        Assert.Equal(new[] { "projekt.kunde" }, Schluessel(cut));
        Assert.Equal("kunde", cut.Find(".epos-vorlage-katalogsuche input").GetAttribute("value"));

        cut.Render(p => p.Add(x => x.Suche, "datum"));
        Assert.Equal(new[] { "bericht.datum" }, Schluessel(cut));
    }

    [Fact]
    public void Die_Fussleiste_traegt_nur_Schliessen_als_primaeren_Knopf()
    {
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()));

        var leisten = cut.FindAll(".epos-leiste");
        Assert.Single(leisten);
        var knoepfe = leisten[0].QuerySelectorAll("button");
        Assert.Single(knoepfe);
        Assert.Equal("Schließen", knoepfe[0].TextContent.Trim());
        Assert.Contains("epos-knopf--primaer", knoepfe[0].ClassName);
    }

    /// <summary>
    /// „Baukasten speichern…" (BV-E5, Konzept 9.7: „Baukasten speichern" · Füller · „Schließen"): links in der
    /// Fußleiste, nur mit dem Weg des Wirts, nicht primär; die Meldung des Wegs steht im Füller, ein Abbruch
    /// (<c>""</c>) lässt ihn leer, und der primäre Knopf bleibt „Schließen".
    /// </summary>
    [Fact]
    public void Baukasten_speichern_steht_links_und_meldet_im_Fueller()
    {
        int aufrufe = 0;
        string antwort = "Baukasten gespeichert: /tmp/Baukasten.docx";
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog())
                              .Add(x => x.BaukastenSpeichern, () => { aufrufe++; return Task.FromResult(antwort); }));

        var knoepfe = cut.Find(".epos-leiste").QuerySelectorAll("button");
        Assert.Equal(new[] { "Baukasten speichern…", "Schließen" }, knoepfe.Select(k => k.TextContent.Trim()));
        Assert.DoesNotContain("epos-knopf--primaer", knoepfe[0].ClassName);
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName);
        Assert.Equal("", cut.Find(".epos-leiste .epos-status").TextContent.Trim());

        cut.Find(".epos-vorlage-baukasten").Click();
        Assert.Equal(1, aufrufe);
        Assert.Equal(antwort, cut.Find(".epos-leiste .epos-status").TextContent.Trim());
        Assert.Equal(antwort, cut.Instance.Baukastenmeldung);

        antwort = "";
        cut.Find(".epos-vorlage-baukasten").Click();
        Assert.Equal(2, aufrufe);
        Assert.Equal("", cut.Find(".epos-leiste .epos-status").TextContent.Trim());
    }

    [Fact]
    public void Schliessen_Esc_und_Kreuz_fuehren_zurueck_und_eingebettet_fehlt_der_Kopf()
    {
        int zu = 0;
        var cut = Zeige(p => p.Add(x => x.Eintraege, Katalog()).Add(x => x.Geschlossen, () => zu++));

        cut.Find(".epos-vorlage-schliessen").Click();
        cut.Find(".epos-vorlage-katalog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(3, zu);

        var eingebettet = Zeige(p => p.Add(x => x.TitelAnzeigen, false));
        Assert.Empty(eingebettet.FindAll(".epos-dialog-titel"));
        Assert.Empty(eingebettet.FindAll(".epos-dialog-zu"));
        Assert.Single(eingebettet.FindAll(".epos-dialog-kopf .epos-hilfepille"));
    }

    [Fact]
    public void Die_Schreibweise_steht_in_doppelten_Klammern()
    {
        Assert.Equal("{{projekt.kunde}}", Platzhalterkatalogsuche.Schreibweise(" projekt.kunde "));
        Assert.Equal("{{}}", Platzhalterkatalogsuche.Schreibweise(null));
        Assert.Empty(Platzhalterkatalogsuche.Filtern(null, "x"));
        Assert.Equal(4, Platzhalterkatalogsuche.Filtern(Katalog(), "   ").Count);
    }
}

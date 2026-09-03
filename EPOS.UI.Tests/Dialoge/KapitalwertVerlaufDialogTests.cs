using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Kapitalwert-Verlauf (iU9-W1.6). Soll ist die Feldkarte von
/// <c>Form_WirtschaftlichkeitVerlauf</c>: Zeitraum (2..60), Szenario,
/// "Aktualisieren", zwei Bilder, Restwertzeile, Statuszeile, "Schliessen".
/// </summary>
public class KapitalwertVerlaufDialogTests : BunitContext
{
    private static readonly (int Id, string Text)[] Szenarien =
    {
        (0, "Erwartet"),
        (1, "Best"),
        (2, "Worst")
    };

    private static readonly byte[] PngA = { 1, 2, 3 };
    private static readonly byte[] PngB = { 4, 5, 6 };

    public KapitalwertVerlaufDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static KapitalwertVerlaufBilder Ergebnis(int jahre, string szenario) =>
        new(PngA, PngB, "Restwert-Barwerte …", "Verlauf über " + jahre + " Jahre, Szenario „" + szenario + "“.");

    private IRenderedComponent<KapitalwertVerlaufDialog> Aufbauen(
        Func<int, int, CancellationToken, Task<KapitalwertVerlaufBilder>>? berechnen = null,
        Action? beimSchliessen = null,
        int jahreVorgabe = 20)
    {
        return Render<KapitalwertVerlaufDialog>(p => p
            .Add(x => x.Szenarien, Szenarien)
            .Add(x => x.JahreVorgabe, jahreVorgabe)
            .Add(x => x.Berechnen, berechnen ??
                ((jahre, szenario, _) => Task.FromResult(Ergebnis(jahre, Szenarien[szenario].Text))))
            .Add(x => x.Geschlossen, () => beimSchliessen?.Invoke()));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll("input[type=text]"));        // Zeitraum
        Assert.Single(cut.FindAll("select"));                  // Szenario
        // Aktualisieren + Schliessen.
        Assert.Equal(2, cut.FindAll("button.epos-knopf").Count);
        // Zwei Bilder (nach dem Lauf beim Oeffnen).
        Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);
    }

    [Fact]
    public void Die_Maske_zeigt_die_heutigen_Beschriftungen()
    {
        var cut = Aufbauen();

        Assert.Equal("Kapitalwert-Verlauf über den Nutzungszeitraum",
                     cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Zeitraum [Jahre]:", texte[0].TextContent);
        Assert.Equal("Szenario:", texte[1].TextContent);
        Assert.Equal("Aktualisieren", cut.FindAll("button.epos-knopf")[0].TextContent);
    }

    [Fact]
    public void Die_Vorgabe_des_Zeitraums_wird_uebernommen()
    {
        // ParameterVorbelegen: der gespeicherte Betrachtungszeitraum.
        var cut = Aufbauen(jahreVorgabe: 35);

        Assert.Equal("35", cut.Find("input[type=text]").GetAttribute("value"));
    }

    [Fact]
    public void Die_Vorgabe_wird_auf_den_Bereich_geklemmt()
    {
        Assert.Equal("2", Aufbauen(jahreVorgabe: 0).Find("input[type=text]").GetAttribute("value"));
        Assert.Equal("60", Aufbauen(jahreVorgabe: 99).Find("input[type=text]").GetAttribute("value"));
    }

    [Fact]
    public void Der_Dialog_rechnet_schon_beim_Oeffnen()
    {
        // Form_WirtschaftlichkeitVerlauf_Load rief btnZeichnen_Click.
        int laeufe = 0;
        var cut = Aufbauen(berechnen: (jahre, szenario, _) =>
        {
            laeufe++;
            return Task.FromResult(Ergebnis(jahre, Szenarien[szenario].Text));
        });

        Assert.Equal(1, laeufe);
        Assert.Equal(2, cut.FindAll("img.epos-chartbild").Count);
    }

    [Fact]
    public void Die_Bilder_kommen_als_data_URL_in_die_Seite()
    {
        var cut = Aufbauen();

        var bilder = cut.FindAll("img.epos-chartbild");
        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(PngA),
                     bilder[0].GetAttribute("src"));
        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(PngB),
                     bilder[1].GetAttribute("src"));
    }

    [Fact]
    public void Restwert_und_Statuszeile_stehen_unter_den_Bildern()
    {
        var cut = Aufbauen();

        var zeilen = cut.FindAll(".epos-herleitung-text");
        Assert.Equal("Restwert-Barwerte …", zeilen[0].TextContent);
        Assert.Equal("Verlauf über 20 Jahre, Szenario „Erwartet“.", zeilen[1].TextContent);
    }

    [Fact]
    public void Aktualisieren_rechnet_mit_Jahren_und_Szenario()
    {
        int erhalteneJahre = 0;
        int erhaltenesSzenario = -1;
        var cut = Aufbauen(berechnen: (jahre, szenario, _) =>
        {
            erhalteneJahre = jahre;
            erhaltenesSzenario = szenario;
            return Task.FromResult(Ergebnis(jahre, Szenarien[szenario].Text));
        });

        cut.Find("input[type=text]").Input("30");
        cut.Find("select").Change("2");
        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.Equal(30, erhalteneJahre);
        Assert.Equal(2, erhaltenesSzenario);
        Assert.Contains("Worst", cut.FindAll(".epos-herleitung-text")[1].TextContent);
    }

    [Fact]
    public void Waehrend_der_Rechnung_sind_die_Eingaben_gesperrt()
    {
        // SetBusy: numJahre/cbSzenario/btnZeichnen gesperrt, Schliessen heisst
        // "Abbrechen".
        var tor = new TaskCompletionSource<KapitalwertVerlaufBilder>();
        var cut = Aufbauen(berechnen: (_, _, _) => tor.Task);

        Assert.True(cut.Instance.Laeuft);
        Assert.True(cut.FindAll("button.epos-knopf")[0].HasAttribute("disabled"));
        Assert.True(cut.Find("select").HasAttribute("disabled"));
        Assert.Equal("Abbrechen", cut.Find(".epos-knopf--primaer").TextContent);
        Assert.Equal("Berechnung läuft …", cut.Instance.Status);

        tor.SetResult(Ergebnis(20, "Erwartet"));
        cut.WaitForAssertion(() => Assert.False(cut.Instance.Laeuft));
        Assert.Equal("Schließen", cut.Find(".epos-knopf--primaer").TextContent);
    }

    [Fact]
    public void Der_Knopf_bricht_die_laufende_Rechnung_ab_und_schliesst_nicht()
    {
        bool geschlossen = false;
        var tor = new TaskCompletionSource<KapitalwertVerlaufBilder>();
        CancellationToken merker = default;
        var cut = Aufbauen(
            berechnen: (_, _, ct) => { merker = ct; return tor.Task; },
            beimSchliessen: () => geschlossen = true);

        cut.Find(".epos-knopf--primaer").Click();

        Assert.True(merker.IsCancellationRequested);
        Assert.False(geschlossen);

        tor.SetCanceled(merker);
        cut.WaitForAssertion(() => Assert.Equal("Vorgang abgebrochen.", cut.Instance.Status));
    }

    [Fact]
    public void Ein_Fehler_erscheint_als_Warnbanner()
    {
        // Frueher eine MessageBox ("Fehler beim Berechnen des Verlaufs: …").
        var cut = Aufbauen(berechnen: (_, _, _) =>
            Task.FromException<KapitalwertVerlaufBilder>(new InvalidOperationException("kein Ergebnis")));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-warnbanner")));
        Assert.Equal("Fehler beim Berechnen des Verlaufs: kein Ergebnis",
                     cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Schliessen_und_Esc_melden_das_Ende()
    {
        int gemeldet = 0;
        var cut = Aufbauen(beimSchliessen: () => gemeldet++);

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(1, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gemeldet);

        // Enter bleibt unbelegt.
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(2, gemeldet);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_WirtschaftlichkeitVerlauf.btn_Help" }, hilfe.Geoeffnet);
    }
}

using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// BV-E2 (Anwenderentscheid BV-E2-1, Lesart b) — <b>das Logo der Rubrik „Bericht"</b>: der Pfad einer
/// PNG- oder JPEG-Datei unter dem Vorlagenordner, „Durchsuchen…" über die Dateiwahl der Hülle,
/// „Entfernen" leert das Feld, eine fehlende Datei nennt der Hinweis „Datei nicht gefunden" — der Pfad
/// geht trotzdem hinaus —, die Übergabe erst im OK-Weg und nur geändert, nichts auf Abbrechen,
/// „Standardwerte" leert, ohne Rückweg kein Feld, und die Sicht des Assistenten.
///
/// <para>Kultur de-DE; alle Werte erfunden.</para>
/// </summary>
public class EinstellungenBerichtLogoTests : EposBunitContext
{
    public EinstellungenBerichtLogoTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const string LOGO = @"C:\Logos\firma.png";
    private const string NEU = @"D:\Bilder\neu.jpg";
    private const string FEHLT = @"C:\Logos\fehlt.png";

    private static Einstellungensatz Satz() => new Einstellungensatz
    {
        VdiPfad = @"C:\Daten\VDI",
        DbExportPfad = @"C:\Daten\Export",
        DbImportPfad = @"C:\Daten\Import",
        DbPfad = @"C:\ProgramData\EPOS_PLAN",
        DbName = "Kenndaten.sqlite",
        AllgemeinPfad = @"C:\Daten"
    };

    private readonly List<string> _protokoll = new();
    private string? _filter;

    private IRenderedComponent<EinstellungenDialog> Zeige(
        string logo = LOGO,
        bool mitWaehler = true,
        bool mitLogo = true,
        Action<ComponentParameterCollectionBuilder<EinstellungenDialog>>? mehr = null)
        => Render<EinstellungenDialog>(p =>
        {
            p.Add(x => x.Satz, Satz())
             .Add(x => x.Farbrollen, Diagrammfarben.Gaben())
             .Add(x => x.Speichern, (Einstellungensatz _, bool _) =>
             {
                 _protokoll.Add("speichern");
                 return Task.FromResult(new SpeicherBefund(true, ""));
             })
             .Add(x => x.Zuruecksetzen, () => Task.FromResult(new Einstellungensatz()))
             .Add(x => x.Geschlossen, (bool ok) => _protokoll.Add("geschlossen:" + ok))
             .Add(x => x.Firma, "Musterbüro GmbH")
             .Add(x => x.FirmaChanged, (string f) => _protokoll.Add("firma:" + f))
             .Add(x => x.Vorlagenordner, @"C:\Users\x\Documents\EPOS-Plan\Berichtsvorlagen")
             .Add(x => x.VorlagenordnerChanged, (string o) => _protokoll.Add("ordner:" + o))
             .Add(x => x.OrdnerWaehler, (string _) => Task.FromResult<string?>(@"\\server\buero\Vorlagen"));
            if (mitLogo)
            {
                p.Add(x => x.Logo, logo)
                 .Add(x => x.LogoChanged, (string l) => _protokoll.Add("logo:" + l))
                 .Add(x => x.LogoVorhanden, (Func<string, bool>)(pfad => pfad != FEHLT));
                if (mitWaehler)
                    p.Add(x => x.LogoWaehler, (string filter) =>
                    {
                        _filter = filter;
                        return Task.FromResult<string?>(NEU);
                    });
            }
            mehr?.Invoke(p);
        });

    private static void Bericht(IRenderedComponent<EinstellungenDialog> cut)
        => cut.FindAll(".epos-reiter-knopf").First(k => k.TextContent.Trim() == "Bericht").Click();

    private static IElement Logofeld(IRenderedComponent<EinstellungenDialog> cut)
        => cut.Find(".epos-bericht-logo .epos-dateiwahl input");

    private static void Ok(IRenderedComponent<EinstellungenDialog> cut) => cut.Find(".epos-knopf--primaer").Click();

    [Fact]
    public void Das_Logo_steht_unter_dem_Vorlagenordner_mit_Durchsuchen_und_Entfernen()
    {
        var cut = Zeige();
        Bericht(cut);

        var felder = cut.FindAll(".epos-dateiwahl input");
        Assert.Equal(2, felder.Count);                                      // Vorlagenordner, dann Logo
        Assert.Equal(LOGO, felder[1].GetAttribute("value"));
        Assert.False(Logofeld(cut).HasAttribute("readonly"));
        Assert.Contains("Logo:", cut.Find(".epos-bericht-logo .epos-feld-text").TextContent);
        Assert.Equal("Durchsuchen…", cut.Find(".epos-bericht-logo .epos-dateiwahl button").TextContent.Trim());
        Assert.Equal("Entfernen", cut.Find(".epos-bericht-logo-entfernen").TextContent.Trim());
        Assert.Contains("Firmenlogo für die Kopfzeile des Berichts", cut.Markup);
        Assert.DoesNotContain("Datei nicht gefunden.", cut.Markup);
        Assert.True(cut.Instance.LogoSichtbar);
    }

    [Fact]
    public void Durchsuchen_und_OK_uebergeben_das_neue_Logo_nach_dem_Speichern()
    {
        var cut = Zeige();
        Bericht(cut);

        cut.Find(".epos-bericht-logo .epos-dateiwahl button").Click();
        Assert.Equal(NEU, Logofeld(cut).GetAttribute("value"));
        Assert.NotNull(_filter);

        Ok(cut);
        Assert.Equal(new[] { "speichern", "logo:" + NEU, "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void Entfernen_leert_das_Feld_und_OK_meldet_ohne_Logo()
    {
        var cut = Zeige();
        Bericht(cut);

        cut.Find(".epos-bericht-logo-entfernen").Click();

        Assert.Equal("", Logofeld(cut).GetAttribute("value") ?? "");
        Assert.Empty(cut.FindAll(".epos-bericht-logo-entfernen"));        // leer: nichts zu entfernen
        Assert.Equal("", cut.Instance.LogoArbeitsstand);

        Ok(cut);
        Assert.Equal(new[] { "speichern", "logo:", "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void Eine_fehlende_Datei_nennt_der_Hinweis_und_der_Pfad_geht_trotzdem_hinaus()
    {
        var cut = Zeige();
        Bericht(cut);

        Logofeld(cut).Input(FEHLT);

        Assert.True(cut.Instance.LogoFehltHinweis);
        Assert.Contains("Datei nicht gefunden.", cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent.Trim()));

        Ok(cut);
        Assert.Equal(new[] { "speichern", "logo:" + FEHLT, "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void Ohne_Aenderung_und_auf_Abbrechen_geht_kein_Logo_hinaus()
    {
        var ohne = Zeige();
        Ok(ohne);
        Assert.Equal(new[] { "speichern", "geschlossen:True" }, _protokoll);

        _protokoll.Clear();
        var cut = Zeige();
        Bericht(cut);
        Logofeld(cut).Input(NEU);
        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.Equal(new[] { "geschlossen:False" }, _protokoll);
    }

    [Fact]
    public void Ohne_Dateiwahl_bleibt_der_Pfad_tippbar_ohne_Durchsuchen()
    {
        var cut = Zeige(mitWaehler: false);
        Bericht(cut);

        Assert.Empty(cut.FindAll(".epos-bericht-logo .epos-dateiwahl button"));
        Logofeld(cut).Input(NEU);
        Ok(cut);
        Assert.Contains("logo:" + NEU, _protokoll);
    }

    [Fact]
    public void Ohne_Rueckweg_steht_kein_Logo_da_und_der_Assistent_lehnt_ab()
    {
        var cut = Zeige(mitLogo: false);
        Bericht(cut);

        Assert.True(cut.Instance.BerichtSichtbar);
        Assert.False(cut.Instance.LogoSichtbar);
        Assert.Empty(cut.FindAll(".epos-bericht-logo"));
        Assert.Single(cut.FindAll(".epos-dateiwahl input"));               // nur der Vorlagenordner

        var fehler = Assert.Throws<InvalidOperationException>(() => cut.Instance.Assistentensicht.BerichtLogo = LOGO);
        Assert.Contains("„Bericht“", fehler.Message);
    }

    [Fact]
    public void Das_Logo_allein_als_Rueckweg_traegt_die_Rubrik()
    {
        var cut = Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, Satz())
            .Add(x => x.LogoChanged, (string _) => { }));

        Assert.True(cut.Instance.BerichtSichtbar);
        Bericht(cut);
        Assert.Single(cut.FindAll(".epos-bericht-logo"));
    }

    [Fact]
    public void Standardwerte_leeren_das_Logo()
    {
        var cut = Zeige();
        Bericht(cut);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.Equal("", cut.Instance.LogoArbeitsstand);
        Assert.Empty(_protokoll);                                         // Standardwerte speichern nicht
    }

    [Fact]
    public async Task Der_Assistent_liest_und_setzt_das_Logo_im_Arbeitsstand()
    {
        var cut = Zeige();
        EinstellungenKiSicht sicht = cut.Instance.Assistentensicht;

        Assert.Equal(LOGO, sicht.BerichtLogo);
        sicht.BerichtLogo = NEU;
        Assert.Equal(NEU, cut.Instance.LogoArbeitsstand);

        KiKern.KiErgebnis ergebnis = await KiMaskenbruecke.Haken(KiMaskennamen.EINSTELLUNGEN).Speichern!();
        Assert.True(ergebnis.Erfolg, ergebnis.Text);
        Assert.Equal(new[] { "speichern", "logo:" + NEU }, _protokoll);
    }
}

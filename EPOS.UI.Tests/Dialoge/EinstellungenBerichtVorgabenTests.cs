using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die zwei Vorgaben der Installation in der Rubrik „Bericht"</b> (Konzept Berichtsvorlagen
/// 10.3): die vorgegebene Word- und Excel-Vorlage als Auswahlfeld — Listen und Wahl der Hülle,
/// die Übergabe erst im OK-Weg nach dem gelungenen Speichern, nichts auf Abbrechen,
/// „Standardwerte", eine gespeicherte Vorgabe, deren Datei es nicht mehr gibt (gesperrt,
/// gewählt, mit dem Grund darunter) und die Sicht des Assistenten. Firma, Vorlagenordner und
/// Logo hält <see cref="EinstellungenBerichtTests"/> bzw. <see cref="EinstellungenBerichtLogoTests"/>.
///
/// <para>Kultur de-DE; alle Werte erfunden.</para>
/// </summary>
public class EinstellungenBerichtVorgabenTests : EposBunitContext
{
    /// <summary>Die Standardvorlage — die Vorgabe ohne eigene Einstellung.</summary>
    private const int ID_STANDARD = 1;

    /// <summary>Eine eigene Word-Vorlage des Vorlagenordners.</summary>
    private const int ID_EIGEN = 2;

    /// <summary>„Ohne Vorlage (EPOS-Plan)" — die Vorgabe ohne eigene Einstellung.</summary>
    private const int ID_OHNE = 11;

    /// <summary>Eine eigene Excel-Vorlage des Vorlagenordners.</summary>
    private const int ID_EXCEL = 12;

    /// <summary>Die gespeicherte Vorgabe, deren Datei es nicht mehr gibt.</summary>
    private const int ID_WEG = 3;

    private const string GRUND_WEG = "„Angebot“ nicht vorhanden – „Standardvorlage (EPOS-Plan)“ verwendet";

    public EinstellungenBerichtVorgabenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static Einstellungensatz Satz() => new Einstellungensatz
    {
        VdiPfad = @"C:\Daten\VDI",
        DbPfad = @"C:\ProgramData\EPOS_PLAN",
        DbName = "Kenndaten.sqlite",
        AllgemeinPfad = @"C:\Daten"
    };

    /// <summary>Was die Hülle mitschreibt — in der Reihenfolge der Aufrufe.</summary>
    private readonly List<string> _protokoll = new();

    private static IReadOnlyList<Vorlagenzeile> WordListe(bool mitFehlender = false)
    {
        var zeilen = new List<Vorlagenzeile>
        {
            new(ID_STANDARD, "Standardvorlage (EPOS-Plan)", false, "", true),
            new(ID_EIGEN, "Büro Nord")
        };
        if (mitFehlender) zeilen.Add(new Vorlagenzeile(ID_WEG, "Angebot", true, GRUND_WEG));
        return zeilen;
    }

    private static IReadOnlyList<Vorlagenzeile> ExcelListe() => new List<Vorlagenzeile>
    {
        new(ID_OHNE, "Ohne Vorlage (EPOS-Plan)", false, "", true),
        new(ID_EXCEL, "Kennzahlen")
    };

    private IRenderedComponent<EinstellungenDialog> Zeige(
        int? word = ID_STANDARD,
        int? excel = ID_OHNE,
        bool mitFehlender = false,
        bool speichernGelingt = true,
        Action<ComponentParameterCollectionBuilder<EinstellungenDialog>>? mehr = null)
        => Render<EinstellungenDialog>(p =>
        {
            p.Add(x => x.Satz, Satz())
             .Add(x => x.Farbrollen, Diagrammfarben.Gaben())
             .Add(x => x.Speichern, (Einstellungensatz _, bool _) =>
             {
                 _protokoll.Add("speichern");
                 return Task.FromResult(new SpeicherBefund(speichernGelingt, speichernGelingt ? "" : "Ordner nicht anlegbar."));
             })
             .Add(x => x.Zuruecksetzen, () => Task.FromResult(new Einstellungensatz()))
             .Add(x => x.Geschlossen, (bool ok) => _protokoll.Add("geschlossen:" + ok))
             .Add(x => x.VorgabeWordVorlagen, WordListe(mitFehlender))
             .Add(x => x.VorgabeWord, word)
             .Add(x => x.VorgabeWordStandard, ID_STANDARD)
             .Add(x => x.VorgabeWordChanged, (int? id) => _protokoll.Add("word:" + Text(id)))
             .Add(x => x.VorgabeExcelVorlagen, ExcelListe())
             .Add(x => x.VorgabeExcel, excel)
             .Add(x => x.VorgabeExcelStandard, ID_OHNE)
             .Add(x => x.VorgabeExcelChanged, (int? id) => _protokoll.Add("excel:" + Text(id)));
            mehr?.Invoke(p);
        });

    private static string Text(int? id) => id.HasValue ? id.Value.ToString(CultureInfo.InvariantCulture) : "-";

    /// <summary>Betritt die Rubrik „Bericht" über ihren Reiterknopf.</summary>
    private static void Bericht(IRenderedComponent<EinstellungenDialog> cut)
        => cut.FindAll(".epos-reiter-knopf").First(k => k.TextContent.Trim() == "Bericht").Click();

    private static IElement Feld(IRenderedComponent<EinstellungenDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-feld").First(l => l.TextContent.Contains(beschriftung)).QuerySelector("select")!;

    private static IElement Wordfeld(IRenderedComponent<EinstellungenDialog> cut)
        => Feld(cut, "Vorgabe Word-Vorlage:");

    private static IElement Excelfeld(IRenderedComponent<EinstellungenDialog> cut)
        => Feld(cut, "Vorgabe Excel-Vorlage:");

    private static void Ok(IRenderedComponent<EinstellungenDialog> cut) => cut.Find(".epos-knopf--primaer").Click();

    [Fact]
    public void Die_Rubrik_zeigt_beide_Listen_mit_der_Wahl_und_dem_Hinweis()
    {
        var cut = Zeige(word: ID_EIGEN, excel: ID_EXCEL);
        Bericht(cut);

        Assert.Equal(new[] { "Standardvorlage (EPOS-Plan)", "Büro Nord" },
                     Wordfeld(cut).QuerySelectorAll("option").Select(o => o.TextContent));
        Assert.Equal(new[] { "Ohne Vorlage (EPOS-Plan)", "Kennzahlen" },
                     Excelfeld(cut).QuerySelectorAll("option").Select(o => o.TextContent));

        Assert.Equal("Büro Nord", Gewaehlt(Wordfeld(cut)));
        Assert.Equal("Kennzahlen", Gewaehlt(Excelfeld(cut)));
        Assert.True(cut.Instance.VorgabeWordSichtbar);
        Assert.True(cut.Instance.VorgabeExcelSichtbar);

        // Die Zeile sagt, dass ein Stammprojekt auf der Berichtsseite abweichen kann.
        Assert.Contains("dort kann ein Stammprojekt abweichen", cut.Markup);
    }

    private static string Gewaehlt(IElement feld)
        => feld.QuerySelectorAll("option").First(o => o.HasAttribute("selected")).TextContent;

    [Fact]
    public void Ohne_Rueckweg_und_ohne_Liste_steht_kein_Vorgabefeld()
    {
        // Kein Rückruf: kein Feld — und mit ihm keine Rubrik, denn die übrigen Rückwege fehlen auch.
        var ohneRueckweg = Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, Satz())
            .Add(x => x.VorgabeWordVorlagen, WordListe())
            .Add(x => x.VorgabeExcelVorlagen, ExcelListe()));
        Assert.False(ohneRueckweg.Instance.VorgabeWordSichtbar);
        Assert.False(ohneRueckweg.Instance.VorgabeExcelSichtbar);
        Assert.False(ohneRueckweg.Instance.BerichtSichtbar);

        // Rückruf, aber leere Liste: ebenfalls kein Feld.
        var ohneListe = Render<EinstellungenDialog>(p => p
            .Add(x => x.Satz, Satz())
            .Add(x => x.VorgabeWordChanged, (int? _) => { })
            .Add(x => x.VorgabeExcelChanged, (int? _) => { }));
        Assert.False(ohneListe.Instance.VorgabeWordSichtbar);
        Assert.False(ohneListe.Instance.VorgabeExcelSichtbar);
    }

    [Fact]
    public void OK_uebergibt_eine_geaenderte_Vorgabe_nach_dem_Speichern_und_nur_einmal()
    {
        var cut = Zeige();
        Bericht(cut);

        Wordfeld(cut).Change(ID_EIGEN.ToString(CultureInfo.InvariantCulture));
        Excelfeld(cut).Change(ID_EXCEL.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(ID_EIGEN, cut.Instance.VorgabeWordArbeitsstand);
        Assert.Equal(ID_EXCEL, cut.Instance.VorgabeExcelArbeitsstand);
        Assert.Empty(_protokoll);            // vor OK geht nichts hinaus

        Ok(cut);

        Assert.Equal(new[] { "speichern", "word:2", "excel:12", "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void OK_ohne_Aenderung_meldet_keine_Vorgabe()
    {
        var cut = Zeige();

        Ok(cut);

        Assert.Equal(new[] { "speichern", "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void Abbrechen_verwirft_die_Wahl()
    {
        var cut = Zeige();
        Bericht(cut);
        Wordfeld(cut).Change(ID_EIGEN.ToString(CultureInfo.InvariantCulture));

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.Equal(new[] { "geschlossen:False" }, _protokoll);
    }

    [Fact]
    public void Scheitert_das_Speichern_geht_keine_Vorgabe_hinaus()
    {
        var cut = Zeige(speichernGelingt: false);
        Bericht(cut);
        Wordfeld(cut).Change(ID_EIGEN.ToString(CultureInfo.InvariantCulture));

        Ok(cut);

        Assert.Equal(new[] { "speichern" }, _protokoll);
        Assert.Equal(ID_EIGEN, cut.Instance.VorgabeWordArbeitsstand);
    }

    [Fact]
    public void Eine_nicht_mehr_vorhandene_Vorgabe_steht_gesperrt_und_gewaehlt_mit_ihrem_Grund()
    {
        var cut = Zeige(word: ID_WEG, mitFehlender: true);
        Bericht(cut);

        IElement feld = Wordfeld(cut);
        IElement weg = feld.QuerySelectorAll("option").First(o => o.TextContent == "Angebot");
        Assert.True(weg.HasAttribute("disabled"));
        Assert.True(weg.HasAttribute("selected"));
        Assert.Equal(GRUND_WEG, weg.GetAttribute("title"));

        // Der Grund steht auch unter dem Feld - derselbe Satz, den der Kern beim Erstellen nennt.
        Assert.Contains(GRUND_WEG, cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent));

        // Eine andere Vorlage wählen geht; die fehlende bleibt in der Liste stehen.
        Wordfeld(cut).Change(ID_STANDARD.ToString(CultureInfo.InvariantCulture));
        Ok(cut);
        Assert.Equal(new[] { "speichern", "word:1", "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void Standardwerte_setzen_die_Vorgaben_auf_Standardvorlage_und_ohne_Vorlage()
    {
        var cut = Zeige(word: ID_EIGEN, excel: ID_EXCEL);
        Bericht(cut);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.Equal(ID_STANDARD, cut.Instance.VorgabeWordArbeitsstand);
        Assert.Equal(ID_OHNE, cut.Instance.VorgabeExcelArbeitsstand);
        Assert.Empty(_protokoll);   // Standardwerte speichern nicht
    }

    [Fact]
    public void Der_Assistent_liest_und_setzt_die_zwei_Vorgaben_und_lehnt_Gesperrtes_ab()
    {
        var cut = Zeige(mitFehlender: true);
        EinstellungenKiSicht sicht = cut.Instance.Assistentensicht;

        Assert.Equal(ID_STANDARD, sicht.BerichtVorgabeWord);
        Assert.Equal(ID_OHNE, sicht.BerichtVorgabeExcel);
        Assert.Equal(new[] { "Standardvorlage (EPOS-Plan)", "Büro Nord", "Angebot" },
                     sicht.BerichtVorgabeWordWahl.Select(e => e.Text));
        Assert.Equal(new[] { "Ohne Vorlage (EPOS-Plan)", "Kennzahlen" },
                     sicht.BerichtVorgabeExcelWahl.Select(e => e.Text));

        sicht.BerichtVorgabeWord = ID_EIGEN;
        sicht.BerichtVorgabeExcel = ID_EXCEL;
        Assert.Equal(ID_EIGEN, cut.Instance.VorgabeWordArbeitsstand);
        Assert.Equal(ID_EXCEL, cut.Instance.VorgabeExcelArbeitsstand);

        // Ein gesperrter Eintrag und eine unbekannte Id werden BENANNT abgelehnt.
        var gesperrt = Assert.Throws<InvalidOperationException>(() => sicht.BerichtVorgabeWord = ID_WEG);
        Assert.Equal(GRUND_WEG, gesperrt.Message);
        var unbekannt = Assert.Throws<InvalidOperationException>(() => sicht.BerichtVorgabeExcel = 99);
        Assert.Equal("Diese Vorlage ist nicht wählbar.", unbekannt.Message);
        Assert.Equal(ID_EIGEN, cut.Instance.VorgabeWordArbeitsstand);
        Assert.Equal(ID_EXCEL, cut.Instance.VorgabeExcelArbeitsstand);
    }

    [Fact]
    public void Ohne_Felder_lehnt_der_Assistent_benannt_ab()
    {
        var cut = Render<EinstellungenDialog>(p => p.Add(x => x.Satz, Satz()));

        var wort = Assert.Throws<InvalidOperationException>(
            () => cut.Instance.Assistentensicht.BerichtVorgabeWord = ID_STANDARD);
        Assert.Contains("„Bericht“", wort.Message);
        var mappe = Assert.Throws<InvalidOperationException>(
            () => cut.Instance.Assistentensicht.BerichtVorgabeExcel = ID_OHNE);
        Assert.Contains("„Bericht“", mappe.Message);
    }
}

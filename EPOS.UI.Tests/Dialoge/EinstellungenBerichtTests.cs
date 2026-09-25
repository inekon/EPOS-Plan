using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// BV-E1 (Konzept Berichtsvorlagen 10.3) — die Rubrik „Bericht" der Programmeinstellungen: Firma
/// und Vorlagenordner im Arbeitsstand, die Übergabe erst im OK-Weg nach dem gelungenen Speichern,
/// nichts auf Abbrechen, Kreuz und Esc, „Standardwerte", die weiche Sperre des Ordners (iOS) mit
/// Grund und die Sicht des Assistenten. Ohne Rückweg der Hülle steht die Rubrik nicht da — die
/// übrigen sechs Rubriken hält <see cref="EinstellungenDialogTests"/>.
///
/// <para>Kultur de-DE; alle Werte erfunden.</para>
/// </summary>
public class EinstellungenBerichtTests : EposBunitContext
{
    public EinstellungenBerichtTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static Einstellungensatz Satz() => new Einstellungensatz
    {
        VdiPfad = @"C:\Daten\VDI",
        DbExportPfad = @"C:\Daten\Export",
        DbImportPfad = @"C:\Daten\Import",
        DbPfad = @"C:\ProgramData\EPOS_PLAN",
        DbName = "Kenndaten.sqlite",
        WikiUrl = "https://wiki.example",
        PvgisUrl = "https://pvgis.example",
        GeokodierungUrl = "https://geo.example",
        TryPortalUrl = "https://try.example",
        TryRegionalUrl = "https://try-regional.example",
        AllgemeinPfad = @"C:\Daten"
    };

    /// <summary>Was die Hülle mitschreibt — in der Reihenfolge der Aufrufe.</summary>
    private readonly List<string> _protokoll = new();

    private IRenderedComponent<EinstellungenDialog> Zeige(
        bool mitWaehler = true,
        string gesperrtGrund = "",
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
             .Add(x => x.Firma, "Musterbüro GmbH")
             .Add(x => x.FirmaChanged, (string f) => _protokoll.Add("firma:" + f))
             .Add(x => x.Vorlagenordner, @"C:\Users\x\Documents\EPOS-Plan\Berichtsvorlagen")
             .Add(x => x.VorlagenordnerChanged, (string o) => _protokoll.Add("ordner:" + o))
             .Add(x => x.VorlagenordnerGesperrtGrund, gesperrtGrund);
            if (mitWaehler)
                p.Add(x => x.OrdnerWaehler, (string _) => Task.FromResult<string?>(@"\\server\buero\Vorlagen"));
            mehr?.Invoke(p);
        });

    /// <summary>Betritt die Rubrik „Bericht" über ihren Reiterknopf.</summary>
    private static void Bericht(IRenderedComponent<EinstellungenDialog> cut)
        => cut.FindAll(".epos-reiter-knopf").First(k => k.TextContent.Trim() == "Bericht").Click();

    private static IElement Firmafeld(IRenderedComponent<EinstellungenDialog> cut)
        => cut.FindAll("label.epos-feld").First(l => l.TextContent.Contains("Firma:")).QuerySelector("input")!;

    private static IElement Ordnerfeld(IRenderedComponent<EinstellungenDialog> cut)
        => cut.Find(".epos-dateiwahl input");

    [Fact]
    public void Die_Rubrik_Bericht_steht_vor_Anwendung_mit_Firma_und_Vorlagenordner()
    {
        var cut = Zeige();

        var reiter = cut.FindAll(".epos-reiter-knopf").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "VDI Datensätze", "Datenbank", "Web-Schnittstellen (API)", "Klimadaten",
                             "Diagramme", "Bericht", "Anwendung" }, reiter);
        Assert.True(cut.Instance.BerichtSichtbar);

        Bericht(cut);
        Assert.Equal("Musterbüro GmbH", Firmafeld(cut).GetAttribute("value"));
        Assert.Equal(@"C:\Users\x\Documents\EPOS-Plan\Berichtsvorlagen", Ordnerfeld(cut).GetAttribute("value"));
        Assert.False(Ordnerfeld(cut).HasAttribute("readonly"));
        Assert.Equal("Durchsuchen…", cut.Find(".epos-dateiwahl button").TextContent.Trim());
        Assert.Contains("{{ersteller.firma}}", cut.Markup);
        Assert.Contains("Die Datenbanksicherung nimmt ihn nicht mit.", cut.Markup);

        // Der Abschnitt trägt seinen eigenen Hilfeschlüssel am Infoknopf.
        Assert.Equal("Form_AdminSettings.btn_Help_Bericht", cut.Instance.HilfeSchluesselBericht);
        Assert.Equal(2, cut.FindAll(".epos-hilfepille").Count);   // Dialogkopf + Abschnitt
    }

    [Fact]
    public void Ohne_Rueckweg_der_Huelle_steht_die_Rubrik_nicht_da()
    {
        var cut = Render<EinstellungenDialog>(p => p.Add(x => x.Satz, Satz()));

        Assert.False(cut.Instance.BerichtSichtbar);
        Assert.DoesNotContain("Bericht", cut.FindAll(".epos-reiter-knopf").Select(e => e.TextContent.Trim()));
    }

    [Fact]
    public void OK_uebergibt_die_geaenderten_Werte_nach_dem_Speichern()
    {
        var cut = Zeige();
        Bericht(cut);

        Firmafeld(cut).Input("  Planungsbüro Beispiel  ");
        cut.Find(".epos-dateiwahl button").Click();   // Durchsuchen…
        Assert.Equal(@"\\server\buero\Vorlagen", Ordnerfeld(cut).GetAttribute("value"));

        cut.Find(".epos-knopf--primaer").Click();     // OK

        Assert.Equal(new[] { "speichern", "firma:Planungsbüro Beispiel", @"ordner:\\server\buero\Vorlagen", "geschlossen:True" },
                     _protokoll);
    }

    [Fact]
    public void OK_ohne_Aenderung_meldet_nur_Speichern_und_Schliessen()
    {
        var cut = Zeige();

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(new[] { "speichern", "geschlossen:True" }, _protokoll);
    }

    [Fact]
    public void Abbrechen_Kreuz_und_Esc_melden_nichts()
    {
        var cut = Zeige();
        Bericht(cut);
        Firmafeld(cut).Input("Verworfen GmbH");

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        cut.Find(".epos-dialog-zu").Click();
        cut.Find(".epos-einstellungen").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(new[] { "geschlossen:False", "geschlossen:False", "geschlossen:False" }, _protokoll);
    }

    [Fact]
    public void Scheitert_das_Speichern_geht_nichts_hinaus_und_der_Dialog_bleibt_offen()
    {
        var cut = Zeige(speichernGelingt: false);
        Bericht(cut);
        Firmafeld(cut).Input("Neu GmbH");

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(new[] { "speichern" }, _protokoll);
        Assert.Contains("Ordner nicht anlegbar.", cut.Instance.Meldung);
        Assert.Equal("Neu GmbH", cut.Instance.FirmaArbeitsstand);
    }

    [Fact]
    public void Ist_die_Ordnerwahl_gesperrt_ist_das_Feld_nur_lesbar_mit_Grund()
    {
        const string GRUND = "Auf dem iPad liegen die Vorlagen fest in der App: Dateien › Auf meinem iPad › EPOS-Plan › Berichtsvorlagen.";
        var cut = Zeige(gesperrtGrund: GRUND);
        Bericht(cut);

        Assert.True(Ordnerfeld(cut).HasAttribute("readonly"));
        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));
        Assert.Contains(GRUND, cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent));
        Assert.True(cut.Instance.VorlagenordnerGesperrt);

        // Die Firma bleibt bedienbar; OK übergibt sie, den Ordner nie.
        Firmafeld(cut).Input("Tablet GmbH");
        Ordnerfeld(cut).Input(@"D:\anders");          // nur lesbar - kommt nicht an
        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(new[] { "speichern", "firma:Tablet GmbH", "geschlossen:True" }, _protokoll);

        // Der Assistent bekommt denselben Grund.
        var fehler = Assert.Throws<InvalidOperationException>(
            () => cut.Instance.Assistentensicht.BerichtVorlagenordner = @"D:\x");
        Assert.Equal(GRUND, fehler.Message);
    }

    [Fact]
    public void Ohne_Ordnerwaehler_ist_der_Vorlagenordner_fest_wie_die_uebrigen_Pfade()
    {
        var cut = Zeige(mitWaehler: false);
        Bericht(cut);

        Assert.True(cut.Instance.VorlagenordnerGesperrt);
        Assert.True(Ordnerfeld(cut).HasAttribute("readonly"));
        Assert.Empty(cut.FindAll(".epos-dateiwahl button"));
        Assert.Contains("Die Ordner sind auf dieser Plattform fest vorgegeben.", cut.Markup);
    }

    [Fact]
    public void Standardwerte_setzen_Firma_und_Ordner_auf_die_Vorgaben()
    {
        var cut = Zeige(mehr: p => p
            .Add(x => x.FirmaVorgabe, "Lizenznehmer AG")
            .Add(x => x.VorlagenordnerVorgabe, @"C:\Users\x\Documents\EPOS-Plan\Berichtsvorlagen"));
        Bericht(cut);
        Firmafeld(cut).Input("Zwischenstand");
        Ordnerfeld(cut).Input(@"E:\Anders");

        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == "Standardwerte").Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.Equal("Lizenznehmer AG", cut.Instance.FirmaArbeitsstand);
        Assert.Equal(@"C:\Users\x\Documents\EPOS-Plan\Berichtsvorlagen", cut.Instance.VorlagenordnerArbeitsstand);
        Assert.Empty(_protokoll);   // Standardwerte speichern nicht
    }

    [Fact]
    public async Task Der_Assistent_liest_und_setzt_den_Arbeitsstand_und_Speichern_wiederholt_nichts()
    {
        var cut = Zeige();
        EinstellungenKiSicht sicht = cut.Instance.Assistentensicht;

        Assert.Equal("Musterbüro GmbH", sicht.BerichtFirma);
        Assert.Equal(@"C:\Users\x\Documents\EPOS-Plan\Berichtsvorlagen", sicht.BerichtVorlagenordner);

        sicht.BerichtFirma = "KI GmbH";
        sicht.BerichtVorlagenordner = @"D:\Vorlagen";
        Assert.Equal("KI GmbH", cut.Instance.FirmaArbeitsstand);
        Assert.Equal(@"D:\Vorlagen", cut.Instance.VorlagenordnerArbeitsstand);

        // Der Speicherweg des Assistenten ist der OK-Weg ohne Schließen ...
        KiKern.KiErgebnis ergebnis = await KiMaskenbruecke.Haken(KiMaskennamen.EINSTELLUNGEN).Speichern!();
        Assert.True(ergebnis.Erfolg, ergebnis.Text);
        Assert.Equal(new[] { "speichern", "firma:KI GmbH", @"ordner:D:\Vorlagen" }, _protokoll);

        // ... und ein OK danach wiederholt das Übergebene nicht.
        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(new[] { "speichern", "firma:KI GmbH", @"ordner:D:\Vorlagen", "speichern", "geschlossen:True" },
                     _protokoll);
    }

    [Fact]
    public void Ohne_Rubrik_lehnt_der_Assistent_benannt_ab()
    {
        var cut = Render<EinstellungenDialog>(p => p.Add(x => x.Satz, Satz()));

        var fehler = Assert.Throws<InvalidOperationException>(() => cut.Instance.Assistentensicht.BerichtFirma = "X");
        Assert.Contains("„Bericht“", fehler.Message);
    }
}

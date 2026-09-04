using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Projektauswahl (iU9-W15a.2) — EINE Komponente fuer <c>Form_ProjektAuswahl</c>
/// („Projekt öffnen") und <c>Form_ProjektDelete</c> („Projekt Löschen").
///
/// <para>Soll sind die beiden Feldkarten: Liste + OK + Abbrechen + Hilfeknopf bzw.
/// Auswahlfeld + OK + Abbrechen. Geprueft wird zusaetzlich, was die Welle
/// ANGLEICHT: die Sicherheitsabfrage mit Vorgabe „Nein" im Dialog (A-7), die
/// Meldung ohne Auswahl (die einzige MessageBox des Vorlaeufers) und der
/// Doppelklick als OK.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class ProjektWahlDialogTests : BunitContext
{
    private static readonly ProjektKopfZeile[] DREI =
    {
        new ProjektKopfZeile(1030, "Referenz BHKW", "Stadtwerke", "Kaskade", new DateTime(2026, 3, 1)),
        new ProjektKopfZeile(1007, "Laurentiuskirche", "Kirchengemeinde", "Denkmalschutz", new DateTime(2026, 5, 4)),
        new ProjektKopfZeile(1017, "Speicherhaus", "Stadtwerke", "PV", new DateTime(2026, 1, 9))
    };

    public ProjektWahlDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Sprache auf de-DE pinnen (Muster <c>GebaeudeKatalogDialogTests</c>).</summary>
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

    private IRenderedComponent<ProjektWahlDialog> Oeffnen(
        Action<ComponentParameterCollectionBuilder<ProjektWahlDialog>>? mehr = null)
        => Render<ProjektWahlDialog>(p =>
        {
            p.Add(x => x.Zeilen, DREI);
            mehr?.Invoke(p);
        });

    private IRenderedComponent<ProjektWahlDialog> Loeschen(
        Action<ComponentParameterCollectionBuilder<ProjektWahlDialog>>? mehr = null)
        => Render<ProjektWahlDialog>(p =>
        {
            p.Add(x => x.Zeilen, DREI);
            p.Add(x => x.Zweck, ProjektWahlDialog.ProjektZweck.Loeschen);
            p.Add(x => x.TitelText, "Projekt Löschen");
            p.Add(x => x.OkText, "Löschen");
            p.Add(x => x.FrageTitel, "Projekt löschen bestätigen");
            p.Add(x => x.FrageFormat,
                  "Sind Sie sicher, dass Sie das Projekt '{0}' und alle dazugehörigen Daten "
                  + "unwiderruflich löschen möchten?");
            mehr?.Invoke(p);
        });

    [Fact]
    public void Der_Oeffnen_Dialog_zeigt_Titel_Liste_und_die_zwei_Knoepfe()
    {
        var cut = Oeffnen();

        Assert.Equal("Projekt öffnen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Contains("OK", Ok(cut).TextContent);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Der_Loeschen_Dialog_zeigt_dieselbe_Liste_mit_anderem_Knopftext()
    {
        var cut = Loeschen();

        Assert.Equal("Projekt Löschen", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.FindAll("tbody tr").Count);   // A-12: Liste statt Klappliste
        Assert.Contains("Löschen", Ok(cut).TextContent);
    }

    [Fact]
    public void Ohne_Auswahl_meldet_OK_und_der_Dialog_bleibt_stehen()
    {
        ProjektKopfZeile? ergebnis = null;
        bool gerufen = false;

        var cut = Oeffnen(p => p
            .Add(x => x.AutoVorauswahl, false)
            .Add(x => x.MeldungKeineWahl, "Bitte auswählen!")
            .Add(x => x.Geschlossen, (ProjektKopfZeile? z) => { ergebnis = z; gerufen = true; }));

        Ok(cut).Click();

        Assert.False(gerufen);
        Assert.Null(ergebnis);
        Assert.Contains("Bitte auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Mit_Auswahl_meldet_OK_die_Zeile()
    {
        ProjektKopfZeile? ergebnis = null;

        var cut = Oeffnen(p => p.Add(x => x.Geschlossen, (ProjektKopfZeile? z) => ergebnis = z));

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();   // sortiert: Laurentiuskirche
        Ok(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(1007, ergebnis!.Id);
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        ProjektKopfZeile? ergebnis = new ProjektKopfZeile(1, "x");
        bool gerufen = false;

        var cut = Oeffnen(p => p.Add(x => x.Geschlossen,
            (ProjektKopfZeile? z) => { ergebnis = z; gerufen = true; }));

        Abbruch(cut).Click();

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Im_Loeschmodus_kommt_erst_die_Rueckfrage_mit_Vorgabe_Nein()
    {
        ProjektKopfZeile? ergebnis = null;

        var cut = Loeschen(p => p.Add(x => x.Geschlossen, (ProjektKopfZeile? z) => ergebnis = z));

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();   // sortiert: Speicherhaus
        Ok(cut).Click();

        // Die Frage steht, der Dialog ist NICHT geschlossen.
        Assert.Null(ergebnis);
        var frage = cut.Find(".epos-rueckfrage-text");
        Assert.Contains("Speicherhaus", frage.TextContent);
        Assert.Contains("unwiderruflich", frage.TextContent);

        // Vorgabe "Nein": der hervorgehobene Knopf der Rueckfrage ist der zweite.
        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Equal(2, knoepfe.Count);
        Assert.DoesNotContain("epos-knopf--primaer", knoepfe[0].ClassList);
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassList);
    }

    [Fact]
    public void Nein_laesst_den_Loeschdialog_stehen_Ja_schliesst_ihn()
    {
        ProjektKopfZeile? ergebnis = null;
        bool gerufen = false;

        var cut = Loeschen(p => p.Add(x => x.Geschlossen,
            (ProjektKopfZeile? z) => { ergebnis = z; gerufen = true; }));

        cut.FindAll("tbody .epos-anlagenwahl")[0].Click();
        Ok(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein

        Assert.False(gerufen);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));

        Ok(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.True(gerufen);
        Assert.NotNull(ergebnis);
        Assert.Equal(1007, ergebnis!.Id);
    }

    [Fact]
    public void Ein_Doppelklick_uebernimmt_wie_OK()
    {
        ProjektKopfZeile? ergebnis = null;

        var cut = Oeffnen(p => p.Add(x => x.Geschlossen, (ProjektKopfZeile? z) => ergebnis = z));

        cut.FindAll("tbody tr")[2].DoubleClick();           // sortiert: Speicherhaus

        Assert.NotNull(ergebnis);
        Assert.Equal(1017, ergebnis!.Id);
    }

    [Fact]
    public void Esc_bricht_ab_und_Enter_bestaetigt()
    {
        ProjektKopfZeile? ergebnis = new ProjektKopfZeile(1, "x");

        var cut = Oeffnen(p => p.Add(x => x.Geschlossen, (ProjektKopfZeile? z) => ergebnis = z));
        cut.Find(".epos-dialog").KeyDown(key: "Escape");
        Assert.Null(ergebnis);

        cut = Oeffnen(p => p.Add(x => x.Geschlossen, (ProjektKopfZeile? z) => ergebnis = z));
        cut.Find(".epos-dialog").KeyDown(key: "Enter");
        Assert.NotNull(ergebnis);
    }

    [Fact]
    public void Die_Vorauswahl_der_Kachel_stellt_das_gemerkte_Projekt_scharf()
    {
        ProjektKopfZeile? ergebnis = null;

        var cut = Oeffnen(p => p
            .Add(x => x.Vorauswahl, "Speicherhaus")
            .Add(x => x.SortSpalte, ProjektListeSpalteGeaendert)
            .Add(x => x.SortAbsteigend, true)
            .Add(x => x.Geschlossen, (ProjektKopfZeile? z) => ergebnis = z));

        Ok(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(1017, ergebnis!.Id);

        // "zuletzt geaendert zuerst": Laurentiuskirche (04.05.) steht oben.
        Assert.Contains("Laurentiuskirche", cut.FindAll("tbody tr")[0].TextContent);
    }

    [Fact]
    public void Ohne_Hilfeschluessel_erscheint_kein_Infoknopf()
    {
        Assert.Empty(Oeffnen().FindAll(".epos-infoknopf"));
        Assert.Single(Oeffnen(p => p.Add(x => x.HilfeSchluessel, "Form_ProjektAuswahl.btn_Help"))
                          .FindAll(".epos-infoknopf"));
    }

    /// <summary>
    /// Der OK-Knopf der SCHLUSSLEISTE. Nicht ueber ".epos-knopf--primaer" allein:
    /// Die markierte Rasterzeile traegt dieselbe Klasse (Baustein Zeilenwahl).
    /// </summary>
    private static AngleSharp.Dom.IElement Ok(IRenderedComponent<ProjektWahlDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste .epos-knopf--primaer")[0];

    /// <summary>Der Abbrechen-Knopf der Schlussleiste.</summary>
    private static AngleSharp.Dom.IElement Abbruch(IRenderedComponent<ProjektWahlDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button")
              .First(k => !k.ClassList.Contains("epos-knopf--primaer"));

    private static int ProjektListeSpalteGeaendert
        => EPOS.UI.Bausteine.ProjektListe.SPALTE_GEAENDERT;
}

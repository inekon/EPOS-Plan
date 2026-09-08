using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Die SECHS Wirte der Stufe S3</b> (Anwenderentscheid <b>W14a‑E‑10</b> vom
/// 07.09.2026, Schritte <b>S3.1</b> und <b>S3.2</b> des
/// <c>Konzept_Katalogfilter_EPOS-Plan.md</c>) — drei Bedarfs- und drei
/// Zeitreihenkataloge, jeder in seiner Verwaltung und, wo es ihn gibt, in seinem
/// Projektdialog.
///
/// <para><b>Was dieser Fall festhält</b>, quer über die acht Dialoge: Jeder trägt
/// die EINE <c>Katalogliste</c> des Hauses — also die Suchzeile mit Trefferzahl,
/// den Spaltenkopf mit Sortierpfeil und Trichter und die Spalten seines
/// <c>Katalogfilterprofil</c>. Die Zahlen und Trefferzahlen prüft der Kern
/// (<c>KatalogfilterBedarfTests</c>, <c>KatalogfilterZeitreihenTests</c>); hier
/// steht, dass sie beim Anwender ankommen.</para>
///
/// <para><b>Die Prüfstände geben ihren Filterstand selbst herein</b> — das Register
/// ist prozessweit, und xunit fährt Testklassen nebeneinander (Muster der neun
/// Katalogdialoge aus S2.5).</para>
///
/// <para>Kultur gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class KatalogfilterS3Tests : BunitContext
{
    public KatalogfilterS3Tests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;

        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  S3.1 - die drei Bedarfskataloge
    // =====================================================================

    private static IReadOnlyList<Katalogfilterzeile> Bedarfszeilen() => new[]
    {
        Bedarfszeile(1, "Wohnen groß (VDI 6002), 1 Person", "Wohnen groß (VDI 6002)",
                     0.743, "Monatswerte in MWh für 1 Person bei 28 l/d @60 °C", true),
        Bedarfszeile(2, "Haushalt-3", "Test", 12.5, "3 Personenhaushalt", false)
    };

    private static Katalogfilterzeile Bedarfszeile(int id, string name, string typ, double summe,
                                                   string beschreibung, bool geschuetzt)
        => new Katalogfilterzeile(id, name) { Geschuetzt = geschuetzt }
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpTyp, typ)
            .MitZahl(Katalogfilterprofil.SpJahressummeMwh, summe, 3)
            .MitText(Katalogfilterprofil.SpBeschreibung, beschreibung)
            .MitKennzeichen(Katalogfilterprofil.SpAuslieferung, geschuetzt);

    /// <summary>
    /// <b>S3.1:</b> Die Verwaltung eines Bedarfskatalogs trägt die Katalogliste samt
    /// Suchzeile und Trefferzahl — bis dahin ein Raster mit dem blossen Bezeichner
    /// und ohne jeden Filter.
    /// </summary>
    [Theory]
    [InlineData(BedarfsArt.Brauchwasser)]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Stromverbraucher)]
    public void Die_Bedarfsverwaltung_traegt_die_Katalogliste(BedarfsArt art)
    {
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Katalogzeilen, Bedarfszeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(art, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B " + n, "T " + n)))
            .Add(x => x.Jahressumme, (Func<string, string>)(_ => "12,500")));

        Assert.Single(cut.FindAll(".epos-katalogliste"));
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile input"));
        Assert.Equal("2 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);

        // Wahl + die fuenf Spalten des Profils.
        Assert.Equal(6, cut.FindAll(".epos-katalogliste thead th").Count);
    }

    /// <summary>
    /// <b>S3.1:</b> Der Filter greift VOR dem Raster — „VDI" im Suchfeld lässt genau
    /// den Satz stehen, der es in seiner BESCHREIBUNG trägt. Genau dafür ist sie eine
    /// Spalte (Konzept 2.9: die VDI‑6002-Sätze tragen ihren Kennwert im Text).
    /// </summary>
    [Fact]
    public void Die_Suche_findet_den_Kennwert_in_der_Beschreibung()
    {
        var cut = Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Brauchwasser)
            .Add(x => x.Katalogzeilen, Bedarfszeilen)
            .Add(x => x.Katalogprofil,
                 Katalogfilterprofil.FuerBedarf(BedarfsArt.Brauchwasser, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Kopf, (Func<string, (string, string)?>)(n => ("B", "T")))
            .Add(x => x.Jahressumme, (Func<string, string>)(_ => "0,743")));

        cut.Find(".epos-katalog-suchzeile input").Input("28 l/d");

        Assert.Equal("1 von 2 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(cut.FindAll(".epos-katalogliste tbody tr"));
    }

    /// <summary>
    /// <b>S3.1 / Q12:</b> Der PROJEKTdialog derselben Ausprägung trägt zusätzlich die
    /// Spalte „im Projekt verwendet" — und sie wird aus der LEBENDEN Projektliste
    /// gestempelt, nicht aus einer Abfrage.
    /// </summary>
    [Fact]
    public void Der_Bedarfsprojektdialog_stempelt_die_Verwendung()
    {
        var zeilen = new List<BedarfsProfilZeile>
        {
            new() { IdZ = 1, IdStamm = 3, Name = "Haushalt-3", Summe = 12.5 }
        };

        var cut = Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Brauchwasser)
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, Bedarfszeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil
                 .FuerBedarf(BedarfsArt.Brauchwasser, s => s).MitVerwendungsspalte(s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, (Func<string, BedarfsProfilInfo?>)(n => new BedarfsProfilInfo(n, "", "")))
            .Add(x => x.Jahressumme, (Func<string, double>)(_ => 12.5)));

        // Wahl + fuenf Profilspalten + "im Projekt verwendet".
        Assert.Equal(7, cut.FindAll(".epos-katalogliste thead th").Count);

        Katalogfilterzeile verwendet =
            cut.Instance.Katalog.First(z => z.Bezeichner == "Haushalt-3");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_JA,
                     verwendet.Text(Katalogfilterprofil.SpVerwendet));

        Katalogfilterzeile frei =
            cut.Instance.Katalog.First(z => z.Bezeichner != "Haushalt-3");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_NEIN,
                     frei.Text(Katalogfilterprofil.SpVerwendet));
    }

    // =====================================================================
    //  S3.2 - die drei Zeitreihenkataloge
    // =====================================================================

    /// <summary>
    /// <b>S3.2:</b> Die Stromganglinien-Verwaltung trägt die Katalogliste mit VIER
    /// Spalten — Bezeichner, Zeitintervall, Jahresarbeit und Spitze. Die zwei
    /// Kennzahlen standen bis dahin überhaupt nicht in der Liste.
    /// </summary>
    [Fact]
    public void Die_Stromganglinienverwaltung_zeigt_Jahresarbeit_und_Spitze()
    {
        var cut = Render<StromganglinieAdminDialog>(p => p
            .Add(x => x.Katalogzeilen,
                 () => Task.FromResult(Zeitreihenproben.Stromganglinien()))
            .Add(x => x.Katalogprofil, Zeitreihenproben.Profil(Zeitreihenart.Stromganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        Assert.Single(cut.FindAll(".epos-katalogliste"));
        Assert.Equal(5, cut.FindAll(".epos-katalogliste thead th").Count);

        // Die Stundenspitze der Viertelstundenreihe steht als Zahl in der Zelle.
        Assert.Contains("1.513,5", cut.Markup);
        Assert.Contains("4.790,0", cut.Markup);
    }

    /// <summary>
    /// <b>S3.2:</b> Der Wärmebedarf führt WEDER Zeitintervall NOCH Beschreibung —
    /// seine Kopftabelle hat die Spalten gar nicht (Konzept 4.10).
    /// </summary>
    [Fact]
    public void Die_Waermebedarfsverwaltung_fuehrt_drei_Spalten()
    {
        var cut = Render<WaermebedarfAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(
                (IReadOnlyList<Katalogfilterzeile>)new[]
                {
                    Zeitreihenproben.Zeile(1, "Lastgang Gas 2010",
                                           jahresarbeitMwh: 6137.6, spitzeKw: 2206.0)
                }))
            .Add(x => x.Katalogprofil, Zeitreihenproben.Profil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        // Wahl + Bezeichner + Jahresarbeit + Spitze.
        Assert.Equal(4, cut.FindAll(".epos-katalogliste thead th").Count);
    }

    /// <summary>
    /// <b>S3.2:</b> Die Solarganglinie führt als einzige die BESCHREIBUNG — sie steht
    /// in ihrer Kopftabelle, in den beiden anderen nicht.
    /// </summary>
    [Fact]
    public void Die_Solarganglinienverwaltung_fuehrt_die_Beschreibung()
    {
        var cut = Render<SolarganglinieAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(
                (IReadOnlyList<Katalogfilterzeile>)new[]
                {
                    Zeitreihenproben.Zeile(1, "Tsol1", beschreibung: "Leistung Solarsystem [W]",
                                           jahresarbeitMwh: 3.9, spitzeKw: 5.4)
                }))
            .Add(x => x.Katalogprofil, Zeitreihenproben.Profil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        // Wahl + Bezeichner + Beschreibung + Jahresarbeit + Spitze.
        Assert.Equal(5, cut.FindAll(".epos-katalogliste thead th").Count);
        Assert.Contains("Leistung Solarsystem [W]", cut.Markup);
    }

    /// <summary>
    /// <b>S3.2 / Q12:</b> Die zwei Projektdialoge der Zeitreihen tragen die Spalte
    /// „im Projekt verwendet" — dieselbe Regel wie bei den sieben Erzeugerdialogen
    /// aus S2.3.
    /// </summary>
    [Fact]
    public void Die_Zeitreihen_Projektdialoge_tragen_die_Verwendungsspalte()
    {
        var strom = Render<StromganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<GanglinienProjektZeile>())
            .Add(x => x.Katalogzeilen,
                 () => Task.FromResult(Zeitreihenproben.Stromganglinien()))
            .Add(x => x.Katalogprofil,
                 Zeitreihenproben.ProjektProfil(Zeitreihenart.Stromganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        Assert.Equal(6, strom.FindAll(".epos-katalogliste thead th").Count);

        var solar = Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogzeilen, () => (IReadOnlyList<Katalogfilterzeile>)new[]
            {
                Zeitreihenproben.Zeile(21, "Ganglinie Nord", beschreibung: "Messreihe",
                                       jahresarbeitMwh: 3.9, spitzeKw: 5.4)
            })
            .Add(x => x.Katalogprofil,
                 Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

        Assert.Equal(6, solar.FindAll(".epos-katalogliste thead th").Count);
    }
}

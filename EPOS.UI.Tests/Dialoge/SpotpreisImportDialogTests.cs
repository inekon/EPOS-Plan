using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Import der Spotmarktpreise (iU9-W3.2), Vorbild
/// <c>Views/Kosten/Form_SpotpreisImport</c>.
///
/// <para>Soll ist die Feldkarte: Info, Pfadfeld + Wählknopf, Bezeichnung,
/// Stammschalter, Protokoll, Statuszeile, „Übernehmen" (anfangs gesperrt) und
/// „Abbrechen". Geprüft wird vor allem die Zweischrittregel: erst prüfen, dann
/// schreiben.</para>
/// </summary>
public class SpotpreisImportDialogTests : BunitContext
{
    public SpotpreisImportDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static SpotpreisPruefung Gut(int jahr = 2026, string protokoll = "alles in Ordnung")
        => new(true, protokoll, jahr);

    private IRenderedComponent<SpotpreisImportDialog> Zeige(
        Func<string, Task<string?>>? waehlen = null,
        Func<string, Task<SpotpreisPruefung>>? pruefen = null,
        Func<string, bool, Action<int>, Task<SpotpreisSpeicherung>>? speichern = null,
        Action<bool>? geschlossen = null)
    {
        return Render<SpotpreisImportDialog>(p => p
            .Add(x => x.Waehlen, waehlen ?? (f => Task.FromResult<string?>(@"C:\Daten\spot2026.csv")))
            .Add(x => x.Pruefen, pruefen ?? (pfad => Task.FromResult(Gut())))
            .Add(x => x.Speichern, speichern ??
                ((b, s, f) => Task.FromResult(new SpotpreisSpeicherung(42, 8760))))
            .Add(x => x.Geschlossen, (bool ok) => geschlossen?.Invoke(ok)));
    }

    private static IElement Uebernehmen(IRenderedComponent<SpotpreisImportDialog> cut)
        => cut.FindAll(".epos-leiste button")[1];

    private static IElement Schliessen(IRenderedComponent<SpotpreisImportDialog> cut)
        => cut.FindAll(".epos-leiste button")[0];

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_die_neun_Zeilen_der_Feldkarte()
    {
        var cut = Zeige();

        Assert.Contains("Spotmarktpreise importieren", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-herleitung"));                    // _lblInfo
        Assert.Single(cut.FindAll(".epos-dateiwahl"));                     // _tbPfad + _btnWaehlen
        Assert.Single(cut.FindAll("textarea"));                            // _tbProtokoll
        Assert.Single(cut.FindAll("input[type=checkbox]"));                // _chkStamm
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);         // Übernehmen, Abbrechen
    }

    [Fact]
    public void Das_Protokollfeld_ist_mehrzeilig_nur_lesbar_und_festbreit()
    {
        var cut = Zeige();
        var flaeche = cut.Find("textarea");

        Assert.True(flaeche.HasAttribute("readonly"));
        Assert.Contains("epos-eingabe--mehrzeilig", flaeche.ClassName);
        Assert.Contains("epos-eingabe--festbreite", flaeche.ClassName);
    }

    /// <summary>Der Designer setzte <c>_chkStamm.Checked = true</c>.</summary>
    [Fact]
    public void Der_Stammschalter_ist_vorbelegt()
    {
        var cut = Zeige();

        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void Uebernehmen_ist_vor_der_Pruefung_gesperrt()
    {
        var cut = Zeige();

        Assert.True(Uebernehmen(cut).HasAttribute("disabled"));
    }

    // =====================================================================
    // Schritt 1 — prüfen
    // =====================================================================

    [Fact]
    public void Eine_gewaehlte_Datei_wird_sofort_geprueft_und_gibt_den_Bezeichner_vor()
    {
        var cut = Zeige();

        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal("alles in Ordnung", cut.Instance.Protokoll);
        Assert.False(cut.Instance.StatusIstFehler);
        Assert.Contains("2026", cut.Instance.Status);
        Assert.Equal("spot2026", cut.Find("input[type=text]:not([readonly])").GetAttribute("value"));
        Assert.False(Uebernehmen(cut).HasAttribute("disabled"));
    }

    /// <summary>Ein schon gefülltes Bezeichnerfeld bleibt stehen.</summary>
    [Fact]
    public void Ein_eigener_Bezeichner_wird_nicht_ueberschrieben()
    {
        var cut = Zeige();

        cut.Find("input[type=text]:not([readonly])").Input("Eigener Name");
        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal("Eigener Name",
            cut.Find("input[type=text]:not([readonly])").GetAttribute("value"));
    }

    [Fact]
    public void Eine_unbrauchbare_Datei_meldet_und_sperrt_Uebernehmen()
    {
        var cut = Zeige(pruefen: pfad =>
            Task.FromResult(new SpotpreisPruefung(false, "Zeile 12 unlesbar", 0)));

        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal("Zeile 12 unlesbar", cut.Instance.Protokoll);
        Assert.True(cut.Instance.StatusIstFehler);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.True(Uebernehmen(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// Wie der catch-Zweig von <c>DateiPruefen</c>: Die Ausnahme wird zum
    /// Protokoll, der Dialog bleibt stehen.
    /// </summary>
    [Fact]
    public void Eine_Ausnahme_beim_Pruefen_wird_zum_Protokoll()
    {
        var cut = Zeige(pruefen: pfad => throw new InvalidOperationException("Datei gesperrt"));

        cut.Find(".epos-dateiwahl button").Click();

        Assert.Equal("Datei gesperrt", cut.Instance.Protokoll);
        Assert.True(cut.Instance.StatusIstFehler);
    }

    [Fact]
    public void Ein_abgebrochener_Dateiwaehler_prueft_nicht()
    {
        bool geprueft = false;
        var cut = Zeige(
            waehlen: f => Task.FromResult<string?>(""),
            pruefen: pfad => { geprueft = true; return Task.FromResult(Gut()); });

        cut.Find(".epos-dateiwahl button").Click();

        Assert.False(geprueft);
        Assert.Equal("", cut.Instance.Status);
    }

    // =====================================================================
    // Schritt 2 — schreiben
    // =====================================================================

    [Fact]
    public void Uebernehmen_reicht_Bezeichner_und_Ziel_weiter_und_schliesst()
    {
        string? bezeichner = null;
        bool? stamm = null;
        bool? ergebnis = null;

        var cut = Zeige(
            speichern: (b, s, f) =>
            {
                bezeichner = b;
                stamm = s;
                return Task.FromResult(new SpotpreisSpeicherung(42, 8760));
            },
            geschlossen: ok => ergebnis = ok);

        cut.Find(".epos-dateiwahl button").Click();
        Uebernehmen(cut).Click();

        Assert.Equal("spot2026", bezeichner);
        Assert.True(stamm);                      // der Schalter ist vorbelegt
        Assert.True(ergebnis);
        Assert.Contains("42", cut.Instance.Status);
        Assert.Contains("8760", cut.Instance.Status);
    }

    [Fact]
    public void Ohne_Stammschalter_geht_das_Projekt_als_Ziel_mit()
    {
        bool? stamm = null;
        var cut = Zeige(speichern: (b, s, f) =>
        {
            stamm = s;
            return Task.FromResult(new SpotpreisSpeicherung(7, 8760));
        });

        cut.Find(".epos-dateiwahl button").Click();
        cut.Find("input[type=checkbox]").Change(false);
        Uebernehmen(cut).Click();

        Assert.False(stamm);
    }

    [Fact]
    public void Ein_gescheitertes_Schreiben_meldet_und_haelt_den_Dialog_offen()
    {
        bool geschlossen = false;
        var cut = Zeige(
            speichern: (b, s, f) => Task.FromResult(new SpotpreisSpeicherung(0, 0)),
            geschlossen: ok => geschlossen = true);

        cut.Find(".epos-dateiwahl button").Click();
        Uebernehmen(cut).Click();

        Assert.False(geschlossen);
        Assert.True(cut.Instance.StatusIstFehler);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
    }

    /// <summary>Der Fortschrittsmelder des Vorläufers schreibt in die Statuszeile.</summary>
    [Fact]
    public void Der_Fortschritt_erscheint_waehrend_des_Schreibens_in_der_Statuszeile()
    {
        IRenderedComponent<SpotpreisImportDialog>? cut = null;
        string zwischenstand = "";

        cut = Zeige(speichern: (b, s, f) =>
        {
            f(3000);
            zwischenstand = cut!.Instance.Status;
            return Task.FromResult(new SpotpreisSpeicherung(9, 8760));
        });

        cut.Find(".epos-dateiwahl button").Click();
        Uebernehmen(cut).Click();

        Assert.Contains("3000", zwischenstand);         // Vorgabe "schreibt … {0} Werte"
        Assert.Contains("9", cut.Instance.Status);      // danach die Schlussmeldung
    }

    // =====================================================================
    // Tastatur und Hilfe
    // =====================================================================

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Zeige(geschlossen: ok => ergebnis = ok);

        Schliessen(cut).Click();
        Assert.False(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.False(ergebnis);
    }

    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        bool geschrieben = false;
        var cut = Zeige(speichern: (b, s, f) =>
        {
            geschrieben = true;
            return Task.FromResult(new SpotpreisSpeicherung(1, 1));
        });

        cut.Find(".epos-dateiwahl button").Click();
        cut.Find(".epos-dialog").KeyDown("Enter");

        Assert.False(geschrieben);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_SpotpreisImport.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Der Feldlauf steht im
    /// <c>Formularraster</c>, EINSPALTIG: Das Pfadfeld der Dateiwahl und das
    /// mehrzeilige Protokoll brauchen die ganze Breite.
    /// </summary>
    [Fact]
    public void Der_Feldlauf_steht_im_einspaltigen_Formularraster()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-formularraster.epos-formularraster--einspaltig"));
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld").Count >= 3);
    }
}

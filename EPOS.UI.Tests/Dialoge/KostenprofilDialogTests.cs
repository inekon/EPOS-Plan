using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Kostenprofil-Editor (iU9-W3.4), Vorbild
/// <c>Views/Kosten/Form_Kostenprofil</c>.
///
/// <para>Soll sind die 14 Kartenzeilen UND die 36 Laufzeitfelder, die in keiner
/// Feldkarte stehen: zwölf Monatsniveaus und 24 Stundenabweichungen (Regel F1 —
/// von Hand nachgetragen).</para>
/// </summary>
public class KostenprofilDialogTests : BunitContext
{
    private static readonly string[] MONATE =
    {
        "Januar", "Februar", "März", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    };

    private static readonly (int, string)[] TAGE =
    {
        (0, "Montag"), (1, "Dienstag"), (2, "Mittwoch"), (3, "Donnerstag"),
        (4, "Freitag"), (5, "Samstag"), (6, "Sonntag")
    };

    public KostenprofilDialogTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static double[] Monatsvorgabe(double wert = 25.0)
    {
        var w = new double[12];
        for (int m = 0; m < 12; m++) w[m] = wert;
        return w;
    }

    private static double[] Wochenvorgabe(double wert = 0.0)
    {
        var w = new double[168];
        for (int i = 0; i < 168; i++) w[i] = wert;
        return w;
    }

    private IRenderedComponent<KostenprofilDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<KostenprofilDialog>>? mehr = null,
        Func<IReadOnlyList<double>, IReadOnlyList<double>, Task<byte[]?>>? vorschau = null,
        double[]? monatswerte = null,
        double[]? wochenwerte = null)
    {
        return Render<KostenprofilDialog>(p =>
        {
            p.Add(x => x.Monatsnamen, MONATE);
            p.Add(x => x.Wochentage, TAGE);
            p.Add(x => x.Monatswerte, monatswerte ?? Monatsvorgabe());
            p.Add(x => x.Wochenwerte, wochenwerte ?? Wochenvorgabe());
            p.Add(x => x.Vorschau, vorschau ?? ((m, w) => Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 })));
            mehr?.Invoke(p);
        });
    }

    /// <summary>
    /// Stellt einen Reiter ein (0 = Monat, 1 = Woche, 2 = Grafik). Seit
    /// iU9-W5.0 stehen die drei Abschnitte als REITER (Nachzug von W3-A-17) —
    /// es ist immer nur einer gezeichnet.
    /// </summary>
    private static void Reiter(IRenderedComponent<KostenprofilDialog> cut, int nr)
        => cut.FindAll(".epos-reiter-knopf")[nr].Click();

    /// <summary>Die Zahlenfelder des Monatsgitters (Reiter „Monat").</summary>
    private static IHtmlCollection<IElement> Monatsfelder(IRenderedComponent<KostenprofilDialog> cut)
    {
        Reiter(cut, 0);
        return cut.Find(".epos-zahlenraster").QuerySelectorAll("input");
    }

    /// <summary>Die Zahlenfelder des Stundengitters (Reiter „Woche").</summary>
    private static IHtmlCollection<IElement> Stundenfelder(IRenderedComponent<KostenprofilDialog> cut)
    {
        Reiter(cut, 1);
        return cut.Find(".epos-zahlenraster").QuerySelectorAll("input");
    }

    /// <summary>Die vier Knöpfe des Reiters „Woche".</summary>
    private static IReadOnlyList<IElement> Wochenknoepfe(IRenderedComponent<KostenprofilDialog> cut)
    {
        Reiter(cut, 1);
        return cut.Find(".epos-reiter-blatt").QuerySelectorAll(".epos-leiste button");
    }

    // =====================================================================
    // Feldbestand: Karte UND Laufzeitfelder
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_die_drei_Reiter_und_die_Schlussleiste()
    {
        var cut = Zeige();

        Assert.Equal("Kostenprofil", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.FindAll(".epos-reiter-knopf").Count);          // Monat, Woche, Grafik
        Assert.Single(cut.FindAll(".epos-reiter-blatt"));                  // nur der aktive
        Assert.Single(cut.FindAll(".epos-zahlenraster"));                  // Monatsgitter
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);
    }

    /// <summary>Die 36 Laufzeitfelder des Vorläufers, plus Bezeichner und Wochentag.</summary>
    [Fact]
    public void Zwoelf_Monatsfelder_und_vierundzwanzig_Stundenfelder_stehen_da()
    {
        var cut = Zeige();

        Assert.Equal(12, Monatsfelder(cut).Length);
        Assert.Equal(24, Stundenfelder(cut).Length);
        Assert.Single(cut.FindAll("select"));                             // Wochentag
    }

    [Fact]
    public void Die_Monatsnamen_und_die_Stundennummern_stehen_an_ihren_Feldern()
    {
        var cut = Zeige();

        foreach (string monat in MONATE) Assert.Contains(monat + ":", cut.Markup);

        Reiter(cut, 1);
        Assert.Contains("1.", cut.Markup);
        Assert.Contains("24.", cut.Markup);
    }

    [Fact]
    public void Die_sieben_Wochentage_beginnen_bei_Montag()
    {
        var cut = Zeige();

        Reiter(cut, 1);
        var eintraege = cut.Find("select").QuerySelectorAll("option");
        Assert.Equal(7, eintraege.Length);
        Assert.Equal("Montag", eintraege[0].TextContent);
        Assert.Equal("Sonntag", eintraege[6].TextContent);
    }

    [Fact]
    public void Die_sieben_Knoepfe_der_Maske_sind_alle_da()
    {
        var cut = Zeige();

        // Reiter Monat 1, Reiter Woche 4, Reiter Grafik 1, Schlussleiste 2
        Reiter(cut, 0);
        Assert.Single(cut.Find(".epos-reiter-blatt").QuerySelectorAll(".epos-leiste button"));
        Assert.Equal(4, Wochenknoepfe(cut).Count);
        Reiter(cut, 2);
        Assert.Single(cut.Find(".epos-reiter-blatt").QuerySelectorAll(".epos-leiste button"));
    }

    // =====================================================================
    // Vorbelegung
    // =====================================================================

    [Fact]
    public void Die_geladenen_Werte_stehen_in_den_Feldern()
    {
        var monat = Monatsvorgabe();
        monat[5] = 31.5;
        var woche = Wochenvorgabe();
        woche[2 * 24 + 7] = -1.25;      // Mittwoch, 8. Stunde

        var cut = Zeige(p => p.Add(x => x.Bezeichner, "Börsenpreis 2026"),
                        monatswerte: monat, wochenwerte: woche);

        Assert.Equal(31.5, cut.Instance.Monatsfelder[5]);
        Assert.Equal(-1.25, cut.Instance.Wochenwert(2, 7));
        Assert.Equal("Börsenpreis 2026",
            cut.Find("input[type=text]:not([inputmode])").GetAttribute("value"));
    }

    [Fact]
    public void Ein_Tagwechsel_zeigt_die_Stundenwerte_des_neuen_Tages()
    {
        var woche = Wochenvorgabe();
        woche[0] = 1.0;                 // Montag, 1. Stunde
        woche[5 * 24] = 9.0;            // Samstag, 1. Stunde

        var cut = Zeige(wochenwerte: woche);

        Assert.Equal("1", Stundenfelder(cut)[0].GetAttribute("value"));

        cut.Find("select").Change("5");

        Assert.Equal(5, cut.Instance.GewaehlterTag);
        Assert.Equal("9", Stundenfelder(cut)[0].GetAttribute("value"));
    }

    // =====================================================================
    // Die sieben Knöpfe
    // =====================================================================

    [Fact]
    public void Januar_fuer_alle_Monate_uebernimmt_den_ersten_Wert()
    {
        var cut = Zeige();

        Monatsfelder(cut)[0].Input("18,5");
        cut.Find(".epos-reiter-blatt").QuerySelectorAll(".epos-leiste button")[0].Click();

        for (int m = 0; m < 12; m++) Assert.Equal(18.5, cut.Instance.Monatsfelder[m]);
    }

    [Fact]
    public void Tag_einfuegen_ohne_Kopie_meldet_sich()
    {
        var cut = Zeige(p => p.Add(x => x.MeldungErstKopieren, "erst kopieren"));

        Assert.True(Wochenknoepfe(cut)[1].HasAttribute("disabled"));

        // Nach dem Kopieren ist der Knopf frei.
        Wochenknoepfe(cut)[0].Click();
        Assert.False(Wochenknoepfe(cut)[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Kopieren_und_Einfuegen_traegt_den_Tagesgang_auf_einen_anderen_Tag()
    {
        var cut = Zeige();

        Stundenfelder(cut)[3].Input("4,5");        // Montag, 4. Stunde
        Wochenknoepfe(cut)[0].Click();             // Tag kopieren

        cut.Find("select").Change("3");            // Donnerstag
        Wochenknoepfe(cut)[1].Click();             // Tag einfügen

        Assert.Equal(4.5, cut.Instance.Wochenwert(3, 3));
        Assert.Equal(4.5, cut.Instance.Wochenwert(0, 3));
    }

    [Fact]
    public void Fuer_alle_Tage_traegt_den_gezeigten_Tag_auf_alle_sieben()
    {
        var cut = Zeige(p => p.Add(x => x.MeldungAlleTage, "gilt jetzt für alle Tage"));

        Stundenfelder(cut)[10].Input("2");
        Wochenknoepfe(cut)[2].Click();

        for (int t = 0; t < 7; t++) Assert.Equal(2.0, cut.Instance.Wochenwert(t, 10));
        Assert.Contains("alle Tage", cut.Instance.Meldung);
    }

    /// <summary>A-7 aus Welle 2: Ein geleertes Feld behält seinen Wert.</summary>
    [Fact]
    public void Ein_geleertes_Monatsfeld_behaelt_seinen_Wert()
    {
        var cut = Zeige();

        Monatsfelder(cut)[2].Input("");

        Assert.Equal(25.0, cut.Instance.Monatsfelder[2]);
    }

    // =====================================================================
    // Vorschau
    // =====================================================================

    [Fact]
    public void Die_Vorschau_wird_beim_Oeffnen_gezeichnet()
    {
        int laeufe = 0;
        var cut = Zeige(vorschau: (m, w) =>
        {
            laeufe++;
            return Task.FromResult<byte[]?>(new byte[] { 9 });
        });

        Assert.Equal(1, laeufe);
        Assert.NotNull(cut.Instance.Bild);

        // Das Bild steht im Reiter „Grafik"; sein Betreten zeichnet erneut
        // (der Vorlaeufer tat dasselbe bei jedem Reiterwechsel).
        Reiter(cut, 2);
        Assert.Single(cut.FindAll("img.epos-chartbild"));
        Assert.Equal(2, laeufe);
    }

    [Fact]
    public void Die_Vorschau_bekommt_zwoelf_Monats_und_168_Wochenwerte()
    {
        IReadOnlyList<double>? monat = null;
        IReadOnlyList<double>? woche = null;

        var cut = Zeige(vorschau: (m, w) =>
        {
            monat = m; woche = w;
            return Task.FromResult<byte[]?>(new byte[] { 9 });
        });

        Assert.NotNull(monat);
        Assert.Equal(12, monat!.Count);
        Assert.Equal(168, woche!.Count);
        Assert.Equal(25.0, monat[0]);
    }

    [Fact]
    public void Der_Aktualisieren_Knopf_zeichnet_neu()
    {
        int laeufe = 0;
        var cut = Zeige(vorschau: (m, w) =>
        {
            laeufe++;
            return Task.FromResult<byte[]?>(new byte[] { 9 });
        });

        Reiter(cut, 2);                            // Betreten zeichnet (1 -> 2)
        cut.Find(".epos-reiter-blatt").QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Equal(3, laeufe);
    }

    [Fact]
    public void Stundenwerte_uebernehmen_zeichnet_die_Vorschau_neu()
    {
        int laeufe = 0;
        var cut = Zeige(vorschau: (m, w) =>
        {
            laeufe++;
            return Task.FromResult<byte[]?>(new byte[] { 9 });
        });

        Wochenknoepfe(cut)[3].Click();

        Assert.Equal(2, laeufe);
    }

    // =====================================================================
    // Abschluss
    // =====================================================================

    [Fact]
    public void OK_speichert_Bezeichner_und_beide_Reihen_und_schliesst()
    {
        string? bezeichner = null;
        IReadOnlyList<double>? monat = null;
        IReadOnlyList<double>? woche = null;
        bool? ergebnis = null;

        var cut = Zeige(p => p
            .Add(x => x.Bezeichner, " Börsenpreis ")
            .Add(x => x.Speichern, (b, m, w) =>
            {
                bezeichner = b; monat = m; woche = w;
                return true;
            })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-dialog > .epos-leiste button")[1].Click();

        Assert.Equal("Börsenpreis", bezeichner);        // getrimmt
        Assert.Equal(12, monat!.Count);
        Assert.Equal(168, woche!.Count);
        Assert.True(ergebnis);
    }

    [Fact]
    public void Ein_gescheitertes_Speichern_meldet_und_haelt_den_Dialog_offen()
    {
        bool geschlossen = false;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, (b, m, w) => false)
            .Add(x => x.MeldungNichtGespeichert, "nicht gespeichert")
            .Add(x => x.Geschlossen, (bool ok) => geschlossen = true));

        cut.FindAll(".epos-dialog > .epos-leiste button")[1].Click();

        Assert.False(geschlossen);
        Assert.Contains("nicht gespeichert", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_false_und_speichern_nicht()
    {
        bool gespeichert = false;
        bool? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, (b, m, w) => { gespeichert = true; return true; })
            .Add(x => x.Geschlossen, (bool ok) => ergebnis = ok));

        cut.FindAll(".epos-dialog > .epos-leiste button")[0].Click();
        Assert.False(ergebnis);
        Assert.False(gespeichert);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.False(ergebnis);
    }

    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        bool gespeichert = false;
        var cut = Zeige(p => p.Add(x => x.Speichern, (b, m, w) => { gespeichert = true; return true; }));

        cut.Find(".epos-dialog").KeyDown("Enter");

        Assert.False(gespeichert);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_Kostenprofil.btn_Help" }, hilfe.Geoeffnet);
    }
}

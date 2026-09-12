using System;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Projekt;

/// <summary>
/// „Als Variante speichern" mit QUELLPROJEKT (Auftrag <b>#237</b>, Anwenderwunsch
/// 12.09.2026).
///
/// <para><b>Was geprüft wird.</b> Ohne Haken soll der Dialog Zeichen für Zeichen das
/// tun, was die generische Namensabfrage bis dahin tat (Knopf gesperrt bei leerem
/// Feld, Rückgabe des getrimmten Bezeichners, kein Quellprojekt). Mit Haken kommen
/// vier Aussagen dazu: die Liste erscheint, der Knopf bleibt ohne Auswahl gesperrt,
/// die Auswahl BELEGT den Bezeichner vor — und ein von Hand getippter Bezeichner
/// bleibt dabei stehen. Das ist der Von-Hand-Merker, und er ist der einzige Teil des
/// Dialogs, den man lautlos falsch bauen kann.</para>
///
/// <para><b>Die Texte kommen aus dem ECHTEN Ressourcenkatalog</b> (Lehre aus
/// Befund #217: Eine Razor-Vorgabe mit weniger Platzhaltern als die Ressource
/// versteckt genau den Absturz, den sie zeigen sollte). Zwei Fälle formatieren
/// deshalb <c>VAR_DLG_HINWEIS_QUELLE</c> und <c>VAR_DLG_ZIELNAME</c> so, wie der
/// Dialog es tut — in BEIDEN Sprachen.</para>
/// </summary>
public class ProjektVarianteDialogTests : EposBunitContext
{
    /// <summary>Drei Projekte — eines davon eine Variante, damit die Artspalte trägt.</summary>
    private static readonly ProjektKopfZeile[] DREI =
    {
        new ProjektKopfZeile(1030, "Musterprojekt", "Stadtwerke", "Kaskade", new DateTime(2026, 3, 1)),
        new ProjektKopfZeile(1007, "Zweitprojekt", "Kirchengemeinde", "Denkmal", new DateTime(2026, 5, 4)),
        new ProjektKopfZeile(1023, "Musterprojekt - Sommer", "Stadtwerke", "", new DateTime(2026, 6, 2),
                             StammId: 1030, Bezeichner: "Sommer", StammName: "Musterprojekt")
    };

    public ProjektVarianteDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Die Kernregel des Zielnamens als Prüfdoppel (<c>VariantenCtrl.Zielname</c>).</summary>
    private static readonly Func<string, string> REGEL = b => "Musterprojekt - " + b;

    private IRenderedComponent<ProjektVarianteDialog> Aufbauen(
        Action<ProjektVarianteWahl?>? beimSchliessen = null,
        Func<string, string>? zielname = null,
        Action<ComponentParameterCollectionBuilder<ProjektVarianteDialog>>? mehr = null)
        => Render<ProjektVarianteDialog>(p =>
        {
            p.Add(x => x.StammName, "Musterprojekt");
            p.Add(x => x.Zeilen, DREI);
            p.Add(x => x.HinweisText, "Der aktuelle Stand wird gesichert.");
            p.Add(x => x.Zielname, zielname ?? REGEL);
            if (beimSchliessen is not null) p.Add(x => x.Geschlossen, beimSchliessen);
            mehr?.Invoke(p);
        });

    // =====================================================================
    //  Die Bedienelemente
    // =====================================================================

    /// <summary>Das Bezeichnerfeld — NICHT das Suchfeld der Projektliste.</summary>
    private static AngleSharp.Dom.IElement Bezeichnerfeld(IRenderedComponent<ProjektVarianteDialog> cut)
        => cut.Find(".epos-dialog > .epos-feld input");

    private static AngleSharp.Dom.IElement Haken(IRenderedComponent<ProjektVarianteDialog> cut)
        => cut.Find(".epos-schalter-kasten");

    private static AngleSharp.Dom.IElement Anlegen(IRenderedComponent<ProjektVarianteDialog> cut)
        => cut.Find(".epos-dialog > .epos-leiste .epos-knopf--primaer");

    private static AngleSharp.Dom.IElement Abbrechen(IRenderedComponent<ProjektVarianteDialog> cut)
        => cut.FindAll(".epos-dialog > .epos-leiste button")
              .First(k => !k.ClassList.Contains("epos-knopf--primaer"));

    /// <summary>
    /// Waehlt die Zeile mit diesem Projektnamen. ueber den NAMEN und nicht ueber
    /// eine Zeilennummer: Die Liste stellt eine Variante unter ihren Stamm
    /// (W15a-E-1), die Reihenfolge ist also nicht die der uebergebenen Zeilen.
    /// </summary>
    private static void Waehlen(IRenderedComponent<ProjektVarianteDialog> cut, string name)
    {
        var titel = cut.FindAll("tbody .epos-projektliste-titel");
        int i = 0;
        while (i < titel.Count && titel[i].TextContent != name) i++;
        Assert.True(i < titel.Count, "Keine Zeile mit dem Namen " + name + ".");
        cut.FindAll("tbody .epos-anlagenwahl")[i].Click();
    }

    // =====================================================================
    //  Ohne Haken: der bisherige Weg
    // =====================================================================

    [Fact]
    public void Ohne_Haken_steht_der_bisherige_Dialog_da()
    {
        var cut = Aufbauen();

        Assert.Equal("Als Variante speichern", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-schalter-kasten"));
        Assert.Empty(cut.FindAll(".epos-projektliste"));       // die Liste ist zu
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-leiste button").Count);
        Assert.Contains("Der aktuelle Stand wird gesichert.",
                        cut.Find(".epos-herleitung").TextContent);
    }

    [Fact]
    public void Ohne_Bezeichner_bleibt_Variante_anlegen_gesperrt()
    {
        var cut = Aufbauen();

        Assert.False(cut.Instance.OkErlaubt);
        Assert.True(Anlegen(cut).HasAttribute("disabled"));

        Bezeichnerfeld(cut).Input("   ");
        Assert.False(cut.Instance.OkErlaubt);

        Bezeichnerfeld(cut).Input("Sommerbetrieb");
        Assert.True(cut.Instance.OkErlaubt);
        Assert.False(Anlegen(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Ohne_Haken_liefert_der_Dialog_den_getrimmten_Bezeichner_und_kein_Quellprojekt()
    {
        ProjektVarianteWahl? ergebnis = null;
        var cut = Aufbauen(w => ergebnis = w);

        Bezeichnerfeld(cut).Input("  Sommerbetrieb  ");
        Anlegen(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal("Sommerbetrieb", ergebnis!.Value.Bezeichner);
        Assert.Equal(0, ergebnis.Value.IdQuelle);
    }

    [Fact]
    public void Abbrechen_meldet_nichts()
    {
        bool gemeldet = false;
        ProjektVarianteWahl? ergebnis = null;
        var cut = Aufbauen(w => { gemeldet = true; ergebnis = w; });

        Bezeichnerfeld(cut).Input("Sommerbetrieb");
        Abbrechen(cut).Click();

        Assert.True(gemeldet);
        Assert.Null(ergebnis);
    }

    // =====================================================================
    //  Mit Haken: das Quellprojekt
    // =====================================================================

    [Fact]
    public void Der_Haken_blendet_die_Projektliste_ein()
    {
        var cut = Aufbauen();

        Haken(cut).Change(true);

        Assert.True(cut.Instance.AusQuelle);
        Assert.Single(cut.FindAll(".epos-projektliste"));
        Assert.Equal(DREI.Length, cut.FindAll("tbody .epos-anlagenwahl").Count);

        // Keine Vorauswahl: Der Anwender wählt ausdrücklich.
        Assert.Equal(0, cut.Instance.IdQuelle);
    }

    [Fact]
    public void Mit_Haken_aber_ohne_Auswahl_bleibt_der_Knopf_gesperrt_und_sagt_warum()
    {
        var cut = Aufbauen(mehr: p => p.Add(x => x.MeldungQuelleWaehlen, "Bitte ein Projekt wählen."));

        Bezeichnerfeld(cut).Input("Sommerbetrieb");
        Assert.True(cut.Instance.OkErlaubt);

        Haken(cut).Change(true);

        Assert.False(cut.Instance.OkErlaubt);
        Assert.True(Anlegen(cut).HasAttribute("disabled"));
        Assert.Equal("Bitte ein Projekt wählen.", cut.Find(".epos-warnbanner-text").TextContent);
    }

    [Fact]
    public void Die_Projektwahl_belegt_den_Bezeichner_mit_dem_Projektnamen_vor()
    {
        // Der Anwenderwunsch woertlich: „default ist Variantenbezeichner =
        // Projektname des ausgewaehlten zu uebernehmenden Projektes".
        var cut = Aufbauen();

        Haken(cut).Change(true);
        Waehlen(cut, "Zweitprojekt");

        Assert.Equal("Zweitprojekt", cut.Instance.Bezeichner);
        Assert.Equal("Zweitprojekt", Bezeichnerfeld(cut).GetAttribute("value"));
        Assert.Equal(1007, cut.Instance.IdQuelle);
        Assert.True(cut.Instance.OkErlaubt);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    [Fact]
    public void Ein_Auswahlwechsel_zieht_die_Vorbelegung_nach()
    {
        var cut = Aufbauen();

        Haken(cut).Change(true);
        Waehlen(cut, "Zweitprojekt");
        Assert.Equal("Zweitprojekt", cut.Instance.Bezeichner);

        Waehlen(cut, "Musterprojekt");
        Assert.Equal("Musterprojekt", cut.Instance.Bezeichner);
        Assert.Equal(1030, cut.Instance.IdQuelle);
    }

    [Fact]
    public void Ein_von_Hand_getippter_Bezeichner_bleibt_bei_jeder_Auswahl_stehen()
    {
        // DER VON-HAND-MERKER. Ohne ihn loescht jede Projektwahl dem Anwender
        // seinen eigenen Text weg.
        var cut = Aufbauen();

        Bezeichnerfeld(cut).Input("Meine Fassung");
        Haken(cut).Change(true);
        Waehlen(cut, "Zweitprojekt");

        Assert.Equal("Meine Fassung", cut.Instance.Bezeichner);
        Assert.Equal(1007, cut.Instance.IdQuelle);

        Waehlen(cut, "Musterprojekt");
        Assert.Equal("Meine Fassung", cut.Instance.Bezeichner);
    }

    [Fact]
    public void Mit_Haken_liefert_der_Dialog_Bezeichner_UND_Quellprojekt()
    {
        ProjektVarianteWahl? ergebnis = null;
        var cut = Aufbauen(w => ergebnis = w);

        Haken(cut).Change(true);
        Waehlen(cut, "Musterprojekt - Sommer");        // eine VARIANTE als Quelle
        Bezeichnerfeld(cut).Input("  Aus der Variante  ");
        Anlegen(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal("Aus der Variante", ergebnis!.Value.Bezeichner);
        Assert.Equal(1023, ergebnis.Value.IdQuelle);
    }

    [Fact]
    public void Ein_abgewaehltes_Kaestchen_gibt_das_Quellprojekt_wieder_ab()
    {
        ProjektVarianteWahl? ergebnis = null;
        var cut = Aufbauen(w => ergebnis = w);

        Haken(cut).Change(true);
        Waehlen(cut, "Zweitprojekt");
        Haken(cut).Change(false);

        Assert.Empty(cut.FindAll(".epos-projektliste"));
        Anlegen(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(0, ergebnis!.Value.IdQuelle);
    }

    // =====================================================================
    //  Zielname und Hinweissatz
    // =====================================================================

    [Fact]
    public void Der_Zielname_kommt_aus_der_Kernregel_und_steht_live_da()
    {
        // Der Dialog baut den Namen NICHT selbst - er fragt den Delegaten.
        var cut = Aufbauen(zielname: b => "Musterprojekt - " + b + " (2)",
                           mehr: p => p.Add(x => x.ZielnameFormat, "Neuer Projektname: {0}"));

        Assert.DoesNotContain(cut.FindAll(".epos-herleitung"),
                              e => e.TextContent.Contains("Neuer Projektname"));

        Bezeichnerfeld(cut).Input("Sommerbetrieb");

        Assert.Contains(cut.FindAll(".epos-herleitung"),
                        e => e.TextContent.Contains("Neuer Projektname: Musterprojekt - Sommerbetrieb (2)"));
    }

    [Fact]
    public void Ohne_Namensregel_bleibt_die_Vorschau_leer()
    {
        var cut = Render<ProjektVarianteDialog>(p =>
        {
            p.Add(x => x.StammName, "Musterprojekt");
            p.Add(x => x.Zeilen, DREI);
            p.Add(x => x.HinweisText, "Der aktuelle Stand wird gesichert.");
        });

        Bezeichnerfeld(cut).Input("Sommerbetrieb");

        Assert.Equal("", cut.Instance.Zielzeile);
    }

    [Fact]
    public void Der_Hinweissatz_folgt_dem_Zustand()
    {
        var cut = Aufbauen(mehr: p => p.Add(x => x.HinweisQuelleFormat,
                                            "Quelle {0}, Ziel {1}, Stamm {2}."));

        Assert.Equal("Der aktuelle Stand wird gesichert.", cut.Instance.HinweisZeile);

        Haken(cut).Change(true);
        Assert.Equal("Der aktuelle Stand wird gesichert.", cut.Instance.HinweisZeile);

        Waehlen(cut, "Zweitprojekt");
        Assert.Equal("Quelle Zweitprojekt, Ziel Musterprojekt - Zweitprojekt, Stamm Musterprojekt.",
                     cut.Instance.HinweisZeile);
    }

    // =====================================================================
    //  Die Platzhalter-Wache (Lehre aus Befund #217)
    // =====================================================================

    /// <summary>
    /// Die drei Formatsätze des Dialogs werden mit den ECHTEN Ressourcentexten
    /// gefüllt — in beiden Sprachen. Trüge eine Ressource einen Platzhalter mehr,
    /// als der Dialog Argumente gibt, flöge hier eine <see cref="FormatException"/>,
    /// genau wie beim Anwender.
    /// </summary>
    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    public void Die_echten_Ressourcentexte_tragen_genau_die_Platzhalter_des_Dialogs(string kultur)
    {
        using var _ = new Kulturvorrichtung(kultur);

        var cut = Render<ProjektVarianteDialog>(p =>
        {
            p.Add(x => x.StammName, "Musterprojekt");
            p.Add(x => x.Zeilen, DREI);
            p.Add(x => x.Zielname, (Func<string, string>)(b => "Musterprojekt - " + b));
            p.Add(x => x.TitelText, Resource.VAR_DLG_TITEL);
            p.Add(x => x.HinweisText,
                  string.Format(Resource.VAR_DLG_HINWEIS, "Musterprojekt"));
            p.Add(x => x.HinweisQuelleFormat, Resource.VAR_DLG_HINWEIS_QUELLE);
            p.Add(x => x.QuelleHakenText, Resource.VAR_DLG_QUELLE_HAKEN);
            p.Add(x => x.ZielnameFormat, Resource.VAR_DLG_ZIELNAME);
            p.Add(x => x.MeldungQuelleWaehlen, Resource.VAR_MSG_QUELLE_WAEHLEN);
            p.Add(x => x.FrageText, Resource.BK_LBL_BEZEICHNER);
            p.Add(x => x.OkText, Resource.BK_BTN_ANLEGEN);
            p.Add(x => x.AbbrechenText, Resource.SIM_BTN_ABBRECHEN);
        });

        // Ohne Haken: der Satz des Bildschirmfotos, unverändert.
        Assert.Contains("Musterprojekt", cut.Instance.HinweisZeile);
        Assert.DoesNotContain("{0}", cut.Instance.HinweisZeile);

        Haken(cut).Change(true);
        Assert.Equal(Resource.VAR_MSG_QUELLE_WAEHLEN,
                     cut.Find(".epos-warnbanner-text").TextContent);

        Waehlen(cut, "Zweitprojekt");

        // Mit Haken: drei Platzhalter, drei Argumente.
        string hinweis = cut.Instance.HinweisZeile;
        Assert.Contains("Zweitprojekt", hinweis);
        Assert.Contains("Musterprojekt - Zweitprojekt", hinweis);
        foreach (string p in new[] { "{0}", "{1}", "{2}" }) Assert.DoesNotContain(p, hinweis);

        // Und die Vorschauzeile: ein Platzhalter, ein Argument.
        Assert.Contains("Musterprojekt - Zweitprojekt", cut.Instance.Zielzeile);
        Assert.DoesNotContain("{0}", cut.Instance.Zielzeile);
    }

    /// <summary>
    /// GEGENPROBE des Zahlenlesers oben: Die zwei Formatsätze tragen WIRKLICH die
    /// Platzhalterzahl, die der Dialog erwartet — drei beim Hinweissatz, einen bei
    /// der Vorschauzeile. Ohne diesen Fall bliebe der Satz „genau die Platzhalter"
    /// auch dann grün, wenn beide Texte gar keinen trügen.
    /// </summary>
    [Fact]
    public void Die_Ressourcen_tragen_drei_beziehungsweise_einen_Platzhalter()
    {
        using var _ = new Kulturvorrichtung();

        foreach (string text in new[] { Resource.VAR_DLG_HINWEIS_QUELLE })
        {
            Assert.Contains("{0}", text);
            Assert.Contains("{1}", text);
            Assert.Contains("{2}", text);
        }

        Assert.Contains("{0}", Resource.VAR_DLG_ZIELNAME);
        Assert.DoesNotContain("{1}", Resource.VAR_DLG_ZIELNAME);

        Assert.Contains("{0}", Resource.VAR_DLG_HINWEIS);
        Assert.DoesNotContain("{1}", Resource.VAR_DLG_HINWEIS);

        Assert.DoesNotContain("{", Resource.VAR_DLG_QUELLE_HAKEN);
        Assert.DoesNotContain("{", Resource.VAR_MSG_QUELLE_WAEHLEN);
    }

    /// <summary>
    /// Der TITEL steht genau EINMAL (Hausregel W11b‑B‑9): Leerer
    /// <c>TitelText</c> heißt „die Überlagerung trägt ihn", und dann zeichnet der
    /// Dialog keinen eigenen.
    /// </summary>
    [Fact]
    public void Ein_leerer_Titel_laesst_den_Kopf_ohne_Ueberschrift()
    {
        var cut = Aufbauen(mehr: p => p.Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-dialog-kopf--ohnetitel"));
    }
}

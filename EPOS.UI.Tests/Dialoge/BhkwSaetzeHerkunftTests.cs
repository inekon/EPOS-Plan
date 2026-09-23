using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// ETAPPE E7c — die Überlagerung <b>„Sätze und Herkunft"</b> des BHKW-Dialogs (Mockup
/// U22), gebaut so weit, wie die zwei Felder des zweiten Falls des § 2 Nr. 16 KWKG es
/// brauchen (Befund K‑1, Entscheid E7‑Q2 (5)): die Wahl Fall 1 / Fall 2 (Kennzeichen
/// „Vorrichtung zur Abwärmeabfuhr") und die Stromkennzahl σ mit Vorschlag
/// (P_el ÷ P_th der Gerätezeile), Herkunft, eigenem Wert und „gilt".
///
/// <para>Geprüft wird: das Öffnen mit Titel und dem Arbeitsstand der gewählten Anlage;
/// „Übernehmen" legt Kennzeichen und Kennzahl auf den Arbeitsstand (geschrieben wird
/// erst im OK-Weg, dann mit beiden Feldern); Abbrechen und Esc ändern nichts; die
/// Spalte „gilt" nennt Vorschlag, eigenen Wert oder — ohne P_th und ohne eigenen Wert
/// — keine Kennzahl, und der Vorschlagsknopf ist dann weich gesperrt.</para>
/// </summary>
public class BhkwSaetzeHerkunftTests : EposBunitContext
{
    private const int STAMM = 1030;
    private const string GROSS = "BHKW EW M 50 S [K] Erdgas";

    public BhkwSaetzeHerkunftTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static List<KwkgAnlagenAngabe> Anlagen(double? pthGross = 81.0) => new List<KwkgAnlagenAngabe>
    {
        new KwkgAnlagenAngabe
        {
            IdAnlage = 14920, IdProjekt = STAMM, Projektname = "Stamm", Bezeichner = GROSS,
            PelKW = 50, PthKW = pthGross, Brennstoffname = "Erdgas E"
        },
        new KwkgAnlagenAngabe
        {
            IdAnlage = 14921, IdProjekt = STAMM, Projektname = "Stamm", Bezeichner = "EC-POWER XRGI 9",
            PelKW = 9, PthKW = 20.1, Brennstoffname = "Erdgas E"
        }
    };

    private IRenderedComponent<BhkwWirtschaftlichkeitDialog> Aufbauen(
        IList<KwkgAnlagenAngabe> anlagen,
        Func<KwkgAnlagenAngabe, bool>? speichereAnlage = null,
        Action<BhkwWirtschaftlichkeitErgebnis>? beimSchliessen = null)
        => Render<BhkwWirtschaftlichkeitDialog>(p => p
            .Add(x => x.IdStamm, STAMM)
            .Add(x => x.StammName, "Musterprojekt")
            .Add(x => x.Anlagen, anlagen)
            .Add(x => x.Parameter, new WirtschaftlichkeitParameter())
            .Add(x => x.Doppelpflege, Array.Empty<KohaerenzHinweis>())
            .Add(x => x.ErgebnisseAusLauf, Array.Empty<WirtschaftlichkeitErgebnis>())
            .Add(x => x.SpeichereAnlage, speichereAnlage ?? (_ => true))
            .Add(x => x.SpeichereVorgaben, _ => true)
            .Add(x => x.Geschlossen, beimSchliessen ?? (_ => { })));

    private static void Oeffnen(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut)
        => cut.Find("button.epos-ueb-oeffnen").Click();

    private static IElement Ueb(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut)
        => cut.Find("div.epos-ueberlagerung");

    private static IElement Sigmazeile(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut)
        => cut.Find("tr.epos-ueb-sigma");

    private static string Zelle(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, int nr)
        => Sigmazeile(cut).QuerySelectorAll("td")[nr].TextContent.Trim();

    private static void Fall(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, int nr)
        => Ueb(cut).QuerySelectorAll("input[type=radio]")[nr].Change(true);

    private static IElement Knopf(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, string text)
        => Ueb(cut).QuerySelectorAll("button").First(b => b.TextContent.Trim() == text);

    // =====================================================================

    [Fact]
    public void Der_Knopf_oeffnet_die_Ueberlagerung_mit_Titel_Fall_und_Vorschlag()
    {
        var cut = Aufbauen(Anlagen());
        Assert.Empty(cut.FindAll("div.epos-ueberlagerung"));
        Assert.Contains("KWK-Strom (§ 2 Nr. 16 KWKG): Fall 1 — Nettostromerzeugung.",
                        cut.Find("p.epos-kwkstrom").TextContent);

        Oeffnen(cut);

        Assert.True(cut.Instance.UeberlagerungOffen);
        Assert.Equal("Sätze und Herkunft — " + GROSS, Ueb(cut).QuerySelector("h2")!.TextContent);
        var radios = Ueb(cut).QuerySelectorAll("input[type=radio]");
        Assert.Equal(2, radios.Length);
        Assert.True(radios[0].HasAttribute("checked"));          // Fall 1 ist der Bestand
        Assert.Contains("Fall 2 — Vorrichtung zur Abwärmeabfuhr", Ueb(cut).TextContent);

        Assert.Equal("Stromkennzahl σ", Zelle(cut, 0));
        Assert.Equal("0,617", Zelle(cut, 1));                    // 50 ÷ 81
        Assert.Equal("P_el ÷ P_th der Gerätezeile: 50,0 kW ÷ 81,0 kW", Zelle(cut, 2));
        Assert.Equal("— (Fall 1 liest keine Stromkennzahl)", Zelle(cut, 4));
    }

    /// <summary>
    /// „Übernehmen" legt Kennzeichen und eigene Kennzahl auf den ARBEITSSTAND — die
    /// geladene Zeile bleibt bis zum OK unberührt; im OK-Weg reisen beide Felder mit.
    /// </summary>
    [Fact]
    public void Uebernehmen_legt_Kennzeichen_und_Kennzahl_auf_den_Arbeitsstand()
    {
        var anlagen = Anlagen();
        KwkgAnlagenAngabe? geschrieben = null;
        var cut = Aufbauen(anlagen, a => { if (a.IdAnlage == 14920) geschrieben = a; return true; });

        Oeffnen(cut);
        Fall(cut, 1);
        Sigmazeile(cut).QuerySelector("input[inputmode=decimal]")!.Input("0,5");
        Assert.Equal("0,500 eigener Wert — Vorschlag 0,617", Zelle(cut, 4));
        Knopf(cut, "Übernehmen").Click();

        Assert.False(cut.Instance.UeberlagerungOffen);
        Assert.True(cut.Instance.AktuellerStand!.Abwaermeabfuhr);
        Assert.Equal(0.5, cut.Instance.AktuellerStand!.Stromkennzahl);
        Assert.True(cut.Instance.Geaendert);
        Assert.False(anlagen[0].Abwaermeabfuhr);                  // noch nicht angewendet
        Assert.Contains("Fall 2 — Vorrichtung zur Abwärmeabfuhr, Stromkennzahl σ 0,500 (gepflegt).",
                        cut.Find("p.epos-kwkstrom").TextContent);

        cut.FindAll(".epos-leiste button")[1].Click();            // OK
        Assert.NotNull(geschrieben);
        Assert.True(geschrieben!.Abwaermeabfuhr);
        Assert.Equal(0.5, geschrieben.Stromkennzahl);
    }

    /// <summary>Leer heißt Vorschlag; „Vorschlag übernehmen" leert den eigenen Wert.</summary>
    [Fact]
    public void Leer_heisst_Vorschlag_und_der_Knopf_fuehrt_zurueck()
    {
        var cut = Aufbauen(Anlagen());
        Oeffnen(cut);
        Fall(cut, 1);
        Assert.Equal("0,617 Vorschlag", Zelle(cut, 4));

        Sigmazeile(cut).QuerySelector("input[inputmode=decimal]")!.Input("0,5");
        Assert.Equal("0,500 eigener Wert — Vorschlag 0,617", Zelle(cut, 4));

        Sigmazeile(cut).QuerySelector("button.epos-ueb-vorschlag")!.Click();
        Assert.Equal("0,617 Vorschlag", Zelle(cut, 4));
        Knopf(cut, "Übernehmen").Click();

        Assert.True(cut.Instance.AktuellerStand!.Abwaermeabfuhr);
        Assert.Null(cut.Instance.AktuellerStand!.Stromkennzahl);  // leer = berechnet
        Assert.Contains("σ 0,617 (berechnet aus P_el ÷ P_th der Gerätezeile = 50,0 kW ÷ 81,0 kW)",
                        cut.Find("p.epos-kwkstrom").TextContent);
    }

    /// <summary>
    /// Ohne P_th in der Gerätezeile und ohne eigenen Wert gibt es keine Kennzahl — keinen
    /// Ersatz, keine Vorgabe (Auflage zu E7‑Q2 (2)); der Vorschlagsknopf ist weich
    /// gesperrt und nennt den Grund.
    /// </summary>
    [Fact]
    public void Ohne_Pth_gibt_es_keinen_Vorschlag_und_keine_Kennzahl()
    {
        var cut = Aufbauen(Anlagen(pthGross: null));
        Oeffnen(cut);
        Fall(cut, 1);

        Assert.Equal("—", Zelle(cut, 1));
        Assert.Equal("Gerätezeile ohne P_el oder P_th — kein Vorschlag", Zelle(cut, 2));
        Assert.Equal("keine — kein KWK-Strom nach Fall 2, kein Zuschlag", Zelle(cut, 4));
        IElement knopf = Sigmazeile(cut).QuerySelector("button.epos-ueb-vorschlag")!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Gerätezeile ohne P_el oder P_th — kein Vorschlag", knopf.GetAttribute("title"));

        Knopf(cut, "Übernehmen").Click();
        Assert.Contains("aber keine Stromkennzahl", cut.Find("p.epos-kwkstrom").TextContent);
        Assert.Contains("kein Zuschlag nach Fall 2", cut.Find("p.epos-kwkstrom").TextContent);
    }

    /// <summary>Abbrechen und Esc schließen die Überlagerung, ohne etwas zu übernehmen —
    /// und Esc erreicht den Dialog dahinter nicht.</summary>
    [Fact]
    public void Abbrechen_und_Esc_aendern_nichts()
    {
        bool dialogZu = false;
        var cut = Aufbauen(Anlagen(), beimSchliessen: _ => dialogZu = true);

        Oeffnen(cut);
        Fall(cut, 1);
        Sigmazeile(cut).QuerySelector("input[inputmode=decimal]")!.Input("0,4");
        Knopf(cut, "Abbrechen").Click();

        Assert.False(cut.Instance.UeberlagerungOffen);
        Assert.False(cut.Instance.AktuellerStand!.Abwaermeabfuhr);
        Assert.Null(cut.Instance.AktuellerStand!.Stromkennzahl);
        Assert.False(cut.Instance.Geaendert);

        Oeffnen(cut);
        Fall(cut, 1);
        Ueb(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.UeberlagerungOffen);
        Assert.False(cut.Instance.AktuellerStand!.Abwaermeabfuhr);
        Assert.False(dialogZu);
    }

    /// <summary>Der Arbeitsstand vergleicht die zwei neuen Felder mit — sonst schriebe
    /// der Sprungweg eine Änderung an ihnen nicht.</summary>
    [Fact]
    public void Der_Arbeitsstand_vergleicht_Kennzeichen_und_Kennzahl()
    {
        var a = new KwkgAnlagenAngabe { IdAnlage = 1, Abwaermeabfuhr = true, Stromkennzahl = 0.6 };
        BhkwAnlagenstand st = BhkwAnlagenstand.Aus(a);
        Assert.True(st.Abwaermeabfuhr);
        Assert.Equal(0.6, st.Stromkennzahl);
        Assert.True(st.Gleicht(a));

        st.Stromkennzahl = 0.5;
        Assert.False(st.Gleicht(a));
        st.Stromkennzahl = 0.6;
        st.Abwaermeabfuhr = false;
        Assert.False(st.Gleicht(a));

        st.Anwenden(a);
        Assert.False(a.Abwaermeabfuhr);
    }
}

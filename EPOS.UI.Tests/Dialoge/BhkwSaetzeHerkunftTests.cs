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
/// <para><b>ETAPPE E7c2 (E7c1‑Q7):</b> der Rest der Überlagerung — Anlagenart und
/// Tatbestand mit der Wirkung je Wahl, die Satztafel (Einspeisung, Eigenstrom, Kontingent,
/// Deckel) mit Vorschlag, Herkunft, eigenem Wert und „gilt", die „Wirkung Jahr 1" aus
/// <c>KwkgJahresbetrag</c>, Energie- und Stromsteuer samt Geltung Projekt/Anlage und der
/// zweite Knopf „Wahl und Herkunft…" (Fälle ab
/// <see cref="Die_Ueberlagerung_fuehrt_Anlagenart_Tatbestand_und_Satztafel"/>).</para>
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
        Action<BhkwWirtschaftlichkeitErgebnis>? beimSchliessen = null,
        Func<string, int, GesetzParameter>? katalog = null,
        IReadOnlyList<WirtschaftlichkeitErgebnis>? ergebnisse = null)
        => Render<BhkwWirtschaftlichkeitDialog>(p => p
            .Add(x => x.IdStamm, STAMM)
            .Add(x => x.StammName, "Musterprojekt")
            .Add(x => x.Anlagen, anlagen)
            .Add(x => x.Parameter, new WirtschaftlichkeitParameter())
            .Add(x => x.Katalog, katalog)
            .Add(x => x.Doppelpflege, Array.Empty<KohaerenzHinweis>())
            .Add(x => x.ErgebnisseAusLauf, ergebnisse ?? Array.Empty<WirtschaftlichkeitErgebnis>())
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
        => Wahl(cut, "epos-ueb-fall", nr);

    /// <summary>Wählt in der Gruppe mit der Klasse <paramref name="gruppe"/> den Eintrag
    /// <paramref name="nr"/> (E7c2: die Überlagerung trägt mehrere Wahlgruppen).</summary>
    private static void Wahl(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, string gruppe, int nr)
        => Ueb(cut).QuerySelectorAll("." + gruppe + " input[type=radio]")[nr].Change(true);

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
        var radios = Ueb(cut).QuerySelectorAll(".epos-ueb-fall input[type=radio]");
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

    // =====================================================================
    //  ETAPPE E7c2 (E7c1‑Q7) — der Rest der Überlagerung
    // =====================================================================

    /// <summary>Ein Katalog mit echter Staffel (8 / 6 / 5 / 4,4 ct/kWh Einspeisung,
    /// 4 / 3 / 2,5 / 2,2 ct/kWh Eigenstrom Nr. 2), den Kontingenten des § 8 und dem
    /// Jahresdeckel 3.100 h/a (2027).</summary>
    private static Func<string, int, GesetzParameter> Katalog() => (schluessel, jahr) =>
    {
        double w = 0;
        string einheit = "ct/kWh";
        if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1) w = 50;
        else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2) w = 100;
        else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3) w = 250;
        else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4) w = 2000;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW) w = 8.0;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW) w = 6.0;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW) w = 5.0;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW) w = 4.4;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW) w = 4.0;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW) w = 3.0;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW) w = 2.5;
        else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW) w = 2.2;
        else if (schluessel == DbWerte.GESETZ_KWKG_VBH_NEUANLAGE) { w = 30000; einheit = "h"; }
        else if (schluessel == DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL) { w = 3100; einheit = "h/a"; }
        return w > 0 ? new GesetzParameter(1, schluessel, "KWKG", 2020, w, einheit, "", "") : null!;
    };

    /// <summary>Die große Anlage mit 100 kW (über der Grenze des § 7 Abs. 3a) als neue
    /// Anlage mit Tatbestand Nr. 2, Inbetriebnahme 2027 und den Sätzen des Vorschlags:
    /// Einspeisung 50 × 8 + 50 × 6 = 700 ÷ 100 = 7,0 ct/kWh, Eigenstrom 50 × 4 + 50 × 3 =
    /// 350 ÷ 100 = 3,5 ct/kWh.</summary>
    private static List<KwkgAnlagenAngabe> AnlagenMitSaetzen()
    {
        List<KwkgAnlagenAngabe> l = Anlagen();
        l[0].PelKW = 100;
        l[0].Inbetriebnahme = new DateTime(2027, 1, 1);
        l[0].Anlagenart = DbWerte.KWKG_ANLAGENART_NEU;
        l[0].Eigenfall = DbWerte.KWKG_EIGENFALL_NR2;
        l[0].SatzEinspCt = 7.0;
        l[0].SatzEigenCt = 3.5;
        return l;
    }

    private static IElement Tafelzeile(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, string klasse)
        => Ueb(cut).QuerySelector("tr." + klasse)!;

    private static string Tafelzelle(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, string klasse, int nr)
        => Tafelzeile(cut, klasse).QuerySelectorAll("td")[nr].TextContent.Trim();

    private static IElement Gruppe(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, string klasse)
        => Ueb(cut).QuerySelector("div." + klasse)!;

    /// <summary>
    /// Anlagenart und Tatbestand stehen als Wahl mit ihrer Wirkung (Kontingent nach § 8,
    /// Eigenstromsatz nach § 7 Abs. 2); die Satztafel nennt je Größe Vorschlag, Herkunft
    /// und „gilt". Ein Satz, der dem Vorschlag entspricht, erscheint als leeres Feld.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_fuehrt_Anlagenart_Tatbestand_und_Satztafel()
    {
        var cut = Aufbauen(AnlagenMitSaetzen(), katalog: Katalog());
        Oeffnen(cut);

        var art = Gruppe(cut, "epos-ueb-art").QuerySelectorAll("input[type=radio]");
        Assert.Equal(3, art.Length);                               // ohne „(bitte wählen)"
        Assert.True(art[0].HasAttribute("checked"));               // neue Anlage
        Assert.Contains("→ 30.000 Vbh", Gruppe(cut, "epos-ueb-art").TextContent);

        var fall = Gruppe(cut, "epos-ueb-eigenfall").QuerySelectorAll("input[type=radio]");
        Assert.Equal(4, fall.Length);
        Assert.True(fall[2].HasAttribute("checked"));              // Nr. 2
        Assert.Contains("→ 3,5000 ct/kWh", Gruppe(cut, "epos-ueb-eigenfall").TextContent);

        Assert.Equal("7,0000 ct/kWh", Tafelzelle(cut, "epos-ueb-einsp", 1));
        Assert.Equal("7,0000 Vorschlag", Tafelzelle(cut, "epos-ueb-einsp", 4));
        Assert.Equal("3,5000 Vorschlag", Tafelzelle(cut, "epos-ueb-eigen", 4));
        Assert.Equal("30.000 Vbh", Tafelzelle(cut, "epos-ueb-kontingent", 1));
        Assert.Equal("30.000 Vorschlag", Tafelzelle(cut, "epos-ueb-kontingent", 4));
        Assert.Equal("Staffel — 3.100 h/a im Jahr 2027", Tafelzelle(cut, "epos-ueb-deckel", 4));
        Assert.Contains("KWKG_VBH_JAHRESDECKEL", Tafelzelle(cut, "epos-ueb-deckel", 2));
    }

    /// <summary>
    /// Ein Satzfeld ohne Wert ist im Formular die 0 „kein Zuschlag" — die Überlagerung
    /// zeigt sie als eigenen Wert 0, damit ein unberührtes „Übernehmen" nicht still den
    /// Vorschlag hineinschreibt; „Vorschlag übernehmen" leert das Feld, und dann schreibt
    /// „Übernehmen" den Vorschlag ins Formular.
    /// </summary>
    [Fact]
    public void Leeres_Satzfeld_bleibt_kein_Zuschlag_bis_der_Vorschlag_gewaehlt_ist()
    {
        List<KwkgAnlagenAngabe> anlagen = AnlagenMitSaetzen();
        anlagen[0].SatzEigenCt = null;
        var cut = Aufbauen(anlagen, katalog: Katalog());

        Oeffnen(cut);
        Assert.Equal("0,0000 eigener Wert — Vorschlag 3,5000", Tafelzelle(cut, "epos-ueb-eigen", 4));
        Knopf(cut, "Übernehmen").Click();
        Assert.Null(cut.Instance.AktuellerStand!.SatzEigenCt);
        Assert.False(cut.Instance.Geaendert);

        Oeffnen(cut);
        Tafelzeile(cut, "epos-ueb-eigen").QuerySelector("button.epos-ueb-vorschlag")!.Click();
        Assert.Equal("3,5000 Vorschlag", Tafelzelle(cut, "epos-ueb-eigen", 4));
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal(3.5, cut.Instance.AktuellerStand!.SatzEigenCt);
        Assert.True(cut.Instance.Geaendert);
    }

    /// <summary>
    /// „Übernehmen" schreibt Sätze, Deckel, die Energiesteuer der Anlage („nur diese
    /// Anlage") und die Stromsteuer des Projekts in die Arbeitsstände — die geladenen
    /// Objekte bleiben bis zum OK unberührt.
    /// </summary>
    [Fact]
    public void Uebernehmen_schreibt_Saetze_Deckel_und_beide_Steuern()
    {
        List<KwkgAnlagenAngabe> anlagen = AnlagenMitSaetzen();
        var cut = Aufbauen(anlagen, katalog: Katalog());
        Oeffnen(cut);

        Tafelzeile(cut, "epos-ueb-einsp").QuerySelector("input[inputmode=decimal]")!.Input("6,5");
        Assert.Equal("6,5000 eigener Wert — Vorschlag 7,0000", Tafelzelle(cut, "epos-ueb-einsp", 4));
        Tafelzeile(cut, "epos-ueb-deckel").QuerySelector("input")!.Input("3000");

        Wahl(cut, "epos-ueb-es-scope", 1);                          // nur diese Anlage
        Wahl(cut, "epos-ueb-es-wahl", 2);                           // § 53a Abs. 5
        Wahl(cut, "epos-ueb-ua", 1);                                // produzierendes Gewerbe
        Wahl(cut, "epos-ueb-modus", 1);                             // Erlös
        Ueb(cut).QuerySelectorAll("input.epos-schalter-kasten")[0].Change(true);   // Hocheffizienz

        Knopf(cut, "Übernehmen").Click();

        BhkwAnlagenstand st = cut.Instance.AktuellerStand!;
        Assert.Equal(6.5, st.SatzEinspCt);
        Assert.Equal(3.5, st.SatzEigenCt);
        Assert.Equal(3000, st.VbhDeckel);
        Assert.Null(st.VbhKontingent);                               // leer = nach § 8 abgeleitet
        Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_53A, st.EnergiesteuerWahl);
        Assert.Equal(DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF, st.AufteilungMethode);
        BhkwVorgabenstand p = cut.Instance.Vorgabenstand;
        Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_KEINE, p.EnergiesteuerWahl);   // das Projekt bleibt
        Assert.Equal(DbWerte.UNTERNEHMENSART_PROD_GEWERBE, p.Unternehmensart);
        Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES, p.StromsteuerBefreiungModus);
        Assert.True(p.HocheffizienzNachweis);
        Assert.True(cut.Instance.Geaendert);
        Assert.Equal(7.0, anlagen[0].SatzEinspCt);                   // noch nicht angewendet
    }

    /// <summary>Die Projektvorgabe der Energiesteuer gilt für alle Anlagen: Die Anlage
    /// folgt ihr dann wieder, ihre zwei Felder werden leer.</summary>
    [Fact]
    public void Projektvorgabe_der_Energiesteuer_leert_die_Wahl_der_Anlage()
    {
        List<KwkgAnlagenAngabe> anlagen = AnlagenMitSaetzen();
        anlagen[0].EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_53;
        var cut = Aufbauen(anlagen, katalog: Katalog());
        Oeffnen(cut);

        var scope = Gruppe(cut, "epos-ueb-es-scope").QuerySelectorAll("input[type=radio]");
        Assert.True(scope[1].HasAttribute("checked"));               // nur diese Anlage
        Assert.True(Gruppe(cut, "epos-ueb-es-wahl").QuerySelectorAll("input[type=radio]")[1]
                        .HasAttribute("checked"));                   // § 53

        Wahl(cut, "epos-ueb-es-scope", 0);                          // Projektvorgabe
        Wahl(cut, "epos-ueb-es-wahl", 3);                           // § 54
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal("", cut.Instance.AktuellerStand!.EnergiesteuerWahl);
        Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_54, cut.Instance.Vorgabenstand.EnergiesteuerWahl);
    }

    /// <summary>Abbrechen verwirft den Zwischenstand aller drei Gruppen.</summary>
    [Fact]
    public void Abbrechen_laesst_Saetze_und_Steuern_unberuehrt()
    {
        var cut = Aufbauen(AnlagenMitSaetzen(), katalog: Katalog());
        Oeffnen(cut);
        Wahl(cut, "epos-ueb-art", 2);
        Wahl(cut, "epos-ueb-eigenfall", 0);
        Tafelzeile(cut, "epos-ueb-einsp").QuerySelector("input[inputmode=decimal]")!.Input("5");
        Wahl(cut, "epos-ueb-ua", 2);
        Knopf(cut, "Abbrechen").Click();

        BhkwAnlagenstand st = cut.Instance.AktuellerStand!;
        Assert.Equal(DbWerte.KWKG_ANLAGENART_NEU, st.Anlagenart);
        Assert.Equal(DbWerte.KWKG_EIGENFALL_NR2, st.Eigenfall);
        Assert.Equal(7.0, st.SatzEinspCt);
        Assert.Equal(DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE, cut.Instance.Vorgabenstand.Unternehmensart);
        Assert.False(cut.Instance.Geaendert);
    }

    /// <summary>
    /// „Wirkung Jahr 1": die Sätze und der Deckel des Zwischenstands auf Mengen und Stunden
    /// des gebuchten Laufs — gerechnet von <see cref="KwkgJahresbetrag"/>, demselben
    /// Ausdruck wie im Lauf; dazu die Energiesteuer der Anlage und die Stromsteuer des
    /// Projekts aus dem Lauf.
    /// </summary>
    [Fact]
    public void Wirkung_Jahr_1_ruft_den_Jahresbetrag_des_Kerns()
    {
        var e = new WirtschaftlichkeitErgebnis
        {
            IdProjekt = STAMM,
            Szenario = WirtschaftlichkeitSzenario.ERWARTET,
            StromsteuerEntlastungJahr1 = 1234.5,
            StromsteuerBefreiungJahr1 = 2345.6
        };
        e.KwkgModule.Add(new KwkgModulNachweis
        {
            Bezeichner = GROSS, PelKW = 50, VbhElektrisch = 5500,
            StromNettoMWh = 275, EigenMWh = 200, EinspeisungMWh = 75
        });
        e.EnergiesteuerNachweise.Add(new EnergiesteuerNachweis
        {
            Anlage = GROSS, Paragraf = EnergiesteuerNachweis.PARAGRAF_53A,
            Menge = 1000, Einheit = "EUR/MWh", SatzEur = 4.42, BetragEur = 4420
        });
        var cut = Aufbauen(AnlagenMitSaetzen(), katalog: Katalog(), ergebnisse: new[] { e });
        Oeffnen(cut);

        double erwartet = KwkgJahresbetrag.Voll(200, 3.5, 75, 7.0)
                        * KwkgJahresbetrag.VerguetetH(5500, 3100, 30000, 0) / 5500;
        string kwkg = Gruppe(cut, "epos-ueb-wirkung-kwkg").TextContent;
        Assert.Contains("Wirkung Jahr 1 (2027): vergütet 3.100 von 5.500 Vbh (Deckel 3.100 h/a) = 0,564 → "
                        + erwartet.ToString("N1", BhwTexte.Kultur) + " €", kwkg);
        Assert.Contains("6.904,5 €", kwkg);
        Assert.Contains("75,0 MWh × 7,0000 ct × 0,564 + 200,0 MWh × 3,5000 ct × 0,564", kwkg);

        // Ein eigener Deckel wirkt sofort auf die Wirkung.
        Tafelzeile(cut, "epos-ueb-deckel").QuerySelector("input")!.Input("5500");
        Assert.Contains("= 1,000 → 12.250,0 €", Gruppe(cut, "epos-ueb-wirkung-kwkg").TextContent);

        Assert.Contains("§ 53a Abs. 5 EnergieStG (1135): 1.000,0 × 4,42 EUR/MWh = 4.420,0 €",
                        Gruppe(cut, "epos-ueb-wirkung-es").TextContent);
        Assert.Contains("Entlastung Netzbezug (§ 9b) 1.234,5 € · Befreiung Eigenverbrauch " +
                        "(§ 9 Abs. 1 Nr. 3) 2.345,6 € — Ausweis (nicht im Kapitalwert)",
                        Gruppe(cut, "epos-ueb-wirkung-st").TextContent);
    }

    [Fact]
    public void Ohne_Lauf_nennt_die_Wirkung_den_Grund()
    {
        var cut = Aufbauen(AnlagenMitSaetzen(), katalog: Katalog());
        Oeffnen(cut);
        Assert.Contains("Ohne gebuchten Lauf keine Mengen", Gruppe(cut, "epos-ueb-wirkung-kwkg").TextContent);
        Assert.Contains("Ohne gebuchten Lauf keine Mengen", Gruppe(cut, "epos-ueb-wirkung-es").TextContent);
        Assert.Contains("Ohne gebuchten Lauf keine Mengen", Gruppe(cut, "epos-ueb-wirkung-st").TextContent);
    }

    // =====================================================================
    //  ETAPPE E7c3 — die Energiesteuer-Vorschau des Kerns (E7c2‑Q8 b)
    // =====================================================================

    /// <summary>Die Wirkungszeilen einer Wahlgruppe der Überlagerung, in Anzeigereihenfolge.</summary>
    private static string[] Wirkungen(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, string gruppe)
        => Gruppe(cut, gruppe).QuerySelectorAll(WIRKUNG).Select(x => x.TextContent).ToArray();

    /// <summary>Wo eine Wahl ihre Wirkung trägt.</summary>
    private const string WIRKUNG = "p.epos-option-beschreibung";

    private static void Vorschau(WirtschaftlichkeitErgebnis e, string anlage, string wahl, string aufteilung,
                                 double? satz, double? menge, double betrag, double sockel = 0,
                                 string? grund = null)
        => e.EnergiesteuerVorschau.Add(new EnergiesteuerVorschauZeile
        {
            Anlage = anlage, Wahl = wahl, Aufteilung = aufteilung, SatzEur = satz,
            Einheit = satz is null ? "" : DbWerte.GESETZ_EINHEIT_EUR_MWH, Menge = menge,
            BetragEur = betrag, SockelEur = sockel, Grund = grund
        });

    /// <summary>
    /// E7c2‑Q8 b: Die Energiesteuer nennt JE WAHL Satz und Betrag aus der Vorschau des
    /// Kerns — § 53 mit der geltenden Aufteilung, § 54 mit dem Sockel, eine Wahl ohne
    /// Position mit ihrem Grund; die Aufteilung nennt ihren Betrag bei § 53, energetisch
    /// samt Stromanteil. Die Zeilen einer anderen Anlage erscheinen nicht.
    /// </summary>
    [Fact]
    public void Die_Energiesteuer_nennt_Satz_und_Betrag_je_Wahl()
    {
        const string GRUND = "Energiesteuer § 53a EnergieStG: Jahresnutzungsgrad 60,0 % unter der " +
                             "Schwelle von 70 %; Gutschrift = 0.";
        var e = new WirtschaftlichkeitErgebnis { IdProjekt = STAMM, Szenario = WirtschaftlichkeitSzenario.ERWARTET };
        Vorschau(e, GROSS, DbWerte.ENERGIESTEUER_WAHL_KEINE, "", null, null, 0);
        Vorschau(e, GROSS, DbWerte.ENERGIESTEUER_WAHL_53, DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF,
                 5.5, 4796.9924812030085, 26383.458646616546);
        Vorschau(e, GROSS, DbWerte.ENERGIESTEUER_WAHL_53, DbWerte.AUFTEILUNG_ENERGETISCH,
                 5.5, 2196.213425129088, 12079.173838209985);
        Vorschau(e, GROSS, DbWerte.ENERGIESTEUER_WAHL_53A, "", null, null, 0, grund: GRUND);
        Vorschau(e, GROSS, DbWerte.ENERGIESTEUER_WAHL_54, "", 1.38, 4796.9924812030085, 6369.849624060152, 250);
        Vorschau(e, "EC-POWER XRGI 9", DbWerte.ENERGIESTEUER_WAHL_53, DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF,
                 9.99, 1, 9.99);

        var cut = Aufbauen(AnlagenMitSaetzen(), katalog: Katalog(), ergebnisse: new[] { e });
        Oeffnen(cut);

        Assert.Equal(new[]
        {
            "→ keine Entlastung, 0 €",
            "→ 5,50 €/MWh · 26.383,5 €/a",
            "→ 0,0 €/a — " + GRUND,
            "→ 1,38 €/MWh − 250 € · 6.369,8 €/a"
        }, Wirkungen(cut, "epos-ueb-es-wahl"));

        Assert.Equal(new[]
        {
            "→ Vorgabe · 26.383,5 €/a bei § 53",
            "× 0,458 → 12.079,2 €/a bei § 53 — bewusste Untergrenze"
        }, Wirkungen(cut, "epos-ueb-es-aufteilung"));

        // § 53 folgt der Aufteilung, die in der Überlagerung gerade gilt.
        Wahl(cut, "epos-ueb-es-aufteilung", 1);
        Assert.Equal("→ 5,50 €/MWh · 12.079,2 €/a", Wirkungen(cut, "epos-ueb-es-wahl")[1]);
        Assert.DoesNotContain("9,99", Gruppe(cut, "epos-ueb-es-wahl").TextContent);
        Assert.DoesNotContain("nächste Lauf", Ueb(cut).TextContent);
    }

    /// <summary>Ein gebuchter Stand ohne Vorschau (vor E7c3) nennt die Vorschriften mit
    /// ihrem Text und sagt, dass der nächste Lauf Satz und Betrag je Wahl nennt.</summary>
    [Fact]
    public void Ohne_Vorschau_bleibt_der_Text_der_Vorschrift()
    {
        var e = new WirtschaftlichkeitErgebnis { IdProjekt = STAMM, Szenario = WirtschaftlichkeitSzenario.ERWARTET };
        var cut = Aufbauen(AnlagenMitSaetzen(), katalog: Katalog(), ergebnisse: new[] { e });
        Oeffnen(cut);

        string wahl = Gruppe(cut, "epos-ueb-es-wahl").TextContent;
        Assert.Contains("→ voller Steuersatz auf den Brennstoff der Stromerzeugung", wahl);
        Assert.Contains("→ Heizstoffe, nur produzierendes Gewerbe, abzüglich Sockelbetrag", wahl);
        Assert.Contains("Satz und Betrag je Wahl nennt der nächste Lauf", Ueb(cut).TextContent);
    }

    /// <summary>Der zweite Weg: „Wahl und Herkunft…" bei der Energiesteuer öffnet dieselbe
    /// Überlagerung für die gewählte Anlage.</summary>
    [Fact]
    public void Wahl_und_Herkunft_oeffnet_dieselbe_Ueberlagerung()
    {
        var cut = Aufbauen(Anlagen());
        IElement knopf = cut.Find("button.epos-ueb-oeffnen-steuern");
        Assert.Equal("Wahl und Herkunft…", knopf.TextContent.Trim());
        knopf.Click();

        Assert.True(cut.Instance.UeberlagerungOffen);
        Assert.Equal("Sätze und Herkunft — " + GROSS, Ueb(cut).QuerySelector("h2")!.TextContent);
        Assert.Contains("Energiesteuer (EnergieStG)", Ueb(cut).TextContent);
        Assert.Contains("Stromsteuer (StromStG) — Projekt", Ueb(cut).TextContent);
    }
}

using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Waermepumpe;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Gruppe „Kühlbetrieb" des Bausteins <c>WaermepumpeKonfiguration</c> — Stufe KU2 Welle 3
/// (Kühlkonzept 8.2, 8.6; Entscheide E15, E33, E34).
///
/// <para>Geprüft wird je Regel der Maske: ohne Kühlgaben keine Gruppe; der Sperrgrund hängt an
/// der Kennlinie, der Schalter ist WEICH gesperrt und meldet den Versuch; der Kühl-Vorlauf ist
/// eine Auswahl aus den Stützstellen (K21) mit dem kleinsten Stützwert als Vorgabe; der
/// Hilfsstromanteil steht in Prozent und wird als Anteil gespeichert (K23); der Kühlträger ist
/// eine Auswahl aus den Stromträgern des Projekts, „wie Heizbetrieb" = leer (K9); die
/// Abrechnungsart erscheint nur bei abweichendem Träger (E34); die Prüfregel an EINER Stelle.</para>
///
/// <para><b>Phantasiewerte</b> — keine Produktdaten.</para>
/// </summary>
public class WaermepumpeKuehlbetriebTests : EposBunitContext
{
    public WaermepumpeKuehlbetriebTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Die Gruppe traegt die Hilfeknoepfe der Kuehlung (Anwendungs- und
        // Grundlagenseite) - der InfoKnopf braucht einen Hilfedienst; beide Wirte
        // des Bausteins haben ihn ohnehin.
        Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());
    }

    private const int PROJEKTTRAEGER = 60, KUEHLTRAEGER = 58;
    private const string SPERRGRUND = "Zu diesem Gerät liegen keine Kühlkenndaten vor — der Kühlbetrieb bleibt gesperrt.";

    private static WaermepumpeAnlageDaten Daten(bool kuehlbetrieb = false, double? kuehlleistung = null) => new()
    {
        Bezeichner = "WP Alpha",
        IdWp = 77,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        CarrierId = PROJEKTTRAEGER,
        Kuehlleistung = kuehlleistung,
        Kuehlbetrieb = kuehlbetrieb
    };

    private static WaermepumpeKuehlGaben Gaben(string? sperrgrund = null, bool mitKennlinie = true,
                                               string? freiSperrgrund = null) => new()
    {
        FreiSperrgrund = _ => freiSperrgrund,
        Vorlaeufe = _ => mitKennlinie
            ? new[] { new KuehlVorlaufEintrag(7), new KuehlVorlaufEintrag(18), new KuehlVorlaufEintrag(45, "Kennlinie in Heizlage — wird nicht gerechnet") }
            : Array.Empty<KuehlVorlaufEintrag>(),
        Sperrgrund = _ => sperrgrund,
        Stromtraeger = new[] { (KUEHLTRAEGER, "Strom Kühlung"), (PROJEKTTRAEGER, "Strom Projekt") },
        ProjektStromtraeger = PROJEKTTRAEGER
    };

    private IRenderedComponent<WaermepumpeKonfiguration> Aufbauen(WaermepumpeAnlageDaten daten,
                                                                   WaermepumpeKuehlGaben? gaben)
        => Render<WaermepumpeKonfiguration>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Kuehlung, gaben));

    private static IElement Feld(IRenderedComponent<WaermepumpeKonfiguration> cut, string bezeichnung, string tag)
        => cut.FindAll("label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung)
              .QuerySelector(tag)!;

    private static IElement Kuehlschalter(IRenderedComponent<WaermepumpeKonfiguration> cut)
        => cut.FindAll("label.epos-schalter")
              .First(l => l.TextContent.Trim() == "Maschine auch zum Kühlen benutzen")
              .QuerySelector("input")!;

    // =================================================================================

    /// <summary>Ohne Kühlgaben (Prüfstand, Plattform ohne Weg) steht keine Gruppe „Kühlbetrieb" — der Block zeichnet wie vorher.</summary>
    [Fact]
    public void Ohne_Kuehlgaben_steht_keine_Gruppe_Kuehlbetrieb()
    {
        var cut = Aufbauen(Daten(), null);
        Assert.DoesNotContain("Kühlbetrieb",
            cut.FindAll(".epos-formulargruppe-titel").Select(e => e.TextContent.Trim()));
        Assert.DoesNotContain(cut.FindAll("label.epos-schalter"), l => l.TextContent.Contains("Kühlen"));
    }

    /// <summary>
    /// Ohne Kühlkennlinie ist der Schalter WEICH gesperrt (aria-disabled + title, nicht disabled);
    /// die Zeile darunter nennt den Grund, der Klick schaltet nicht und meldet ihn; mit
    /// Nennkühlleistung warnt der Baustein, dass die Maschine nur Wärme rechnet (8.2).
    /// </summary>
    [Fact]
    public void Ohne_Kennlinie_ist_der_Kuehlbetrieb_weich_gesperrt_und_nennt_den_Grund()
    {
        WaermepumpeAnlageDaten d = Daten(kuehlleistung: 8.0);
        var cut = Aufbauen(d, Gaben(SPERRGRUND, mitKennlinie: false));

        IElement kasten = Kuehlschalter(cut);
        Assert.Equal("true", kasten.GetAttribute("aria-disabled"));
        Assert.False(kasten.HasAttribute("disabled"));
        Assert.Equal(SPERRGRUND, kasten.ParentElement!.GetAttribute("title"));
        Assert.Contains(SPERRGRUND, cut.FindAll(".epos-herleitung").Select(e => e.TextContent.Trim()));
        Assert.Contains(cut.FindAll(".epos-warnbanner"), b => b.TextContent.Contains("Nennkühlleistung ohne Kühlkennlinie — diese Maschine rechnet nur Wärme."));

        kasten.Click();
        Assert.False(d.Kuehlbetrieb);
        Assert.Contains(cut.FindAll(".epos-warnbanner"), b => b.TextContent.Contains(SPERRGRUND));

        // Ohne Haken keine Felder darunter.
        Assert.DoesNotContain(cut.FindAll(".epos-feld-text"), e => e.TextContent.Trim() == "Kühl-Vorlauf");
    }

    /// <summary>
    /// Mit Kennlinie schaltet der Haken den Kühlbetrieb; darunter die Vorlaufwahl aus den
    /// Stützstellen (K21) mit dem kleinsten Stützwert als Vorgabe und dem gesperrten Vorlauf in
    /// Heizlage, die feste Umschaltregel, der Hilfsstromanteil in Prozent (gespeichert als Anteil)
    /// und die Wahl des Kühlträgers mit „wie Heizbetrieb" als leerem Eintrag.
    /// </summary>
    [Fact]
    public void Der_Kuehlbetrieb_zeigt_Vorlauf_Hilfsstrom_und_Kuehltraeger()
    {
        WaermepumpeAnlageDaten d = Daten();
        var cut = Aufbauen(d, Gaben());

        Kuehlschalter(cut).Change(true);
        Assert.True(d.Kuehlbetrieb);

        IElement vorlauf = Feld(cut, "Kühl-Vorlauf", "select");
        var optionen = vorlauf.QuerySelectorAll("option").ToList();
        Assert.Equal("Vorgabe: kleinster Stützwert (7 °C)", optionen[0].TextContent);
        Assert.Equal(new[] { "7 °C", "18 °C", "45 °C" }, optionen.Skip(1).Select(o => o.TextContent));
        Assert.True(optionen[3].HasAttribute("disabled"));
        Assert.Equal("Kennlinie in Heizlage — wird nicht gerechnet", optionen[3].GetAttribute("title"));
        vorlauf.Change("18");
        Assert.Equal(18, d.KuehlVorlauf);

        Assert.Contains(cut.FindAll(".epos-herleitung"), e => e.TextContent.StartsWith("Umschaltung je Tag"));

        IElement hilfs = Feld(cut, "Hilfsstromanteil", "input");
        Assert.Equal("Vorgabe: kein Zuschlag", hilfs.GetAttribute("placeholder"));
        hilfs.Input("5");
        Assert.Equal(0.05, d.KuehlHilfsstromanteil!.Value, 12);

        IElement traeger = Feld(cut, "Stromträger des Kältestroms", "select");
        Assert.Equal("wie Heizbetrieb", traeger.QuerySelectorAll("option")[0].TextContent);
        Assert.Null(d.KuehlCarrierId);
    }

    /// <summary>
    /// E34: Die Abrechnungsart erscheint erst mit einem ABWEICHENDEN Kühlträger — anteilig (Vorgabe,
    /// NULL) oder eigener Zähler (true); der Träger des Projekts oder „wie Heizbetrieb" nimmt sie
    /// wieder weg und setzt sie zurück.
    /// </summary>
    [Fact]
    public void Die_Abrechnungsart_steht_nur_bei_abweichendem_Kuehltraeger()
    {
        WaermepumpeAnlageDaten d = Daten(kuehlbetrieb: true);
        var cut = Aufbauen(d, Gaben());
        Assert.DoesNotContain(cut.FindAll(".epos-feld-text"), e => e.TextContent.Trim() == "Abrechnung des Kältestroms");

        Feld(cut, "Stromträger des Kältestroms", "select").Change(KUEHLTRAEGER.ToString());
        Assert.Equal(KUEHLTRAEGER, d.KuehlCarrierId);
        Assert.Contains(cut.FindAll(".epos-herleitung"), e => e.TextContent.StartsWith("Ein Netzanschluss"));

        IElement zaehler = cut.FindAll("label")
            .First(l => l.TextContent.Trim() == "eigener Zähler").QuerySelector("input")!;
        zaehler.Change(true);
        Assert.Equal(true, d.KuehlEigenerZaehler);
        Assert.Contains(cut.FindAll(".epos-herleitung"), e => e.TextContent.StartsWith("Der ganze Kältestrom"));

        // Der Träger des Projekts: keine Wahl, die Abrechnungsart fällt zurück.
        Feld(cut, "Stromträger des Kältestroms", "select").Change(PROJEKTTRAEGER.ToString());
        Assert.Null(d.KuehlEigenerZaehler);
        Assert.DoesNotContain(cut.FindAll("label"), l => l.TextContent.Trim() == "eigener Zähler");
        Assert.Contains(cut.FindAll(".epos-herleitung"), e => e.TextContent.StartsWith("Der Kältestrom trägt Tarif"));

        // „wie Heizbetrieb" = leer.
        Feld(cut, "Stromträger des Kältestroms", "select").Change("");
        Assert.Null(d.KuehlCarrierId);
    }

    /// <summary>Die Prüfregel an EINER Stelle (8.6): Hilfsstrom 0 ≤ x &lt; 1, ein gesperrter Kühlbetrieb meldet seinen Grund.</summary>
    [Fact]
    public void Die_Pruefregel_der_Kuehlfelder()
    {
        var texte = new WaermepumpeKonfigurationTexte();
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(Daten(true), null, texte));
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(Daten(true), Gaben(), texte));

        WaermepumpeAnlageDaten zuViel = Daten(true);
        zuViel.KuehlHilfsstromanteil = 1.0;
        Assert.Equal(texte.MeldungHilfsstromBereich, WaermepumpeKonfiguration.KuehlFehler(zuViel, Gaben(), texte));
        zuViel.KuehlHilfsstromanteil = -0.1;
        Assert.Equal(texte.MeldungHilfsstromBereich, WaermepumpeKonfiguration.KuehlFehler(zuViel, Gaben(), texte));

        Assert.Equal(SPERRGRUND, WaermepumpeKonfiguration.KuehlFehler(Daten(true), Gaben(SPERRGRUND), texte));
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(Daten(false), Gaben(SPERRGRUND), texte));
    }

    /// <summary>Ein gespeicherter Kühlträger, der nicht (mehr) zum Projekt gehört, steht als Eintrag — die Liste schuldet den gespeicherten Wert.</summary>
    [Fact]
    public void Ein_gespeicherter_Kuehltraeger_ausserhalb_der_Liste_steht_als_Eintrag()
    {
        WaermepumpeAnlageDaten d = Daten(true);
        d.KuehlCarrierId = 99;
        var cut = Aufbauen(d, Gaben());
        var optionen = Feld(cut, "Stromträger des Kältestroms", "select").QuerySelectorAll("option");
        Assert.Contains(optionen, o => o.GetAttribute("value") == "99" && o.HasAttribute("selected"));
    }

    // ==== Freie Kühlung über die Wärmequelle (KU3-6, F2, F6) =========================

    private const string FREI_GRUND = "Freie Kühlung nur an einer Sole-Wasser- oder Wasser-Wasser-Wärmepumpe — diese Maschine nutzt die Außenluft.";

    private static IElement? Freischalter(IRenderedComponent<WaermepumpeKonfiguration> cut)
        => cut.FindAll("label.epos-schalter")
              .FirstOrDefault(l => l.TextContent.Trim() == "Freie Kühlung über die Wärmequelle")
              ?.QuerySelector("input");

    private static bool HatFeld(IRenderedComponent<WaermepumpeKonfiguration> cut, string bezeichnung)
        => cut.FindAll(".epos-feld-text").Any(e => e.TextContent.Trim() == bezeichnung);

    /// <summary>Der Schalter steht nur mit Kühlbetrieb, Grädigkeit und Leistungsgrenze nur mit dem Schalter.</summary>
    [Fact]
    public void Freie_Kuehlung_steht_nur_mit_Kuehlbetrieb_und_ihre_Felder_nur_mit_dem_Schalter()
    {
        WaermepumpeAnlageDaten d = Daten();
        var cut = Aufbauen(d, Gaben());
        Assert.Null(Freischalter(cut));

        Kuehlschalter(cut).Change(true);
        IElement frei = Freischalter(cut)!;
        Assert.False(frei.HasAttribute("aria-disabled"));
        Assert.False(HatFeld(cut, "Grädigkeit des Wärmetauschers"));
        Assert.False(HatFeld(cut, "Leistungsgrenze"));

        frei.Change(true);
        Assert.True(d.KuehlFrei);
        Assert.True(HatFeld(cut, "Grädigkeit des Wärmetauschers"));
        Assert.True(HatFeld(cut, "Leistungsgrenze"));
        Assert.Contains(cut.FindAll(".epos-herleitung"), e => e.TextContent.StartsWith("Leer = 3,0 K"));

        IElement graed = Feld(cut, "Grädigkeit des Wärmetauschers", "input");
        Assert.Equal("Vorgabe: 3,0 K", graed.GetAttribute("placeholder"));
        graed.Input("5");
        Assert.Equal(5.0, d.KuehlFreiGraedigkeitK);

        IElement leistung = Feld(cut, "Leistungsgrenze", "input");
        Assert.Equal("Vorgabe: Kälteleistung der Kennlinie", leistung.GetAttribute("placeholder"));
        leistung.Input("12");
        Assert.Equal(12.0, d.KuehlFreiLeistungKw);

        // Ausschalten nimmt die Felder weg; die Werte bleiben im Feldsatz.
        Freischalter(cut)!.Change(false);
        Assert.False(d.KuehlFrei);
        Assert.False(HatFeld(cut, "Leistungsgrenze"));
    }

    /// <summary>
    /// F2: Ohne Sole-/Wasser-Wasser-Bauart oder gepflegte Quelle ist der Schalter WEICH gesperrt
    /// (aria-disabled + title, nicht disabled), die Herleitung nennt den Grund, der Klick schaltet
    /// nicht und meldet ihn. Ein schon gesetzter Schalter lässt sich ausschalten.
    /// </summary>
    [Fact]
    public void Freie_Kuehlung_mit_Sperrgrund_ist_weich_gesperrt_und_nennt_die_Herleitung()
    {
        WaermepumpeAnlageDaten d = Daten(kuehlbetrieb: true);
        var cut = Aufbauen(d, Gaben(freiSperrgrund: FREI_GRUND));

        IElement frei = Freischalter(cut)!;
        Assert.Equal("true", frei.GetAttribute("aria-disabled"));
        Assert.False(frei.HasAttribute("disabled"));
        Assert.Equal(FREI_GRUND, frei.ParentElement!.GetAttribute("title"));
        Assert.Contains(FREI_GRUND, cut.FindAll(".epos-herleitung").Select(e => e.TextContent.Trim()));

        frei.Click();
        Assert.False(d.KuehlFrei);
        Assert.Contains(cut.FindAll(".epos-warnbanner"), b => b.TextContent.Contains(FREI_GRUND));
        Assert.False(HatFeld(cut, "Grädigkeit des Wärmetauschers"));

        // Ein gesetzter Schalter (Gerätewechsel danach) ist nicht gesperrt — Ausschalten geht immer.
        WaermepumpeAnlageDaten an = Daten(kuehlbetrieb: true);
        an.KuehlFrei = true;
        var cut2 = Aufbauen(an, Gaben(freiSperrgrund: FREI_GRUND));
        IElement frei2 = Freischalter(cut2)!;
        Assert.False(frei2.HasAttribute("aria-disabled"));
        Assert.Contains(FREI_GRUND, cut2.FindAll(".epos-herleitung").Select(e => e.TextContent.Trim()));
        frei2.Change(false);
        Assert.False(an.KuehlFrei);
    }

    /// <summary>Die Prüfregel der freien Kühlung: Grädigkeit 0 bis 20 K, Leistung &gt; 0 kW; leer gilt (Vorgaben); ohne Kühlbetrieb oder Schalter wird nicht geprüft.</summary>
    [Fact]
    public void Die_Pruefregel_der_freien_Kuehlung()
    {
        var texte = new WaermepumpeKonfigurationTexte();
        WaermepumpeAnlageDaten d = Daten(true);
        d.KuehlFrei = true;
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));

        foreach (double g in new[] { -0.1, 20.1, double.NaN })
        {
            d.KuehlFreiGraedigkeitK = g;
            Assert.Equal(texte.MeldungKuehlFreiGraedigkeit, WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));
        }
        foreach (double g in new[] { 0.0, 3.0, 20.0 })
        {
            d.KuehlFreiGraedigkeitK = g;
            Assert.Null(WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));
        }

        d.KuehlFreiGraedigkeitK = null;
        foreach (double kw in new[] { 0.0, -5.0 })
        {
            d.KuehlFreiLeistungKw = kw;
            Assert.Equal(texte.MeldungKuehlFreiLeistung, WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));
        }
        d.KuehlFreiLeistungKw = 0.5;
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));

        // Ohne Schalter oder ohne Kühlbetrieb stehen die Felder nicht — sie sperren das OK nicht.
        d.KuehlFreiLeistungKw = -1;
        d.KuehlFrei = false;
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));
        d.KuehlFrei = true;
        d.Kuehlbetrieb = false;
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(d, Gaben(), texte));

        // Ein Sperrgrund der freien Kühlung ist kein Fehler — der Lauf lehnt sie benannt ab (F2).
        WaermepumpeAnlageDaten gesperrt = Daten(true);
        gesperrt.KuehlFrei = true;
        Assert.Null(WaermepumpeKonfiguration.KuehlFehler(gesperrt, Gaben(freiSperrgrund: FREI_GRUND), texte));
    }

    /// <summary>Die Regel F2 aus Bauart und Quelle: wirksam nur Sole-/Wasser-Wasser mit Erdreich, Konstant, Profil oder CSV; Unbekanntes wird nicht geprüft.</summary>
    [Fact]
    public void Der_Sperrgrund_der_freien_Kuehlung_folgt_Bauart_und_Quelle()
    {
        var texte = new WaermepumpeKonfigurationTexte();
        foreach (string q in new[] { "Erdreich", "Konstant", "Profil", "CSV" })
        {
            Assert.Null(WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus("Sole-Wasser", q, texte));
            Assert.Null(WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus("Wasser-Wasser", q, texte));
        }
        foreach (string q in new[] { "Aussenluft", "", "Pufferspeicher" })
            Assert.Equal(texte.SperrgrundFreiQuelle, WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus("Sole-Wasser", q, texte));
        Assert.Equal(texte.SperrgrundFreiBauart, WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus("Luft-Wasser", "Erdreich", texte));
        Assert.Equal(texte.SperrgrundFreiBauart, WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus("", "Erdreich", texte));
        Assert.Null(WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus(null, null, texte));
        Assert.Null(WaermepumpeKonfiguration.FreieKuehlungSperrgrundAus("Sole-Wasser", null, texte));
    }

    /// <summary>Die Wege des Hilfe-Assistenten fahren dieselben Regeln: gesperrt benannt abgelehnt, Bereiche geprüft, leer gilt.</summary>
    [Fact]
    public void Die_KI_Wege_der_freien_Kuehlung_fahren_dieselben_Regeln()
    {
        WaermepumpeAnlageDaten d = Daten(true);
        var gesperrt = Assert.Throws<InvalidOperationException>(
            () => WaermepumpeKuehlKiWege.KuehlFreiSetzen(d, Gaben(freiSperrgrund: FREI_GRUND), true));
        Assert.Equal(FREI_GRUND, gesperrt.Message);
        Assert.False(d.KuehlFrei);

        WaermepumpeKuehlKiWege.KuehlFreiSetzen(d, Gaben(), true);
        Assert.True(d.KuehlFrei);
        Assert.Throws<InvalidOperationException>(() => WaermepumpeKuehlKiWege.KuehlFreiGraedigkeitSetzen(d, Gaben(), 25));
        Assert.Throws<InvalidOperationException>(() => WaermepumpeKuehlKiWege.KuehlFreiLeistungSetzen(d, Gaben(), 0));
        WaermepumpeKuehlKiWege.KuehlFreiGraedigkeitSetzen(d, Gaben(), 4);
        WaermepumpeKuehlKiWege.KuehlFreiLeistungSetzen(d, Gaben(), null);
        Assert.Equal(4.0, d.KuehlFreiGraedigkeitK);
        Assert.Null(d.KuehlFreiLeistungKw);

        // Ohne Kühlgaben kein Weg (keine Gruppe in der Maske).
        Assert.Throws<InvalidOperationException>(() => WaermepumpeKuehlKiWege.KuehlFreiSetzen(d, null, false));
    }

    /// <summary>Kopie und Abbrechen-Weg führen die drei Felder der freien Kühlung mit.</summary>
    [Fact]
    public void Kopie_und_Kuehlfelder_fuehren_die_freie_Kuehlung_mit()
    {
        WaermepumpeAnlageDaten d = Daten(true);
        d.KuehlFrei = true;
        d.KuehlFreiGraedigkeitK = 2.5;
        d.KuehlFreiLeistungKw = 9.0;
        WaermepumpeAnlageDaten k = d.Kopie();
        Assert.True(k.KuehlFrei);
        Assert.Equal(2.5, k.KuehlFreiGraedigkeitK);
        Assert.Equal(9.0, k.KuehlFreiLeistungKw);

        var ziel = Daten();
        ziel.KuehlfelderAus(d);
        Assert.True(ziel.KuehlFrei);
        Assert.Equal(2.5, ziel.KuehlFreiGraedigkeitK);
        Assert.Equal(9.0, ziel.KuehlFreiLeistungKw);
    }
}

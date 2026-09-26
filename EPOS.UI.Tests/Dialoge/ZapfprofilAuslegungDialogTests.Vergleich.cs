using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Auslegung mit den Feldern der Stufe Z4, Gruppe 2b (Umsetzungskonzept Zapfprofilgenerator
/// 4.7, N10 (i)/(j), N11 (d)): die Eingaben des Verfahrensvergleichs (Ladeleistung auto/manuell mit
/// Vorschlag, Ladezeitfenster, Personen auto/manuell, nutzbarer Anteil, Zuschlag, Bezug des
/// Füllstands), die Erzeugerart als gespeicherte Wahl mit dem Vorschlag des Projekts und der Bezug
/// des konstruierten Tags (Bezugsart und Bezugsmenge) — an den Feldern und beim Assistenten.
///
/// <para>Die Auslegung kommt aus einem Prüfdelegaten — der Dialog rechnet nicht. Kultur de-DE,
/// alle Zahlen erfunden.</para>
/// </summary>
public partial class ZapfprofilAuslegungDialogTests
{
    /// <summary>Eine Speichergruppe mit dem Vorschlag der Ladeleistung und den angesetzten Werten des Vergleichs.</summary>
    private static ZapfprofilAuslegungsgruppeDaten MitLadevorschlag()
    {
        ZapfprofilAuslegungsgruppeDaten g = Speichergruppe();
        g.Vergleich!.LadeleistungKw = 9.5;
        g.Vergleich.LadeRechenweg = "Ladeweg der Probe";
        g.Vergleich.Personen = 40;
        g.Vergleich.PersonenVorschlag = 40;
        g.Vergleich.FuellstandBezug = "Nenninhalt des Punkts";
        g.Vergleich.FuellstandBezugL = 400;
        g.Vergleich.Ladeleistung = new ZapfprofilSchaetzhilfeDaten { Auto = true, Vorschlag = 9.5, Angesetzt = 9.5, Einheit = "kW" };
        return g;
    }

    /// <summary>Der Start mit den Wertemengen des Schemas für Bezugsart und Füllstandsbezug.</summary>
    private static ZapfprofilAuslegungStartDaten MitWertemengen(ZapfprofilAuslegungDaten? ergebnis = null)
    {
        ZapfprofilAuslegungStartDaten s = Start(ergebnis ?? Ergebnis(MitLadevorschlag()));
        s.Bezugsarten.Add(new ZapfprofilKatalogeintragDaten { Id = 1, Name = "Personen" });
        s.Bezugsarten.Add(new ZapfprofilKatalogeintragDaten { Id = 2, Name = "Wohneinheiten" });
        s.Fuellstandbezuege.Add(new ZapfprofilKatalogeintragDaten { Id = 1, Name = "Nenninhalt des Punkts" });
        s.Fuellstandbezuege.Add(new ZapfprofilKatalogeintragDaten { Id = 2, Name = "Punkt" });
        s.Fuellstandbezuege.Add(new ZapfprofilKatalogeintragDaten { Id = 3, Name = "Nenninhalt des Bands" });
        s.Fuellstandbezuege.Add(new ZapfprofilKatalogeintragDaten { Id = 4, Name = "Obergrenze des Bands" });
        return s;
    }

    [Fact]
    public void Die_Eingaben_des_Verfahrensvergleichs_stehen_als_Felder_und_gehen_mit_OK_zurueck()
    {
        var gerechnet = new List<ZapfprofilAuslegungEingabeDaten>();
        ZapfprofilAuslegungEingabeDaten? ergebnis = null;
        ZapfprofilAuslegungStartDaten start = MitWertemengen();
        var cut = Aufbauen(start, rechnen: e => { gerechnet.Add(e); return start.Ergebnis!; }, geschlossen: e => ergebnis = e);

        Assert.Contains(cut.FindAll(".epos-gruppenkopf-titel"), t => t.TextContent.Trim() == "Eingaben des Verfahrensvergleichs");
        foreach (string f in new[] { "Ladeleistung manuell", "Ladezeitfenster", "Beginn des Ladezeitfensters", "Nutzbarer Anteil",
                                     "Zuschlag", "Personen manuell" })
            Assert.NotNull(Feld(cut, f));
        Assert.Contains("Angesetzt: 9,5 kW (auto) · Ladeweg der Probe", cut.Markup);
        Assert.Contains("auto = aus dem Mengengerüst der Zonen: 40,0; angesetzt: 40,0", cut.Markup);
        Assert.Contains("Auf dieses Volumen bezieht sich der Füllstand der Kachel; angesetzt: Nenninhalt des Punkts 400 l", cut.Markup);

        // Der Vorschlag der Ladeleistung wird der manuelle Wert, der Umschalter „manuell".
        IElement vorschlag = cut.FindAll(".epos-vorschlagszeile").First(z => z.TextContent.Contains("Vorschlag: 9,5 kW"));
        vorschlag.QuerySelector("button")!.Click();
        Assert.False(cut.Instance.Eingabe.LadeAuto);
        Assert.Equal(9.5, cut.Instance.Eingabe.LadeManuellKw);
        Assert.False(gerechnet.Last().LadeAuto);

        cut.Find("fieldset[aria-label='Personen']").QuerySelectorAll("label.epos-option")
           .First(l => l.TextContent.Trim() == "manuell").QuerySelector("input")!.Change("1");
        Feld(cut, "Personen manuell").Input("35");
        Feld(cut, "Nutzbarer Anteil").Input("0,8");
        Feld(cut, "Zuschlag").Input("0,15");
        Feld(cut, "Ladezeitfenster").Input("6");
        IElement bezug = Feld(cut, "Bezug des Füllstands", "select");
        Assert.Equal(5, bezug.QuerySelectorAll("option").Length);
        Assert.Equal("Vorgabe: Nenninhalt des Punkts, sonst der Punkt", bezug.QuerySelectorAll("option")[0].TextContent.Trim());
        bezug.Change("4");

        Knopf(cut, "OK").Click();
        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.LadeAuto);
        Assert.False(ergebnis.PersonenAuto);
        Assert.Equal(35.0, ergebnis.PersonenManuell);
        Assert.Equal(0.8, ergebnis.Nutzanteil);
        Assert.Equal(0.15, ergebnis.Zuschlag);
        Assert.Equal(6.0, ergebnis.LadefensterH);
        Assert.Equal(ZapfprofilFuellstandbezug.BandMax, ergebnis.FuellstandBezug);
    }

    [Fact]
    public void Ohne_Speichergruppe_nennt_die_Ladeleistung_den_Grund_statt_eines_Vorschlags()
    {
        var cut = Aufbauen(MitWertemengen(Ergebnis(Speichergruppe())), rechnen: _ => Ergebnis(Speichergruppe()));
        Assert.Contains("Einen Vorschlag gibt es erst mit einer gerechneten Speichergruppe.", cut.Markup);
        Assert.DoesNotContain(cut.FindAll(".epos-vorschlagszeile"), z => z.TextContent.Contains(" kW"));
    }

    [Fact]
    public void Die_Erzeugerart_wird_gespeichert_und_der_Vorschlag_des_Projekts_ist_eine_Wahl()
    {
        ZapfprofilAuslegungDaten ergebnis = Ergebnis(MitLadevorschlag());
        ergebnis.ErzeugerartVorschlag = ZapfprofilErzeugerart.Kessel;
        ergebnis.ErzeugerartHerkunft = "Vorschlag aus dem Anlagenbestand: Kessel";
        var cut = Aufbauen(MitWertemengen(ergebnis), rechnen: _ => ergebnis);

        Assert.Contains("wird mit dem Projekt gespeichert; Vorschlag aus dem Anlagenbestand: Kessel", cut.Markup);
        Assert.Contains("wird mit dem Projekt gespeichert; kein Vorschlag aus dem Projekt", cut.Markup);
        IElement zeile = cut.FindAll(".epos-vorschlagszeile").First(z => z.TextContent.Contains("Vorschlag aus dem Projekt: Kessel"));
        zeile.QuerySelector("button")!.Click();

        Assert.Equal(ZapfprofilErzeugerart.Kessel, cut.Instance.Eingabe.Erzeugerart);
        // Gewählt ist gewählt: der Vorschlag verschwindet.
        Assert.DoesNotContain(cut.FindAll(".epos-vorschlagszeile"), z => z.TextContent.Contains("Vorschlag aus dem Projekt"));
    }

    [Fact]
    public void Der_Konstruktor_waehlt_Bezugsart_und_Bezugsmenge_und_beginnt_erneut_mit_ihnen()
    {
        (double? Menge, int? Art)? bezug = null;
        var cut = Aufbauen(MitWertemengen(), rechnen: _ => Ergebnis(MitLadevorschlag()),
                           konstruieren: (zeilen, name, menge, art) => { bezug = (menge, art); return Bauen(zeilen, name, menge, art); });
        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();

        Assert.True(Feld(k, "Bezugsmenge").HasAttribute("disabled"));     // ohne Bezugsart nicht bedienbar
        IElement art = Feld(k, "Bezugsart", "select");
        Assert.Equal(3, art.QuerySelectorAll("option").Length);
        Assert.Equal("ohne Bezug — der Tag gilt, wie er ist", art.QuerySelectorAll("option")[0].TextContent.Trim());
        art.Change("2");
        Assert.False(Feld(k, "Bezugsmenge").HasAttribute("disabled"));
        Feld(k, "Bezugsmenge").Input("4");
        Knopf(k, "OK").Click();

        Assert.Equal((4.0, (int?)2), bezug);
        Assert.Equal(4.0, cut.Instance.Eingabe.Entwurf!.Bezugsmenge);
        Assert.Equal(2, cut.Instance.Eingabe.Entwurf.Bezugsart);

        // Erneut geöffnet beginnt er mit dem Bezug des Entwurfs; „ohne Bezug" nimmt die Menge weg.
        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> erneut = cut.FindComponent<BedarfstagKonstruktor>();
        Assert.Equal(2, erneut.Instance.GewaehlteBezugsart);
        Assert.Equal(4.0, erneut.Instance.GewaehlteBezugsmenge);
        Feld(erneut, "Bezugsart", "select").Change("0");
        Assert.Null(erneut.Instance.GewaehlteBezugsart);
        Knopf(erneut, "OK").Click();
        Assert.Equal((null, null), bezug);
    }

    /// <summary>
    /// <b>Der ZEUGE der neuen Felder an der Maskenbrücke.</b> Die Eingaben des Vergleichs setzt der
    /// Assistent auf den Wegen der Felder, außerhalb der Grenzen lehnt er benannt ab; am Konstruktor
    /// gibt es die Bezugsmenge nur mit einer Bezugsart.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_die_Eingaben_des_Vergleichs_und_den_Bezug_des_Konstruktors()
    {
        KiMaskenbruecke.Leeren();
        var cut = Aufbauen(MitWertemengen(), rechnen: _ => Ergebnis(MitLadevorschlag()), konstruieren: Bauen);

        Setze("lade_modus", "manuell");
        Assert.False(cut.Instance.Eingabe.LadeAuto);
        Setze("lade_manuell", "11");
        Assert.Equal(11.0, cut.Instance.Eingabe.LadeManuellKw);
        Assert.Throws<InvalidOperationException>(() => Zugang("ladefenster").Setzen(30.0));
        Assert.Throws<InvalidOperationException>(() => Zugang("nutzanteil").Setzen(1.5));
        Setze("nutzanteil", "0,7");
        Assert.Equal(0.7, cut.Instance.Eingabe.Nutzanteil);
        Setze("fuellstand_bezug", "Nenninhalt des Bands");
        Assert.Equal(ZapfprofilFuellstandbezug.NenninhaltBand, cut.Instance.Eingabe.FuellstandBezug);
        Setze("personen_modus", "manuell");
        Setze("personen_manuell", "25");
        Assert.False(cut.Instance.Eingabe.PersonenAuto);
        Assert.Equal(25.0, cut.Instance.Eingabe.PersonenManuell);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();
        Assert.Throws<InvalidOperationException>(() => KonstruktorZugang("bezugsmenge").Setzen(3.0));   // erst mit Bezugsart
        KonstruktorSetze("bezugsart", "Personen");
        KonstruktorSetze("bezugsmenge", "3");
        Assert.Equal(1, k.Instance.GewaehlteBezugsart);
        Assert.Equal(3.0, k.Instance.GewaehlteBezugsmenge);
        Assert.Throws<InvalidOperationException>(() => KonstruktorZugang("bezugsmenge").Setzen(-1.0));
    }

    /// <summary>
    /// <b>Ein gespeicherter Konstruktortag öffnet mit seinem Bezug</b> (Folge (a) aus N21): Ohne
    /// Entwurf kommen Bezugsart und Bezugsmenge seiner Katalogzeile über die Eingaben
    /// (<c>KonstruktorBezugsart</c>, <c>KonstruktorBezugsmenge</c>, von der Hülle gefüllt) — der
    /// Konstruktor zeigt sie an Feld und Auswahl, und ein OK baut den Tag mit demselben Bezug.
    /// </summary>
    [Fact]
    public void Ein_gespeicherter_Konstruktortag_oeffnet_mit_seinem_Bezug()
    {
        (double? Menge, int? Art)? bezug = null;
        ZapfprofilAuslegungStartDaten s = MitWertemengen();
        s.Eingabe = new ZapfprofilAuslegungEingabeDaten
        {
            SpeicherC = 60,
            ErzeugerKw = 25,
            Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
            IdBedarfstag = 5,
            KonstruktorBezugsart = 2,
            KonstruktorBezugsmenge = 4,
            Konstruktorzeilen = { new ZapfprofilKonstruktorZeileDaten { BeginnH = 6, EndeH = 7, Regel = "Dusche", Anzahl = 2 } }
        };
        var cut = Aufbauen(s, rechnen: _ => Ergebnis(MitLadevorschlag()),
                           konstruieren: (zeilen, name, menge, art) => { bezug = (menge, art); return Bauen(zeilen, name, menge, art); });
        Assert.Null(cut.Instance.Eingabe.Entwurf);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();
        Assert.Equal(2, k.Instance.GewaehlteBezugsart);
        Assert.Equal(4.0, k.Instance.GewaehlteBezugsmenge);
        Assert.Equal("2", Feld(k, "Bezugsart", "select").GetAttribute("value"));
        Assert.False(Feld(k, "Bezugsmenge").HasAttribute("disabled"));
        Assert.Empty(k.FindAll(".epos-zapfausl-konstruktorbezug"));

        Feld(k, "Name des Bedarfstags").Input("Tag Neu");
        Knopf(k, "OK").Click();
        Assert.Equal((4.0, (int?)2), bezug);
        Assert.Equal(2, cut.Instance.Eingabe.Entwurf!.Bezugsart);
        Assert.Null(cut.Instance.Eingabe.KonstruktorBezugsart);            // überholt: der Entwurf trägt ihn
    }

    /// <summary>
    /// <b>Ein alter Datensatz ohne Bezug:</b> Der Konstruktor beginnt ohne Bezug, aber nicht still —
    /// der Hinweis der Hülle steht als Statuszeile; nach OK (der Entwurf trägt seinen Bezug) ist er weg.
    /// </summary>
    [Fact]
    public void Ein_gespeicherter_Tag_ohne_Bezug_zeigt_den_Hinweis()
    {
        ZapfprofilAuslegungStartDaten s = MitWertemengen();
        s.Eingabe = new ZapfprofilAuslegungEingabeDaten
        {
            SpeicherC = 60,
            ErzeugerKw = 25,
            Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
            IdBedarfstag = 5,
            KonstruktorBezugHinweis = "Hinweis der Probe: ohne Bezug"
        };
        var cut = Aufbauen(s, rechnen: _ => Ergebnis(MitLadevorschlag()), konstruieren: Bauen);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();
        Assert.Null(k.Instance.GewaehlteBezugsart);
        IElement hinweis = Assert.Single(k.FindAll(".epos-zapfausl-konstruktorbezug"));
        Assert.Equal("Hinweis der Probe: ohne Bezug", hinweis.TextContent.Trim());
        Assert.Equal("status", hinweis.GetAttribute("role"));

        Feld(k, "Name des Bedarfstags").Input("Tag Neu");
        Knopf(k, "OK").Click();
        Knopf(cut, "Bedarfstag konstruieren…").Click();
        Assert.Empty(cut.FindComponent<BedarfstagKonstruktor>().FindAll(".epos-zapfausl-konstruktorbezug"));
    }
}

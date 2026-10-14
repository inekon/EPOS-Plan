using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Editor „Zeitverlauf je Größe"</b> im Blatt „Nutzungsprofile" (Stufe NP4c; Konzept Nutzungsprofile 6.1, 4.3,
/// NP-F7, NP-F9): der Umschalter je Größe samt Rückfrage und Vorschlag, der Zeilenbild-Editor (Zeilenarten, Grenzen,
/// Nachtfenster, ΔT nur an Lüftung/Nacht, Anteile in Prozent), der Stundenprofil-Editor (24 Werte je Tagesart, Einfügen
/// aus der Zwischenablage mit benannter Ablehnung), die Vorschau, die dem Entwurf folgt, der Lesemodus eines
/// ausgelieferten Profils und der Fall OHNE Gaben — über einem Weg ohne Datenbank, der mitschreibt.
/// </summary>
/// <remarks>Die Kultur ist über <see cref="EposBunitContext"/> auf de-DE gepinnt.</remarks>
public class RaumnutzungBildEditorTests : EposBunitContext
{
    /// <summary>Ein Weg, der mitschreibt — die Rolle der Hülle im Prüfstand.</summary>
    private sealed class Probeweg
    {
        internal readonly List<string> Spur = new();
        internal int Vorschauen;

        internal RaumnutzungBildWeg Weg() => new()
        {
            Vorschau = (p, g) =>
            {
                Vorschauen++;
                return new RaumnutzungBildvorschau(true, RaumnutzungBild.Weg(p, g), Enumerable.Repeat(20.0, 168).ToArray(), null,
                                                   "", "Bezugsjahr 2025");
            },
            Zeilenbildvorschlag = (p, g) =>
            {
                Spur.Add("Zeilenbildvorschlag:" + g);
                return new[]
                {
                    new RaumnutzungZeilenbildDaten(g, "TAG", 21, false, null, null, null),
                    new RaumnutzungZeilenbildDaten(g, "NACHT", 17, false, 22, 6, null),
                };
            },
            Stundenvorschlag = (p, g) =>
            {
                Spur.Add("Stundenvorschlag:" + g);
                return new[]
                {
                    new RaumnutzungStundenDaten(g, RaumnutzungTagesart.Werktag, Enumerable.Repeat(20.0, 24).ToArray()),
                    new RaumnutzungStundenDaten(g, RaumnutzungTagesart.Frei, Enumerable.Repeat(16.0, 24).ToArray()),
                };
            },
        };
    }

    private static RaumnutzungProfilDaten Profil(bool ausgeliefert = false) => new()
    {
        Id = 7, IdKategorie = 2, Bezeichner = "Probe", Ausgeliefert = ausgeliefert,
        NutzungVon = 8, NutzungBis = 18, NutzungstageWoche = "1111100", HeizSoll = 20, HeizSollAusserhalb = 16,
    };

    private IRenderedComponent<RaumnutzungBildEditor> Editor(RaumnutzungProfilDaten p, RaumnutzungBildWeg? weg, bool nurLesen = false)
        => Render<RaumnutzungBildEditor>(ps => ps
            .Add(x => x.Profil, p)
            .Add(x => x.Weg, weg)
            .Add(x => x.NurLesen, nurLesen));

    private static IElement Wegknopf(IRenderedComponent<RaumnutzungBildEditor> c, RaumnutzungBildweg w)
        => c.Find(".epos-raumnutzung-bild-wegknopf[data-weg='" + (int)w + "']");

    // =================================================================
    //  Die reine Arbeit am Entwurf
    // =================================================================

    [Fact]
    public void Der_Weg_folgt_der_Reihenfolge_des_Generators()
    {
        RaumnutzungProfilDaten p = Profil();
        Assert.Equal(RaumnutzungBildweg.Kennwerte, RaumnutzungBild.Weg(p, KonditionierungGroesse.Heizen));
        RaumnutzungBild.StundenSetzen(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag, Enumerable.Repeat(20.0, 24).ToArray());
        Assert.Equal(RaumnutzungBildweg.Stundenprofil, RaumnutzungBild.Weg(p, KonditionierungGroesse.Heizen));
        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag, 21, false, null, null, null);
        Assert.Equal(RaumnutzungBildweg.Zeilenbild, RaumnutzungBild.Weg(p, KonditionierungGroesse.Heizen));
        Assert.Equal(RaumnutzungBildweg.Kennwerte, RaumnutzungBild.Weg(p, KonditionierungGroesse.Kuehlen));
    }

    [Fact]
    public void Eine_leere_Zeile_entfaellt_Fenster_nur_an_der_Nacht_DeltaT_nur_an_Lueftung()
    {
        RaumnutzungProfilDaten p = Profil();
        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag, 21, false, 6, 22, 2);
        RaumnutzungZeilenbildDaten tag = Assert.Single(p.Zeilenbild);
        Assert.Equal("TAG", tag.Zeile);
        Assert.Null(tag.Von);
        Assert.Null(tag.DeltaT);

        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht, 2, false, 22, 6, 2);
        RaumnutzungZeilenbildDaten nacht = RaumnutzungBild.Zeile(p, KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht)!;
        Assert.Equal((22, 6, 2.0), (nacht.Von!.Value, nacht.Bis!.Value, nacht.DeltaT!.Value));

        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag, null, false, null, null, null);
        Assert.Null(RaumnutzungBild.Zeile(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag));
        Assert.Equal(new[] { "NACHT" }, p.Zeilenbild.Select(z => z.Zeile));
    }

    [Fact]
    public void Der_erste_Stundenwert_nimmt_den_ganzen_Tag_und_beide_Tagesarten()
    {
        RaumnutzungProfilDaten p = Profil();
        RaumnutzungBild.StundeSetzen(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag, 7, 21);
        Assert.Equal(Enumerable.Repeat(21.0, 24), RaumnutzungBild.Stunden(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag)!);
        Assert.Equal(Enumerable.Repeat(21.0, 24), RaumnutzungBild.Stunden(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Frei)!);
        RaumnutzungBild.StundeSetzen(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag, 7, 18);
        Assert.Equal(18.0, RaumnutzungBild.Stunden(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag)![7]);
        Assert.Equal(21.0, RaumnutzungBild.Stunden(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Frei)![7]);
    }

    [Fact]
    public void Eine_Zeile_aus_der_Zwischenablage_liest_Tab_Leerzeichen_Komma_Prozent_und_aus()
    {
        var texte = new RaumnutzungBildTexte();
        RaumnutzungBildgrenzen anteil = RaumnutzungBildgrenzen.Offen(KonditionierungGroesse.Personen);
        string zeile = string.Join("\t", Enumerable.Range(0, 12).Select(_ => "0")) + " " +
                       string.Join(" ", Enumerable.Range(0, 12).Select(_ => "50,5"));
        double[] werte = RaumnutzungBild.ZeileLesen(zeile, anteil, "aus", texte, out string? meldung)!;
        Assert.Null(meldung);
        Assert.Equal(0.0, werte[0]);
        Assert.Equal(0.505, werte[23], 6);

        RaumnutzungBildgrenzen heizen = RaumnutzungBildgrenzen.Offen(KonditionierungGroesse.Heizen);
        werte = RaumnutzungBild.ZeileLesen(string.Join(" ", Enumerable.Repeat("aus", 6).Concat(Enumerable.Repeat("21", 18))), heizen,
                                           "aus", texte, out meldung)!;
        Assert.True(double.IsNaN(werte[0]));
        Assert.Equal(21.0, werte[6]);

        Assert.Null(RaumnutzungBild.ZeileLesen("1 2 3", heizen, "aus", texte, out meldung));
        Assert.Equal("Erwartet 24 Werte, gefunden 3.", meldung);
        Assert.Null(RaumnutzungBild.ZeileLesen(string.Join(" ", Enumerable.Repeat("21", 23).Append("90")), heizen, "aus", texte, out meldung));
        Assert.StartsWith("Stunde 23: „90“", meldung);
        Assert.Null(RaumnutzungBild.ZeileLesen(string.Join(" ", Enumerable.Repeat("aus", 24)), anteil, "aus", texte, out meldung));
        Assert.StartsWith("Stunde 0:", meldung);
    }

    // =================================================================
    //  Der Editor: Umschalter, Rückfrage, Vorschlag, Vorschau
    // =================================================================

    [Fact]
    public void Ohne_Gaben_zeigt_der_Editor_lesend_und_ohne_Vorschau()
    {
        RaumnutzungProfilDaten p = Profil();
        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag, 21, false, null, null, null);
        var c = Editor(p, null);

        Assert.Equal(5, c.FindAll(".epos-raumnutzung-bild-groesse").Count);
        Assert.Equal(3, c.FindAll(".epos-raumnutzung-bild-wegknopf").Count);
        Assert.Equal("true", Wegknopf(c, RaumnutzungBildweg.Zeilenbild).GetAttribute("aria-checked"));
        Assert.Empty(c.FindAll("input"));
        Assert.Contains("21", c.Find(".epos-raumnutzung-zeilenbildeditor tr[data-zeile='TAG']").TextContent);
        Assert.Empty(c.FindAll(".epos-raumnutzung-bild-vorschau"));

        Wegknopf(c, RaumnutzungBildweg.Kennwerte).Click();
        Assert.NotNull(c.Find(".epos-raumnutzung-bild-sperre"));
        Assert.Single(p.Zeilenbild);
    }

    [Fact]
    public void Ohne_Profil_zeichnet_der_Editor_nichts()
    {
        var c = Render<RaumnutzungBildEditor>();
        Assert.Empty(c.FindAll(".epos-raumnutzung-bild"));
    }

    [Fact]
    public void Umschalten_auf_Zeilenbild_legt_den_Vorschlag_aus_den_Kennwerten_an()
    {
        var w = new Probeweg();
        RaumnutzungProfilDaten p = Profil();
        var c = Editor(p, w.Weg());
        Assert.Equal("true", Wegknopf(c, RaumnutzungBildweg.Kennwerte).GetAttribute("aria-checked"));

        Wegknopf(c, RaumnutzungBildweg.Zeilenbild).Click();
        Assert.Equal(new[] { "Zeilenbildvorschlag:Heizen" }, w.Spur);
        Assert.Equal(new[] { "TAG", "NACHT" }, p.Zeilenbild.Select(z => z.Zeile));
        Assert.Equal("true", Wegknopf(c, RaumnutzungBildweg.Zeilenbild).GetAttribute("aria-checked"));
        Assert.Equal(4, c.FindAll(".epos-raumnutzung-zeilenbildeditor tbody tr").Count);
        Assert.Empty(c.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Aus_Kennwerten_fragt_und_loescht_erst_nach_Ja()
    {
        var w = new Probeweg();
        RaumnutzungProfilDaten p = Profil();
        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag, 21, false, null, null, null);
        var c = Editor(p, w.Weg());

        Wegknopf(c, RaumnutzungBildweg.Kennwerte).Click();
        Assert.Contains("Zeilenbild und Stundenprofil der Größe „Heizen“ löschen?", c.Find(".epos-rueckfrage-text").TextContent);
        c.FindAll(".epos-rueckfrage button").Single(b => b.TextContent == "Nein").Click();
        Assert.Single(p.Zeilenbild);
        Assert.Equal("true", Wegknopf(c, RaumnutzungBildweg.Zeilenbild).GetAttribute("aria-checked"));

        Wegknopf(c, RaumnutzungBildweg.Kennwerte).Click();
        c.FindAll(".epos-rueckfrage button").Single(b => b.TextContent == "Ja").Click();
        Assert.Empty(p.Zeilenbild);
        Assert.Equal("true", Wegknopf(c, RaumnutzungBildweg.Kennwerte).GetAttribute("aria-checked"));
        Assert.Empty(c.FindAll(".epos-raumnutzung-zeilenbildeditor"));
    }

    [Fact]
    public void Zeilenbild_zu_Stundenprofil_fragt_und_nimmt_den_Vorschlag_vor_dem_Loeschen()
    {
        var w = new Probeweg();
        RaumnutzungProfilDaten p = Profil();
        RaumnutzungBild.ZeileSetzen(p, KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag, 24, false, null, null, null);
        var c = Editor(p, w.Weg());
        c.Find(".epos-raumnutzung-bild-groesse[data-groesse='1']").Click();

        Wegknopf(c, RaumnutzungBildweg.Stundenprofil).Click();
        Assert.Contains("Das Zeilenbild der Größe „Kühlen“ löschen?", c.Find(".epos-rueckfrage-text").TextContent);
        c.FindAll(".epos-rueckfrage button").Single(b => b.TextContent == "Ja").Click();

        Assert.Equal(new[] { "Stundenvorschlag:Kuehlen" }, w.Spur);
        Assert.Empty(p.Zeilenbild);
        Assert.Equal(2, p.Stunden.Count(s => s.Groesse == KonditionierungGroesse.Kuehlen));
        Assert.Equal(48, c.FindAll(".epos-raumnutzung-stundeneditor .epos-raumnutzung-stunde input").Count);
    }

    [Fact]
    public void Die_Vorschau_folgt_dem_Entwurf_und_rechnet_nur_bei_Aenderung()
    {
        var w = new Probeweg();
        RaumnutzungProfilDaten p = Profil();
        var c = Editor(p, w.Weg());
        Assert.Equal(1, w.Vorschauen);
        Assert.NotNull(c.Find(".epos-raumnutzung-bild-vorschau [data-kennung='rnp-woche-0']"));

        c.Render();
        Assert.Equal(1, w.Vorschauen);

        Wegknopf(c, RaumnutzungBildweg.Zeilenbild).Click();
        Assert.Equal(2, w.Vorschauen);
        c.Find(".epos-raumnutzung-zeilenbildeditor tr[data-zeile='TAG'] input").Input("22");
        Assert.Equal(3, w.Vorschauen);
        Assert.Equal(22.0, RaumnutzungBild.Zeile(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag)!.Wert);

        // Eine Eingabe in den Kennwertgruppen des Blatts: der Wirt zeichnet neu, die Vorschau folgt.
        p.HeizSoll = 19;
        c.Render();
        Assert.Equal(4, w.Vorschauen);
    }

    [Fact]
    public void Ein_ausgeliefertes_Profil_bleibt_lesend_und_meldet_den_Grund()
    {
        var w = new Probeweg();
        RaumnutzungProfilDaten p = Profil(ausgeliefert: true);
        RaumnutzungBild.StundenSetzen(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag, Enumerable.Repeat(20.0, 24).ToArray());
        var c = Editor(p, w.Weg(), nurLesen: true);

        Assert.Equal("true", Wegknopf(c, RaumnutzungBildweg.Kennwerte).GetAttribute("aria-disabled"));
        Assert.Empty(c.FindAll(".epos-raumnutzung-stundeneditor input"));
        // Die Vorschau ist ein Leseraster: ihre Zellen sind gesperrt.
        Assert.All(c.FindAll(".epos-raumnutzung-bild-vorschau input"), i => Assert.True(i.HasAttribute("disabled")));
        Assert.Equal(48, c.FindAll(".epos-raumnutzung-stundeneditor .epos-kond-text").Count);
        Wegknopf(c, RaumnutzungBildweg.Kennwerte).Click();
        Assert.Equal("Ausgeliefert — nur duplizierbar.", c.Find(".epos-raumnutzung-bild-sperre").TextContent);
        Assert.Equal(2, p.Stunden.Count);
        Assert.Empty(w.Spur);
    }

    // =================================================================
    //  Der Zeilenbild-Editor
    // =================================================================

    [Fact]
    public void Der_Zeilenbild_Editor_fuehrt_vier_Zeilen_Fenster_an_der_Nacht_und_DeltaT_nur_an_Lueftung()
    {
        RaumnutzungProfilDaten p = Profil();
        var heizen = Render<RaumnutzungZeilenbildEditor>(ps => ps.Add(x => x.Profil, p).Add(x => x.Groesse, KonditionierungGroesse.Heizen));
        Assert.Equal(new[] { "TAG", "NACHT", "WOCHENENDE", "FERIEN" },
                     heizen.FindAll("tbody tr").Select(r => r.GetAttribute("data-zeile")));
        Assert.Equal(5, heizen.FindAll("input").Count);          // vier Werte, ein Nachtfenster
        Assert.Equal(3, heizen.FindAll("thead th").Count);

        var lueftung = Render<RaumnutzungZeilenbildEditor>(ps => ps.Add(x => x.Profil, p).Add(x => x.Groesse, KonditionierungGroesse.Lueftung));
        Assert.Equal(6, lueftung.FindAll("input").Count);        // dazu ΔT an der Nacht
        Assert.Equal(4, lueftung.FindAll("thead th").Count);

        heizen.Find("tr[data-zeile='NACHT'] .epos-stundenfenster input").Input("22-6");
        RaumnutzungZeilenbildDaten nacht = RaumnutzungBild.Zeile(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht)!;
        Assert.Equal((22, 6), (nacht.Von!.Value, nacht.Bis!.Value));
        heizen.Find("tr[data-zeile='WOCHENENDE'] input").Input("aus");
        Assert.True(RaumnutzungBild.Zeile(p, KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende)!.Aus);
    }

    [Fact]
    public void Der_Zeilenbild_Editor_zeigt_Anteile_in_Prozent_und_schreibt_den_Anteil()
    {
        RaumnutzungProfilDaten p = Profil();
        var c = Render<RaumnutzungZeilenbildEditor>(ps => ps.Add(x => x.Profil, p).Add(x => x.Groesse, KonditionierungGroesse.Personen));
        c.Find("tr[data-zeile='TAG'] input").Input("50");
        Assert.Equal(0.5, RaumnutzungBild.Zeile(p, KonditionierungGroesse.Personen, KonditionierungZeile.Tag)!.Wert);
        c.Find("tr[data-zeile='FERIEN'] input").Input("150");   // außerhalb der Grenzen: färbt, schreibt nicht
        Assert.Null(RaumnutzungBild.Zeile(p, KonditionierungGroesse.Personen, KonditionierungZeile.Ferien));
    }

    // =================================================================
    //  Der Stundenprofil-Editor
    // =================================================================

    [Fact]
    public void Der_Stunden_Editor_fuehrt_24_Werte_je_Tagesart_und_fuegt_eine_Zeile_ein()
    {
        RaumnutzungProfilDaten p = Profil();
        var c = Render<RaumnutzungStundenEditor>(ps => ps.Add(x => x.Profil, p).Add(x => x.Groesse, KonditionierungGroesse.Geraete));
        Assert.Equal(2, c.FindAll(".epos-raumnutzung-stundentag").Count);
        Assert.Equal(48, c.FindAll(".epos-raumnutzung-stunde input").Count);

        IElement knopf = c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-einfuegen-knopf");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.Equal("Zuerst eine Zeile mit 24 Werten eintragen.", c.Find(".epos-raumnutzung-einfuegen-meldung").TextContent);

        c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-einfuegen input").Input("10 20 30");
        c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-einfuegen-knopf").Click();
        Assert.Equal("Erwartet 24 Werte, gefunden 3.", c.Find(".epos-raumnutzung-einfuegen-meldung").TextContent);
        Assert.Empty(p.Stunden);

        string zeile = string.Join("\t", Enumerable.Range(0, 24).Select(h => h is >= 8 and < 18 ? "100" : "10"));
        c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-einfuegen input").Input(zeile);
        c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-einfuegen-knopf").Click();
        double[] werktag = RaumnutzungBild.Stunden(p, KonditionierungGroesse.Geraete, RaumnutzungTagesart.Werktag)!;
        Assert.Equal(1.0, werktag[8]);
        Assert.Equal(0.1, werktag[0]);
        Assert.Empty(c.FindAll(".epos-raumnutzung-einfuegen-meldung"));

        c.Find(".epos-raumnutzung-stundentag[data-tagesart='1'] .epos-raumnutzung-stunde[data-stunde='3'] input").Input("25");
        Assert.Equal(0.25, RaumnutzungBild.Stunden(p, KonditionierungGroesse.Geraete, RaumnutzungTagesart.Frei)![3]);
    }

    [Fact]
    public void Der_Stunden_Editor_im_Lesemodus_zeigt_nur_Text()
    {
        RaumnutzungProfilDaten p = Profil();
        RaumnutzungBild.StundenSetzen(p, KonditionierungGroesse.Heizen, RaumnutzungTagesart.Werktag,
                                      Enumerable.Repeat(double.NaN, 6).Concat(Enumerable.Repeat(21.0, 18)).ToArray());
        var c = Render<RaumnutzungStundenEditor>(ps => ps.Add(x => x.Profil, p).Add(x => x.Groesse, KonditionierungGroesse.Heizen)
                                                         .Add(x => x.NurLesen, true));
        Assert.Empty(c.FindAll("input"));
        Assert.Empty(c.FindAll(".epos-raumnutzung-einfuegen"));
        Assert.Equal("aus", c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-stunde[data-stunde='0'] .epos-kond-text").TextContent);
        Assert.Equal("21", c.Find(".epos-raumnutzung-stundentag[data-tagesart='0'] .epos-raumnutzung-stunde[data-stunde='6'] .epos-kond-text").TextContent);
    }
}

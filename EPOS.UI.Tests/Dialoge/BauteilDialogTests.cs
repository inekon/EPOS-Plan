using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Bauteildialog (Gebäudesimulation G3, Welle D2; Mehrzonenkonzept 5.1, 5.3): Feldbestand des
/// Formularrasters (Fensterwerte nur an Fenster und Vorhangfassade), die Aufbauwahl aus Projekt und
/// Katalog (die Katalogwahl schreibt nichts, sie merkt sich die Katalog-Id), die leise Zeile zum
/// U-Wert, die Prüfregeln des Kerns genau einmal im Rückruf der Leiste — eine Wand an Außenluft ohne
/// Azimut wird benannt abgelehnt —, der Rückweg und der Dialog ohne Gaben.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und
/// Zahlen.</para>
/// </summary>
public class BauteilDialogTests : EposBunitContext
{
    public BauteilDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly IReadOnlyList<AufbauWahl> PROJEKT = new[]
    {
        new AufbauWahl(11, false, "Außenwand Bestand", DbWerte.BAUTEILART_AUSSENWAND, 1.2),
        new AufbauWahl(12, false, "Außenwand gedämmt", DbWerte.BAUTEILART_AUSSENWAND, 0.25)
    };

    private static readonly IReadOnlyList<AufbauWahl> KATALOG = new[]
    {
        new AufbauWahl(501, true, "Dach Sparren gedämmt", DbWerte.BAUTEILART_DACH, 0.2)
    };

    private static BauteilDaten Wand() => new()
    {
        Id = 4,
        Bezeichner = "Wand West",
        Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
        Flaeche = 60,
        UWert = 0.3,
        Azimut = 270
    };

    /// <summary>Die übrigen Zonen eines Gebäudes mit zwei weiteren Zonen — eine davon vorläufig (negative Id).</summary>
    private static readonly IReadOnlyList<NachbarzoneWahl> NACHBARN = new[]
    {
        new NachbarzoneWahl(-3, "Keller"),
        new NachbarzoneWahl(8, "Obergeschoss")
    };

    private IRenderedComponent<BauteilDialog> Aufbauen(BauteilDaten? bauteil = null, Action<BauteilDaten?>? geschlossen = null,
                                                       IReadOnlyList<AufbauWahl>? katalog = null, bool neu = false,
                                                       IReadOnlyList<NachbarzoneWahl>? nachbarn = null, string? kopplungSperre = null)
        => Render<BauteilDialog>(p => p
            .Add(x => x.Bauteil, bauteil ?? Wand())
            .Add(x => x.Neu, neu)
            .Add(x => x.Projektaufbauten, PROJEKT)
            .Add(x => x.Katalogaufbauten, katalog ?? Array.Empty<AufbauWahl>())
            .Add(x => x.Nachbarzonen, nachbarn ?? Array.Empty<NachbarzoneWahl>())
            .Add(x => x.KopplungSperre, kopplungSperre)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement? Feld(IRenderedComponent<BauteilDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-feld")
              .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              ?.QuerySelector("input, select");

    private static IElement Knopf(IRenderedComponent<BauteilDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static void Ok(IRenderedComponent<BauteilDialog> cut)
        => cut.Find(".epos-bauteildialog > .epos-leiste button.epos-knopf--primaer").Click();

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Eine_Wand_zeigt_die_Grundfelder_ohne_Fensterwerte()
    {
        var cut = Aufbauen();

        foreach (string feld in new[] { "Bauteilart", "Bezeichnung", "Fläche", "Azimut", "Neigung",
                                        "Randbedingung", "Wärmebrücke ψ·L", "U-Wert", "Aufbau im Projekt" })
            Assert.True(Feld(cut, feld) is not null, feld);
        foreach (string feld in new[] { "g-Wert", "Rahmenanteil", "Verschattungsfaktor" })
            Assert.Null(Feld(cut, feld));

        Assert.Equal("Wand West", Feld(cut, "Bezeichnung")!.GetAttribute("value"));
        Assert.Equal("270", Feld(cut, "Azimut")!.GetAttribute("value"));
        Assert.Contains("90", Feld(cut, "Neigung")!.GetAttribute("placeholder") ?? "");
        Assert.Contains("Außenluft (Vorgabe)", Feld(cut, "Randbedingung")!.TextContent);
        // Mit einer einzigen Zone gibt es keine Nachbarzone zur Wahl — ab zwei Zonen steht sie da
        // (Stufe G6b, Ab_zwei_Zonen_waehlt_die_Randbedingung_Nachbarzone_samt_Zone_und_Zuordnung).
        Assert.DoesNotContain("Nachbarzone", cut.Markup);
        Assert.Equal("Bauteil", cut.Find(".epos-dialog-titel").TextContent.Trim());
    }

    [Fact]
    public void Ein_Fenster_zeigt_g_Wert_Rahmenanteil_und_Verschattung()
    {
        var cut = Aufbauen();

        Feld(cut, "Bauteilart")!.Change("3");                    // Fenster

        foreach (string feld in new[] { "g-Wert", "Rahmenanteil", "Verschattungsfaktor" })
            Assert.True(Feld(cut, feld) is not null, feld);
        Assert.Contains("Gebäude", Feld(cut, "g-Wert")!.GetAttribute("placeholder") ?? "");
    }

    [Fact]
    public void Ein_neues_Bauteil_traegt_den_Titel_Neues_Bauteil()
    {
        var cut = Aufbauen(new BauteilDaten { Id = -1 }, neu: true);

        Assert.Equal("Neues Bauteil", cut.Find(".epos-dialog-titel").TextContent.Trim());
    }

    // =================================================================================
    // Aufbau
    // =================================================================================

    [Fact]
    public void Die_Projektwahl_setzt_Aufbau_und_seinen_U_Wert()
    {
        var cut = Aufbauen();

        Feld(cut, "Aufbau im Projekt")!.Change("12");

        Assert.Equal(12, cut.Instance.Arbeitsstand.IdAufbau);
        Assert.Equal(0.25, cut.Instance.Arbeitsstand.UAufbau);
        Assert.Equal("Außenwand gedämmt", cut.Instance.Arbeitsstand.AufbauText);

        // Eingetragen 0,3 gegen 0,25 aus dem Aufbau: 20 % Abweichung - eine leise Zeile.
        Assert.Contains(cut.FindAll(".epos-herleitung"), z => z.TextContent.Contains("weicht um 20 %"));
    }

    [Fact]
    public void Ohne_Aufbau_bleibt_nur_der_U_Wert()
    {
        BauteilDaten b = Wand();
        b.IdAufbau = 11;
        b.AufbauText = "Außenwand Bestand";
        b.UAufbau = 1.2;
        var cut = Aufbauen(b);

        Feld(cut, "Aufbau im Projekt")!.Change("0");

        Assert.Null(cut.Instance.Arbeitsstand.IdAufbau);
        Assert.Null(cut.Instance.Arbeitsstand.UAufbau);
        Assert.Equal("", cut.Instance.Arbeitsstand.AufbauText);
    }

    /// <summary>Die Katalogwahl schreibt NICHTS: Sie merkt sich die Katalog-Id; übernommen wird im OK-Weg.</summary>
    [Fact]
    public void Die_Katalogwahl_merkt_sich_nur_die_Katalog_Id()
    {
        var cut = Aufbauen(katalog: KATALOG);

        // Ohne Wahl ist „Übernehmen" weich gesperrt und nennt seinen Grund.
        IElement knopf = Knopf(cut, "Übernehmen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.Equal("(Aufbau aus dem Katalog wählen)", cut.Instance.Meldung);
        Assert.Null(cut.Instance.Arbeitsstand.IdAufbauStamm);
    }

    // =================================================================================
    // Prüfregeln (Mehrzonenkonzept 5.3) und Rückweg
    // =================================================================================

    /// <summary>Eine Wand an Außenluft ohne Azimut wird benannt abgelehnt - nicht auf Nord vorbelegt.</summary>
    [Fact]
    public void Eine_Wand_an_Aussenluft_ohne_Azimut_wird_benannt_abgelehnt()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Feld(cut, "Azimut")!.Input("");
        Feld(cut, "Randbedingung")!.Change("1");                 // Außenluft
        Ok(cut);

        Assert.False(gerufen);
        Assert.Contains("Wand West", cut.Instance.Meldung);
        Assert.Contains("Azimut", cut.Instance.Meldung);
        Assert.Null(cut.Instance.Arbeitsstand.Azimut);
    }

    [Theory]
    [InlineData("Fläche", "0", "Fläche muss größer als null")]
    [InlineData("U-Wert", "9", "U = 9 liegt nicht in")]
    [InlineData("Bezeichnung", " ", "braucht einen Namen")]
    public void Die_Pruefregeln_des_Kerns_halten_den_Dialog_offen(string feld, string wert, string teil)
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Feld(cut, feld)!.Input(wert);
        Ok(cut);

        Assert.False(gerufen);
        Assert.Contains(teil, cut.Instance.Meldung);
        Assert.Contains(teil, cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_Fenster_an_Erdreich_wird_abgelehnt()
    {
        BauteilDaten b = Wand();
        b.Bauteilart = DbWerte.BAUTEILART_FENSTER;
        b.Randbedingung = DbWerte.RANDBEDINGUNG_ERDREICH;
        var cut = Aufbauen(b);

        Ok(cut);

        Assert.Contains("Außenluft, an einen unbeheizten Raum oder an eine Nachbarzone", cut.Instance.Meldung);
    }

    // =================================================================================
    // Stufe G6b: die Trennfläche zu einer Nachbarzone
    // =================================================================================

    /// <summary>
    /// Ab zwei Zonen steht „Nachbarzone" in der Randwahl (Umkehr der G3-Zusicherung „keine
    /// Nachbarzone"); gewählt, erscheinen die Wahl der Zone — über die Ids des Arbeitsstands, auch
    /// eine vorläufige, negative — und die Zuordnung der Trennfläche.
    /// </summary>
    [Fact]
    public void Ab_zwei_Zonen_waehlt_die_Randbedingung_Nachbarzone_samt_Zone_und_Zuordnung()
    {
        BauteilDaten? zurueck = null;
        BauteilDaten b = Wand();
        b.Bauteilart = DbWerte.BAUTEILART_INNENWAND;
        b.Azimut = null;
        var cut = Aufbauen(b, z => zurueck = z, nachbarn: NACHBARN);

        Assert.Contains("Nachbarzone", Feld(cut, "Randbedingung")!.TextContent);
        Assert.Null(Feld(cut, "Nachbarzone"));

        Feld(cut, "Randbedingung")!.Change("4");
        IElement zone = Feld(cut, "Nachbarzone")!;
        Assert.Contains("Keller", zone.TextContent);
        Assert.Contains("Obergeschoss", zone.TextContent);
        Assert.Contains("(Zone wählen)", zone.TextContent);
        Assert.Contains("automatisch (4-K-Regel)", Feld(cut, "Trennfläche rechnen")!.TextContent);
        Assert.Contains("4 K", cut.Markup);

        zone.Change("-3");
        Feld(cut, "Trennfläche rechnen")!.Change("2");
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, zurueck!.Randbedingung);
        Assert.Equal(-3, zurueck.IdNachbarzone);
        Assert.Equal(DbWerte.TRENNFLAECHE_AW, zurueck.TrennflaecheZuordnung);
    }

    [Fact]
    public void Eine_Trennflaeche_ohne_Nachbarzone_wird_benannt_abgelehnt()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true, nachbarn: NACHBARN);

        Feld(cut, "Randbedingung")!.Change("4");
        Ok(cut);

        Assert.False(gerufen);
        Assert.Contains("braucht die Angabe der Nachbarzone", cut.Instance.Meldung);
    }

    [Fact]
    public void Wer_die_Nachbarzone_verlaesst_nimmt_Nachbar_und_Zuordnung_mit()
    {
        BauteilDaten? zurueck = null;
        BauteilDaten b = Wand();
        b.Randbedingung = DbWerte.RANDBEDINGUNG_ZONE;
        b.IdNachbarzone = 8;
        b.TrennflaecheZuordnung = DbWerte.TRENNFLAECHE_IW;
        var cut = Aufbauen(b, z => zurueck = z, nachbarn: NACHBARN);

        Assert.Equal("8", Feld(cut, "Nachbarzone")!.GetAttribute("value"));
        Feld(cut, "Randbedingung")!.Change("1");                 // Außenluft
        Assert.Null(Feld(cut, "Nachbarzone"));
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, zurueck!.Randbedingung);
        Assert.Null(zurueck.IdNachbarzone);
        Assert.Null(zurueck.TrennflaecheZuordnung);
    }

    /// <summary>Ohne Schemaschritt S-G (etwa iOS) steht die Nachbarzone nicht in der Wahl; eine leise Zeile nennt den Grund.</summary>
    [Fact]
    public void Ohne_Schemaschritt_steht_die_Nachbarzone_nicht_in_der_Wahl()
    {
        var cut = Aufbauen(nachbarn: NACHBARN, kopplungSperre: "Schemaschritt 147 fehlt.");

        Assert.DoesNotContain("Nachbarzone", Feld(cut, "Randbedingung")!.TextContent);
        Assert.Contains("Schemaschritt 147 fehlt.", cut.Markup);
    }

    [Fact]
    public void Der_Assistent_setzt_Nachbarzone_und_Zuordnung_einer_Trennflaeche()
    {
        BauteilDaten b = Wand();
        b.Randbedingung = DbWerte.RANDBEDINGUNG_ZONE;
        var cut = Aufbauen(b, nachbarn: NACHBARN);

        KiFeldzugang nachbar = KiMaskenbruecke.Feldzugang(KiMaskennamen.BAUTEIL, "nachbarzone");
        Assert.NotNull(nachbar);
        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(nachbar, "Keller");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        nachbar.Setzen(umsetzung.Wert);

        KiFeldzugang zuordnung = KiMaskenbruecke.Feldzugang(KiMaskennamen.BAUTEIL, "zuordnung");
        Assert.NotNull(zuordnung);
        KiFeldumsetzung z = KiFeldwandler.Wandle(zuordnung, "wie Innenbauteil");
        Assert.True(z.Ok, z.Grund);
        zuordnung.Setzen(z.Wert);
        cut.Render();

        Assert.Equal(-3, cut.Instance.Arbeitsstand.IdNachbarzone);
        Assert.Equal(DbWerte.TRENNFLAECHE_IW, cut.Instance.Arbeitsstand.TrennflaecheZuordnung);
    }

    [Fact]
    public void OK_gibt_eine_Kopie_zurueck_und_laesst_Fensterwerte_an_einer_Wand_fallen()
    {
        BauteilDaten vorlage = Wand();
        vorlage.GWert = 0.6;                                     // aus einer früheren Fensterzeile
        BauteilDaten? zurueck = null;
        var cut = Aufbauen(vorlage, b => zurueck = b);

        Feld(cut, "Fläche")!.Input("65,5");
        Feld(cut, "Wärmebrücke ψ·L")!.Input("2");
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(65.5, zurueck!.Flaeche);
        Assert.Equal(2, zurueck.PsiL);
        Assert.Null(zurueck.GWert);
        Assert.Equal(60, vorlage.Flaeche);
        Assert.NotSame(vorlage, zurueck);
    }

    [Fact]
    public void Abbrechen_und_Esc_geben_null()
    {
        var antworten = new List<BauteilDaten?>();
        var cut = Aufbauen(geschlossen: b => antworten.Add(b));

        Knopf(cut, "Abbrechen").Click();
        cut.Find(".epos-bauteildialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(2, antworten.Count);
        Assert.All(antworten, Assert.Null);
    }

    // =================================================================================
    // Ohne Gaben und der Hilfe-Assistent
    // =================================================================================

    [Fact]
    public void Ohne_Gaben_steht_eine_Aussenwand_ohne_Aufbauwahl_aus_dem_Katalog()
    {
        var cut = Render<BauteilDialog>();

        Assert.Equal(DbWerte.BAUTEILART_AUSSENWAND, cut.Instance.Arbeitsstand.Bauteilart);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Übernehmen");
        Ok(cut);
        Assert.Equal("Das Bauteil braucht einen Namen.", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die Sichtklasse
    /// <c>BauteilKiSicht</c>: Die Bauteilart ist ein Wahlfeld über ihren Anzeigetext, die Fläche eine Zahl.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BAUTEIL));

        KiFeldzugang art = KiMaskenbruecke.Feldzugang(KiMaskennamen.BAUTEIL, "art");
        Assert.NotNull(art);
        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(art, "Fenster");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        art.Setzen(umsetzung.Wert);
        cut.Render();
        Assert.Equal(DbWerte.BAUTEILART_FENSTER, cut.Instance.Arbeitsstand.Bauteilart);

        KiFeldzugang flaeche = KiMaskenbruecke.Feldzugang(KiMaskennamen.BAUTEIL, "flaeche");
        Assert.NotNull(flaeche);
        Assert.Equal(60.0, flaeche.Lesen());
    }
}

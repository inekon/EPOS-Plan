using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// QuellePufferspeicherDialog (iU9-W10a.5) - der Ersatz fuer
/// Form_QuellePufferspeicher.
///
/// <para>FELDBESTAND laut Feldkarte: 29 Steuerelemente JE ART. Die beiden
/// Ausprägungen - Waermepumpe und Heizkessel - haben deshalb ihre eigenen Faelle,
/// wie in Welle 8 die drei Bedarfsblaetter.</para>
/// </summary>
public class QuellePufferspeicherDialogTests : BunitContext
{
    private const string BERECHNET = "berechnet";
    private const string FEST = "fest";

    public QuellePufferspeicherDialogTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<QuellPufferzeile> Zwei() => new[]
    {
        new QuellPufferzeile(11, "Heizungsspeicher", "Heizungsspeicher · Heizung · 800 l · 70/50 °C",
                             "Heizung, 800 l, 1,5 kWh/24h, 70/50 °C", 800),
        new QuellPufferzeile(12, "Brauchwasserspeicher", "Brauchwasserspeicher · Brauchwasser · 300 l",
                             "Brauchwasser, 300 l, 0,8 kWh/24h, -", 300)
    };

    private static QuellePufferspeicherDaten Wp() => new()
    {
        WPName = "WP Erdgeschoss",
        IdProjekt = 1030,
        IstKessel = false,
        IdPuffer = 11,
        Pufferspeicher = "Heizungsspeicher",
        Quelltemperatur = 12,
        Spreizung = 6,
        Regeneration = 2,
        Unbegrenzt = false,
        Anschlusshoehe = null,
        TemperaturModus = BERECHNET
    };

    private static QuellePufferspeicherDaten Kessel() => Wp() with
    {
        WPName = "Kessel 1",
        IstKessel = true,
        TemperaturModus = BERECHNET,
        VorlaufAnlage = 0,
        RuecklaufAnlage = 0
    };

    private IRenderedComponent<QuellePufferspeicherDialog> Zeige(
        QuellePufferspeicherDaten daten,
        Action<QuellePufferspeicherDaten?>? geschlossen = null,
        IReadOnlyList<QuellPufferzeile>? puffer = null,
        IReadOnlyDictionary<string, object>? verwaltung = null)
    {
        return Render<QuellePufferspeicherDialog>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Puffer, puffer ?? Zwei());
            p.Add(x => x.SteuerwertBerechnet, BERECHNET);
            p.Add(x => x.SteuerwertFest, FEST);
            p.Add(x => x.Kapazitaet, (v, dt) => v * 1.16 * dt / 1000.0);
            if (verwaltung is not null) p.Add(x => x.VerwaltungGaben, verwaltung);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });
    }

    // ============================================================ Feldbestand je Art

    /// <summary>
    /// Die WP zeigt den Parameterblock (drei Zahlen und den Haken) plus die
    /// Entnahmehoehe; den Temperaturbezug zeigt sie NICHT.
    /// </summary>
    [Fact]
    public void Die_Waermepumpe_zeigt_ihren_Parameterblock()
    {
        var cut = Zeige(Wp());

        Assert.Equal("Wärmequelle Pufferspeicher — WP Erdgeschoss",
                     cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll("input[type=checkbox]"));                 // "unbegrenzt"
        Assert.Empty(cut.FindAll("input[type=radio]"));                    // kein Temperaturbezug
        // Quelltemperatur, Spreizung, Regeneration, Entnahmehoehe.
        Assert.Equal(4, cut.FindAll("input.epos-eingabe").Count);
    }

    /// <summary>
    /// Der Kessel zeigt den Temperaturbezug (zwei Wahlknoepfe) und KEINEN
    /// Parameterblock - ArtAnwenden:764-780.
    /// </summary>
    [Fact]
    public void Der_Kessel_zeigt_den_Temperaturbezug_statt_der_Parameter()
    {
        var cut = Zeige(Kessel());

        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);
        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        // Nur die Entnahmehoehe - die beiden Temperaturfelder erscheinen erst bei "fest".
        Assert.Single(cut.FindAll("input.epos-eingabe"));
    }

    // ============================================================ Vorauswahl

    /// <summary>
    /// VorauswahlSetzen:829-847 - Fremdschluessel, sonst Bezeichner, sonst der erste.
    /// </summary>
    [Fact]
    public void Die_Vorauswahl_folgt_dem_Fremdschluessel()
    {
        Assert.Equal(11, Zeige(Wp()).Instance.GewaehlterPuffer);
        Assert.Equal(12, Zeige(Wp() with { IdPuffer = 12 }).Instance.GewaehlterPuffer);
    }

    [Fact]
    public void Ohne_Fremdschluessel_entscheidet_der_Bezeichner()
    {
        var cut = Zeige(Wp() with { IdPuffer = 0, Pufferspeicher = "Brauchwasserspeicher" });
        Assert.Equal(12, cut.Instance.GewaehlterPuffer);
    }

    [Fact]
    public void Ohne_beides_steht_der_erste_Eintrag()
    {
        var cut = Zeige(Wp() with { IdPuffer = 0, Pufferspeicher = "" });
        Assert.Equal(11, cut.Instance.GewaehlterPuffer);
    }

    // ============================================================ Die zwei Hinweise

    /// <summary>
    /// Befund W10-B16: Im Vorlaeufer trug EIN Label zwei Aussagen. Hier sind es zwei
    /// Warnbanner - der Leerhinweis (es gibt gar keinen Puffer) …
    /// </summary>
    [Fact]
    public void Ohne_Projektpuffer_steht_der_Leerhinweis()
    {
        var cut = Zeige(Wp(), puffer: Array.Empty<QuellPufferzeile>());

        Assert.Contains("noch keinen Pufferspeicher", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-zr-zeile"));
    }

    /// <summary>… und die Altbezeichner-Warnung (Fremdschluessel fehlt, Name steht da).</summary>
    [Fact]
    public void Ein_Altbezeichner_ohne_Fremdschluessel_warnt()
    {
        var mit = Zeige(Wp() with { IdPuffer = 0, Pufferspeicher = "Heizungsspeicher" });
        Assert.Contains("bisher nur über den Namen", mit.Instance.Altbezeichnerhinweis);

        var ohne = Zeige(Wp());
        Assert.Equal("", ohne.Instance.Altbezeichnerhinweis);
    }

    // ============================================================ Kapazität

    [Fact]
    public void Die_Kapazitaet_folgt_Speicher_und_Spreizung()
    {
        var cut = Zeige(Wp());       // 800 l, Spreizung 6 -> 800*1,16*6/1000 = 5,568
        Assert.Contains("5,6", cut.Instance.Kapazitaetszeile);

        // Ein anderer Speicher, andere Zahl.
        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Assert.Contains("2,1", cut.Instance.Kapazitaetszeile);
    }

    // ============================================================ Konflikt

    /// <summary>
    /// UnbegrenztKonfliktAnzeigen:934-953 - Haken UND gewaehlter Puffer ergeben eine
    /// WARNUNG samt der Temperatur, die dann gaelte. Der Dialog verwirft nichts.
    /// </summary>
    [Fact]
    public void Unbegrenzt_und_Puffer_ergeben_eine_Warnung_ohne_Korrektur()
    {
        var cut = Zeige(Wp());
        Assert.False(cut.Instance.UnbegrenztKonflikt);

        cut.Find("input[type=checkbox]").Change(true);

        Assert.True(cut.Instance.UnbegrenztKonflikt);
        Assert.Contains("konstant 12 °C", cut.Instance.UnbegrenztKonflikttext);
        Assert.Equal(11, cut.Instance.GewaehlterPuffer);        // nichts wurde verworfen
    }

    // ============================================================ Temperaturbezug

    /// <summary>
    /// rbTemperaturbezug_CheckedChanged:478-490 - der Vorschlag 70/50 erscheint NUR bei
    /// leeren Feldern.
    /// </summary>
    [Fact]
    public void Der_Vorschlag_70_50_kommt_nur_bei_leeren_Feldern()
    {
        var leer = Zeige(Kessel());
        Assert.Null(leer.Instance.Temperaturpaar.Vorlauf);

        leer.FindAll("input[type=radio]")[1].Change("1");
        Assert.Equal((70, 50), leer.Instance.Temperaturpaar);

        // Mit gepflegtem Paar bleibt es dabei.
        var mit = Zeige(Kessel() with { VorlaufAnlage = 65, RuecklaufAnlage = 45 });
        Assert.Equal((65, 45), mit.Instance.Temperaturpaar);
        mit.FindAll("input[type=radio]")[1].Change("1");
        Assert.Equal((65, 45), mit.Instance.Temperaturpaar);
    }

    /// <summary>
    /// TemperaturbezugSetzen:383-395 - beide Zahlen bleiben LEER, solange kein
    /// VOLLSTAENDIGES Paar dasteht: "0/0 °C" waere eine Angabe, die es nicht gibt.
    /// </summary>
    [Fact]
    public void Ein_unvollstaendiges_Paar_bleibt_leer()
    {
        var cut = Zeige(Kessel() with { VorlaufAnlage = 60, RuecklaufAnlage = 0 });
        Assert.Equal((null, null), cut.Instance.Temperaturpaar);
    }

    // ============================================================ OK-Regeln

    [Fact]
    public void Ohne_Auswahl_meldet_OK_je_nach_Lage()
    {
        // Liste leer: der Leerhinweis, denn nur der Absprung hilft.
        var leer = Zeige(Wp() with { IdPuffer = 0, Pufferspeicher = "" },
                         puffer: Array.Empty<QuellPufferzeile>());
        Ok(leer);
        Assert.Contains("noch keinen Pufferspeicher", leer.Instance.Meldung);
    }

    /// <summary>
    /// AnschlusshoeheUebernehmen:1066-1087 - leer ist gueltig (oben), sonst 0…1; ein
    /// Wert ausserhalb wird ABGEWIESEN statt geklemmt.
    /// </summary>
    [Fact]
    public void Eine_Entnahmehoehe_ausserhalb_wird_abgewiesen()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var cut = Zeige(Wp(), d => ergebnis = d);

        Hoehe(cut, "1,5");
        Ok(cut);

        Assert.Contains("zwischen 0 und 1", cut.Instance.Meldung);
        Assert.Null(ergebnis);
    }

    /// <summary>Leer heisst OBEN und geht durch.</summary>
    [Fact]
    public void Eine_leere_Entnahmehoehe_geht_durch()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var cut = Zeige(Wp(), d => ergebnis = d);

        Ok(cut);

        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.Anschlusshoehe);
    }

    [Fact]
    public void Eine_Spreizung_von_null_wird_abgewiesen()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var cut = Zeige(Wp(), d => ergebnis = d);

        cut.FindAll("input.epos-eingabe")[1].Input("0");
        Ok(cut);

        Assert.Contains("größer als 0 K", cut.Instance.Meldung);
        Assert.Null(ergebnis);
    }

    /// <summary>Beim Kessel wird das Temperaturpaar geprueft — aber nur im Modus „fest".</summary>
    [Fact]
    public void Der_Kessel_prueft_sein_Temperaturpaar_nur_bei_fest()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var cut = Zeige(Kessel(), d => ergebnis = d);

        // "berechnet": geht durch, ohne dass ein Paar dasteht.
        Ok(cut);
        Assert.NotNull(ergebnis);

        // "fest" mit unbrauchbarem Paar: abgewiesen.
        ergebnis = null;
        cut = Zeige(Kessel(), d => ergebnis = d);
        cut.FindAll("input[type=radio]")[1].Change("1");
        cut.FindAll("input.epos-eingabe")[0].Input("40");     // Vorlauf unter Ruecklauf
        cut.FindAll("input.epos-eingabe")[1].Input("60");
        Ok(cut);
        Assert.Contains("Rücklauf über 0", cut.Instance.Meldung);
        Assert.Null(ergebnis);
    }

    // ============================================================ Rückschreiben

    [Fact]
    public void Die_Waermepumpe_schreibt_ihre_vier_Parameter_zurueck()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var cut = Zeige(Wp(), d => ergebnis = d);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();     // Brauchwasserspeicher
        cut.Find("input[type=checkbox]").Change(true);
        Ok(cut);

        Assert.NotNull(ergebnis);
        Assert.Equal(12, ergebnis!.IdPuffer);
        Assert.Equal("Brauchwasserspeicher", ergebnis.Pufferspeicher);
        Assert.Equal(12.0, ergebnis.Quelltemperatur);
        Assert.Equal(6.0, ergebnis.Spreizung);
        Assert.Equal(2.0, ergebnis.Regeneration);
        Assert.True(ergebnis.Unbegrenzt);
    }

    /// <summary>
    /// WOERTLICH TROTZ BEFUND W10-B15: Beim KESSEL bleiben die vier WP-Parameter
    /// UNANGETASTET - sonst ueberschriebe eine Kesselbearbeitung sie mit 10 °C/5 K.
    /// </summary>
    [Fact]
    public void Der_Kessel_laesst_die_WP_Parameter_unangetastet()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var eingang = Kessel() with { Quelltemperatur = 17, Spreizung = 9, Regeneration = 3,
                                      Unbegrenzt = true };
        var cut = Zeige(eingang, d => ergebnis = d);

        cut.FindAll("input[type=radio]")[1].Change("1");
        Ok(cut);

        Assert.NotNull(ergebnis);
        Assert.Equal(17.0, ergebnis!.Quelltemperatur);
        Assert.Equal(9.0, ergebnis.Spreizung);
        Assert.Equal(3.0, ergebnis.Regeneration);
        Assert.True(ergebnis.Unbegrenzt);

        // Der Temperaturbezug dagegen kommt neu.
        Assert.Equal(FEST, ergebnis.TemperaturModus);
        Assert.Equal(70, ergebnis.VorlaufAnlage);
        Assert.Equal(50, ergebnis.RuecklaufAnlage);
    }

    /// <summary>
    /// Bei „berechnet" bleibt ein einmal gepflegtes Paar an der Anlage stehen — es ist
    /// dort auch fuer andere Auswertungen die Systemvorgabe.
    /// </summary>
    [Fact]
    public void Bei_berechnet_bleibt_das_gepflegte_Paar_stehen()
    {
        QuellePufferspeicherDaten? ergebnis = null;
        var cut = Zeige(Kessel() with { VorlaufAnlage = 65, RuecklaufAnlage = 45 },
                        d => ergebnis = d);

        Ok(cut);

        Assert.Equal(BERECHNET, ergebnis!.TemperaturModus);
        Assert.Equal(65, ergebnis.VorlaufAnlage);
        Assert.Equal(45, ergebnis.RuecklaufAnlage);
    }

    // ============================================================ Überlagerung

    /// <summary>Ohne Parametersatz der Verwaltung fehlt der Knopf — Hausregel.</summary>
    [Fact]
    public void Ohne_Verwaltungsgaben_gibt_es_keinen_Anlegeknopf()
    {
        var cut = Zeige(Wp());
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("anlegen"));
    }

    [Fact]
    public void Der_Anlegeknopf_oeffnet_die_Ueberlagerung()
    {
        var gaben = new Dictionary<string, object> { ["IdProjekt"] = 1030 };
        var cut = Zeige(Wp(), verwaltung: gaben);

        Assert.False(cut.Instance.VerwaltungOffen);
        cut.FindAll("button").First(b => b.TextContent.Contains("anlegen")).Click();
        Assert.True(cut.Instance.VerwaltungOffen);
    }

    // ============================================================ Schluss

    [Fact]
    public void Abbrechen_und_Esc_liefern_null()
    {
        QuellePufferspeicherDaten? ergebnis = Wp();
        var cut = Zeige(Wp(), d => ergebnis = d);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent == "Abbrechen").Click();
        Assert.Null(ergebnis);

        ergebnis = Wp();
        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Null(ergebnis);
    }

    private static void Hoehe(IRenderedComponent<QuellePufferspeicherDialog> cut, string wert)
        => cut.FindAll("input.epos-eingabe").Last().Input(wert);

    /// <summary>
    /// Der OK-Knopf der Schlussleiste. NICHT ueber "button.epos-knopf--primaer"
    /// suchen: Auch der Wahlknopf der markierten Rasterzeile traegt diese Klasse
    /// (Baustein Zeilenwahl).
    /// </summary>
    private static void Ok(IRenderedComponent<QuellePufferspeicherDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").Last().Click();
}

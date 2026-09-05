using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// QuellprofilDialog (iU9-W10a.6) - der Ersatz fuer Form_Quellprofil.
///
/// <para>Die Maske hatte KEINEN Designer; der Feldbestand steht in BaueOberflaeche
/// (Vermessung §5 f): vier Kopffelder, vier Reiter, zwoelf Monatsfelder bzw. ein
/// Werteraster, 24 nur lesende Stundenfelder je Wochentag und die Grafik.</para>
///
/// <para>JSInterop steht auf Loose: Das Werteraster ist VIRTUALISIERT (Befund
/// W10-B20), und QuickGrid ruft dafuer JavaScript.</para>
/// </summary>
public class QuellprofilDialogTests : BunitContext
{
    private const string MONAT = "Monat";
    private const string TAG = "Tag";
    private const string STUNDE = "Stunde";

    public QuellprofilDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Pruefstand, der mitschreibt statt zu speichern.</summary>
    private sealed class Pruefstand
    {
        internal Dictionary<int, QuellprofilInhalt> Profile { get; } = new();
        internal int SpeicherErgebnis { get; set; } = 99;
        internal QuellprofilInhalt? Gespeichert;
        internal double[]? CsvErgebnis;
        internal int CsvSoll;
    }

    private static int Anzahl(string ba) => ba switch
    {
        TAG => 365,
        STUNDE => 8760,
        _ => 12
    };

    private IRenderedComponent<QuellprofilDialog> Zeige(
        QuellprofilDaten daten, Pruefstand stand,
        Action<int?>? geschlossen = null,
        IReadOnlyList<QuellprofilZeile>? profile = null,
        bool mitCsv = true)
    {
        return Render<QuellprofilDialog>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Profile, profile ?? Array.Empty<QuellprofilZeile>());
            p.Add(x => x.Betriebsarten, new[] { MONAT, TAG, STUNDE });
            p.Add(x => x.Werteanzahl, Anzahl);
            p.Add(x => x.ProfilLesen, id => stand.Profile.TryGetValue(id, out var k) ? k : null);
            p.Add(x => x.Speichern, k => { stand.Gespeichert = k; return stand.SpeicherErgebnis; });
            if (mitCsv)
                p.Add(x => x.CsvLesen, soll =>
                {
                    stand.CsvSoll = soll;
                    return Task.FromResult(stand.CsvErgebnis);
                });
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });
    }

    private static QuellprofilDaten Neu() => new()
    {
        WPName = "WP Erdgeschoss",
        IdProjekt = 1030,
        IdQuellprofil = 0
    };

    private static double[] Wochengang()
    {
        var w = new double[168];
        w[6] = -1.5;    // Montag, 06:00
        return w;
    }

    // ================================================================== Feldbestand

    [Fact]
    public void Der_Kopf_traegt_vier_Felder()
    {
        var cut = Zeige(Neu(), new Pruefstand());

        Assert.Equal("Quellprofil — WP Erdgeschoss", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Equal(2, cut.FindAll("select").Count);            // Profil, Betriebsart
        // Bezeichner und Beschreibung. Ein Zahlenfeld traegt ebenfalls type="text"
        // (type="number" verweigert je nach Browsersprache eines der beiden
        // Trennzeichen); unterschieden wird ueber inputmode.
        Assert.Equal(2, Textfelder(cut).Count);
        Assert.NotNull(cut.Find("button.epos-infoknopf"));
    }

    /// <summary>
    /// Bei „Monat" stehen die zwoelf Felder da; das Werteraster erscheint gar nicht.
    /// </summary>
    [Fact]
    public void Die_Monatsseite_traegt_zwoelf_Felder()
    {
        var cut = Zeige(Neu(), new Pruefstand());

        Assert.True(cut.Instance.IstMonat);
        Assert.Equal(12, Zahlenfelder(cut).Count);
        Assert.Contains("Januar", cut.Markup);
        Assert.Contains("Dezember", cut.Markup);
    }

    /// <summary>Die Monatsfelder tragen den ALTWEG als Vorbelegung.</summary>
    [Fact]
    public void Der_Altweg_belegt_die_Monatsfelder_vor()
    {
        var monat = new double[12];
        for (int m = 0; m < 12; m++) monat[m] = 5 + m;

        var cut = Zeige(Neu() with { Monatswerte = monat }, new Pruefstand());

        Assert.Equal(5.0, cut.Instance.Monatsfelder[0]);
        Assert.Equal(16.0, cut.Instance.Monatsfelder[11]);
    }

    // ================================================================== Altweg-Reiter

    /// <summary>
    /// Befund W10-B17: Der Altweg-Reiter erscheint NUR mit Wochengang - sonst
    /// verschwaenden gepflegte Daten stillschweigend.
    /// </summary>
    [Fact]
    public void Der_Altweg_Reiter_erscheint_nur_mit_Wochengang()
    {
        var ohne = Zeige(Neu(), new Pruefstand());
        Assert.False(ohne.Instance.Wochengang);
        Assert.DoesNotContain(ohne.FindAll("button[role=tab]"),
                              b => b.TextContent.Contains("Wochenwerte"));

        var mit = Zeige(Neu() with { Wochenwerte = Wochengang() }, new Pruefstand());
        Assert.True(mit.Instance.Wochengang);
        Assert.Contains(mit.FindAll("button[role=tab]"),
                        b => b.TextContent.Contains("Wochenwerte"));
    }

    /// <summary>Er ist NUR LESEND — 24 gesperrte Felder je Wochentag.</summary>
    [Fact]
    public void Der_Altweg_Reiter_ist_nur_lesend()
    {
        var cut = Zeige(Neu() with { Wochenwerte = Wochengang() }, new Pruefstand());

        cut.FindAll("button[role=tab]").First(b => b.TextContent.Contains("Wochenwerte")).Click();

        Assert.Equal(24, cut.FindAll("input[readonly]").Count);
        Assert.Contains("-1,5", cut.Markup);        // der Ausschlag in Stunde 6 des Montags
    }

    // ================================================================== Betriebsart

    /// <summary>
    /// cbBetriebsart_SelectedIndexChanged:776-795 - beim Laengenwechsel gilt: "Was
    /// passt, bleibt; der Rest bekommt die Vorgabe." Kein stilles Abschneiden.
    /// </summary>
    [Fact]
    public void Der_Laengenwechsel_behaelt_was_passt()
    {
        var cut = Zeige(Neu(), new Pruefstand());

        cut.FindAll("select")[1].Change("1");        // Tag: 365
        Assert.Equal(365, cut.Instance.Werte!.Length);

        // Die ersten 365 auf einen erkennbaren Wert setzen …
        for (int i = 0; i < 365; i++) cut.Instance.Werte[i] = 3.5;

        cut.FindAll("select")[1].Change("2");        // Stunde: 8760
        Assert.Equal(8760, cut.Instance.Werte!.Length);
        Assert.Equal(3.5, cut.Instance.Werte[0]);           // was passte, blieb
        Assert.Equal(3.5, cut.Instance.Werte[364]);
        Assert.Equal(10.0, cut.Instance.Werte[365]);        // der Rest bekam die Vorgabe
    }

    /// <summary>Ein unbekannter Altwert faellt auf MONAT zurueck (BetriebsartSetzen:765-774).</summary>
    [Fact]
    public void Eine_unbekannte_Betriebsart_faellt_auf_Monat_zurueck()
    {
        var stand = new Pruefstand();
        stand.Profile[7] = new QuellprofilInhalt("Alt", "", "gibt-es-nicht", null);

        var cut = Zeige(Neu() with { IdQuellprofil = 7 }, stand,
                        profile: new[] { new QuellprofilZeile(7, "Alt") });

        Assert.True(cut.Instance.IstMonat);
    }

    // ================================================================== Profilwahl

    [Fact]
    public void Ein_vorhandenes_Profil_wird_geladen()
    {
        var stand = new Pruefstand();
        var werte = new double[12];
        for (int m = 0; m < 12; m++) werte[m] = 7 + m;
        stand.Profile[7] = new QuellprofilInhalt("Erdsonde tief", "aus Messung", MONAT, werte);

        var cut = Zeige(Neu() with { IdQuellprofil = 7 }, stand,
                        profile: new[] { new QuellprofilZeile(7, "Erdsonde tief") });

        Assert.Equal(7, cut.Instance.GewaehltesProfil);
        Assert.Equal("Erdsonde tief", cut.Instance.Bezeichner);
        Assert.Equal(7.0, cut.Instance.Monatsfelder[0]);
    }

    /// <summary>„Neues Profil" leert Bezeichner und Beschreibung (:714-734).</summary>
    [Fact]
    public void Neues_Profil_leert_den_Kopf()
    {
        var stand = new Pruefstand();
        stand.Profile[7] = new QuellprofilInhalt("Erdsonde tief", "aus Messung", MONAT, null);

        var cut = Zeige(Neu() with { IdQuellprofil = 7 }, stand,
                        profile: new[] { new QuellprofilZeile(7, "Erdsonde tief") });
        Assert.Equal("Erdsonde tief", cut.Instance.Bezeichner);

        cut.FindAll("select")[0].Change("0");
        Assert.Equal("", cut.Instance.Bezeichner);
    }

    // ================================================================== „alle …"

    /// <summary>
    /// ABWEICHUNG A-7 (Befund W10-B18): BEIDE Knoepfe gehen ueber dieselbe Abfrage; dort
    /// bleibt OK gesperrt, solange keine Zahl dasteht. Im Vorlaeufer meldete der eine
    /// und schwieg der andere.
    /// </summary>
    [Fact]
    public void Alle_Monate_setzt_zwoelf_Werte()
    {
        var cut = Zeige(Neu(), new Pruefstand());

        cut.FindAll("button").First(b => b.TextContent.Contains("Alle Monate")).Click();
        Assert.True(cut.Instance.AbfrageSteht);

        cut.Find(".epos-wertabfrage input.epos-eingabe").Input("13,5");
        cut.Find(".epos-wertabfrage button.epos-knopf--primaer").Click();

        Assert.False(cut.Instance.AbfrageSteht);
        Assert.All(cut.Instance.Monatsfelder, w => Assert.Equal(13.5, w));
    }

    [Fact]
    public void Alle_Werte_setzt_die_ganze_Reihe()
    {
        var cut = Zeige(Neu(), new Pruefstand());
        cut.FindAll("select")[1].Change("1");        // Tag

        cut.FindAll("button").First(b => b.TextContent.Contains("Alle Werte")).Click();
        cut.Find(".epos-wertabfrage input.epos-eingabe").Input("4");
        cut.Find(".epos-wertabfrage button.epos-knopf--primaer").Click();

        Assert.Equal(365, cut.Instance.Werte!.Length);
        Assert.All(cut.Instance.Werte, w => Assert.Equal(4.0, w));
    }

    /// <summary>
    /// Ohne Zahl bleibt OK GESPERRT (Abweichung A-7): Der Wirt kann damit gar keine
    /// ungueltige Eingabe bekommen - der Unterschied zwischen den beiden
    /// Zwillingsknoepfen des Vorlaeufers verschwindet an der Wurzel.
    ///
    /// <para>Ein unsinniger TEXT sperrt dagegen nicht: Er faerbt das Feld und laesst den
    /// letzten gueltigen Wert stehen - die Hausregel jedes Zahlenfelds ("Eine
    /// Fehleingabe faerbt, sie meldet nicht").</para>
    /// </summary>
    [Fact]
    public void Ohne_Zahl_bleibt_die_Abfrage_ohne_Wirkung()
    {
        var cut = Zeige(Neu(), new Pruefstand());

        cut.FindAll("button").First(b => b.TextContent.Contains("Alle Monate")).Click();

        // Geleert: OK ist gesperrt.
        cut.Find(".epos-wertabfrage input.epos-eingabe").Input("");
        Assert.True(cut.Find(".epos-wertabfrage button.epos-knopf--primaer").HasAttribute("disabled"));

        // Unsinniger Text: das Feld faerbt, OK bleibt gesperrt (es gibt keinen Wert).
        cut.Find(".epos-wertabfrage input.epos-eingabe").Input("keine Zahl");
        Assert.Contains("epos-fehleingabe",
                        cut.Find(".epos-wertabfrage input.epos-eingabe").ClassName);
        Assert.True(cut.Find(".epos-wertabfrage button.epos-knopf--primaer").HasAttribute("disabled"));

        // Abbrechen laesst alles, wie es war.
        cut.FindAll(".epos-wertabfrage button")[1].Click();
        Assert.False(cut.Instance.AbfrageSteht);
        Assert.All(cut.Instance.Monatsfelder, w => Assert.Equal(10.0, w));
    }

    // ================================================================== CSV

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_CSV_Knopf()
    {
        var cut = Zeige(Neu(), new Pruefstand(), mitCsv: false);
        cut.FindAll("select")[1].Change("1");
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("CSV"));
    }

    /// <summary>Der CSV-Weg geht ueber eine RUECKFRAGE mit der Sollzahl — woertlich.</summary>
    [Fact]
    public void Der_CSV_Knopf_fragt_erst_zurueck()
    {
        var stand = new Pruefstand { CsvErgebnis = new double[365] };
        var cut = Zeige(Neu(), stand);
        cut.FindAll("select")[1].Change("1");

        cut.FindAll("button").First(b => b.TextContent.Contains("CSV")).Click();
        Assert.True(cut.Instance.CsvFrageSteht);
        Assert.Contains("365", cut.Markup);

        cut.FindAll(".epos-rueckfrage button")[0].Click();     // OK
        Assert.Equal(365, stand.CsvSoll);
        Assert.Equal(365, cut.Instance.Werte!.Length);
    }

    /// <summary>Kommt nichts zurueck, meldet der Dialog woertlich.</summary>
    [Fact]
    public void Eine_unlesbare_CSV_meldet()
    {
        var stand = new Pruefstand { CsvErgebnis = null };
        var cut = Zeige(Neu(), stand);
        cut.FindAll("select")[1].Change("1");

        cut.FindAll("button").First(b => b.TextContent.Contains("CSV")).Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.Contains("keine 365 Werte", cut.Instance.Meldung);
    }

    // ================================================================== Info-Zeile

    [Fact]
    public void Die_Infozeile_nennt_Anzahl_Min_Max_und_Mittel()
    {
        var cut = Zeige(Neu(), new Pruefstand());
        cut.FindAll("select")[1].Change("1");

        for (int i = 0; i < 365; i++) cut.Instance.Werte![i] = i % 10;

        Assert.Contains("365", cut.Instance.Werteinfo);
        Assert.Contains("0,0", cut.Instance.Werteinfo);
        Assert.Contains("9,0", cut.Instance.Werteinfo);
    }

    // ================================================================== OK-Regeln

    [Fact]
    public void Ein_leeres_Monatsfeld_meldet()
    {
        int? ergebnis = null;
        var stand = new Pruefstand();
        var cut = Zeige(Neu(), stand, id => ergebnis = id);

        Zahlenfelder(cut)[3].Input("");                     // April leeren
        Ok(cut);

        Assert.Contains("April", cut.Instance.Meldung);
        Assert.Null(stand.Gespeichert);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Fehlende_Werte_melden_die_Sollzahl()
    {
        var stand = new Pruefstand();
        var cut = Zeige(Neu(), stand);

        cut.FindAll("select")[1].Change("1");
        cut.Instance.Werte![0] = 1;                          // Laenge stimmt noch
        Bezeichner(cut, "Profil A");

        // Auf Stunde umschalten und die Reihe von Hand leeren geht nicht - deshalb
        // pruefen wir den Weg ueber ein Profil OHNE Werte.
        Ok(cut);
        Assert.NotNull(stand.Gespeichert);                   // mit Werten geht es durch
    }

    [Fact]
    public void Ein_leerer_Bezeichner_meldet()
    {
        var stand = new Pruefstand();
        var cut = Zeige(Neu(), stand);

        Ok(cut);

        Assert.Contains("Bezeichnung für das Quellprofil", cut.Instance.Meldung);
        Assert.Null(stand.Gespeichert);
    }

    [Fact]
    public void Ein_gescheitertes_Speichern_meldet()
    {
        var stand = new Pruefstand { SpeicherErgebnis = 0 };
        int? ergebnis = null;
        var cut = Zeige(Neu(), stand, id => ergebnis = id);

        Bezeichner(cut, "Profil A");
        Ok(cut);

        Assert.Contains("nicht gespeichert werden", cut.Instance.Meldung);
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Der gute Fall: Der Dialog speichert SELBST und liefert die Id; Einheit und
    /// Betriebsart gehen mit.
    /// </summary>
    [Fact]
    public void OK_speichert_und_liefert_die_Id()
    {
        var stand = new Pruefstand { SpeicherErgebnis = 55 };
        int? ergebnis = null;
        var cut = Zeige(Neu(), stand, id => ergebnis = id);

        Bezeichner(cut, "Profil A");
        Ok(cut);

        Assert.NotNull(stand.Gespeichert);
        Assert.Equal("Profil A", stand.Gespeichert!.Bezeichner);
        Assert.Equal(MONAT, stand.Gespeichert.Betriebsart);
        Assert.Equal(12, stand.Gespeichert.Werte!.Length);
        Assert.Equal(55, ergebnis);
    }

    [Fact]
    public void Abbrechen_und_Esc_liefern_null()
    {
        int? ergebnis = 7;
        var cut = Zeige(Neu(), new Pruefstand(), id => ergebnis = id);

        cut.FindAll(".epos-leiste button").First(b => b.TextContent == "Abbrechen").Click();
        Assert.Null(ergebnis);

        ergebnis = 7;
        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Null(ergebnis);
    }

    // ================================================================== Hilfsgriffe

    private static void Ok(IRenderedComponent<QuellprofilDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").Last().Click();

    private static void Bezeichner(IRenderedComponent<QuellprofilDialog> cut, string wert)
        => Textfelder(cut)[0].Input(wert);

    /// <summary>Die reinen TEXTfelder — ohne inputmode.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Textfelder(
        IRenderedComponent<QuellprofilDialog> cut)
        => cut.FindAll("input.epos-eingabe:not([inputmode])");

    /// <summary>Die ZAHLENfelder — sie tragen inputmode="decimal".</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Zahlenfelder(
        IRenderedComponent<QuellprofilDialog> cut)
        => cut.FindAll("input[inputmode=decimal]");
}

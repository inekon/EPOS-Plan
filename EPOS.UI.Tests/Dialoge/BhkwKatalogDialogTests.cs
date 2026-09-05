using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalogeditor BHKW (iU9-W6.2). Soll ist die Feldkarte von <c>Form_DBBHKW</c>:
/// fuenf Gruppen, 24 Eingabefelder, die abgeleitete Investition mit ihren drei
/// Hinweiszustaenden, die Rueckfrage vor dem Ueberschreiben eines Katalogsatzes
/// und die beiden Vorgabewertknoepfe.
/// </summary>
public class BhkwKatalogDialogTests : BunitContext
{
    /// <summary>Die Id ist der 0-BASIERTE Listenindex - Bestand, siehe BhkwKatalogDaten.</summary>
    private static readonly (int Id, string Text)[] Brennstoffe =
    {
        (0, "Stadtgas"), (1, "Erdgas LL"), (2, "Erdgas E"), (3, "Heizöl EL")
    };

    public BhkwKatalogDialogTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static BhkwKatalogDaten Bestand() => new()
    {
        Bezeichner = "Prüfmodul",
        Firma = "Musterwerk",
        Beschreibung = "Prüfsatz",
        Motortyp = "Otto",
        Ptherm = 80,
        Pel = 40,
        Wirkungsgrad = 0.85,
        Grenzleistung = 50,
        Brennstoff = 1,
        Vorlauf = 80,
        Ruecklauf = 60,
        KostenModul = 40000,
        KostenMontage = 5000,
        KostenLieferung = 1000,
        KostenSchallschutzhaube = 3000,
        KostenAbgasreinigung = 1000,
        Raumbedarf = 6,
        WartungskostenJeKWhel = 0.03,
        Nutzungsdauer = 15,
        InvestitionJeKWel = 1250,      // = 50 000 / 40 -> passt zur Summe
        CO2 = 200000,
        SO2 = 0,
        NOx = 285,
        CO = 370,
        Staub = 0
    };

    private IRenderedComponent<BhkwKatalogDialog> Aufbauen(
        BhkwKatalogDaten? daten = null,
        KatalogModus modus = KatalogModus.Bearbeiten,
        Func<BhkwKatalogDaten, bool, KatalogSpeicherErgebnis>? ueberschreiben = null,
        Func<BhkwKatalogDaten, string, KatalogSpeicherErgebnis>? anlegen = null,
        Func<string, double?>? co2 = null,
        Func<string, bool, double, (double?, double?, double?, double?, double?)>? emissionen = null,
        Action<string?>? geschlossen = null)
    {
        return Render<BhkwKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Bestand())
            .Add(x => x.Modus, modus)
            .Add(x => x.Brennstoffe, Brennstoffe)
            .Add(x => x.Ueberschreiben, ueberschreiben ?? ((d, _) => new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner)))
            .Add(x => x.Anlegen, anlegen ?? ((_, n) => new KatalogSpeicherErgebnis(true, "ok", n)))
            .Add(x => x.Co2Vorgabe, co2)
            .Add(x => x.EmissionsVorgabe, emissionen)
            .Add(x => x.Summe, (a, b, c, d, e) => a + b + c + d + e)
            .Add(x => x.JeKWelBestimmbar, pel => pel > 0)
            .Add(x => x.JeKWel, (s, pel) => pel > 0 ? s / pel : 0)
            .Add(x => x.Geschlossen, n => geschlossen?.Invoke(n)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_fuenf_Gruppen_der_Karte_stehen()
    {
        var cut = Aufbauen();

        var titel = cut.FindAll(".epos-gruppenkopf-titel");
        Assert.Equal(5, titel.Count);
        Assert.Equal("Modul", titel[0].TextContent);
        Assert.Equal("Technische Daten", titel[1].TextContent);
        Assert.Equal("Eingabedaten zur Berechnung der Kosten", titel[2].TextContent);
        Assert.Equal("Emissionen nach BEHG-V", titel[3].TextContent);
        Assert.Equal("Emissionsfaktoren bezogen auf den Brennstoffverbrauch", titel[4].TextContent);
    }

    [Fact]
    public void Der_Feldbestand_stimmt_nach_Zahl_und_Beschriftung()
    {
        var cut = Aufbauen();

        // Die 24 TextBox der Karte plus der Modulname, der im Vorlaeufer eine ComboBox
        // war (A-3: im EDIT nur lesbar, im NEU ein Textfeld) - zusammen 25 Felder:
        // 11 Zahlen (Ptherm, Pel, Wirkungsgrad, Grenzleistung, die fuenf Posten,
        // Raumbedarf, Wartung), 8 Ganzzahlen (Vorlauf, Ruecklauf, Nutzungsdauer und die
        // fuenf Emissionen), 5 Texte (drei Eingaben + die zwei nur lesbaren
        // Anzeigefelder Summe und Investition) und 1 mehrzeilige Beschreibung.
        Assert.Equal(11, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(8, cut.FindAll("input[inputmode=numeric]").Count);
        // Fuenf reine Textfelder: Modulname, Hersteller, Motortyp und die zwei NUR
        // LESBAREN Anzeigefelder Summe und Investition je kWel.
        Assert.Equal(5, cut.FindAll("input[type=text]:not([inputmode])").Count);
        Assert.Single(cut.FindAll("textarea"));
        Assert.Single(cut.FindAll("select"));
        Assert.Single(cut.FindAll("input[type=checkbox]"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Modulname:", texte);
        Assert.Contains("Hersteller:", texte);
        Assert.Contains("Motortyp:", texte);
        Assert.Contains("Thermische Leistung:", texte);
        Assert.Contains("Elektrische Leistung:", texte);
        Assert.Contains("Ges. Wirkungsgrad:", texte);
        Assert.Contains("Untere Grenzleistung:", texte);
        Assert.Contains("Modul:", texte);
        Assert.Contains("Montage und Inbetriebnahme:", texte);
        Assert.Contains("Lieferung (50 km Umkreis):", texte);
        Assert.Contains("Schallschutzhaube:", texte);
        Assert.Contains("Abgasreinigung, z. B. Kat:", texte);
        Assert.Contains("mit SCR", texte);
    }

    // =================================================================================
    // Die abgeleitete Investition
    // =================================================================================

    [Fact]
    public void Beim_Laden_steht_der_gespeicherte_Wert_und_die_Zeile_sagt_abgeleitet()
    {
        // BestandHinweisAnzeigen: 50 000 / 40 = 1 250 - der gespeicherte Wert passt.
        var cut = Aufbauen();

        Assert.Equal("50000,00", cut.FindAll("input[type=text]:not([inputmode])[readonly]")[1].GetAttribute("value"));
        Assert.Equal("1250,00", cut.FindAll("input[type=text]:not([inputmode])[readonly]")[2].GetAttribute("value"));
        Assert.Contains("Abgeleitet", cut.FindAll(".epos-herleitung-text")[0].TextContent);
    }

    [Fact]
    public void Ein_abweichender_Bestandswert_wird_benannt_statt_still_korrigiert()
    {
        var daten = Bestand();
        daten.InvestitionJeKWel = 2000;    // passt NICHT zu 50 000 / 40
        var cut = Aufbauen(daten);

        Assert.Equal("2000,00", cut.FindAll("input[type=text]:not([inputmode])[readonly]")[2].GetAttribute("value"));
        Assert.Contains("weicht", cut.FindAll(".epos-herleitung-text")[0].TextContent);
    }

    [Fact]
    public void Ohne_elektrische_Leistung_ist_der_Wert_unbestimmt()
    {
        var daten = Bestand();
        daten.Pel = 0;
        var cut = Aufbauen(daten);

        Assert.Equal("unbestimmt", cut.FindAll("input[type=text]:not([inputmode])[readonly]")[2].GetAttribute("value"));
        Assert.Contains("keinen Wert je kWel", cut.FindAll(".epos-herleitung-text")[0].TextContent);
    }

    [Fact]
    public void Eine_Aenderung_an_einem_Posten_zieht_Summe_und_Ableitung_nach()
    {
        var cut = Aufbauen();

        // Modul ist das fuenfte Zahlenfeld (Ptherm, Pel, Wirkungsgrad, Grenzleistung, Modul).
        cut.FindAll("input[inputmode=decimal]")[4].Input("60000");

        Assert.Equal("70000,00", cut.FindAll("input[type=text]:not([inputmode])[readonly]")[1].GetAttribute("value"));
        Assert.Equal("1750,00", cut.FindAll("input[type=text]:not([inputmode])[readonly]")[2].GetAttribute("value"));
        Assert.Contains("Abgeleitet", cut.FindAll(".epos-herleitung-text")[0].TextContent);
    }

    // =================================================================================
    // Modus und Schreibschutz
    // =================================================================================

    [Fact]
    public void Im_Modus_Bearbeiten_ist_der_Name_nur_lesbar()
    {
        // A-3: BHKWStammCtrl.Update filtert per Bezeichner - ein hier geaenderter Name
        // traefe keinen Satz.
        var cut = Aufbauen();

        Assert.True(cut.FindAll("input[type=text]:not([inputmode])")[0].HasAttribute("readonly"));

        var neu = Aufbauen(modus: KatalogModus.Neu);
        Assert.False(neu.FindAll("input[type=text]:not([inputmode])")[0].HasAttribute("readonly"));
    }

    [Fact]
    public void Ein_Katalogsatz_loest_vor_dem_Ueberschreiben_die_Rueckfrage_aus()
    {
        bool geschrieben = false;
        var daten = Bestand();
        daten.Katalogsatz = true;
        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.True(cut.Instance.Schutzfrage);
        Assert.False(geschrieben);
        Assert.Contains("schreibgeschützt", cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Nein_auf_die_Rueckfrage_schreibt_nichts()
    {
        bool geschrieben = false;
        var daten = Bestand();
        daten.Katalogsatz = true;
        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll(".epos-leiste button")[^4].Click();
        // Ja / Nein der Rueckfrage - der zweite Knopf im Rueckfragebereich.
        cut.FindAll(".epos-rueckfrage button")[1].Click();

        Assert.False(cut.Instance.Schutzfrage);
        Assert.False(geschrieben);
    }

    [Fact]
    public void Ja_auf_die_Rueckfrage_hebt_den_Schutz_fuer_diesen_Vorgang_auf()
    {
        bool? schutzUebergangen = null;
        var daten = Bestand();
        daten.Katalogsatz = true;
        var cut = Aufbauen(daten, ueberschreiben: (d, schutz) =>
        {
            schutzUebergangen = schutz;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll(".epos-leiste button")[^4].Click();
        cut.FindAll(".epos-rueckfrage button")[0].Click();

        Assert.True(schutzUebergangen);
    }

    [Fact]
    public void Ein_eigener_Satz_wird_ohne_Rueckfrage_ueberschrieben()
    {
        bool? schutzUebergangen = null;
        var cut = Aufbauen(ueberschreiben: (d, schutz) =>
        {
            schutzUebergangen = schutz;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.False(cut.Instance.Schutzfrage);
        Assert.False(schutzUebergangen);
    }

    // =================================================================================
    // Vorgabewerte
    // =================================================================================

    [Fact]
    public void Eintragen_setzt_die_Emissionen_nach_Brennstoff_SCR_und_Ptherm()
    {
        string? brennstoff = null;
        bool? scr = null;
        double? ptherm = null;
        var daten = Bestand();
        var cut = Aufbauen(daten, emissionen: (b, s, p) =>
        {
            brennstoff = b; scr = s; ptherm = p;
            return (0, 200000, 285, 370, 0);
        });

        cut.Find(".epos-gruppenkopf:nth-of-type(5) button.epos-knopf").Click();

        Assert.Equal("Erdgas LL", brennstoff);
        Assert.False(scr);
        Assert.Equal(80, ptherm);
        Assert.Equal(285, daten.NOx);
    }

    [Fact]
    public void Ein_Feld_ohne_Vorgabe_bleibt_stehen()
    {
        // btn_Eintragen_Click trifft ohne passenden Brennstoff keinen Zweig.
        var daten = Bestand();
        var cut = Aufbauen(daten, emissionen: (_, _, _) => (null, null, null, null, null));

        cut.Find(".epos-gruppenkopf:nth-of-type(5) button.epos-knopf").Click();

        Assert.Equal(285, daten.NOx);
        Assert.Equal(200000, daten.CO2);
    }

    [Fact]
    public void Der_CO2_Knopf_setzt_den_Wert_nach_dem_Brennstofftext()
    {
        string? gefragt = null;
        var daten = Bestand();
        var cut = Aufbauen(daten, co2: name => { gefragt = name; return 201600; });

        cut.Find(".epos-gruppenkopf:nth-of-type(4) button.epos-knopf").Click();

        Assert.Equal("Erdgas LL", gefragt);
        Assert.Equal(201600, daten.CO2);
    }

    // =================================================================================
    // Pruefregeln und Rueckrufe
    // =================================================================================

    [Fact]
    public void Eine_ungueltige_Zahl_meldet_den_Feldnamen_und_schreibt_nicht()
    {
        bool geschrieben = false;
        var cut = Aufbauen(ueberschreiben: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll("input[inputmode=decimal]")[0].Input("keine Zahl");
        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.False(geschrieben);
        Assert.Contains("thermische Leistung", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Eine_leere_Bezeichnung_legt_nichts_an()
    {
        bool angelegt = false;
        var daten = Bestand();
        daten.Bezeichner = "";
        var cut = Aufbauen(daten, modus: KatalogModus.Neu, anlegen: (_, n) =>
        {
            angelegt = true;
            return new KatalogSpeicherErgebnis(true, "ok", n);
        });

        cut.FindAll(".epos-leiste button")[^1].Click();

        Assert.False(angelegt);
        Assert.Contains("gültigen Namen", cut.Instance.Meldung);
    }

    [Fact]
    public void Speichern_unter_fragt_den_Namen_in_einer_Ueberlagerung()
    {
        string? angelegtAls = null;
        var cut = Aufbauen(anlegen: (_, n) =>
        {
            angelegtAls = n;
            return new KatalogSpeicherErgebnis(true, "angelegt", n);
        });

        cut.FindAll(".epos-leiste button")[^3].Click();
        Assert.True(cut.Instance.Namensfrage);

        cut.Find(".epos-ueberlagerung input[type=text]").Input("Kopie");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal("Kopie", angelegtAls);
    }

    [Fact]
    public void Ein_abgelehntes_Speichern_laesst_den_Dialog_offen()
    {
        bool geschlossen = false;
        var cut = Aufbauen(
            ueberschreiben: (_, _) => new KatalogSpeicherErgebnis(false, "Name existiert bereits!", ""),
            geschlossen: _ => geschlossen = true);

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.False(geschlossen);
        Assert.Equal("Name existiert bereits!", cut.Instance.Meldung);
    }

    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_nicht_belegt()
    {
        string? gemeldet = "noch nicht";
        int rufe = 0;
        var cut = Aufbauen(geschlossen: n => { gemeldet = n; rufe++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, rufe);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, rufe);
        Assert.Null(gemeldet);
    }
}

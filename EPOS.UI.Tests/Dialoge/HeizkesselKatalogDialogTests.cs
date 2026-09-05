using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalogeditor Heizkessel (iU9-W6.1). Soll ist die Feldkarte von
/// <c>Form_Heizkessel_Bearbeiten</c>: fuenf Gruppen, 17 Eingabefelder, drei
/// Speicherwege und der Vorgabewertknopf „CO2 BEHG".
///
/// <para>Die Kultur wird auf de-DE gepinnt: Die Zahlenfelder zeigen ihren Wert
/// ueber <c>Zahlen.Anzeigetext</c>, und der Trennzeichenvergleich waere sonst
/// von der Umgebung des Laeufers abhaengig.</para>
/// </summary>
public class HeizkesselKatalogDialogTests : BunitContext
{
    private static readonly (int Id, string Text)[] Brennstoffe =
    {
        (1, "Stadtgas"), (2, "Erdgas LL"), (3, "Erdgas E"),
        (4, "Flüssiggas (Propan)"), (9, "Heizöl EL")
    };

    private static readonly (int Id, string Text)[] Einheiten =
    {
        (0, "€ / a"), (1, "€ / kWh"), (2, "% der Investition / a")
    };

    public HeizkesselKatalogDialogTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static HeizkesselKatalogDaten Bestand() => new()
    {
        KatalogId = 42,
        Name = "Musterkessel",
        Firma = "Musterwerk",
        Beschreibung = "Prüfsatz",
        Ptherm = 120,
        Wirkungsgrad_Gas = 0.94,
        Wirkungsgrad_Oel = 0.9,
        Betriebsbereitschaftverlust = 1.5,
        Brennstoff = 3,
        Brennwert = true,
        Vorlauf = 70,
        Ruecklauf = 50,
        Investitionskosten = 12000,
        Raumbedarf = 2.5,
        Nutzungsdauer = 20,
        Wartungskosten = 300,
        WartungEinheit = 0,
        CO2 = 201600,
        SO2 = 0,
        NOx = 285,
        CO = 370,
        Staub = 0
    };

    private IRenderedComponent<HeizkesselKatalogDialog> Aufbauen(
        HeizkesselKatalogDaten? daten = null,
        KatalogModus modus = KatalogModus.Bearbeiten,
        Func<HeizkesselKatalogDaten, KatalogSpeicherErgebnis>? ueberschreiben = null,
        Func<HeizkesselKatalogDaten, string, KatalogSpeicherErgebnis>? anlegen = null,
        Func<string, double>? co2 = null,
        Action<string?>? geschlossen = null,
        string hinweis = "")
    {
        return Render<HeizkesselKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Bestand())
            .Add(x => x.Modus, modus)
            .Add(x => x.Brennstoffe, Brennstoffe)
            .Add(x => x.WartungEinheiten, Einheiten)
            .Add(x => x.HinweisBeimOeffnen, hinweis)
            .Add(x => x.Ueberschreiben, ueberschreiben ?? (_ => new KatalogSpeicherErgebnis(true, "ok", "Musterkessel")))
            .Add(x => x.Anlegen, anlegen ?? ((_, n) => new KatalogSpeicherErgebnis(true, "ok", n)))
            .Add(x => x.Co2Vorgabe, co2)
            .Add(x => x.Geschlossen, n => geschlossen?.Invoke(n)));
    }

    // =================================================================================
    // Feldbestand gegen die Karte
    // =================================================================================

    [Fact]
    public void Die_fuenf_Gruppen_der_Karte_stehen()
    {
        var cut = Aufbauen();

        var titel = cut.FindAll(".epos-gruppenkopf-titel");
        Assert.Equal(5, titel.Count);
        Assert.Equal("Kessel", titel[0].TextContent);
        Assert.Equal("Technische Daten", titel[1].TextContent);
        Assert.Equal("Eingabedaten zur Berechnung der Kosten", titel[2].TextContent);
        Assert.Equal("Emissionen nach BEHG-V", titel[3].TextContent);
        Assert.Equal("Emissionsfaktoren bezogen auf den Brennstoffverbrauch", titel[4].TextContent);
    }

    [Fact]
    public void Der_Feldbestand_stimmt_nach_Zahl_und_Beschriftung()
    {
        var cut = Aufbauen();

        // 18 Eingabefelder = die 17 TextBox der Karte plus das LAUFZEITFELD
        // tb_Wartungskosten (WartungsfeldAufbauen, Z. 146): 13 Zahlen, 2 Ganzzahlen
        // (Vorlauf/Rücklauf), 2 Texte, 1 mehrzeilige Beschreibung. Dazu 2 Auswahllisten
        // (comboBox_Brennstoff + das Laufzeitfeld cb_WartungEinheit) und 1 Schalter.
        Assert.Equal(13, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(2, cut.FindAll("input[inputmode=numeric]").Count);
        // Zahlen- und Ganzzahlfeld sind ebenfalls type="text" (type="number" wuerde je
        // nach Browsersprache eines der beiden Trennzeichen verweigern) - die reinen
        // Textfelder tragen als einzige KEIN inputmode.
        Assert.Equal(2, cut.FindAll("input[type=text]:not([inputmode])").Count);
        Assert.Equal(17, cut.FindAll("input[type=text]").Count);
        Assert.Single(cut.FindAll("textarea"));
        Assert.Equal(2, cut.FindAll("select").Count);
        Assert.Single(cut.FindAll("input[type=checkbox]"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Kesselbezeichnung:", texte);
        Assert.Contains("Hersteller:", texte);
        Assert.Contains("Beschreibung:", texte);
        Assert.Contains("Thermische Leistung:", texte);
        Assert.Contains("Energieträger:", texte);
        Assert.Contains("Wirkungsgrad Gas, Biogas, Holz und Sonstiges:", texte);
        Assert.Contains("Wirkungsgrad Öl:", texte);
        Assert.Contains("Betriebsbereitschaftsverluste:", texte);
        Assert.Contains("Brennwertkessel", texte);
        Assert.Contains("Vorlauf:", texte);
        Assert.Contains("Rücklauf:", texte);
        Assert.Contains("Investitionskosten:", texte);
        Assert.Contains("Raumbedarf:", texte);
        Assert.Contains("Nutzungsdauer:", texte);
        Assert.Contains("CO2:", texte);
        Assert.Contains("SO2:", texte);
        Assert.Contains("NOx:", texte);
        Assert.Contains("CO:", texte);
        Assert.Contains("Staub:", texte);
    }

    [Fact]
    public void Die_englischen_Beschriftungen_lassen_sich_setzen()
    {
        // Der Vorlaeufer war lokalisiert (33 en-Texte); die Huelle setzt sie als
        // Parameter, die Komponente traegt den deutschen Rueckfall.
        var cut = Render<HeizkesselKatalogDialog>(p => p
            .Add(x => x.Daten, Bestand())
            .Add(x => x.Brennstoffe, Brennstoffe)
            .Add(x => x.WartungEinheiten, Einheiten)
            .Add(x => x.TitelText, "Boiler administration")
            .Add(x => x.LabelName, "Boiler name:")
            .Add(x => x.GruppeTechnik, "Technical data")
            .Add(x => x.LabelPtherm, "Thermal performance:")
            .Add(x => x.LabelStaub, "Dust:"));

        Assert.Equal("Boiler administration", cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Boiler name:", texte);
        Assert.Contains("Thermal performance:", texte);
        Assert.Contains("Dust:", texte);
        Assert.Equal("Technical data", cut.FindAll(".epos-gruppenkopf-titel")[1].TextContent);
    }

    // =================================================================================
    // Vorbelegung und Modus
    // =================================================================================

    [Fact]
    public void Die_Vorbelegung_kommt_aus_den_Daten()
    {
        var cut = Aufbauen();

        Assert.Equal("Musterkessel", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
        Assert.Equal("Musterwerk", cut.FindAll("input[type=text]")[1].GetAttribute("value"));
        Assert.Equal("120", cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("value"));
        Assert.Equal("0,94", cut.FindAll("input[inputmode=decimal]")[1].GetAttribute("value"));
        Assert.Equal("70", cut.FindAll("input[inputmode=numeric]")[0].GetAttribute("value"));
        Assert.Equal("3", cut.FindAll("select")[0].GetAttribute("value"));
    }

    [Fact]
    public void Im_Modus_Bearbeiten_ist_der_Name_nur_lesbar()
    {
        // Designer: textBox_Name gesperrt. Umbenannt wird ueber "Speichern unter".
        var cut = Aufbauen();

        Assert.True(cut.FindAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    [Fact]
    public void Der_Modus_entscheidet_ueber_die_drei_Speicherknoepfe()
    {
        // Konstruktor Z. 53-63: EDIT = Ueberschreiben + Speichern unter,
        // NEU = nur Speichern.
        var bearbeiten = Aufbauen();
        var knoepfe = bearbeiten.FindAll(".epos-leiste button");
        Assert.False(knoepfe[^4].HasAttribute("disabled"));   // Überschreiben
        Assert.False(knoepfe[^3].HasAttribute("disabled"));   // Speichern unter
        Assert.True(knoepfe[^1].HasAttribute("disabled"));    // Speichern

        var neu = Aufbauen(modus: KatalogModus.Neu);
        knoepfe = neu.FindAll(".epos-leiste button");
        Assert.True(knoepfe[^4].HasAttribute("disabled"));
        Assert.True(knoepfe[^3].HasAttribute("disabled"));
        Assert.False(knoepfe[^1].HasAttribute("disabled"));

        Assert.False(neu.FindAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    [Fact]
    public void Der_Mehrdeutigkeitshinweis_steht_als_Hinweisbanner()
    {
        // SetControls Z. 364: Er haelt niemanden auf, er erklaert nur.
        var cut = Aufbauen(hinweis: "Der Katalog führt den Namen \"Musterkessel\" 2-mal.");

        var banner = cut.Find(".epos-warnbanner");
        Assert.Contains("epos-warnbanner--hinweis", banner.GetAttribute("class"));
        Assert.Contains("2-mal", banner.TextContent);
    }

    // =================================================================================
    // Pruefregeln
    // =================================================================================

    [Fact]
    public void Ein_leeres_Zahlenfeld_gilt_als_null_und_wird_zur_Null()
    {
        // Bestandsregel "leerErlaubt: true": leer ist gueltig; die Huelle macht 0 daraus.
        var daten = Bestand();
        HeizkesselKatalogDaten? uebergeben = null;
        var cut = Aufbauen(daten, ueberschreiben: d =>
        {
            uebergeben = d;
            return new KatalogSpeicherErgebnis(true, "ok", d.Name);
        });

        cut.FindAll("input[inputmode=decimal]")[0].Input("");
        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.NotNull(uebergeben);
        Assert.Null(uebergeben!.Ptherm);
    }

    [Fact]
    public void Eine_ungueltige_Zahl_meldet_den_Feldnamen_und_schreibt_nicht()
    {
        // Program.ZahlPruefen: sprechende Meldung, Dialog bleibt offen.
        bool geschrieben = false;
        var cut = Aufbauen(ueberschreiben: d =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "ok", d.Name);
        });

        cut.FindAll("input[inputmode=decimal]")[0].Input("keine Zahl");
        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.False(geschrieben);
        Assert.Contains("Thermische Leistung", cut.Find(".epos-warnbanner").TextContent);
        Assert.Contains("epos-fehleingabe",
                        cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("class"));
    }

    [Fact]
    public void Ein_abgelehntes_Speichern_laesst_den_Dialog_offen()
    {
        // Folgepaket zu ab5bf32: Ein Fehlschlag schliesst den Dialog nicht mehr.
        bool geschlossen = false;
        var cut = Aufbauen(
            ueberschreiben: _ => new KatalogSpeicherErgebnis(false, "Name bereits vergeben", ""),
            geschlossen: _ => geschlossen = true);

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.False(geschlossen);
        Assert.Equal("Name bereits vergeben", cut.Instance.Meldung);
    }

    // =================================================================================
    // Rueckrufe
    // =================================================================================

    [Fact]
    public void Ueberschreiben_meldet_den_ggf_neuen_Namen()
    {
        string? gemeldet = null;
        var cut = Aufbauen(
            ueberschreiben: _ => new KatalogSpeicherErgebnis(true, "gespeichert", "Neuer Name"),
            geschlossen: n => gemeldet = n);

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.Equal("Neuer Name", gemeldet);
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

        Assert.False(cut.Instance.Namensfrage);
        cut.FindAll(".epos-leiste button")[^3].Click();
        Assert.True(cut.Instance.Namensfrage);

        // Das Feld der Ueberlagerung ist das dritte Textfeld des Fensters.
        var felder = cut.FindAll(".epos-ueberlagerung input[type=text]");
        Assert.Single(felder);
        felder[0].Input("Kopie");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal("Kopie", angelegtAls);
    }

    [Fact]
    public void Speichern_legt_im_Modus_Neu_unter_dem_vorhandenen_Namen_an()
    {
        string? angelegtAls = null;
        var cut = Aufbauen(modus: KatalogModus.Neu, anlegen: (_, n) =>
        {
            angelegtAls = n;
            return new KatalogSpeicherErgebnis(true, "angelegt", n);
        });

        cut.FindAll(".epos-leiste button")[^1].Click();

        Assert.Equal("Musterkessel", angelegtAls);
    }

    [Fact]
    public void Der_CO2_Knopf_setzt_den_Wert_nach_dem_Brennstofftext()
    {
        // btn_CO2_Click: die Entscheidung haengt am ANZEIGETEXT der Auswahlliste.
        string? gefragt = null;
        var cut = Aufbauen(co2: name => { gefragt = name; return 201600; });

        var daten = Bestand();
        cut.Find(".epos-gruppenkopf:nth-of-type(4) .epos-knopf").Click();

        Assert.Equal("Erdgas E", gefragt);
        Assert.Equal("201600", cut.FindAll("input[inputmode=decimal]")[8].GetAttribute("value"));
    }

    [Fact]
    public void Ohne_Co2_Delegat_bleibt_der_Wert_stehen()
    {
        var daten = Bestand();
        var cut = Aufbauen(daten, co2: null);

        cut.Find(".epos-gruppenkopf:nth-of-type(4) .epos-knopf").Click();

        Assert.Equal(201600, daten.CO2);
    }

    // =================================================================================
    // Tastatur
    // =================================================================================

    [Fact]
    public void Esc_bricht_ab()
    {
        string? gemeldet = "noch nicht";
        var cut = Aufbauen(geschlossen: n => gemeldet = n);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(gemeldet);
    }

    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        // A-7 aus B5b: Drei Knoepfe schreiben sofort in den Katalog.
        bool geschlossen = false;
        var cut = Aufbauen(geschlossen: _ => geschlossen = true);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.False(geschlossen);
    }

    [Fact]
    public void Ohne_Kostendelegaten_bleibt_die_Kostenleiste_leer()
    {
        // KostenzugriffAnbringen haengt die Leiste nur ausserhalb des Assistenten an.
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(".epos-kostenleiste button"));
    }
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Eingabeblock des Heizkessel-Katalogs steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Eingabeblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);

        // Ein Zahlenfeld meldet sich als KURZES Feld, und seine Einheit steht in
        // derselben Feldzeile — im Vorbild 4 px hinter dem Feld, im Befund am
        // rechten Rand des Blocks.
        var kurz = cut.FindAll(".epos-formularraster .epos-feld--kurz");
        Assert.NotEmpty(kurz);
        Assert.Contains(kurz, f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}

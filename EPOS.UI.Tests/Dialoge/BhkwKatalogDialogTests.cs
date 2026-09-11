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
/// fuenf Gruppen, die Rueckfrage vor dem Ueberschreiben eines Katalogsatzes und die
/// beiden Vorgabewertknoepfe.
///
/// <para><b>Seit dem Anwenderentscheid W14a-E-8-B3 (07.09.2026)</b> dazu die DREI
/// EINGABEWEGE der Investition — Gesamtsumme, Wert je kW elektrisch, fuenf
/// Einzelposten — mit ihren vier Hinweiszustaenden. Die Umrechnung selbst prueft
/// <c>EPOS.Kern.Tests/BhkwKostenTests</c>; hier steht, dass der Dialog sie richtig
/// herum ruft, die Eingabe nicht zurueckspringt und das Ergebnis in den Posten
/// landet.</para>
/// </summary>
public class BhkwKatalogDialogTests : EposBunitContext
{
    /// <summary>Die Id ist der 0-BASIERTE Listenindex - Bestand, siehe BhkwKatalogDaten.</summary>
    private static readonly (int Id, string Text)[] Brennstoffe =
    {
        (0, "Stadtgas"), (1, "Erdgas LL"), (2, "Erdgas E"), (3, "Heizöl EL")
    };

    public BhkwKatalogDialogTests()
    {
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
        // 13 Zahlen (Ptherm, Pel, Wirkungsgrad, Grenzleistung, Gesamtsumme und Wert je
        // kW, die fuenf Posten, Raumbedarf, Wartung), 8 Ganzzahlen (Vorlauf, Ruecklauf,
        // Nutzungsdauer und die fuenf Emissionen), 3 Texte und 1 mehrzeilige
        // Beschreibung. Die zwei ANZEIGEFELDER Summe und Investition je kWel waren bis
        // W14a-E-8-B3 nur lesbare Textfelder; seither sind sie Zahlen-EINGABEfelder.
        Assert.Equal(13, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(8, cut.FindAll("input[inputmode=numeric]").Count);
        // Drei reine Textfelder: Modulname, Hersteller, Motortyp.
        Assert.Equal(3, cut.FindAll("input[type=text]:not([inputmode])").Count);
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
        Assert.Contains("Investition gesamt:", texte);
        Assert.Contains("Investition je kW elektrisch:", texte);
        Assert.Contains("Modul:", texte);
        Assert.Contains("Montage und Inbetriebnahme:", texte);
        Assert.Contains("Lieferung (50 km Umkreis):", texte);
        Assert.Contains("Schallschutzhaube:", texte);
        Assert.Contains("Abgasreinigung, z. B. Kat:", texte);
        Assert.Contains("mit SCR", texte);
    }

    // =================================================================================
    // Die Investition: drei Eingabewege auf dieselbe Groesse (W14a-E-8-B3)
    // =================================================================================
    //
    // Die Reihenfolge der 13 Zahlenfelder - sie traegt jeden Fall dieses Abschnitts:
    //   0 Ptherm   1 Pel   2 Wirkungsgrad   3 Grenzleistung
    //   4 GESAMT   5 JE KW
    //   6 Modul    7 Montage   8 Lieferung   9 Schallschutz   10 Abgasreinigung
    //  11 Raumbedarf  12 Wartung
    // Der Bestandssatz traegt 40 000 + 5 000 + 1 000 + 3 000 + 1 000 = 50 000 EUR bei
    // Pel = 40 kW, also 1 250 EUR/kW; die vier Nebenposten sind zusammen 10 000 EUR.

    private const int FELD_GESAMT = 4;
    private const int FELD_JE_KW = 5;
    private const int FELD_MODUL = 6;

    private static string Wert(IRenderedComponent<BhkwKatalogDialog> cut, int feld)
        => cut.FindAll("input[inputmode=decimal]")[feld].GetAttribute("value") ?? "";

    private static string Hinweis(IRenderedComponent<BhkwKatalogDialog> cut)
        => cut.FindAll(".epos-herleitung-text")[0].TextContent;

    [Fact]
    public void Beim_Laden_stehen_Gesamtsumme_und_Wert_je_kW_und_die_Zeile_nennt_die_Regel()
    {
        // 50 000 / 40 = 1 250 - der gespeicherte Wert passt zur Summe.
        var cut = Aufbauen();

        Assert.Equal("50000,00", Wert(cut, FELD_GESAMT));
        Assert.Equal("1250", Wert(cut, FELD_JE_KW));
        Assert.Contains("zuletzt geänderte Eingabe führt", Hinweis(cut));

        // Die Rechnung steht daneben, mit Zahlen statt mit Buchstaben.
        Assert.Equal("50000,00 € / 40,00 kW = 1250 € / kW",
                     cut.FindAll(".epos-herleitung-formel")[0].TextContent);
    }

    [Fact]
    public void Ein_abweichender_Bestandswert_wird_benannt_statt_still_korrigiert()
    {
        var daten = Bestand();
        daten.InvestitionJeKWel = 2000;    // passt NICHT zu 50 000 / 40
        var cut = Aufbauen(daten);

        Assert.Equal("2000", Wert(cut, FELD_JE_KW));
        Assert.Contains("weicht", Hinweis(cut));
    }

    [Fact]
    public void Ohne_elektrische_Leistung_ist_das_Feld_je_kW_leer_und_gesperrt()
    {
        var daten = Bestand();
        daten.Pel = 0;
        var cut = Aufbauen(daten);

        var jeKw = cut.FindAll("input[inputmode=decimal]")[FELD_JE_KW];
        Assert.Equal("", jeKw.GetAttribute("value"));
        Assert.True(jeKw.HasAttribute("disabled"));

        // Die Gesamtsumme bleibt erfasst - nur die Kennzahl je kW faellt weg.
        Assert.Equal("50000,00", Wert(cut, FELD_GESAMT));
        Assert.Contains("keinen Wert je kW", Hinweis(cut));

        // Und es steht KEINE erfundene Rechnung daneben.
        Assert.Null(cut.FindAll(".epos-herleitung")[0].QuerySelector(".epos-herleitung-formel"));
    }

    [Fact]
    public void Eine_Aenderung_an_einem_Posten_zieht_Gesamtsumme_und_Wert_je_kW_nach()
    {
        var cut = Aufbauen();

        cut.FindAll("input[inputmode=decimal]")[FELD_MODUL].Input("60000");

        Assert.Equal("70000,00", Wert(cut, FELD_GESAMT));
        Assert.Equal("1750", Wert(cut, FELD_JE_KW));
        Assert.Contains("zuletzt geänderte Eingabe führt", Hinweis(cut));
    }

    [Fact]
    public void Eine_Gesamteingabe_verteilt_sich_auf_den_Modulpreis()
    {
        var daten = Bestand();
        var cut = Aufbauen(daten);

        cut.FindAll("input[inputmode=decimal]")[FELD_GESAMT].Input("60000");

        // 60 000 - 10 000 Nebenposten = 50 000 fuer das Modul; die vier Nebenposten
        // bleiben unberuehrt (der Ausgleich laeuft NUR ueber den Modulpreis).
        Assert.Equal(50000.0, daten.KostenModul!.Value, 10);
        Assert.Equal(5000.0, daten.KostenMontage!.Value, 10);
        Assert.Equal(1000.0, daten.KostenLieferung!.Value, 10);
        Assert.Equal(3000.0, daten.KostenSchallschutzhaube!.Value, 10);
        Assert.Equal(1000.0, daten.KostenAbgasreinigung!.Value, 10);

        // Das getippte Feld behaelt SEINEN Text (kein Zurueckspringen auf "60000,00" -
        // die Anzeigeformatierung wuerde sonst mitten in der Eingabe zuschlagen), und
        // der Wert je kW folgt: 60 000 / 40.
        Assert.Equal("60000", Wert(cut, FELD_GESAMT));
        Assert.Equal("1500", Wert(cut, FELD_JE_KW));
    }

    [Fact]
    public void Eine_Eingabe_je_kW_wird_ueber_Pel_in_die_Gesamtsumme_umgerechnet()
    {
        var daten = Bestand();
        var cut = Aufbauen(daten);

        cut.FindAll("input[inputmode=decimal]")[FELD_JE_KW].Input("1500");

        // 1 500 EUR/kW x 40 kW = 60 000 EUR gesamt, davon 10 000 Nebenposten.
        Assert.Equal(50000.0, daten.KostenModul!.Value, 10);
        Assert.Equal(5000.0, daten.KostenMontage!.Value, 10);
        Assert.Equal("60000,00", Wert(cut, FELD_GESAMT));
        Assert.Equal("1500", Wert(cut, FELD_JE_KW));
    }

    [Fact]
    public void Eine_Gesamtsumme_unter_den_Nebenposten_deckelt_das_Modul_und_sagt_es()
    {
        var daten = Bestand();
        var cut = Aufbauen(daten);

        cut.FindAll("input[inputmode=decimal]")[FELD_GESAMT].Input("3000");

        Assert.Equal(0.0, daten.KostenModul!.Value, 10);
        Assert.True(cut.Instance.Gedeckelt);

        // Die Eingabe bleibt stehen - und die Zeile sagt, warum sie nicht aufgeht.
        Assert.Equal("3000", Wert(cut, FELD_GESAMT));
        Assert.Contains("übersteigen", Hinweis(cut));
    }

    [Fact]
    public void Die_Eingabe_der_Gesamtsumme_springt_nicht_zurueck()
    {
        // Kein Rueckruf im Kreis: Wer "60000" tippt, tippt vier Zwischenstaende, von
        // denen die ersten unter den Nebenposten liegen. Spraenge das Feld dabei auf
        // die abgeleitete Summe, waere weitertippen unmoeglich.
        var daten = Bestand();
        var cut = Aufbauen(daten);

        foreach (string stufe in new[] { "6", "60", "600", "6000", "60000" })
        {
            cut.FindAll("input[inputmode=decimal]")[FELD_GESAMT].Input(stufe);
            Assert.Equal(stufe, Wert(cut, FELD_GESAMT));
        }

        Assert.Equal(50000.0, daten.KostenModul!.Value, 10);
        Assert.False(cut.Instance.Gedeckelt);
    }

    [Fact]
    public void Speichern_schreibt_die_fuenf_Posten_und_den_abgeleiteten_Wert_je_kW()
    {
        BhkwKatalogDaten? geschrieben = null;
        var daten = Bestand();
        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = d;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll("input[inputmode=decimal]")[FELD_JE_KW].Input("1500");
        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.NotNull(geschrieben);
        Assert.Equal(50000.0, geschrieben!.KostenModul!.Value, 10);
        Assert.Equal(5000.0, geschrieben.KostenMontage!.Value, 10);
        Assert.Equal(1000.0, geschrieben.KostenLieferung!.Value, 10);
        Assert.Equal(3000.0, geschrieben.KostenSchallschutzhaube!.Value, 10);
        Assert.Equal(1000.0, geschrieben.KostenAbgasreinigung!.Value, 10);

        // Investition_kwel ist die ABLEITUNG der Posten, nicht das Eingabefeld:
        // 60 000 / 40 = 1 500.
        Assert.Equal(1500.0, geschrieben.InvestitionJeKWel!.Value, 10);
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
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Eingabeblock des BHKW-Katalogs steht seither im <c>Formularraster</c>: Die Beschriftung
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

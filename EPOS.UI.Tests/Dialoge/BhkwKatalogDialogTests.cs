using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using KiKern;
using WindowsFormsApplication1;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalogeditor BHKW (iU9-W6.2). Soll sind ZWEI Gruppen — „Modul" und „Technische
/// Daten" — und die Rueckfrage vor dem Ueberschreiben eines Katalogsatzes.
///
/// <para><b>Anwenderentscheid 15.09.2026:</b> „Der Dialog ueber Button Bearbeiten soll
/// keine Kosten und Emissionen enthalten." Damit sind die Gruppen „Eingabedaten zur
/// Berechnung der Kosten", „Emissionen nach BEHG-V" und „Emissionsfaktoren bezogen auf
/// den Brennstoffverbrauch" ersatzlos entfallen — samt den drei Eingabewegen der
/// Investition (W14a-E-8-B3) und den zwei Vorgabewertknoepfen. Gepflegt werden diese
/// Spalten jetzt im Aufklapper „Alle Daten anzeigen" des Projektdialogs
/// (<c>BhkwDialogTests</c>), gerechnet wird mit ihnen im Kern
/// (<c>EPOS.Kern.Tests/BhkwKostenTests</c>).</para>
///
/// <para><b>Was der Dialog nicht zeigt, verliert er nicht:</b> Die Huelle laedt den
/// vollstaendigen Satz und schreibt ihn vollstaendig zurueck — dafuer steht der Fall
/// <see cref="Die_entfallenen_Felder_gehen_beim_Speichern_unveraendert_durch"/>.</para>
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
        Action<string?>? geschlossen = null)
    {
        return Render<BhkwKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Bestand())
            .Add(x => x.Modus, modus)
            .Add(x => x.Brennstoffe, Brennstoffe)
            .Add(x => x.Ueberschreiben, ueberschreiben ?? ((d, _) => new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner)))
            .Add(x => x.Anlegen, anlegen ?? ((_, n) => new KatalogSpeicherErgebnis(true, "ok", n)))
            .Add(x => x.Geschlossen, n => geschlossen?.Invoke(n)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    /// <summary>
    /// <b>Nur noch ZWEI Gruppen</b> (Anwenderentscheid 15.09.2026). Der Fall haelt
    /// zugleich die Abwesenheit der drei entfallenen fest — sonst kaemen sie bei der
    /// naechsten Pflege unbemerkt zurueck.
    /// </summary>
    [Fact]
    public void Der_Editor_traegt_nur_noch_Modul_und_Technische_Daten()
    {
        var cut = Aufbauen();

        var titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();
        Assert.Equal(2, titel.Count);
        Assert.Equal("Modul", titel[0]);
        Assert.Equal("Technische Daten", titel[1]);

        Assert.DoesNotContain("Eingabedaten zur Berechnung der Kosten", titel);
        Assert.DoesNotContain("Emissionen nach BEHG-V", titel);
        Assert.DoesNotContain("Emissionsfaktoren bezogen auf den Brennstoffverbrauch", titel);
    }

    [Fact]
    public void Der_Feldbestand_stimmt_nach_Zahl_und_Beschriftung()
    {
        var cut = Aufbauen();

        // Zwoelf Felder: 5 Zahlen (Ptherm, Pel, elektrischer und thermischer
        // Wirkungsgrad, Grenzleistung), 2 Ganzzahlen (Vorlauf, Ruecklauf),
        // 4 Texte (Modulname, Hersteller, Motortyp und die BERECHNETE Anzeige des
        // Gesamtwirkungsgrads), 1 mehrzeilige Beschreibung und die Klappliste des
        // Energietraegers.
        Assert.Equal(5, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(2, cut.FindAll("input[inputmode=numeric]").Count);
        Assert.Equal(4, cut.FindAll("input[type=text]:not([inputmode])").Count);
        Assert.Single(cut.FindAll("textarea"));
        Assert.Single(cut.FindAll("select"));

        // Der Schalter "mit SCR" gehoerte zur Emissionsgruppe und ist mit ihr gegangen.
        Assert.Empty(cut.FindAll("input[type=checkbox]"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Modulname:", texte);
        Assert.Contains("Hersteller:", texte);
        Assert.Contains("Motortyp:", texte);
        Assert.Contains("Beschreibung:", texte);
        Assert.Contains("Thermische Leistung:", texte);
        Assert.Contains("Elektrische Leistung:", texte);
        // Schemaschritt 99: zwei Eingaben und die berechnete Summe.
        Assert.Contains("Elektrischer Wirkungsgrad:", texte);
        Assert.Contains("Thermischer Wirkungsgrad:", texte);
        Assert.Contains("Ges. Wirkungsgrad:", texte);
        Assert.Contains("Untere Grenzleistung:", texte);
        Assert.Contains("Energieträger:", texte);
        Assert.Contains("Vorlauf:", texte);
        Assert.Contains("Rücklauf:", texte);

        // Und KEIN Kosten- oder Emissionsfeld mehr.
        Assert.DoesNotContain("Investition gesamt:", texte);
        Assert.DoesNotContain("Investition je kW elektrisch:", texte);
        Assert.DoesNotContain("Modul:", texte);
        Assert.DoesNotContain("Montage und Inbetriebnahme:", texte);
        Assert.DoesNotContain("Lieferung (50 km Umkreis):", texte);
        Assert.DoesNotContain("Schallschutzhaube:", texte);
        Assert.DoesNotContain("Abgasreinigung, z. B. Kat:", texte);
        Assert.DoesNotContain("Raumbedarf:", texte);
        Assert.DoesNotContain("Wartungskosten:", texte);
        Assert.DoesNotContain("Nutzungsdauer:", texte);
        Assert.DoesNotContain("mit SCR", texte);
    }

    /// <summary>
    /// <b>Die Kostenwege bleiben</b> — als Knopfleiste unter den Gruppen. Entfallen ist
    /// die EINGABE der Kosten im Editor, nicht der Weg in die Kostenverwaltung.
    /// </summary>
    [Fact]
    public void Die_Kostenknoepfe_stehen_weiter_unter_den_Gruppen()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-kostenleiste"));
    }

    /// <summary>
    /// <b>Was der Dialog nicht mehr zeigt, verliert er nicht.</b> Die Huelle laedt den
    /// vollstaendigen Satz in <c>BhkwKatalogDaten</c> und schreibt ihn vollstaendig
    /// zurueck (<c>BhkwHuelle.AusModell</c> / <c>NachModell</c>); die fuenf
    /// Kostenposten, Raumbedarf, Wartung, Nutzungsdauer und die fuenf
    /// Emissionsfaktoren gehen unveraendert durch den Dialog hindurch.
    /// </summary>
    [Fact]
    public void Die_entfallenen_Felder_gehen_beim_Speichern_unveraendert_durch()
    {
        BhkwKatalogDaten? geschrieben = null;
        var daten = Bestand();
        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = d;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        // Eine Aenderung an einem Feld, das der Dialog noch zeigt.
        cut.FindAll("input[inputmode=decimal]")[0].Input("90");
        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.NotNull(geschrieben);
        Assert.Equal(90.0, geschrieben!.Ptherm!.Value, 10);

        Assert.Equal(40000.0, geschrieben.KostenModul!.Value, 10);
        Assert.Equal(5000.0, geschrieben.KostenMontage!.Value, 10);
        Assert.Equal(1000.0, geschrieben.KostenLieferung!.Value, 10);
        Assert.Equal(3000.0, geschrieben.KostenSchallschutzhaube!.Value, 10);
        Assert.Equal(1000.0, geschrieben.KostenAbgasreinigung!.Value, 10);
        Assert.Equal(6.0, geschrieben.Raumbedarf!.Value, 10);
        Assert.Equal(0.03, geschrieben.WartungskostenJeKWhel!.Value, 10);
        Assert.Equal(15, geschrieben.Nutzungsdauer);

        Assert.Equal(200000, geschrieben.CO2);
        Assert.Equal(285, geschrieben.NOx);
        Assert.Equal(370, geschrieben.CO);

        // Investition_kwel bleibt stehen, wie sie geladen wurde - nachgerechnet wird
        // sie auf dem Schreibweg im Kern (W14a-E-8-B3).
        Assert.Equal(1250.0, geschrieben.InvestitionJeKWel!.Value, 10);
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

    // HIER STANDEN DIE FAELLE ZU DEN VORGABEWERTKNOEPFEN "CO2 BEHG" und "Eintragen"
    // (Anwenderentscheid 15.09.2026). Sie gehoerten zu den Gruppen BEHG und
    // Emissionen; mit den Gruppen sind auch die Knoepfe entfallen. Die Emissionsfelder
    // waren ohnehin nur ANZEIGE (W14a-E-8-B1) - gerechnet wird mit dem Faktor des
    // Energietraegers aus dem Emissionskatalog.

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

    // =================================================================================
    // Die zwei Wirkungsgrade (Schemaschritt 99, Anwenderentscheid 20.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Die Summe läuft mit.</b> Gepflegt werden die zwei Anteile; der
    /// Gesamtwirkungsgrad daneben ist die berechnete Anzeige und folgt jeder Eingabe.
    /// </summary>
    [Fact]
    public void Der_Gesamtwirkungsgrad_laeuft_bei_jeder_Eingabe_mit()
    {
        var daten = Bestand();
        daten.WirkungsgradEl = 0.30;
        daten.WirkungsgradTh = 0.60;

        var cut = Aufbauen(daten);

        Assert.Equal("0,9", Gesamtanzeige(cut));

        // Der elektrische Anteil steigt - die Summe folgt.
        cut.FindAll("input[inputmode=decimal]")[2].Input("0,32");
        Assert.Equal("0,92", Gesamtanzeige(cut));

        // Der thermische ebenso.
        cut.FindAll("input[inputmode=decimal]")[3].Input("0,625");
        Assert.Equal("0,945", Gesamtanzeige(cut));

        // Und die Anzeige ist NICHT editierbar - sonst stuenden drei Zahlen da, von
        // denen zwei einander widersprechen.
        Assert.True(cut.FindAll("input[type=text]:not([inputmode])")[3].HasAttribute("readonly"));
    }

    /// <summary>
    /// <b>Ohne beide Anteile keine Summe</b>: Aus einem halben Paar lässt sich keine
    /// bilden, und eine 0 stünde dort für „Wirkungsgrad null".
    /// </summary>
    [Fact]
    public void Ohne_beide_Anteile_bleibt_die_Summe_leer()
    {
        var daten = Bestand();
        daten.Wirkungsgrad = null;
        daten.WirkungsgradEl = null;
        daten.WirkungsgradTh = null;

        var cut = Aufbauen(daten);
        Assert.Equal("", Gesamtanzeige(cut));

        cut.FindAll("input[inputmode=decimal]")[2].Input("0,30");
        Assert.Equal("", Gesamtanzeige(cut));
    }

    /// <summary>
    /// <b>Der Rückfall des Altbestands</b> (Schemaschritt 99): Ein Satz ohne
    /// Aufteilung — bei der Migration fehlte ihm eine Leistung — bekommt beim Öffnen
    /// die Aufteilung des Gesamtwirkungsgrads im Verhältnis der Leistungen als
    /// VORSCHLAG. 0,85 bei 40 kWel und 80 kWth ergibt 0,2833 und 0,5667.
    /// </summary>
    [Fact]
    public void Ein_Altbestandssatz_bekommt_die_Aufteilung_als_Vorschlag()
    {
        var daten = Bestand();          // Wirkungsgrad 0,85, Pel 40, Ptherm 80
        daten.WirkungsgradEl = null;
        daten.WirkungsgradTh = null;

        var cut = Aufbauen(daten);

        Assert.Equal(0.2833, daten.WirkungsgradEl!.Value, 4);
        Assert.Equal(0.5667, daten.WirkungsgradTh!.Value, 4);
        Assert.Equal("0,85", Gesamtanzeige(cut));
    }

    /// <summary>Das Speichern reicht BEIDE Werte durch — und die Hülle zieht die Summe nach.</summary>
    [Fact]
    public void Das_Speichern_reicht_beide_Anteile_durch()
    {
        BhkwKatalogDaten? geschrieben = null;
        var daten = Bestand();
        daten.WirkungsgradEl = 0.30;
        daten.WirkungsgradTh = 0.60;

        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = d;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll("input[inputmode=decimal]")[2].Input("0,31");
        cut.FindAll("input[inputmode=decimal]")[3].Input("0,59");
        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.NotNull(geschrieben);
        Assert.Equal(0.31, geschrieben!.WirkungsgradEl!.Value, 6);
        Assert.Equal(0.59, geschrieben.WirkungsgradTh!.Value, 6);
    }

    /// <summary>
    /// <b>A-BW2-2:</b> 0,30 + 0,80 = 1,10 wird abgewiesen — die Summe liegt über der
    /// Obergrenze 1,05, die auch das Brennwertgerät noch zulässt. Der Dialog bleibt
    /// offen und schreibt nicht.
    /// </summary>
    [Fact]
    public void Eine_Summe_ueber_1_05_wird_benannt_abgewiesen()
    {
        bool geschrieben = false;
        bool geschlossen = false;
        var daten = Bestand();
        daten.WirkungsgradEl = 0.30;
        daten.WirkungsgradTh = 0.80;

        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        }, geschlossen: _ => geschlossen = true);

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.False(geschrieben);
        Assert.False(geschlossen);
        Assert.Contains("zusammen", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// Ein PROZENTWERT im Anteilsfeld wird benannt abgelehnt — jeder Anteil liegt in
    /// (0; 1), und der Rechenweg teilt durch die Summe.
    /// </summary>
    [Theory]
    [InlineData(29.5, 0.60, false, "elektrische")]
    [InlineData(0.30, 59.0, false, "thermische")]
    [InlineData(0.0, 0.60, false, "elektrische")]
    [InlineData(1.0, 0.60, false, "elektrische")]
    [InlineData(0.45, 0.60, true, "")]
    [InlineData(0.451, 0.60, false, "zusammen")]
    [InlineData(0.295, 0.6266, true, "")]
    public void Jede_Regel_weist_benannt_ab(double el, double th, bool erlaubt, string wortlaut)
    {
        bool geschrieben = false;
        var daten = Bestand();
        daten.WirkungsgradEl = el;
        daten.WirkungsgradTh = th;

        var cut = Aufbauen(daten, ueberschreiben: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "ok", d.Bezeichner);
        });

        cut.FindAll(".epos-leiste button")[^4].Click();

        Assert.Equal(erlaubt, geschrieben);
        if (!erlaubt) Assert.Contains(wortlaut, cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>Der Text der berechneten Anzeige — das vierte einfache Textfeld.</summary>
    private static string Gesamtanzeige(IRenderedComponent<BhkwKatalogDialog> cut)
    {
        return cut.FindAll("input[type=text]:not([inputmode])")[3].GetAttribute("value") ?? "";
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

    /// <summary>
    /// <b>„Das Kreuz steht beim Titel"</b> (Anwenderentscheid 15.09.2026): Das ✕ der
    /// Kopfzeile wirkt genau wie Esc — es schließt ohne zu speichern und meldet
    /// <c>null</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_bricht_ab_wie_Esc()
    {
        string? gemeldet = "noch nicht";
        int rufe = 0;
        var cut = Aufbauen(geschlossen: n => { gemeldet = n; rufe++; });

        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(1, rufe);
        Assert.Null(gemeldet);
    }

    /// <summary>
    /// Die Kehrseite derselben Regel: Eingebettet trägt die <c>Ueberlagerung</c> den
    /// Titel (<c>TitelText=""</c>, Hausregel „Ein Titel, eine Stelle") — dann steht
    /// das Kreuz dort und NICHT ein zweites Mal im Dialogkopf.
    /// </summary>
    [Fact]
    public void Ohne_Titel_traegt_der_Kopf_kein_Kreuz()
    {
        var cut = Render<BhkwKatalogDialog>(p => p
            .Add(x => x.Daten, Bestand())
            .Add(x => x.Brennstoffe, Brennstoffe)
            .Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
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

    // =================================================================================
    //  Der Hilfe-Assistent (Welle KI-F5)
    // =================================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Der Editor meldet sein
    /// DATEN-OBJEKT an; gelesen und gesetzt wird über den Eigenschaftspfad des
    /// Katalogs, der Energieträger als WAHLFELD über seinen Anzeigetext (KI‑D‑Q6).
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_ihre_Felder()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BHKW));

        KiFeldzugang leistung = KiMaskenbruecke.Feldzugang(KiMaskennamen.BHKW, "th_leistung");
        Assert.NotNull(leistung);
        Assert.True(leistung.Setzbar);
        leistung.Setzen(95.0);
        cut.Render();
        Assert.Equal(95.0, Convert.ToDouble(leistung.Lesen(), CultureInfo.InvariantCulture));

        // Das Wahlfeld löst seinen Anzeigetext in den Listenschlüssel auf.
        KiFeldzugang traeger = KiMaskenbruecke.Feldzugang(KiMaskennamen.BHKW, "energietraeger");
        KiFeldumsetzung wahl = KiFeldwandler.Wandle(traeger, "Heizöl EL");
        Assert.True(wahl.Ok, wahl.Grund);
        traeger.Setzen(wahl.Wert);
        cut.Render();
        Assert.Equal(3, Convert.ToInt32(traeger.Lesen(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// <b>Der MODULNAME und der Gesamtwirkungsgrad sind nur lesbar.</b> Der Name ist
    /// der Schlüssel des UPDATE, die Summe der zwei Anteile eine Anzeige.
    /// </summary>
    [Fact]
    public void Name_und_Gesamtwirkungsgrad_sind_fuer_den_Assistenten_nur_lesbar()
    {
        Aufbauen();

        Assert.False(KiMaskenbruecke.Feldzugang(KiMaskennamen.BHKW, "name").Setzbar);
        Assert.False(KiMaskenbruecke.Feldzugang(KiMaskennamen.BHKW, "wirkungsgrad_gesamt").Setzbar);
    }

    /// <summary>
    /// <b>Der Speicherweg überschreibt den geladenen Satz</b> — und er lehnt benannt
    /// ab, solange der Satz aus der Auslieferung stammt: Diesen Schutz hebt der
    /// Anwender an der Maske auf, nicht der Assistent.
    /// </summary>
    [Fact]
    public void Der_Speicherweg_schreibt_und_lehnt_den_Auslieferungssatz_benannt_ab()
    {
        int gerufen = 0;
        var cut = Aufbauen(ueberschreiben: (d, _) =>
        {
            gerufen++;
            return new KatalogSpeicherErgebnis(true, "geschrieben", d.Bezeichner);
        });

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.BHKW);
        Assert.NotNull(haken.Speichern);

        KiErgebnis ok = haken.Speichern().GetAwaiter().GetResult();
        Assert.Equal(KiStatus.Ausgefuehrt, ok.Status);
        Assert.Equal(1, gerufen);

    }

    /// <summary>
    /// <b>Am AUSLIEFERUNGSSATZ lehnt der Speicherweg benannt ab.</b> Die Rückfrage
    /// „Schreibgeschützter Datensatz" ist eine ausdrückliche Bestätigung an genau
    /// diesem Satz; der Assistent lässt sie nicht aus.
    /// </summary>
    [Fact]
    public void Der_Speicherweg_lehnt_den_Auslieferungssatz_benannt_ab()
    {
        BhkwKatalogDaten katalogsatz = Bestand();
        katalogsatz.Katalogsatz = true;

        int gerufen = 0;
        Aufbauen(daten: katalogsatz, ueberschreiben: (d, _) =>
        {
            gerufen++;
            return new KatalogSpeicherErgebnis(true, "geschrieben", d.Bezeichner);
        });

        KiErgebnis abgelehnt = KiMaskenbruecke.Haken(KiMaskennamen.BHKW)
                                              .Speichern().GetAwaiter().GetResult();

        Assert.NotEqual(KiStatus.Ausgefuehrt, abgelehnt.Status);
        Assert.Equal(0, gerufen);
    }
}

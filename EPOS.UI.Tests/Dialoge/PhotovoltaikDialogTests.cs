using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Verwaltung Photovoltaik Module (iU9-W6.5). Soll ist die Feldkarte von
/// <c>Form_PV</c> — mit der Berichtigung aus R‑W6‑7: Die Karte ordnet die drei
/// Panel-Beschriftungen falsch zu; maßgeblich ist der Designer (Neigung [°],
/// Azimut [°], Anzahl Module).
/// </summary>
public class PhotovoltaikDialogTests : BunitContext
{
    private static readonly string[] Hersteller = { "Alle", "Musterwerk", "Solar AG" };

    private static readonly KatalogZeile[] Katalog =
    {
        new(31, "Modul 400"), new(32, "Modul 500")
    };

    public PhotovoltaikDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int geraetId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = geraetId,
                   Neigung = 30, Azimut = 180, AnzahlModule = 20 };

    private static ErzeugerDetail Detail(string name) => new(
        name, "Beschreibung",
        new[] { ("Hersteller:", "Musterwerk"), ("Modul Leistung [KW]:", "0,40") },
        null, Parameter(name));

    /// <summary>
    /// Die dreizehn Parameterzeilen des Aufklappers (W6‑E‑1). Sie kommen FERTIG
    /// FORMATIERT aus der Hülle — die Komponente rechnet und übersetzt nichts; der
    /// Wert hängt am Modulnamen, damit ein Wechsel sichtbar wird.
    /// </summary>
    private static IReadOnlyList<Modulparameter> Parameter(string name)
    {
        string kennung = name.EndsWith("500", StringComparison.Ordinal) ? "500" : "400";
        return new[]
        {
            new Modulparameter("Wirkungsgrad:", "16," + kennung, "%"),
            new Modulparameter("Spannung im MPP (Umpp):", "30,99", "V"),
            new Modulparameter("Leerlaufspannung (Uoc):", "38,97", "V"),
            new Modulparameter("Strom im MPP (Impp):", "8,88", "A"),
            new Modulparameter("Kurzschlussstrom (Isc):", "9,42", "A"),
            new Modulparameter("α_Isc / aIsc [A/°C]:", "–"),
            new Modulparameter("β_Voc / BVoco [V/°C]:", "–"),
            new Modulparameter("Temp.-Koeffizient Pmax:", "-0,4509", "%/K"),
            new Modulparameter("Zelltemperatur NOCT:", "–", "°C"),
            new Modulparameter("Länge:", "1,64", "m"),
            new Modulparameter("Breite:", "0,992", "m"),
            new Modulparameter("Modulkosten:", "–", "€"),
            new Modulparameter("Zelltechnologie:", "Kristallines Silizium (mono/poly)")
        };
    }

    private IRenderedComponent<PhotovoltaikDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, AufnahmeErgebnis>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        Action<ErzeugerZeile>? uebernehmen = null,
        Func<string>? gesamt = null,
        Func<int, bool>? katalogLoeschen = null,
        Func<IReadOnlyDictionary<string, object>>? verwaltung = null,
        bool wizard = false,
        Func<string, ErzeugerDetail>? detail = null,
        Action<bool>? geschlossen = null)
    {
        return Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Hersteller, Hersteller)
            .Add(x => x.Filtern, _ => Katalog)
            .Add(x => x.Detail, detail ?? (n => Detail(n)))
            .Add(x => x.Aufnehmen, aufnehmen ?? (_ => new AufnahmeErgebnis(Zeile(9, "Modul 500", 32))))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Uebernehmen, uebernehmen)
            .Add(x => x.Gesamtleistung, gesamt ?? (() => "8"))
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.VerwaltungGaben, verwaltung)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Geschlossen, ok => geschlossen?.Invoke(ok)));
    }

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-mitte button").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("ausgewählte Module", ueberschriften);
        Assert.Contains("Module aus Datenbank", ueberschriften);

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();
        Assert.Contains("PV Anlage Eigenschaften:", gruppen);
        Assert.Contains("Modul Eigenschaften:", gruppen);
    }

    [Fact]
    public void Die_drei_Anlagenfelder_tragen_die_Beschriftungen_des_Designers()
    {
        // R-W6-7: Die Feldkarte ordnet "Azimut [°]" dem Feld textBox_AnlagenLeistung
        // und "10" dem Feld textBox_Azimut zu. Der Designer sagt es anders, und er
        // hat recht: label3 "Neigung [°]:" liegt ueber textBox_Neigung, label6
        // "Azimut [°]:" ueber textBox_Azimut, label7 "Anzahl Module:" ueber
        // textBox_AnlagenLeistung.
        var cut = Aufbauen();

        var block = cut.Find(".epos-anlagenblock");
        var texte = block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "Neigung [°]:", "Azimut [°]:", "Anzahl Module:" }, texte);

        // Neigung und Azimut sind ganzzahlig, die Anzahl Module ist ein double
        // (WErzeugerModel.PV_Leistung) - der Feldname taeuscht, der Inhalt ist eine
        // Stueckzahl.
        Assert.Equal(2, block.QuerySelectorAll("input[inputmode=numeric]").Length);
        Assert.Single(block.QuerySelectorAll("input[inputmode=decimal]"));
    }

    [Fact]
    public void Der_Anlagenblock_erscheint_nur_bei_gewaehlter_Projektzeile()
    {
        // panel1.Visible - der Vorlaeufer blendete ihn beim Katalogsatz aus.
        var cut = Aufbauen();
        Assert.Single(cut.FindAll(".epos-anlagenblock"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();

        Assert.Empty(cut.FindAll(".epos-anlagenblock"));
    }

    /// <summary>
    /// Ohne Parametersatz der Modulverwaltung kein Knopf — Hausregel. Seit
    /// iU9-W14a.3 ist die Verwaltung eine ÜBERLAGERUNG im selben Fenster.
    /// </summary>
    [Fact]
    public void Der_Bearbeiten_Knopf_erscheint_nur_mit_Verwaltungsgaben()
    {
        var ohne = Aufbauen();
        Assert.DoesNotContain(ohne.FindAll("button").Select(b => b.TextContent),
                              t => t == "Modul Bearbeiten...");

        var mit = Aufbauen(verwaltung: () => Verwaltungsgaben());
        Assert.Contains(mit.FindAll("button").Select(b => b.TextContent),
                        t => t == "Modul Bearbeiten...");
    }

    /// <summary>Ein Mindestsatz für die Überlagerung — der Katalog braucht sein Profil.</summary>
    private static IReadOnlyDictionary<string, object> Verwaltungsgaben()
        => new Dictionary<string, object>
        {
            ["Art"] = WindowsFormsApplication1.ModulKatalogArt.Photovoltaik,
            ["Wege"] = new EPOS.UI.Dialoge.Erzeuger.ModulKatalogWege()
        };

    [Fact]
    public void Im_Assistenten_fehlt_die_OK_Leiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.Empty(cut.FindAll(".epos-status"));
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_Pfeil_nimmt_ohne_Traegerdialog_auf()
    {
        // Anders als Heizkessel und BHKW: keine Traegervariante, keine Projektkopie.
        int? gefragt = null;
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) };
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gefragt = id;
            return new AufnahmeErgebnis(Zeile(9, "Modul 500", 32));
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[0].Click();

        Assert.Equal(32, gefragt);
        Assert.Equal(2, zeilen.Count);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Der_Pfeil_zurueck_entfernt_die_ZEILE_nicht_ihren_Index()
    {
        // A-5: btn_Entfernen_Click nahm RemoveAt(SelectedIndex) auf eine Liste, die im
        // Assistenten ALLE Erzeugertypen fuehrt - der Index passte dort nicht.
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31), Zeile(2, "Modul 400", 31) };
        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-anlagenwahl")[1].Click();
        cut.FindAll(".epos-zweispalten-mitte button")[1].Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].Schluessel);
        Assert.Equal(2, entfernt[0].Schluessel);
    }

    // =================================================================================
    // Anlagenwerte und Gesamtleistung
    // =================================================================================

    [Fact]
    public void Die_drei_Anlagenwerte_wandern_ins_Modell()
    {
        var uebernommen = new List<ErzeugerZeile>();
        var cut = Aufbauen(uebernehmen: z => uebernommen.Add(z));

        var block = cut.Find(".epos-anlagenblock");
        block.QuerySelectorAll("input[inputmode=numeric]")[0].Input("35");
        block.QuerySelectorAll("input[inputmode=numeric]")[1].Input("200");
        block.QuerySelectorAll("input[inputmode=decimal]")[0].Input("25");

        Assert.Equal(35, cut.Instance.Projektzeile!.Neigung);
        Assert.Equal(200, cut.Instance.Projektzeile!.Azimut);
        Assert.Equal(25, cut.Instance.Projektzeile!.AnzahlModule);
        Assert.Equal(3, uebernommen.Count);
    }

    [Fact]
    public void Eine_neue_Modulzahl_zieht_die_Gesamtleistung_nach()
    {
        int rufe = 0;
        var cut = Aufbauen(gesamt: () => (++rufe).ToString());

        int vorher = rufe;
        cut.Find(".epos-anlagenblock input[inputmode=decimal]").Input("25");

        Assert.True(rufe > vorher, "Die Gesamtleistung wurde nicht neu erfragt.");
    }

    [Fact]
    public void Die_Gesamtleistung_kommt_fertig_von_aussen()
    {
        var cut = Aufbauen(gesamt: () => "12,50");
        Assert.Equal("12,50", cut.Instance.Gesamt);
    }

    // =================================================================================
    // Katalogpflege und Tastatur
    // =================================================================================

    [Fact]
    public void Loeschen_fragt_zuerst_nach()
    {
        var geloescht = new List<int>();
        var cut = Aufbauen(katalogLoeschen: id => { geloescht.Add(id); return true; });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[0].Click();
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Empty(geloescht);

        cut.FindAll(".epos-rueckfrage button")[0].Click();
        Assert.Equal(new[] { 31 }, geloescht);
    }

    /// <summary>
    /// „Modul Bearbeiten…" öffnet den Modulkatalog als ÜBERLAGERUNG im selben Fenster —
    /// bis iU9-W14a war es ein Sprung in ein zweites Fenster
    /// (<c>Sprungziel.PvAdmin</c>).
    /// </summary>
    [Fact]
    public void Bearbeiten_oeffnet_die_Modulverwaltung_als_Ueberlagerung()
    {
        var cut = Aufbauen(verwaltung: () => Verwaltungsgaben());

        Assert.False(cut.Instance.VerwaltungOffen);
        cut.FindAll(".epos-zweispalten-spalte")[1].QuerySelectorAll(".epos-leiste button")[0].Click();

        Assert.True(cut.Instance.VerwaltungOffen);
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_nicht_belegt()
    {
        int rufe = 0;
        bool? gemeldet = null;
        var cut = Aufbauen(geschlossen: ok => { gemeldet = ok; rufe++; });

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, rufe);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, rufe);
        Assert.False(gemeldet);
    }
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Detailblock des Projektdialogs steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Detailblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);
    }

    // =====================================================================
    //  Alle Modulparameter — Anwenderwunsch W6‑E‑1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>W6‑E‑1:</b> „optional sollten beim ausgewählten PV-Modul alle
    /// Eigenschaften/Parameter angezeigt werden."
    ///
    /// <para>OPTIONAL heißt: zugeklappt als Vorgabe. Der Dialog sieht beim Öffnen
    /// aus wie bisher — der Aufklapper steht darunter, und der Knopf sagt seinen
    /// Zustand über <c>aria-expanded</c> an.</para>
    /// </summary>
    [Fact]
    public void Der_Parameterblock_ist_zugeklappt_die_Vorgabe()
    {
        var cut = Aufbauen();

        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("Alle Modulparameter anzeigen", knopf.TextContent);

        Assert.False(cut.Instance.ParameterOffen);
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// Aufgeklappt stehen alle dreizehn Katalogfelder da — Beschriftung, Wert und
    /// die Einheit unmittelbar hinter dem kurzen Feld (iU8‑E‑2). Und sie sind
    /// NUR LESEND: Der Katalog wird hier angesehen, nicht gepflegt.
    /// </summary>
    [Fact]
    public void Aufgeklappt_stehen_alle_dreizehn_Parameter_da()
    {
        var cut = Aufbauen();
        cut.Find(".epos-modulparameter-knopf").Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal("true", cut.Find(".epos-modulparameter-knopf").GetAttribute("aria-expanded"));

        var block = cut.Find(".epos-modulparameter");
        Assert.Single(block.QuerySelectorAll(".epos-formularraster"));

        var texte = block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Equal(13, texte.Count);
        Assert.Equal(new[]
        {
            "Wirkungsgrad:", "Spannung im MPP (Umpp):", "Leerlaufspannung (Uoc):",
            "Strom im MPP (Impp):", "Kurzschlussstrom (Isc):",
            "α_Isc / aIsc [A/°C]:", "β_Voc / BVoco [V/°C]:",
            "Temp.-Koeffizient Pmax:", "Zelltemperatur NOCT:",
            "Länge:", "Breite:", "Modulkosten:", "Zelltechnologie:"
        }, texte);

        // Jedes Feld ist nur lesbar.
        var eingaben = block.QuerySelectorAll("input");
        Assert.Equal(13, eingaben.Length);
        Assert.All(eingaben, e => Assert.True(e.HasAttribute("readonly")));

        // Die Einheit steht hinter dem Feld - dieselbe Klasse wie beim Zahlenfeld.
        var einheiten = block.QuerySelectorAll(".epos-einheit").Select(e => e.TextContent).ToList();
        Assert.Equal(new[] { "%", "V", "V", "A", "A", "%/K", "°C", "m", "m", "€" }, einheiten);
    }

    /// <summary>
    /// Ein zweiter Druck klappt wieder zu — der Zustand gehört dem Dialog, nicht
    /// dem Browser.
    /// </summary>
    [Fact]
    public void Ein_zweiter_Druck_klappt_wieder_zu()
    {
        var cut = Aufbauen();

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.True(cut.Instance.ParameterOffen);

        cut.Find(".epos-modulparameter-knopf").Click();
        Assert.False(cut.Instance.ParameterOffen);
        Assert.Empty(cut.Find(".epos-modulparameter").QuerySelectorAll(".epos-feld"));
    }

    /// <summary>
    /// <b>Der Block gehört zum GEWÄHLTEN Modul.</b> Wer in der Katalogliste ein
    /// anderes Modul wählt, sieht dessen Werte — und der Aufklappzustand bleibt
    /// stehen, sonst müsste man ihn beim Vergleichen zweier Module jedes Mal neu
    /// aufziehen.
    /// </summary>
    [Fact]
    public void Ein_Modulwechsel_zieht_den_Block_nach()
    {
        var cut = Aufbauen();
        cut.Find(".epos-modulparameter-knopf").Click();

        string vorher = cut.Find(".epos-modulparameter").QuerySelectorAll("input")[0]
                           .GetAttribute("value")!;
        Assert.Equal("16,400", vorher);

        // Zweite Zeile der KATALOGliste: "Modul 500".
        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-anlagenwahl")[1].Click();

        Assert.True(cut.Instance.ParameterOffen);
        Assert.Equal("16,500", cut.Find(".epos-modulparameter").QuerySelectorAll("input")[0]
                                  .GetAttribute("value"));
    }

    /// <summary>
    /// <b>Ein nicht gepflegter Wert steht als „–" da, nicht als 0.</b> Die
    /// Entscheidung fällt im Kern (<c>PhotovoltaikStammCtrl.Parameterzeilen</c>);
    /// hier ist nachgewiesen, dass die Komponente den Strich unverändert zeigt und
    /// nicht etwa ein leeres Feld daraus macht.
    /// </summary>
    [Fact]
    public void Ein_nicht_gepflegter_Wert_steht_als_Strich_da()
    {
        var cut = Aufbauen();
        cut.Find(".epos-modulparameter-knopf").Click();

        var werte = cut.Find(".epos-modulparameter").QuerySelectorAll("input")
                       .Select(e => e.GetAttribute("value")).ToList();

        // alpha_SC, beta_OC, T_NOCT und die Modulkosten sind im Katalog leer -
        // und stehen als Strich da, nicht als "0" und nicht als leeres Feld.
        foreach (int i in new[] { 5, 6, 8, 11 })
        {
            Assert.Equal("–", werte[i]);
            Assert.NotEqual("0", werte[i]);
        }
    }

    /// <summary>
    /// Ohne Parameterzeilen gibt es keinen Aufklapper — so ist es heute bei den
    /// vier übrigen Erzeugerdialogen, die denselben Detailblock zeichnen. Ein
    /// leerer Knopf wäre ein Versprechen ohne Inhalt.
    /// </summary>
    [Fact]
    public void Ohne_Parameterzeilen_gibt_es_keinen_Aufklapper()
    {
        var cut = Aufbauen(detail: n => new ErzeugerDetail(
            n, "Beschreibung", new[] { ("Hersteller:", "Musterwerk") }));

        Assert.Empty(cut.FindAll(".epos-modulparameter-knopf"));
    }

    /// <summary>
    /// <b>Die Komponente formatiert und übersetzt nichts.</b> Unter <c>en-US</c>
    /// stehen genau die Texte da, die die Hülle hereingibt — samt der Zahlen, die
    /// dort schon in der Kultur des Anwenders formatiert wurden.
    /// </summary>
    [Fact]
    public void Auf_englisch_zeigt_die_Komponente_was_die_Huelle_hereingibt()
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");

        var cut = Render<PhotovoltaikDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile> { Zeile(1, "Modul 400", 31) })
            .Add(x => x.Hersteller, Hersteller)
            .Add(x => x.Filtern, _ => Katalog)
            .Add(x => x.Detail, n => new ErzeugerDetail(
                n, "", Array.Empty<(string, string)>(), null,
                new[]
                {
                    new Modulparameter("Efficiency:", "16.91", "%"),
                    new Modulparameter("Cell technology:", "–")
                }))
            .Add(x => x.LabelAlleParameter, "Show all module parameters")
            .Add(x => x.Gesamtleistung, () => "8"));

        var knopf = cut.Find(".epos-modulparameter-knopf");
        Assert.Contains("Show all module parameters", knopf.TextContent);
        knopf.Click();

        var block = cut.Find(".epos-modulparameter");
        Assert.Equal(new[] { "Efficiency:", "Cell technology:" },
                     block.QuerySelectorAll(".epos-feld-text").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "16.91", "–" },
                     block.QuerySelectorAll("input").Select(e => e.GetAttribute("value")).ToArray());
    }
}

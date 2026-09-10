using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der SPEICHERPARAMETERBLOCK des Ergebnisreiters „Stromspeicher" (W11b‑B‑28),
/// Anwenderwunsch 10.09.2026: „bringe den Tab Parameter → Stromspeicher aus
/// Dialog ‚Detaillierte Simulation' in den Tab ‚Stromspeicher'. Die Felder mit
/// Parametern sollen änderbar sein (und die Möglichkeit die geänderten Parameter
/// zu Speichern)."
///
/// <para><b>Soll:</b> Der Block ist die BENANNTE AUSNAHME von der Hausregel
/// „jedes Feld schreibt sofort" — er sammelt in einer Arbeitskopie und schreibt
/// erst auf Knopfdruck, in EINEM Zug und mit ALLEN Werten. Ohne Änderung ist der
/// Speichern-Knopf gesperrt, ohne Schreibdienst gibt es ihn gar nicht, und die
/// Übergabe wird nie verändert.</para>
///
/// <para>Die Felder selbst sind die des früheren Blatts P3 (siehe
/// <c>ParameterReiterTests</c> bis W11b‑B‑27): ohne aktive Variante gesperrt, die
/// Gerätegröße nur bei genau einer Speicheranlage, der Ausbaustufen-Schalter
/// dauerhaft gesperrt.</para>
/// </summary>
public class SpeicherParameterBlockTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;

    private readonly List<SpeicherParameterDaten> _geschrieben = new();
    private int _gelesen;
    private int _optimierungen;
    private Rueckmeldung _antwort = new Rueckmeldung(true, "gespeichert");

    public SpeicherParameterBlockTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
    }

    // =====================================================================
    // Probendaten
    // =====================================================================

    private static SpeicherParameterDaten Voll() => new SpeicherParameterDaten
    {
        VarianteVorhanden = true,
        Variantenstatus = "Aktive Variante: Speicher 1",
        SoCMinProzent = 10,
        SoCMaxProzent = 90,
        SoCMinKwh = "= 1,0 kWh",
        SoCMaxKwh = "= 9,0 kWh",
        Ladeschwellwert = 90,
        LadeleistungKw = 11.04,
        KapazitaetKwh = 10.0,
        Betriebsart = DbWerte.SP_BETRIEBSART_GRUENSTROM,
        Berechnungsart = DbWerte.SP_BERECHNUNG_DAUERNUTZUNG,
        Betriebsarten = new[]
        {
            new Steuerwahl(DbWerte.SP_BETRIEBSART_GRUENSTROM, "Grünstrom"),
            new Steuerwahl(DbWerte.SP_BETRIEBSART_GRAUSTROM, "Graustrom")
        },
        Berechnungsarten = new[]
        {
            new Steuerwahl(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG, "Dauernutzung"),
            new Steuerwahl(DbWerte.SP_BERECHNUNG_NACHTNUTZUNG, "Nachtnutzung"),
            new Steuerwahl(DbWerte.SP_BERECHNUNG_ARBITRAGE, "Preissteuerung / Arbitrage")
        },
        KompatibilitaetMoeglich = false,
        LadenAusPv = true,
        Kapitalzins = 3.5,
        Nutzungsdauer = 15,
        Leistungspreis = 120,
        Netzladeaufschlag = 2.5,
        Preisquelle = DbWerte.SP_PREISQUELLE_FIXPREIS,
        Preisquellen = new[]
        {
            new Steuerwahl(DbWerte.SP_PREISQUELLE_FIXPREIS, "Fixpreis"),
            new Steuerwahl(DbWerte.SP_PREISQUELLE_PROFIL, "Kostenprofil"),
            new Steuerwahl(DbWerte.SP_PREISQUELLE_SPOTMARKT, "Spotmarkt")
        },
        PreisreiheLabel = "Preisreihe",
        Preisreihen = Array.Empty<(int, string)>(),
        PreisreiheMoeglich = false
    };

    private SimulationErgebnisDienste Dienste(bool mitSchreiben = true,
                                              bool mitLesen = true,
                                              bool mitPreisreihen = false,
                                              SpeicherParameterDaten? lesestand = null)
        => new SimulationErgebnisDienste
        {
            SpeicherparameterSchreiben = mitSchreiben
                ? d => { _geschrieben.Add(d); return _antwort; }
                : null,
            SpeicherparameterLesen = mitLesen
                ? () => { _gelesen++; return lesestand ?? Voll(); }
                : null,
            SpeicherPreisreihen = mitPreisreihen ? Preisreihen : null
        };

    /// <summary>Der Lesedienst der Hülle in klein: je Quelle Beschriftung, Liste und Id.</summary>
    private static SpeicherPreisreihenDaten Preisreihen(string quelle)
    {
        if (quelle == DbWerte.SP_PREISQUELLE_SPOTMARKT)
            return new SpeicherPreisreihenDaten
            {
                Label = "Preisreihe",
                Moeglich = true,
                Reihen = new (int, string)[] { (7, "Spot 2024") },
                Id = 7
            };

        if (quelle == DbWerte.SP_PREISQUELLE_PROFIL)
            return new SpeicherPreisreihenDaten
            {
                Label = "Kostenprofil",
                Moeglich = true,
                Reihen = new (int, string)[] { (3, "Profil Werk 1") },
                Id = 3
            };

        return new SpeicherPreisreihenDaten { Label = "Preisreihe", Moeglich = false };
    }

    private IRenderedComponent<SpeicherParameterBlock> Zeichnen(
        SpeicherParameterDaten? daten = null,
        SimulationErgebnisDienste? dienste = null,
        bool gesperrt = false,
        bool optimierung = false)
        => Render<SpeicherParameterBlock>(p =>
        {
            p.Add(x => x.Daten, daten ?? Voll());
            p.Add(x => x.Dienste, dienste ?? Dienste());
            p.Add(x => x.Gesperrt, gesperrt);
            if (optimierung)
            {
                p.Add(x => x.OptimierungMoeglich, true);
                p.Add(x => x.Optimierung, EventCallback.Factory.Create(this, () => _optimierungen++));
            }
        });

    /// <summary>Die Zahlenfelder in Markupreihenfolge: SoC min, SoC max, Leistung, Kapazität, Schwelle, …</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Zahlen(
        IRenderedComponent<SpeicherParameterBlock> block)
        => block.FindAll("input[type='text']");

    private static AngleSharp.Dom.IElement Knopf(
        IRenderedComponent<SpeicherParameterBlock> block, string text)
        => block.FindAll("button.epos-simerg-knopf").First(b => b.TextContent.Trim() == text);

    private static bool HatKnopf(IRenderedComponent<SpeicherParameterBlock> block, string text)
        => block.FindAll("button.epos-simerg-knopf").Any(b => b.TextContent.Trim() == text);

    // =====================================================================
    //  Die AUSNAHME: es wird gepuffert
    // =====================================================================

    /// <summary>
    /// <b>Der Kernfall.</b> Eine Feldänderung schreibt NICHT — anders als auf der
    /// Parameterseite, wo jedes Feld sofort schreibt. Der Anwender hat ausdrücklich
    /// „die Möglichkeit die geänderten Parameter zu Speichern" verlangt.
    /// </summary>
    [Fact]
    public void Eine_Feldaenderung_schreibt_nicht_sofort()
    {
        var block = Zeichnen();
        Zahlen(block)[0].Input("15");

        Assert.Empty(_geschrieben);
        Assert.True(block.Instance.HatAenderungen);
        Assert.Equal(15.0, block.Instance.Arbeitskopie.SoCMinProzent);
    }

    /// <summary>Und die ÜBERGABE bleibt unangetastet — der Block arbeitet auf einer Kopie.</summary>
    [Fact]
    public void Die_Uebergabe_bleibt_unveraendert()
    {
        SpeicherParameterDaten uebergabe = Voll();
        var block = Zeichnen(uebergabe);

        Zahlen(block)[0].Input("15");

        Assert.Equal(10.0, uebergabe.SoCMinProzent);
    }

    /// <summary>
    /// Speichern ruft den Dienst GENAU EINMAL — mit ALLEN gepufferten Werten, auch
    /// denen, die in demselben Zug geändert wurden. Ein halber Satz wäre schlimmer
    /// als keiner.
    /// </summary>
    [Fact]
    public void Speichern_uebergibt_alle_gepufferten_Werte_auf_einmal()
    {
        var block = Zeichnen();

        Zahlen(block)[0].Input("15");
        Zahlen(block)[4].Input("77");                       // Ladeschwellwert
        block.FindAll("select")[0].Change("1");             // Betriebsart -> Graustrom
        block.FindAll("input[type='checkbox']")[1].Change(false);   // Laden aus PV aus

        Knopf(block, Resource.SP_PARAM_BTN_SPEICHERN).Click();

        SpeicherParameterDaten satz = Assert.Single(_geschrieben);
        Assert.Equal(15.0, satz.SoCMinProzent);
        Assert.Equal(77.0, satz.Ladeschwellwert);
        Assert.Equal(DbWerte.SP_BETRIEBSART_GRAUSTROM, satz.Betriebsart);
        Assert.False(satz.LadenAusPv);

        // Unberuehrtes faehrt unveraendert mit.
        Assert.Equal(90.0, satz.SoCMaxProzent);
        Assert.Equal(15.0, satz.Nutzungsdauer);
    }

    /// <summary>Nach dem Speichern gilt der FRISCHE Lesestand, nicht der Puffer.</summary>
    [Fact]
    public void Nach_dem_Speichern_liest_der_Block_neu()
    {
        var block = Zeichnen();
        Zahlen(block)[0].Input("15");
        Knopf(block, Resource.SP_PARAM_BTN_SPEICHERN).Click();

        Assert.Equal(1, _gelesen);
        Assert.False(block.Instance.HatAenderungen);
        Assert.Equal(10.0, block.Instance.Arbeitskopie.SoCMinProzent);   // der Stand aus Voll()
    }

    /// <summary>„Änderungen verwerfen" holt den Lesestand zurück — über den Lesedienst.</summary>
    [Fact]
    public void Verwerfen_stellt_den_Lesestand_wieder_her()
    {
        var block = Zeichnen();
        Zahlen(block)[0].Input("15");
        Assert.True(block.Instance.HatAenderungen);

        Knopf(block, Resource.SP_PARAM_BTN_VERWERFEN).Click();

        Assert.Equal(1, _gelesen);
        Assert.Empty(_geschrieben);
        Assert.False(block.Instance.HatAenderungen);
        Assert.Equal(10.0, block.Instance.Arbeitskopie.SoCMinProzent);
    }

    /// <summary>
    /// Der Speichern-Knopf ist nur aktiv, wenn es etwas zu speichern gibt; „Verwerfen"
    /// steht erst gar nicht da (dieselbe Regel wie in der <c>SpeichernLeiste</c> des
    /// Hauses: markierter Satz UND Änderung).
    /// </summary>
    [Fact]
    public void Ohne_Aenderung_ist_Speichern_gesperrt_und_Verwerfen_weg()
    {
        var block = Zeichnen();

        Assert.True(Knopf(block, Resource.SP_PARAM_BTN_SPEICHERN).HasAttribute("disabled"));
        Assert.False(HatKnopf(block, Resource.SP_PARAM_BTN_VERWERFEN));

        Zahlen(block)[0].Input("15");

        Assert.False(Knopf(block, Resource.SP_PARAM_BTN_SPEICHERN).HasAttribute("disabled"));
        Assert.True(HatKnopf(block, Resource.SP_PARAM_BTN_VERWERFEN));
    }

    /// <summary>„Kein Delegat ist kein Knopf" (Regel seit W2.2).</summary>
    [Fact]
    public void Ohne_Schreibdienst_gibt_es_keinen_Speichern_Knopf()
    {
        var block = Zeichnen(dienste: Dienste(mitSchreiben: false));

        Assert.False(HatKnopf(block, Resource.SP_PARAM_BTN_SPEICHERN));
    }

    /// <summary>
    /// Die Rückmeldung des Dienstes steht in der Statuszeile — bei einem Fehlschlag in
    /// der Warnfarbe, und der Puffer bleibt stehen, damit die Eingabe nicht verloren
    /// ist.
    /// </summary>
    [Fact]
    public void Die_Rueckmeldung_des_Speicherns_steht_in_der_Statuszeile()
    {
        _antwort = new Rueckmeldung(false, Resource.SP_PARAM_MSG_SOC_BAND);

        var block = Zeichnen();
        Zahlen(block)[0].Input("95");
        Knopf(block, Resource.SP_PARAM_BTN_SPEICHERN).Click();

        var zeile = block.FindAll("p.epos-simerg-status")
                         .First(p => p.TextContent.Contains(Resource.SP_PARAM_MSG_SOC_BAND));
        Assert.Contains("epos-simerg-warn", zeile.ClassName);

        Assert.Equal(0, _gelesen);                      // kein frisches Lesen nach Fehlschlag
        Assert.True(block.Instance.HatAenderungen);     // die Eingabe steht noch da
    }

    /// <summary>Und bei Erfolg steht sie ohne Warnfarbe da.</summary>
    [Fact]
    public void Die_Bestaetigung_steht_ohne_Warnfarbe()
    {
        _antwort = new Rueckmeldung(true, Resource.SP_PARAM_MSG_GESPEICHERT);

        var block = Zeichnen();
        Zahlen(block)[0].Input("15");
        Knopf(block, Resource.SP_PARAM_BTN_SPEICHERN).Click();

        var zeile = block.FindAll("p.epos-simerg-status")
                         .First(p => p.TextContent.Contains(Resource.SP_PARAM_MSG_GESPEICHERT));
        Assert.DoesNotContain("epos-simerg-warn", zeile.ClassName);
    }

    /// <summary>Solange etwas offen ist, sagt es die Statuszeile.</summary>
    [Fact]
    public void Ungespeicherte_Aenderungen_stehen_in_der_Statuszeile()
    {
        var block = Zeichnen();
        Assert.DoesNotContain(Resource.SP_PARAM_STATUS_UNGESPEICHERT, block.Markup);

        Zahlen(block)[0].Input("15");
        Assert.Contains(Resource.SP_PARAM_STATUS_UNGESPEICHERT, block.Markup);
    }

    /// <summary>
    /// Ein neuer Lesestand (die Seite hat nach einem Lauf neu geladen) übernimmt den
    /// Puffer — ABER NUR, solange nichts Ungespeichertes offen ist. Die Eingabe des
    /// Anwenders wiegt schwerer als der Nachschlag.
    /// </summary>
    [Fact]
    public void Eine_neue_Uebergabe_ersetzt_den_Puffer_nur_ohne_offene_Aenderung()
    {
        var block = Zeichnen();

        SpeicherParameterDaten frisch = Voll();
        frisch.SoCMinProzent = 20;
        block.Render(p => p.Add(x => x.Daten, frisch));
        Assert.Equal(20.0, block.Instance.Arbeitskopie.SoCMinProzent);

        Zahlen(block)[0].Input("35");

        SpeicherParameterDaten nochmal = Voll();
        nochmal.SoCMinProzent = 44;
        block.Render(p => p.Add(x => x.Daten, nochmal));

        Assert.Equal(35.0, block.Instance.Arbeitskopie.SoCMinProzent);
        Assert.True(block.Instance.HatAenderungen);
    }

    // =====================================================================
    //  Die Felder
    // =====================================================================

    /// <summary>
    /// Ohne aktive Variante gäbe es kein Ziel für das Zurückschreiben — dann sind die
    /// Felder Attrappen (<c>LeseSpeicherVariante</c> :6455-6470).
    /// </summary>
    [Fact]
    public void Ohne_aktive_Variante_sind_die_Felder_gesperrt()
    {
        SpeicherParameterDaten d = Voll();
        d.VarianteVorhanden = false;
        d.Variantenstatus = Resource.SP_PARAM_STATUS_KEINE_VARIANTE;

        var block = Zeichnen(d);

        Assert.Empty(block.FindAll("input:not([disabled])"));
        Assert.Empty(block.FindAll("select:not([disabled])"));
        Assert.Contains("epos-simerg-warn",
                        block.Find("p.epos-simerg-status").ClassName);
    }

    /// <summary>Der Sperrzustand (Schemamigration) sperrt ebenfalls alles.</summary>
    [Fact]
    public void Der_Sperrzustand_sperrt_alle_Felder()
    {
        var block = Zeichnen(gesperrt: true);

        Assert.Empty(block.FindAll("input:not([disabled])"));
        Assert.Empty(block.FindAll("select:not([disabled])"));
    }

    /// <summary>
    /// <b>Die Gerätegröße ist neu änderbar</b> (W11b‑B‑28) — aber nur bei GENAU EINER
    /// Speicheranlage. Sonst bleibt sie gesperrt und trägt den bisherigen Hinweis:
    /// Varianten desselben Speichers teilen sich EINE Gerätekopie.
    /// </summary>
    [Fact]
    public void Die_Geraetegroesse_ist_nur_mit_der_Erlaubnis_aenderbar()
    {
        var ohne = Zeichnen();
        Assert.True(Zahlen(ohne)[2].HasAttribute("disabled"));   // Ladeleistung
        Assert.True(Zahlen(ohne)[3].HasAttribute("disabled"));   // Kapazitaet
        Assert.Contains(Resource.SP_PARAM_HINWEIS_LADELEISTUNG, ohne.Markup);

        SpeicherParameterDaten d = Voll();
        d.GeraetegroesseAenderbar = true;
        var mit = Zeichnen(d);

        Assert.False(Zahlen(mit)[2].HasAttribute("disabled"));
        Assert.False(Zahlen(mit)[3].HasAttribute("disabled"));
        Assert.DoesNotContain(Resource.SP_PARAM_HINWEIS_LADELEISTUNG, mit.Markup);
    }

    /// <summary>
    /// Das kWh-Äquivalent folgt der GEÄNDERTEN Kapazität — sonst stünde unter dem
    /// SoC-Band eine Zahl, die zu einem Gerät gehört, das der Anwender gerade
    /// weggeschrieben hat.
    /// </summary>
    [Fact]
    public void Das_kWh_Aequivalent_folgt_der_geaenderten_Kapazitaet()
    {
        SpeicherParameterDaten d = Voll();
        d.GeraetegroesseAenderbar = true;
        var block = Zeichnen(d);

        // Ungeaendert steht der Text der Huelle da.
        Assert.Equal("= 1,0 kWh", block.FindAll("span.epos-simerg-aequivalent")[0].TextContent);

        Zahlen(block)[3].Input("200");      // Kapazitaet 10 -> 200 kWh

        Assert.Equal("= 20,0 kWh", block.FindAll("span.epos-simerg-aequivalent")[0].TextContent);
        Assert.Equal("= 180,0 kWh", block.FindAll("span.epos-simerg-aequivalent")[1].TextContent);
    }

    /// <summary>Und dem geänderten Prozentwert ebenso.</summary>
    [Fact]
    public void Das_kWh_Aequivalent_folgt_dem_geaenderten_Prozentwert()
    {
        var block = Zeichnen();
        Zahlen(block)[0].Input("50");

        Assert.Equal("= 5,0 kWh", block.FindAll("span.epos-simerg-aequivalent")[0].TextContent);
    }

    /// <summary>
    /// Der Wechsel der Preisquelle holt Beschriftung UND Liste über den Lesedienst —
    /// aus „Preisreihe" wird „Kostenprofil". Geschrieben wird dabei nichts.
    /// </summary>
    [Fact]
    public void Der_Preisquellenwechsel_holt_Label_und_Liste()
    {
        var block = Zeichnen(dienste: Dienste(mitPreisreihen: true));

        // [0] Betriebsart, [1] Berechnungsart, [2] Preisquelle, [3] Reihe
        block.FindAll("select")[2].Change("1");     // -> Kostenprofil

        Assert.Empty(_geschrieben);
        Assert.Equal("Kostenprofil", block.Instance.Arbeitskopie.PreisreiheLabel);
        Assert.Equal(3, block.Instance.Arbeitskopie.PreisreiheId);
        Assert.True(block.Instance.Arbeitskopie.PreisreiheMoeglich);
        Assert.Contains("Profil Werk 1", block.Markup);
    }

    /// <summary>Ohne den Lesedienst bleibt die Liste, wie sie geliefert wurde.</summary>
    [Fact]
    public void Ohne_Preisreihendienst_bleibt_die_Liste_stehen()
    {
        var block = Zeichnen();
        block.FindAll("select")[2].Change("2");     // -> Spotmarkt

        Assert.Equal(DbWerte.SP_PREISQUELLE_SPOTMARKT, block.Instance.Arbeitskopie.Preisquelle);
        Assert.Equal("Preisreihe", block.Instance.Arbeitskopie.PreisreiheLabel);
        Assert.False(block.Instance.Arbeitskopie.PreisreiheMoeglich);
    }

    /// <summary>
    /// Der Kompatibilitätsmodus ist nur bei NACHTNUTZUNG wählbar — dieselbe Regel, die
    /// die Hülle beim Lesen anwendet. Im Puffer muss sie mitlaufen, sonst bliebe der
    /// Schalter bis zum nächsten Lesen falsch gesperrt.
    /// </summary>
    [Fact]
    public void Die_Kompatibilitaet_haengt_an_der_gepufferten_Berechnungsart()
    {
        var block = Zeichnen();

        // [0] Kompatibilitaet, [1..3] Quellen, [4] Ausbaustufe
        Assert.True(block.FindAll("input[type='checkbox']")[0].HasAttribute("disabled"));

        block.FindAll("select")[1].Change("1");     // Berechnungsart -> Nachtnutzung

        Assert.True(block.Instance.Arbeitskopie.KompatibilitaetMoeglich);
        Assert.False(block.FindAll("input[type='checkbox']")[0].HasAttribute("disabled"));

        block.FindAll("select")[1].Change("2");     // -> Arbitrage
        Assert.False(block.Instance.Arbeitskopie.KompatibilitaetMoeglich);
        Assert.True(block.FindAll("input[type='checkbox']")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// Der Ausbaustufen-Schalter „BHKW stromgeführt" bleibt in JEDEM Fall gesperrt —
    /// der Anwender soll sehen, dass es ihn gibt, aber nicht auf eine Wirkung warten,
    /// die ausbleibt.
    /// </summary>
    [Fact]
    public void Der_Ausbaustufen_Schalter_bleibt_gesperrt()
    {
        var block = Zeichnen();

        Assert.True(block.FindAll("input[type='checkbox']")[4].HasAttribute("disabled"));
    }

    /// <summary>Kein Sprungdelegat = kein Knopf (Regel seit W2.2).</summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Optimierungsknopf_weg()
    {
        Assert.DoesNotContain(Zeichnen().FindAll("button"),
                              b => b.TextContent.Contains("optimieren"));

        var mit = Zeichnen(optimierung: true);
        mit.FindAll("button").First(b => b.TextContent.Contains("optimieren")).Click();

        Assert.Equal(1, _optimierungen);
    }

    /// <summary>
    /// Der Block steht im einspaltigen Formularraster — wie das Blatt, aus dem er kommt
    /// (iU8‑E‑2, Paket P3). Geprüft wird das MARKUP; was der Raster daraus MACHT, steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> (Lehre W6‑B‑1).
    /// </summary>
    [Fact]
    public void Der_Block_steht_im_einspaltigen_Formularraster()
    {
        var block = Zeichnen();

        var raster = block.FindAll(".epos-simerg-felder .epos-formularraster");
        Assert.Equal(5, raster.Count);
        Assert.Equal(raster.Count,
                     block.FindAll(".epos-simerg-felder .epos-formularraster--einspaltig").Count);
        Assert.NotEmpty(block.FindAll(
            ".epos-formularraster .epos-feld--kurz .epos-feld-zeile .epos-einheit"));
    }
}

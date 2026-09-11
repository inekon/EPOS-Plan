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
/// nach dem ANWENDERENTSCHEID 1 vom 10.09.2026: „Sofort schreiben, wie überall
/// sonst im Programm." (W11b‑B‑29).
///
/// <para><b>Soll:</b> JEDES Feld schreibt SOFORT über
/// <c>Dienste.SpeicherfeldSchreiben</c> — einen Aufruf je Änderung, mit dem
/// sprachneutralen Feldschlüssel und einem invarianten Wert. Die Rückmeldung des
/// Dienstes steht in der Statuszeile am KOPF des Blocks, und dort steht auch der
/// Optimierungsknopf. Es gibt keinen Speichern- und keinen Verwerfen-Knopf mehr:
/// Der gepufferte Block aus W11b‑B‑28 verlor seine Eingaben beim Reiterwechsel,
/// meldete einen fehlgeschlagenen Schreibvorgang als Erfolg, und seinen Knopf am
/// Blockende fand der Anwender nicht.</para>
///
/// <para>Die Felder selbst sind die des früheren Blatts P3 (siehe
/// <c>ParameterReiterTests</c> bis W11b‑B‑27): ohne aktive Variante gesperrt, die
/// Gerätegröße nur bei genau einer Speicheranlage, der Ausbaustufen-Schalter
/// dauerhaft gesperrt.</para>
/// </summary>
public class SpeicherParameterBlockTests : EposBunitContext
{
    /// <summary>
    /// Gate sept39 (11.09.2026, Gegenprobe #230b, LANG=en_US.UTF-8): Diese Klasse erbt bereits
    /// <see cref="EposBunitContext"/>, das im KONSTRUKTORRUMPF pinnt — C# wertet die
    /// FELDINITIALISIERER der abgeleiteten Klasse aber VOR dem gesamten Basiskonstruktor aus
    /// (empirisch geprüft, entgegen der verbreiteten Annahme „Basis zuerst"). Ein
    /// Feldinitialisierer, der hier eine Ressource läse, säße also VOR der geerbten Pinnung fest
    /// (traf <c>_antwort</c> unten: <c>Resource.SP_PARAM_MSG_GESPEICHERT</c> fiel dadurch auf
    /// Englisch fest, siehe Konstruktor). Dieses EIGENE Feld pinnt zusätzlich VOR jedem weiteren
    /// Feldinitialisierer dieser Klasse (deklariert als erstes) und stellt in <c>Dispose</c>
    /// zurück — NACH der geerbten Vorrichtung (<c>base.Dispose</c> zuerst), sonst überschriebe die
    /// geerbte Rückstellung (die bei ihrer eigenen Konstruktion bereits DIESE Pinnung als „vorher"
    /// sah) die hier wiederhergestellte echte Ausgangskultur erneut mit de-DE.
    /// </summary>
    private readonly Kulturvorrichtung _kultur = new();

    /// <summary>Jeder Schreibvorgang: Feldschlüssel und Wert, in der Reihenfolge des Anfalls.</summary>
    private readonly List<(string Feld, string Wert)> _geschrieben = new();
    private int _optimierungen;

    /// <summary>
    /// Bewusst NICHT als Feldinitialisierer (siehe Doku an <see cref="_kultur"/>): Der
    /// Konstruktorrumpf läuft nachweislich NACH der gesamten Basiskonstruktion — hier ist
    /// <c>Resource.SP_PARAM_MSG_GESPEICHERT</c> sicher gepinnt.
    /// </summary>
    private Rueckmeldung _antwort;

    public SpeicherParameterBlockTests()
    {
        _antwort = new Rueckmeldung(true, Resource.SP_PARAM_MSG_GESPEICHERT);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _kultur.Dispose();
        }
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
                                              bool mitPreisreihen = false)
        => new SimulationErgebnisDienste
        {
            SpeicherfeldSchreiben = mitSchreiben
                ? (feld, wert) => { _geschrieben.Add((feld, wert)); return _antwort; }
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

    private (string Feld, string Wert) Einziger => Assert.Single(_geschrieben);

    // =====================================================================
    //  Der Kernfall: jedes Feld schreibt SOFORT
    // =====================================================================

    /// <summary>
    /// <b>Der Kernfall</b> (W11b‑B‑29). Eine Feldänderung ruft den Schreibdienst
    /// GENAU EINMAL — mit dem Schlüssel dieses Feldes und dem Wert INVARIANT
    /// geschrieben, auch wenn der Anwender ihn mit Komma eingibt. Nichts wird
    /// gesammelt, und es gibt keinen Knopf, den man dazu drücken müßte.
    /// </summary>
    [Fact]
    public void Eine_Feldaenderung_schreibt_sofort_und_genau_einmal()
    {
        var block = Zeichnen();

        Zahlen(block)[0].Input("12,5");

        Assert.Equal((SpeicherFeld.SoCMin, "12.5"), Einziger);
    }

    /// <summary>
    /// Und zwar JEDES Feld — die neun Zahlen, die vier Schalter und die drei
    /// Auswahllisten, jedes mit seinem eigenen Schlüssel. Der Fall ist die
    /// Vollständigkeitsprobe: Ein vergessener Schreibweg fällt hier auf, nicht erst
    /// dem Anwender.
    /// </summary>
    [Fact]
    public void Jedes_Feld_schreibt_mit_seinem_eigenen_Schluessel()
    {
        SpeicherParameterDaten d = Voll();
        d.GeraetegroesseAenderbar = true;
        var block = Zeichnen(d, Dienste(mitPreisreihen: true));

        Zahlen(block)[0].Input("15");                            // SoC min
        Zahlen(block)[1].Input("85");                            // SoC max
        Zahlen(block)[2].Input("12");                            // Ladeleistung
        Zahlen(block)[3].Input("200");                           // Kapazitaet
        Zahlen(block)[4].Input("77");                            // Ladeschwelle
        Zahlen(block)[5].Input("4");                             // Kapitalzins
        Zahlen(block)[6].Input("20");                            // Nutzungsdauer
        Zahlen(block)[7].Input("130");                           // Leistungspreis
        Zahlen(block)[8].Input("3");                             // Netzladeaufschlag

        block.FindAll("select")[0].Change("1");                  // Betriebsart -> Graustrom
        block.FindAll("select")[1].Change("1");                  // Berechnungsart -> Nachtnutzung
        block.FindAll("input[type='checkbox']")[0].Change(true); // Kompatibilitaet
        block.FindAll("input[type='checkbox']")[1].Change(false);// Laden aus PV
        block.FindAll("input[type='checkbox']")[2].Change(true); // Laden aus BHKW
        block.FindAll("input[type='checkbox']")[3].Change(true); // Netzentladung
        block.FindAll("input[type='checkbox']")[5].Change(true); // Aufschlag
        block.FindAll("select")[2].Change("2");                  // Preisquelle -> Spotmarkt
        block.FindAll("select")[3].Change("7");                  // Preisreihe

        Assert.Equal(new List<(string, string)>
        {
            (SpeicherFeld.SoCMin, "15"),
            (SpeicherFeld.SoCMax, "85"),
            (SpeicherFeld.Leistung, "12"),
            (SpeicherFeld.Kapazitaet, "200"),
            (SpeicherFeld.Ladeschwelle, "77"),
            (SpeicherFeld.Kapitalzins, "4"),
            (SpeicherFeld.Nutzungsdauer, "20"),
            (SpeicherFeld.Leistungspreis, "130"),
            (SpeicherFeld.Netzladeaufschlag, "3"),
            (SpeicherFeld.Betriebsart, DbWerte.SP_BETRIEBSART_GRAUSTROM),
            (SpeicherFeld.Berechnungsart, DbWerte.SP_BERECHNUNG_NACHTNUTZUNG),
            (SpeicherFeld.Kompatibilitaet, "1"),
            (SpeicherFeld.LadenPv, "0"),
            (SpeicherFeld.LadenBhkw, "1"),
            (SpeicherFeld.Netzentladung, "1"),
            (SpeicherFeld.Aufschlag, "1"),
            (SpeicherFeld.Preisquelle, DbWerte.SP_PREISQUELLE_SPOTMARKT),
            (SpeicherFeld.Preisreihe, "7")
        }, _geschrieben);
    }

    /// <summary>
    /// Die ÜbERGABE ist der Arbeitsstand — sie wird mitgeführt (anders als im
    /// gepufferten Block, der sie unangetastet ließ). Nur so zeigt das Feld nach
    /// dem Reiterwechsel noch, was geschrieben wurde.
    /// </summary>
    [Fact]
    public void Die_Uebergabe_traegt_den_geschriebenen_Wert()
    {
        SpeicherParameterDaten uebergabe = Voll();
        var block = Zeichnen(uebergabe);

        Zahlen(block)[0].Input("15");

        Assert.Equal(15.0, uebergabe.SoCMinProzent);
    }

    /// <summary>
    /// <b>Ohne Schreibdienst schreibt kein Feld</b> — und dann sind die Felder auch
    /// gesperrt: Ein Eingabefeld, das nirgendwo ankommt, ist eine Attrappe (und war
    /// genau der Befund, der zu diesem Paket geführt hat).
    /// </summary>
    [Fact]
    public void Ohne_Schreibdienst_sind_die_Felder_gesperrt()
    {
        var block = Zeichnen(dienste: Dienste(mitSchreiben: false));

        Assert.Empty(block.FindAll("input:not([disabled])"));
        Assert.Empty(block.FindAll("select:not([disabled])"));
        Assert.Empty(_geschrieben);
    }

    // =====================================================================
    //  Die Rückmeldung
    // =====================================================================

    /// <summary>
    /// Jeder Schreibvorgang meldet sich — die Bestätigung steht ohne Warnfarbe in der
    /// Statuszeile. „Stumm gespeichert" war der Zustand, in dem der Anwender nicht
    /// merkte, dass nichts ankam.
    /// </summary>
    [Fact]
    public void Die_Bestaetigung_steht_ohne_Warnfarbe_in_der_Statuszeile()
    {
        var block = Zeichnen();
        Zahlen(block)[0].Input("15");

        var zeile = block.FindAll("p.epos-simerg-status")
                         .First(p => p.TextContent.Contains(Resource.SP_PARAM_MSG_GESPEICHERT));
        Assert.DoesNotContain("epos-simerg-warn", zeile.ClassName);
    }

    /// <summary>
    /// Ein Fehlschlag steht in der WARNFARBE da — und der eingegebene Wert bleibt im
    /// Feld stehen. Das Zahlenfeld meldet jede Taste: Die „9" auf dem Weg zur „95"
    /// ist ein Zwischenzustand, keine Fehleingabe; ein zurückgesetztes Feld risse dem
    /// Anwender die Eingabe unter den Fingern weg.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_steht_in_der_Warnfarbe_und_der_Wert_bleibt()
    {
        _antwort = new Rueckmeldung(false, Resource.SP_PARAM_MSG_SOC_BAND);

        SpeicherParameterDaten d = Voll();
        var block = Zeichnen(d);
        Zahlen(block)[0].Input("95");

        var zeile = block.FindAll("p.epos-simerg-status")
                         .First(p => p.TextContent.Contains(Resource.SP_PARAM_MSG_SOC_BAND));
        Assert.Contains("epos-simerg-warn", zeile.ClassName);

        Assert.Equal(95.0, d.SoCMinProzent);
        Assert.Equal("95", Zahlen(block)[0].GetAttribute("value"));
    }

    /// <summary>
    /// Die nächste, gelungene Eingabe löst die Warnung ab — geprüft am TEXT der
    /// Statuszeilen und an ihren Klassen (die Bandmeldung trägt ein „&lt;", das im
    /// Markup maskiert steht).
    /// </summary>
    [Fact]
    public void Der_naechste_gueltige_Wert_loest_die_Warnung_ab()
    {
        _antwort = new Rueckmeldung(false, Resource.SP_PARAM_MSG_SOC_BAND);

        var block = Zeichnen();
        Zahlen(block)[0].Input("95");
        Assert.Equal(Resource.SP_PARAM_MSG_SOC_BAND, block.Instance.Meldung);

        _antwort = new Rueckmeldung(true, Resource.SP_PARAM_MSG_GESPEICHERT);
        Zahlen(block)[0].Input("15");

        Assert.Equal(Resource.SP_PARAM_MSG_GESPEICHERT, block.Instance.Meldung);
        Assert.DoesNotContain(block.FindAll("p.epos-simerg-status"),
                              p => p.ClassName is string klassen && klassen.Contains("epos-simerg-warn"));
    }

    /// <summary>Vor dem ersten Schreibvorgang steht keine Meldung da.</summary>
    [Fact]
    public void Ohne_Schreibvorgang_steht_keine_Meldung()
    {
        var block = Zeichnen();

        Assert.Single(block.FindAll("p.epos-simerg-status"));       // nur der Variantenstatus
        Assert.Equal("", block.Instance.Meldung);
    }

    // =====================================================================
    //  Der Kopf des Blocks: Statuszeile, dann Knopf, dann Felder
    // =====================================================================

    /// <summary>
    /// <b>Der Optimierungsknopf steht OBEN</b> (W11b‑B‑29) — unter der Statuszeile und
    /// VOR den Feldern. Am Blockende hat der Anwender ihn nicht gefunden.
    /// </summary>
    [Fact]
    public void Der_Optimierungsknopf_steht_oben_unter_der_Statuszeile()
    {
        string markup = Zeichnen(optimierung: true).Markup;

        int status = markup.IndexOf("epos-simerg-status", StringComparison.Ordinal);
        int knopf = markup.IndexOf("epos-simerg-knopfzeile", StringComparison.Ordinal);
        int felder = markup.IndexOf("epos-simerg-felder", StringComparison.Ordinal);

        Assert.True(status >= 0 && status < knopf, "Die Statuszeile steht über der Knopfzeile.");
        Assert.True(knopf < felder, "Die Knopfzeile steht über den Feldern.");
    }

    /// <summary>Kein Sprungdelegat = kein Knopf (Regel seit W2.2).</summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Optimierungsknopf_weg()
    {
        Assert.Empty(Zeichnen().FindAll("button"));

        var mit = Zeichnen(optimierung: true);
        mit.FindAll("button").First(b => b.TextContent.Contains("optimieren")).Click();

        Assert.Equal(1, _optimierungen);
    }

    /// <summary>
    /// <b>Es gibt keine Speichern- und keine Verwerfen-Knöpfe mehr</b> (W11b‑B‑29):
    /// Der Optimierungsknopf ist der EINZIGE Knopf des Blocks.
    /// </summary>
    [Fact]
    public void Der_Block_hat_ausser_der_Optimierung_keinen_Knopf()
    {
        Assert.Single(Zeichnen(optimierung: true).FindAll("button"));
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
    /// <b>Die Gerätegröße</b> ist nur bei GENAU EINER Speicheranlage änderbar. Sonst
    /// bleibt sie gesperrt und trägt den bisherigen Hinweis: Varianten desselben
    /// Speichers teilen sich EINE Gerätekopie.
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
    /// Und sie schreibt über ihre EIGENEN Schlüssel: Kapazität und Leistung gehen in
    /// die ANLAGE (<c>Tab_Stromspeicher</c>) und nicht in die Variante — die Hülle
    /// nimmt dafür denselben Weg wie die Auslegungsoptimierung.
    /// </summary>
    [Fact]
    public void Die_Geraetegroesse_schreibt_ueber_Kapazitaet_und_Leistung()
    {
        SpeicherParameterDaten d = Voll();
        d.GeraetegroesseAenderbar = true;
        var block = Zeichnen(d);

        Zahlen(block)[3].Input("200");
        Assert.Equal((SpeicherFeld.Kapazitaet, "200"), Einziger);

        _geschrieben.Clear();
        Zahlen(block)[2].Input("50");
        Assert.Equal((SpeicherFeld.Leistung, "50"), Einziger);
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
    /// Der Wechsel der Preisquelle SCHREIBT die Quelle und holt Beschriftung UND Liste
    /// über den Lesedienst — aus „Preisreihe" wird „Kostenprofil". Die Reihen-Id
    /// selbst kommt aus der gespeicherten Variante und wird nicht noch einmal
    /// geschrieben.
    /// </summary>
    [Fact]
    public void Der_Preisquellenwechsel_schreibt_und_holt_Label_und_Liste()
    {
        SpeicherParameterDaten d = Voll();
        var block = Zeichnen(d, Dienste(mitPreisreihen: true));

        // [0] Betriebsart, [1] Berechnungsart, [2] Preisquelle, [3] Reihe
        block.FindAll("select")[2].Change("1");     // -> Kostenprofil

        Assert.Equal((SpeicherFeld.Preisquelle, DbWerte.SP_PREISQUELLE_PROFIL), Einziger);
        Assert.Equal("Kostenprofil", d.PreisreiheLabel);
        Assert.Equal(3, d.PreisreiheId);
        Assert.True(d.PreisreiheMoeglich);
        Assert.Contains("Profil Werk 1", block.Markup);
    }

    /// <summary>Ohne den Lesedienst bleibt die Liste, wie sie geliefert wurde.</summary>
    [Fact]
    public void Ohne_Preisreihendienst_bleibt_die_Liste_stehen()
    {
        SpeicherParameterDaten d = Voll();
        var block = Zeichnen(d);
        block.FindAll("select")[2].Change("2");     // -> Spotmarkt

        Assert.Equal((SpeicherFeld.Preisquelle, DbWerte.SP_PREISQUELLE_SPOTMARKT), Einziger);
        Assert.Equal("Preisreihe", d.PreisreiheLabel);
        Assert.False(d.PreisreiheMoeglich);
    }

    /// <summary>
    /// Der Kompatibilitätsmodus ist nur bei NACHTNUTZUNG wählbar — dieselbe Regel, die
    /// die Hülle beim Lesen anwendet. Sie muß hier mitlaufen, sonst bliebe der
    /// Schalter bis zum nächsten Lesen falsch gesperrt.
    /// </summary>
    [Fact]
    public void Die_Kompatibilitaet_haengt_an_der_Berechnungsart()
    {
        SpeicherParameterDaten d = Voll();
        var block = Zeichnen(d);

        // [0] Kompatibilitaet, [1..3] Quellen, [4] Ausbaustufe, [5] Aufschlag
        Assert.True(block.FindAll("input[type='checkbox']")[0].HasAttribute("disabled"));

        block.FindAll("select")[1].Change("1");     // Berechnungsart -> Nachtnutzung

        Assert.True(d.KompatibilitaetMoeglich);
        Assert.False(block.FindAll("input[type='checkbox']")[0].HasAttribute("disabled"));

        block.FindAll("select")[1].Change("2");     // -> Arbitrage
        Assert.False(d.KompatibilitaetMoeglich);
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

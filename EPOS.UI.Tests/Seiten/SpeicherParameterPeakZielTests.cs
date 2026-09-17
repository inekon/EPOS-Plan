using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die ZWEI PEAK-FELDER des Speicherparameterblocks (Entscheide LS-E-1 (a), LS-E-3).
///
/// <para><b>Soll:</b> Die Klappliste „Berechnungsart" bietet die Lastspitzenkappung als
/// DRITTEN Eintrag an. Wählt der Anwender sie, erscheinen Peak-Ziel [kW] und das Häkchen
/// „adaptiv" — bei jeder anderen Art stehen sie NICHT da, weil sie dort keine Wirkung
/// hätten. Beide schreiben SOFORT, wie jedes andere Feld des Blocks (W11b‑B‑29). Die
/// Herleitungszeile darunter nennt die Netzbezugsspitze des letzten Laufs; ohne Lauf
/// steht dort nichts.</para>
/// </summary>
public class SpeicherParameterPeakZielTests : EposBunitContext
{
    private readonly Kulturvorrichtung _kultur = new();

    private readonly List<(string Feld, string Wert)> _geschrieben = new();

    private Rueckmeldung _antwort;

    public SpeicherParameterPeakZielTests()
    {
        _antwort = new Rueckmeldung(true, Resource.SP_PARAM_MSG_GESPEICHERT);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _kultur.Dispose();
    }

    // =====================================================================
    // Probendaten
    // =====================================================================

    /// <summary>Ein Stand mit allen DREI Berechnungsarten in der Klappliste.</summary>
    private static SpeicherParameterDaten Daten(bool kappung) => new SpeicherParameterDaten
    {
        VarianteVorhanden = true,
        Variantenstatus = "Aktive Variante: Speicher 1",
        SoCMinProzent = 10,
        SoCMaxProzent = 90,
        Betriebsart = DbWerte.SP_BETRIEBSART_GRAUSTROM,
        Berechnungsart = kappung
            ? DbWerte.SP_BERECHNUNG_PEAKSHAVING
            : DbWerte.SP_BERECHNUNG_DAUERNUTZUNG,
        Betriebsarten = new[]
        {
            new Steuerwahl(DbWerte.SP_BETRIEBSART_GRUENSTROM, "Grünstrom"),
            new Steuerwahl(DbWerte.SP_BETRIEBSART_GRAUSTROM, "Graustrom")
        },
        Berechnungsarten = new[]
        {
            new Steuerwahl(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG, "Dauernutzung"),
            new Steuerwahl(DbWerte.SP_BERECHNUNG_ARBITRAGE, "Preissteuerung / Arbitrage"),
            new Steuerwahl(DbWerte.SP_BERECHNUNG_PEAKSHAVING, "Lastspitzenkappung")
        },
        PeakZielMoeglich = kappung,
        PeakZiel = kappung ? 80.0 : 0.0,
        PeakHerleitung = kappung ? "Netzbezugsspitze ohne Speicher im letzten Lauf: 96,4 kW." : "",
        Preisquelle = DbWerte.SP_PREISQUELLE_FIXPREIS,
        Preisquellen = new[]
        {
            new Steuerwahl(DbWerte.SP_PREISQUELLE_FIXPREIS, "Fixpreis")
        },
        Preisreihen = Array.Empty<(int, string)>()
    };

    private SimulationErgebnisDienste Dienste()
        => new SimulationErgebnisDienste
        {
            SpeicherfeldSchreiben = (feld, wert) =>
            {
                _geschrieben.Add((feld, wert));
                return _antwort;
            }
        };

    private IRenderedComponent<SpeicherParameterBlock> Zeichnen(SpeicherParameterDaten daten)
        => Render<SpeicherParameterBlock>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Dienste, Dienste());
        });

    // =====================================================================
    // 1 — Die Klappliste
    // =====================================================================

    /// <summary>
    /// Die Klappliste „Berechnungsart" führt DREI Einträge, und der dritte ist die
    /// Lastspitzenkappung. Sie ist die zweite Klappliste des Blocks (nach der
    /// Betriebsart).
    /// </summary>
    [Fact]
    public void Die_Klappliste_fuehrt_die_Lastspitzenkappung_als_dritten_Eintrag()
    {
        var block = Zeichnen(Daten(kappung: false));

        var art = block.FindAll("select")[1];
        Assert.Equal(3, art.QuerySelectorAll("option").Length);
        Assert.Equal("Lastspitzenkappung", art.QuerySelectorAll("option")[2].TextContent);
    }

    /// <summary>
    /// Die Wahl schreibt SOFORT — mit dem Persistenzwert, nicht mit dem Anzeigetext.
    /// </summary>
    [Fact]
    public void Die_Wahl_der_Lastspitzenkappung_schreibt_sofort()
    {
        var block = Zeichnen(Daten(kappung: false));

        block.FindAll("select")[1].Change("2");

        Assert.Equal((SpeicherFeld.Berechnungsart, DbWerte.SP_BERECHNUNG_PEAKSHAVING),
                     Assert.Single(_geschrieben));
    }

    // =====================================================================
    // 2 — Die zwei Felder stehen nur bei dieser Art
    // =====================================================================

    /// <summary>
    /// Bei DAUERNUTZUNG steht weder das Peak-Ziel noch das Häkchen da — und auch die
    /// Herleitungszeile nicht. Ein Feld ohne Wirkung wäre eine Einladung, es zu pflegen.
    /// </summary>
    [Fact]
    public void Ohne_Lastspitzenkappung_stehen_die_Peakfelder_nicht_da()
    {
        var block = Zeichnen(Daten(kappung: false));

        Assert.DoesNotContain(Resource.SP_PARAM_LABEL_PEAKZIEL, block.Markup);
        Assert.DoesNotContain(Resource.SP_PARAM_LABEL_PEAKZIEL_ADAPTIV, block.Markup);
        Assert.DoesNotContain("Netzbezugsspitze", block.Markup);
    }

    /// <summary>
    /// Mit der Lastspitzenkappung stehen beide Felder da, das Ziel trägt seinen Wert und
    /// seine Einheit, und die Herleitungszeile nennt die Netzbezugsspitze des letzten
    /// Laufs — die Messlatte, unter der das Ziel liegen muss (LS-E-3).
    /// </summary>
    [Fact]
    public void Mit_Lastspitzenkappung_stehen_Ziel_Haekchen_und_Herleitung_da()
    {
        var block = Zeichnen(Daten(kappung: true));

        Assert.Contains(Resource.SP_PARAM_LABEL_PEAKZIEL, block.Markup);
        Assert.Contains(Resource.SP_PARAM_LABEL_PEAKZIEL_ADAPTIV, block.Markup);
        Assert.Contains("96,4 kW", block.Markup);
        Assert.Contains("kW", block.Markup);
    }

    /// <summary>
    /// Der Wechsel auf die Lastspitzenkappung lässt die zwei Felder SOFORT erscheinen —
    /// ohne neues Lesen. Ohne das Nachziehen stünde die Maske bis zum nächsten
    /// Reiterwechsel leer da.
    /// </summary>
    [Fact]
    public void Der_Wechsel_auf_die_Kappung_zeigt_die_Felder_sofort()
    {
        SpeicherParameterDaten daten = Daten(kappung: false);
        var block = Zeichnen(daten);

        block.FindAll("select")[1].Change("2");

        Assert.True(daten.PeakZielMoeglich);
        Assert.Contains(Resource.SP_PARAM_LABEL_PEAKZIEL, block.Markup);
    }

    // =====================================================================
    // 3 — Beide Felder schreiben sofort
    // =====================================================================

    /// <summary>
    /// Das Peak-Ziel schreibt SOFORT und INVARIANT — auch mit Komma eingegeben.
    /// </summary>
    [Fact]
    public void Das_Peakziel_schreibt_sofort_und_invariant()
    {
        SpeicherParameterDaten daten = Daten(kappung: true);
        var block = Zeichnen(daten);

        // Markupreihenfolge: SoC min, SoC max, Leistung, Kapazitaet, Ladeschwelle,
        // PEAK-ZIEL (Gruppe Betriebsfuehrung), dann die vier Wirtschaftswerte.
        block.FindAll("input[type='text']")[5].Input("62,5");

        Assert.Equal((SpeicherFeld.PeakZiel, "62.5"), Assert.Single(_geschrieben));
        Assert.Equal(62.5, daten.PeakZiel);
    }

    /// <summary>Das Häkchen „adaptiv" ebenso — als „1"/„0", wie jeder Schalter.</summary>
    [Fact]
    public void Das_Haekchen_adaptiv_schreibt_sofort()
    {
        SpeicherParameterDaten daten = Daten(kappung: true);
        var block = Zeichnen(daten);

        block.FindAll("input[type='checkbox']")[0].Change(true);

        Assert.Equal((SpeicherFeld.PeakZielAdaptiv, "1"), Assert.Single(_geschrieben));
        Assert.True(daten.PeakZielAdaptiv);
    }

    /// <summary>
    /// GEGENPROBE: Steht „adaptiv", ist das Zielfeld GESPERRT — die Schwelle zieht sich
    /// dann selbst nach, und eine Eingabe dort wäre eine Zahl ohne Wirkung.
    /// </summary>
    [Fact]
    public void Mit_adaptiv_ist_das_Zielfeld_gesperrt()
    {
        SpeicherParameterDaten daten = Daten(kappung: true);
        daten.PeakZielAdaptiv = true;

        var block = Zeichnen(daten);

        Assert.True(block.FindAll("input[type='text']")[5].HasAttribute("disabled"));
    }
}

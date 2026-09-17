using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der KNOPF „In Variante übernehmen" der Maske „Lastspitzenkappung"
/// (Entscheid LS-E-1 (a), Anwenderbefund 17.09.2026).
///
/// <para><b>Die Lage im Bestand.</b> Die Maske rechnete die Kappung und vergaß ihr
/// Ergebnis: Es gab keinen Weg von ihr in den Projektlauf. Der Knopf ist dieser Weg —
/// er schreibt Berechnungsart, Zielschwelle und Adaptiv-Flag in die AKTIVE
/// Speichervariante.</para>
///
/// <para><b>Soll:</b> Ohne Delegat gibt es den Knopf nicht (etwa ohne Projekt). Ohne
/// gerechnetes Ergebnis schreibt er nichts und sagt es. Führt die Variante bereits die
/// Lastspitzenkappung, schreibt er ohne Rückfrage; führt sie eine ANDERE Art, fragt er
/// vorher — sonst tauschte der Anwender seinen Rechenweg ungewollt aus. Übernommen wird
/// die ERREICHTE Schwelle des Laufs, nicht die Eingabe.</para>
/// </summary>
public class PeakShavingVarianteTests : EposBunitContext
{
    private readonly List<(double Ziel, bool Adaptiv)> _geschrieben = new();

    public PeakShavingVarianteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---------------------------------------------------------------- Daten

    /// <summary>Tagesgang mit einer klaren Spitze — wie in <c>PeakShavingDialogTests</c>.</summary>
    private static double[] Lastgang()
    {
        double[] w = new double[35040];
        for (int i = 0; i < w.Length; i++)
            w[i] = 100.0 + 40.0 * Math.Sin(2.0 * Math.PI * i / 96.0);
        for (int i = 1000; i < 1040; i++) w[i] = 400.0;
        return w;
    }

    private static Task<PeakShavingErgebnis> Rechnen(double[] reihe, PeakShavingEingaben e)
        => Task.FromResult(new PeakShaving(e.AlsPeakShavingParameter(), e.Modus)
                               .BerechnePeakShaving(reihe, e.AlsSpeicherParameter()));

    private IRenderedComponent<PeakShavingDialog> Zeige(bool mitUebernahme = true,
                                                        string? aktuelleArt = null)
        => Render<PeakShavingDialog>(p => p
            .Add(x => x.Ganglinien, new[] { (0, "Werk Nord  [Projekt]") })
            .Add(x => x.Vorgaben, new PeakShavingVorbelegung
            {
                Bezeichner = "Speicher A",
                LeistungspreisEurProKwA = 120.0,
                BezugspreisMittelCtKwh = 25.0
            })
            .Add(x => x.Werte, i => Task.FromResult(Lastgang()))
            .Add(x => x.Rechnen, Rechnen)
            .Add(x => x.VarianteUebernehmen, mitUebernahme
                ? (ziel, adaptiv) =>
                  {
                      _geschrieben.Add((ziel, adaptiv));
                      return Task.FromResult(true);
                  }
                : null)
            .Add(x => x.VariantenBerechnungsart, mitUebernahme
                ? () => Task.FromResult<string?>(
                    aktuelleArt ?? Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG)
                : null));

    private static IElement Rechenknopf(IRenderedComponent<PeakShavingDialog> cut)
        => cut.Find(".epos-peakshaving-rechnen button");

    private static IElement Uebernahmeknopf(IRenderedComponent<PeakShavingDialog> cut)
    {
        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        foreach (IElement knopf in leisten[leisten.Count - 1].QuerySelectorAll("button"))
            if (knopf.TextContent.Trim() == Resource.PEAK_BTN_VARIANTE) return knopf;

        throw new Xunit.Sdk.XunitException(
            "Der Knopf fuer die Uebernahme in die Variante steht nicht in der Fussleiste.");
    }

    // =====================================================================

    /// <summary>OHNE Delegat gibt es den Knopf nicht — er wäre eine Attrappe.</summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Knopf_weg()
    {
        var cut = Zeige(mitUebernahme: false);

        Assert.DoesNotContain(Resource.PEAK_BTN_VARIANTE, cut.Markup);
    }

    /// <summary>Mit Delegat steht er in der Fußleiste, neben „Schließen".</summary>
    [Fact]
    public void Mit_Delegat_steht_der_Knopf_in_der_Fussleiste()
    {
        var cut = Zeige();

        Assert.NotNull(Uebernahmeknopf(cut));
    }

    /// <summary>
    /// OHNE gerechnetes Ergebnis schreibt er nichts und meldet es. Es gäbe auch nichts
    /// zu übernehmen: Die erreichte Schwelle entsteht erst im Lauf.
    /// </summary>
    [Fact]
    public void Ohne_Ergebnis_schreibt_der_Knopf_nichts()
    {
        var cut = Zeige();

        Uebernahmeknopf(cut).Click();

        Assert.Empty(_geschrieben);
        Assert.Equal(Resource.PEAK_MSG_KEIN_ERGEBNIS, cut.Instance.Meldung);
    }

    /// <summary>
    /// FÜHRT DIE VARIANTE EINE ANDERE ART, wird gefragt — und erst das „Ja" schreibt.
    /// Die Frage nennt die Art, die dabei verlorenginge.
    /// </summary>
    [Fact]
    public void Eine_abweichende_Art_wird_erst_erfragt_und_dann_geschrieben()
    {
        var cut = Zeige(aktuelleArt: Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE);
        Rechenknopf(cut).Click();

        Uebernahmeknopf(cut).Click();

        // Erst die Rückfrage, noch kein Schreibvorgang.
        Assert.Empty(_geschrieben);
        Assert.Contains(Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE, cut.Markup);

        cut.Find(".epos-rueckfrage button").Click();   // „Ja"

        (double ziel, bool adaptiv) = Assert.Single(_geschrieben);
        Assert.True(ziel > 0.0);
        Assert.True(adaptiv);                           // die Maske startet adaptiv

        // Die Bestaetigung nennt die uebernommene Schwelle.
        Assert.Contains(ziel.ToString("0.#", System.Globalization.CultureInfo.CurrentCulture),
                        cut.Instance.Meldung);
    }

    /// <summary>
    /// FÜHRT DIE VARIANTE SCHON DIE LASTSPITZENKAPPUNG, wird NICHT gefragt: Es geht
    /// kein Rechenweg verloren, und eine Frage ohne Entscheidung ist nur ein Klick mehr.
    /// Übernommen wird die ERREICHTE Schwelle des Laufs.
    /// </summary>
    [Fact]
    public void Dieselbe_Art_wird_ohne_Rueckfrage_geschrieben()
    {
        var cut = Zeige(aktuelleArt: Resource.SP_BERECHNUNG_ANZEIGE_PEAKSHAVING);
        Rechenknopf(cut).Click();

        Uebernahmeknopf(cut).Click();

        (double ziel, bool _) = Assert.Single(_geschrieben);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));

        // Die erreichte Schwelle des Laufs, nicht die Eingabe: Der adaptive Lauf startet
        // bei 0 und zieht nach — die Zahl steht danach im Ergebnis.
        Assert.True(ziel > 0.0);
    }

    /// <summary>
    /// GEGENPROBE: Wer die Rückfrage mit „Nein" beantwortet, hat nichts geschrieben —
    /// die Variante behält ihren Rechenweg.
    /// </summary>
    [Fact]
    public void Ein_Nein_auf_die_Rueckfrage_schreibt_nichts()
    {
        var cut = Zeige(aktuelleArt: Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE);
        Rechenknopf(cut).Click();
        Uebernahmeknopf(cut).Click();

        cut.FindAll(".epos-rueckfrage button")[1].Click();   // „Nein"

        Assert.Empty(_geschrieben);
    }

    /// <summary>
    /// Ohne aktive Variante (der Leser liefert <c>null</c>) schreibt der Knopf nichts
    /// und sagt, woran es liegt.
    /// </summary>
    [Fact]
    public void Ohne_aktive_Variante_meldet_der_Knopf_den_Grund()
    {
        var cut = Render<PeakShavingDialog>(p => p
            .Add(x => x.Ganglinien, new[] { (0, "Werk Nord  [Projekt]") })
            .Add(x => x.Vorgaben, new PeakShavingVorbelegung { Bezeichner = "Speicher A" })
            .Add(x => x.Werte, i => Task.FromResult(Lastgang()))
            .Add(x => x.Rechnen, Rechnen)
            .Add(x => x.VarianteUebernehmen, (ziel, adaptiv) =>
            {
                _geschrieben.Add((ziel, adaptiv));
                return Task.FromResult(true);
            })
            .Add(x => x.VariantenBerechnungsart, () => Task.FromResult<string?>(null)));

        Rechenknopf(cut).Click();
        Uebernahmeknopf(cut).Click();

        Assert.Empty(_geschrieben);
        Assert.Equal(Resource.PEAK_MSG_VARIANTE_KEINE, cut.Instance.Meldung);
    }
}

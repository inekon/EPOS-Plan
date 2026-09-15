using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// DIE HERLEITUNG DER INVESTITION unter der Jahresprojektion (Auftrag #249,
/// Anwenderentscheid vom 13.09.2026).
///
/// <para><b>Anlass.</b> Die Ergebnisansicht nannte unter „Jahresprojektion" nur die
/// Summe. Der Anwender rechnete am 12.09.2026 nach — 150 €/kWh mal 1 395 kWh sind
/// 209 250 €, im Ergebnis standen 284 250 € — und fragte nach dem Unterschied. Er
/// steckt im festen und im leistungsbezogenen Anteil; die Herleitung zeigt beide.</para>
///
/// <para><b>Geprüft wird über die KOMPONENTE, nicht über den DOM-Text</b>
/// (<c>FindComponents&lt;Herleitungszeile&gt;()</c>): Wie viele Zeilen stehen da, welche
/// Texte tragen sie, und stehen die formatierten Zahlen der Einheit in der Formel. Eine
/// Textsuche im Markup fände dieselben Ziffern auch in einer Kachel.</para>
/// </summary>
public sealed class SpeicherFlottenErgebnisHerleitungTests : EposBunitContext
{
    private static string Komma(string zahl)
        => zahl.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

    /// <summary>
    /// Zwei Einheiten: zwei Herleitungszeilen mit ihren Namen, darunter die Summenzeile
    /// und EINMAL der Hinweis zur Leistung.
    /// </summary>
    [Fact]
    public void Zwei_Einheiten_tragen_zwei_Herleitungszeilen_und_eine_Summe()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        string[] texte = cut.FindComponents<Herleitungszeile>()
            .Select(z => z.Instance.Text).ToArray();

        Assert.Contains("Speicher A", texte);
        Assert.Contains("Speicher B", texte);
        Assert.Contains(Resource.FLOTTE_PROJ_INVEST_SUMME, texte);

        // Der Hinweis steht EINMAL unter der Gruppe, nicht je Einheit.
        Assert.Equal(1, texte.Count(t => t == Resource.FLOTTE_PROJ_INVEST_LEISTUNG_HINWEIS));

        // Zwei Einheiten + Summe + Hinweis.
        Assert.Equal(4, texte.Length);
    }

    /// <summary>
    /// Die Formel führt die Zahlen DIESER Einheit: fester Anteil, Kapazität mal Satz,
    /// die größere Leistung mal Satz — und die Summe aus der Formel des Rechenkerns.
    /// </summary>
    [Fact]
    public void Die_Formel_nennt_die_drei_Anteile_der_Einheit()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        string formelA = cut.FindComponents<Herleitungszeile>()
            .Single(z => z.Instance.Text == "Speicher A").Instance.Formel;

        // 1 000 € fest + 24 kWh × 150 €/kWh + 12 kW × 60 €/kW = 5 320 €
        Assert.Contains(Komma("1.000,00"), formelA);
        Assert.Contains(Komma("24,00"), formelA);
        Assert.Contains("150", formelA);
        Assert.Contains(Komma("12,00"), formelA);   // die GRÖSSERE der beiden Leistungen
        Assert.Contains("60", formelA);
        Assert.Contains(Komma("5.320,00"), formelA);

        // Die Sätze stehen ohne Nachkommanullen: „150 €/kWh", nicht „150,00 €/kWh".
        Assert.DoesNotContain(Komma("150,00"), formelA);
    }

    /// <summary>Die Summenzeile nennt die Summe der beiden Einheiten.</summary>
    [Fact]
    public void Die_Summenzeile_nennt_die_Summe_der_Einheiten()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        string summe = cut.FindComponents<Herleitungszeile>()
            .Single(z => z.Instance.Text == Resource.FLOTTE_PROJ_INVEST_SUMME).Instance.Formel;

        // 5 320 € + 3 700 € = 9 020 €
        Assert.Contains(Komma("9.020,00"), summe);
    }

    /// <summary>
    /// EINE Einheit: eine Herleitungszeile, KEINE Summenzeile — die Summe steht schon
    /// in der Investitionszeile darüber.
    /// </summary>
    [Fact]
    public void Eine_Einheit_traegt_keine_Summenzeile()
    {
        SpeicherFlottenErgebnis e = Ergebnis();
        e.Konfiguration.Einheiten.RemoveAt(1);

        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, e));

        string[] texte = cut.FindComponents<Herleitungszeile>()
            .Select(z => z.Instance.Text).ToArray();

        Assert.Contains("Speicher A", texte);
        Assert.DoesNotContain(Resource.FLOTTE_PROJ_INVEST_SUMME, texte);
        Assert.Contains(Resource.FLOTTE_PROJ_INVEST_LEISTUNG_HINWEIS, texte);
        Assert.Equal(2, texte.Length);
    }

    /// <summary>Ohne Einheiten steht keine einzige Herleitungszeile da.</summary>
    [Fact]
    public void Ohne_Einheiten_steht_keine_Herleitung()
    {
        SpeicherFlottenErgebnis e = Ergebnis();
        e.Konfiguration.Einheiten.Clear();

        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, e));

        Assert.Empty(cut.FindComponents<Herleitungszeile>());
    }

    // =====================================================================
    // Prüfstand
    // =====================================================================

    /// <summary>
    /// Zwei Einheiten mit VERSCHIEDENEN Kostensätzen und getrennten Richtungsleistungen,
    /// dazu ein Jahreskonto — ohne Wirtschaftlichkeit gäbe es die Gruppe
    /// „Jahresprojektion" nicht, unter der die Herleitung steht.
    /// </summary>
    private static SpeicherFlottenErgebnis Ergebnis() => new()
    {
        Erfolg = true,
        Konfiguration = new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit>
            {
                new()
                {
                    Id = "a", Name = "Speicher A", KapazitaetKWh = 24,
                    LadeleistungKw = 10, EntladeleistungKw = 12,
                    InvestitionEuro = 1000, InvestitionEuroProKWh = 150, InvestitionEuroProKw = 60
                },
                new()
                {
                    Id = "b", Name = "Speicher B", KapazitaetKWh = 16,
                    LadeleistungKw = 9, EntladeleistungKw = 7,
                    InvestitionEuro = 500, InvestitionEuroProKWh = 200, InvestitionEuroProKw = 0
                }
            },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                Verteilung = FlottenVerteilung.Kaskade
            },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            { Kalkulationszins = 0.03, ProjektjahreBeiWiederholung = 20 }
        },
        Studie = new FlottenStudienErgebnis
        {
            Variante = new FlottenSimulationErgebnis { Zulaessig = true, MaximalerNetzbezugKw = 100 },
            ReferenzOhneSpeicher = new FlottenSimulationErgebnis { Zulaessig = true, MaximalerNetzbezugKw = 120 },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitErgebnis
            {
                InvestitionEuro = 9020,
                KapitalwertEuro = 3140,
                Jahreskonten = new List<FlottenJahreskonto>
                {
                    new() { Jahr = 1, OpexEuro = 120, DurchsatzkostenEuro = 40, NettoCashflowEuro = 1400 }
                }
            }
        }
    };
}

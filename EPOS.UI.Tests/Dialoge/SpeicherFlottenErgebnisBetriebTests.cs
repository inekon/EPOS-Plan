using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// DAS ERGEBNIS DER SPEICHERFLOTTE (Auftrag #184, Paket P2 des Konzepts
/// „Stromspeicher-Dialoge", Anwenderentscheide SD‑Q6 und SD‑Q7 vom 11.09.2026).
///
/// <para>Geprüft wird die Vollfassung 2.2/2.3: vier Kennzahlkacheln, die Δ-Spalte
/// samt Farbklasse, die eingeklappten Nullzeilen, die Jahresprojektion als BILD, die
/// Steuerzeile der Hausregel § 5 über dem Netzbild (sortiert, Reihenwahl, Zeitraum,
/// Datenzoom) — und dass der Betriebseditor hier NICHT mehr steht.</para>
///
/// <para><b>Wie „kommt der Schalter am Bild an?" geprüft wird.</b> Ein PNG lässt sich
/// nicht befragen. Die Komponente legt deshalb ihren Bildauftrag offen
/// (<see cref="SpeicherFlottenErgebnisAnsicht.Bildschluessel"/>, derselbe Schlüssel wie
/// auf der Ergebnisseite): Ändert ein Schalter den Schlüssel, ist er beim Kern
/// angekommen. Dass der Kern ihn an den Renderer durchreicht, zeigt
/// <c>EPOS.Kern.Tests/SpeicherFlottenAnzeigeCtrlTests</c> über die Bildbytes.</para>
/// </summary>
public sealed class SpeicherFlottenErgebnisBetriebTests : EposBunitContext
{
    private static string Komma(string zahl)
        => zahl.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

    // =====================================================================
    // Kopfzeile, Kacheln und Banner
    // =====================================================================

    [Fact]
    public void Ergebnis_erklaert_den_berechneten_Betrieb_und_nicht_erreichten_Peak()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));
        string text = cut.Markup;

        Assert.Contains(Resource.FLOTTE_ZIEL_PEAKSHAVING, text, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_VERT_KASKADE, text, StringComparison.Ordinal);
        Assert.Contains(Komma("789,36"), text, StringComparison.Ordinal);
        Assert.Contains(cut.FindAll("*"), x => x.TextContent.Contains("Peak-Ziel wurde nicht erreicht"));
        Assert.Contains(Resource.FLOTTE_VGL_EINSPEISUNG_PV, text, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_VGL_EINSPEISUNG_BHKW, text, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_VGL_EINSPEISUNG_BATTERIE, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Geaenderte_Eingaben_werden_auch_in_der_gemeinsamen_Ergebnisansicht_markiert()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p
            .Add(x => x.Ergebnis, Ergebnis()).Add(x => x.Veraltet, true));

        Assert.Contains(cut.FindAll("*"), x => x.TextContent.Contains("Diese Ergebnisse gehören zum vorherigen Stand"));
    }

    /// <summary>Vier Kacheln, keine Tabelle (Konzept 2.2 Punkt 1).</summary>
    [Fact]
    public void Vier_Kennzahlkacheln_stehen_oben()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        var kacheln = cut.FindAll(".epos-kennzahlkachel");
        Assert.Equal(4, kacheln.Count);

        string alle = string.Join(" | ", kacheln.Select(k => k.TextContent));
        Assert.Contains(Resource.FLOTTE_KACHEL_KAPITALWERT, alle, StringComparison.Ordinal);
        Assert.Contains(Komma("3.140,00"), alle, StringComparison.Ordinal);   // Kapitalwert
        Assert.Contains(Komma("120,00"), alle, StringComparison.Ordinal);     // Spitze vorher
        Assert.Contains(Komma("100,00"), alle, StringComparison.Ordinal);     // Spitze nachher
        Assert.Contains(Resource.FLOTTE_STATUS_ERREICHT, alle, StringComparison.Ordinal);
        Assert.Contains(Komma("800,00"), alle, StringComparison.Ordinal);     // Ersparnis
        Assert.Contains(Komma("34,2"), alle, StringComparison.Ordinal);       // Vollzyklen A
        Assert.Contains(Komma("25,6"), alle, StringComparison.Ordinal);       // Vollzyklen B
    }

    // =====================================================================
    // Die Δ-Spalte und die Nullzeilen
    // =====================================================================

    [Fact]
    public void Die_Vergleichstabelle_traegt_drei_Spalten_und_ein_Delta()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        var kopf = cut.FindAll("table.epos-flotte-vergleich thead th").Select(x => x.TextContent.Trim()).ToArray();
        Assert.Equal(new[]
        {
            Resource.FLOTTE_VGL_SP_KENNZAHL, Resource.FLOTTE_VGL_SP_EINHEIT,
            Resource.FLOTTE_VGL_SP_OHNE, Resource.FLOTTE_VGL_SP_MIT, Resource.FLOTTE_VGL_SP_DELTA
        }, kopf);

        // Die Zeile der Bezugsspitze: 120 → 100, Δ = −20 und das ist eine Verbesserung.
        var zeile = cut.FindAll("table.epos-flotte-vergleich tbody tr")
            .Single(r => r.TextContent.Contains(Resource.FLOTTE_VGL_SPITZE, StringComparison.Ordinal));
        var zellen = zeile.QuerySelectorAll("td").Select(x => x.TextContent.Trim()).ToArray();

        Assert.Equal("kW", zellen[0]);
        Assert.Equal(Komma("120,00"), zellen[1]);
        Assert.Equal(Komma("100,00"), zellen[2]);
        Assert.Equal(Komma("-20,00"), zellen[3]);
        Assert.Contains("epos-flotte-delta--besser",
                        zeile.QuerySelectorAll("td")[3].ClassName ?? "", StringComparison.Ordinal);
    }

    /// <summary>Mehr Netzbezug ist schlechter — und trägt die andere Farbklasse.</summary>
    [Fact]
    public void Ein_schlechteres_Delta_traegt_die_andere_Farbklasse()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        var zeile = cut.FindAll("table.epos-flotte-vergleich tbody tr")
            .Single(r => r.TextContent.Contains(Resource.FLOTTE_VGL_NETZBEZUG, StringComparison.Ordinal));

        Assert.Contains("epos-flotte-delta--schlechter",
                        zeile.QuerySelectorAll("td")[3].ClassName ?? "", StringComparison.Ordinal);
    }

    /// <summary>Nullzeilen stehen hinter einem Aufklapper und NICHT in der Haupttabelle.</summary>
    [Fact]
    public void Nullzeilen_stehen_eingeklappt()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        var aufklapper = cut.Find("details.epos-flotte-nullzeilen");
        Assert.False(aufklapper.HasAttribute("open"));
        Assert.Contains(Resource.FLOTTE_VGL_EINSPEISUNG_PV, aufklapper.TextContent, StringComparison.Ordinal);

        var haupttabelle = cut.FindAll("table.epos-flotte-vergleich").First();
        Assert.DoesNotContain(Resource.FLOTTE_VGL_EINSPEISUNG_PV, haupttabelle.TextContent, StringComparison.Ordinal);
    }

    // =====================================================================
    // Jahresprojektion als Bild
    // =====================================================================

    [Fact]
    public void Die_Jahresprojektion_ist_ein_Bild_mit_aufklappbarer_Tabelle()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        Assert.Contains(cut.FindAll("img"), b => b.GetAttribute("alt") == Resource.FLOTTE_ALT_PROJEKTION);

        var tabelle = cut.Find("details.epos-flotte-jahreskonten");
        Assert.False(tabelle.HasAttribute("open"));
        Assert.Contains(Resource.FLOTTE_PROJ_SP_CASHFLOW, tabelle.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Eine_abgewaehlte_Projektionsreihe_zeichnet_das_Bild_neu()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        string vorher = Bildquelle(cut, Resource.FLOTTE_ALT_PROJEKTION);

        await Schalten(cut, Resource.FLOTTE_R_KUMULIERT, false);

        cut.WaitForAssertion(() =>
            Assert.NotEqual(vorher, Bildquelle(cut, Resource.FLOTTE_ALT_PROJEKTION)));
    }

    // =====================================================================
    // Die Steuerzeile der Hausregel 5
    // =====================================================================

    [Fact]
    public void Ueber_dem_Netzbild_steht_je_Reihe_ein_Schalter_mit_der_Legendenressource()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        string[] schalter = cut.FindAll("label.epos-schalter .epos-feld-text")
                               .Select(x => x.TextContent.Trim()).ToArray();

        Assert.Contains(Resource.SIM_CHK_SORTIERT, schalter);
        Assert.Contains(Resource.OPT_BETRIEB_R_OHNE, schalter);
        Assert.Contains(Resource.OPT_BETRIEB_R_MIT, schalter);
        Assert.Contains(Resource.FLOTTE_R_GESAMT, schalter);
        Assert.Contains(Resource.FLOTTE_R_PEAKZIEL, schalter);
        Assert.Contains(string.Format(CultureInfo.CurrentCulture, Resource.FLOTTE_R_EINHEIT, "Speicher A"), schalter);
        Assert.Contains(string.Format(CultureInfo.CurrentCulture, Resource.FLOTTE_R_SOC, "Speicher A"), schalter);
        Assert.Contains(Resource.FLOTTE_CHK_ZWEITES_BILD, schalter);

        // Vorbelegt ist ALLES an (Hausregel 5 Punkt 2).
        Assert.Equal(SpeicherFlottenAnzeigeCtrl.Betriebsreihen(Vollstaendig()).Count,
                     cut.Instance.GewaehlteReihen.Count);
    }

    [Fact]
    public async Task Ein_Reihenschalter_blendet_die_Reihe_aus_und_der_Bildauftrag_traegt_es()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        string vorher = cut.Instance.Bildschluessel;

        await Schalten(cut, Resource.FLOTTE_R_GESAMT, false);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(SpeicherFlottenAnzeigeCtrl.REIHE_GESAMT, cut.Instance.GewaehlteReihen);
            Assert.NotEqual(vorher, cut.Instance.Bildschluessel);
        });
    }

    [Fact]
    public async Task Der_Schalter_sortiert_setzt_das_Flag_und_zeichnet_neu()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        string vorher = cut.Instance.Bildschluessel;
        Assert.False(cut.Instance.Sortiert);

        await Schalten(cut, Resource.SIM_CHK_SORTIERT, true);

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.Sortiert);
            Assert.NotEqual(vorher, cut.Instance.Bildschluessel);
        });
    }

    /// <summary>SD‑Q7: Jahr / Woche / Tag mit Navigator; Vorgabe ist Woche 1.</summary>
    [Fact]
    public async Task Die_Zeitraumwahl_verschiebt_das_Fenster()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        Assert.Equal(7, cut.Instance.Tage);
        Assert.Equal(0, cut.Instance.StartTag);

        // Ein Schritt vor: Woche 2 beginnt am achten Tag.
        await cut.FindAll("button.epos-gang-knopf")[1].ClickAsync(new());
        cut.WaitForAssertion(() => Assert.Equal(7, cut.Instance.StartTag));

        // Stufe „Tag": ein Tag ab dem Beginn.
        await Stufe(cut, Resource.BERG_STUFE_TAG);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, cut.Instance.Tage);
            Assert.Equal(0, cut.Instance.StartTag);
        });

        // Stufe „Jahr": kein Navigator mehr.
        await Stufe(cut, Resource.BERG_STUFE_JAHR);
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(366, cut.Instance.Tage);
            Assert.Empty(cut.FindAll("button.epos-gang-knopf"));
        });
    }

    /// <summary>Der Datenzoom hängt an JEDER Jahresganglinie (Hausregel § 5.1).</summary>
    [Fact]
    public void Das_Netzbild_traegt_den_Datenzoom()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        var bild = cut.FindComponents<EPOS.UI.Standards.ChartBild>()
                      .Single(b => b.Instance.Alt == Resource.FLOTTE_ALT_NETZ);

        Assert.True(bild.Instance.BereichGewaehlt.HasDelegate);
        Assert.True(bild.Instance.Zurueckgesetzt.HasDelegate);
    }

    /// <summary>Das zweite Bild ist WÄHLBAR und steht anfangs nicht da (Konzept 2.3).</summary>
    [Fact]
    public async Task Das_Ladezustandsbild_ist_waehlbar()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        Assert.DoesNotContain(cut.FindAll("img"), b => b.GetAttribute("alt") == Resource.FLOTTE_ALT_SOC);

        await Schalten(cut, Resource.FLOTTE_CHK_ZWEITES_BILD, true);

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("img"), b => b.GetAttribute("alt") == Resource.FLOTTE_ALT_SOC));
    }

    // =====================================================================
    // Der Betriebseditor steht nicht mehr im Ergebnis
    // =====================================================================

    [Fact]
    public void Der_Betriebseditor_steht_NICHT_mehr_im_Ergebnis()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        Assert.Empty(cut.FindComponents<SpeicherFlottenBetriebEditor>());
    }

    [Fact]
    public async Task Der_Verweis_auf_die_Betriebsfuehrung_meldet_nach_aussen()
    {
        int gerufen = 0;
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p
            .Add(x => x.Ergebnis, Vollstaendig())
            .Add(x => x.BetriebsfuehrungAendern, () => gerufen++));

        await cut.FindAll("button")
                 .Single(b => b.TextContent.Trim() == Resource.FLOTTE_ERG_BTN_BETRIEB_AENDERN)
                 .ClickAsync(new());

        cut.WaitForAssertion(() => Assert.Equal(1, gerufen));
    }

    /// <summary>Kein Delegat ist kein Knopf (Hausregel).</summary>
    [Fact]
    public void Ohne_Rueckruf_kein_Knopf_zur_Betriebsfuehrung()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        Assert.DoesNotContain(cut.FindAll("button"),
            b => b.TextContent.Trim() == Resource.FLOTTE_ERG_BTN_BETRIEB_AENDERN);
    }

    // =====================================================================
    // Helfer
    // =====================================================================

    private static string Bildquelle(IRenderedComponent<SpeicherFlottenErgebnisAnsicht> cut, string alt)
        => cut.FindAll("img").Single(b => b.GetAttribute("alt") == alt).GetAttribute("src") ?? "";

    private static Task Schalten(IRenderedComponent<SpeicherFlottenErgebnisAnsicht> cut,
                                 string beschriftung, bool an)
    {
        var kasten = cut.FindAll("label.epos-schalter")
            .Single(l => l.QuerySelector(".epos-feld-text")!.TextContent.Trim() == beschriftung)
            .QuerySelector("input")!;
        return kasten.ChangeAsync(new ChangeEventArgs { Value = an });
    }

    private static Task Stufe(IRenderedComponent<SpeicherFlottenErgebnisAnsicht> cut, string beschriftung)
    {
        var knopf = cut.FindAll("fieldset.epos-optionsgruppe label.epos-option")
            .Single(l => l.QuerySelector(".epos-feld-text")!.TextContent.Trim() == beschriftung)
            .QuerySelector("input")!;
        return knopf.ChangeAsync(new ChangeEventArgs { Value = true });
    }

    // =====================================================================
    // Prüfstände
    // =====================================================================

    /// <summary>Der schlanke Fall: Betriebsführung und verfehltes Peak-Ziel, kein Lauf.</summary>
    private static SpeicherFlottenErgebnis Ergebnis() => new()
    {
        Konfiguration = new FlottenStudieKonfiguration { Optionen = new()
            { Betriebsziel = FlottenBetriebsziel.PeakShaving, Verteilung = FlottenVerteilung.Kaskade,
              WirtschaftlicherPeakZielwertKw = 50 } },
        Studie = new FlottenStudienErgebnis { Variante = new() { Zulaessig = true, MaximalerNetzbezugKw = 789.36 } }
    };

    /// <summary>
    /// Der VOLLE Fall: zwei Einheiten, zehn Tage im Viertelstundenraster, Jahreskonten.
    /// Die Zahlen sind dieselben wie im Kern-Prüfstand
    /// (<c>EPOS.Kern.Tests/SpeicherFlottenAnzeigeCtrlTests</c>) — zwei Prüfstände für
    /// dasselbe Bild liefen sonst auseinander.
    /// </summary>
    private static SpeicherFlottenErgebnis Vollstaendig()
    {
        var einheiten = new List<FlottenEinheit>
        {
            new() { Id = "a", Name = "Speicher A", KapazitaetKWh = 24, LadeleistungKw = 10,
                    EntladeleistungKw = 12, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5 },
            new() { Id = "b", Name = "Speicher B", KapazitaetKWh = 16, LadeleistungKw = 6,
                    EntladeleistungKw = 7, SocMin = 0.1, SocMax = 0.9, SocStart = 0.5 }
        };

        var variante = new FlottenSimulationErgebnis
        {
            Zulaessig = true, NetzbezugKWh = 41000, MaximalerNetzbezugKw = 100, VerlusteKWh = 122.4,
            SpeicherKennzahlen = new List<FlottenSpeicherKennzahlen>
            {
                new() { SpeicherId = "a", LadeenergieAcKWh = 1800, EntladeenergieAcKWh = 1650,
                        AnfangsenergieKWh = 12, EndenergieKWh = 12, AequivalenteVollzyklen = 34.2 },
                new() { SpeicherId = "b", LadeenergieAcKWh = 900, EntladeenergieAcKWh = 820,
                        AnfangsenergieKWh = 8, EndenergieKWh = 8, AequivalenteVollzyklen = 25.6 }
            }
        };
        var referenz = new FlottenSimulationErgebnis
        {
            Zulaessig = true, NetzbezugKWh = 40000, MaximalerNetzbezugKw = 120
        };

        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < 10 * 96; i++)
        {
            double stunde = i % 96 / 4.0;
            double last = 70 + 40 * Math.Sin(2 * Math.PI * (stunde - 6) / 24.0);
            double a = 8 * Math.Sin(2 * Math.PI * (stunde - 6) / 24.0);
            double b = 4 * Math.Sin(2 * Math.PI * (stunde - 12) / 24.0);

            referenz.Intervalle.Add(new FlottenIntervallErgebnis
            { Zeitstempel = start.AddMinutes(15 * i), LastKw = last, NetzleistungKw = last });
            variante.Intervalle.Add(new FlottenIntervallErgebnis
            {
                Zeitstempel = start.AddMinutes(15 * i), LastKw = last, NetzleistungKw = last - a - b,
                IstleistungKwJeSpeicher = new List<double> { a, b },
                EnergieStartKWhJeSpeicher = new List<double> { 12 - a, 8 - b },
                EnergieEndeKWhJeSpeicher = new List<double> { 12 + a, 8 + b }
            });
        }

        var konten = new List<FlottenJahreskonto>();
        for (int jahr = 1; jahr <= 20; jahr++)
            konten.Add(new FlottenJahreskonto
            {
                Jahr = jahr, OpexEuro = 120, DurchsatzkostenEuro = 40,
                ErsatzkostenEuro = jahr == 10 ? 4200 : 0,
                NettoCashflowEuro = 1400 - (jahr == 10 ? 4200 : 0)
            });

        return new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Konfiguration = new FlottenStudieKonfiguration
            {
                Einheiten = einheiten,
                Optionen = new FlottenSimulationOptionen
                {
                    Betriebsziel = FlottenBetriebsziel.PeakShaving,
                    Verteilung = FlottenVerteilung.Kaskade,
                    WirtschaftlicherPeakZielwertKw = 100
                },
                Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
                { Kalkulationszins = 0.03, ProjektjahreBeiWiederholung = 20 }
            },
            Studie = new FlottenStudienErgebnis
            {
                Variante = variante,
                ReferenzOhneSpeicher = referenz,
                Referenzrechnung = new FlottenRechnung { GesamtEuro = 18000, EnergiekostenEuro = 15000, LeistungskostenEuro = 3000 },
                Variantenrechnung = new FlottenRechnung { GesamtEuro = 17200, EnergiekostenEuro = 14700, LeistungskostenEuro = 2500 },
                Wirtschaftlichkeit = new FlottenWirtschaftlichkeitErgebnis
                { InvestitionEuro = 15000, KapitalwertEuro = 3140, Jahreskonten = konten }
            }
        };
    }
}

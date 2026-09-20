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
/// Steuerzeile der Hausregel § 5 über dem Netzbild (sortiert, Reihenwahl, Zeitraum)
/// — und dass der Betriebseditor hier NICHT mehr steht.</para>
///
/// <para><b>Wie „kommt der Schalter am Bild an?" geprüft wird.</b> Die Komponente
/// legt ihren Bildauftrag offen
/// (<see cref="SpeicherFlottenErgebnisAnsicht.Bildschluessel"/>, derselbe Schlüssel wie
/// auf der Ergebnisseite): Ändert ein Schalter den Schlüssel, ist er beim Kern
/// angekommen. Dass der Kern ihn an den Renderer durchreicht, zeigt
/// <c>EPOS.Kern.Tests/SpeicherFlottenAnzeigeCtrlTests</c>.</para>
///
/// <para><b>Seit der Etappe DG-E3 stehen die drei Bilder als
/// <see cref="EPOS.UI.Bausteine.DiagrammSvg"/> da</b> — befragt wird deshalb die
/// KOMPONENTE (Bezeichnung, Modell) und nicht ein <c>img</c> mit <c>data:</c>-URI.
/// Der Rundlauf-Datenzoom ist mit ihnen entfallen (Entscheid DG-E3-9): Der Zoom liegt
/// in der <c>viewBox</c>, also gibt es kein Rechteck mehr zu melden.</para>
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

        Assert.NotNull(Bild(cut, Resource.FLOTTE_ALT_PROJEKTION).Instance.Modell);

        var tabelle = cut.Find("details.epos-flotte-jahreskonten");
        Assert.False(tabelle.HasAttribute("open"));
        Assert.Contains(Resource.FLOTTE_PROJ_SP_CASHFLOW, tabelle.TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ein abgewählter Reihenschalter holt ein NEUES Zeichenmodell — geprüft an seiner
    /// REFERENZ: Genau daran entscheidet der Baustein, ob er seinen Knotenbaum neu baut
    /// (Etappe DG-E3). Bliebe sie stehen, zeigte das Bild weiter die alte Reihe.
    /// </summary>
    [Fact]
    public async Task Eine_abgewaehlte_Projektionsreihe_zeichnet_das_Bild_neu()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        object? vorher = Bild(cut, Resource.FLOTTE_ALT_PROJEKTION).Instance.Modell;

        await Schalten(cut, Resource.FLOTTE_R_KUMULIERT, false);

        cut.WaitForAssertion(() =>
            Assert.NotSame(vorher, Bild(cut, Resource.FLOTTE_ALT_PROJEKTION).Instance.Modell));
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

    /// <summary>
    /// DER ZOOM LIEGT IM BILD (Entscheid DG-E3-9). Hier stand bis zur Etappe DG-E3 die
    /// Wache „das Netzbild trägt den Datenzoom": Das Netzbild war ein PNG, der Anwender
    /// zog darin ein Rechteck auf, und die Ansicht ließ den Kern das Bild ein zweites
    /// Mal rechnen. Jetzt trägt das Zeichenmodell die Werte ohnehin, und der Zoom ist
    /// eine Attributänderung an EINER <c>viewBox</c> — es gibt kein Rechteck mehr zu
    /// melden. Geprüft wird statt dessen, dass beide Zeitbilder als
    /// <see cref="EPOS.UI.Bausteine.DiagrammSvg"/> ankommen, jedes mit EIGENER Kennung:
    /// Zwei gleiche schnitten das eine Bild am <c>clipPath</c>-Rechteck des anderen.
    /// </summary>
    [Fact]
    public async Task Die_Zeitbilder_stehen_als_DiagrammSvg_mit_eigener_Kennung_da()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        await Schalten(cut, Resource.FLOTTE_CHK_ZWEITES_BILD, true);

        cut.WaitForAssertion(() =>
        {
            var netz = Bild(cut, Resource.FLOTTE_ALT_NETZ);
            var soc = Bild(cut, Resource.FLOTTE_ALT_SOC);

            Assert.NotNull(netz.Instance.Modell);
            Assert.NotNull(soc.Instance.Modell);
            Assert.NotEqual(netz.Instance.Kennung, soc.Instance.Kennung);

            // Die Einheiten der Zeigerzeile: links kW, rechts der Ladezustand in kWh.
            Assert.Equal("kW", netz.Instance.Einheit);
            Assert.Equal("kWh", netz.Instance.EinheitRechts);

            // KEIN Pixelbild mehr: Der PNG-Weg der Oberfläche ist mit DG-E3 fort.
            Assert.Empty(cut.FindAll("img"));
        });
    }

    /// <summary>Das zweite Bild ist WÄHLBAR und steht anfangs nicht da (Konzept 2.3).</summary>
    [Fact]
    public async Task Das_Ladezustandsbild_ist_waehlbar()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        Assert.DoesNotContain(cut.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>(),
                              b => b.Instance.Bezeichnung == Resource.FLOTTE_ALT_SOC);

        await Schalten(cut, Resource.FLOTTE_CHK_ZWEITES_BILD, true);

        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>(),
                            b => b.Instance.Bezeichnung == Resource.FLOTTE_ALT_SOC));
    }

    // =====================================================================
    // Kennzahlen je Speicher — eine Zeile je Einheit (#210)
    // =====================================================================

    /// <summary>
    /// ANWENDERBEFUND #210 (11.09.2026): „Kennzahlen je Speicher" zeigte EINE Zeile,
    /// obwohl die Flotte zwei Einheiten führt. Die Ansicht selbst war nie der Grund —
    /// sie zählt, was das Ergebnis trägt. Diese Wache hält das fest: zwei Einheiten,
    /// zwei Kennzahlzeilen, beide mit ihrem Anlagennamen.
    /// </summary>
    [Fact]
    public void Kennzahlen_je_Speicher_fuehren_JEDE_Einheit()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));

        var zeilen = Kennzahlentabelle(cut).QuerySelectorAll("tbody tr");
        Assert.Equal(2, zeilen.Length);
        Assert.Equal(new[] { "Speicher A", "Speicher B" },
                     zeilen.Select(r => r.QuerySelectorAll("td")[0].TextContent.Trim()).ToArray());
        Assert.Contains(Komma("1.800,00"), zeilen[0].TextContent, StringComparison.Ordinal);
        Assert.Contains(Komma("900,00"), zeilen[1].TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Dieselbe Zusage für die REIHENWAHL: je Einheit ein Paar aus Leistungs- und
    /// Ladezustandsreihe — mit ihrem Namen, nicht mit ihrer Kennung.
    /// </summary>
    [Fact]
    public void Die_Reihenwahl_fuehrt_je_Einheit_ein_Paar()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p.Add(x => x.Ergebnis, Vollstaendig()));
        string[] schalter = cut.FindAll("label.epos-schalter .epos-feld-text")
                               .Select(x => x.TextContent.Trim()).ToArray();

        foreach (string name in new[] { "Speicher A", "Speicher B" })
        {
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, Resource.FLOTTE_R_EINHEIT, name), schalter);
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, Resource.FLOTTE_R_SOC, name), schalter);
        }
    }

    /// <summary>Die Kennzahlentabelle — erkannt an ihrer Vollzyklenspalte.</summary>
    private static AngleSharp.Dom.IElement Kennzahlentabelle(
        IRenderedComponent<SpeicherFlottenErgebnisAnsicht> cut)
        => cut.FindAll("table.epos-raster")
              .Single(t => (t.QuerySelector("thead")?.TextContent ?? "")
                            .Contains(Resource.FLOTTE_KENN_SP_VOLLZYKLEN, StringComparison.Ordinal));

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

    /// <summary>Das Diagramm mit dieser Bildbeschreibung (Etappe DG-E3).</summary>
    private static IRenderedComponent<EPOS.UI.Bausteine.DiagrammSvg> Bild(
        IRenderedComponent<SpeicherFlottenErgebnisAnsicht> cut, string bezeichnung)
        => cut.FindComponents<EPOS.UI.Bausteine.DiagrammSvg>()
              .Single(b => b.Instance.Bezeichnung == bezeichnung);

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

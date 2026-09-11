using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// DER EINHEITENEDITOR DER SPEICHERFLOTTE.
///
/// <para><b>Seit Auftrag #224 trägt er nur noch die Einheiten</b> (Anwenderentscheid
/// SD‑E‑9, Option A): Der Größenbereich steht als Tabelle in Station 4
/// (<c>OptimierungStationTests</c>), „Netz und Planung" in Schritt 3
/// (<c>SpeicherFlottenBloeckeTests</c>) und die wirtschaftliche Jahresprojektion in
/// Schritt 2 (ebenda). Die Prüffälle dieser Blöcke sind mitgewandert — sie prüfen
/// dieselben Felder an ihrem neuen Ort.</para>
/// </summary>
public sealed class SpeicherFlottenEditorTests : EposBunitContext
{
    [Fact]
    public void Einheiten_lassen_sich_hinzufuegen_kopieren_und_entfernen()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, new FlottenStudieKonfiguration())
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Speicher hinzufügen")).Click();
        Assert.NotNull(gemeldet);
        Assert.Single(gemeldet!.Einheiten);
        Assert.Single(gemeldet.Auslegung.Achsen);

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Kopieren").Click();
        Assert.Equal(2, gemeldet.Einheiten.Count);
        Assert.Equal(2, gemeldet.Auslegung.Achsen.Count);
        Assert.NotEqual(gemeldet.Einheiten[0].Id, gemeldet.Einheiten[1].Id);

        cut.FindAll("button").First(x => x.TextContent.Trim() == "Entfernen").Click();
        Assert.Single(gemeldet.Einheiten);
        Assert.Single(gemeldet.Auslegung.Achsen);
    }

    [Fact]
    public void Rueckruf_erhaelt_einen_unabhaengigen_Snapshot()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var eingang = KonfigurationMitEinheit();
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, eingang)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Eingabe(cut, "Maximale Ladeleistung:").Input("61");

        Assert.NotNull(gemeldet);
        Assert.Equal(61, gemeldet!.Einheiten[0].LadeleistungKw);
        Assert.Equal(40, eingang.Einheiten[0].LadeleistungKw);
        gemeldet.Einheiten[0].LadeleistungKw = 999;
        Assert.Equal(61, cut.Instance.AktuellerSnapshot.Einheiten[0].LadeleistungKw);
    }

    [Fact]
    public void Technik_haelt_Laden_und_Entladen_sowie_Wirkungsgrade_getrennt()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Eingabe(cut, "Maximale Ladeleistung:").Input("35");
        Eingabe(cut, "Maximale Entladeleistung:").Input("72");
        Eingabe(cut, "Ladewirkungsgrad:").Input("93");
        Eingabe(cut, "Entladewirkungsgrad:").Input("96");

        Assert.Equal(35, gemeldet!.Einheiten[0].LadeleistungKw);
        Assert.Equal(72, gemeldet.Einheiten[0].EntladeleistungKw);
        Assert.Equal(0.93, gemeldet.Einheiten[0].Ladewirkungsgrad, 10);
        Assert.Equal(0.96, gemeldet.Einheiten[0].Entladewirkungsgrad, 10);
    }

    /// <remarks>
    /// Multi Use PLANT und ist ohne Fahrplan-Löser gesperrt (Auftrag #170c); dieser Fall
    /// prüft die Felder des Ziels und setzt den Planer deshalb ausdrücklich voraus.
    /// </remarks>
    [Fact]
    public void MultiUse_zeigt_Peak_und_Prognoseparameter()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Auswahl(cut, "Betriebsziel:").Change(((int)FlottenBetriebsziel.MultiUse).ToString());

        Assert.Contains("Wirtschaftlicher Peak-Zielwert", cut.Markup);
        Assert.Single(cut.FindAll("label"), x => x.TextContent.Contains("Wirtschaftlicher Peak-Zielwert"));
        Assert.Single(cut.FindAll("label"), x => x.TextContent.Contains("Laden aus dem Netz erlauben"));
        Assert.Single(cut.FindAll("label"), x => x.TextContent.Contains("Batterieexport ins Netz erlauben"));
        Assert.Equal(FlottenBetriebsziel.MultiUse, gemeldet!.Optionen.Betriebsziel);

        // DIE PROGNOSEPLANUNG IST MIT #224 AUSGEZOGEN (Zielbild 7.4): Sie steht in
        // Schritt 3 „Betriebsfuehrung" — im SpeicherFlottenNetzBlock, und dort
        // geprueft. Hier darf sie nicht ein zweites Mal stehen.
        Assert.DoesNotContain("Informationsstand", cut.Markup);
        Assert.DoesNotContain("Planungshorizont", cut.Markup);
    }

    /// <summary>
    /// DER GRÖSSENBEREICH IST MIT #224 AUSGEZOGEN (Zielbild 7.4): Er beschreibt die
    /// SUCHE und nicht die Einheit und steht seither als Tabelle in Station 4
    /// („Suchraum je Einheit", <c>OptimierungStationTests</c>). Die ACHSEN bleiben
    /// trotzdem Sache dieses Editors — er legt je Einheit genau eine an und zieht ihre
    /// Vorlage nach.
    /// </summary>
    [Fact]
    public void Der_Groessenbereich_steht_nicht_mehr_im_Einheiteneditor()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));

        Assert.Empty(cut.FindAll("section.epos-flotte-auslegungskategorie"));
        Assert.DoesNotContain("Größenkopplung", cut.Markup);
        Assert.DoesNotContain("In der Auslegung variieren", cut.Markup);

        // Die Achse selbst ist da — sonst haette Station 4 nichts zu bearbeiten.
        Assert.Single(cut.Instance.AktuellerSnapshot.Auslegung.Achsen);
    }

    /// <summary>
    /// „Ersatz &amp; Restwert" bleibt beim Einheiteneditor — es ist die einzige
    /// Kostenangabe, die wirklich an EINER Einheit hängt. Die wirtschaftliche
    /// Jahresprojektion daneben ist mit #224 nach Schritt 2 gezogen (SD‑Q12).
    /// </summary>
    [Fact]
    public void Ersatz_und_Restwert_bleiben_bei_der_Einheit()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Eingabe(cut, "Ersatzkosten:").Input("12000");
        Eingabe(cut, "Ersatzintervall:").Input("8");
        Eingabe(cut, "Restwert der Einheit:").Input("2500");

        Assert.Equal(12000, gemeldet!.Einheiten[0].ErsatzkostenEuro);
        Assert.Equal(8, gemeldet.Einheiten[0].ErsatzintervallJahre);
        Assert.Equal(2500, gemeldet.Einheiten[0].RestwertEuro);

        // Die Jahresprojektion steht hier NICHT mehr (SD-Q12).
        Assert.DoesNotContain("Projektlaufzeit", cut.Markup);
        Assert.DoesNotContain("Kalkulationszins", cut.Markup);
        Assert.DoesNotContain("Energie-Ausgleichswert", cut.Markup);
        Assert.DoesNotContain("Maximale Auslegungskandidaten", cut.Markup);
    }

    /// <summary>
    /// ZWEI KOSTENBLÖCKE STATT EINEM (Auftrag #224, Konzept 7.2): Die sieben
    /// Kostenfelder standen unter der Überschrift „Ersatz und Restwert", weil der
    /// Schalter „Eigene Kosten" dort eingehängt war.
    /// </summary>
    [Fact]
    public void Die_eigenen_Kosten_stehen_unter_einer_eigenen_Ueberschrift()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));
        Schalter(cut, "Eigene Kosten für diese Einheit verwenden").Change(true);

        AngleSharp.Dom.IElement kosten = cut.FindAll("div.epos-flotte-untergruppe")
            .Single(x => x.QuerySelector("h4")?.TextContent.Trim() == "Kosten dieser Einheit");
        AngleSharp.Dom.IElement ersatz = cut.FindAll("div.epos-flotte-untergruppe")
            .Single(x => x.QuerySelector("h4")?.TextContent.Trim() == "Ersatz und Restwert");

        Assert.Contains("Investition Kapazität", kosten.TextContent);
        Assert.Contains("Kosten je Entladung", kosten.TextContent);
        Assert.DoesNotContain("Investition Kapazität", ersatz.TextContent);
        Assert.Contains("Ersatzkosten", ersatz.TextContent);
    }

    /// <summary>
    /// DER KARTENKOPF NENNT NUR DEN NAMEN (Konzept 7.8): Der Kurztext
    /// <c>[100kW, 129.0kWh]</c> stand mit Punkt als Dezimaltrenner daneben; dieselben
    /// Zahlen stehen aufgeklappt in Hauskultur im Technikblock.
    /// </summary>
    [Fact]
    public void Der_Kartenkopf_traegt_keinen_Zahlenzusatz_mehr()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));

        AngleSharp.Dom.IElement kopf = cut.Find("button.epos-flotte-einheit__umschalter");

        Assert.Contains("Hauptspeicher", kopf.TextContent);
        Assert.Empty(kopf.QuerySelectorAll(".epos-flotte-einheit__kurz"));
        Assert.DoesNotContain("kWh", kopf.TextContent);
    }

    /// <summary>
    /// Der Erklärsatz über der Einheitenliste ist eine HERLEITUNGSZEILE, die Pille
    /// zeigt die Einheitenzahl NEUTRAL (Konzept 7.8: kein Rot ohne Fehler).
    /// </summary>
    [Fact]
    public void Der_Kopf_traegt_eine_Herleitungszeile_und_eine_neutrale_Pille()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));

        Assert.Single(cut.FindAll(".epos-flotte-editor__kopf .epos-herleitung"));
        Assert.Contains("1", cut.Find(".epos-flotte-editor__zaehler").TextContent);
    }

    [Fact]
    public void Eigene_Kosten_oeffnen_den_Einheiten_Override()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));

        Assert.DoesNotContain("Investition Kapazität", cut.Markup);
        Schalter(cut, "Eigene Kosten für diese Einheit verwenden").Change(true);

        Assert.Contains("Investition Kapazität", cut.Markup);
        Assert.Contains("Betriebskosten Leistung", cut.Markup);
    }

    private static FlottenStudieKonfiguration KonfigurationMitEinheit()
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Hauptspeicher", KapazitaetKWh = 100,
            LadeleistungKw = 40, EntladeleistungKw = 50,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.96,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };
        return new FlottenStudieKonfiguration
        {
            Einheiten = new() { einheit },
            Auslegung = new FlottenAuslegungEingang
            {
                Achsen = new()
                {
                    new FlottenAuslegungsAchse
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1, AnzahlBis = 1,
                        KapazitaetVonKWh = 100, KapazitaetBisKWh = 300, KapazitaetSchrittKWh = 50,
                        LeistungVonKw = 40, LeistungBisKw = 80, LeistungSchrittKw = 20,
                        Vorlage = einheit
                    }
                }
            }
        };
    }

    private static AngleSharp.Dom.IElement Eingabe(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;

    private static AngleSharp.Dom.IElement Kategorie(IRenderedComponent<SpeicherFlottenEditor> cut, string bezeichnung) =>
        cut.FindAll("section.epos-flotte-auslegungskategorie")
            .Single(x => x.GetAttribute("aria-label") == bezeichnung);

    private static AngleSharp.Dom.IElement KategorieEingabe(
        IRenderedComponent<SpeicherFlottenEditor> cut, string kategorie, string label) =>
        Kategorie(cut, kategorie).QuerySelectorAll("label")
            .Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;

    private static AngleSharp.Dom.IElement Auswahl(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;

    private static AngleSharp.Dom.IElement Schalter(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;
}

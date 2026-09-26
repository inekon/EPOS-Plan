using AngleSharp.Dom;
using Bunit;
using System.Globalization;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Administrationsdialog „Nutzungsdauern (AfA)" (Konzept „Nutzungsdauer je
/// Technik und Positionsart", Stufe S1, Abschnitt 2.5).
///
/// <para>Soll ist die Feldkarte: Kopf mit Kontextzeile, Suchfeld und
/// Wiederherstellen-Knopf, das nach Technik gruppierte Raster mit acht Spalten (Etappe
/// E10: dazu Instandsetzung und Wartung),
/// die Neuzeile und die Schlussleiste. Dazu die drei Regeln, die dieser Dialog
/// mehr trägt als ein gewöhnlicher: Eine Auslieferungszeile ist im Wert änderbar
/// und NICHT löschbar (ND‑Q5), das Wiederherstellen fragt zurück, und eine
/// Eingabe lebt bis „Speichern" im Objekt.</para>
///
/// <para>Kulturpinnung über <see cref="EposBunitContext"/> — die Fälle prüfen
/// deutschen Text, und der CI-Läufer steht auf en-US.</para>
/// </summary>
public class NutzungsdauerDialogTests : EposBunitContext
{
    public NutzungsdauerDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static NutzungsdauerZeileAnzeige Kessel => new()
    {
        Id = 1, TechnikId = 2, Technik = "Heizkessel", Positionsart = "Wärmeerzeuger",
        IstStandard = true, Nutzungsdauer = 20, Quelle = "VDI 2067 Blatt 1, Tab. A2 (Richtwert)",
        Auslieferung = true,
    };

    private static NutzungsdauerZeileAnzeige Abgas => new()
    {
        Id = 3, TechnikId = 2, Technik = "Heizkessel", Positionsart = "Abgasanlage / Schornstein",
        Nutzungsdauer = 25, Quelle = "VDI 2067 Blatt 1, Tab. A2 (Richtwert)", Auslieferung = true,
    };

    private static NutzungsdauerZeileAnzeige Module => new()
    {
        Id = 17, TechnikId = 3, Technik = "Photovoltaik", Positionsart = "Module",
        IstStandard = true, Nutzungsdauer = 25, AfaSteuerlich = 20,
        Quelle = "VDI 2067 Blatt 1, Tab. A2 (Richtwert); AfA-Tabelle AV (Richtwert)",
        Auslieferung = true,
    };

    private static NutzungsdauerZeileAnzeige Montage => new()
    {
        Id = 27, TechnikId = null, Technik = "", Positionsart = "Montage",
        Nutzungsdauer = null, Quelle = "wie Standardzeile der Technik", Auslieferung = true,
    };

    /// <summary>Eine EIGENE Zeile — sie allein ist löschbar.</summary>
    private static NutzungsdauerZeileAnzeige Eigene => new()
    {
        Id = 99, TechnikId = 2, Technik = "Heizkessel", Positionsart = "Eigene Art",
        Nutzungsdauer = 7, Quelle = "eigener Wert", Auslieferung = false,
    };

    private static NutzungsdauerZeileAnzeige[] Zeilen()
        => new[] { Kessel, Abgas, Eigene, Module, Montage };

    // ---- Aufbau ----------------------------------------------------------

    private IRenderedComponent<NutzungsdauerDialog> Zeige(
        Action<ComponentParameterCollectionBuilder<NutzungsdauerDialog>>? mehr = null,
        IReadOnlyList<NutzungsdauerZeileAnzeige>? zeilen = null)
    {
        IReadOnlyList<NutzungsdauerZeileAnzeige> stand = zeilen ?? Zeilen();
        return Render<NutzungsdauerDialog>(p =>
        {
            p.Add(x => x.Zeilen, stand);
            p.Add(x => x.Techniken, new[] { (2, "Heizkessel"), (3, "Photovoltaik") });
            p.Add(x => x.Neuladen, () => stand);
            mehr?.Invoke(p);
        });
    }

    private static IReadOnlyList<IElement> Datenzeilen(IRenderedComponent<NutzungsdauerDialog> cut)
        => cut.FindAll(".epos-raster tbody tr:not(.epos-raster-gruppe):not(.epos-raster-leer)");

    private static IReadOnlyList<IElement> Gruppenzeilen(IRenderedComponent<NutzungsdauerDialog> cut)
        => cut.FindAll(".epos-raster tbody tr.epos-raster-gruppe");

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Suche_Raster_Neuzeile_und_Schlussleiste()
    {
        var cut = Zeige();

        Assert.Equal("Nutzungsdauern (AfA)", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Ersatzbeschaffung", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Single(cut.FindAll(".epos-raster"));
        // ETAPPE E10 (Stufe S3): zwei Spalten mehr — Instandsetzung und Wartung (6 → 8).
        Assert.Equal(8, cut.FindAll(".epos-raster thead th").Count);
        Assert.Contains("Nutzungsdauer [a]", cut.Markup);
        Assert.Contains("AfA steuerlich [a]", cut.Markup);
        Assert.Contains("Instandsetzung [%/a]", cut.Markup);
        Assert.Contains("Wartung [%/a]", cut.Markup);
        Assert.Contains("Auslieferungswerte wiederherstellen", cut.Markup);
        Assert.Single(cut.FindAll(".epos-leiste"));
    }

    /// <summary>Ohne Gaben zeichnet die Seite trotzdem — jede Liste leer, jeder Delegat null.</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_seinen_Leerzustand()
    {
        var cut = Render<NutzungsdauerDialog>();

        Assert.Equal("Nutzungsdauern (AfA)", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Empty(Datenzeilen(cut));
        Assert.Contains("Keine Zeile passt zur Suche.", cut.Markup);
    }

    // =====================================================================
    // Gruppierung
    // =====================================================================

    /// <summary>
    /// Das Raster ist nach TECHNIK gruppiert, und die technikübergreifenden Zeilen
    /// stehen unter ihrem eigenen Kopf.
    /// </summary>
    [Fact]
    public void Das_Raster_ist_nach_Technik_gruppiert()
    {
        var cut = Zeige();

        var koepfe = Gruppenzeilen(cut);
        Assert.Equal(3, koepfe.Count);
        Assert.Equal("Heizkessel", koepfe[0].TextContent);
        Assert.Equal("Photovoltaik", koepfe[1].TextContent);
        Assert.Equal("technikübergreifend", koepfe[2].TextContent);
        Assert.Equal(5, Datenzeilen(cut).Count);
    }

    /// <summary>Die Standardzeile ist gekennzeichnet — sie gilt ohne Positionsart.</summary>
    [Fact]
    public void Die_Standardzeile_traegt_ihr_Kennzeichen()
    {
        var cut = Zeige();

        Assert.Equal(2, cut.FindAll(".epos-kennzeichen[title*='Standardzeile']").Count);
    }

    // =====================================================================
    // Suche
    // =====================================================================

    [Fact]
    public void Die_Suche_schraenkt_ueber_Technik_Positionsart_und_Quelle_ein()
    {
        var cut = Zeige();

        cut.Find(".epos-kontextleiste input[type=text]").Input("Abgas");
        cut.WaitForAssertion(() => Assert.Single(Datenzeilen(cut)));
        Assert.Single(Gruppenzeilen(cut));

        cut.Find(".epos-kontextleiste input[type=text]").Input("Photovoltaik");
        cut.WaitForAssertion(() => Assert.Single(Datenzeilen(cut)));

        cut.Find(".epos-kontextleiste input[type=text]").Input("gibtesnicht");
        cut.WaitForAssertion(() => Assert.Contains("Keine Zeile passt zur Suche.", cut.Markup));
    }

    // =====================================================================
    // Auslieferungszeilen (ND-Q5)
    // =====================================================================

    /// <summary>
    /// Eine Auslieferungszeile ist NICHT löschbar — und ihr Knopf trägt die WEICHE
    /// Sperre, damit der Grund überhaupt erscheinen kann.
    /// </summary>
    [Fact]
    public void Die_Auslieferungszeile_ist_nicht_loeschbar_und_meldet_den_Grund()
    {
        int gerufen = 0;
        var cut = Zeige(p => p.Add(x => x.LoeschenDelegat, id => { gerufen++; return null; }));

        var gesperrt = cut.FindAll(".epos-zeilenknoepfe button[aria-disabled=true]");
        Assert.Equal(4, gesperrt.Count);                       // vier Auslieferungszeilen
        Assert.False(gesperrt[0].HasAttribute("disabled"));    // weiche Sperre, nicht disabled

        gesperrt[0].Click();
        cut.WaitForAssertion(() => Assert.Contains("nicht gelöscht", cut.Instance.Meldung));
        Assert.Equal(0, gerufen);
        Assert.Equal("", cut.Instance.OffeneFrage);
    }

    /// <summary>Eine EIGENE Zeile fragt zurück und löscht erst auf „Ja".</summary>
    [Fact]
    public void Die_eigene_Zeile_wird_nach_Rueckfrage_geloescht()
    {
        var geloescht = new List<int>();
        var cut = Zeige(p => p.Add(x => x.LoeschenDelegat, id => { geloescht.Add(id); return null; }));

        var frei = cut.FindAll(".epos-zeilenknoepfe button:not([aria-disabled])");
        Assert.Single(frei);
        frei[0].Click();

        cut.WaitForAssertion(() => Assert.Contains("Eigene Art", cut.Instance.OffeneFrage));
        Assert.Empty(geloescht);

        cut.Find(".epos-rueckfrage .epos-leiste button").Click();   // „Ja"
        cut.WaitForAssertion(() => Assert.Equal(new[] { 99 }, geloescht.ToArray()));
    }

    // =====================================================================
    // Wiederherstellen
    // =====================================================================

    [Fact]
    public void Wiederherstellen_laeuft_erst_nach_der_Rueckfrage()
    {
        int laeufe = 0;
        var cut = Zeige(p => p.Add(x => x.WiederherstellenDelegat, () => { laeufe++; return 28; }));

        cut.FindAll(".epos-kontextleiste button")[0].Click();
        cut.WaitForAssertion(() => Assert.Contains("zurücksetzen", cut.Instance.OffeneFrage));
        Assert.Equal(0, laeufe);

        cut.Find(".epos-rueckfrage .epos-leiste button").Click();   // „Ja"
        cut.WaitForAssertion(() => Assert.Equal(1, laeufe));
        Assert.Equal("", cut.Instance.OffeneFrage);
    }

    // =====================================================================
    // Eingabe und Speichern
    // =====================================================================

    /// <summary>
    /// Eine Eingabe lebt bis „Speichern" im Objekt: Der Speicherknopf ist erst danach
    /// frei, und geschrieben wird nur die GEÄNDERTE Zeile.
    /// </summary>
    [Fact]
    public void Eine_Eingabe_wird_erst_mit_Speichern_geschrieben()
    {
        var geschrieben = new List<int>();
        var cut = Zeige(p => p.Add(x => x.Speichern, zeilen =>
        {
            foreach (NutzungsdauerZeileAnzeige z in zeilen) geschrieben.Add(z.Id);
            return null;
        }));

        // Weich gesperrt (Hausregel): aria-disabled statt disabled, ein Klick schreibt nicht.
        var speichern = cut.FindAll(".epos-leiste button")[0];
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        speichern.Click();
        Assert.Empty(geschrieben);

        cut.FindAll(".epos-raster tbody input")[0].Input("30");
        cut.WaitForAssertion(() =>
            Assert.False(cut.FindAll(".epos-leiste button")[0].HasAttribute("aria-disabled")));
        Assert.Empty(geschrieben);

        cut.FindAll(".epos-leiste button")[0].Click();
        cut.WaitForAssertion(() => Assert.Equal(new[] { 1 }, geschrieben.ToArray()));
    }

    /// <summary>
    /// ETAPPE E10 (Stufe S3): Instandsetzung und Wartung stehen je Zeile als eigene
    /// Felder — gezeigt, eingegeben und mit der Zeile gespeichert —, und die leise Zeile
    /// über dem Raster nennt ihre Herkunft (VDI 2067 Blatt 1, Tabelle A2).
    /// </summary>
    [Fact]
    public void Instandsetzung_und_Wartung_werden_mit_der_Zeile_gespeichert()
    {
        var geschrieben = new List<NutzungsdauerZeileAnzeige>();
        NutzungsdauerZeileAnzeige kessel = Kessel;
        kessel.InstandsetzungProzent = 2.0;
        var cut = Zeige(p => p.Add(x => x.Speichern, zeilen =>
        {
            geschrieben.AddRange(zeilen);
            return null;
        }), new[] { kessel, Abgas, Eigene, Module, Montage });

        Assert.Contains("VDI 2067 Blatt 1, Tabelle A2", cut.Find(".epos-nd-saetze").TextContent);

        // Je Zeile: Nutzungsdauer, AfA, Instandsetzung, Wartung, Quelle.
        cut.FindAll(".epos-raster tbody input")[2].Input("3,5");
        cut.FindAll(".epos-raster tbody input")[3].Input("0.75");
        cut.WaitForAssertion(() =>
            Assert.False(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled")));

        cut.FindAll(".epos-leiste button")[0].Click();

        cut.WaitForAssertion(() => Assert.Single(geschrieben));
        Assert.Equal(1, geschrieben[0].Id);
        Assert.Equal(3.5, geschrieben[0].InstandsetzungProzent);
        Assert.Equal(0.75, geschrieben[0].WartungProzent);
    }

    /// <summary>ETAPPE E10: Die Neuzeile trägt die zwei Sätze mit.</summary>
    [Fact]
    public void Die_Neuzeile_traegt_Instandsetzung_und_Wartung()
    {
        NutzungsdauerNeuEingabe? angelegt = null;
        var cut = Zeige(p => p.Add(x => x.AnlegenDelegat, e => { angelegt = e; return null; }));

        var neuzeile = cut.FindAll(".epos-kontextleiste")[1];
        var felder = neuzeile.QuerySelectorAll("input");   // Positionsart, Nutzungsdauer, AfA, Instandsetzung, Wartung
        felder[0].Input("Eigene Art 2");
        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("input")[3].Input("1,5");
        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("input")[4].Input("0,5");

        cut.FindAll(".epos-kontextleiste")[1].QuerySelectorAll("button")[0].Click();

        cut.WaitForAssertion(() => Assert.True(angelegt.HasValue));
        Assert.Equal("Eigene Art 2", angelegt!.Value.Positionsart);
        Assert.Equal(1.5, angelegt.Value.InstandsetzungProzent);
        Assert.Equal(0.5, angelegt.Value.WartungProzent);
    }

    /// <summary>Eine Neuzeile ohne Positionsart wird benannt abgelehnt.</summary>
    [Fact]
    public void Eine_Neuzeile_ohne_Positionsart_wird_abgelehnt()
    {
        int gerufen = 0;
        var cut = Zeige(p => p.Add(x => x.AnlegenDelegat, e => { gerufen++; return null; }));

        var leisten = cut.FindAll(".epos-kontextleiste");
        leisten[1].QuerySelectorAll("button")[0].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Positionsart darf nicht leer", cut.Instance.Meldung));
        Assert.Equal(0, gerufen);
    }

    // =====================================================================
    // Rückweg
    // =====================================================================

    /// <summary>„Abbrechen" ohne Schreibvorgang meldet <c>null</c> (Hausregel).</summary>
    [Fact]
    public void Abbrechen_meldet_null()
    {
        NutzungsdauerErgebnis? ergebnis = new NutzungsdauerErgebnis(true);
        bool gemeldet = false;

        var cut = Zeige(p => p.Add(x => x.Geschlossen, (NutzungsdauerErgebnis? e) =>
        {
            ergebnis = e;
            gemeldet = true;
        }));

        var knoepfe = cut.FindAll(".epos-leiste button");
        knoepfe[knoepfe.Count - 2].Click();      // Abbrechen steht vor OK

        cut.WaitForAssertion(() => Assert.True(gemeldet));
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Anwenderentscheid 15.09.2026: Das Kreuz im Kopf geht denselben Weg wie
    /// Abbrechen — ohne Schreibvorgang meldet es <c>null</c>.
    /// </summary>
    [Fact]
    public void Das_Kreuz_meldet_null()
    {
        NutzungsdauerErgebnis? ergebnis = new NutzungsdauerErgebnis(true);
        bool gemeldet = false;

        var cut = Zeige(p => p.Add(x => x.Geschlossen, (NutzungsdauerErgebnis? e) =>
        {
            ergebnis = e;
            gemeldet = true;
        }));

        cut.Find(".epos-dialog-zu").Click();

        cut.WaitForAssertion(() => Assert.True(gemeldet));
        Assert.Null(ergebnis);
    }

    /// <summary>„OK" speichert und meldet ein Ergebnis.</summary>
    [Fact]
    public void OK_speichert_und_meldet_ein_Ergebnis()
    {
        NutzungsdauerErgebnis? ergebnis = null;
        var cut = Zeige(p => p
            .Add(x => x.Speichern, zeilen => null)
            .Add(x => x.Geschlossen, (NutzungsdauerErgebnis? e) => ergebnis = e));

        cut.FindAll(".epos-raster tbody input")[0].Input("30");
        cut.WaitForAssertion(() =>
            Assert.False(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled")));

        var knoepfe = cut.FindAll(".epos-leiste button");
        knoepfe[knoepfe.Count - 1].Click();      // OK

        cut.WaitForAssertion(() => Assert.True(ergebnis.HasValue));
        Assert.True(ergebnis!.Value.Geaendert);
    }
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>NutzungsdauerKiSicht</c>: sieben Kopffelder und fünf SPALTEN
    /// über die lebende Zeilenliste — je Zeile wird aus einer Spaltendeklaration ein
    /// gewöhnliches Feld, benannt nach der Positionsart.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_eine_Zeile()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.NUTZUNGSDAUER));

        // Ein Kopffeld.
        KiFeldzugang suche = KiMaskenbruecke.Feldzugang(KiMaskennamen.NUTZUNGSDAUER, "suche");
        Assert.NotNull(suche);
        suche.Setzen("Heiz");
        cut.Render();
        Assert.Equal("Heiz", suche.Lesen());

        // Eine SPALTE: Aus der Deklaration wird je Zeile ein eigenes Feld, und sein
        // Anzeigename traegt die Positionsart.
        string spalte = KiMaskenbruecke.Lesen(KiMaskennamen.NUTZUNGSDAUER)
                                       .Select(w => w.Name)
                                       .First(n => n.StartsWith("nutzungsdauer_",
                                                                StringComparison.Ordinal));

        KiFeldzugang zeile = KiMaskenbruecke.Feldzugang(KiMaskennamen.NUTZUNGSDAUER, spalte);
        Assert.NotNull(zeile);
        Assert.True(zeile.Setzbar);

        KiFeldumsetzung neu = KiFeldwandler.Wandle(zeile, "18");
        Assert.True(neu.Ok, neu.Grund);
        zeile.Setzen(neu.Wert);
        cut.Render();

        Assert.Equal(18, Convert.ToDouble(zeile.Lesen(), CultureInfo.InvariantCulture), 3);
    }
}

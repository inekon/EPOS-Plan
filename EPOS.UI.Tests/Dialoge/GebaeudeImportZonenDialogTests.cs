using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Gebäudeimport mit mehreren Zonen</b> (Stufe G6c, Welle C; Mehrzonenkonzept 6.4) — gefahren
/// mit synthetischen Gaben wie die Hülle sie reicht, dazu ein Durchgang mit der echten Hülle: Regelwahl,
/// Bilanz, Obergrenze mit dem Vorschlag einer gröberen Regel, Zonenliste mit Räumen und Haken „beheizt",
/// die Flächen je Zone mit den drei Filtern, die englischen Texte und der Einzonenweg unverändert.
/// </summary>
public class GebaeudeImportZonenDialogTests : EposBunitContext
{
    public GebaeudeImportZonenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Prüfstand — die Gaben, wie die Hülle sie reicht
    // =====================================================================

    private static readonly GebaeudeImportProfilDaten Profil =
        new("Format Alpha", "(*.alpha)|*.alpha", "25 MB", new[] { "Regel eins – eine Zone" }, "Form_Alpha.btn_Help");

    private static readonly IReadOnlyList<string> Klassen = Enumerable.Range(0, 13).Select(i => "Klasse " + (char)('A' + i)).ToList();

    private static readonly IReadOnlyList<GebaeudeZonenregelDaten> Regeln = new[]
    {
        new GebaeudeZonenregelDaten("A1", "A1 – je Geschoss"),
        new GebaeudeZonenregelDaten("A2", "A2 – je Raum"),
        new GebaeudeZonenregelDaten("A3", "A3 – eine Zone"),
    };

    private static Katalogfilterprofil Flaechenprofil() => Katalogfilterprofil.AusSpalten("PROBE_FLAECHEN", new[]
    {
        new Katalogspalte("ZONE", "Zone"),
        new Katalogspalte(Katalogfilterprofil.SpBezeichner, "Bauteil"),
        new Katalogspalte("FLAECHE", "Fläche", "m²", Katalogspaltenart.Zahl),
        new Katalogspalte("BEFUND", "Befund"),
    });

    /// <summary>
    /// Ein Stand mit Zonierung: A1 zwei Zonen (Standard), A2 zu viele Zonen mit Vorschlag A1, A3 eine Zone.
    /// Die Flächen: <paramref name="flaechen"/> Zeilen; jede dritte ohne Gegenstück, jede fünfte ohne U-Wert.
    /// </summary>
    private static GebaeudeImportStand Stand(GebaeudeZuordnungsanfrage a, int flaechen = 30)
    {
        string regel = a.Zonenregel ?? "A1";
        bool eine = regel == "A3", viele = regel == "A2";
        bool kellerWarm = a.BeheiztUebersteuert.TryGetValue("r-keller", out bool k) && k;
        var zonen = new List<GebaeudeZonenzeileDaten>
        {
            new() { Name = "Kellergeschoss", Regel = regel, Raeume = "1", Flaeche = "40 m²", Volumen = "100 m³", Beheizt = kellerWarm,
                    Hinweis = "unter der Mindestgröße",
                    Raumliste = new[] { new GebaeudeZonenraumDaten("r-keller", "Keller", "KG", "40 m²", "B5", "Beleg Lage", false) } },
            new() { Name = "Erdgeschoss", Regel = regel, Raeume = "2", Flaeche = "80 m²", Volumen = "200 m³", Beheizt = true,
                    Raumliste = new[]
                    {
                        new GebaeudeZonenraumDaten("r-wohnen", "Wohnen", "EG", "50 m²", "B6", "Beleg Annahme", true),
                        new GebaeudeZonenraumDaten("r-kueche", "Küche", "EG", "30 m²", "B6", "Beleg Annahme", true),
                    } },
        };
        var zeilen = new List<GebaeudeFlaechenzeileDaten>();
        for (int i = 0; i < (eine || viele ? 0 : flaechen); i++)
        {
            bool ohneGegen = i % 3 == 0, ohneU = i % 5 == 0;
            var z = new Katalogfilterzeile(i, "Fläche " + i)
                .MitText(Katalogfilterprofil.SpBezeichner, "Fläche " + i)
                .MitText("ZONE", i % 2 == 0 ? "Kellergeschoss" : "Erdgeschoss")
                .MitZahl("FLAECHE", 10 + i, 2)
                .MitText("BEFUND", ohneGegen || ohneU ? "Befund" : "");
            z.Schluessel = i.ToString(CultureInfo.InvariantCulture);
            zeilen.Add(new GebaeudeFlaechenzeileDaten(z, ohneGegen || ohneU, ohneGegen, ohneU));
        }
        return new GebaeudeImportStand
        {
            Kopftext = "Kopf-Probe",
            Vorschlagsname = "Zonenhaus",
            Raeume = new[] { new GebaeudeRaumzeileDaten("r-wohnen", "Wohnen", "50 m²", true, true, "Grund", false) },
            Zeilen = new[]
            {
                new GebaeudeFeldzeileDaten { Zielfeld = "NUTZ", Gruppe = "Kenngrößen", Feld = "Nutzfläche", Wert = 80, Einheit = "m²",
                                             WertText = "80", HerkunftSchluessel = "DATEI", HerkunftText = "Datei", Haken = true,
                                             Eingebbar = true, HakenSetzbar = true },
            },
            Bauteile = new GebaeudeBauteileDaten
            {
                Moeglich = !viele,
                Ablehnung = viele ? "zu viele Zonen" : "",
                Kopftext = eine ? "Zone „Zonenhaus“" : "2 Zonen · Nutzfläche 120 m²",
                Zeilen = new[] { new GebaeudeBauteilzeileDaten("Wand", "Außenwand", "20 m²", "0,3", "180°", "90°", "Außenluft", "Datei", "DATEI", "w-1") },
            },
            Zonierung = new GebaeudeZonierungDaten
            {
                Regeln = Regeln,
                Regel = regel,
                RegelText = Regeln.Single(r => r.Schluessel == regel).Text,
                Einzonig = eine,
                Bilanz = new GebaeudeZonenbilanzDaten(eine ? "1" : viele ? "60" : "2", "80 m²", "200 m³", "250,5 m²", eine ? "0 m²" : "40 m²"),
                Schwerste = viele
                    ? new GebaeudeImportMeldung(WarnStufe.Warnung, "Warnung", "Zu viele Zonen — Vorschlag A1", "IMP_X_ZU_VIELE_ZONEN_VORSCHLAG")
                    : null,
                ZuViele = viele,
                Vorschlagsregel = viele ? "A1" : null,
                VorschlagsregelText = viele ? "A1 – je Geschoss" : "",
                Zonen = eine ? zonen.Take(1).ToList() : zonen,
                Flaechenprofil = eine || viele ? null : Flaechenprofil(),
                Flaechen = zeilen,
            },
        };
    }

    private sealed class Protokoll
    {
        public List<GebaeudeZuordnungsanfrage> Anfragen { get; } = new();
        public List<GebaeudeImportErgebnis> Uebernommen { get; } = new();
    }

    private IRenderedComponent<GebaeudeImportDialog> Bauen(Protokoll p, Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>? zuordnen = null,
                                                           GebaeudeImportTexte? texte = null)
    {
        Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> stand = zuordnen ?? (a => Stand(a));
        return Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, Profil);
            c.Add(x => x.Baualtersklassen, Klassen);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)((_, _, _) =>
                Task.FromResult(new GebaeudeLesestand(true, new GebaeudeImportKopf("haus.alpha", "Format Alpha", "Schema 1", "20 KB", "Regel eins"),
                                                      new[] { "Haus 1" }, Array.Empty<GebaeudeImportMeldung>()))));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return stand(a); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(e => { p.Uebernommen.Add(e); return Task.FromResult<string?>(null); }));
            if (texte is not null) c.Add(x => x.Texte, texte);
        });
    }

    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut, string knopf = "Datei wählen")
    {
        cut.FindAll("button").First(k => k.TextContent.Contains(knopf)).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
    }

    private static IElement Regelwahl(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.FindAll("select").First(s => s.TextContent.Contains("A1 –"));

    private static void RegelWaehlen(IRenderedComponent<GebaeudeImportDialog> cut, string schluessel)
    {
        IElement wahl = Regelwahl(cut);
        wahl.Change(wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith(schluessel, StringComparison.Ordinal)).GetAttribute("value"));
    }

    private static IElement Schalter(IRenderedComponent<GebaeudeImportDialog> cut, string beschriftung)
        => cut.FindAll("label.epos-schalter").First(l => l.TextContent.Contains(beschriftung)).QuerySelector("input")!;

    private static void Ok(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    // =====================================================================
    //  Regelwahl und Bilanz
    // =====================================================================

    [Fact]
    public void Die_Regelwahl_zeigt_die_gebildete_Regel_und_ordnet_mit_der_neuen_zu()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p);
        Einlesen(cut);

        Assert.Null(p.Anfragen[0].Zonenregel);                         // die Vorgabe der Datei
        IElement wahl = Regelwahl(cut);
        Assert.Equal(3, wahl.QuerySelectorAll("option").Length);
        Assert.Equal("A1 – je Geschoss", wahl.QuerySelectorAll("option").Single(o => o.HasAttribute("selected")).TextContent);
        Assert.Contains("A1 – je Geschoss", cut.Find("dl.epos-gebimport-kopf").TextContent);   // Kopf nennt die gebildete Regel
        Assert.Equal("A1", cut.Instance.Zonenregel);

        RegelWaehlen(cut, "A3");
        Assert.Equal("A3", p.Anfragen.Last().Zonenregel);
        Assert.Contains("A3 – eine Zone", cut.Find("dl.epos-gebimport-kopf").TextContent);
        Assert.Empty(cut.FindAll(".epos-gebimport-zonen"));            // eine Zone: keine Zonenliste
        Assert.NotEmpty(cut.FindAll(".epos-gebimport-bauteilliste tbody tr"));

        Ok(cut);
        Assert.Equal("A3", Assert.Single(p.Uebernommen).Zonenregel);
    }

    [Fact]
    public void Die_Bilanz_nennt_Zonen_Flaechen_Volumen_und_Summen()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p);
        Einlesen(cut);

        IElement bilanz = cut.Find("dl.epos-gebimport-bilanz");
        List<string> titel = bilanz.QuerySelectorAll("dt").Select(d => d.TextContent).ToList();
        List<string> werte = bilanz.QuerySelectorAll("dd").Select(d => d.TextContent).ToList();
        Assert.Equal(new[] { "Zonen", "Beheizte Fläche", "Beheiztes Volumen", "Σ Außenfläche", "Σ Trennfläche" }, titel);
        Assert.Equal(new[] { "2", "80 m²", "200 m³", "250,5 m²", "40 m²" }, werte);
        Assert.Empty(cut.FindAll(".epos-gebimport-zonenbanner"));      // keine Warnung
        Assert.Contains("Als Zonen mit Bauteilen übernehmen", cut.Find(".epos-gebimport-bauteile").TextContent);
    }

    [Fact]
    public void Ueber_der_Obergrenze_warnt_der_Dialog_und_der_Knopf_waehlt_die_groebere_Regel()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p);
        Einlesen(cut);
        RegelWaehlen(cut, "A2");

        IElement banner = cut.Find(".epos-gebimport-zonenbanner");
        Assert.Contains("Zu viele Zonen", banner.TextContent);
        Assert.Contains("ZU_VIELE_ZONEN_VORSCHLAG", banner.GetAttribute("data-kennung"));
        Assert.Contains("nicht möglich", cut.Find(".epos-gebimport-bauteile").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.False(cut.Instance.AlsZoneWirksam);
        Assert.NotEmpty(cut.FindAll(".epos-gebimport-flaechen-leer"));

        IElement knopf = cut.Find("button.epos-gebimport-groeber-knopf");
        Assert.Equal("Gröbere Regel übernehmen: A1 – je Geschoss", knopf.TextContent);
        knopf.Click();
        Assert.Equal("A1", p.Anfragen.Last().Zonenregel);
        Assert.Empty(cut.FindAll("button.epos-gebimport-groeber-knopf"));
        Assert.True(cut.Instance.AlsZoneWirksam);
    }

    // =====================================================================
    //  Zonen
    // =====================================================================

    [Fact]
    public void Die_Zonenliste_klappt_die_Raeume_auf_und_der_Haken_stellt_alle_Raeume_der_Zone_um()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p);
        Einlesen(cut);

        IReadOnlyList<IElement> zonen = cut.FindAll(".epos-gebimport-zonen > tbody > tr.epos-gebimport-zone");
        Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss" }, zonen.Select(z => z.GetAttribute("data-zone")));
        Assert.Contains("unter der Mindestgröße", zonen[0].TextContent);
        Assert.Contains("Zusammenlegen", cut.Markup, StringComparison.OrdinalIgnoreCase);   // benannt nicht angeboten
        Assert.Empty(cut.FindAll(".epos-gebimport-zonenraumliste"));

        IElement klappe = zonen[1].QuerySelector("button.epos-gebimport-aufklappen")!;
        Assert.Equal("false", klappe.GetAttribute("aria-expanded"));
        klappe.Click();
        IElement raeume = cut.Find(".epos-gebimport-zonenraumliste");
        Assert.Equal(new[] { "r-wohnen", "r-kueche" }, raeume.QuerySelectorAll("tr[data-raum]").Select(r => r.GetAttribute("data-raum")));
        Assert.Contains("B6", raeume.TextContent);
        Assert.Contains("Beleg Annahme", raeume.TextContent);
        Assert.Contains("EG", raeume.TextContent);

        // Der Haken „beheizt" des Kellers: sein Raum wird umgestellt, die Zonierung neu gebildet.
        Schalter(cut, "Zone „Kellergeschoss“ beheizt").Change(true);
        Assert.True(p.Anfragen.Last().BeheiztUebersteuert["r-keller"]);
        Assert.Equal("A1", p.Anfragen.Last().Zonenregel ?? "A1");
        Schalter(cut, "Zone „Kellergeschoss“ beheizt").Change(false);
        Assert.False(p.Anfragen.Last().BeheiztUebersteuert.ContainsKey("r-keller"));   // wie die Datei: keine Übersteuerung
    }

    // =====================================================================
    //  Flächen je Zone
    // =====================================================================

    [Fact]
    public void Die_Flaechen_stehen_in_der_Katalogliste_und_die_Filter_schraenken_sie_ein()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p);
        Einlesen(cut);

        IRenderedComponent<Katalogliste> liste = cut.FindComponent<Katalogliste>();
        Assert.Equal(30, cut.Instance.Flaechenzeilen.Count);
        Assert.Equal(30, liste.Instance.Angezeigt.Count);

        Schalter(cut, "nur ohne Gegenstück").Change(true);
        Assert.Equal(10, cut.Instance.Flaechenzeilen.Count);          // 0, 3, …, 27
        Schalter(cut, "nur ohne U-Wert").Change(true);
        Assert.Equal(2, cut.Instance.Flaechenzeilen.Count);           // 0 und 15
        Schalter(cut, "nur ohne Gegenstück").Change(false);
        Schalter(cut, "nur ohne U-Wert").Change(false);
        Schalter(cut, "nur Fehler").Change(true);
        Assert.Equal(14, cut.Instance.Flaechenzeilen.Count);          // Vielfache von 3 oder 5 unter 30
        Assert.Equal(14, cut.FindComponent<Katalogliste>().Instance.Angezeigt.Count);

        // Ein Neuzuordnen behält die Filter.
        RegelWaehlen(cut, "A3");
        RegelWaehlen(cut, "A1");
        Assert.Equal(14, cut.Instance.Flaechenzeilen.Count);
    }

    [Fact]
    public void Viele_Flaechen_virtualisiert_die_Liste()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p, a => Stand(a, 2400));
        Einlesen(cut);
        IRenderedComponent<Katalogliste> liste = cut.FindComponent<Katalogliste>();
        Assert.True(liste.Instance.Virtualisiert);
        Assert.Equal(Katalogliste.ZEILENHOEHE_ZEILENWAHL, liste.Instance.Zeilenhoehe);
    }

    // =====================================================================
    //  Englisch und Einzonenweg
    // =====================================================================

    [Fact]
    public void Die_Texte_der_Zonen_stehen_auch_englisch()
    {
        GebaeudeImportTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Take over as zones with components", englisch.AlsZonen);
        Assert.Equal("Zones", englisch.GruppeZonen);
        Assert.Equal("Surfaces per zone", englisch.GruppeFlaechen);
        Assert.Equal("errors only", englisch.FilterFehler);
        Assert.Equal("without counterpart only", englisch.FilterOhneGegenstueck);
        Assert.Equal("without U-value only", englisch.FilterOhneUWert);
        Assert.Equal("Use the coarser rule: {0}", englisch.GroebereRegel);
        Assert.Equal("Heated area", englisch.BilanzFlaeche);

        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p, texte: englisch);
        Einlesen(cut, englisch.DateiKnopf.TrimEnd('…', '.'));
        Assert.Contains("Take over as zones with components", cut.Find(".epos-gebimport-bauteile").TextContent);
        Assert.Contains("Surfaces per zone", cut.Markup);
        Assert.Contains("errors only", cut.Markup);
    }

    [Fact]
    public void Ohne_Zonierung_bleibt_der_Einzonenweg_unveraendert()
    {
        var p = new Protokoll();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(p, a => Stand(a) with { Zonierung = null });
        Einlesen(cut);

        Assert.DoesNotContain(cut.FindAll("select"), s => s.TextContent.Contains("A1 –"));
        Assert.Empty(cut.FindAll("dl.epos-gebimport-bilanz"));
        Assert.Empty(cut.FindAll(".epos-gebimport-zonen"));
        Assert.Empty(cut.FindAll(".epos-gebimport-flaechen"));
        Assert.Contains("Regel eins", cut.Find("dl.epos-gebimport-kopf").TextContent);
        Assert.Contains("Als Zone mit Bauteilen übernehmen", cut.Find(".epos-gebimport-bauteile").TextContent);
        Assert.Single(cut.FindAll(".epos-gebimport-bauteilliste tbody tr"));
        Assert.Null(cut.Instance.Zonenregel);
        Ok(cut);
        Assert.Null(Assert.Single(p.Uebernommen).Zonenregel);
    }

    // =====================================================================
    //  Durchgang mit der echten Hülle
    // =====================================================================

    [Fact]
    public void Mit_der_echten_Huelle_zeigt_das_Zonenhaus_drei_Zonen_und_seine_Flaechen()
    {
        string probe = Path.Combine(Wurzel(), "Referenzlaeufe", "Importproben", "ifc4_zonen.ifc");
        var huelle = new GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(probe, "ifc4_zonen.ifc", new FileInfo(probe).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
        });
        Einlesen(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zonen")));

        Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" },
                     cut.FindAll(".epos-gebimport-zonen > tbody > tr.epos-gebimport-zone").Select(z => z.GetAttribute("data-zone")));
        Assert.Equal("Z4", cut.Instance.Zonenregel);
        Assert.NotEmpty(cut.Instance.Flaechenzeilen);
        Assert.Contains(cut.Instance.Flaechenzeilen, z => z.Text("RAND") == "Nachbarzone" && z.Text("NACHBARZONE") == "Obergeschoss");
        Assert.Contains("Z4 – eine Zone je Geschoss", cut.Find("dl.epos-gebimport-kopf").TextContent);

        // Nach den Zonen der Datei: Flächen ohne Gegenstück, der Filter zeigt nur sie.
        IElement wahl = cut.FindAll("select").First(s => s.TextContent.Contains("Z4 –"));
        wahl.Change(wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Z1", StringComparison.Ordinal)).GetAttribute("value"));
        Schalter(cut, "nur ohne Gegenstück").Change(true);
        Assert.NotEmpty(cut.Instance.Flaechenzeilen);
        Assert.All(cut.Instance.Flaechenzeilen, z => Assert.Contains("ohne Gegenstück", z.Text("BEFUND")));
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}

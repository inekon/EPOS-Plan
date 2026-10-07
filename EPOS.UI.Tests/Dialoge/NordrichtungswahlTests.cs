using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// G5-N — der Baustein <see cref="Nordrichtungswahl"/>: Wert, Schnellwahl, Rundung auf 0,1°, Fehleingabe, die drehende
/// Vorschau, der Vorschlag der Datei und die Beschriftung in beiden Sprachen.
///
/// <para>Die Kultur ist auf de-DE gepinnt; der englische Fall pinnt en-US im Klammerblock.</para>
/// </summary>
public class NordrichtungswahlTests : EposBunitContext
{
    private IRenderedComponent<Nordrichtungswahl> Bauen(List<double> gemeldet, double? wert = 0.0, bool vorschlag = false,
                                                        bool unbestaetigt = false, bool aktiv = true, string hinweis = "")
        => Render<Nordrichtungswahl>(c => c
            .Add(x => x.Wert, wert)
            .Add(x => x.OnWert, EventCallback.Factory.Create<double>(this, w => gemeldet.Add(w)))
            .Add(x => x.Vorschlag, vorschlag)
            .Add(x => x.Unbestaetigt, unbestaetigt)
            .Add(x => x.Aktiv, aktiv)
            .Add(x => x.Hinweis, hinweis));

    private static IElement Feld(IRenderedComponent<Nordrichtungswahl> cut) => cut.Find("input.epos-nordwahl-wert");

    private static string Drehung(IRenderedComponent<Nordrichtungswahl> cut)
        => cut.Find("svg.epos-nordwahl-vorschau g.epos-nordwahl-pfeil").GetAttribute("transform")!;

    [Fact]
    public void Der_Wert_steht_im_Feld_und_dreht_die_Vorschau()
    {
        var gemeldet = new List<double>();
        var cut = Bauen(gemeldet, 90.0);

        Assert.Equal("90", Feld(cut).GetAttribute("value"));
        Assert.Equal("rotate(90 50 50)", Drehung(cut));
        Assert.Equal("Vorschau: Die Planoberseite zeigt nach 90°.", cut.Find("svg.epos-nordwahl-vorschau").GetAttribute("aria-label"));
        Assert.Equal("N", cut.Find("text.epos-nordwahl-nord").TextContent);

        // Ein neuer Wert von außen dreht mit.
        cut.Render(c => c.Add(x => x.Wert, 202.5));
        Assert.Equal("202,5", Feld(cut).GetAttribute("value"));
        Assert.Equal("rotate(202.5 50 50)", Drehung(cut));
        Assert.Empty(gemeldet);
    }

    [Fact]
    public void Tippen_dreht_nur_die_Vorschau_Enter_meldet_gerundet_auf_ein_Zehntel()
    {
        var gemeldet = new List<double>();
        var cut = Bauen(gemeldet);

        Feld(cut).Input("22,46");
        Assert.Equal("rotate(22.5 50 50)", Drehung(cut));
        Assert.Empty(gemeldet);

        Feld(cut).KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(new[] { 22.5 }, gemeldet);
        Assert.Equal("22,5", Feld(cut).GetAttribute("value"));

        // Das change nach dem Enter meldet denselben Wert nicht noch einmal.
        Feld(cut).Change("22,5");
        Assert.Single(gemeldet);

        // Punkt geht ebenso; change allein bestätigt.
        Feld(cut).Change("45.04");
        Assert.Equal(new[] { 22.5, 45.0 }, gemeldet);
    }

    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("359,94", 359.9)]
    [InlineData("359.96", 0.0)]
    [InlineData("12,25", 12.3)]
    public void Die_Rundung_auf_ein_Zehntel_normiert_auf_unter_360(string text, double erwartet)
    {
        Assert.True(Nordrichtungswahl.Pruefen(text, out double? w, out bool bereich));
        Assert.False(bereich);
        Assert.Equal(erwartet, w!.Value, 9);
    }

    [Theory]
    [InlineData("abc", false)]
    [InlineData("1.234,5", false)]
    [InlineData("360", true)]
    [InlineData("-0,5", true)]
    public void Eine_Fehleingabe_faerbt_und_meldet_nicht(string text, bool bereich)
    {
        var gemeldet = new List<double>();
        var cut = Bauen(gemeldet, 45.0);

        Feld(cut).Change(text);
        IElement feld = Feld(cut);
        Assert.Contains("epos-fehleingabe", feld.ClassList);
        Assert.Equal("true", feld.GetAttribute("aria-invalid"));
        Assert.Equal(bereich ? "Zulässig sind 0° bis unter 360°." : "Die Richtung der Planoberseite ist keine gültige Zahl.",
                     feld.GetAttribute("title"));
        feld.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Empty(gemeldet);
        Assert.Equal("rotate(45 50 50)", Drehung(cut));   // die Vorschau bleibt beim geltenden Wert

        Feld(cut).Input("10");
        Assert.DoesNotContain("epos-fehleingabe", Feld(cut).ClassList);
        Assert.Null(Feld(cut).GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Die_Schnellwahl_meldet_und_markiert_die_Richtung()
    {
        var gemeldet = new List<double>();
        var cut = Bauen(gemeldet);

        IReadOnlyList<IElement> knoepfe = cut.FindAll(".epos-nordwahl-schnellwahl button");
        Assert.Equal(new[] { "N", "NO", "O", "SO", "S", "SW", "W", "NW" }, knoepfe.Select(k => k.TextContent.Trim()));
        Assert.Equal("Schnellwahl der Himmelsrichtung", cut.Find(".epos-nordwahl-schnellwahl").GetAttribute("aria-label"));
        Assert.Equal("Südost", knoepfe[3].GetAttribute("aria-label"));
        Assert.Equal("true", knoepfe[0].GetAttribute("aria-pressed"));

        cut.Find("button[data-kuerzel=\"SO\"]").Click();
        Assert.Equal(new[] { 135.0 }, gemeldet);
        Assert.Equal("135", Feld(cut).GetAttribute("value"));
        Assert.Equal("rotate(135 50 50)", Drehung(cut));
        Assert.Equal("true", cut.Find("button[data-kuerzel=\"SO\"]").GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.Find("button[data-kuerzel=\"N\"]").GetAttribute("aria-pressed"));

        // Dieselbe Richtung noch einmal: nichts Neues.
        cut.Find("button[data-kuerzel=\"SO\"]").Click();
        Assert.Single(gemeldet);
    }

    [Fact]
    public void Der_Vorschlag_der_Datei_gilt_erst_mit_Uebernehmen()
    {
        var gemeldet = new List<double>();
        var cut = Render<Nordrichtungswahl>(c => c
            .Add(x => x.Wert, 30.0)
            .Add(x => x.OnWert, EventCallback.Factory.Create<double>(this, w => gemeldet.Add(w)))
            .Add(x => x.DateiText, "Die Datei nennt die Nordrichtung: Planoberseite nach 30°.")
            .Add(x => x.Vorschlag, true)
            .Add(x => x.Unbestaetigt, true));

        Assert.Contains("aus der Datei, noch nicht angewandt", cut.Find(".epos-nordwahl-datei").TextContent);
        Assert.Empty(gemeldet);

        cut.Find("button.epos-nordwahl-uebernehmen").Click();
        Assert.Equal(new[] { 30.0 }, gemeldet);

        // Unbestätigt meldet auch das bestätigte Feld mit unverändertem Wert.
        var zweite = new List<double>();
        var cut2 = Render<Nordrichtungswahl>(c => c
            .Add(x => x.Wert, 30.0)
            .Add(x => x.OnWert, EventCallback.Factory.Create<double>(this, w => zweite.Add(w)))
            .Add(x => x.Vorschlag, true)
            .Add(x => x.Unbestaetigt, true));
        cut2.Find("input.epos-nordwahl-wert").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(new[] { 30.0 }, zweite);
    }

    [Fact]
    public void Gesperrt_nennt_den_Grund_und_meldet_nichts()
    {
        var gemeldet = new List<double>();
        var cut = Bauen(gemeldet, 0.0, aktiv: false, hinweis: "Keine Quelle.");

        Assert.True(Feld(cut).HasAttribute("disabled"));
        Assert.All(cut.FindAll(".epos-nordwahl-schnellwahl button"), k => Assert.True(k.HasAttribute("disabled")));
        Assert.Equal("Keine Quelle.", cut.Find(".epos-nordwahl-hinweis").TextContent);
        Assert.Empty(cut.FindAll("button.epos-nordwahl-uebernehmen"));
        Assert.Empty(gemeldet);
    }

    [Fact]
    public void Die_Beschriftung_steht_in_beiden_Sprachen()
    {
        var gemeldet = new List<double>();
        var de = Bauen(gemeldet);
        Assert.Equal("Planoberseite zeigt nach", de.Find(".epos-feld-text").TextContent);
        Assert.StartsWith("Wohin zeigt die Planoberseite (+y der Datei)?", de.Find(".epos-nordwahl-frage").TextContent);
        string id = de.Find(".epos-nordwahl-frage").Id!;
        Assert.Equal(id, Feld(de).GetAttribute("aria-describedby"));

        using (new Kulturvorrichtung("en-US"))
        {
            var en = Bauen(gemeldet);
            Assert.Equal("Top of the plan points to", en.Find(".epos-feld-text").TextContent);
            Assert.StartsWith("Where does the top of the plan (+y of the file) point?", en.Find(".epos-nordwahl-frage").TextContent);
            Assert.Equal(new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" },
                         en.FindAll(".epos-nordwahl-schnellwahl button").Select(k => k.TextContent.Trim()));
            Assert.Equal("Quick selection of the compass direction", en.Find(".epos-nordwahl-schnellwahl").GetAttribute("aria-label"));
            Assert.Equal("Preview: the top of the plan points to 0°.", en.Find("svg.epos-nordwahl-vorschau").GetAttribute("aria-label"));
        }
    }
}

/// <summary>
/// G5-N (N1–N3) — die Nordrichtung im Zuordnungsdialog: Abschnitt unter dem Dateikopf, Warnstil ohne Dateiwert, der
/// gbXML-Vorschlag gilt erst mit „Übernehmen“, und jede Änderung ruft die Hülle, liest neu und ordnet neu zu.
/// </summary>
public class GebaeudeImportNordrichtungDialogTests : EposBunitContext
{
    public GebaeudeImportNordrichtungDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly IReadOnlyList<GebaeudeNordSchnellwahl> Schnell = new[]
    {
        new GebaeudeNordSchnellwahl("N", "Nord", 0), new GebaeudeNordSchnellwahl("NO", "Nordost", 45),
        new GebaeudeNordSchnellwahl("O", "Ost", 90), new GebaeudeNordSchnellwahl("SO", "Südost", 135),
        new GebaeudeNordSchnellwahl("S", "Süd", 180), new GebaeudeNordSchnellwahl("SW", "Südwest", 225),
        new GebaeudeNordSchnellwahl("W", "West", 270), new GebaeudeNordSchnellwahl("NW", "Nordwest", 315),
    };

    private static GebaeudeNordrichtungDaten Nord(double? datei, double planoberseite, string herkunft)
        => new(datei,
               datei is double d ? "Die Datei nennt die Nordrichtung: Planoberseite nach " + d + "°." : "Die Datei nennt keine Nordrichtung.",
               planoberseite, (360 - planoberseite) % 360, herkunft,
               herkunft == "DATEI" ? "aus der Datei" : herkunft == "EINGABE" ? "eingegeben" : "angenommen (Planoberseite = Nord)",
               "Planoberseite zeigt nach", Schnell);

    /// <summary>Was die Delegaten gesehen haben, und was die Hülle als Nordrichtung führt.</summary>
    private sealed class Pruefstand
    {
        public GebaeudeNordrichtungDaten Nord { get; set; } = null!;
        public List<double?> Gesetzt { get; } = new();
        public int Zuordnungen { get; set; }
        public TaskCompletionSource<GebaeudeLesestand>? Haelt { get; set; }
    }

    private static GebaeudeLesestand Gelesen()
        => new(true, new GebaeudeImportKopf("haus.alpha", "Format Alpha", "Schema 1", "20,1 KB", "Regel eins"),
               new[] { "Haus 1" }, Array.Empty<GebaeudeImportMeldung>());

    private IRenderedComponent<GebaeudeImportDialog> Bauen(Pruefstand p)
        => Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, new GebaeudeImportProfilDaten("Format Alpha", "(*.alpha)|*.alpha", "25 MB", new[] { "Regel eins" }, "Form_Alpha.btn_Help"));
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)Enumerable.Range(0, 13).Select(i => "Klasse " + (char)('A' + i)).ToList());
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)(
                (_, _, _) => Task.FromResult(Gelesen())));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(_ =>
            {
                p.Zuordnungen++;
                return new GebaeudeImportStand
                {
                    Kopftext = "Kopf", Vorschlagsname = "Haus", ManuellHerkunftText = "Hand",
                    Zeilen = new[]
                    {
                        new GebaeudeFeldzeileDaten
                        {
                            Zielfeld = "NUTZ", Gruppe = "Kenngrößen", Feld = "Nutzfläche", Wert = 100 + p.Zuordnungen, Einheit = "m²",
                            WertText = (100 + p.Zuordnungen).ToString(), Beleg = "B", HerkunftText = "HK", HerkunftSchluessel = "DATEI",
                            Haken = true, Eingebbar = true, HakenSetzbar = true,
                        },
                    },
                };
            }));
            c.Add(x => x.NordrichtungDaten, (Func<GebaeudeNordrichtungDaten?>)(() => p.Nord));
            c.Add(x => x.NordrichtungSetzen, (Func<double?, CancellationToken, Task<GebaeudeLesestand>>)((w, _) =>
            {
                p.Gesetzt.Add(w);
                p.Nord = Nord(p.Nord.DateiPlanoberseiteGrad, w ?? 0, "EINGABE");
                return p.Haelt?.Task ?? Task.FromResult(Gelesen());
            }));
        });

    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-nordrichtung")));
    }

    private static string Nutzwert(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-gebimport-zeilen tr[data-zielfeld=\"NUTZ\"] input").GetAttribute("value") ?? "";

    [Fact]
    public void Ohne_Dateiwert_ist_der_Abschnitt_hervorgehoben()
    {
        var p = new Pruefstand { Nord = Nord(null, 0, "ANNAHME") };
        var cut = Bauen(p);
        Assert.Empty(cut.FindAll(".epos-gebimport-nordrichtung"));   // vor dem Lesen keiner

        Einlesen(cut);
        IElement abschnitt = cut.Find(".epos-gebimport-nordrichtung");
        Assert.Equal("Nordrichtung", abschnitt.QuerySelector("h2.epos-gruppenkopf-titel")!.TextContent.Trim());
        Assert.NotNull(abschnitt.QuerySelector(".epos-nordwahl.epos-nordwahl--warnung"));
        Assert.Contains("Die Datei nennt keine Nordrichtung.", abschnitt.QuerySelector(".epos-nordwahl-datei")!.TextContent);
        Assert.Contains("angenommen (Planoberseite = Nord)", abschnitt.QuerySelector(".epos-nordwahl-herkunft")!.TextContent);
        Assert.Empty(abschnitt.QuerySelectorAll("button.epos-nordwahl-uebernehmen"));
        Assert.Empty(p.Gesetzt);

        // Die Annahme bestätigt (Nord gewählt) gilt als Eingabe: der Warnstil fällt.
        cut.Find("button[data-kuerzel=\"N\"]").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-nordwahl--warnung")));
        Assert.Equal(new double?[] { 0.0 }, p.Gesetzt);
    }

    [Fact]
    public void Ein_IFC_Dateiwert_steht_im_Feld_ohne_Warnstil_und_laesst_sich_ueberschreiben()
    {
        var p = new Pruefstand { Nord = Nord(30, 30, "DATEI") };
        var cut = Bauen(p);
        Einlesen(cut);

        Assert.Empty(cut.FindAll(".epos-nordwahl--warnung"));
        Assert.Equal("30", cut.Find("input.epos-nordwahl-wert").GetAttribute("value"));
        Assert.Contains("aus der Datei", cut.Find(".epos-nordwahl-herkunft").TextContent);
        int vorher = p.Zuordnungen;
        Assert.Equal("101", Nutzwert(cut));

        cut.Find("button[data-kuerzel=\"O\"]").Click();
        cut.WaitForAssertion(() => Assert.Equal(vorher + 1, p.Zuordnungen));
        Assert.Equal(new double?[] { 90.0 }, p.Gesetzt);
        Assert.Equal("102", Nutzwert(cut));                       // die Bauteilliste ist neu zugeordnet
        Assert.Equal("90", cut.Find("input.epos-nordwahl-wert").GetAttribute("value"));
        Assert.Contains("eingegeben", cut.Find(".epos-nordwahl-herkunft").TextContent);
        Assert.Equal("rotate(90 50 50)", cut.Find("g.epos-nordwahl-pfeil").GetAttribute("transform"));
    }

    [Fact]
    public void Der_gbXML_Vorschlag_gilt_erst_mit_Uebernehmen()
    {
        var p = new Pruefstand { Nord = Nord(30, 0, "ANNAHME") };
        var cut = Bauen(p);
        Einlesen(cut);

        Assert.NotEmpty(cut.FindAll(".epos-nordwahl--warnung"));
        Assert.Equal("30", cut.Find("input.epos-nordwahl-wert").GetAttribute("value"));
        Assert.Contains("aus der Datei, noch nicht angewandt", cut.Find(".epos-nordwahl-datei").TextContent);
        Assert.Contains("angenommen", cut.Find(".epos-nordwahl-herkunft").TextContent);
        Assert.Empty(p.Gesetzt);
        int vorher = p.Zuordnungen;

        cut.Find("button.epos-nordwahl-uebernehmen").Click();
        cut.WaitForAssertion(() => Assert.Equal(new double?[] { 30.0 }, p.Gesetzt));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-nordwahl--warnung")));
        Assert.Equal(vorher + 1, p.Zuordnungen);
        Assert.Empty(cut.FindAll("button.epos-nordwahl-uebernehmen"));
    }

    [Fact]
    public void Waehrend_des_Neulesens_ist_die_Wahl_gesperrt()
    {
        var p = new Pruefstand { Nord = Nord(null, 0, "ANNAHME"), Haelt = new TaskCompletionSource<GebaeudeLesestand>() };
        var cut = Bauen(p);
        Einlesen(cut);

        cut.Find("input.epos-nordwahl-wert").Change("45");
        cut.WaitForAssertion(() => Assert.True(cut.Find("input.epos-nordwahl-wert").HasAttribute("disabled")));
        Assert.Contains("Die Datei wird mit der neuen Nordrichtung neu gelesen", cut.Markup);
        Assert.Equal(new double?[] { 45.0 }, p.Gesetzt);

        p.Haelt.SetResult(Gelesen());
        cut.WaitForAssertion(() => Assert.False(cut.Find("input.epos-nordwahl-wert").HasAttribute("disabled")));
        Assert.Equal("45", cut.Find("input.epos-nordwahl-wert").GetAttribute("value"));
    }
}

/// <summary>
/// G5-N (N5/N6) — der Abschnitt „Ausrichtung“ im Gebäudedialog: gesperrt ohne Importquelle, Rückfrage mit der Zahl der
/// Bauteile und der Drehung, danach die Meldung und neu gelesen.
/// </summary>
public class GebaeudeDialogAusrichtungTests : EposBunitContext
{
    public GebaeudeDialogAusrichtungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private sealed class Pruefstand
    {
        public List<int> Gefragt { get; } = new();
        public List<(int IdZ, double Wert)> Geaendert { get; } = new();
        public List<int> Aufgefrischt { get; } = new();
        public double Gespeichert { get; set; }
    }

    private static GebaeudeAusrichtungDaten Daten(bool aenderbar, double planoberseite, int bauteile)
        => new(aenderbar, aenderbar ? "" : "Das Gebäude stammt aus keiner Importdatei.", planoberseite,
               aenderbar ? (360 - planoberseite) % 360 : null, aenderbar ? "DATEI" : "ANNAHME",
               aenderbar ? "aus der Datei" : "angenommen (Planoberseite = Nord)", "Ausrichtung", "Planoberseite zeigt nach",
               Array.Empty<GebaeudeNordSchnellwahl>(), bauteile);

    private IRenderedComponent<GebaeudeDialog> Bauen(Pruefstand p)
        => Render<GebaeudeDialog>(c => c
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>
            {
                new() { IdZ = 1, IdGebaeude = 7, IdKatalog = 42, Name = "Importhaus", HatProjektkopie = true, Wohnflaeche = 150 },
                new() { IdZ = 2, IdGebaeude = 8, IdKatalog = 43, Name = "Altbau", HatProjektkopie = true, Wohnflaeche = 90 },
            })
            .Add(x => x.Katalogzeilen, () => Array.Empty<Katalogfilterzeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.KatalogGaben, _ => new Dictionary<string, object>())
            .Add(x => x.Ausrichtung, z =>
            {
                p.Gefragt.Add(z.IdZ);
                return z.IdZ == 1 ? Daten(true, p.Gespeichert, 12) : Daten(false, 0, 0);
            })
            .Add(x => x.AusrichtungAendern, (z, w) =>
            {
                p.Geaendert.Add((z.IdZ, w));
                p.Gespeichert = w;
                return new GebaeudeAusrichtungErgebnis(true, "Ausrichtung geändert: 12 Bauteile gedreht, die Planoberseite zeigt jetzt nach " + w + "°.", 12, w);
            })
            .Add(x => x.ZeileAuffrischen, z => p.Aufgefrischt.Add(z.IdZ)));

    private static void Zeile(IRenderedComponent<GebaeudeDialog> cut, int index)
        => cut.FindAll(".epos-raster-huelle table.epos-raster tbody tr")[index].QuerySelector("button")!.Click();

    private static string Banner(IRenderedComponent<GebaeudeDialog> cut) => cut.Find(".epos-warnbanner-text").TextContent;

    [Fact]
    public void Ohne_Importquelle_ist_der_Abschnitt_gesperrt_mit_Hinweis()
    {
        var p = new Pruefstand();
        var cut = Bauen(p);
        Zeile(cut, 1);

        IElement abschnitt = cut.Find(".epos-gebaeude-ausrichtung");
        Assert.Equal("nein", abschnitt.GetAttribute("data-aenderbar"));
        Assert.True(abschnitt.QuerySelector("input.epos-nordwahl-wert")!.HasAttribute("disabled"));
        Assert.Equal("Das Gebäude stammt aus keiner Importdatei.", abschnitt.QuerySelector(".epos-nordwahl-hinweis")!.TextContent);
        IElement knopf = cut.Find("button.epos-gebaeude-ausrichtung-aendern");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Ausrichtung ändern…", knopf.TextContent.Trim());

        knopf.Click();   // weiche Sperre: der Versuch meldet den Grund
        Assert.Equal("Das Gebäude stammt aus keiner Importdatei.", Banner(cut));
        Assert.Null(cut.Instance.AusrichtungFrage);
        Assert.Empty(p.Geaendert);
    }

    [Fact]
    public void Aendern_fragt_mit_der_Zahl_der_Bauteile_und_meldet_das_Ergebnis()
    {
        var p = new Pruefstand { Gespeichert = 30 };
        var cut = Bauen(p);
        Zeile(cut, 0);
        Assert.Equal("30", cut.Find("input.epos-nordwahl-wert").GetAttribute("value"));

        cut.Find("button[data-kuerzel=\"O\"]").Click();          // nur die Eingabe - gedreht wird noch nichts
        Assert.Empty(p.Geaendert);
        cut.Find("button.epos-gebaeude-ausrichtung-aendern").Click();
        Assert.Equal("Alle 12 Bauteile des Gebäudes werden um 60° gedreht. Fortfahren?", cut.Instance.AusrichtungFrage);
        Assert.Equal("Alle 12 Bauteile des Gebäudes werden um 60° gedreht. Fortfahren?", cut.Find(".epos-rueckfrage-text").TextContent);

        cut.FindAll(".epos-rueckfrage button").First(k => k.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { (1, 90.0) }, p.Geaendert);
        Assert.Equal("Ausrichtung geändert: 12 Bauteile gedreht, die Planoberseite zeigt jetzt nach 90°.", Banner(cut));
        Assert.Equal(new[] { 1 }, p.Aufgefrischt);
        Assert.Equal(2, p.Gefragt.Count(i => i == 1));            // neu gelesen
        Assert.Equal("90", cut.Find("input.epos-nordwahl-wert").GetAttribute("value"));
        Assert.Null(cut.Instance.AusrichtungFrage);
    }

    [Fact]
    public void Nein_dreht_nichts_und_unveraendert_fragt_nicht()
    {
        var p = new Pruefstand { Gespeichert = 0 };
        var cut = Bauen(p);
        Zeile(cut, 0);

        cut.Find("button.epos-gebaeude-ausrichtung-aendern").Click();
        Assert.Null(cut.Instance.AusrichtungFrage);
        Assert.Equal("Die eingegebene Richtung entspricht der gespeicherten — es gibt nichts zu drehen.", Banner(cut));

        cut.Find("input.epos-nordwahl-wert").Change("270");
        cut.Find("button.epos-gebaeude-ausrichtung-aendern").Click();
        Assert.Equal("Alle 12 Bauteile des Gebäudes werden um 270° gedreht. Fortfahren?", cut.Instance.AusrichtungFrage);
        cut.FindAll(".epos-rueckfrage button").First(k => k.TextContent.Trim() == "Nein").Click();
        Assert.Empty(p.Geaendert);
        Assert.Empty(p.Aufgefrischt);
        Assert.Null(cut.Instance.AusrichtungFrage);
    }
}

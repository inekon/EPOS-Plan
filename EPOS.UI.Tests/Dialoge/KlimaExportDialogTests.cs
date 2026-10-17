using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Export;
using EPOS.UI.Dialoge.Klimadaten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Klimaexport</b> (Auftrag KD-5): Parameterwahl, Format, Vorschau, der OK-Weg über die Naht mit dem erwarteten
/// Inhalt, die abgebrochene Dateiwahl, die Region ohne Rohdaten, der Fall ohne Gaben und die Handlung „Exportieren…“
/// in der Auswahlleiste des Katalogdialogs.
/// </summary>
public class KlimaExportDialogTests : EposBunitContext
{
    public KlimaExportDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Eine erfundene Region: Rechenwerte (UTC) Stunde · 0,5, Feuchte 50 %; mit Rohdaten Luftdruck 1000 + Stunde (MEZ).</summary>
    private static KlimaExportDaten Region(bool rohdaten)
    {
        var d = new KlimaExportDaten
        {
            IdRegion = 7, Name = "Probeort", Quelle = "TRY_Datei", Latitude = 48.78, Longitude = 9.18, Rohdaten = rohdaten,
            RohZeitbezug = KlimaRohwerteSchema.ZEITBEZUG_MEZ
        };
        foreach (KlimaExportGroesse g in KlimaExportOptionen.Groessen.Where(g => g.Art == KlimaWertart.Rechenwert))
            d.Rechenwerte[g.Spalte] = Enumerable.Range(0, 8760).Select(i => g.Spalte == "Luftfeuchte" ? 50.0 : i * 0.5).ToArray();
        if (rohdaten)
            foreach (string s in KlimaRohwerteSchema.ROHSPALTEN)
                d.Rohwerte[s] = Enumerable.Range(0, 8760).Select(i => s == KlimaRohwerteSchema.SPALTE_GLOBAL ? double.NaN : 1001.0 + i).ToArray();
        return d;
    }

    private sealed class Stand
    {
        internal readonly List<(string Inhalt, string Vorschlag)> Gespeichert = new();
        internal readonly List<string?> Geschlossen = new();
        internal KlimaExportErgebnis Ergebnis = new(true, false, "Gespeichert: /probe/Klimadaten_Probeort.csv");
    }

    private IRenderedComponent<KlimaExportDialog> Aufbauen(Stand s, KlimaExportDaten? daten)
        => Render<KlimaExportDialog>(p => p
            .Add(x => x.Lesen, () => Task.FromResult(daten))
            .Add(x => x.Speichern, (inhalt, vorschlag) =>
            {
                s.Gespeichert.Add((inhalt, vorschlag));
                return Task.FromResult(s.Ergebnis);
            })
            .Add(x => x.Geschlossen, e => s.Geschlossen.Add(e)));

    private static void Waehlen(IRenderedComponent<KlimaExportDialog> cut, string gruppe, int wert)
        => cut.Find($"input[name='klimaexport-{gruppe}'][value='{wert}']").Change(true);

    [Fact]
    public void Vorgabe_Vorschau_und_Formatwahl()
    {
        var s = new Stand();
        var cut = Aufbauen(s, Region(rohdaten: true));
        cut.WaitForAssertion(() => Assert.Contains("Stunde;", cut.Find("pre.epos-klimaexport-vorschau").TextContent));

        // Vorgabe (KD-Q8): MEZ, Semikolon, Komma, Stunde; fünf Rechenwerte, Herkunftsblock.
        Assert.Equal(KlimaZeitbezug.Mez, cut.Instance.Optionen.Zeitbezug);
        string vorschau = cut.Instance.Vorschautext;
        Assert.StartsWith("# ", vorschau);
        Assert.Contains(R.KEXP_SP_STUNDE + ";" + R.KEXP_G_TEMPERATUR + " [°C];", vorschau);
        Assert.Contains("\r\n1;4379,5;50,0;", vorschau); // MEZ-Stunde 1 = letzte UTC-Stunde

        // Komma als Trennzeichen zieht den Punkt nach; Zeitspalte ISO, Zeitbezug UTC.
        Waehlen(cut, "trennzeichen", 1);
        Assert.Equal(KlimaDezimalzeichen.Punkt, cut.Instance.Optionen.Dezimalzeichen);
        Waehlen(cut, "zeitspalte", 3);
        Waehlen(cut, "zeitbezug", 1);
        vorschau = cut.Instance.Vorschautext;
        Assert.Contains("\r\n2025-01-01T00:00Z,0.0,50.0,", vorschau);
        Assert.Equal(KlimaCsv.Vorschau(Region(true), cut.Instance.Optionen), vorschau);

        // Komma als Dezimalzeichen ist neben dem Trennzeichen Komma weich gesperrt.
        var komma = cut.Find("input[name='klimaexport-dezimal'][value='0']");
        Assert.Equal("true", komma.GetAttribute("aria-disabled"));

        // Vorlage Haus setzt zurück.
        cut.Find("button.epos-klimaexport-haus").Click();
        Assert.Equal((KlimaTrennzeichen.Semikolon, KlimaDezimalzeichen.Komma, KlimaZeitspalte.Stunde),
                     (cut.Instance.Optionen.Trennzeichen, cut.Instance.Optionen.Dezimalzeichen, cut.Instance.Optionen.Zeitspalte));
    }

    [Fact]
    public void Parameterwahl_mit_Rohwerten_und_gekennzeichneten_gerechneten_Groessen()
    {
        var s = new Stand();
        var cut = Aufbauen(s, Region(rohdaten: true));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-gruppe=Rohwerte] input[type=checkbox]")));
        Assert.Contains(R.KEXP_GERECHNET, cut.Find("[data-gruppe=Abgeleitet]").TextContent);
        Assert.Contains(R.KEXP_GERECHNET, cut.Find("[data-gruppe=Fassadenwerte]").TextContent);
        Assert.DoesNotContain(R.KEXP_GERECHNET, cut.Find("[data-gruppe=Rechenwerte]").TextContent);

        // Taupunkt dazu: Spalte in der Vorschau.
        cut.Find("[data-gruppe=Abgeleitet] input[type=checkbox]").Change(true);
        Assert.Contains(KlimaExportOptionen.A_TAUPUNKT, cut.Instance.Optionen.Parameter);
        Assert.Contains(R.KEXP_G_TAUPUNKT + " [°C]", cut.Instance.Vorschautext);

        // Globalstrahlung roh führt die Quelle nicht: weich gesperrt mit Grund.
        string name = string.Format(R.KEXP_ROH_NAME, R.KEXP_G_GLOBAL);
        var global = cut.FindAll("[data-gruppe=Rohwerte] label").Single(l => l.TextContent.Contains(R.KEXP_G_GLOBAL));
        Assert.Equal(R.KEXP_GRUND_NICHT_GEFUEHRT, global.GetAttribute("title"));
        Assert.DoesNotContain(name, cut.Instance.Vorschautext);
    }

    [Fact]
    public void Export_ruft_die_Naht_mit_dem_erwarteten_Inhalt_und_schliesst()
    {
        var s = new Stand();
        KlimaExportDaten daten = Region(rohdaten: true);
        var cut = Aufbauen(s, daten);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.Instance.Vorschautext));
        Waehlen(cut, "kopf", 1);
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();

        cut.WaitForAssertion(() => Assert.Single(s.Geschlossen));
        var (inhalt, vorschlag) = Assert.Single(s.Gespeichert);
        Assert.Equal(KlimaCsv.Text(daten, cut.Instance.Optionen), inhalt);
        Assert.Equal(string.Format(R.KEXP_DATEI, "Probeort"), vorschlag);
        Assert.Equal(2 + 8760, inhalt.Split("\r\n").Count(z => z.Length > 0 && !z.StartsWith('#')));
        Assert.Equal(s.Ergebnis.Text, s.Geschlossen[0]);
    }

    [Fact]
    public void Abgebrochene_Dateiwahl_laesst_den_Dialog_offen_Fehler_steht_im_Banner()
    {
        var s = new Stand { Ergebnis = KlimaExportErgebnis.Abgebrochen };
        var cut = Aufbauen(s, Region(rohdaten: false));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.Instance.Vorschautext));
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        cut.WaitForAssertion(() => Assert.Single(s.Gespeichert));
        Assert.Empty(s.Geschlossen);
        Assert.Equal("", cut.Instance.Meldung);

        s.Ergebnis = new KlimaExportErgebnis(false, false, "Platte voll");
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        cut.WaitForAssertion(() => Assert.Equal("Platte voll", cut.Instance.Meldung));
        Assert.Empty(s.Geschlossen);

        // Abbrechen gibt null und ruft den Speicherweg nicht.
        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == R.ALLG_BTN_ABBRECHEN).Click();
        Assert.Equal(new string?[] { null }, s.Geschlossen);
        Assert.Equal(2, s.Gespeichert.Count);
    }

    [Fact]
    public void Region_ohne_Rohdaten_nennt_den_Grund_und_bietet_nur_Rechenwerte()
    {
        var s = new Stand();
        var cut = Aufbauen(s, Region(rohdaten: false));
        cut.WaitForAssertion(() => Assert.Equal(R.KEXP_GRUND_OHNE_ROHDATEN, cut.Find(".epos-klimaexport-ohneroh").TextContent.Trim()));
        foreach (var l in cut.FindAll("[data-gruppe=Rohwerte] label"))
            Assert.Equal(R.KEXP_GRUND_OHNE_ROHDATEN, l.GetAttribute("title"));
        cut.FindAll("[data-gruppe=Rohwerte] button").First(b => b.TextContent.Trim() == R.KEXP_ALLE).Click();
        Assert.DoesNotContain(cut.Instance.Optionen.Parameter, p => p.StartsWith(KlimaExportOptionen.ROH_PREFIX, StringComparison.Ordinal));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_Abbrechen_gibt_null()
    {
        string? geschlossen = "offen";
        var cut = Render<KlimaExportDialog>(p => p.Add(x => x.Geschlossen, e => geschlossen = e));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-klimaexport-keinedaten")));
        Assert.Empty(cut.FindAll("pre.epos-klimaexport-vorschau"));
        cut.FindAll(".epos-leiste button").First(b => b.TextContent.Trim() == R.ALLG_BTN_ABBRECHEN).Click();
        Assert.Null(geschlossen);
    }

    [Fact]
    public void Handlung_Exportieren_in_der_Auswahlleiste_oeffnet_die_Ueberlagerung()
    {
        string? gelesen = null;
        var cut = Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new List<Katalogfilterzeile>
            {
                new(17, "Berlin"), new(42, "Stuttgart")
            }))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.ExportLesen, n => { gelesen = n; return Task.FromResult<KlimaExportDaten?>(Region(false)); })
            .Add(x => x.ExportSpeichern, (_, _) => Task.FromResult(KlimaExportErgebnis.Abgebrochen)));
        cut.WaitForAssertion(() => Assert.Contains(R.KEXP_HANDLUNG, cut.Find(".epos-auswahlleiste").TextContent));
        cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim() == R.KEXP_HANDLUNG).Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-klimaexport-dialog")));
        Assert.Equal("Berlin", gelesen);
        // Die Überlagerung trägt den Titel, der Dialog darin keinen eigenen Kopf.
        Assert.Empty(cut.FindAll(".epos-klimaexport-dialog h1"));

        // Ohne Speicherweg steht die Handlung nicht da.
        var ohne = Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new List<Katalogfilterzeile> { new(17, "Berlin") }))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.ExportLesen, _ => Task.FromResult<KlimaExportDaten?>(null)));
        ohne.WaitForAssertion(() => Assert.Single(ohne.FindAll(".epos-auswahlleiste")));
        Assert.DoesNotContain(R.KEXP_HANDLUNG, ohne.Find(".epos-auswahlleiste").TextContent);
    }
}

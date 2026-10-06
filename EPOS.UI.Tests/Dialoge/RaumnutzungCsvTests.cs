using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>CSV-Import und -Export der Nutzungsprofile</b> (Stufe NP4a; Konzept Nutzungsprofile 6.1, 6.4, NP-F11, NP-F20) über
/// einem Weg OHNE Datenbank — die Delegaten merken sich, was sie bekommen haben. Geprüft werden: der Fall ohne Gaben,
/// „kein Delegat, kein Knopf", die Vorschau mit Stand und Meldungen, die Zielliste nur mit eigenen Kategorien, die weiche
/// Sperre von „Übernehmen" mit Grund, die Rückfrage ersetzen/überspringen/abbrechen, Abbrechen ohne zu schreiben, der
/// Export und die Einbindung im Blatt.
/// </summary>
/// <remarks>Die Kultur ist über <see cref="EposBunitContext"/> auf de-DE gepinnt.</remarks>
public class RaumnutzungCsvTests : EposBunitContext
{
    private const long KAT_MUSTER = 1;
    private const long KAT_EIGEN = 2;

    private static readonly IReadOnlyList<RaumnutzungKategorieDaten> KATEGORIEN = new[]
    {
        new RaumnutzungKategorieDaten(KAT_MUSTER, "Muster", RaumnutzungArt.EposMuster, true, "", ""),
        new RaumnutzungKategorieDaten(KAT_EIGEN, "Eigene", RaumnutzungArt.Eigen, false, "", ""),
    };

    /// <summary>Ein Weg, der mitschreibt — die Rolle der Hülle im Prüfstand.</summary>
    private sealed class Probeweg
    {
        internal readonly List<string> Spur = new();
        internal RaumnutzungCsvDatei? Datei = new("profile.csv", new byte[] { 1 });
        internal Exception? Lesefehler;
        internal int Vorhanden;
        internal int Abgelehnt;
        internal int Neu = 2;
        internal RaumnutzungErgebnis Antwort = new(true, "2 Profile angelegt.");
        internal RaumnutzungErgebnis Exportantwort = new(true, "gespeichert");

        internal RaumnutzungCsvWeg Weg(bool mitImport = true, bool mitExport = true) => new()
        {
            DateiWaehlen = !mitImport ? null : () =>
            {
                Spur.Add("Wahl");
                if (Lesefehler is not null) throw Lesefehler;
                return Task.FromResult(Datei);
            },
            Pruefen = !mitImport ? null : (d, ziel) =>
            {
                Spur.Add("Pruefen:" + (ziel?.ToString() ?? "neu"));
                return Vorschau(ziel);
            },
            Uebernehmen = !mitImport ? null : (d, ziel, name, ersetzen) =>
            {
                Spur.Add("Uebernehmen:" + (ziel?.ToString() ?? "neu") + ":" + name + ":" + ersetzen);
                return Antwort;
            },
            Exportieren = !mitExport ? null : id =>
            {
                Spur.Add("Export:" + id);
                return Task.FromResult(Exportantwort);
            },
        };

        private RaumnutzungCsvVorschau Vorschau(long? ziel)
        {
            var profile = new List<RaumnutzungCsvProfilzeile>();
            int zeile = 2;
            for (int i = 0; i < Neu; i++)
                profile.Add(new(zeile++, "N" + i, "Neu " + i, RaumnutzungCsvStand.Neu, "", 3, 0, 0));
            for (int i = 0; i < (ziel is null ? 0 : Vorhanden); i++)
                profile.Add(new(zeile++, "V" + i, "Vorhanden " + i, RaumnutzungCsvStand.Vorhanden, "", 3, 0, 0));
            for (int i = 0; i < Abgelehnt; i++)
                profile.Add(new(zeile++, "", "Abgelehnt " + i, RaumnutzungCsvStand.Abgelehnt, "ohne Bezeichner", 0, 0, 1));
            var meldungen = new List<RaumnutzungCsvMeldungDaten>
            {
                new(1, "Unbekannt", RaumnutzungCsvMeldungsart.Ignoriert, "unbekannte Spalte"),
                new(2, "Heiz_Soll", RaumnutzungCsvMeldungsart.Uebernommen, "20"),
                new(2, "Kuehl_Soll", RaumnutzungCsvMeldungsart.Fehler, "99 liegt außerhalb 15 … 35"),
            };
            return new RaumnutzungCsvVorschau(null, profile, meldungen);
        }
    }

    private IRenderedComponent<RaumnutzungCsvAustausch> Zeichnen(Probeweg w, RaumnutzungCsvWeg? weg = null,
                                                        IReadOnlyList<RaumnutzungKategorieDaten>? kategorien = null,
                                                        Action? geaendert = null)
        => Render<RaumnutzungCsvAustausch>(p => p
            .Add(x => x.Weg, weg ?? w.Weg())
            .Add(x => x.Kategorien, kategorien ?? KATEGORIEN)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    private static IElement Knopf(IRenderedComponent<RaumnutzungCsvAustausch> cut, string klasse) => cut.Find("button." + klasse);

    [Fact]
    public void Ohne_Gaben_steht_nichts_da()
    {
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Render<RaumnutzungCsvAustausch>();
        Assert.Empty(cut.FindAll(".epos-raumnutzung-csv"));
        IRenderedComponent<RaumnutzungCsvAustausch> leer = Render<RaumnutzungCsvAustausch>(p => p.Add(x => x.Weg, new RaumnutzungCsvWeg()));
        Assert.Empty(leer.FindAll("button"));
    }

    [Fact]
    public void Kein_Delegat_kein_Knopf()
    {
        var w = new Probeweg();
        IRenderedComponent<RaumnutzungCsvAustausch> nurExport = Zeichnen(w, w.Weg(mitImport: false));
        Assert.Empty(nurExport.FindAll("button.epos-raumnutzung-csv-import"));
        Assert.Single(nurExport.FindAll("button.epos-raumnutzung-csv-export"));

        IRenderedComponent<RaumnutzungCsvAustausch> nurImport = Zeichnen(w, w.Weg(mitExport: false));
        Assert.Single(nurImport.FindAll("button.epos-raumnutzung-csv-import"));
        Assert.Empty(nurImport.FindAll("button.epos-raumnutzung-csv-export"));

        // Ohne Kategorien gibt es nichts zu exportieren.
        IRenderedComponent<RaumnutzungCsvAustausch> ohne = Zeichnen(w, kategorien: Array.Empty<RaumnutzungKategorieDaten>());
        Assert.Empty(ohne.FindAll("button.epos-raumnutzung-csv-export"));
    }

    [Fact]
    public void Der_Import_zeigt_Vorschau_und_Meldungen_und_schreibt_erst_mit_Uebernehmen()
    {
        var w = new Probeweg { Abgelehnt = 1 };
        int geaendert = 0;
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w, geaendert: () => geaendert++);
        Knopf(cut, "epos-raumnutzung-csv-import").Click();

        Assert.True(cut.Instance.ImportOffen);
        Assert.Contains("profile.csv", cut.Find(".epos-raumnutzung-csv-datei").TextContent);
        Assert.Equal(new[] { "Wahl", "Pruefen:neu" }, w.Spur);
        // Ohne eigene Wahl: neue Kategorie, Name aus dem Dateinamen.
        Assert.Equal("profile", cut.Find(".epos-raumnutzung-csv-importbereich input").GetAttribute("value"));

        IReadOnlyList<IElement> zeilen = cut.FindAll("tr.epos-raumnutzung-csv-profil");
        Assert.Equal(3, zeilen.Count);
        Assert.Single(cut.FindAll("tr.epos-raumnutzung-csv-profil--abgelehnt"));
        Assert.Contains("nicht übernommen", zeilen[2].TextContent);
        // Die Liste zeigt ignorierte Werte und Fehler, nicht die übernommenen.
        IReadOnlyList<IElement> hinweise = cut.FindAll("tr.epos-raumnutzung-csv-hinweis");
        Assert.Equal(2, hinweise.Count);
        Assert.Single(cut.FindAll("tr.epos-raumnutzung-csv-hinweis--fehler"));
        Assert.Contains("Kuehl_Soll", hinweise[1].TextContent);

        Assert.Null(Knopf(cut, "epos-raumnutzung-csv-uebernehmen").GetAttribute("aria-disabled"));
        Knopf(cut, "epos-raumnutzung-csv-uebernehmen").Click();
        Assert.Equal("Uebernehmen:neu:profile:False", w.Spur.Last());
        Assert.False(cut.Instance.ImportOffen);
        Assert.Equal(1, geaendert);
        Assert.Contains("2 Profile angelegt.", cut.Find(".epos-raumnutzung-csv-meldung").TextContent);
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_schreiben_und_eine_abgebrochene_Wahl_oeffnet_nichts()
    {
        var w = new Probeweg();
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w);
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        Knopf(cut, "epos-raumnutzung-csv-abbrechen").Click();
        Assert.False(cut.Instance.ImportOffen);
        Assert.DoesNotContain(w.Spur, s => s.StartsWith("Uebernehmen", StringComparison.Ordinal));

        w.Datei = null;
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        Assert.False(cut.Instance.ImportOffen);

        w.Lesefehler = new InvalidOperationException("Die Datei lässt sich nicht lesen: gesperrt");
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        Assert.Contains("gesperrt", cut.Find(".epos-raumnutzung-csv-fehler").TextContent);
    }

    [Fact]
    public void Die_Zielliste_fuehrt_nur_eigene_Kategorien()
    {
        var w = new Probeweg();
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w);
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        string[] texte = cut.FindAll(".epos-raumnutzung-csv-importbereich select option").Select(o => o.TextContent.Trim()).ToArray();
        Assert.Contains("(neue Kategorie)", texte);
        Assert.Contains("Eigene", texte);
        Assert.DoesNotContain("Muster", texte);

        cut.Find(".epos-raumnutzung-csv-importbereich select").Change("1");
        Assert.Equal("Pruefen:" + KAT_EIGEN, w.Spur.Last());
        Assert.Empty(cut.FindAll(".epos-raumnutzung-csv-importbereich input"));   // kein Name bei eigener Kategorie
    }

    [Fact]
    public void Uebernehmen_ist_weich_gesperrt_und_nennt_den_Grund()
    {
        var w = new Probeweg { Neu = 0, Abgelehnt = 2 };
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w);
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        IElement knopf = Knopf(cut, "epos-raumnutzung-csv-uebernehmen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Die Datei enthält kein übernehmbares Profil.", knopf.GetAttribute("title"));
        knopf.Click();
        Assert.Contains("kein übernehmbares Profil", cut.Find(".epos-raumnutzung-csv-fehler").TextContent);
        Assert.DoesNotContain(w.Spur, s => s.StartsWith("Uebernehmen", StringComparison.Ordinal));

        // Mit Profilen, aber ohne Namen der neuen Kategorie.
        var w2 = new Probeweg();
        IRenderedComponent<RaumnutzungCsvAustausch> cut2 = Zeichnen(w2);
        Knopf(cut2, "epos-raumnutzung-csv-import").Click();
        cut2.Find(".epos-raumnutzung-csv-importbereich input").Input("");
        Assert.Equal("Der Name der neuen Kategorie fehlt.", Knopf(cut2, "epos-raumnutzung-csv-uebernehmen").GetAttribute("title"));
    }

    [Theory]
    [InlineData(0, "True")]
    [InlineData(1, "False")]
    [InlineData(2, null)]
    public void Vorhandene_Profile_fragen_ersetzen_ueberspringen_oder_abbrechen(int knopf, string? ersetzen)
    {
        var w = new Probeweg { Vorhanden = 1 };
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w);
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        cut.Find(".epos-raumnutzung-csv-importbereich select").Change("1");
        Knopf(cut, "epos-raumnutzung-csv-uebernehmen").Click();

        IElement frage = cut.Find(".epos-rueckfrage");
        Assert.Contains("1 Profile", frage.TextContent);
        IReadOnlyList<IElement> knoepfe = frage.QuerySelectorAll("button").ToList();
        Assert.Equal(new[] { "Ersetzen", "Überspringen", "Abbrechen" }, knoepfe.Select(k => k.TextContent.Trim()));
        knoepfe[knopf].Click();

        if (ersetzen is null)
        {
            Assert.DoesNotContain(w.Spur, s => s.StartsWith("Uebernehmen", StringComparison.Ordinal));
            Assert.True(cut.Instance.ImportOffen);
        }
        else
        {
            Assert.Equal("Uebernehmen:" + KAT_EIGEN + "::" + ersetzen, w.Spur.Last());
            Assert.False(cut.Instance.ImportOffen);
        }
    }

    [Fact]
    public void Eine_Ablehnung_haelt_den_Bereich_offen_und_nennt_sie()
    {
        var w = new Probeweg { Antwort = new RaumnutzungErgebnis(false, "Der Name „Eigene“ ist schon vergeben.") };
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w);
        Knopf(cut, "epos-raumnutzung-csv-import").Click();
        Knopf(cut, "epos-raumnutzung-csv-uebernehmen").Click();
        Assert.True(cut.Instance.ImportOffen);
        Assert.Contains("schon vergeben", cut.Find(".epos-raumnutzung-csv-fehler").TextContent);
    }

    [Fact]
    public void Der_Export_waehlt_eine_Kategorie_auch_eine_ausgelieferte()
    {
        var w = new Probeweg();
        IRenderedComponent<RaumnutzungCsvAustausch> cut = Zeichnen(w);
        Knopf(cut, "epos-raumnutzung-csv-export").Click();
        Assert.Equal(new[] { "Muster", "Eigene" },
                     cut.FindAll(".epos-raumnutzung-csv-exportbereich select option").Select(o => o.TextContent.Trim()).Where(t => t.Length > 0));
        Knopf(cut, "epos-raumnutzung-csv-speichern").Click();
        Assert.Equal("Export:" + KAT_MUSTER, w.Spur.Last());
        Assert.Contains("gespeichert", cut.Find(".epos-raumnutzung-csv-meldung").TextContent);
        Assert.Empty(cut.FindAll(".epos-raumnutzung-csv-exportbereich"));

        // Abgebrochene Dateiwahl: der Bereich bleibt offen, keine Meldung.
        w.Exportantwort = new RaumnutzungErgebnis(true, "");
        Knopf(cut, "epos-raumnutzung-csv-export").Click();
        cut.Find(".epos-raumnutzung-csv-exportbereich select").Change("1");
        Knopf(cut, "epos-raumnutzung-csv-speichern").Click();
        Assert.Equal("Export:" + KAT_EIGEN, w.Spur.Last());
        Assert.Single(cut.FindAll(".epos-raumnutzung-csv-exportbereich"));
        Assert.Empty(cut.FindAll(".epos-raumnutzung-csv-meldung"));
    }

    [Fact]
    public void Das_Blatt_bindet_den_Baustein_im_Kopf_ein()
    {
        var w = new Probeweg();
        var weg = new RaumnutzungWeg
        {
            Kategorien = () => KATEGORIEN,
            Profile = id => Array.Empty<RaumnutzungProfilDaten>(),
            Zuordnungen = () => Array.Empty<RaumnutzungZuordnungDaten>(),
            Csv = w.Weg(),
        };
        int geaendert = 0;
        Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());   // die Hilfepille des Blatts
        IRenderedComponent<RaumnutzungBlatt> blatt = Render<RaumnutzungBlatt>(p => p
            .Add(x => x.Katalogweg, weg)
            .Add(x => x.Geaendert, () => geaendert++));
        Assert.Single(blatt.FindAll("button.epos-raumnutzung-csv-import"));
        blatt.Find("button.epos-raumnutzung-csv-import").Click();
        blatt.Find("button.epos-raumnutzung-csv-uebernehmen").Click();
        Assert.Equal(1, geaendert);

        IRenderedComponent<RaumnutzungBlatt> ohne = Render<RaumnutzungBlatt>(p => p
            .Add(x => x.Katalogweg, new RaumnutzungWeg { Kategorien = () => KATEGORIEN, Profile = id => Array.Empty<RaumnutzungProfilDaten>() }));
        Assert.Empty(ohne.FindAll(".epos-raumnutzung-csv"));
    }
}

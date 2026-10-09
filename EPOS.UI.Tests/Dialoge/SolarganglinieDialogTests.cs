using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Solarthermieganglinien — EIN Dialog für Projekt und Katalog: zwei Listen, die
/// Übernahmeleiste, Name und Beschreibung als Anzeige, OK und Abbrechen; an der
/// Katalogseite Vergleichen, Schloss, Löschen und Import (Baustein
/// <c>GanglinieKatalogseite</c>). Ohne Projekt (Katalogbetrieb) nur die Katalogseite und
/// „Beenden".
/// </summary>
public class SolarganglinieDialogTests : EposBunitContext
{
    /// <summary>
    /// Der Katalog als <see cref="Katalogfilterzeile"/> — seit Stufe S3.2
    /// (W14a-E-10) traegt die Liste Beschreibung, Jahresarbeit und Spitze und steht
    /// im Baustein <c>Katalogliste</c>.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalog => new[]
    {
        Zeitreihenproben.Zeile(21, "Ganglinie Nord",
                               beschreibung: "Messreihe 2024, Standort Nord",
                               jahresarbeitMwh: 3.9, spitzeKw: 5.4),
        Zeitreihenproben.Zeile(22, "Ganglinie Süd",
                               beschreibung: "Messreihe 2024, Standort Süd",
                               jahresarbeitMwh: 4.2, spitzeKw: 6.0)
    };

    public SolarganglinieDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Zeile(int schluessel, string name, int ganglinieId)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = ganglinieId };

    private IRenderedComponent<SolarganglinieDialog> Aufbauen(
        List<ErzeugerZeile>? zeilen = null,
        Func<int, ErzeugerZeile?>? aufnehmen = null,
        Action<ErzeugerZeile>? entfernen = null,
        GanglinienKatalogwege? wege = null,
        Action<bool>? geschlossen = null,
        bool katalogbetrieb = false)
        => Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 21) })
            .Add(x => x.Katalogbetrieb, katalogbetrieb)
            .Add(x => x.Katalogwege, wege ?? Wege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, aufnehmen ?? (id => Zeile(100000, "Ganglinie Süd", id)))
            .Add(x => x.Entfernen, entfernen)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    /// <summary>Die Wege der Katalogseite mit dem Probekatalog — ohne Import.</summary>
    private static GanglinienKatalogwege Wege(IReadOnlyList<Katalogfilterzeile>? katalog = null)
        => new() { Katalogzeilen = () => Task.FromResult(katalog ?? Katalog) };

    private static IElement Knopf(IRenderedComponent<SolarganglinieDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);

        var ueberschriften = cut.FindAll(".epos-untergruppe").Select(e => e.TextContent).ToList();
        Assert.Contains("Ausgewählt im Projekt", ueberschriften);
        Assert.Contains("Solarthermieganglinie aus DB", ueberschriften);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Name:", texte);
        Assert.Contains("Beschreibung:", texte);

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Bearbeiten...", knoepfe);
        Assert.Contains("OK", knoepfe);
        Assert.Contains("Abbrechen", knoepfe);
    }

    [Fact]
    public void Name_und_Beschreibung_sind_nur_lesbar()
    {
        var cut = Aufbauen();
        Assert.Single(cut.FindAll("input[readonly]"));       // Name
        Assert.Single(cut.FindAll("textarea[readonly]"));    // Beschreibung
    }

    /// <summary>Die Maske ist lokalisiert (6 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogwege, Wege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.TitelText, "Solar thermal energy curves")
            .Add(x => x.LabelProjektliste, "Selected in the project")
            .Add(x => x.LabelBeschreibung, "Description:"));

        Assert.Equal("Solar thermal energy curves", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Selected in the project",
                        cut.FindAll(".epos-untergruppe").Select(e => e.TextContent));
        Assert.Contains("Description:", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    // =================================================================================
    // Auswahl
    // =================================================================================

    [Fact]
    public void Die_Wahl_zeigt_Name_UND_Beschreibung()
    {
        // A-27: Der Vorlaeufer setzte nur den Namen; sein Beschreibungsfeld blieb in
        // JEDEM Zustand leer, obwohl der Katalogsatz sie fuehrt.
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0].Click();

        Assert.Equal("Ganglinie Nord", cut.Find("input[readonly]").GetAttribute("value"));
        Assert.Equal("Messreihe 2024, Standort Nord", cut.Find("textarea[readonly]").TextContent);
    }

    [Fact]
    public void Auch_eine_Projektzeile_zeigt_ihre_Beschreibung()
    {
        var cut = Aufbauen();
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr button")[0].Click();

        Assert.Equal("Messreihe 2024, Standort Nord", cut.Find("textarea[readonly]").TextContent);
    }

    // =================================================================================
    // Aufnehmen und Entfernen
    // =================================================================================

    [Fact]
    public void Der_linke_Pfeil_ist_ohne_Katalogwahl_gesperrt()
    {
        var cut = Aufbauen();
        Assert.True(cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_linke_Pfeil_legt_eine_Zeile_an()
    {
        var zeilen = new List<ErzeugerZeile> { Zeile(1, "Ganglinie Nord", 21) };
        int gerufen = 0;
        var cut = Aufbauen(zeilen, aufnehmen: id =>
        {
            gerufen = id;
            return Zeile(100000, "Ganglinie Süd", id);
        });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[1].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        Assert.Equal(22, gerufen);
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("Ganglinie Süd", cut.Instance.Projektzeile!.Bezeichner);
    }

    [Fact]
    public void Der_rechte_Pfeil_entfernt_GENAU_die_gewaehlte_Zeile()
    {
        // Der Vorlaeufer suchte die erste Zeile mit demselben Namen - bei zwei
        // Zuordnungen derselben Ganglinie also nicht zwingend die markierte.
        var a = Zeile(1, "Ganglinie Nord", 21);
        var b = Zeile(2, "Ganglinie Nord", 21);
        var zeilen = new List<ErzeugerZeile> { a, b };

        var entfernt = new List<ErzeugerZeile>();
        var cut = Aufbauen(zeilen, entfernen: z => entfernt.Add(z));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button")!.Click();
        cut.FindAll(".epos-zweispalten-knopf--entfernen")[0].Click();

        Assert.Single(zeilen);
        Assert.Same(a, zeilen[0]);
        Assert.Same(b, entfernt.Single());
    }

    // =================================================================================
    // Abschluss, Tastatur
    // =================================================================================

    [Fact]
    public void OK_und_Abbrechen_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut2, "Abbrechen").Click();
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_mit_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    /// <summary>Das Schliesskreuz im Kopf wirkt wie Esc: schliesst mit <c>false</c>.</summary>
    [Fact]
    public void Kreuz_schliesst_mit_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();
        Assert.False(ergebnis);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>SolarganglinieKiSicht</c>: Die Katalogwahl ist ein WAHLFELD, und
    /// ein Setzen zieht die Beschreibung nach — derselbe Weg wie ein Klick in die
    /// Liste.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_markiert_im_Katalog()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.SOLARGANGLINIE));

        WindowsFormsApplication1.KiFeldzugang projekt =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SOLARGANGLINIE, "projektganglinie");
        Assert.NotNull(projekt);
        Assert.False(projekt.Setzbar);

        WindowsFormsApplication1.KiFeldzugang katalog =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SOLARGANGLINIE, "katalogganglinie");
        Assert.NotNull(katalog);
        Assert.True(katalog.Setzbar);

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(katalog, "Ganglinie Süd");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        katalog.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal("Ganglinie Süd", cut.Instance.Katalogzeile?.Bezeichner);

        WindowsFormsApplication1.KiFeldzugang beschreibung =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.SOLARGANGLINIE, "beschreibung");
        Assert.Equal("Messreihe 2024, Standort Süd", beschreibung.Lesen());
    }

    // =====================================================================
    //  Die Katalogseite: Vergleichen, Schloss, Löschen, Import
    // =====================================================================

    private static IReadOnlyList<Katalogfilterzeile> PFLEGE => new[]
    {
        Zeitreihenproben.Zeile(21, "Ganglinie Nord", beschreibung: "Nord",
                               jahresarbeitMwh: 3.9, spitzeKw: 5.4),
        Zeitreihenproben.Zeile(22, "Ganglinie Süd", beschreibung: "Süd",
                               jahresarbeitMwh: 4.2, spitzeKw: 6.0),
        Zeitreihenproben.Zeile(23, "Auslieferung Ost", geschuetzt: true, beschreibung: "Ost",
                               jahresarbeitMwh: 2.0, spitzeKw: 3.0),
        Zeitreihenproben.Zeile(24, "Messreihe West", beschreibung: "West",
                               jahresarbeitMwh: 1.0, spitzeKw: 2.0)
    };

    /// <summary>Wählt die Katalogzeile <paramref name="index"/> über ihre Wahlspalte.</summary>
    private static void Katalogwahl(IRenderedComponent<SolarganglinieDialog> cut, int index, bool katalogbetrieb = false)
        => cut.FindAll(".epos-raster")[katalogbetrieb ? 0 : 1].QuerySelectorAll("tbody tr")[index]
              .QuerySelector("button")!.Click();

    private static IElement Loeschknopf(IRenderedComponent<SolarganglinieDialog> cut)
        => cut.Find("button.epos-katalog-loeschen");

    [Fact]
    public void Die_Katalogseite_traegt_Kennzahlen_Vergleichen_Schloss_Loeschen_und_Import()
    {
        var schloss = new Schlosspruefung();
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            Loeschen = _ => Task.FromResult(true),
            Schloss = schloss.Weg(),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, false, "", "", ""))
        });

        var koepfe = cut.FindAll(".epos-raster")[1].QuerySelectorAll("thead th").Select(t => t.TextContent).ToList();
        Assert.Contains(koepfe, k => k.Contains("BESCHREIBUNG"));
        Assert.Contains(koepfe, k => k.Contains("JAHRESARBEIT"));
        Assert.Contains(koepfe, k => k.Contains("SPITZE"));

        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains(knoepfe, k => k.StartsWith("Vergleichen"));
        Assert.Contains("Import…", knoepfe);
        Assert.Contains("Ganglinie Löschen", knoepfe);
        Assert.Single(cut.FindAll("button.epos-katalogschloss"));
        Assert.DoesNotContain("Bearbeiten...", knoepfe);
    }

    [Fact]
    public void Vergleichen_stellt_zwei_markierte_Ganglinien_nebeneinander()
    {
        var cut = Aufbauen(wege: Wege(PFLEGE));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[0]
           .Click(new MouseEventArgs { CtrlKey = true });
        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr button")[1]
           .Click(new MouseEventArgs { CtrlKey = true });
        cut.FindAll("button").First(b => b.TextContent.Trim().StartsWith("Vergleichen")).Click();

        var liste = cut.FindComponent<Katalogliste>();
        Assert.True(liste.Instance.VergleichOffen);
        Assert.NotEmpty(liste.Instance.Vergleich);
    }

    [Fact]
    public void Ohne_Delegat_kein_Loeschknopf_und_ohne_Wahl_gesperrt()
    {
        var cut = Aufbauen(wege: Wege(PFLEGE));
        Assert.Empty(cut.FindAll("button.epos-katalog-loeschen"));

        var mit = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            Loeschen = _ => Task.FromResult(true)
        });
        Assert.True(Loeschknopf(mit).HasAttribute("disabled"));
    }

    [Fact]
    public void Loeschen_ist_gesperrt_fuer_Auslieferung_Projektverwendung_und_offenes_Projekt()
    {
        var verwendung = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Ganglinie Süd"] = new[] { "Projekt Alpha" }
        };
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            Verwendung = () => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(verwendung),
            Loeschen = _ => throw new InvalidOperationException("darf nicht loeschen")
        });

        // Im offenen Projekt: "Ganglinie Nord" steht links.
        Katalogwahl(cut, 0);
        Assert.Equal("true", Loeschknopf(cut).GetAttribute("aria-disabled"));
        Assert.Contains("diesem Projekt zugeordnet", Loeschknopf(cut).GetAttribute("title"));
        Loeschknopf(cut).Click();
        Assert.Contains("Ganglinie Nord", cut.Instance.Katalogseite!.Meldung);

        // In einem anderen Projekt: der Name steht im Grund.
        Katalogwahl(cut, 1);
        Assert.Contains("Projekt Alpha", Loeschknopf(cut).GetAttribute("title"));

        // Auslieferungssatz.
        Katalogwahl(cut, 2);
        Assert.Equal("true", Loeschknopf(cut).GetAttribute("aria-disabled"));
        Assert.Equal(Resource.SGAD_MSG_SCHREIBGESCHUETZT, Loeschknopf(cut).GetAttribute("title"));
    }

    [Fact]
    public void Loeschen_einer_freien_Ganglinie_fragt_loescht_und_meldet()
    {
        var liste = new List<Katalogfilterzeile>(PFLEGE);
        var geloescht = new List<string>();
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(liste.ToList()),
            HatProjektzuordnung = _ => Task.FromResult(false),
            Loeschen = n =>
            {
                geloescht.Add(n);
                liste.RemoveAll(z => z.Bezeichner == n);
                return Task.FromResult(true);
            }
        });

        Katalogwahl(cut, 3);                                  // "Messreihe West"
        Assert.Null(Loeschknopf(cut).GetAttribute("aria-disabled"));
        Loeschknopf(cut).Click();
        Assert.True(cut.Instance.Katalogseite!.Loeschfrage);
        Assert.Contains("Messreihe West", cut.Find(".epos-rueckfrage").TextContent);

        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { "Messreihe West" }, geloescht);
        Assert.Equal(3, cut.Instance.Katalog.Count);
        Assert.Null(cut.Instance.Katalogzeile);
        Assert.Contains("Messreihe West", cut.Instance.Katalogseite.Status);
    }

    [Fact]
    public void Eine_Zuordnung_aus_der_Datenbank_haelt_das_Loeschen_auf()
    {
        bool geloescht = false;
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            HatProjektzuordnung = _ => Task.FromResult(true),
            Loeschen = _ => { geloescht = true; return Task.FromResult(true); }
        });

        Katalogwahl(cut, 3);
        Loeschknopf(cut).Click();

        Assert.False(cut.Instance.Katalogseite!.Loeschfrage);
        Assert.Equal(Resource.WBAD_MSG_PROJEKTZUORDNUNG, cut.Instance.Katalogseite.Meldung);
        Assert.False(geloescht);
    }

    [Fact]
    public void Schloss_aufheben_gibt_das_Loeschen_frei()
    {
        IReadOnlyList<Katalogfilterzeile> zeilen = PFLEGE;
        var schloss = new Schlosspruefung(23);
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(zeilen),
            Loeschen = _ => Task.FromResult(true),
            Schloss = schloss.Weg(zeilen: zeilen)
        });

        Katalogwahl(cut, 2);                                  // "Auslieferung Ost"
        Assert.Equal("true", Loeschknopf(cut).GetAttribute("aria-disabled"));

        cut.Find("button.epos-katalogschloss").Click();
        Assert.True(cut.Instance.Katalogseite!.Schlossfrage);
        Schlosspruefung.Ja(cut);

        cut.WaitForAssertion(() => Assert.Null(Loeschknopf(cut).GetAttribute("aria-disabled")),
                             TimeSpan.FromSeconds(10));
        Assert.Single(schloss.Aufrufe);
        Assert.Equal("Schloss von „Auslieferung Ost“ aufgehoben.", cut.Instance.Katalogseite.Status);
    }

    [Fact]
    public void Import_liest_ueber_den_Rueckruf_waehlt_den_neuen_Satz_und_zeigt_das_Protokoll()
    {
        var liste = new List<Katalogfilterzeile>(PFLEGE);
        string? gelesen = null;
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(liste.ToList()),
            DateiWaehlen = _ => Task.FromResult<string?>("D:/Daten/Sued 2025.csv"),
            Einlesen = (pfad, _) =>
            {
                gelesen = pfad;
                liste.Add(Zeitreihenproben.Zeile(30, "Sued 2025", beschreibung: "Leistung [kW]",
                                                 jahresarbeitMwh: 5.0, spitzeKw: 7.0));
                return Task.FromResult(new GanglinienKatalogimport(true, false, "Sued 2025", "",
                    "Format: Trennzeichen ;, Dezimalzeichen , · 35.040 Werte"));
            }
        });

        cut.Find("button.epos-importknopf").Click();
        Assert.True(cut.Instance.Katalogseite!.ImportOffen);
        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei Einlesen...").Click();

        Assert.Equal("D:/Daten/Sued 2025.csv", gelesen);
        Assert.False(cut.Instance.Katalogseite.ImportOffen);
        Assert.Equal("Sued 2025", cut.Instance.Katalogzeile?.Bezeichner);
        Assert.Equal("Leistung [kW]", cut.Find("textarea[readonly]").TextContent);
        Assert.Contains("35.040 Werte", cut.Instance.Katalogseite.Protokoll);
        Assert.Contains("35.040 Werte", cut.Markup);
        Assert.Contains("Sued 2025", cut.Instance.Katalogseite.Status);
    }

    [Fact]
    public void Ein_gescheiterter_Import_bleibt_offen_und_meldet()
    {
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            DateiWaehlen = _ => Task.FromResult<string?>("x.csv"),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, false, "x",
                Resource.SGAD_MSG_VORHANDEN, ""))
        });

        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, "Datei Auswählen...").Click();
        Knopf(cut, "Datei Einlesen...").Click();

        Assert.True(cut.Instance.Katalogseite!.ImportOffen);
        Assert.Equal(Resource.SGAD_MSG_VORHANDEN, cut.Instance.Katalogseite.Importmeldung);
        Assert.Equal(4, cut.Instance.Katalog.Count);
    }

    [Fact]
    public void Ohne_Einleseweg_lehnt_Import_benannt_ab()
    {
        var cut = Aufbauen(wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            ImportAbgelehnt = Resource.SGL_IMP_NICHT_VERFUEGBAR
        });

        IElement knopf = cut.Find("button.epos-importknopf");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(Resource.SGL_IMP_NICHT_VERFUEGBAR, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.False(cut.Instance.Katalogseite!.ImportOffen);
        Assert.Equal(Resource.SGL_IMP_NICHT_VERFUEGBAR, cut.Instance.Katalogseite.Meldung);

        var ohne = Aufbauen(wege: Wege(PFLEGE));
        Assert.Empty(ohne.FindAll("button.epos-importknopf"));
    }

    [Fact]
    public void Esc_schliesst_nicht_solange_der_Import_offen_ist()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b, wege: new GanglinienKatalogwege
        {
            Katalogzeilen = () => Task.FromResult(PFLEGE),
            Einlesen = (_, _) => Task.FromResult(new GanglinienKatalogimport(false, false, "", "", ""))
        });

        cut.Find("button.epos-importknopf").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    // =====================================================================
    //  Katalogbetrieb — der Weg des Administrationsmenüs
    // =====================================================================

    [Fact]
    public void Ohne_Projekt_steht_nur_die_Katalogseite_mit_Beenden()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(katalogbetrieb: true, geschlossen: b => ergebnis = b, wege: Wege(PFLEGE));

        Assert.Empty(cut.FindAll(".epos-zweispalten-knopf--richtung"));
        Assert.Single(cut.FindAll(".epos-raster"));
        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Beenden", knoepfe);
        Assert.DoesNotContain("OK", knoepfe);

        Katalogwahl(cut, 1, katalogbetrieb: true);
        Assert.Equal("Süd", cut.Find("textarea[readonly]").TextContent);

        Knopf(cut, "Beenden").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(ergebnis);
    }

    /// <summary>
    /// Der Menüpunkt „Solarthermie-Ganglinie" öffnet DENSELBEN Dialog ohne Projekt: Der
    /// Parametersatz der Datenseite trifft nur Parameter von <see cref="SolarganglinieDialog"/>,
    /// setzt den Katalogbetrieb und lehnt den Import ohne Dateiwege der Schale benannt ab.
    /// </summary>
    [Fact]
    public void Der_Menuepunkt_oeffnet_den_vereinten_Dialog_ohne_Projekt()
    {
        Menuepunkt punkt = Menuetabelle.Alle.Single(x => x.Name == "MenuItem_SolThermGanglinie");
        Assert.Equal(Seitenschluessel.SolarganglinieAdmin, punkt.Ziel);

        var vorher = Katalogwege.SolarganglinienDatei;
        Katalogwege.SolarganglinienDatei = null;
        try
        {
            IReadOnlyDictionary<string, object> gaben = SolarganglinieKatalogGaben.KatalogGaben();
            var parameter = typeof(SolarganglinieDialog).GetProperties()
                .Where(pi => pi.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.ParameterAttribute), true).Length > 0)
                .Select(pi => pi.Name).ToHashSet();
            Assert.All(gaben.Keys, k => Assert.Contains(k, parameter));
            Assert.Equal(true, gaben["Katalogbetrieb"]);

            var wege = (GanglinienKatalogwege)gaben["Katalogwege"];
            Assert.Null(wege.Einlesen);
            Assert.Equal(Resource.SGL_IMP_NICHT_VERFUEGBAR, wege.ImportAbgelehnt);
        }
        finally
        {
            Katalogwege.SolarganglinienDatei = vorher;
        }
    }
}

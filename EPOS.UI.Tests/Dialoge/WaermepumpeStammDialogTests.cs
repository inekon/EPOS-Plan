using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Datenbank Wärmepumpen (iU9-W7.3). Soll ist die Feldkarte von <c>Form_WP</c>:
/// 28 Zeilen — die Stammliste, neun Felder, zwei Kennlinienbilder in zwei
/// Reiterblättern, der Umschalter Wärme/Kühlung und sechs Knöpfe.
/// </summary>
public class WaermepumpeStammDialogTests : BunitContext
{
    private static readonly byte[] BildCop = { 1, 2, 3 };
    private static readonly byte[] BildLeistung = { 4, 5, 6 };

    private static readonly WaermepumpeStammZeile[] Liste =
    {
        new(1, "WP Alpha", false),
        new(2, "WP Ausliefer", true)
    };

    public WaermepumpeStammDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermepumpeStammDaten Satz(int id) => id switch
    {
        1 => new WaermepumpeStammDaten
        {
            Id = 1, Name = "WP Alpha", Firma = "Alpha", Beschreibung = "Testgerät",
            Typ = "Luft-Wasser", Baujahr = 2023, Aufstellung = "Innenaufstellung",
            Nennleistung = 12, Heizstab = 6, Regelung = "stetig",
            Kuehlleistung = 8.5, Modulkosten = 4000, MaxPtherm = 14, NurLesen = false
        },
        _ => new WaermepumpeStammDaten
        {
            Id = 2, Name = "WP Ausliefer", Firma = "Beta", Typ = "Sole-Wasser",
            Baujahr = 2020, Regelung = "einstufig", Nennleistung = 20, Heizstab = 9,
            NurLesen = true
        }
    };

    private IRenderedComponent<WaermepumpeStammDialog> Aufbauen(
        Func<WaermepumpeStammDaten, bool, KatalogSpeicherErgebnis>? speichern = null,
        Func<string, string?>? gesperrtDurch = null,
        Func<string, bool>? loeschen = null,
        Func<int, bool>? hatKuehlung = null,
        Func<int, IReadOnlyList<KennlinienZeile>>? kennlinien = null,
        Func<int, IReadOnlyList<KennlinienZeile>, bool>? abgleichen = null,
        Func<IReadOnlyList<WaermepumpenKatalogZeile>>? katalog = null,
        Func<IReadOnlyList<WaermepumpeStammZeile>>? liste = null,
        Action<bool>? geschlossen = null)
        => Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Liste, liste ?? (() => Liste))
            .Add(x => x.Satz, Satz)
            .Add(x => x.Bilder, (id, kuehl) => new KennlinienBilder(
                kuehl ? BildLeistung : BildCop, kuehl ? BildCop : BildLeistung))
            .Add(x => x.HatKuehlung, hatKuehlung ?? (id => id == 1))
            .Add(x => x.Speichern, speichern ?? ((d, _) => new KatalogSpeicherErgebnis(true, "Gespeichert", d.Name)))
            .Add(x => x.GesperrtDurch, gesperrtDurch ?? (_ => null))
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Kennlinien, kennlinien ?? (_ => new List<KennlinienZeile>
            {
                new() { Id = 1, Vorlauf = 35, Temperatur = -7, Cop = 2.8, Ptherm = 6.1 }
            }))
            .Add(x => x.KennlinienAbgleichen, abgleichen ?? ((_, _) => true))
            .Add(x => x.Katalog, katalog ?? (() => new[]
            {
                new WaermepumpenKatalogZeile("Beta", "WP Ausliefer", "Split", "Außen",
                                             60, 35, 20, 9, "Sole-Wasser", "einstufig", "Heizen")
            }))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermepumpeStammDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        var gruppe = cut.Find(".epos-gruppenkopf-koerper");
        // Name, Hersteller (Text), Nennleistung, Heizstab (Ganzzahl), Kuehlleistung (Zahl).
        Assert.Equal(5, gruppe.QuerySelectorAll("input").Length);
        Assert.Single(gruppe.QuerySelectorAll("textarea"));
        // Vier Klapplisten: Typ, Leistungsstufen, Aufstellung, Baujahr.
        Assert.Equal(4, gruppe.QuerySelectorAll("select").Length);

        var knopftexte = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("📋  Modul-Katalog...", knopftexte);
        Assert.Contains("Kennliniendaten Ansicht/Bearbeiten...", knopftexte);
        Assert.Contains("Speichern", knopftexte);
        Assert.Contains("Neu", knopftexte);
        Assert.Contains("Löschen", knopftexte);
        Assert.Contains("Beenden", knopftexte);
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Designer()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        foreach (string soll in new[]
                 {
                     "Name", "Hersteller", "Beschreibung", "Wärmepumpentyp", "Leistungsstufen",
                     "Aufstellung", "Baujahr", "Nennleistung", "Heizstab", "Kühlleistung"
                 })
            Assert.Contains(soll, texte);

        Assert.Equal("Verwaltung Daten zu Wärmepumpen und deren Kennlinien",
                     cut.Find(".epos-kontextzeile").TextContent.Trim());
    }

    [Fact]
    public void Die_beiden_Reiterblaetter_heissen_COP_und_Leistung()
    {
        var cut = Aufbauen();
        var reiter = cut.FindAll(".epos-reiter-knopf").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "COP", "Leistung" }, reiter);
    }

    /// <summary>Die Maske ist lokalisiert (21 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Liste, () => Liste)
            .Add(x => x.Satz, Satz)
            .Add(x => x.TitelText, "Heat pump database")
            .Add(x => x.LabelBaujahr, "Year of construction")
            .Add(x => x.BtnNeuText, "New"));

        Assert.Equal("Heat pump database", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Year of construction", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("New", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    // =================================================================================
    // Liste und Auswahl
    // =================================================================================

    [Fact]
    public void Vorbelegt_ist_die_erste_Zeile()
    {
        var cut = Aufbauen();
        Assert.Equal(1, cut.Instance.GewaehlteId);
        Assert.Equal("WP Alpha", cut.Find(".epos-gruppenkopf-koerper input").GetAttribute("value"));
    }

    [Fact]
    public void Ein_Auslieferungssatz_steht_gedimmt_in_der_Liste()
    {
        // Der Vorlaeufer zeichnete ihn GRAU (listBox_WP_DrawItem:187).
        var cut = Aufbauen();
        var zeilen = cut.FindAll(".epos-raster tbody tr");

        Assert.Null(zeilen[0].GetAttribute("aria-disabled"));
        Assert.Equal("true", zeilen[1].GetAttribute("aria-disabled"));
        Assert.Contains("epos-gesperrt", zeilen[1].QuerySelectorAll("td")[1].ClassName);
    }

    [Fact]
    public void Die_Wahl_fuellt_die_Felder_und_die_Bilder()
    {
        var cut = Aufbauen();
        Assert.Contains(Convert.ToBase64String(BildCop), cut.Find(".epos-chartbild").GetAttribute("src"));

        cut.FindAll(".epos-raster tbody tr button")[1].Click();

        Assert.Equal(2, cut.Instance.GewaehlteId);
        Assert.Equal("WP Ausliefer", cut.Find(".epos-gruppenkopf-koerper input").GetAttribute("value"));
    }

    // =================================================================================
    // Waerme / Kuehlung
    // =================================================================================

    [Fact]
    public void Der_Umschalter_erscheint_nur_mit_Kuehl_Kenndaten()
    {
        var cut = Aufbauen();
        Assert.Single(cut.FindAll(".epos-optionsgruppe"));      // WP Alpha hat Kuehlung

        cut.FindAll(".epos-raster tbody tr button")[1].Click(); // WP Ausliefer hat keine
        Assert.Empty(cut.FindAll(".epos-optionsgruppe"));
    }

    [Fact]
    public void Der_Umschalter_zeichnet_die_Bilder_neu()
    {
        var cut = Aufbauen();
        Assert.Contains(Convert.ToBase64String(BildCop), cut.Find(".epos-chartbild").GetAttribute("src"));

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);   // Kühlung
        Assert.Contains(Convert.ToBase64String(BildLeistung), cut.Find(".epos-chartbild").GetAttribute("src"));
    }

    [Fact]
    public void Ein_Zeilenwechsel_faellt_auf_Waerme_zurueck()
    {
        // listBox_WP_SelectedIndexChanged:338 rief radioButton_Waerme.PerformClick().
        var cut = Aufbauen(hatKuehlung: _ => true);

        cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].Change(true);
        cut.FindAll(".epos-raster tbody tr button")[1].Click();

        Assert.True(cut.FindAll(".epos-optionsgruppe input[type=radio]")[0].IsChecked());
    }

    // =================================================================================
    // Speichern, Neu, Loeschen
    // =================================================================================

    [Fact]
    public void Speichern_auf_einem_Auslieferungssatz_wird_abgelehnt()
    {
        bool geschrieben = false;
        var cut = Aufbauen(speichern: (d, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "", d.Name);
        });

        cut.FindAll(".epos-raster tbody tr button")[1].Click();   // WP Ausliefer
        Knopf(cut, "Speichern").Click();

        Assert.False(geschrieben);
        Assert.Contains("schreibgeschützt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Neu_leert_die_Felder_und_legt_beim_Speichern_an()
    {
        bool? alsNeu = null;
        var cut = Aufbauen(speichern: (d, neu) =>
        {
            alsNeu = neu;
            return new KatalogSpeicherErgebnis(true, "Gespeichert", d.Name);
        });

        Knopf(cut, "Neu").Click();
        Assert.Equal(0, cut.Instance.GewaehlteId);
        Assert.Equal("", cut.Find(".epos-gruppenkopf-koerper input").GetAttribute("value"));
        Assert.Empty(cut.FindAll(".epos-optionsgruppe"));        // keine Kuehlung ohne Geraet

        Knopf(cut, "Speichern").Click();
        Assert.True(alsNeu);
    }

    [Fact]
    public void Ein_abgelehntes_Speichern_bleibt_als_Banner_stehen()
    {
        var cut = Aufbauen(speichern: (_, _) =>
            new KatalogSpeicherErgebnis(false, "Speicherung nicht möglich, Fehler aufgetreten!", ""));

        Knopf(cut, "Speichern").Click();
        Assert.Contains("Fehler aufgetreten", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Loeschen_fragt_erst_nach()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return true; });

        Knopf(cut, "Löschen").Click();
        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.False(geloescht);

        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.False(geloescht);
    }

    [Fact]
    public void Eine_Projektzuordnung_sperrt_das_Loeschen_und_nennt_das_Projekt()
    {
        bool geloescht = false;
        var cut = Aufbauen(gesperrtDurch: _ => "Musterprojekt",
                           loeschen: _ => { geloescht = true; return true; });

        Knopf(cut, "Löschen").Click();
        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.False(geloescht);
        Assert.Contains("Musterprojekt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Loeschen_eines_Auslieferungssatzes_wird_abgelehnt()
    {
        bool geloescht = false;
        var cut = Aufbauen(loeschen: _ => { geloescht = true; return true; });

        cut.FindAll(".epos-raster tbody tr button")[1].Click();   // WP Ausliefer
        Knopf(cut, "Löschen").Click();
        cut.Find(".epos-rueckfrage").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.False(geloescht);
        Assert.Contains("schreibgeschützt", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Die beiden Ueberlagerungen
    // =================================================================================

    [Fact]
    public void Kennliniendaten_oeffnen_den_Editor_in_der_Ueberlagerung()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        Knopf(cut, "Kennliniendaten Ansicht/Bearbeiten...").Click();

        Assert.True(cut.Instance.KennlinieneditorOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Der_Editor_schreibt_bei_OK_zurueck_und_zeichnet_neu()
    {
        int geschrieben = 0;
        var cut = Aufbauen(abgleichen: (_, _) => { geschrieben++; return true; });

        Knopf(cut, "Kennliniendaten Ansicht/Bearbeiten...").Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal(1, geschrieben);
        Assert.False(cut.Instance.KennlinieneditorOffen);
    }

    [Fact]
    public void Ein_Abbruch_im_Editor_schreibt_nichts()
    {
        int geschrieben = 0;
        var cut = Aufbauen(abgleichen: (_, _) => { geschrieben++; return true; });

        Knopf(cut, "Kennliniendaten Ansicht/Bearbeiten...").Click();
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "Abbruch").Click();

        Assert.Equal(0, geschrieben);
    }

    [Fact]
    public void Bei_einem_Auslieferungssatz_sagt_der_Editor_warum_und_schreibt_nicht()
    {
        int geschrieben = 0;
        var cut = Aufbauen(abgleichen: (_, _) => { geschrieben++; return true; });

        cut.FindAll(".epos-raster tbody tr button")[1].Click();   // WP Ausliefer
        Knopf(cut, "Kennliniendaten Ansicht/Bearbeiten...").Click();

        Assert.Contains("nur angesehen", cut.FindAll(".epos-warnbanner")[0].TextContent);

        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();
        Assert.Equal(0, geschrieben);
    }

    [Fact]
    public void Der_Modulkatalog_waehlt_die_Zeile()
    {
        var cut = Aufbauen();

        Knopf(cut, "📋  Modul-Katalog...").Click();
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        ueberlagerung.QuerySelector(".epos-raster tbody tr button")!.Click();
        ueberlagerung.QuerySelectorAll("button")
                     .First(b => b.TextContent.Trim() == "✔ Auswahl übernehmen").Click();

        Assert.Equal(2, cut.Instance.GewaehlteId);              // "WP Ausliefer"
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    // =================================================================================
    // Klapplisten mit Bestandswert, Tastatur, Abschluss
    // =================================================================================

    [Fact]
    public void Ein_Bestandswert_ausserhalb_der_festen_Liste_bleibt_stehen()
    {
        // A-16: Der Vorlaeufer hatte frei beschreibbare ComboBoxen; ein select wuerde
        // einen unbekannten Wert still verwerfen.
        var cut = Render<WaermepumpeStammDialog>(p => p
            .Add(x => x.Liste, () => new[] { new WaermepumpeStammZeile(9, "Sonder", false) })
            .Add(x => x.Satz, _ => new WaermepumpeStammDaten
            {
                Id = 9, Name = "Sonder", Typ = "Abwasser-Wasser", Regelung = "stetig"
            }));

        var typen = cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("select")[0]
                       .QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal("Abwasser-Wasser", typen[0]);
        Assert.Equal(5, typen.Count);                            // 4 feste + der Bestandswert
    }

    [Fact]
    public void Die_Baujahrliste_ist_lueckenlos_und_ohne_Dublette()
    {
        // A-15 / Befund W7-O-2: Der Vorlaeufer trug "2024" zweimal und "2022" nie.
        var cut = Aufbauen();
        var jahre = cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("select")[3]
                       .QuerySelectorAll("option").Select(o => o.TextContent).ToList();

        Assert.Equal(new[] { "2025", "2024", "2023", "2022", "2021",
                             "2020", "2019", "2018", "2017", "2016" }, jahre);
    }

    [Fact]
    public void Beenden_und_Esc_melden_das_Ergebnis()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "Beenden").Click();
        Assert.True(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_bei_offener_Ueberlagerung_schliesst_den_Dialog_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "📋  Modul-Katalog...").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Stammdatenblock steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Stammdatenblock_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);

        // Ein Zahlenfeld meldet sich als KURZES Feld, und seine Einheit steht in
        // derselben Feldzeile — im Vorbild 4 px hinter dem Feld, im Befund am
        // rechten Rand des Blocks.
        var kurz = cut.FindAll(".epos-formularraster .epos-feld--kurz");
        Assert.NotEmpty(kurz);
        Assert.Contains(kurz, f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}

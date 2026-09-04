using System.Globalization;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Schritt 0 des Projektassistenten (iU9-W16a.3), Vorbild
/// <c>Views/Wizard/Wizard_Komponenten</c>.
///
/// <para>Soll ist die Feldkarte (24 Zeilen): drei Kopftexte, dreizehn
/// <c>AktionsKarte</c> und die sieben Satzbausteine des unsichtbaren
/// <c>panel_Textvorlagen</c>. Geprüft werden die Kachelzahl und ihre Reihenfolge,
/// die beiden Zustände des Statuspunktes, die zwei Anzeigekacheln ohne
/// Assistentenseite, der Beschreibungssatz und vor allem die <b>wörtliche</b>
/// Rückfrage beim Abwählen einer belegten Komponente.</para>
/// </summary>
public class KomponentenauswahlDialogTests : BunitContext
{
    public KomponentenauswahlDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =====================================================================
    // Die dreizehn Zeilen - Lesereihenfolge des Bestands
    // =====================================================================

    /// <summary>
    /// Die dreizehn Kacheln in der Reihenfolge von <c>KomponentenBestandCtrl</c>:
    /// Bedarf, Strom, Erzeuger, Speicher. Der Aufbau spiegelt einen Projektstand
    /// mit Gebäude, Wärmebedarf, Brauchwasser und Wärmepumpe.
    /// </summary>
    private static List<KomponentenZeile> Zeilen()
    {
        (string Titel, int Seite, int Anzahl, string[] Namen, bool An)[] satz =
        {
            ("Gebäude",                  2, 2, new[] { "Haus A", "Haus B" }, true),
            ("Wärmebedarfsdaten",        3, 1, new[] { "Ganglinie 1" },      true),
            ("Prozesswärme",             4, 0, new string[0],               false),
            ("Brauchwasser",            -1, 1, new[] { "TWW Standard" },     true),
            ("Standard-Stromlastprofil", 5, 0, new string[0],               false),
            ("Stromlastgang",            6, 0, new string[0],               false),
            ("Wärmepumpe",               7, 1, new[] { "WP 12 kW" },         true),
            ("BHKW",                    12, 0, new string[0],               false),
            ("Spitzenkessel",           11, 0, new string[0],               false),
            ("Solarthermie",             8, 0, new string[0],               false),
            ("Photovoltaik",             9, 0, new string[0],               false),
            ("Stromspeicher",           10, 0, new string[0],               false),
            ("Pufferspeicher",          -1, 0, new string[0],               false)
        };

        var liste = new List<KomponentenZeile>();
        for (int k = 0; k < satz.Length; k++)
        {
            liste.Add(new KomponentenZeile
            {
                Kennung = k,
                Titel = satz[k].Titel,
                SeitenIndex = satz[k].Seite,
                Anzahl = satz[k].Anzahl,
                Namen = satz[k].Namen,
                An = satz[k].An
            });
        }
        return liste;
    }

    private IRenderedComponent<KomponentenauswahlDialog> Zeige(
        List<KomponentenZeile>? zeilen = null,
        bool bearbeiten = false,
        Action<int, bool>? geschaltet = null)
    {
        return Render<KomponentenauswahlDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? Zeilen())
            .Add(x => x.BearbeitenModus, bearbeiten)
            .Add(x => x.Geschaltet, geschaltet));
    }

    private static IHtmlCollection<IElement> Kacheln(IRenderedComponent<KomponentenauswahlDialog> cut)
        => cut.Find(".epos-kachelraster").QuerySelectorAll(".epos-kachel");

    private static IElement Kachel(IRenderedComponent<KomponentenauswahlDialog> cut, int i)
        => Kacheln(cut)[i];

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_dreizehn_Kacheln_in_der_Lesereihenfolge()
    {
        var cut = Zeige();

        var kacheln = Kacheln(cut);
        Assert.Equal(13, kacheln.Length);

        Assert.Equal("Gebäude", kacheln[0].QuerySelector(".epos-kachel-titel")!.TextContent);
        Assert.Equal("Brauchwasser", kacheln[3].QuerySelector(".epos-kachel-titel")!.TextContent);
        Assert.Equal("Wärmepumpe", kacheln[6].QuerySelector(".epos-kachel-titel")!.TextContent);
        Assert.Equal("Pufferspeicher", kacheln[12].QuerySelector(".epos-kachel-titel")!.TextContent);
    }

    /// <summary>Die drei Kopftexte — <c>label1</c>, <c>label2</c>, <c>label3</c>.</summary>
    [Fact]
    public void Die_drei_Kopftexte_stehen_woertlich()
    {
        var cut = Zeige();

        // Befund W16-B12: die de-DE-Fassung, nicht die neutrale.
        Assert.Contains("Projekt-Erstellungskonfiguration", cut.Find(".epos-gruppenkopf").TextContent);
        Assert.Contains("Der Projektassistent führt Sie durch alle notwendigen Schritte", cut.Markup);
        Assert.Contains("Wärmeerzeuger bzw. Energieerzeuger Komponenten auswählen:",
                        cut.Find(".epos-untergruppe").TextContent);
    }

    // =====================================================================
    // Der Statuspunkt - zwei Zustaende (Befund W16-B7)
    // =====================================================================

    [Fact]
    public void Der_Statuspunkt_steht_immer_und_ist_gruen_oder_grau()
    {
        var cut = Zeige();

        // Gebaeude ist im Projekt, Prozesswaerme nicht - beide zeigen einen Punkt.
        Assert.False(Kachel(cut, 0).QuerySelector(".epos-kachel-statuspunkt")!
                     .ClassList.Contains("epos-kachel-statuspunkt--aus"));
        Assert.True(Kachel(cut, 2).QuerySelector(".epos-kachel-statuspunkt")!
                    .ClassList.Contains("epos-kachel-statuspunkt--aus"));

        Assert.Equal(13, cut.FindAll(".epos-kachel-statuspunkt").Count);
    }

    /// <summary>
    /// Der Beschreibungssatz — wörtlich <c>KachelZeichnen</c>: „N im Projekt" oder
    /// „nicht im Projekt", bei einer Kachel ohne Seite zusätzlich „ · nur Anzeige".
    /// </summary>
    [Fact]
    public void Der_Bestandssatz_ist_woertlich()
    {
        var cut = Zeige();

        Assert.Equal("2 im Projekt",
                     Kachel(cut, 0).QuerySelector(".epos-kachel-beschreibung")!.TextContent);
        Assert.Equal("nicht im Projekt",
                     Kachel(cut, 2).QuerySelector(".epos-kachel-beschreibung")!.TextContent);
        Assert.Equal("1 im Projekt · nur Anzeige",
                     Kachel(cut, 3).QuerySelector(".epos-kachel-beschreibung")!.TextContent);
        Assert.Equal("nicht im Projekt · nur Anzeige",
                     Kachel(cut, 12).QuerySelector(".epos-kachel-beschreibung")!.TextContent);
    }

    // =====================================================================
    // Brauchwasser und Pufferspeicher sind nur Anzeige
    // =====================================================================

    [Fact]
    public void Die_beiden_Kacheln_ohne_Seite_lassen_sich_nicht_umschalten()
    {
        var zeilen = Zeilen();
        int gerufen = 0;
        var cut = Zeige(zeilen, geschaltet: (_, __) => gerufen++);

        Assert.True(Kachel(cut, 3).HasAttribute("disabled"));    // Brauchwasser
        Assert.True(Kachel(cut, 12).HasAttribute("disabled"));   // Pufferspeicher

        Assert.False(Kachel(cut, 0).HasAttribute("disabled"));
        Assert.False(Kachel(cut, 6).HasAttribute("disabled"));

        // Der Riegel dahinter: auch ein programmierter Klick aendert nichts.
        Kachel(cut, 3).Click();
        Assert.True(zeilen[3].An);
        Assert.Equal(0, gerufen);
    }

    // =====================================================================
    // Umschalten
    // =====================================================================

    /// <summary>
    /// Eine LEERE Komponente einschalten geht ohne Rückfrage — im Neu- wie im
    /// Bearbeiten-Modus. Gemeldet wird der SEITENINDEX, nicht die Kennung.
    /// </summary>
    [Fact]
    public void Eine_leere_Komponente_schaltet_ohne_Rueckfrage()
    {
        var zeilen = Zeilen();
        var geschaltet = new List<(int Seite, bool Aktiv)>();
        var cut = Zeige(zeilen, bearbeiten: true, geschaltet: (s, a) => geschaltet.Add((s, a)));

        Kachel(cut, 9).Click();          // Solarthermie, Anzahl 0

        Assert.True(zeilen[9].An);
        Assert.Equal(new[] { (8, true) }, geschaltet);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    /// <summary>
    /// Im NEU-Modus fragt auch das Abwählen einer belegten Komponente nicht —
    /// wörtlich <c>karte_Geklickt</c> (:201: die Frage steht nur unter
    /// <c>Betriebsart == WIZARD_MODE_BEARBEITEN</c>).
    /// </summary>
    [Fact]
    public void Im_Neu_Modus_fragt_das_Abwaehlen_nicht()
    {
        var zeilen = Zeilen();
        var geschaltet = new List<(int Seite, bool Aktiv)>();
        var cut = Zeige(zeilen, bearbeiten: false, geschaltet: (s, a) => geschaltet.Add((s, a)));

        Kachel(cut, 0).Click();          // Gebaeude, 2 Eintraege

        Assert.False(zeilen[0].An);
        Assert.Equal(new[] { (2, false) }, geschaltet);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    // =====================================================================
    // Die Rueckfrage - woertlich (Wizard_Komponenten :206-207)
    // =====================================================================

    [Fact]
    public void Das_Abwaehlen_einer_belegten_Komponente_fragt_woertlich_nach()
    {
        var zeilen = Zeilen();
        var cut = Zeige(zeilen, bearbeiten: true);

        Kachel(cut, 0).Click();          // Gebaeude, 2 Eintraege

        var frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("„Gebäude“ wird aus dem Projekt genommen.", frage);
        Assert.Contains("Beim Speichern werden 2 Einträge gelöscht:", frage);
        Assert.Contains("Haus A, Haus B", frage);
        Assert.Contains("Wirklich entfernen?", frage);

        // Die Ueberschrift der MessageBox wird die des Bereichs.
        Assert.Contains("Komponente entfernen", cut.Markup);

        // Solange die Frage steht, ist nichts umgeschaltet.
        Assert.True(zeilen[0].An);
    }

    /// <summary>
    /// Vorbelegung „Nein" (<c>MessageBoxDefaultButton.Button2</c>): Der Baustein
    /// hebt „Nein" hervor, Enter ist nicht belegt, der Fokus liegt auf dem Bereich.
    /// </summary>
    [Fact]
    public void Die_Rueckfrage_hebt_Nein_hervor()
    {
        var cut = Zeige(bearbeiten: true);
        Kachel(cut, 0).Click();

        var knoepfe = cut.Find(".epos-rueckfrage .epos-leiste").QuerySelectorAll("button");
        Assert.Equal(2, knoepfe.Length);
        Assert.Equal("Ja", knoepfe[0].TextContent);
        Assert.Equal("Nein", knoepfe[1].TextContent);
        Assert.False(knoepfe[0].ClassList.Contains("epos-knopf--primaer"));
        Assert.True(knoepfe[1].ClassList.Contains("epos-knopf--primaer"));
    }

    [Fact]
    public void Ja_entfernt_die_Komponente_und_schaltet_ihre_Seite_ab()
    {
        var zeilen = Zeilen();
        var geschaltet = new List<(int Seite, bool Aktiv)>();
        var cut = Zeige(zeilen, bearbeiten: true, geschaltet: (s, a) => geschaltet.Add((s, a)));

        Kachel(cut, 0).Click();
        cut.Find(".epos-rueckfrage .epos-leiste").QuerySelectorAll("button")[0].Click();

        Assert.False(zeilen[0].An);
        Assert.Equal(new[] { (2, false) }, geschaltet);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Equal("nicht im Projekt",
                     Kachel(cut, 0).QuerySelector(".epos-kachel-beschreibung")!.TextContent);
    }

    [Fact]
    public void Nein_laesst_alles_stehen()
    {
        var zeilen = Zeilen();
        var geschaltet = new List<(int Seite, bool Aktiv)>();
        var cut = Zeige(zeilen, bearbeiten: true, geschaltet: (s, a) => geschaltet.Add((s, a)));

        Kachel(cut, 0).Click();
        cut.Find(".epos-rueckfrage .epos-leiste").QuerySelectorAll("button")[1].Click();

        Assert.True(zeilen[0].An);
        Assert.Empty(geschaltet);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Equal("2 im Projekt",
                     Kachel(cut, 0).QuerySelector(".epos-kachel-beschreibung")!.TextContent);
    }

    /// <summary>
    /// Wiedereinschalten fragt nicht — die Frage steht nur beim ABwählen
    /// (<c>if (!neu &amp;&amp; …)</c>).
    /// </summary>
    [Fact]
    public void Das_Wiedereinschalten_fragt_nicht()
    {
        var zeilen = Zeilen();
        var cut = Zeige(zeilen, bearbeiten: true);

        Kachel(cut, 0).Click();
        cut.Find(".epos-rueckfrage .epos-leiste").QuerySelectorAll("button")[0].Click();
        Assert.False(zeilen[0].An);

        Kachel(cut, 0).Click();
        Assert.True(zeilen[0].An);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }
}

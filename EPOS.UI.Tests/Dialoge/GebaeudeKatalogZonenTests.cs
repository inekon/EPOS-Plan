using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Zonen eines Katalogsatzes im Editor</b> (Welle ZK-b, Anwenderwunsch 08.10.2026): Der Reiter „Zonen" ist im
/// Katalogmodus (Neu, Bearbeiten) bedienbar wie im Projekt — Liste, Zone als Blatt, Anlegen, Ändern, Entfernen im
/// Arbeitsstand; OK schreibt erst den Satz, dann die Zonen, Abbrechen verwirft; ein ausgelieferter Satz ist weich
/// gesperrt mit dem Grund des Schlosses; „Speichern unter" lässt den neuen Satz die Zonen tragen, wie sie stehen.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class GebaeudeKatalogZonenTests : EposBunitContext
{
    private const string NEUE_ZONE = "+ Neue Zone …";
    private const string SCHLOSS = "Dieser Katalogsatz gehört zur Auslieferung und ist nur lesbar.";

    public GebaeudeKatalogZonenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz, der die Prüfung besteht (wie in <see cref="GebaeudeZonenlisteTests"/>).</summary>
    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5
    };

    private static ZoneDaten Zone(int id, string name, double? flaeche)
        => new()
        {
            Id = id, Bezeichner = name, Nutzflaeche = flaeche,
            Bauteile =
            {
                new BauteilDaten { Id = id * 10 + 1, Bezeichner = "Wand " + name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                   Flaeche = 100, UWert = 0.3, Azimut = 180 },
                new BauteilDaten { Id = id * 10 + 2, Bezeichner = "Dach " + name, Bauteilart = DbWerte.BAUTEILART_DACH,
                                   Flaeche = 50, UWert = 0.2 }
            }
        };

    /// <summary>Zwei gespeicherte Katalogzonen, 90 + 60 = 150 m² — so viel wie das Gebäude.</summary>
    private static ZoneDaten[] ZweiZonen() => new[] { Zone(1, "Erdgeschoss", 90), Zone(2, "Obergeschoss", 60) };

    /// <summary>Die Gegenseite des Editors im Katalog: merkt sich jeden Schreib- und Leseaufruf in seiner Folge.</summary>
    private sealed class Weg
    {
        internal readonly List<string> Folge = new();
        internal readonly List<(GebaeudeKatalogDaten Daten, bool IstNeu, string Name)> Gespeichert = new();
        internal readonly List<ZonenstandDaten> Zonengeschrieben = new();
        internal string Zonenfehler = "";
        internal int Neugelesen;
        internal ZonenNeulesung Neulesung = new(new[] { Zone(7, "Erdgeschoss", 90), Zone(8, "Obergeschoss", 60) },
                                                Array.Empty<ZonenluftstromDaten>());

        internal GebaeudeKatalogErgebnis Speichern(GebaeudeKatalogDaten d, bool istNeu, string name)
        {
            Folge.Add("Satz");
            Gespeichert.Add((d, istNeu, name));
            return new GebaeudeKatalogErgebnis(true, "");
        }

        internal GebaeudeZonenweg Zonenweg(IReadOnlyList<ZoneDaten>? zonen, string? sperre = null) => new()
        {
            Katalog = true,
            Sperre = sperre,
            Zonen = zonen ?? Array.Empty<ZoneDaten>(),
            Speichern = s =>
            {
                Folge.Add("Zonen");
                Zonengeschrieben.Add(new ZonenstandDaten(s.Zonen.Select(z => z.Kopie()).ToList(),
                                                         s.Luftstroeme?.Select(l => l.Kopie()).ToList(), s.SollTagGebaeude));
                return Zonenfehler.Length == 0 ? ZonenSchreibergebnis.Gut : ZonenSchreibergebnis.Fehler(Zonenfehler);
            },
            Neulesen = () => { Neugelesen++; return Neulesung; }
        };
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(
        Weg weg, GebaeudeKatalogModus modus = GebaeudeKatalogModus.Bearbeiten, IReadOnlyList<ZoneDaten>? zonen = null,
        bool gesperrt = false, string? sperre = null, Action<bool>? geschlossen = null)
    {
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
            .Add(x => x.Modus, modus)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, weg.Speichern)
            .Add(x => x.Gesperrt, gesperrt)
            .Add(x => x.SperrGrund, SCHLOSS)
            .Add(x => x.Zonen, weg.Zonenweg(zonen, sperre))
            .Add(x => x.StartReiter, GebaeudeKatalogDialog.REITER_ZONEN)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
        return cut;
    }

    private static IReadOnlyList<IElement> Knoepfe(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").Where(b => b.TextContent.Trim() == text).ToList();

    private static IElement Antwort(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == text);

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("table.epos-zonenliste tbody tr");

    private static void ZonendialogOk(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Single().Click();

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").First(b => b.TextContent.Trim() == "OK").Click();

    private static string[] Namen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Instance.ZonenImArbeitsstand.Select(z => z.Bezeichner).ToArray();

    // =================================================================================
    // Der Reiter im Katalogmodus
    // =================================================================================

    /// <summary>
    /// <b>Der Reiter „Zonen" steht im Katalogmodus mit der Liste der Katalogzonen</b> und ist bedienbar; die
    /// Herleitungszeile nennt, dass der Satz Zonen trägt und die Übernahme ins Projekt sie kopiert; „Gebäude als eine
    /// Zone übernehmen" bleibt dem Projekt (weich gesperrt mit Grund).
    /// </summary>
    [Fact]
    public void Der_Reiter_Zonen_zeigt_die_Katalogzonen_und_nennt_die_Uebernahme_ins_Projekt()
    {
        var cut = Aufbauen(new Weg(), zonen: ZweiZonen());

        Assert.Equal(2, Zeilen(cut).Count);
        Assert.Null(cut.Instance.NeueZoneSperre);
        Assert.Null(cut.Instance.Zonensperre);
        IElement neu = Knoepfe(cut, NEUE_ZONE).Single();
        Assert.Null(neu.GetAttribute("aria-disabled"));
        Assert.Equal("Der Katalogsatz trägt Zonen (2) mit 4 Bauteilen; die Übernahme ins Projekt kopiert sie, und das " +
                     "Gebäude rechnet dort nach dem Zonenmodell.", cut.Instance.Huellwegzeile);
        Assert.StartsWith("„Gebäude als eine Zone übernehmen“ rechnet mit dem Klima eines Projekts", cut.Instance.UebernahmeSperre);
    }

    /// <summary>Im Modus Neu ist der Reiter ebenso bedienbar; ohne Zone nennt die Zeile den Klassenweg und die Übernahme.</summary>
    [Fact]
    public void Im_Modus_Neu_ist_der_Reiter_bedienbar()
    {
        var cut = Aufbauen(new Weg(), GebaeudeKatalogModus.Neu);

        Assert.Null(cut.Instance.NeueZoneSperre);
        Assert.StartsWith("Rechenweg der Hülle: Klassenweg", cut.Instance.Huellwegzeile);
        Assert.Contains("Der Katalogsatz trägt keine Zone", cut.Instance.Huellwegzeile);
    }

    /// <summary><b>Die Zone als Blatt</b>: Öffnen zeigt den Zonendialog im Blatt; der Rückweg verwirft die Eingabe.</summary>
    [Fact]
    public void Die_Zone_steht_als_Blatt_und_der_Rueckweg_verwirft()
    {
        var cut = Aufbauen(new Weg(), zonen: ZweiZonen());

        Knoepfe(cut, "Öffnen…")[0].Click();
        Assert.True(cut.Instance.ZonendialogOffen);
        Assert.NotNull(cut.Find(".epos-blatt .epos-zonendialog"));

        cut.Find(".epos-zonendialog label.epos-feld input").Input("Umbenannt");
        cut.Find(".epos-blatt-zurueck").Click();

        Assert.False(cut.Instance.ZonendialogOffen);
        Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, Namen(cut));
    }

    /// <summary>
    /// <b>Anlegen, Ändern, Entfernen im Arbeitsstand; OK schreibt Satz und Zonen</b> — in dieser Folge, unter dem
    /// Ursprungsnamen; vorher schreibt nichts.
    /// </summary>
    [Fact]
    public void Zone_aendern_entfernen_duplizieren_und_OK_schreibt_erst_den_Satz_dann_die_Zonen()
    {
        var weg = new Weg();
        bool? zu = null;
        var cut = Aufbauen(weg, zonen: ZweiZonen(), geschlossen: b => zu = b);

        // Ändern: die erste Zone umbenennen.
        Knoepfe(cut, "Öffnen…")[0].Click();
        cut.Find(".epos-zonendialog label.epos-feld input").Input("Erdgeschoss neu");
        ZonendialogOk(cut);
        // Entfernen: die zweite, mit Ja.
        Knoepfe(cut, "Zone entfernen")[1].Click();
        Antwort(cut, "Ja").Click();
        // Anlegen: die verbliebene duplizieren (aus einer Zone werden zwei - mit Rückfrage).
        Knoepfe(cut, "Duplizieren")[0].Click();
        Antwort(cut, "Ja").Click();

        Assert.Equal(2, cut.Instance.ZonenImArbeitsstand.Count);
        Assert.Equal("Erdgeschoss neu", cut.Instance.ZonenImArbeitsstand[0].Bezeichner);
        Assert.True(cut.Instance.ZonenImArbeitsstand[1].Id < 0);
        Assert.Empty(weg.Folge);

        Ok(cut);

        Assert.Equal(new[] { "Satz", "Zonen" }, weg.Folge);
        var (_, istNeu, name) = Assert.Single(weg.Gespeichert);
        Assert.False(istNeu);
        Assert.Equal("Haus A", name);
        ZonenstandDaten geschrieben = Assert.Single(weg.Zonengeschrieben);
        Assert.Equal(2, geschrieben.Zonen.Count);
        Assert.Equal("Erdgeschoss neu", geschrieben.Zonen[0].Bezeichner);
        Assert.Equal(1, geschrieben.Zonen[0].Id);
        Assert.True(zu);
    }

    /// <summary><b>Abbrechen verwirft</b>: Eine geänderte Zonenliste schreibt nichts.</summary>
    [Fact]
    public void Abbrechen_verwirft_die_Zonen()
    {
        var weg = new Weg();
        bool? zu = null;
        var cut = Aufbauen(weg, zonen: ZweiZonen(), geschlossen: b => zu = b);

        Knoepfe(cut, "Zone entfernen")[0].Click();
        Antwort(cut, "Ja").Click();
        Assert.Single(cut.Instance.ZonenImArbeitsstand);

        Knoepfe(cut, "Abbrechen")[0].Click();

        Assert.Empty(weg.Folge);
        Assert.False(zu);
    }

    /// <summary>Ohne Änderung an den Zonen schreibt OK nur den Satz.</summary>
    [Fact]
    public void Ohne_Aenderung_an_den_Zonen_schreibt_OK_nur_den_Satz()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, zonen: ZweiZonen());

        Ok(cut);

        Assert.Equal(new[] { "Satz" }, weg.Folge);
    }

    // =================================================================================
    // Das Schloss und die Sperre des Wegs
    // =================================================================================

    /// <summary>
    /// <b>Ein ausgelieferter Satz zeigt seine Zonen nur</b> (Regel B11): „+ Neue Zone …", Duplizieren, Entfernen und
    /// Umordnen sind weich gesperrt und melden den Grund des Schlosses; Öffnen zeigt die Zone, ihr OK nimmt nichts in
    /// den Arbeitsstand; das OK des Editors schreibt nichts.
    /// </summary>
    [Fact]
    public void Ein_gesperrter_Satz_ist_weich_gesperrt_mit_dem_Grund_des_Schlosses()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, zonen: ZweiZonen(), gesperrt: true);

        Assert.Equal(SCHLOSS, cut.Instance.Zonensperre);
        Assert.Equal(SCHLOSS, cut.Instance.NeueZoneSperre);
        IElement neu = Knoepfe(cut, NEUE_ZONE).Single();
        Assert.Equal("true", neu.GetAttribute("aria-disabled"));
        Assert.Equal(SCHLOSS, neu.GetAttribute("title"));
        neu.Click();
        Assert.Equal(SCHLOSS, cut.Instance.Meldung);
        Assert.False(cut.Instance.NachfrageOffen);

        IElement entfernen = Knoepfe(cut, "Zone entfernen")[0];
        Assert.Equal("true", entfernen.GetAttribute("aria-disabled"));
        entfernen.Click();
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));

        Knoepfe(cut, "Öffnen…")[0].Click();
        cut.Find(".epos-zonendialog label.epos-feld input").Input("Umbenannt");
        ZonendialogOk(cut);
        Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, Namen(cut));
        Assert.Equal(SCHLOSS, cut.Instance.Meldung);

        Ok(cut);
        Assert.Empty(weg.Folge);
    }

    /// <summary>Kennt die Datenbank die Katalogzonen nicht, nennt der Weg seinen Grund — die Knöpfe sind weich gesperrt.</summary>
    [Fact]
    public void Ohne_Katalogzonen_der_Datenbank_nennt_der_Reiter_den_Grund()
    {
        const string GRUND = "Diese Datenbank kennt die Zonen eines Katalogsatzes noch nicht (Schemaschritt 204 fehlt).";
        var cut = Aufbauen(new Weg(), sperre: GRUND);

        Assert.Equal(GRUND, cut.Instance.NeueZoneSperre);
        Assert.Contains(cut.FindAll("p.epos-leisezeile"), p => p.TextContent == GRUND);
    }

    // =================================================================================
    // Modus Neu und „Speichern unter"
    // =================================================================================

    /// <summary>
    /// <b>Modus Neu</b>: OK legt den Satz an und schreibt danach seine Zonen; scheitert der Zonenschritt, bleibt der
    /// Dialog offen, und das zweite OK überschreibt den angelegten Satz, statt ihn noch einmal anzulegen.
    /// </summary>
    [Fact]
    public void Im_Modus_Neu_legt_OK_den_Satz_an_und_ein_zweites_OK_ueberschreibt_ihn()
    {
        var weg = new Weg { Zonenfehler = "Zonen nicht geschrieben." };
        bool? zu = null;
        var cut = Aufbauen(weg, GebaeudeKatalogModus.Neu, geschlossen: b => zu = b);

        Knoepfe(cut, NEUE_ZONE)[0].Click();
        Antwort(cut, "Ja").Click();
        ZonendialogOk(cut);
        Assert.Single(cut.Instance.ZonenImArbeitsstand);

        Ok(cut);
        Assert.Equal(new[] { "Satz", "Zonen" }, weg.Folge);
        Assert.True(weg.Gespeichert[0].IstNeu);
        Assert.Contains("Zonen nicht geschrieben.", cut.Instance.Meldung);
        Assert.Null(zu);

        weg.Zonenfehler = "";
        Ok(cut);
        Assert.Equal(new[] { "Satz", "Zonen", "Satz", "Zonen" }, weg.Folge);
        Assert.False(weg.Gespeichert[1].IstNeu);
        Assert.Equal("Haus A", weg.Gespeichert[1].Name);
        Assert.True(zu);
    }

    /// <summary>
    /// <b>„Speichern unter" mit unveränderten Zonen</b>: Der Kern hat sie an den neuen Satz kopiert — der Editor liest
    /// sie dort neu (neue Ids) und schreibt keine Zonen; ohne Rückfrage, die Zeile unter dem Knopf sagt es.
    /// </summary>
    [Fact]
    public void Speichern_unter_liest_unveraenderte_Zonen_am_neuen_Satz_neu()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, zonen: ZweiZonen());

        Assert.Contains(cut.FindAll(".epos-herleitung"),
                        z => z.TextContent == "Der neue Katalogsatz übernimmt die Zonen (2) mit 4 Bauteilen, wie sie hier stehen.");
        Knoepfe(cut, "Speichern unter")[0].Click();

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.True(Assert.Single(weg.Gespeichert).IstNeu);
        Assert.Equal(1, weg.Neugelesen);
        Assert.Empty(weg.Zonengeschrieben);
        Assert.Equal(new[] { 7, 8 }, cut.Instance.ZonenImArbeitsstand.Select(z => z.Id));
    }

    /// <summary>
    /// <b>„Speichern unter" mit geänderten Zonen</b>: Der neue Satz soll sie tragen, wie sie stehen — der Editor schreibt
    /// sie als NEUE Zonen (vorläufige Ids, die alte Zeile als Vorlage) samt aller Luftströme an den neuen Satz.
    /// </summary>
    [Fact]
    public void Speichern_unter_schreibt_geaenderte_Zonen_als_neue_an_den_neuen_Satz()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, zonen: ZweiZonen());

        Knoepfe(cut, "Öffnen…")[1].Click();
        cut.Find(".epos-zonendialog label.epos-feld input").Input("Obergeschoss neu");
        ZonendialogOk(cut);
        Knoepfe(cut, "Speichern unter")[0].Click();

        Assert.Equal(new[] { "Satz", "Zonen" }, weg.Folge);
        Assert.Equal(0, weg.Neugelesen);
        ZonenstandDaten stand = Assert.Single(weg.Zonengeschrieben);
        Assert.All(stand.Zonen, z => Assert.True(z.Id < 0));
        Assert.Equal(new int?[] { 1, 2 }, stand.Zonen.Select(z => z.VorlageId));
        Assert.All(stand.Zonen.SelectMany(z => z.Bauteile), b => Assert.True(b.Id <= 0));
        Assert.Equal("Obergeschoss neu", stand.Zonen[1].Bezeichner);
        Assert.NotNull(stand.Luftstroeme);
    }
}

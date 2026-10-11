using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using EPOS.UI.Tests.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Positionsform an der Wirtschaftlichkeitsseite</b> (Konzept Berichtsvorlagen 4.5, 9.4; Katalog v8): Die Marke
/// der Sensitivität nennt je Stand ihrer Tafel die Positionsform mit Namen, die Marken von Mehrjahrestafel und
/// Zahlungsstrombild die Position des Stands, den die Klappliste von Block 2 zeigt — und folgen ihr. Ohne Positionen
/// der Hülle nennt keine Marke eine Form.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class WirtschaftlichkeitPositionsformTests : VorlagenfeldBunitContext
{
    private const int STAMM = 1030;
    private const int WP = 1031;
    private const int BHKW = 1032;

    private const string SENSITIVITAET = "stand.tabelle.sensitivitaet";
    private const string MEHRJAHRES = "stand.tabelle.mehrjahres";
    private const string ZAHLUNGSSTROM = "stand.bild.zahlungsstrom";

    public WirtschaftlichkeitPositionsformTests()
    {
        Vorlagenfeldhalter.Setzen(new[]
        {
            new Vorlagenfeldanzeige(SENSITIVITAET, "Tabelle", "Tabelle", "je Stand", "Stand", "Sensitivität des Stands",
                                    Positionsrest: "tabelle.sensitivitaet"),
            new Vorlagenfeldanzeige(MEHRJAHRES, "Tabelle", "Tabelle", "je Stand", "Stand", "Mehrjahresübersicht des Stands",
                                    Positionsrest: "tabelle.mehrjahres"),
            new Vorlagenfeldanzeige(ZAHLUNGSSTROM, "Bild", "Bild", "je Stand", "Stand", "Zahlungsstrom des Stands",
                                    Positionsrest: "bild.zahlungsstrom"),
        });
        Ansicht.Setzen(Vorlagenfeldstellung.Marken);
    }

    /// <summary>Eine Gruppe aus Stamm und zwei Varianten, Darstellung ValERI mit Sensitivität und Zahlungsreihen.</summary>
    private static WirtschaftlichkeitStand Stand(bool mitPositionen)
    {
        var positionen = new Dictionary<int, Vorlagenfeldposition>
        {
            [STAMM] = new Vorlagenfeldposition(1, 0),
            [WP] = new Vorlagenfeldposition(2, 1),
            [BHKW] = new Vorlagenfeldposition(3, 2),
        };
        return new WirtschaftlichkeitStand
        {
            Varianten = new[]
            {
                new VarianteZeile { IdProjekt = STAMM, Art = "Stamm", Bezeichner = "(Stammprojekt)", Projektname = "Musterhaus", IstStamm = true },
                new VarianteZeile { IdProjekt = WP, Art = "Variante", Bezeichner = "WP klein", Projektname = "Musterhaus" },
                new VarianteZeile { IdProjekt = BHKW, Art = "Variante", Bezeichner = "BHKW", Projektname = "Musterhaus" },
            },
            GewaehlteVarianten = new[] { STAMM, WP, BHKW },
            Szenarien = new[] { (0, "Erwartet"), (1, "Günstig"), (2, "Ungünstig") },
            HatErgebnisse = true,
            Darstellung = WirtschaftlichkeitStand.DARSTELLUNG_VALERI,
            Vorlagenfeldpositionen = mitPositionen ? positionen : new Dictionary<int, Vorlagenfeldposition>(),
            Ansicht = new ErgebnisAnsicht
            {
                Sensitivitaet = new ErgebnisMatrix
                {
                    Spalten = new[] { "Variante", "Einflussgröße", "bei −Δ [€]", "Basis [€]", "bei +Δ [€]", "Steigung" },
                    Zeilen = new[]
                    {
                        new MatrixZeile { Titel = "WP klein", Zellen = new[] { "Zinssatz ±1 %-Pkt", "14.000", "12.300", "10.700", "—" } },
                        new MatrixZeile { Titel = "BHKW", Zellen = new[] { "Zinssatz ±1 %-Pkt", "-3.000", "-4.100", "-5.200", "—" } },
                    }
                },
                SensitivitaetPositionen = mitPositionen
                    ? new[] { positionen[WP].MitName("WP klein"), positionen[BHKW].MitName("BHKW") }
                    : Array.Empty<Vorlagenfeldposition>(),
                Leitversion = WP,
                Zahlungsstaende = new[] { (STAMM, "Stamm"), (WP, "WP klein"), (BHKW, "BHKW") },
                Zahlungsreihen = new[] { Tafel(STAMM), Tafel(WP), Tafel(BHKW) },
            },
        };
    }

    /// <summary>Eine Jahrestafel samt Zahlungsstrombild des Stands im Erwartungsfall.</summary>
    private static ZahlungsreihenTafel Tafel(int stand) => new ZahlungsreihenTafel
    {
        IdStand = stand,
        Szenario = 0,
        Tafel = new ErgebnisMatrix
        {
            Spalten = new[] { "Jahr", "Energiekosten", "Netto nominal", "Barwert" },
            Zeilen = new[] { new MatrixZeile { Titel = "1", Zellen = new[] { "−500", "−500", "−485" } } }
        },
        Bild = WindowsFormsApplication1.ChartRenderer.ZahlungsstromModell(
            new List<WindowsFormsApplication1.ChartRenderer.Zahlungsstromreihe>
            {
                new WindowsFormsApplication1.ChartRenderer.Zahlungsstromreihe
                {
                    Schluessel = "ENERGIE", Name = "Energiekosten", JeJahr = new[] { 0.0, -500.0 }
                }
            }, Array.Empty<int>(), null),
    };

    private IRenderedComponent<WirtschaftlichkeitSeite> Zeige(WirtschaftlichkeitStand stand)
        => Render<WirtschaftlichkeitSeite>(p => p.Add(x => x.Laden, () => stand));

    private static IReadOnlyList<string> Formen(IRenderedComponent<WirtschaftlichkeitSeite> cut, string schluessel)
        => cut.FindComponents<Vorlagenfeldknopf>().First(k => k.Instance.Vorlagenfeld == schluessel).Instance.Positionsformen;

    [Fact]
    public void Die_Sensitivitaet_nennt_je_Stand_ihrer_Tafel_die_Positionsform_mit_Namen()
    {
        var cut = Zeige(Stand(mitPositionen: true));

        Assert.Equal(new[]
        {
            "stand.2.tabelle.sensitivitaet", "variante.1.tabelle.sensitivitaet",
            "stand.3.tabelle.sensitivitaet", "variante.2.tabelle.sensitivitaet",
        }, Formen(cut, SENSITIVITAET));
        var marke = cut.FindAll("[data-vorlagenfeld='" + SENSITIVITAET + "']").First();
        Assert.Equal(new[] { "WP klein", "BHKW" },
                     marke.QuerySelectorAll(".epos-vorlagenfeld-auf-positionsname").Select(e => e.TextContent.Trim()).ToArray());
    }

    [Fact]
    public void Mehrjahrestafel_und_Zahlungsstrombild_nennen_die_Position_des_gewaehlten_Stands_und_folgen_der_Wahl()
    {
        var cut = Zeige(Stand(mitPositionen: true));

        // Vorgabe: die Leitversion (WP klein, Stand 2, Variante 1).
        Assert.Equal(new[] { "stand.2.tabelle.mehrjahres", "variante.1.tabelle.mehrjahres" }, Formen(cut, MEHRJAHRES));
        Assert.Equal(new[] { "stand.2.bild.zahlungsstrom", "variante.1.bild.zahlungsstrom" }, Formen(cut, ZAHLUNGSSTROM));

        // Der Stamm ist Stand 1 und keine Variante.
        cut.Find(".epos-wirt-zahlungsreihen-wahl select").Change(STAMM.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(new[] { "stand.1.tabelle.mehrjahres" }, Formen(cut, MEHRJAHRES));
        Assert.Equal(new[] { "stand.1.bild.zahlungsstrom" }, Formen(cut, ZAHLUNGSSTROM));

        cut.Find(".epos-wirt-zahlungsreihen-wahl select").Change(BHKW.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(new[] { "stand.3.tabelle.mehrjahres", "variante.2.tabelle.mehrjahres" }, Formen(cut, MEHRJAHRES));
        Assert.Equal(new[] { "stand.3.bild.zahlungsstrom", "variante.2.bild.zahlungsstrom" }, Formen(cut, ZAHLUNGSSTROM));
    }

    [Fact]
    public void Ohne_Positionen_der_Huelle_nennt_keine_Marke_eine_Form()
    {
        var cut = Zeige(Stand(mitPositionen: false));

        Assert.Empty(Formen(cut, SENSITIVITAET));
        Assert.Empty(Formen(cut, MEHRJAHRES));
        Assert.Empty(Formen(cut, ZAHLUNGSSTROM));
        Assert.Empty(cut.FindAll(".epos-vorlagenfeld-auf-position"));
    }
}

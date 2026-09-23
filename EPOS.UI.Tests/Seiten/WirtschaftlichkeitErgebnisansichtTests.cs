using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// ETAPPE E5 Teil b — <b>die Ergebnisansicht der Wirtschaftlichkeitsseite</b> nach dem
/// abgenommenen Mockup (<c>Dialog_Formel_Zahlenprobe.html</c>, Kategorie 8): der
/// Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf (U2), die vier Abschnitte, die
/// Empfehlungskarten je Version (U5), die Bandbreite nebeneinander (U4) mit der
/// Klappliste, die nur die Tafeln darunter steuert, die Sensitivität mit Steigung, der
/// Hinweistext unter der Annahmentafel (U10), die Deklarationen als Klappblock, die
/// Nutzungsdauer- und die Nr.-31-Zeile, „— ‹Grund›" statt einer Null (Q16), der Knopf
/// „Bericht erzeugen" (U44) und die ValERI-Ansicht mit den Blöcken 1, 3, 4, 5 und der
/// benannten Lücke für Block 2 (E8).
///
/// <para><b>Kulturpinnung</b>: Die Beschriftungen kommen aus dem Bündel
/// <see cref="WirtschaftlichkeitSeiteTexte"/> und damit aus <c>MyResource</c>; die
/// Hausvorrichtung <see cref="EposBunitContext"/> pinnt de-DE.</para>
/// </summary>
public class WirtschaftlichkeitErgebnisansichtTests : EposBunitContext
{
    public WirtschaftlichkeitErgebnisansichtTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const int STAMM = 1030;
    private const int WP = 1031;
    private const int BHKW = 1032;

    private const string GRUND = "— keine Amortisation im Betrachtungszeitraum";
    private const string IZF_WARNUNG = "mehrdeutig — die Differenzreihe wechselt 2-mal das Vorzeichen";
    private const string DEKL_RISIKO = "Risikozuschlag nicht angesetzt (6.5 optional) · nicht monetäre Wirkungen: keine benannt";

    // =====================================================================
    //  Probendaten — eine Gruppe mit allem, was die Ansicht zeigt
    // =====================================================================

    private static ErgebnisAnsicht VolleAnsicht(string bandbreiteWp = "12.300", string gliederungWp = "-40.000")
        => new ErgebnisAnsicht
        {
            Kacheln = new[]
            {
                new KachelZeile { Titel = "Kapitalwert ggü. Stamm", Wert = "12.300 €", Quelle = "beste Variante: WP klein" },
                new KachelZeile { Titel = "Annuität", Wert = "827 €/a" },
                new KachelZeile { Titel = "Amortisation", Wert = "6,5 a", Kennzeichen = "nachrichtlich (Anhang C)" },
                new KachelZeile { Titel = "Interner Zinsfuß", Wert = "9,1 %", Kennzeichen = "nachrichtlich (Anhang C)",
                                  Warnung = IZF_WARNUNG }
            },
            Empfehlungen = new[]
            {
                new EmpfehlungKarte { Name = "WP klein", Stufe = EmpfehlungKarte.STUFE_JA,
                                      StufeText = "empfohlen", Differenz = "+12.300 €" },
                new EmpfehlungKarte { Name = "BHKW", Stufe = EmpfehlungKarte.STUFE_NEIN,
                                      StufeText = "nicht empfohlen", Differenz = "−4.100 €" }
            },
            Empfehlungszeile = "Vorschlag zur Entscheidung: Variante „WP klein“ — Kapitalwertdifferenz zu Stamm +12.300 € (Erwartet).",
            Kennzahltafel = new ErgebnisMatrix
            {
                Spalten = new[] { "Kennzahl", "Stamm", "WP klein", "BHKW" },
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "Kapitalwert gegenüber Stamm [€]", Abschnitt = MatrixZeile.ABSCHNITT_KENNZAHL,
                                      Zellen = new[] { "(Referenz)", "12.300", "-4.100" } },
                    new MatrixZeile { Titel = "Amortisation, dynamisch [a]", Abschnitt = MatrixZeile.ABSCHNITT_KENNZAHL,
                                      Kennzeichen = "nachrichtlich (Anhang C)",
                                      Zellen = new[] { "—", "6,5", GRUND } },
                    new MatrixZeile { Titel = "Interner Zinsfuß [%]", Abschnitt = MatrixZeile.ABSCHNITT_KENNZAHL,
                                      Kennzeichen = "nachrichtlich (Anhang C)",
                                      Zellen = new[] { "—", "9,1", "—" },
                                      Zellwarnungen = new[] { "", IZF_WARNUNG, "" } }
                }
            },
            Matrix = new ErgebnisMatrix
            {
                Spalten = new[] { "Kennzahl", "Stamm", "WP klein", "BHKW" },
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "Investition [€]", Zellen = new[] { "0", gliederungWp, "-60.000" } },
                    new MatrixZeile { Titel = "Kapitalwert gegenüber Stamm [€]", Abschnitt = MatrixZeile.ABSCHNITT_KENNZAHL,
                                      Zellen = new[] { "(Referenz)", "12.300", "-4.100" } },
                    new MatrixZeile { Titel = "Nettobarwert über T [€]", Zellen = new[] { "-90.000", "-77.700", "-94.100" } }
                }
            },
            Bandbreite = new ErgebnisMatrix
            {
                Spalten = new[] { "Variante", "Ungünstig", "Erwartet", "Günstig", "Spanne [€]", "Einstufung" },
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "Stamm", Zellen = new[] { "(Referenz)", "(Referenz)", "(Referenz)", "—", "—" } },
                    new MatrixZeile { Titel = "WP klein", Zellen = new[] { "10.100", bandbreiteWp, "14.600", "4.500", "empfohlen" } },
                    new MatrixZeile { Titel = "BHKW", Zellen = new[] { "-7.000", "-4.100", "-1.200", "5.800", "nicht empfohlen" } }
                }
            },
            Bandbreitenfuss = "ΔKW = Kapitalwert der Variante abzüglich Kapitalwert von Stamm (Referenz).",
            Sensitivitaet = new ErgebnisMatrix
            {
                Spalten = new[] { "Variante", "Einflussgröße", "bei −Δ [€]", "Basis [€]", "bei +Δ [€]", "Steigung" },
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "WP klein",
                                      Zellen = new[] { "Zinssatz ±1 %-Pkt", "14.000", "12.300", "10.700", "−1.650,00 €/%-Pkt." } },
                    new MatrixZeile { Titel = "",
                                      Zellen = new[] { "Investition Variante ±10 %", "16.500", "12.300", "8.100", "−420,00 €/%" } }
                }
            },
            Nachweiszeile = "Stamm, WP klein: Nachweis liegt mit der nächsten Rechnung vor",
            Rahmen = new ErgebnisMatrix
            {
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "Maßnahme", Zellen = new[] { "Musterhaus: WP klein, BHKW" } },
                    new MatrixZeile { Titel = "Referenz", Zellen = new[] { "Stamm" } },
                    new MatrixZeile { Titel = "Betrachtungszeitraum", Zellen = new[] { "20 a" } },
                    new MatrixZeile { Titel = "Kalkulationszins", Zellen = new[] { "3,00 %" } }
                }
            }
        };

    private static WirtschaftlichkeitStand Voll() => new WirtschaftlichkeitStand
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = STAMM, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", IstStamm = true },
            new VarianteZeile { IdProjekt = WP, Art = "Variante", Bezeichner = "WP klein", Projektname = "Musterhaus" },
            new VarianteZeile { IdProjekt = BHKW, Art = "Variante", Bezeichner = "BHKW", Projektname = "Musterhaus" }
        },
        GewaehlteVarianten = new[] { STAMM, WP, BHKW },
        Szenarien = new[] { (0, "Erwartet"), (1, "Günstig"), (2, "Ungünstig") },
        SzenarioId = 0,
        Parameterzeile = "Parameter: 20 a, 3,0 %",
        Zeitraumzeile = "Betrachtungszeitraum T = 20 a · Nutzungsdauern 15 bis 25 a",
        Nutzungsdauerhinweise = new[] { "Stamm, WP klein: 1 von 4 Positionen ohne Nutzungsdauer (Planung, 21.888 €)" },
        Szenariohinweis = "Was ein Szenario heute variiert — und was nicht. Ungünstig und Günstig verändern …",
        Deklarationen = new[]
        {
            "Rechnung nominal",
            "Energie- und Stromsteuerentlastungen berücksichtigt · Ertragsteuern nicht berücksichtigt",
            "keine Abschreibungen als Zahlung · Restwert linear — dokumentierte Abweichung von 6.4",
            DEKL_RISIKO
        },
        Annahmen = new ErgebnisMatrix
        {
            Spalten = new[] { "Größe", "Ungünstig", "Erwartet", "Günstig", "Herkunft" },
            Zeilen = new[]
            {
                new MatrixZeile { Titel = "Kalkulationszins", Zellen = new[] { "4,0 %", "3,0 %", "2,0 %", "Vorgabe" } },
                new MatrixZeile { Titel = "Betrachtungszeitraum", Zellen = new[] { "20 a", "20 a", "20 a", "Projektwert — in allen drei Szenarien gleich" } }
            }
        },
        Ansicht = VolleAnsicht(),
        Statuszeile = "Gespeicherte Ergebnisse vom 22.09.2026 20:00."
    };

    private WirtschaftlichkeitStand _stand = Voll();

    private IRenderedComponent<WirtschaftlichkeitSeite> Zeige(
        WirtschaftlichkeitStand? stand = null,
        Action<ComponentParameterCollectionBuilder<WirtschaftlichkeitSeite>>? mehr = null)
    {
        _stand = stand ?? Voll();
        return Render<WirtschaftlichkeitSeite>(p =>
        {
            p.Add(x => x.Laden, () => _stand);
            mehr?.Invoke(p);
        });
    }

    private static string[] Abschnittskoepfe(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToArray();

    private static IElement Abschnitt(IRenderedComponent<WirtschaftlichkeitSeite> cut, int index)
        => cut.FindAll("section.epos-gruppenkopf")[index];

    private static IReadOnlyList<IElement> Umschalter(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-wirt-kopf .epos-wirt-umschalter button");

    private static IReadOnlyList<IElement> Fussknoepfe(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-seite > .epos-leiste button");

    private static IReadOnlyList<string> Zellen(IElement zeile)
        => zeile.QuerySelectorAll("td").Select(e => e.TextContent.Trim()).ToList();

    // =====================================================================
    //  U2 — der Umschalter im Kopf, beide Zustände
    // =====================================================================

    /// <summary>
    /// Der Umschalter steht im Kopf der Seite, vor dem Warnband; Vorgabe „Kennzahlen".
    /// Beide Zustände zeichnen ihren Leib — vier Abschnitte bzw. fünf Blöcke —, der
    /// Wechsel geht an die Hülle (Sitzungswahl), und die Fußleiste bleibt dieselbe.
    /// </summary>
    [Fact]
    public void Der_Umschalter_zeichnet_beide_Zustaende_und_meldet_die_Wahl()
    {
        var gemeldet = new List<int>();
        var cut = Zeige(mehr: p => p.Add(x => x.DarstellungGewaehlt, (int d) => gemeldet.Add(d)));

        IReadOnlyList<IElement> knoepfe = Umschalter(cut);
        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Kennzahlen", knoepfe[0].TextContent.Trim());
        Assert.Equal("ValERI-Bewertung", knoepfe[1].TextContent.Trim());
        Assert.Equal("true", knoepfe[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", knoepfe[1].GetAttribute("aria-pressed"));
        Assert.Equal("Darstellung", cut.Find(".epos-wirt-umschalter").GetAttribute("aria-label"));
        Assert.Equal(WirtschaftlichkeitStand.DARSTELLUNG_KENNZAHLEN, cut.Instance.Darstellung);
        Assert.Equal(4, Abschnittskoepfe(cut).Length);
        int fuss = Fussknoepfe(cut).Count;

        knoepfe[1].Click();

        knoepfe = Umschalter(cut);
        Assert.Equal("false", knoepfe[0].GetAttribute("aria-pressed"));
        Assert.Equal("true", knoepfe[1].GetAttribute("aria-pressed"));
        Assert.Equal(WirtschaftlichkeitStand.DARSTELLUNG_VALERI, cut.Instance.Darstellung);
        Assert.Equal(new[] { WirtschaftlichkeitStand.DARSTELLUNG_VALERI }, gemeldet);
        Assert.Equal(5, Abschnittskoepfe(cut).Length);
        Assert.Equal(fuss, Fussknoepfe(cut).Count);          // derselbe Fuß

        Umschalter(cut)[0].Click();
        Assert.Equal(4, Abschnittskoepfe(cut).Length);
        Assert.Equal(new[] { WirtschaftlichkeitStand.DARSTELLUNG_VALERI,
                             WirtschaftlichkeitStand.DARSTELLUNG_KENNZAHLEN }, gemeldet);

        // Der Umschalter steht vor dem Warnband und vor den Abschnitten.
        Assert.True(cut.Markup.IndexOf("epos-wirt-umschalter", StringComparison.Ordinal) <
                    cut.Markup.IndexOf("epos-gruppenkopf", StringComparison.Ordinal));
    }

    /// <summary>
    /// Die Hülle gibt die Sitzungswahl als Stand herein: Steht dort „ValERI-Bewertung",
    /// geht die Seite so auf. Ein zweiter Klick auf den gewählten Zustand meldet nichts.
    /// </summary>
    [Fact]
    public void Die_Seite_oeffnet_in_der_gemerkten_Darstellung()
    {
        WirtschaftlichkeitStand stand = Voll();
        stand.Darstellung = WirtschaftlichkeitStand.DARSTELLUNG_VALERI;
        int gemeldet = 0;
        var cut = Zeige(stand, p => p.Add(x => x.DarstellungGewaehlt, (int _) => gemeldet++));

        Assert.Equal(WirtschaftlichkeitStand.DARSTELLUNG_VALERI, cut.Instance.Darstellung);
        Assert.Equal("true", Umschalter(cut)[1].GetAttribute("aria-pressed"));

        Umschalter(cut)[1].Click();
        Assert.Equal(0, gemeldet);
    }

    // =====================================================================
    //  U2 — die vier Abschnitte
    // =====================================================================

    /// <summary>Die vier Köpfe der Darstellung „Kennzahlen" in der Reihenfolge des Mockups.</summary>
    [Fact]
    public void Die_Kennzahlen_stehen_in_vier_Abschnitten()
    {
        var cut = Zeige();

        Assert.Equal(new[] { "Lohnt es sich?", "Wie sicher ist das?", "Woraus entsteht die Zahl?",
                             "Was ist angenommen?" },
                     Abschnittskoepfe(cut));

        // Die Kennzahl-Karten stehen im ersten Abschnitt (V‑3: behalten, mit Label).
        IElement lohnt = Abschnitt(cut, 0);
        Assert.Equal(4, lohnt.QuerySelectorAll(".epos-kennzahlkachel").Length);
    }

    // =====================================================================
    //  U5 — Empfehlungskarten
    // =====================================================================

    /// <summary>
    /// Je Version eine Karte mit Name, Kapitalwertdifferenz, leiser Zeile und Stufe; die
    /// Stufe färbt über ihre Klasse (Tokens des Stilblatts). Darunter der Vorschlag und
    /// die Einstufungsregel.
    /// </summary>
    [Fact]
    public void Je_Version_eine_Karte_mit_Stufe_und_Differenz()
    {
        var cut = Zeige();

        IReadOnlyList<IElement> karten = cut.FindAll(".epos-wirt-karte");
        Assert.Equal(2, karten.Count);
        Assert.Contains("epos-wirt-karte--ja", karten[0].ClassList);
        Assert.Contains("epos-wirt-karte--nein", karten[1].ClassList);
        Assert.Equal("WP klein", karten[0].QuerySelector(".epos-wirt-karte-name")!.TextContent);
        Assert.Equal("+12.300 €", karten[0].QuerySelector(".epos-wirt-karte-wert")!.TextContent);
        Assert.Equal("Kapitalwertdifferenz, Szenario Erwartet",
                     karten[0].QuerySelector(".epos-wirt-karte-unter")!.TextContent);
        Assert.Equal("empfohlen", karten[0].QuerySelector(".epos-wirt-stufe--ja")!.TextContent);
        Assert.Equal("nicht empfohlen", karten[1].QuerySelector(".epos-wirt-stufe--nein")!.TextContent);
        Assert.Equal("−4.100 €", karten[1].QuerySelector(".epos-wirt-karte-wert")!.TextContent);

        Assert.StartsWith("Vorschlag zur Entscheidung:",
                          cut.Find(".epos-wirt-vorschlag .epos-herleitung-text").TextContent);
        Assert.Contains(cut.FindAll(".epos-herleitung-text"), e => e.TextContent.StartsWith("Einstufung:"));
    }

    /// <summary>
    /// Q7: Ist eine Variante die Referenz, bekommt auch der Stamm eine Karte — die Seite
    /// zeichnet jede Karte, die die Hülle liefert, auch die des Stammes.
    /// </summary>
    [Fact]
    public void Ist_eine_Variante_die_Referenz_zeigt_auch_der_Stamm_eine_Karte()
    {
        WirtschaftlichkeitStand stand = Voll();
        stand.IdReferenz = WP;
        stand.Ansicht.Empfehlungen = new[]
        {
            new EmpfehlungKarte { Name = "Stamm", Stufe = EmpfehlungKarte.STUFE_NEIN,
                                  StufeText = "nicht empfohlen", Differenz = "−12.300 €" },
            new EmpfehlungKarte { Name = "BHKW", Stufe = EmpfehlungKarte.STUFE_BEDINGT,
                                  StufeText = "bedingt empfohlen", Differenz = "+800 €" }
        };
        var cut = Zeige(stand);

        IReadOnlyList<IElement> karten = cut.FindAll(".epos-wirt-karte");
        Assert.Equal(new[] { "Stamm", "BHKW" },
                     karten.Select(k => k.QuerySelector(".epos-wirt-karte-name")!.TextContent).ToArray());
        Assert.Contains("epos-wirt-karte--bedingt", karten[1].ClassList);
        Assert.Equal("−12.300 €", karten[0].QuerySelector(".epos-wirt-karte-wert")!.TextContent);
    }

    /// <summary>Ohne Karten weder Karten noch Einstufungsregel.</summary>
    [Fact]
    public void Ohne_Karten_bleibt_die_Regel_weg()
    {
        WirtschaftlichkeitStand stand = Voll();
        stand.Ansicht.Empfehlungen = Array.Empty<EmpfehlungKarte>();
        var cut = Zeige(stand);

        Assert.Empty(cut.FindAll(".epos-wirt-karte"));
        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"), e => e.TextContent.StartsWith("Einstufung:"));
    }

    // =====================================================================
    //  V‑A, Q16 — die Kennzahltafel und die Karten mit Label
    // =====================================================================

    /// <summary>
    /// Die Kennzahltafel (Szenario Erwartet): Label „nachrichtlich" an Amortisation und
    /// Zinsfuß, keines an der Kapitalwertdifferenz; „— ‹Grund›" statt einer Null; das
    /// Warnzeichen in der Zinsfußzelle und der Satz dazu unter der Tafel. Die
    /// Zinsfusskarte trägt Label und Warnung ebenso.
    /// </summary>
    [Fact]
    public void Die_Kennzahltafel_traegt_Label_Grund_und_Warnung()
    {
        var cut = Zeige();

        IElement tafel = cut.Find(".epos-wirt-kennzahltafel");
        IReadOnlyList<IElement> zeilen = tafel.QuerySelectorAll("tbody tr").ToList();
        Assert.Equal(3, zeilen.Count);
        Assert.Null(zeilen[0].QuerySelector(".epos-wirt-kennzeichen"));
        Assert.Equal("nachrichtlich (Anhang C)",
                     zeilen[1].QuerySelector(".epos-wirt-kennzeichen")!.TextContent);
        Assert.Equal(GRUND, Zellen(zeilen[1])[2]);
        Assert.DoesNotContain(tafel.QuerySelectorAll("td"), td => td.TextContent.Trim() == "0");

        IElement warnung = zeilen[2].QuerySelector(".epos-wirt-zellwarnung")!;
        Assert.Equal(IZF_WARNUNG, warnung.GetAttribute("title"));
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent == "⚠ WP klein — Interner Zinsfuß [%]: " + IZF_WARNUNG);

        IElement karte = cut.FindAll(".epos-kennzahlkachel")[3];
        Assert.Equal("nachrichtlich (Anhang C)",
                     karte.QuerySelector(".epos-kennzahlkachel-kennzeichen")!.TextContent);
        Assert.Contains(IZF_WARNUNG, karte.QuerySelector(".epos-kennzahlkachel-warnung")!.TextContent);
        Assert.Null(cut.FindAll(".epos-kennzahlkachel")[1].QuerySelector(".epos-kennzahlkachel-kennzeichen"));
    }

    // =====================================================================
    //  U4 — Bandbreite nebeneinander, Klappliste nur für die Tafeln darunter
    // =====================================================================

    /// <summary>
    /// Die Bandbreite steht nebeneinander: je Version Ungünstig · Erwartet · Günstig
    /// gefüllt, dazu Spanne und Einstufung; die erste Zeile ist die Referenz; darunter der
    /// Fußtext.
    /// </summary>
    [Fact]
    public void Die_Bandbreite_steht_nebeneinander_mit_drei_Spalten_und_Spanne()
    {
        var cut = Zeige();

        IElement tafel = cut.Find(".epos-wirt-bandbreite");
        string[] koepfe = tafel.QuerySelectorAll("thead th").Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Variante", "Ungünstig", "Erwartet", "Günstig", "Spanne [€]", "Einstufung" }, koepfe);

        IReadOnlyList<IElement> zeilen = tafel.QuerySelectorAll("tbody tr").ToList();
        Assert.Contains("epos-wirt-referenzzeile", zeilen[0].ClassList);
        IReadOnlyList<string> wp = Zellen(zeilen[1]);
        for (int i = 0; i < 3; i++)
            Assert.False(string.IsNullOrEmpty(wp[i]) || wp[i] == "—", "Spalte " + i + " ist leer.");
        Assert.Equal("4.500", wp[3]);
        Assert.Equal("empfohlen", wp[4]);

        Assert.Contains(cut.FindAll(".epos-herleitung-text"), e => e.TextContent.StartsWith("ΔKW ="));
        Assert.Contains(Abschnitt(cut, 1).QuerySelectorAll(".epos-wirt-bandbreite"), _ => true);
    }

    /// <summary>
    /// <b>Die Klappliste steuert nur die Tafeln darunter</b> (Mockup: „Die Bandbreite
    /// bleibt immer vollständig sichtbar"): Sie steht unter Bandbreite und Sensitivität
    /// und über der Gliederung. Ein Wechsel tauscht die Gliederung; Karten, Kennzahltafel
    /// und Bandbreite darüber bleiben — auch wenn die Hülle sie neu mitliefert.
    /// </summary>
    [Fact]
    public void Die_Klappliste_steuert_nur_die_Tafeln_darunter()
    {
        int gefragt = -1;
        var cut = Zeige(mehr: p => p.Add(x => x.Anzeigen, (int id) =>
        {
            gefragt = id;
            ErgebnisAnsicht neu = VolleAnsicht(bandbreiteWp: "99.999", gliederungWp: "-55.555");
            neu.Kacheln = new[] { new KachelZeile { Titel = "Kapitalwert ggü. Stamm", Wert = "1 €" } };
            neu.Empfehlungen = Array.Empty<EmpfehlungKarte>();
            neu.Szenariozeile = "Szenario: Ungünstig: Vorgaben — i = 4,0 %";
            return neu;
        }));

        // Stellung: Bandbreite und Sensitivität über der Klappliste, die Gliederung darunter.
        var knoten = cut.FindAll(".epos-wirt-bandbreite, .epos-wirt-sensitivitaet, .epos-wirt-szenariozeile, .epos-wirt-gliederung")
                        .ToList();
        int band = knoten.FindIndex(e => e.ClassList.Contains("epos-wirt-bandbreite"));
        int sens = knoten.FindIndex(e => e.ClassList.Contains("epos-wirt-sensitivitaet"));
        int liste = knoten.FindIndex(e => e.ClassList.Contains("epos-wirt-szenariozeile"));
        int gliederung = knoten.FindIndex(e => e.ClassList.Contains("epos-wirt-gliederung"));
        Assert.True(band < liste && sens < liste && liste < gliederung);
        Assert.Equal("Einzelheiten anzeigen für Szenario:",
                     cut.Find(".epos-wirt-szenariozeile .epos-feld-text").TextContent.Trim());

        cut.Find(".epos-wirt-szenariozeile select").Change("2");

        Assert.Equal(2, gefragt);
        // Darunter: die Gliederung und die Szenariozeile des gewählten Szenarios.
        Assert.Equal("-55.555", Zellen(cut.Find(".epos-wirt-gliederung tbody tr"))[1]);
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent == "Szenario: Ungünstig: Vorgaben — i = 4,0 %");
        // Darüber: unverändert.
        Assert.Equal("12.300", Zellen(cut.Find(".epos-wirt-bandbreite").QuerySelectorAll("tbody tr")[1])[1]);
        Assert.Equal("12.300 €", cut.FindAll(".epos-kennzahlkachel-wert")[0].TextContent);
        Assert.Equal(2, cut.FindAll(".epos-wirt-karte").Count);
        Assert.Equal(4, cut.FindAll(".epos-kennzahlkachel").Count);
    }

    /// <summary>
    /// Die Gliederung („Woraus entsteht die Zahl?") trägt die Zeilen der Definition OHNE
    /// die Kennzahlen, die schon in „Lohnt es sich?" stehen — der Nettobarwert als Summe
    /// der Gliederung bleibt.
    /// </summary>
    [Fact]
    public void Die_Gliederung_zeigt_die_Kennzahlen_nicht_zweimal()
    {
        var cut = Zeige();

        string[] titel = Abschnitt(cut, 2).QuerySelectorAll(".epos-wirt-gliederung tbody th")
                                          .Select(e => e.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Investition [€]", "Nettobarwert über T [€]" }, titel);
    }

    // =====================================================================
    //  V‑A — Sensitivität mit Steigung
    // =====================================================================

    /// <summary>Die Sensitivitätstafel steht mit ihrer Steigungsspalte auf der Seite.</summary>
    [Fact]
    public void Die_Sensitivitaet_traegt_die_Steigung()
    {
        var cut = Zeige();

        IElement tafel = Abschnitt(cut, 1).QuerySelector(".epos-wirt-sensitivitaet")!;
        Assert.Equal("Steigung", tafel.QuerySelectorAll("thead th").Last().TextContent.Trim());
        IReadOnlyList<IElement> zeilen = tafel.QuerySelectorAll("tbody tr").ToList();
        Assert.Equal("WP klein", zeilen[0].QuerySelector("th")!.TextContent.Trim());
        Assert.Equal("−1.650,00 €/%-Pkt.", Zellen(zeilen[0])[4]);
        Assert.Equal("Zinssatz ±1 %-Pkt", Zellen(zeilen[0])[0]);
        Assert.Contains("epos-wirt-textzelle", zeilen[0].QuerySelectorAll("td")[0].ClassList);
    }

    // =====================================================================
    //  U10, V‑A, U39, Nr. 31 — „Was ist angenommen?"
    // =====================================================================

    /// <summary>
    /// U10: Der Hinweistext steht UNMITTELBAR unter der Annahmentafel, beide im
    /// Abschnitt „Was ist angenommen?".
    /// </summary>
    [Fact]
    public void Der_Hinweistext_steht_unter_der_Annahmentafel()
    {
        var cut = Zeige();

        IElement annahmen = Abschnitt(cut, 3);
        IElement tafel = annahmen.QuerySelector(".epos-wirt-annahmen")!;
        Assert.Equal(new[] { "Größe", "Ungünstig", "Erwartet", "Günstig", "Herkunft" },
                     tafel.QuerySelectorAll("thead th").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal("Annahmen und ihre Herkunft", annahmen.QuerySelector(".epos-untergruppe")!.TextContent);

        IElement huelle = tafel.Closest(".epos-raster-huelle")!;
        IElement? danach = huelle.NextElementSibling;
        Assert.NotNull(danach);
        Assert.Contains("epos-wirt-szenariohinweis", danach!.ClassList);
        Assert.StartsWith("Was ein Szenario heute variiert", danach.TextContent.Trim());
    }

    /// <summary>
    /// Die Deklarationen stehen im Klappblock „Bewertung nach DIN EN 17463" — auch ohne
    /// Schreibweg: zugeklappt nicht sichtbar, aufgeklappt in fester Reihenfolge, die
    /// Risikozeile ohne gepflegten Text mit „keine benannt" (Q5). Ohne Schreibweg kein
    /// Freitextfeld.
    /// </summary>
    [Fact]
    public void Die_Deklarationen_stehen_im_Klappblock()
    {
        var cut = Zeige();

        IElement knopf = Abschnitt(cut, 3).QuerySelector("button.epos-modulparameter-knopf")!;
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".epos-wirt-deklarationen"));

        knopf.Click();

        string[] zeilen = cut.FindAll(".epos-wirt-deklarationen .epos-herleitung-text")
                             .Select(e => e.TextContent).ToArray();
        Assert.Equal(4, zeilen.Length);
        Assert.Equal("Rechnung nominal", zeilen[0]);
        Assert.Equal(DEKL_RISIKO, zeilen[3]);
        Assert.Empty(cut.FindAll("textarea"));
    }

    /// <summary>
    /// U39 und Nr. 31: Zeitraumzeile, die Hinweiszeile „k von n Positionen ohne
    /// Nutzungsdauer" und die Zeile „Nachweis liegt mit der nächsten Rechnung vor" stehen
    /// im Abschnitt „Was ist angenommen?", nach dem Parameternachweis.
    /// </summary>
    [Fact]
    public void Nutzungsdauer_und_Nachweis_stehen_unter_den_Annahmen()
    {
        var cut = Zeige();

        IElement annahmen = Abschnitt(cut, 3);
        string[] texte = annahmen.QuerySelectorAll(".epos-herleitung-text").Select(e => e.TextContent).ToArray();
        int parameter = Array.FindIndex(texte, t => t.StartsWith("Parameter:"));
        int zeitraum = Array.FindIndex(texte, t => t.StartsWith("Betrachtungszeitraum T = 20 a"));
        int kvonn = Array.FindIndex(texte, t => t.Contains("1 von 4 Positionen ohne Nutzungsdauer"));
        int nachweis = Array.FindIndex(texte, t => t.EndsWith("Nachweis liegt mit der nächsten Rechnung vor"));
        Assert.True(parameter >= 0 && parameter < zeitraum && zeitraum < kvonn && kvonn < nachweis,
                    string.Join(" | ", texte));
        Assert.NotNull(annahmen.QuerySelector(".epos-wirt-pruefauftrag"));
        Assert.Equal("Stamm, WP klein: Nachweis liegt mit der nächsten Rechnung vor",
                     annahmen.QuerySelector(".epos-wirt-nachweis .epos-herleitung-text")!.TextContent);
    }

    // =====================================================================
    //  U44 — „Bericht erzeugen"
    // =====================================================================

    /// <summary>
    /// Der Knopf „Bericht erzeugen" steht im Fuß des Bewertungsblocks — nicht in der
    /// Fußleiste und nicht als Primärknopf (die Seite trägt genau einen). Er fragt wie
    /// die Berichtsseite, ruft dann den Berichtsweg mit den gewählten Versionen ohne Stamm
    /// und bietet danach das Öffnen an.
    /// </summary>
    [Fact]
    public async Task Der_Knopf_Bericht_erzeugen_ruft_den_Berichtsweg()
    {
        IReadOnlyList<int>? varianten = null;
        string? geoeffnet = null;
        var cut = Zeige(mehr: p => p
            .Add(x => x.BerichtErzeugen, (IReadOnlyList<int> v, Action<Laufschritt> _) =>
            {
                varianten = v;
                return Task.FromResult(new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = "Bericht erstellt: C:\\Berichte\\Musterhaus.docx",
                    Frage = "Bericht jetzt öffnen?",
                    Datei = "C:\\Berichte\\Musterhaus.docx"
                });
            })
            .Add(x => x.DateiOeffnen, (string pfad) => { geoeffnet = pfad; return Task.CompletedTask; }));

        IElement knopf = Abschnitt(cut, 3).QuerySelector(".epos-wirt-abschnitt-fuss button")!;
        Assert.Equal("Bericht erzeugen", knopf.TextContent.Trim());
        Assert.DoesNotContain("epos-knopf--primaer", knopf.ClassList);
        Assert.DoesNotContain(Fussknoepfe(cut), k => k.TextContent.Trim() == "Bericht erzeugen");

        knopf.Click();

        // Die Rückfrage vor dem Lauf — derselbe Wortlaut wie auf der Berichtsseite.
        Assert.Contains("3 Projekt(e)", cut.Find(".epos-rueckfrage-text").TextContent);
        Assert.Null(varianten);
        await cut.InvokeAsync(() => cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click());

        Assert.Equal(new[] { WP, BHKW }, varianten);
        Assert.Equal("Bericht erstellt: C:\\Berichte\\Musterhaus.docx", cut.Instance.Status);
        Assert.False(cut.Instance.Beschaeftigt);

        // Danach „öffnen?" — Ja öffnet die Datei über die Naht.
        Assert.Equal("Bericht jetzt öffnen?", cut.Find(".epos-rueckfrage-text").TextContent);
        await cut.InvokeAsync(() => cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click());
        Assert.Equal("C:\\Berichte\\Musterhaus.docx", geoeffnet);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    /// <summary>Nein auf die erste Frage startet nichts; ohne Delegat gibt es den Knopf nicht.</summary>
    [Fact]
    public void Ohne_Zusage_und_ohne_Delegat_kein_Bericht()
    {
        int laeufe = 0;
        var cut = Zeige(mehr: p => p.Add(x => x.BerichtErzeugen,
            (IReadOnlyList<int> _, Action<Laufschritt> _) => { laeufe++; return Task.FromResult(new LaufErgebnis()); }));

        cut.Find(".epos-wirt-berichtknopf").Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[1].Click();       // Nein
        Assert.Equal(0, laeufe);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));

        var ohne = Zeige();
        Assert.Empty(ohne.FindAll(".epos-wirt-berichtknopf"));
    }

    // =====================================================================
    //  V‑1 — die ValERI-Ansicht
    // =====================================================================

    /// <summary>
    /// Die ValERI-Ansicht zeichnet die Blöcke 1, 3, 4 und 5 mit ihren Inhalten, den
    /// Normbezug als Untertitel im Balken, und an der Stelle von Block 2 (E8) genau EINE
    /// benannte Hinweiszeile statt eines leeren Rahmens. Die vier Abschnitte der
    /// Kennzahlen-Darstellung stehen dann nicht da.
    /// </summary>
    [Fact]
    public void Die_ValERI_Ansicht_traegt_die_Bloecke_und_die_Hinweiszeile_fuer_Block_2()
    {
        var cut = Zeige(mehr: p => p.Add(x => x.BerichtErzeugen,
            (IReadOnlyList<int> _, Action<Laufschritt> _) => Task.FromResult(new LaufErgebnis())));
        Umschalter(cut)[1].Click();

        Assert.Equal(new[] { "1 · Gegenstand und Rahmen", "2 · Zahlungsreihen", "3 · Kennzahlen",
                             "4 · Unsicherheit", "5 · Deklarationen" },
                     Abschnittskoepfe(cut));
        Assert.Equal(new[] { "DIN EN 17463 · 6.1 · 7.3", "DIN EN 17463 · 6.1 bis 6.4",
                             "DIN EN 17463 · 7 · Anhang C", "DIN EN 17463 · 7.3 · 8.1.3",
                             "DIN EN 17463 · 7 · 9 · Anhang E" },
                     cut.FindAll(".epos-gruppenkopf-summe").Select(e => e.TextContent.Trim()).ToArray());
        Assert.DoesNotContain("Lohnt es sich?", Abschnittskoepfe(cut));

        // Block 1: Rahmen aus dem Parametersatz, Rechnung nominal.
        IElement b1 = Abschnitt(cut, 0);
        Assert.Equal(new[] { "Maßnahme", "Referenz", "Betrachtungszeitraum", "Kalkulationszins" },
                     b1.QuerySelectorAll(".epos-wirt-rahmen th").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Contains(b1.QuerySelectorAll(".epos-herleitung-text"), e => e.TextContent == "Rechnung nominal");

        // Block 2: GENAU eine Zeile, keine Tafel.
        IElement b2 = Abschnitt(cut, 1);
        Assert.Single(b2.QuerySelectorAll(".epos-herleitung-text"));
        Assert.Empty(b2.QuerySelectorAll("table"));
        Assert.StartsWith("Die Zahlungsreihen je Jahr", b2.QuerySelector(".epos-wirt-block-luecke")!.TextContent.Trim());

        // Block 3: die Kennzahltafel mit Label und die Einordnung.
        IElement b3 = Abschnitt(cut, 2);
        Assert.NotNull(b3.QuerySelector(".epos-wirt-kennzahltafel .epos-wirt-kennzeichen"));
        Assert.Contains(b3.QuerySelectorAll(".epos-herleitung-text"),
                        e => e.TextContent.StartsWith("Maß der Vorteilhaftigkeit ist allein der Kapitalwert"));

        // Block 4: Bandbreite, Vorschlag, Hinweistext, Sensitivität — und seit E8a (E6‑Q1)
        // die Stellen von Spannenbild und Verlauf (ohne Datenseite mit Platzhalter).
        IElement b4 = Abschnitt(cut, 3);
        Assert.NotNull(b4.QuerySelector(".epos-wirt-bandbreite"));
        Assert.NotNull(b4.QuerySelector(".epos-wirt-sensitivitaet"));
        Assert.NotNull(b4.QuerySelector(".epos-wirt-szenariohinweis"));
        Assert.NotNull(b4.QuerySelector(".epos-wirt-spanne-teil"));
        Assert.NotNull(b4.QuerySelector(".epos-wirt-verlauf-teil .epos-chartbild-platzhalter"));

        // Block 5: Deklarationen, Nr. 31 und der Berichtsknopf in seinem Fuß.
        IElement b5 = Abschnitt(cut, 4);
        string[] dekl = b5.QuerySelectorAll(".epos-wirt-deklarationen .epos-herleitung-text")
                          .Select(e => e.TextContent).ToArray();
        Assert.Equal(4, dekl.Length);
        Assert.Equal(DEKL_RISIKO, dekl[3]);
        Assert.NotNull(b5.QuerySelector(".epos-wirt-nachweis"));
        Assert.NotNull(b5.QuerySelector(".epos-wirt-abschnitt-fuss .epos-wirt-berichtknopf"));

        // Die Szenario-Klappliste gehört zur Kennzahlen-Darstellung.
        Assert.Empty(cut.FindAll(".epos-wirt-szenariozeile"));
    }

    /// <summary>
    /// Block 5 nennt die nicht monetären Wirkungen, wenn ein Text gepflegt ist — mit der
    /// Zeilenform des Berichts.
    /// </summary>
    [Fact]
    public void Block_5_nennt_die_gepflegten_nicht_monetaeren_Wirkungen()
    {
        WirtschaftlichkeitStand stand = Voll();
        stand.NichtMonetaer = "Versorgungssicherheit";
        stand.Darstellung = WirtschaftlichkeitStand.DARSTELLUNG_VALERI;
        var cut = Zeige(stand);

        Assert.Contains(Abschnitt(cut, 4).QuerySelectorAll(".epos-herleitung-text"),
                        e => e.TextContent == "Nicht monetäre Wirkungen: Versorgungssicherheit");
    }

    // =====================================================================
    //  Ohne Gaben
    // =====================================================================

    /// <summary>
    /// Die Seite zeichnet auch OHNE Gaben — kein Delegat, jede Liste leer —, in beiden
    /// Darstellungen; ohne Delegat schaltet der Umschalter trotzdem.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_zeichnen_beide_Darstellungen()
    {
        var cut = Render<WirtschaftlichkeitSeite>();

        Assert.Equal(4, Abschnittskoepfe(cut).Length);
        Assert.Empty(cut.FindAll(".epos-wirt-karte"));
        Assert.Empty(cut.FindAll(".epos-wirt-bandbreite"));
        Assert.Empty(cut.FindAll(".epos-wirt-berichtknopf"));

        Umschalter(cut)[1].Click();

        Assert.Equal(5, Abschnittskoepfe(cut).Length);
        Assert.Single(Abschnitt(cut, 1).QuerySelectorAll(".epos-herleitung-text"));
    }
}

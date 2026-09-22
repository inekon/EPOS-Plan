using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>KONZEPT § 2.9 und § 2.15</b> — die wählbare Referenz der Vergleichsgruppe und die
/// Vergleichssicht der Ergebnisansicht auf der Seite „Wirtschaftlichkeit".
///
/// <para>Geprüft wird die Bedienung: die Optionsgruppe „Alle Varianten gegen die
/// Referenz | Zwei Stände", die zwei Klapplisten (ohne den jeweils anderen Stand), die
/// Sperre in Sicht 1 und bei nur einem Stand, der Tauschknopf, die Erklärzeile und die
/// Referenzwahl in der Vergleichsgruppen-Liste samt der Regel, dass die Referenz nicht
/// abwählbar ist.</para>
///
/// <para><b>Ohne Rückruf kein Bedienelement</b> — das ist die Hausregel dieser Seite
/// (wie beim Bewertungsblock) und zugleich der Nachweis, dass der Bestand unverändert
/// bleibt, solange eine Hülle die neuen Gaben nicht setzt.</para>
/// </summary>
public class WirtschaftlichkeitSichtTests : EposBunitContext
{
    public WirtschaftlichkeitSichtTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const int STAMM = 1030;
    private const int VAR_A = 1031;
    private const int VAR_B = 1032;

    private static WirtschaftlichkeitStand Stand(int sicht = 0, int a = 0, int b = 0,
                                                 int referenz = 0, bool paarMoeglich = true)
        => new WirtschaftlichkeitStand
        {
            Varianten = new[]
            {
                new VarianteZeile { IdProjekt = STAMM, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                    Projektname = "Musterhaus", IstStamm = true },
                new VarianteZeile { IdProjekt = VAR_A, Art = "Variante", Bezeichner = "WP klein",
                                    Projektname = "Musterhaus" },
                new VarianteZeile { IdProjekt = VAR_B, Art = "Variante", Bezeichner = "BHKW",
                                    Projektname = "Musterhaus" }
            },
            GewaehlteVarianten = paarMoeglich ? new[] { STAMM, VAR_A, VAR_B } : new[] { STAMM },
            Staende = new[] { (STAMM, "Stamm"), (VAR_A, "WP klein"), (VAR_B, "BHKW") },
            Szenarien = new[] { (0, "Erwartet") },
            IdReferenz = referenz,
            Sicht = sicht,
            SichtA = a,
            SichtB = b,
            PaarMoeglich = paarMoeglich,
            Referenzzeile = sicht == 1
                ? "Referenz dieser Sicht: WP klein · Referenz der Gruppe: Stamm · Differenz = B − A"
                : "Referenz: Stamm · Differenz = Variante − Referenz",
            Ansicht = new ErgebnisAnsicht()
        };

    private IRenderedComponent<WirtschaftlichkeitSeite> Zeige(
        WirtschaftlichkeitStand stand,
        Action<Bunit.ComponentParameterCollectionBuilder<WirtschaftlichkeitSeite>>? mehr = null)
        => Render<WirtschaftlichkeitSeite>(p =>
        {
            p.Add(x => x.Laden, () => stand);
            mehr?.Invoke(p);
        });

    /// <summary>Die Optionsgruppe der Sichtwahl (das zweite fieldset gibt es nur mit Rückruf).</summary>
    private static IReadOnlyList<IElement> Sichtoptionen(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-optionsgruppe[role='radiogroup'] input[type=radio]");

    // =====================================================================
    // Die Optionsgruppe
    // =====================================================================

    /// <summary>Ohne Rückruf gibt es keine Sichtwahl — der Bestand bleibt, wie er ist.</summary>
    [Fact]
    public void Ohne_Rueckruf_keine_Sichtwahl()
    {
        var cut = Zeige(Stand());

        Assert.Empty(Sichtoptionen(cut));
    }

    /// <summary>
    /// Mit Rückruf stehen zwei Optionen da, und Sicht 1 ist gewählt — die Vorgabe.
    /// </summary>
    [Fact]
    public void Die_Optionsgruppe_traegt_beide_Sichten()
    {
        var cut = Zeige(Stand(), p => p.Add(x => x.SichtGewaehlt, _ => null));

        IReadOnlyList<IElement> optionen = Sichtoptionen(cut);
        Assert.Equal(2, optionen.Count);
        Assert.True(optionen[0].HasAttribute("checked"));
        Assert.False(optionen[1].HasAttribute("checked"));
    }

    /// <summary>In Sicht 1 sind beide Klapplisten und der Tauschknopf gesperrt.</summary>
    [Fact]
    public void In_Sicht_1_sind_die_Listen_gesperrt()
    {
        var cut = Zeige(Stand(), p =>
        {
            p.Add(x => x.SichtGewaehlt, _ => null);
            p.Add(x => x.PaarGewaehlt, (_, _) => null);
            p.Add(x => x.PaarTauschen, () => null);
        });

        foreach (IElement liste in Listen(cut)) Assert.True(liste.HasAttribute("disabled"));
        Assert.True(Tauschknopf(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// In Sicht 2 sind sie offen, und <b>die Liste A führt B nicht, die Liste B nicht
    /// A</b> — so ist A ≠ B ohne Meldung gesichert.
    /// </summary>
    [Fact]
    public void In_Sicht_2_fuehrt_keine_Liste_den_Stand_der_anderen()
    {
        var cut = Zeige(Stand(1, VAR_A, VAR_B), p =>
        {
            p.Add(x => x.SichtGewaehlt, _ => null);
            p.Add(x => x.PaarGewaehlt, (_, _) => null);
            p.Add(x => x.PaarTauschen, () => null);
        });

        IReadOnlyList<IElement> listen = Listen(cut);
        Assert.False(listen[0].HasAttribute("disabled"));
        Assert.False(listen[1].HasAttribute("disabled"));

        Assert.DoesNotContain(Werte(listen[0]), v => v == VAR_B.ToString());
        Assert.Contains(Werte(listen[0]), v => v == VAR_A.ToString());
        Assert.DoesNotContain(Werte(listen[1]), v => v == VAR_A.ToString());
        Assert.Contains(Werte(listen[1]), v => v == VAR_B.ToString());
    }

    /// <summary>
    /// RANDFALL: Eine Gruppe mit nur dem Stamm sperrt Sicht 2 — weich, mit dem Grund am
    /// Bedienelement; der Versuch meldet ihn in der Statuszeile.
    /// </summary>
    [Fact]
    public void Eine_Gruppe_mit_einem_Stand_sperrt_Sicht_2()
    {
        var cut = Zeige(Stand(paarMoeglich: false), p => p.Add(x => x.SichtGewaehlt, _ => null));

        IElement paar = Sichtoptionen(cut)[1];
        Assert.Equal("true", paar.GetAttribute("aria-disabled"));
        Assert.False(string.IsNullOrEmpty(paar.GetAttribute("title")));

        paar.Change("1");
        Assert.Contains("zwei", cut.Instance.Status, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Der Tauschknopf (VG‑Q7) meldet sich — und nur in Sicht 2.</summary>
    [Fact]
    public void Der_Tauschknopf_meldet_sich()
    {
        int getauscht = 0;
        var cut = Zeige(Stand(1, VAR_A, VAR_B), p =>
        {
            p.Add(x => x.SichtGewaehlt, _ => null);
            p.Add(x => x.PaarGewaehlt, (_, _) => null);
            p.Add(x => x.PaarTauschen, () => { getauscht++; return null; });
        });

        Tauschknopf(cut).Click();
        Assert.Equal(1, getauscht);
    }

    /// <summary>Die Sichtwahl meldet die neue Sicht.</summary>
    [Fact]
    public void Die_Sichtwahl_meldet_die_neue_Sicht()
    {
        int gemeldet = -1;
        var cut = Zeige(Stand(), p => p.Add(x => x.SichtGewaehlt, s => { gemeldet = s; return null; }));

        Sichtoptionen(cut)[1].Change("1");
        Assert.Equal(1, gemeldet);
    }

    /// <summary>Die Erklärzeile steht unter der Optionsgruppe und nennt die Referenz.</summary>
    [Fact]
    public void Die_Erklaerzeile_nennt_die_Referenz()
    {
        var cut = Zeige(Stand(1, VAR_A, VAR_B), p => p.Add(x => x.SichtGewaehlt, _ => null));

        Assert.Contains("Referenz dieser Sicht",
                        cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Referenz der Gruppe", cut.Markup, StringComparison.Ordinal);
    }

    // =====================================================================
    // Die Referenzwahl (§ 2.9)
    // =====================================================================

    /// <summary>Ohne Rückruf gibt es die Referenzspalte nicht.</summary>
    [Fact]
    public void Ohne_Rueckruf_keine_Referenzspalte()
    {
        var cut = Zeige(Stand());

        Assert.Empty(cut.FindAll(".epos-optionsgruppe--einzeln"));
    }

    /// <summary>
    /// Mit Rückruf trägt jede Zeile ihr Optionsfeld, und die GEWÄHLTE Referenz ist
    /// markiert — ohne Wahl der Stamm.
    /// </summary>
    [Fact]
    public void Die_Referenzspalte_markiert_die_gewaehlte_Referenz()
    {
        var cut = Zeige(Stand(referenz: VAR_A), p => p.Add(x => x.ReferenzGewaehlt, _ => null));

        IReadOnlyList<IElement> felder = cut.FindAll(".epos-optionsgruppe--einzeln input[type=radio]");
        Assert.Equal(3, felder.Count);
        Assert.Equal(VAR_A.ToString(),
                     felder.First(f => f.HasAttribute("checked")).GetAttribute("value"));
    }

    /// <summary>Die Referenzwahl meldet die neue Referenz.</summary>
    [Fact]
    public void Die_Referenzwahl_meldet_die_neue_Referenz()
    {
        int gemeldet = 0;
        var cut = Zeige(Stand(), p => p.Add(x => x.ReferenzGewaehlt, id => { gemeldet = id; return null; }));

        cut.FindAll(".epos-optionsgruppe--einzeln input[type=radio]")
           .First(f => f.GetAttribute("value") == VAR_B.ToString()).Change(VAR_B.ToString());

        Assert.Equal(VAR_B, gemeldet);
    }

    /// <summary>
    /// Die REFERENZ ist nicht abwählbar — bis zur wählbaren Referenz war das der Stamm,
    /// jetzt ist es die gewählte Variante, und der Stamm wird abwählbar.
    /// </summary>
    [Fact]
    public void Die_gewaehlte_Referenz_ist_nicht_abwaehlbar()
    {
        var cut = Zeige(Stand(referenz: VAR_A), p => p.Add(x => x.ReferenzGewaehlt, _ => null));

        IReadOnlyList<IElement> haken = cut.FindAll(".epos-raster input[type=checkbox]");
        Assert.Equal(3, haken.Count);
        Assert.False(haken[0].HasAttribute("disabled"));   // Stamm
        Assert.True(haken[1].HasAttribute("disabled"));    // Referenz
        Assert.False(haken[2].HasAttribute("disabled"));
    }

    // =====================================================================
    // Hilfsmittel
    // =====================================================================

    private static IReadOnlyList<IElement> Listen(IRenderedComponent<WirtschaftlichkeitSeite> cut)
    {
        // ETAPPE E5 Teil b: Die Szenariowahl steht seither im Abschnitt „Wie sicher ist
        // das?" UNTER der Vergleichssicht. A und B sind die Klapplisten DER Zeile, die
        // die Optionsgruppe der Sicht trägt — nicht mehr die letzten zwei der Seite.
        IElement zeile = cut.FindAll(".epos-seite-zeile")
                            .First(z => z.QuerySelector(".epos-optionsgruppe") is not null);
        var l = new List<IElement>();
        foreach (IElement e in zeile.QuerySelectorAll("select")) l.Add(e);
        Assert.Equal(2, l.Count);
        return l;
    }

    private static IElement Tauschknopf(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-seite-zeile button").Last(b => b.TextContent.Contains('⇄'));

    private static List<string> Werte(IElement liste)
    {
        var l = new List<string>();
        foreach (IElement o in liste.QuerySelectorAll("option")) l.Add(o.GetAttribute("value") ?? "");
        return l;
    }
}

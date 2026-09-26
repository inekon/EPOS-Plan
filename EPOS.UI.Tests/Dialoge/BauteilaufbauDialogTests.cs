using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Verwaltung „Bauteilaufbauten" (Gebäudesimulation G3, Welle C): Gerüst der Verwaltungen,
/// Stammblatt mit Kenndaten, dem Schichtenraster samt Summenfuß (aus der Hülle — die Oberfläche
/// rechnet nicht) und der Herkunft; der Arbeitsstand wird erst mit „Speichern" bzw. dem OK von
/// „Neu…" als EIN Aggregat geschrieben, die Prüfregeln laufen genau dort.
/// </summary>
public class BauteilaufbauDialogTests : EposBunitContext
{
    public BauteilaufbauDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly IReadOnlyList<(string Wert, string Text)> ARTEN = new[]
    {
        ("", "für jede Bauteilart"), ("AUSSENWAND", "Außenwand"), ("DACH", "Dach"), ("BODENPLATTE", "Bodenplatte")
    };

    private static readonly IReadOnlyList<BaustoffWahl> STOFFE = new[]
    {
        new BaustoffWahl(1, "Kalkzementputz · λ 1", 1.0, 1800, 1000),
        new BaustoffWahl(7, "Mineralwolle · λ 0,035", 0.035, 30, 1030)
    };

    /// <summary>Der Summenfuß, den die „Hülle" liefert — fest, damit sichtbar wird, dass die Oberfläche ihn nur zeigt.</summary>
    private static AufbauKennwerteDaten Fuss(BauteilaufbauDaten d) => new()
    {
        NeigungGrad = 90, RSi_M2KW = 0.13, RSe_M2KW = 0.04,
        RJeSchicht_M2KW = d.Schichten.Select((s, i) => (double?)(0.1 * (i + 1))).ToList(),
        R_M2KW = 4.0, U_WM2K = 0.25, Kapazitaet_KJM2K = 320, KapazitaetWirksam_KJM2K = 60,
        Bezugsperiode_D = 7, R1Rel = 1.0, C1Rel = 0.9
    };

    private sealed class Katalog
    {
        internal readonly List<BauteilaufbauDaten> Saetze = new()
        {
            new BauteilaufbauDaten
            {
                Id = 3, Bezeichner = "Außenwand Bestand", Bauteilart = "AUSSENWAND", Herkunft = "Vorgabe", Auslieferung = true,
                Schichten = { new BauteilschichtDaten { IdBaustoff = 1, DickeMm = 15, Lambda = 1.0, Rho = 1800, Cp = 1000 } }
            },
            new BauteilaufbauDaten
            {
                Id = 4, Bezeichner = "Außenwand gedämmt", Bauteilart = "AUSSENWAND", Herkunft = "Manuell",
                Schichten =
                {
                    new BauteilschichtDaten { IdBaustoff = 1, DickeMm = 15, Lambda = 1.0, Rho = 1800, Cp = 1000 },
                    new BauteilschichtDaten { DickeMm = 175, Lambda = 0.99, Rho = 1800, Cp = 1000 },
                    new BauteilschichtDaten { IdBaustoff = 7, DickeMm = 140, Lambda = 0.035, Rho = 30, Cp = 1030 }
                }
            }
        };

        internal readonly List<BauteilaufbauDaten> Gespeichert = new();
        internal readonly List<BauteilaufbauDaten> Geprueft = new();
        internal readonly List<BauteilaufbauDaten> Gerechnet = new();
        internal string Pruefbefund = "";
        internal bool? Geschlossen;
        private int _naechste = 100;

        internal IReadOnlyList<Katalogfilterzeile> Zeilen()
            => Saetze.Select(s => new Katalogfilterzeile(s.Id, s.Bezeichner)
                    {
                        Geschuetzt = s.Auslieferung,
                        Schluessel = s.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }
                    .MitText(Katalogfilterprofil.SpBezeichner, s.Bezeichner)
                    .MitText(Katalogfilterprofil.SpBauteilart, ARTEN.First(a => a.Wert == s.Bauteilart).Text)
                    .MitZahl(Katalogfilterprofil.SpUWert, 0.25, 3)
                    .MitZahl(Katalogfilterprofil.SpSchichten, s.Schichten.Count, 0))
                .ToList();

        internal BauteilaufbauSpeicherErgebnis Speichern(BauteilaufbauDaten d)
        {
            BauteilaufbauDaten kopie = d.Kopie();
            Gespeichert.Add(kopie);
            if (kopie.Id <= 0) { kopie.Id = ++_naechste; Saetze.Add(kopie); }
            else Saetze[Saetze.FindIndex(s => s.Id == kopie.Id)] = kopie;
            return new BauteilaufbauSpeicherErgebnis(true, "", kopie.Id);
        }
    }

    private IRenderedComponent<BauteilaufbauDialog> Aufbauen(Katalog? k = null)
    {
        Katalog kat = k ?? new Katalog();
        return Render<BauteilaufbauDialog>(b => b
            .Add(x => x.Katalogzeilen, kat.Zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBauteilaufbau(
                s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, id => kat.Saetze.FirstOrDefault(s => s.Id == id)?.Kopie())
            .Add(x => x.Baustoffe, () => STOFFE)
            .Add(x => x.Bauteilarten, ARTEN)
            .Add(x => x.Kennwerte, d => { kat.Gerechnet.Add(d.Kopie()); return Fuss(d); })
            .Add(x => x.Pruefen, d => { kat.Geprueft.Add(d.Kopie()); return kat.Pruefbefund; })
            .Add(x => x.Speichern, kat.Speichern)
            .Add(x => x.Loeschen, id => { kat.Saetze.RemoveAll(s => s.Id == id); return new BauteilaufbauSpeicherErgebnis(true, "", id); })
            .Add(x => x.Duplizieren, (id, name) =>
            {
                BauteilaufbauDaten neu = kat.Saetze.First(s => s.Id == id).Kopie();
                neu.Id = 0;
                neu.Bezeichner = name;
                neu.Auslieferung = false;
                return kat.Speichern(neu);
            })
            .Add(x => x.Geschlossen, e => kat.Geschlossen = e));
    }

    private static IElement Fussleiste(IRenderedComponent<BauteilaufbauDialog> cut)
        => cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();

    private static IElement Knopf(IRenderedComponent<BauteilaufbauDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Stammblatt(IRenderedComponent<BauteilaufbauDialog> cut) => cut.Find(".epos-stammblatt");

    private static IElement Feld(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  .QuerySelector("input, textarea, select")!;

    private static IReadOnlyList<IElement> Schichtzeilen(IElement bereich)
        => bereich.QuerySelectorAll(".epos-bauteilschicht-kopf").ToList();

    /// <summary>Die vier Zahlenfelder der Schicht <paramref name="index"/> (Dicke, λ, ρ, c_p).</summary>
    private static IReadOnlyList<IElement> Zahlen(IElement bereich, int index)
        => bereich.QuerySelectorAll(".epos-zeilenraster input[inputmode='decimal']").Skip(4 * index).Take(4).ToList();

    // =================================================================================
    // Gerüst, Feldbestand, ohne Gaben
    // =================================================================================

    [Fact]
    public void Das_Geruest_steht_wie_bei_den_Verwaltungen()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);

        Assert.Equal("Bauteilaufbauten", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-dialog-zu"));
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Equal(new[] { "Kenndaten", "Schichten", "Herkunft" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu…", "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        string kopf = cut.Find(".epos-katalogliste thead").TextContent;
        foreach (string spalte in new[] { "Name", "Bauteilart", "U-Wert", "R-Wert", "Kapazität", "Schichten", "Herkunft" })
            Assert.Contains(spalte, kopf);
    }

    [Fact]
    public void Kenndaten_und_Schichtenraster_tragen_ihre_Felder()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);                                   // Außenwand gedämmt, drei Schichten

        IElement blatt = Stammblatt(cut);
        Assert.Equal(new[] { "Name", "Bauteilart", "Beschreibung", "Quelle" },
                     blatt.QuerySelectorAll(".epos-stammblattgruppe")[0].QuerySelectorAll(".epos-feld-text")
                          .Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(new[] { "für jede Bauteilart", "Außenwand", "Dach", "Bodenplatte" },
                     Feld(blatt, "Bauteilart").QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray());

        Assert.Equal(new[] { "Dicke [mm]", "λ [W/(mK)]", "ρ [kg/m³]", "c_p [J/(kgK)]" },
                     blatt.QuerySelectorAll(".epos-zr-kopfzelle").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(3, Schichtzeilen(blatt).Count);
        Assert.Equal(new[] { "1", "2", "3" },
                     blatt.QuerySelectorAll(".epos-bauteilschicht-nr").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal("175", Zahlen(blatt, 1)[0].GetAttribute("value"));
        Assert.Equal(3, blatt.QuerySelectorAll(".epos-bauteilschicht-fuss .epos-schalter").Length);
        Assert.Equal(9, blatt.QuerySelectorAll(".epos-bauteilschicht-fuss .epos-zr-aktionen button").Length);
        Assert.Contains("+ Neue Schicht …", blatt.QuerySelector(".epos-zr-neuzeile")!.TextContent);
    }

    [Fact]
    public void Der_Summenfuss_kommt_aus_der_Huelle()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        IElement blatt = Stammblatt(cut);
        string summen = string.Join(" | ", blatt.QuerySelectorAll(".epos-zr-summenzelle").Select(e => e.TextContent.Trim()));
        Assert.Contains("R = Σ d/λ = 4,000 m²K/W", summen);
        Assert.Contains("U = 0,250 W/(m²K)", summen);
        Assert.Contains("C = Σ ρ·c_p·d = 320,0 kJ/(m²K)", summen);
        Assert.Contains("T_BT = 7 d", summen);
        Assert.Contains("R_si = 0,13 und R_se = 0,04", blatt.QuerySelector(".epos-herleitung, .epos-herleitungszeile")?.TextContent
                                                        ?? blatt.TextContent);
        Assert.Equal(new[] { "R = 0,100 m²K/W", "R = 0,200 m²K/W", "R = 0,300 m²K/W" },
                     blatt.QuerySelectorAll(".epos-bauteilschicht-r").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal("0,250 W/(m²K)", cut.FindAll(".epos-stammblatt-kennzahl dd").First().TextContent);
        Assert.Equal(3, k.Gerechnet.Last().Schichten.Count);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<BauteilaufbauDialog>();

        Assert.Equal("Bauteilaufbauten", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("noch keinen Bauteilaufbau", cut.Find(".epos-bauteilaufbau-leer").TextContent);
        Assert.Equal(new[] { "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }

    // =================================================================================
    // Das Schichtenraster
    // =================================================================================

    [Fact]
    public void Eine_neue_Schicht_haengt_aussen_an_und_der_Fuss_kommt_neu()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);
        int vorher = k.Gerechnet.Count;

        Knopf(cut, "+ Neue Schicht …").Click();

        Assert.Equal(4, cut.Instance.Arbeitsstand.Schichten.Count);
        Assert.Null(cut.Instance.Arbeitsstand.Schichten[3].DickeMm);
        Assert.True(cut.Instance.Geaendert);
        Assert.Equal(4, Schichtzeilen(Stammblatt(cut)).Count);
        Assert.True(k.Gerechnet.Count > vorher);
        Assert.Equal(4, k.Gerechnet.Last().Schichten.Count);
        Assert.Empty(k.Gespeichert);                                 // der Knopf speichert nicht, er übernimmt
    }

    [Fact]
    public void Pfeile_verschieben_und_das_Kreuz_entfernt_eine_Schicht()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);
        IElement fuss = Stammblatt(cut).QuerySelectorAll(".epos-bauteilschicht-fuss")[1];

        fuss.QuerySelectorAll("button")[1].Click();                  // Schicht 2 nach außen
        Assert.Equal(new double?[] { 15, 140, 175 }, cut.Instance.Arbeitsstand.Schichten.Select(s => s.DickeMm).ToArray());

        Stammblatt(cut).QuerySelectorAll(".epos-bauteilschicht-fuss")[2].QuerySelectorAll("button")[0].Click();   // zurück
        Assert.Equal(new double?[] { 15, 175, 140 }, cut.Instance.Arbeitsstand.Schichten.Select(s => s.DickeMm).ToArray());

        Stammblatt(cut).QuerySelectorAll(".epos-bauteilschicht-fuss")[0].QuerySelectorAll("button")[2].Click();   // entfernen
        Assert.Equal(new double?[] { 175, 140 }, cut.Instance.Arbeitsstand.Schichten.Select(s => s.DickeMm).ToArray());

        // Am Rand ist der Pfeil gesperrt.
        IElement erste = Stammblatt(cut).QuerySelectorAll(".epos-bauteilschicht-fuss")[0];
        Assert.True(erste.QuerySelectorAll("button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Wahl_des_Baustoffs_uebernimmt_die_Stoffwerte_als_Kopie()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);
        IElement feld = Schichtzeilen(Stammblatt(cut))[1].QuerySelector("input[role='combobox']")!;

        feld.Click();
        cut.FindAll("li[role='option']").First(li => li.TextContent.StartsWith("Mineralwolle", StringComparison.Ordinal)).Click();

        BauteilschichtDaten s = cut.Instance.Arbeitsstand.Schichten[1];
        Assert.Equal(7, s.IdBaustoff);
        Assert.Equal(0.035, s.Lambda);
        Assert.Equal(30, s.Rho);
        Assert.Equal(1030, s.Cp);
        Assert.Equal(175, s.DickeMm);                                // die Dicke bleibt

        // Danach überschreibbar.
        Zahlen(Stammblatt(cut), 1)[1].Input("0,04");
        Assert.Equal(0.04, cut.Instance.Arbeitsstand.Schichten[1].Lambda);
        Assert.Equal(7, cut.Instance.Arbeitsstand.Schichten[1].IdBaustoff);
    }

    [Fact]
    public void Die_Bauteilart_speichert_den_Persistenzwert()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);

        Feld(Stammblatt(cut), "Bauteilart").Change("2");            // Dach

        Assert.Equal("DACH", cut.Instance.Arbeitsstand.Arbeit.Bauteilart);
        Assert.True(cut.Instance.Geaendert);
    }

    // =================================================================================
    // Der eine Schreibweg
    // =================================================================================

    [Fact]
    public void Speichern_prueft_einmal_und_schreibt_Kopf_und_Schichten_als_ein_Aggregat()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        Zahlen(Stammblatt(cut), 2)[0].Input("160");
        Knopf(cut, "+ Neue Schicht …").Click();
        Zahlen(Stammblatt(cut), 3)[0].Input("10");
        Zahlen(Stammblatt(cut), 3)[1].Input("0,7");
        Zahlen(Stammblatt(cut), 3)[2].Input("1400");
        Zahlen(Stammblatt(cut), 3)[3].Input("1000");

        Knopf(cut, "Speichern").Click();

        Assert.Single(k.Geprueft);
        BauteilaufbauDaten a = Assert.Single(k.Gespeichert);
        Assert.Equal(4, a.Id);
        Assert.Equal(new double?[] { 15, 175, 160, 10 }, a.Schichten.Select(s => s.DickeMm).ToArray());
        Assert.False(cut.Instance.Geaendert);
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
    }

    [Fact]
    public void Eine_verletzte_Regel_meldet_und_schreibt_nicht()
    {
        var k = new Katalog { Pruefbefund = "Schicht 2: Die Wärmeleitfähigkeit λ fehlt." };
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        Zahlen(Stammblatt(cut), 1)[1].Input("");
        Knopf(cut, "Speichern").Click();

        Assert.Equal(k.Pruefbefund, cut.Instance.Meldung);
        Assert.Contains(k.Pruefbefund, cut.Find(".epos-warnbanner").TextContent);
        Assert.Empty(k.Gespeichert);
        Assert.True(cut.Instance.Geaendert);

        // Der Grund steht auch ROT neben dem Knopf; ein gelungenes Speichern nimmt die
        // Färbung zurück.
        IElement status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Equal(k.Pruefbefund, status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);

        k.Pruefbefund = "";
        Knopf(cut, "Speichern").Click();
        status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.StartsWith("Gespeichert um", status.TextContent);
        Assert.DoesNotContain("epos-status--fehler", status.ClassName);
    }

    [Fact]
    public void Ein_Auslieferungssatz_steht_als_Text_samt_Summenfuss()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);                                       // die erste Zeile ist geschützt

        Assert.True(cut.Instance.Auslieferung);
        IElement blatt = Stammblatt(cut);
        Assert.Empty(blatt.QuerySelectorAll(".epos-stammblatt-inhalt input"));
        Assert.Empty(blatt.QuerySelectorAll(".epos-zeilenraster"));
        Assert.Contains("Schicht 1 – Kalkzementputz", blatt.TextContent);
        Assert.Contains("U = 0,250 W/(m²K)", blatt.QuerySelector(".epos-bauteilschicht-summen")!.TextContent);

        Knopf(cut, "Speichern").Click();
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_SPEICHERN_GESPERRT, cut.Instance.Meldung);
        Assert.Empty(k.Gespeichert);
    }

    [Fact]
    public void Neu_legt_Kopf_und_Schichten_erst_mit_OK_an()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);

        Knopf(cut, "Neu…").Click();
        IElement neu = cut.Find(".epos-bauteilaufbau-neu");
        Feld(neu, "Name").Input("Innenwand leicht");
        Feld(neu, "Bauteilart").Change("0");
        neu.QuerySelectorAll("button").First(b => b.TextContent.Trim() == "+ Neue Schicht …").Click();
        neu = cut.Find(".epos-bauteilaufbau-neu");
        Zahlen(neu, 0)[0].Input("100");
        Zahlen(neu, 0)[1].Input("0,5");
        Zahlen(neu, 0)[2].Input("1000");
        Zahlen(neu, 0)[3].Input("1000");
        Assert.Empty(k.Gespeichert);

        neu.QuerySelectorAll(".epos-leiste button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.NeuOffen);
        BauteilaufbauDaten a = Assert.Single(k.Gespeichert);
        Assert.Equal(0, k.Geprueft.Single().Id);
        Assert.Equal("Innenwand leicht", a.Bezeichner);
        Assert.Equal("", a.Bauteilart);
        Assert.Equal(100, a.Schichten.Single().DickeMm);
        Assert.Equal(a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), cut.Instance.Gewaehlt);
        Assert.Equal("„Innenwand leicht“ angelegt.", cut.Instance.Status);
    }

    [Fact]
    public void Neu_mit_verletzter_Regel_bleibt_offen()
    {
        var k = new Katalog { Pruefbefund = "Der Bauteilaufbau braucht mindestens eine Schicht." };
        var cut = Aufbauen(k);

        Knopf(cut, "Neu…").Click();
        Feld(cut.Find(".epos-bauteilaufbau-neu"), "Name").Input("Leer");
        cut.Find(".epos-bauteilaufbau-neu").QuerySelectorAll(".epos-leiste button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(cut.Instance.NeuOffen);
        Assert.Equal(k.Pruefbefund, cut.Instance.NeuMeldung);
        Assert.Empty(k.Gespeichert);
    }

    [Fact]
    public void Beenden_schliesst_ohne_Aenderung_und_Esc_wirkt_gleich()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(k.Geschlossen);
    }
}

/// <summary>
/// Der Arbeitsstand eines Aufbaus ohne Oberfläche — Anlegen, Verschieben, Entfernen, die Wahl
/// des Baustoffs und die Zählung „geändert".
/// </summary>
public class BauteilaufbauArbeitsstandTests
{
    private static BauteilaufbauDaten Aufbau() => new()
    {
        Id = 4, Bezeichner = "Wand",
        Schichten =
        {
            new BauteilschichtDaten { DickeMm = 15, Lambda = 1.0 },
            new BauteilschichtDaten { DickeMm = 175, Lambda = 0.99 }
        }
    };

    [Fact]
    public void Der_Arbeitsstand_laesst_das_Hereingereichte_unangetastet()
    {
        BauteilaufbauDaten original = Aufbau();
        var stand = new BauteilaufbauArbeitsstand(original);

        stand.SchichtAnlegen();
        stand.Arbeit.Schichten[0].DickeMm = 20;
        stand.Arbeit.Bezeichner = "Neu";

        Assert.Equal(2, original.Schichten.Count);
        Assert.Equal(15, original.Schichten[0].DickeMm);
        Assert.Equal("Wand", original.Bezeichner);
        Assert.Equal(3, stand.GeaenderteFelder);                     // Name, Schicht 1, eine Schicht mehr
    }

    [Fact]
    public void Verschieben_Entfernen_und_Zuruecksetzen()
    {
        var stand = new BauteilaufbauArbeitsstand(Aufbau());
        int fassung = stand.Fassung;

        Assert.False(stand.Verschieben(0, -1));
        Assert.True(stand.Verschieben(0, +1));
        Assert.Equal(new double?[] { 175, 15 }, stand.Schichten.Select(s => s.DickeMm).ToArray());
        Assert.Equal(2, stand.GeaenderteFelder);
        Assert.True(stand.Fassung > fassung);

        Assert.True(stand.Entfernen(1));
        Assert.False(stand.Entfernen(5));
        Assert.Single(stand.Schichten);

        stand.Zuruecksetzen();
        Assert.False(stand.Geaendert);
        Assert.Equal(2, stand.Schichten.Count);
    }

    [Fact]
    public void Die_Stoffwahl_kopiert_die_Werte_und_freie_Eingabe_behaelt_sie()
    {
        var stand = new BauteilaufbauArbeitsstand(Aufbau());
        var stoffe = new[] { new BaustoffWahl(7, "Mineralwolle", 0.035, 30, 1030) };

        stand.StoffWaehlen(1, 7, stoffe);
        Assert.Equal((7, 0.035, 30.0, 1030.0),
                     (stand.Schichten[1].IdBaustoff!.Value, stand.Schichten[1].Lambda!.Value,
                      stand.Schichten[1].Rho!.Value, stand.Schichten[1].Cp!.Value));

        stand.StoffWaehlen(1, null, stoffe);
        Assert.Null(stand.Schichten[1].IdBaustoff);
        Assert.Equal(0.035, stand.Schichten[1].Lambda);

        stand.Uebernehmen(stand.Arbeit.Kopie());
        Assert.False(stand.Geaendert);
    }
}

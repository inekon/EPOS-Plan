using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Verwaltung „Rückkühlwerke" (K-F5a) im Gerüst der Verwaltungen: Katalogliste mit sechs Spalten, Stammblatt mit
/// den Gruppen Bauart und Nennpunkt, Annäherung, Ventilator, Befeuchtung und Wasser (nur nass) und Freikühlung samt
/// Herkunft, die Vorgaben als Platzhalter, der Hinweis auf die 42. BImSchV, Speichern über den Persistenzwert, Löschen
/// nur ohne Verwendung, Schloss über die Rückfrage, Rückweg und der Fall ohne Gaben.
///
/// <para>Die Kultur ist über die Hausvorrichtung auf de-DE gepinnt — die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class RueckkuehlwerkDialogTests : EposBunitContext
{
    public RueckkuehlwerkDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly RueckkuehlwerkBauart[] BAUARTEN =
    {
        new("TROCKEN", "Trockenkühler", false, false),
        new("ADIABAT", "Adiabatischer Kühler", true, true),
        new("HYBRID", "Hybridkühler", true, true),
        new("KUEHLTURM_OFFEN", "Kühlturm, offen", true, false),
        new("KUEHLTURM_GESCHLOSSEN", "Kühlturm, geschlossen", true, false),
    };

    private static readonly RueckkuehlwerkWahl[] WEGE = { new("FEST", "fest"), new("LASTABHAENGIG", "lastabhängig") };
    private static readonly RueckkuehlwerkWahl[] REGELUNGEN =
        { new("EIN_AUS", "Ein/Aus"), new("STUFEN", "Stufen"), new("DREHZAHL", "Drehzahl") };
    private static readonly RueckkuehlwerkWahl[] SCHALTUNGEN = { new("PARALLEL", "parallel"), new("REIHE", "in Reihe") };

    /// <summary>Der Katalog im Speicher: Id 1 ausgeliefert (trocken), Id 2 eigen (Kühlturm), Id 3 eigen und verwendet.</summary>
    private sealed class Katalog
    {
        internal readonly List<RueckkuehlwerkDaten> Saetze = new()
        {
            new RueckkuehlwerkDaten { Id = 1, Bezeichner = "A Trockenkühler 200 kW", Bauart = "TROCKEN", NennleistungKw = 200, Auslieferung = true },
            new RueckkuehlwerkDaten { Id = 2, Bezeichner = "B Kühlturm 500 kW", Bauart = "KUEHLTURM_OFFEN", NennleistungKw = 500 },
            new RueckkuehlwerkDaten { Id = 3, Bezeichner = "C Hybridkühler 300 kW", Bauart = "HYBRID", NennleistungKw = 300 },
        };

        internal readonly List<RueckkuehlwerkDaten> Gespeichert = new();
        internal readonly List<int> Geloescht = new();
        internal string Pruefbefund = "";
        internal Dictionary<int, int> Verwendung = new() { [3] = 2 };
        internal bool? Geschlossen;
        private int _naechste = 100;

        internal IReadOnlyList<Katalogfilterzeile> Zeilen()
            => Saetze.Select(s => new Katalogfilterzeile(s.Id, s.Bezeichner)
                    {
                        Geschuetzt = s.Auslieferung,
                        Schluessel = s.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }
                    .MitText(Katalogfilterprofil.SpBezeichner, s.Bezeichner)
                    .MitText(Katalogfilterprofil.SpBauart, BAUARTEN.First(b => b.Code == (s.Bauart == "" ? "TROCKEN" : s.Bauart)).Text)
                    .MitZahl(Katalogfilterprofil.SpNennleistung, s.NennleistungKw, 1)
                    .MitZahl(Katalogfilterprofil.SpAnnaeherung, s.AnnaeherungNennK, 1)
                    .MitText(Katalogfilterprofil.SpVentilator, "Ein/Aus")
                    .MitText(Katalogfilterprofil.SpFreikuehlung, "parallel"))
                .ToList();

        internal RueckkuehlwerkSpeicherErgebnis Speichern(RueckkuehlwerkDaten d)
        {
            RueckkuehlwerkDaten kopie = d.Kopie();
            Gespeichert.Add(kopie);
            if (kopie.Id <= 0)
            {
                kopie.Id = ++_naechste;
                Saetze.Add(kopie);
            }
            else
            {
                Saetze[Saetze.FindIndex(s => s.Id == kopie.Id)] = kopie;
            }
            return new RueckkuehlwerkSpeicherErgebnis(true, "", kopie.Id);
        }
    }

    /// <summary>Die Vorgaben wie im Kern: Annäherung 10 K trocken bzw. 5 K nass, die übrigen Festwerte.</summary>
    private static RueckkuehlwerkVorgaben Vorgaben(RueckkuehlwerkDaten d)
    {
        bool nass = BAUARTEN.FirstOrDefault(b => b.Code == d.Bauart)?.Nass == true;
        bool kuehlturm = d.Bauart.StartsWith("KUEHLTURM", StringComparison.Ordinal);
        return new RueckkuehlwerkVorgaben(kuehlturm ? 5 : 10, 2, 0.2, 0.8, 25, kuehlturm ? 0.9 : 1.0, 3, kuehlturm ? 0.0001 : 0, nass);
    }

    private IRenderedComponent<RueckkuehlwerkDialog> Aufbauen(Katalog? k = null, Schlosspruefung? schloss = null)
    {
        Katalog kat = k ?? new Katalog();
        return Render<RueckkuehlwerkDialog>(b => b
            .Add(x => x.Katalogzeilen, () => schloss is null ? kat.Zeilen() : schloss.Markieren(kat.Zeilen()))
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerRueckkuehlwerk(
                s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Bauarten, BAUARTEN)
            .Add(x => x.Wege, WEGE)
            .Add(x => x.Regelungen, REGELUNGEN)
            .Add(x => x.Schaltungen, SCHALTUNGEN)
            .Add(x => x.Lies, id => kat.Saetze.FirstOrDefault(s => s.Id == id) is RueckkuehlwerkDaten d
                ? Mit(d.Kopie(), schloss) : null)
            .Add(x => x.Pruefen, d => kat.Pruefbefund)
            .Add(x => x.Speichern, kat.Speichern)
            .Add(x => x.Loeschen, id =>
            {
                kat.Geloescht.Add(id);
                kat.Saetze.RemoveAll(s => s.Id == id);
                return new RueckkuehlwerkSpeicherErgebnis(true, "", id);
            })
            .Add(x => x.Duplizieren, (id, name) =>
            {
                RueckkuehlwerkDaten neu = kat.Saetze.First(s => s.Id == id).Kopie();
                neu.Id = 0;
                neu.Bezeichner = name;
                neu.Auslieferung = false;
                return kat.Speichern(neu);
            })
            .Add(x => x.Schloss, schloss?.Weg())
            .Add(x => x.Verwendung, () => kat.Verwendung)
            .Add(x => x.Vorgaben, Vorgaben)
            .Add(x => x.Geschlossen, e => kat.Geschlossen = e));
    }

    private static RueckkuehlwerkDaten Mit(RueckkuehlwerkDaten d, Schlosspruefung? schloss)
    {
        if (schloss is not null) d.Auslieferung = schloss.Gesperrt.Contains(d.Id);
        return d;
    }

    private static IElement Fussleiste(IRenderedComponent<RueckkuehlwerkDialog> cut)
        => cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();

    private static IElement Knopf(IRenderedComponent<RueckkuehlwerkDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung(IRenderedComponent<RueckkuehlwerkDialog> cut, string text)
        => cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

    private static IElement Feld(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  .QuerySelector("input, textarea, select")!;

    private static IElement Stammblatt(IRenderedComponent<RueckkuehlwerkDialog> cut) => cut.Find(".epos-stammblatt");

    private static string[] Gruppen(IRenderedComponent<RueckkuehlwerkDialog> cut)
        => cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray();

    // =================================================================================
    // Gerüst, Liste, Gruppen
    // =================================================================================

    [Fact]
    public void Das_Geruest_steht_wie_bei_den_Verwaltungen()
    {
        var cut = Aufbauen();

        Assert.Contains("epos-katalog-dialog", cut.Find(".epos-dialog").ClassName);
        Assert.Equal("Rückkühlwerke", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Empty(cut.FindAll(".epos-zeilenwahl"));
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu…", "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-dialog-zu"));
        Assert.Equal(EPOS.UI.Seiten.Seitenschluessel.Rueckkuehlwerk, RueckkuehlwerkDialog.SEITE);
    }

    [Fact]
    public void Die_Liste_fuehrt_die_sechs_Spalten_des_Kerns()
    {
        var cut = Aufbauen();
        string kopf = cut.Find(".epos-katalogliste thead").TextContent;
        foreach (string spalte in new[] { "Name", "Bauart", "Nennleistung", "Annäherung", "Ventilator", "Freikühlung" })
            Assert.Contains(spalte, kopf);
    }

    [Fact]
    public void Ein_trockener_Satz_hat_keine_Wassergruppe_und_keinen_BImSchV_Hinweis()
    {
        var cut = Aufbauen();                                        // A: trocken, ausgeliefert
        Assert.Equal(new[] { "Bauart und Nennpunkt", "Annäherung", "Ventilator", "Freikühlung", "Herkunft" }, Gruppen(cut));
        Assert.DoesNotContain("42. BImSchV", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Eine_nasse_Bauart_zeigt_die_Wassergruppe_und_den_Hinweis()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);                                   // B: Kühlturm, eigen

        Assert.Equal(new[] { "Bauart und Nennpunkt", "Annäherung", "Ventilator", "Befeuchtung und Wasser", "Freikühlung", "Herkunft" },
                     Gruppen(cut));
        Assert.Contains("42. BImSchV", cut.Markup, StringComparison.Ordinal);
        // Ein Kühlturm befeuchtet nicht - nur die drei Felder der Wasserbilanz.
        IElement blatt = Stammblatt(cut);
        Assert.DoesNotContain(blatt.QuerySelectorAll(".epos-feld-text"), e => e.TextContent.Trim() == "Befeuchtungswirkungsgrad");
        Assert.Contains(blatt.QuerySelectorAll(".epos-feld-text"), e => e.TextContent.Trim() == "Eindickung");

        Zeilenklick.Zeile(cut, 2);                                   // C: Hybrid - mit Befeuchtung
        Assert.Contains(Stammblatt(cut).QuerySelectorAll(".epos-feld-text"), e => e.TextContent.Trim() == "Befeuchtungswirkungsgrad");
    }

    [Fact]
    public void Leere_Felder_zeigen_die_Vorgabe_als_Platzhalter()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);                                   // B: Kühlturm, Annäherung leer

        Assert.Equal("Vorgabe: 5 K", Feld(Stammblatt(cut), "Annäherung im Nennpunkt").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 3", Feld(Stammblatt(cut), "Eindickung").GetAttribute("placeholder"));
        Assert.Contains("Vorgabe: Hilfsstrom der Rückkühlung", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Vorgabe: Ein/Aus", Feld(Stammblatt(cut), "Ventilatorregelung").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Ein_Auslieferungssatz_steht_als_Text()
    {
        var cut = Aufbauen();                                        // A: ausgeliefert
        Assert.True(cut.Instance.Auslieferung);
        Assert.Empty(Stammblatt(cut).QuerySelectorAll("input"));
        Assert.Contains("Vorgabe: 10 K", Stammblatt(cut).TextContent, StringComparison.Ordinal);
        Assert.Equal("true", Knopf(cut, "Speichern").GetAttribute("aria-disabled"));
    }

    // =================================================================================
    // Speichern, Neu, Löschen, Schloss
    // =================================================================================

    [Fact]
    public void Speichern_schreibt_den_Persistenzwert_der_Auswahl()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);                                   // B

        Feld(Stammblatt(cut), "Ventilatorregelung").Change("2");     // Stufen
        Feld(Stammblatt(cut), "Schaltung der freien Kühlung").Change("2"); // in Reihe
        Feld(Stammblatt(cut), "Ventilatorstufen").Input("3");
        Assert.True(cut.Instance.Geaendert);

        Knopf(cut, "Speichern").Click();
        RueckkuehlwerkDaten d = Assert.Single(k.Gespeichert);
        Assert.Equal(("STUFEN", "REIHE", (int?)3), (d.VentilatorRegelung, d.FreikuehlungSchaltung, d.VentilatorStufen));
        Assert.False(cut.Instance.Geaendert);
    }

    [Fact]
    public void Eine_Pruefmeldung_des_Kerns_haelt_das_Speichern_an()
    {
        var k = new Katalog { Pruefbefund = "Eindickung: Der Wert liegt außerhalb seines Bereichs (über 1 bis 20)." };
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);
        Feld(Stammblatt(cut), "Eindickung").Input("25");
        Knopf(cut, "Speichern").Click();

        Assert.Empty(k.Gespeichert);
        Assert.Equal(k.Pruefbefund, cut.Instance.Meldung);
    }

    [Fact]
    public void Neu_legt_nach_OK_an_und_waehlt_den_neuen_Satz()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Knopf(cut, "Neu…").Click();
        Assert.True(cut.Instance.NeuOffen);

        IElement ueberlagerung = cut.Find(".epos-rueckkuehlwerk-neu");
        Feld(ueberlagerung, "Bezeichnung").Input("D Adiabat 100 kW");
        Feld(ueberlagerung, "Bauart").Change("2");                   // adiabat
        cut.Find(".epos-rueckkuehlwerk-neu .epos-knopf--primaer").Click();

        Assert.False(cut.Instance.NeuOffen);
        RueckkuehlwerkDaten d = Assert.Single(k.Gespeichert);
        Assert.Equal(("D Adiabat 100 kW", "ADIABAT"), (d.Bezeichner, d.Bauart));
        Assert.Equal("101", cut.Instance.Gewaehlt);
    }

    [Fact]
    public void Ein_verwendeter_Satz_ist_gegen_Loeschen_gesperrt()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 2);                                   // C: in 2 Projektanlagen gewählt

        IElement loeschen = Handlung(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains("In 2 Projektanlage(n) gewählt", loeschen.GetAttribute("title") ?? "", StringComparison.Ordinal);
        Assert.Contains("in 2 Projektanlage(n) gewählt", Stammblatt(cut).TextContent, StringComparison.Ordinal);

        Zeilenklick.Zeile(cut, 1);                                   // B: frei
        Handlung(cut, "Löschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { 2 }, k.Geloescht);
    }

    [Fact]
    public void Das_Schloss_schaltet_nach_Rueckfrage_ueber_den_Weg_der_Huelle()
    {
        var schloss = new Schlosspruefung(1);
        var cut = Aufbauen(schloss: schloss);
        Zeilenklick.Zeile(cut, 1);                                   // B, offen

        Assert.Equal("Schloss setzen...", Schlosspruefung.Beschriftung(cut));
        Schlosspruefung.Knopf(cut).Click();
        Schlosspruefung.Ja(cut);

        Assert.Contains(2, schloss.Gesperrt);
        Assert.True(cut.Instance.Auslieferung);
    }

    // =================================================================================
    // Rückweg und ohne Gaben
    // =================================================================================

    [Fact]
    public void Beenden_schliesst_und_haelt_bei_Aenderungen_an()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);
        Feld(Stammblatt(cut), "Nennleistung (abzuführende Wärme)").Input("550");

        Knopf(cut, "Beenden").Click();
        Assert.Null(k.Geschlossen);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_MSG_UNGESPEICHERT, cut.Instance.Meldung);

        Knopf(cut, "Verwerfen").Click();
        Knopf(cut, "Beenden").Click();
        Assert.True(k.Geschlossen);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_die_Verwaltung_leer()
    {
        var cut = Render<RueckkuehlwerkDialog>();
        Assert.Empty(cut.FindAll(".epos-katalogliste tbody tr"));
        Assert.Contains("noch kein Rückkühlwerk", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(new[] { "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }
}

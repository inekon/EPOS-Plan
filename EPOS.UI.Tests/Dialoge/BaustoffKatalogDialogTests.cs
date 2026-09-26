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
/// Die Verwaltung „Baustoffe" (Gebäudesimulation G3, Welle C) im Gerüst der Verwaltungen:
/// Katalogliste mit sieben Spalten und dem Werkzeug „nur herstellerneutral", Stammblatt mit
/// Kenndaten und Herkunft, Auswahlleiste (Vergleichen, Duplizieren…, Schloss, Löschen mit
/// Sperrgrund), Fußleiste Speichern · Verwerfen · Status · Neu… · Beenden.
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class BaustoffKatalogDialogTests : EposBunitContext
{
    public BaustoffKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Der Katalog im Speicher: Id 1 und 2 herstellerneutral (1 geschützt), Id 1001 mit Hersteller.</summary>
    private sealed class Katalog
    {
        internal readonly List<BaustoffDaten> Saetze = new()
        {
            new BaustoffDaten { Id = 1, Bezeichner = "Kalkzementputz", Gruppe = "Putze", Lambda = 1.0, Rho = 1800, Cp = 1000,
                                Quelle = "DIN 4108-4", Herkunft = "Vorgabe", Auslieferung = true },
            new BaustoffDaten { Id = 2, Bezeichner = "Holzfaser", Gruppe = "Dämmstoffe", Lambda = 0.04, Rho = 160, Cp = 2100,
                                Herkunft = "Manuell" },
            new BaustoffDaten { Id = 1001, Bezeichner = "Dämmplatte 035", Gruppe = "Dämmstoffe", Hersteller = "Hersteller A",
                                Lambda = 0.035, Rho = 30, Cp = 1030, Herkunft = "Manuell" }
        };

        internal readonly List<BaustoffDaten> Gespeichert = new();
        internal readonly List<int> Geloescht = new();
        internal readonly List<(int Id, string Name)> Dupliziert = new();
        internal readonly List<BaustoffDaten> Geprueft = new();
        internal string Pruefbefund = "";
        internal Dictionary<int, int> Verwendung = new();
        internal bool? Geschlossen;
        private int _naechste = 5000;

        internal IReadOnlyList<Katalogfilterzeile> Zeilen()
            => Saetze.Select(s => new Katalogfilterzeile(s.Id, s.Bezeichner)
                    {
                        Geschuetzt = s.Auslieferung,
                        Schluessel = s.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }
                    .MitText(Katalogfilterprofil.SpBezeichner, s.Bezeichner)
                    .MitText(Katalogfilterprofil.SpGruppe, s.Gruppe)
                    .MitText(Katalogfilterprofil.SpHersteller, s.Hersteller)
                    .MitZahl(Katalogfilterprofil.SpLambda, s.Lambda, 3)
                    .MitZahl(Katalogfilterprofil.SpRho, s.Rho, 0)
                    .MitZahl(Katalogfilterprofil.SpCp, s.Cp, 0)
                    .MitText(Katalogfilterprofil.SpQuelle, s.Quelle))
                .ToList();

        internal BaustoffSpeicherErgebnis Speichern(BaustoffDaten d)
        {
            BaustoffDaten kopie = d.Kopie();
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
            return new BaustoffSpeicherErgebnis(true, "", kopie.Id);
        }
    }

    private IRenderedComponent<BaustoffKatalogDialog> Aufbauen(Katalog? k = null, Schlosspruefung? schloss = null)
    {
        Katalog kat = k ?? new Katalog();
        return Render<BaustoffKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, () => schloss is null ? kat.Zeilen() : schloss.Markieren(kat.Zeilen()))
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBaustoff(
                s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, id => kat.Saetze.FirstOrDefault(s => s.Id == id) is BaustoffDaten d
                ? Mit(d.Kopie(), schloss) : null)
            .Add(x => x.Pruefen, d => { kat.Geprueft.Add(d.Kopie()); return kat.Pruefbefund; })
            .Add(x => x.Speichern, kat.Speichern)
            .Add(x => x.Loeschen, id =>
            {
                kat.Geloescht.Add(id);
                kat.Saetze.RemoveAll(s => s.Id == id);
                return new BaustoffSpeicherErgebnis(true, "", id);
            })
            .Add(x => x.Duplizieren, (id, name) =>
            {
                kat.Dupliziert.Add((id, name));
                BaustoffDaten neu = kat.Saetze.First(s => s.Id == id).Kopie();
                neu.Id = 0;
                neu.Bezeichner = name;
                neu.Auslieferung = false;
                return kat.Speichern(neu);
            })
            .Add(x => x.Schloss, schloss?.Weg())
            .Add(x => x.Verwendung, () => kat.Verwendung)
            .Add(x => x.Geschlossen, e => kat.Geschlossen = e));
    }

    private static BaustoffDaten Mit(BaustoffDaten d, Schlosspruefung? schloss)
    {
        if (schloss is not null) d.Auslieferung = schloss.Gesperrt.Contains(d.Id);
        return d;
    }

    private static IElement Fussleiste(IRenderedComponent<BaustoffKatalogDialog> cut)
        => cut.FindAll(".epos-katalog-dialog > .epos-leiste").Last();

    private static IElement Knopf(IRenderedComponent<BaustoffKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung(IRenderedComponent<BaustoffKatalogDialog> cut, string text)
        => cut.FindAll(".epos-auswahlleiste button").First(b => b.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

    /// <summary>Das Eingabefeld unter einer Beschriftung — im Stammblatt oder in der Überlagerung.</summary>
    private static IElement Feld(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  .QuerySelector("input, textarea")!;

    private static IElement Stammblatt(IRenderedComponent<BaustoffKatalogDialog> cut) => cut.Find(".epos-stammblatt");

    // =================================================================================
    // Gerüst und Feldbestand
    // =================================================================================

    [Fact]
    public void Das_Geruest_steht_wie_bei_den_Verwaltungen()
    {
        var cut = Aufbauen();

        Assert.Contains("epos-katalog-dialog", cut.Find(".epos-dialog").ClassName);
        Assert.Equal("Baustoffe", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.FindAll(".epos-katalogliste tbody tr").Count);
        Assert.Empty(cut.FindAll(".epos-zeilenwahl"));
        Assert.Equal(new[] { "Kenndaten", "Herkunft" },
                     cut.FindAll(".epos-stammblattgruppe-titel").Select(e => e.TextContent).ToArray());
        Assert.Equal(new[] { "Speichern", "Verwerfen", "Neu…", "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.Same(Fussleiste(cut).QuerySelectorAll("button").Last(),
                    Fussleiste(cut).QuerySelectorAll("button.epos-knopf--primaer").Single());
        Assert.Single(cut.FindAll(".epos-dialog-kopf .epos-dialog-zu"));
    }

    [Fact]
    public void Die_Liste_fuehrt_die_sieben_Spalten_des_Kerns()
    {
        var cut = Aufbauen();
        string kopf = cut.Find(".epos-katalogliste thead").TextContent;
        foreach (string spalte in new[] { "Name", "Gruppe", "Hersteller", "λ", "Rohdichte", "Wärmekapazität", "Quelle" })
            Assert.Contains(spalte, kopf);
    }

    [Fact]
    public void Das_Stammblatt_traegt_die_sieben_Kenndaten_mit_Einheiten()
    {
        var cut = Aufbauen();
        Zeilenklick.Zeile(cut, 1);                                   // Holzfaser, eigener Satz

        IElement blatt = Stammblatt(cut);
        Assert.Equal(new[] { "Name", "Gruppe", "Hersteller", "Wärmeleitfähigkeit λ", "Rohdichte ρ",
                             "Spez. Wärmekapazität c_p", "Quelle" },
                     blatt.QuerySelectorAll(".epos-stammblattgruppe")[0].QuerySelectorAll(".epos-feld-text")
                          .Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal(new[] { "W/(mK)", "kg/m³", "J/(kgK)" },
                     blatt.QuerySelectorAll(".epos-einheit").Select(e => e.TextContent.Trim()).ToArray());
        Assert.Equal("0,04", Feld(blatt, "Wärmeleitfähigkeit λ").GetAttribute("value"));
        Assert.Equal("herstellerneutral", Feld(blatt, "Hersteller").GetAttribute("placeholder"));
        Assert.Equal("Dämmstoffe · herstellerneutral · eigener Satz", cut.Find(".epos-stammblatt-unter").TextContent);
        Assert.Contains("in keiner Schicht des Aufbaukatalogs", blatt.TextContent);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        var cut = Render<BaustoffKatalogDialog>();

        Assert.Equal("Baustoffe", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("noch keinen Baustoff", cut.Find(".epos-baustoff-leer").TextContent);
        Assert.Equal(new[] { "Beenden" },
                     Fussleiste(cut).QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
    }

    // =================================================================================
    // Auslieferungssatz
    // =================================================================================

    [Fact]
    public void Ein_Auslieferungssatz_ist_nur_lesbar_und_Speichern_nennt_den_Grund()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);                                       // die erste Zeile ist geschützt

        Assert.True(cut.Instance.Auslieferung);
        IElement blatt = Stammblatt(cut);
        Assert.Empty(blatt.QuerySelectorAll(".epos-stammblattgruppe")[0].QuerySelectorAll("input"));
        Assert.Contains("Kalkzementputz", blatt.QuerySelector(".epos-stammblattwerte")!.TextContent);

        IElement speichern = Knopf(cut, "Speichern");
        Assert.Equal("true", speichern.GetAttribute("aria-disabled"));
        speichern.Click();

        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_SPEICHERN_GESPERRT, cut.Instance.Meldung);
        Assert.Empty(k.Gespeichert);
    }

    /// <summary>
    /// Ein abgelehntes Speichern nennt den Grund auch ROT in der Statuszeile neben dem
    /// Knopf — das Warnband oben ist bei einer langen Liste nicht im Blick.
    /// </summary>
    [Fact]
    public void Ein_abgelehntes_Speichern_steht_rot_in_der_Statuszeile()
    {
        var cut = Aufbauen(new Katalog());                          // die erste Zeile ist geschützt

        Knopf(cut, "Speichern").Click();

        IElement status = cut.Find(".epos-leiste-fueller.epos-status");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_SPEICHERN_GESPERRT, status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);
    }

    // =================================================================================
    // Der eine Schreibweg
    // =================================================================================

    [Fact]
    public void Speichern_prueft_einmal_und_schreibt_den_Arbeitsstand()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        Feld(Stammblatt(cut), "Wärmeleitfähigkeit λ").Input("0,045");
        Assert.True(cut.Instance.Geaendert);
        Assert.Equal("1 Feld geändert", cut.Find(".epos-stammblatt-hinweis").TextContent);

        Knopf(cut, "Speichern").Click();

        Assert.Single(k.Geprueft);
        BaustoffDaten gespeichert = Assert.Single(k.Gespeichert);
        Assert.Equal(2, gespeichert.Id);
        Assert.Equal(0.045, gespeichert.Lambda);
        Assert.False(cut.Instance.Geaendert);
        Assert.StartsWith("Gespeichert um", cut.Instance.Status);
        Assert.Equal("2", cut.Instance.Gewaehlt);
    }

    [Fact]
    public void Eine_verletzte_Regel_meldet_und_schreibt_nicht()
    {
        var k = new Katalog { Pruefbefund = "λ = 900 liegt außerhalb des zulässigen Bereichs." };
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        Feld(Stammblatt(cut), "Wärmeleitfähigkeit λ").Input("900");
        Knopf(cut, "Speichern").Click();

        Assert.Equal(k.Pruefbefund, cut.Instance.Meldung);
        Assert.Contains(k.Pruefbefund, cut.Find(".epos-warnbanner").TextContent);
        Assert.Empty(k.Gespeichert);
        Assert.True(cut.Instance.Geaendert);
    }

    [Fact]
    public void Verwerfen_nimmt_den_Arbeitsstand_zurueck_und_Aenderungen_halten_die_Wahl()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);
        Feld(Stammblatt(cut), "Gruppe").Input("Holz");

        Zeilenklick.Zeile(cut, 2);                                   // haelt an
        Assert.Equal("2", cut.Instance.Gewaehlt);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_MSG_UNGESPEICHERT, cut.Instance.Meldung);

        Knopf(cut, "Verwerfen").Click();
        Assert.False(cut.Instance.Geaendert);
        Assert.Equal("Dämmstoffe", cut.Instance.Arbeitsstand.Gruppe);
        Assert.Empty(k.Gespeichert);
    }

    [Fact]
    public void Neu_legt_nach_OK_an_und_waehlt_den_neuen_Satz()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);

        Knopf(cut, "Neu…").Click();
        Assert.True(cut.Instance.NeuOffen);
        IElement neu = cut.Find(".epos-baustoff-neu");
        Feld(neu, "Name").Input("Lehmputz");
        Feld(neu, "Wärmeleitfähigkeit λ").Input("0,8");
        Feld(neu, "Rohdichte ρ").Input("1700");
        neu.QuerySelectorAll(".epos-leiste button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.NeuOffen);
        BaustoffDaten angelegt = Assert.Single(k.Gespeichert);
        Assert.Equal(0, k.Geprueft.Single().Id);
        Assert.Equal("Lehmputz", angelegt.Bezeichner);
        Assert.Equal(0.8, angelegt.Lambda);
        Assert.Equal(1700, angelegt.Rho);
        Assert.Null(angelegt.Cp);
        Assert.Equal(angelegt.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), cut.Instance.Gewaehlt);
        Assert.Equal("„Lehmputz“ angelegt.", cut.Instance.Status);
    }

    [Fact]
    public void Neu_Abbrechen_schreibt_nichts()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);

        Knopf(cut, "Neu…").Click();
        Feld(cut.Find(".epos-baustoff-neu"), "Name").Input("Verworfen");
        cut.Find(".epos-baustoff-neu").QuerySelectorAll(".epos-leiste button")
           .First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.False(cut.Instance.NeuOffen);
        Assert.Empty(k.Gespeichert);
        Assert.Empty(k.Geprueft);
    }

    // =================================================================================
    // Werkzeug, Auswahlleiste
    // =================================================================================

    [Fact]
    public void Nur_herstellerneutral_setzt_den_Trichter_ohne_Wert()
    {
        var cut = Aufbauen();

        cut.Find(".epos-katalogliste .epos-schalter-kasten").Change(true);

        Assert.True(cut.Instance.NurHerstellerneutral);
        Assert.Equal(Katalogfilterprofil.AUSDRUCK_LEER, cut.Instance.Filterstand.Ausdruck(Katalogfilterprofil.SpHersteller));
        Assert.Equal(2, cut.FindAll(".epos-katalogliste tbody tr").Count);
    }

    [Fact]
    public void Loeschen_ist_gesperrt_solange_Schichten_den_Stoff_verwenden()
    {
        var k = new Katalog();
        k.Verwendung[2] = 3;
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        IElement loeschen = Handlung(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Contains("In 3 Schicht(en) des Aufbaukatalogs verwendet", loeschen.GetAttribute("title") ?? loeschen.OuterHtml);
        Assert.Contains("in 3 Schicht(en) des Aufbaukatalogs", Stammblatt(cut).TextContent);
    }

    [Fact]
    public void Loeschen_fragt_zurueck_und_loescht_einen_eigenen_Satz()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);

        Handlung(cut, "Löschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Contains("„Holzfaser“", Schlosspruefung.Frage(cut));
        cut.Find(".epos-rueckfrage").QuerySelectorAll(".epos-knopf").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.Equal(new[] { 2 }, k.Geloescht);
        Assert.Equal("„Holzfaser“ gelöscht.", cut.Instance.Status);
    }

    [Fact]
    public void Duplizieren_legt_eine_Kopie_an_und_waehlt_sie()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);                                       // Kalkzementputz (Auslieferung)

        Handlung(cut, "Duplizieren").Click();
        Assert.True(cut.Instance.Duplizierfrage);
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal((1, "Kalkzementputz (Kopie)"), k.Dupliziert.Single());
        Assert.False(cut.Instance.Auslieferung);
        Assert.Equal("Kalkzementputz (Kopie)", cut.Instance.Arbeitsstand.Bezeichner);
    }

    [Fact]
    public void Das_Schloss_schaltet_nach_Rueckfrage_ueber_den_Weg_der_Huelle()
    {
        var schloss = new Schlosspruefung(1);
        var cut = Aufbauen(schloss: schloss);
        Zeilenklick.Zeile(cut, 1);                                   // Holzfaser, offen

        Assert.Equal("Schloss setzen...", Schlosspruefung.Beschriftung(cut));
        Schlosspruefung.Knopf(cut).Click();
        Assert.True(cut.Instance.Schlossfrage);
        Schlosspruefung.Ja(cut);

        Assert.Contains(2, schloss.Gesperrt);
        Assert.True(cut.Instance.Auslieferung);
    }

    // =================================================================================
    // Schluss
    // =================================================================================

    [Fact]
    public void Beenden_schliesst_und_haelt_bei_Aenderungen_an()
    {
        var k = new Katalog();
        var cut = Aufbauen(k);
        Zeilenklick.Zeile(cut, 1);
        Feld(Stammblatt(cut), "Quelle").Input("Datenblatt");

        Knopf(cut, "Beenden").Click();
        Assert.Null(k.Geschlossen);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_MSG_UNGESPEICHERT, cut.Instance.Meldung);

        Knopf(cut, "Verwerfen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(k.Geschlossen);
    }
}

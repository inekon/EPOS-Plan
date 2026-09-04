using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die drei Bedarfsprofil-Blätter (iU9-W9.5). Soll sind die Feldkarten von
/// <c>Form_Prozesswaerme</c> (21 + 4 Zeilen), <c>Form_Stromverbraucher</c> (22 + 3) und
/// <c>Form_Brauchwasser</c> (21 + 4).
///
/// <para>Der Feldbestand wird JE AUSPRÄGUNG geprüft (Risiko R‑W8‑1): Die drei
/// unterscheiden sich in Beschriftungen, im Katalograster und im Rechenweg.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche
/// Beschriftungen.</para>
/// </summary>
public class BedarfsProfileDialogTests : BunitContext
{
    private static readonly BedarfsKatalogZeile[] KATALOG =
    {
        new("Profil A", "Typ 1"),
        new("Profil B", "Typ 2")
    };

    public BedarfsProfileDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static BedarfsProfilZeile Zeile(int idZ, string name = "Profil A", double summe = 12.5)
        => new() { IdZ = idZ, IdStamm = 3, Name = name, Summe = summe };

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(
        BedarfsArt art = BedarfsArt.Prozesswaerme,
        List<BedarfsProfilZeile>? zeilen = null,
        bool wizard = false,
        Func<bool>? projektGespeichert = null,
        Action<string, double>? summeSichern = null,
        Func<IReadOnlyList<string>, IReadOnlyDictionary<string, object>?>? simulieren = null,
        Func<IReadOnlyDictionary<string, object>?>? ergebnisGaben = null,
        Func<string, string, string, bool, IReadOnlyDictionary<string, object>>? typStammGaben = null,
        Func<IReadOnlyDictionary<string, object>>? typProfilGaben = null,
        Func<string, bool>? katalogLoeschen = null,
        Action? geaendert = null,
        Action<bool>? geschlossen = null,
        string meldungWert = "Bitte den Jahresverbrauch als Zahl in MWh eingeben, z. B. 12,5.",
        string labelJahresverbrauch = "jährlicher Prozesswärmebedarf:",
        string labelSumme = "Summe aller ausgew. Prozesse:",
        string btnDbAendern = "Prozess in DB ändern",
        string meldungGeloescht = "Prozess erfolgreich gelöscht.")
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.Zeilen, zeilen ?? new List<BedarfsProfilZeile> { Zeile(1) })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Katalog, () => KATALOG)
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung " + n, "Typ 1"))
            .Add(x => x.Jahressumme, _ => 42.0)
            .Add(x => x.Aufnehmen, n => new BedarfsProfilZeile
            {
                IdZ = 100000, IdStamm = 9, Name = n, Summe = 42.0
            })
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.ProjektGespeichert, projektGespeichert ?? (() => true))
            .Add(x => x.SummeSichern, summeSichern)
            .Add(x => x.Simulieren, simulieren ?? (_ => new Dictionary<string, object>()))
            .Add(x => x.ErgebnisGaben, ergebnisGaben ?? (() => new Dictionary<string, object>()))
            .Add(x => x.TypStammGaben, typStammGaben)
            .Add(x => x.TypProfilGaben, typProfilGaben)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.MeldungWertUngueltig, meldungWert)
            .Add(x => x.LabelJahresverbrauch, labelJahresverbrauch)
            .Add(x => x.LabelSumme, labelSumme)
            .Add(x => x.BtnDbAendernText, btnDbAendern)
            .Add(x => x.MeldungGeloescht, meldungGeloescht)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<BedarfsProfileDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand JE AUSPRAEGUNG
    // =================================================================================

    [Fact]
    public void Prozesswaerme_traegt_die_Felder_ihrer_Karte()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme);

        Assert.Contains("jährlicher Prozesswärmebedarf:", cut.Markup);
        Assert.Contains("Summe aller ausgew. Prozesse:", cut.Markup);
        Assert.Contains("Ändern des Jahresverbrauchs", cut.Markup);

        // Ein Zahlenfeld ("neuer Wert"), vier gesperrte Textfelder + ein Textbereich.
        Assert.Single(cut.FindAll("input[inputmode=decimal]"));
        Assert.Equal(4, cut.FindAll("input[type=text][readonly]").Count);
        Assert.Single(cut.FindAll("textarea[readonly]"));

        // Prozess und Brauchwasser zeigen den Katalog als Raster MIT Typspalte.
        Assert.Contains("Typ</th>", cut.Markup);

        foreach (string t in new[] { "◀", "▶", "Prozess in DB ändern", "Prozess in DB neu",
                                     "Prozess in DB löschen", "Simulation",
                                     "monatlicher Verlauf", "Übernehmen", "OK", "Abbrechen" })
            Assert.NotNull(Knopf(cut, t));
    }

    [Fact]
    public void Stromverbraucher_zeigt_den_Katalog_OHNE_Typspalte()
    {
        var cut = Aufbauen(BedarfsArt.Stromverbraucher,
                           labelJahresverbrauch: "jährlicher Strombedarf:",
                           labelSumme: "Summe aller ausgewählten Strombedarfe:",
                           btnDbAendern: "Stromverbraucher ändern...",
                           meldungGeloescht: "");

        Assert.Contains("jährlicher Strombedarf:", cut.Markup);
        Assert.Contains("Summe aller ausgewählten Strombedarfe:", cut.Markup);
        Assert.DoesNotContain("Typ</th>", cut.Markup);
        Assert.NotNull(Knopf(cut, "Stromverbraucher ändern..."));
    }

    [Fact]
    public void Brauchwasser_traegt_die_Felder_seiner_Karte()
    {
        var cut = Aufbauen(BedarfsArt.Brauchwasser,
                           labelJahresverbrauch: "jährlicher Wärmebedarf:",
                           labelSumme: "Summe Brauchwasserprofile:",
                           btnDbAendern: "Profil in DB ändern",
                           meldungGeloescht: "");

        Assert.Contains("jährlicher Wärmebedarf:", cut.Markup);
        Assert.Contains("Summe Brauchwasserprofile:", cut.Markup);
        Assert.Contains("Typ</th>", cut.Markup);
        Assert.NotNull(Knopf(cut, "Profil in DB ändern"));
    }

    [Fact]
    public void Im_Assistenten_gibt_es_keine_Schlussleiste()
    {
        var cut = Aufbauen(wizard: true);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "OK");
    }

    // =================================================================================
    // Auswahl, Info und Summen
    // =================================================================================

    [Fact]
    public void Eine_Katalogzeile_zeigt_die_Summe_der_Monatswerte()
    {
        var cut = Aufbauen();

        cut.FindAll("button.epos-anlagenwahl")[1].Click();   // erste Katalogzeile

        Assert.Equal("42,00", cut.Instance.Jahresverbrauch);
        Assert.Equal("Profil A", cut.Instance.InfoName);
    }

    [Fact]
    public void Eine_Projektzeile_zeigt_die_Summe_IHRER_Zuordnung()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { Zeile(1, summe: 7.5) });

        Assert.Equal("7,50", cut.Instance.Jahresverbrauch);
    }

    [Fact]
    public void Die_Gesamtsumme_zaehlt_alle_Zuordnungen()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile>
        {
            Zeile(1, "Profil A", 10), Zeile(2, "Profil B", 5)
        });

        Assert.Contains("15,00", cut.Markup);
    }

    [Fact]
    public void Der_Pfeil_nach_links_uebernimmt_die_Katalogsumme()
    {
        bool gemeldet = false;
        var zeilen = new List<BedarfsProfilZeile>();
        var cut = Aufbauen(zeilen: zeilen, geaendert: () => gemeldet = true);

        cut.FindAll("button.epos-anlagenwahl").First().Click();
        Knopf(cut, "◀").Click();

        Assert.Single(zeilen);
        Assert.Equal(42.0, zeilen[0].Summe);
        Assert.True(gemeldet);
    }

    [Fact]
    public void Der_Pfeil_nach_rechts_trifft_die_markierte_Zeile()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1), Zeile(2, "Profil B") };
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Knopf(cut, "▶").Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].IdZ);
    }

    // =================================================================================
    // Uebernehmen
    // =================================================================================

    [Fact]
    public void Uebernehmen_ohne_Zeile_meldet()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile>());

        Knopf(cut, "Übernehmen").Click();

        Assert.Contains("Liste auswählen", cut.Instance.Meldung);
    }

    [Fact]
    public void Uebernehmen_schreibt_den_Wert_in_die_Zeile()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 1) };
        var cut = Aufbauen(zeilen: zeilen);

        cut.Find("input[inputmode=decimal]").Input("33,5");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(33.5, zeilen[0].Summe);
        Assert.Equal("33,50", cut.Instance.Jahresverbrauch);
        Assert.Contains("übernommen", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Befund W9‑B7, erledigt:</b> Der Bestand nannte beim Stromverbraucher kWh, bei
    /// Prozess und Brauchwasser MWh — für dieselbe Größe. Seit dem Entscheid des
    /// Anwenders vom 04.09.2026 steht in allen drei Ausprägungen derselbe Text, und die
    /// Einheit ist der Platzhalter <c>{0}</c>.
    /// </summary>
    [Theory]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Stromverbraucher)]
    [InlineData(BedarfsArt.Brauchwasser)]
    public void Ein_negativer_Wert_meldet_die_gewaehlte_Einheit(BedarfsArt art)
    {
        var cut = Aufbauen(art);
        cut.Find("input[inputmode=decimal]").Input("-1");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal("Bitte den Jahresverbrauch als Zahl in MWh eingeben, z. B. 12,5.",
                     cut.Instance.Meldung);
    }

    // =================================================================================
    // Simulation
    // =================================================================================

    [Fact]
    public void Simulation_ohne_Auswahl_meldet()
    {
        bool gerechnet = false;
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile>(),
                           simulieren: _ => { gerechnet = true; return new Dictionary<string, object>(); });

        Knopf(cut, "Simulation").Click();

        Assert.False(gerechnet);
        Assert.Contains("Liste auswählen", cut.Instance.Meldung);
    }

    [Fact]
    public void Simulation_rechnet_bei_Prozess_ALLE_Zuordnungen()
    {
        IReadOnlyList<string> uebergeben = Array.Empty<string>();
        var cut = Aufbauen(BedarfsArt.Prozesswaerme,
                           zeilen: new List<BedarfsProfilZeile> { Zeile(1, "Profil A"), Zeile(2, "Profil B") },
                           simulieren: n => { uebergeben = n; return new Dictionary<string, object>(); });

        Knopf(cut, "Simulation").Click();

        Assert.Equal(new[] { "Profil A", "Profil B" }, uebergeben);
        Assert.True(cut.Instance.ErgebnisOffen);
    }

    /// <summary>
    /// Die Brauchwassermaske legt GENAU EINEN Namen in die Liste
    /// (<c>btn_Simulation_Click</c>:296-298).
    /// </summary>
    [Fact]
    public void Simulation_rechnet_bei_Brauchwasser_nur_das_gewaehlte_Profil()
    {
        IReadOnlyList<string> uebergeben = Array.Empty<string>();
        var cut = Aufbauen(BedarfsArt.Brauchwasser,
                           zeilen: new List<BedarfsProfilZeile> { Zeile(1, "Profil A"), Zeile(2, "Profil B") },
                           simulieren: n => { uebergeben = n; return new Dictionary<string, object>(); });

        Knopf(cut, "Simulation").Click();

        Assert.Single(uebergeben);
        Assert.Equal("Profil A", uebergeben[0]);
    }

    [Fact]
    public void Monatlicher_Verlauf_ist_bis_zur_ersten_Simulation_gesperrt()
    {
        var cut = Aufbauen();

        Assert.True(Knopf(cut, "monatlicher Verlauf").HasAttribute("disabled"));

        Knopf(cut, "Simulation").Click();
        cut.Find(".epos-ueberlagerung-schliessen, .epos-dialog");   // Ueberlagerung steht

        Assert.False(Knopf(cut, "monatlicher Verlauf").HasAttribute("disabled"));
    }

    /// <summary>
    /// Der Hinweis „Vorschau ohne Projektwerte" erscheint NUR im Assistenten, nur bei
    /// ungespeichertem Projekt und nur EINMAL.
    /// </summary>
    [Fact]
    public void Der_Vorschauhinweis_kommt_im_Assistenten_genau_einmal()
    {
        bool gesichert = false;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, wizard: true,
                           projektGespeichert: () => false,
                           summeSichern: (_, _) => gesichert = true);

        cut.Find("input[inputmode=decimal]").Input("10");
        Knopf(cut, "Simulation").Click();

        Assert.False(gesichert);
        Assert.Contains("noch nicht gespeichert", cut.Instance.Meldung);

        // Zweiter Lauf: kein Hinweis mehr (die Meldung wird vorher geleert).
        Knopf(cut, "Simulation").Click();
        Assert.DoesNotContain("noch nicht gespeichert", cut.Instance.Meldung);
    }

    [Fact]
    public void Bei_gespeichertem_Projekt_wird_die_Summe_vor_dem_Lauf_gesichert()
    {
        double gesichert = -1;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, wizard: true,
                           projektGespeichert: () => true,
                           summeSichern: (_, w) => gesichert = w);

        cut.Find("input[inputmode=decimal]").Input("10");
        Knopf(cut, "Simulation").Click();

        Assert.Equal(10.0, gesichert);
    }

    /// <summary>
    /// Die Brauchwassermaske prüft NICHT, ob das Projekt gespeichert ist, und zeigt
    /// deshalb auch keinen Hinweis (<c>btn_Simulation_Click</c>:290).
    /// </summary>
    [Fact]
    public void Brauchwasser_sichert_ohne_Pruefung_und_ohne_Hinweis()
    {
        double gesichert = -1;
        var cut = Aufbauen(BedarfsArt.Brauchwasser, wizard: true,
                           projektGespeichert: () => false,
                           summeSichern: (_, w) => gesichert = w);

        cut.Find("input[inputmode=decimal]").Input("8");
        Knopf(cut, "Simulation").Click();

        Assert.Equal(8.0, gesichert);
        Assert.DoesNotContain("noch nicht gespeichert", cut.Instance.Meldung);
    }

    // =================================================================================
    // Ueberlagerungen
    // =================================================================================

    [Fact]
    public void DB_aendern_oeffnet_den_Stammkopf_im_Modus_Bearbeiten()
    {
        bool istNeu = true;
        var cut = Aufbauen(typStammGaben: (_, _, _, neu) =>
        {
            istNeu = neu;
            return new Dictionary<string, object>();
        });

        Knopf(cut, "Prozess in DB ändern").Click();

        Assert.True(cut.Instance.TypStammOffen);
        Assert.False(istNeu);
    }

    [Fact]
    public void DB_neu_fragt_erst_den_Namen()
    {
        string uebergeben = "";
        var cut = Aufbauen(typStammGaben: (name, _, _, _) =>
        {
            uebergeben = name;
            return new Dictionary<string, object>();
        });

        Knopf(cut, "Prozess in DB neu").Click();
        Assert.False(cut.Instance.TypStammOffen);

        cut.Find("[role=dialog] input[type=text]").Input("Neues Profil");
        cut.FindAll("[role=dialog] button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal("Neues Profil", uebergeben);
        Assert.True(cut.Instance.TypStammOffen);
    }

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Typ_aendern_Knopf()
    {
        Assert.DoesNotContain("Typ in DB ändern", Aufbauen().Markup);
        Assert.Contains("Typ in DB ändern",
                        Aufbauen(typProfilGaben: () => new Dictionary<string, object>()).Markup);
    }

    [Fact]
    public void Loeschen_fragt_nach()
    {
        string geloescht = "";
        var cut = Aufbauen(katalogLoeschen: n => { geloescht = n; return true; });

        cut.FindAll("button.epos-anlagenwahl")[1].Click();
        Knopf(cut, "Prozess in DB löschen").Click();

        Assert.Contains("wirklich gelöscht", cut.Markup);
        Knopf(cut, "Ja").Click();

        Assert.Equal("Profil A", geloescht);
        Assert.Contains("erfolgreich gelöscht", cut.Instance.Meldung);
    }

    // =================================================================================
    // Tastatur
    // =================================================================================

    [Fact]
    public void Esc_schliesst_mit_Abbruch()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_NICHT_wenn_eine_Ueberlagerung_offen_ist()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Knopf(cut, "Simulation").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(gerufen);
    }
}

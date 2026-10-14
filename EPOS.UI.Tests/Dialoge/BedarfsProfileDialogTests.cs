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
public class BedarfsProfileDialogTests : EposBunitContext
{
    /// <summary>
    /// Der Profilkatalog als <see cref="Katalogfilterzeile"/> — seit Stufe S3.1
    /// (W14a-E-10) traegt die Liste fuenf Spalten statt zweier und steht im Baustein
    /// <c>Katalogliste</c>.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        Katalogzeile(1, "Profil A", "Typ 1", 42.0, "Beschreibung A"),
        Katalogzeile(2, "Profil B", "Typ 2", 7.5, "Beschreibung B")
    };

    private static Katalogfilterzeile Katalogzeile(int id, string name, string typ,
                                                   double summe, string beschreibung)
        => new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpTyp, typ)
            .MitZahl(Katalogfilterprofil.SpJahressummeMwh, summe, 3)
            .MitText(Katalogfilterprofil.SpBeschreibung, beschreibung)
            .MitKennzeichen(Katalogfilterprofil.SpAuslieferung, false);

    /// <summary>Das Profil mit der Spalte „im Projekt verwendet" (Q12).</summary>
    private static Katalogfilterprofil Profil(BedarfsArt art)
        => Katalogfilterprofil.FuerBedarf(art, s => s).MitVerwendungsspalte(s => s);

    public BedarfsProfileDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
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
        Func<string, string, string, bool, IReadOnlyDictionary<string, object>>? typStammGaben = null,
        Func<IReadOnlyDictionary<string, object>>? typProfilGaben = null,
        Func<string, bool>? katalogLoeschen = null,
        Action? geaendert = null,
        Action<bool>? geschlossen = null,
        Energieeinheit? einheit = null,
        Action<Energieeinheit>? einheitGewaehlt = null,
        double jahressumme = 42.0,
        string meldungWert = "Bitte den Jahresverbrauch als Zahl in {0} eingeben, z. B. 12,5.",
        string labelJahresverbrauch = "jährlicher Prozesswärmebedarf:",
        string labelSumme = "Summe aller ausgew. Prozesse:",
        string btnDbAendern = "Prozess in DB ändern",
        string meldungGeloescht = "Prozess erfolgreich gelöscht.",
        string titel = "Prozesswärme")
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.TitelText, titel)
            .Add(x => x.Zeilen, zeilen ?? new List<BedarfsProfilZeile> { Zeile(1) })
            .Add(x => x.Wizard, wizard)
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Katalogprofil, Profil(art))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung " + n, "Typ 1"))
            .Add(x => x.Jahressumme, _ => jahressumme)
            .Add(x => x.Aufnehmen, n => new BedarfsProfilZeile
            {
                IdZ = 100000, IdStamm = 9, Name = n, Summe = jahressumme
            })
            .Add(x => x.KatalogLoeschen, katalogLoeschen ?? (_ => true))
            .Add(x => x.ProjektGespeichert, projektGespeichert ?? (() => true))
            .Add(x => x.SummeSichern, summeSichern)
            .Add(x => x.Simulieren, simulieren ?? (_ => new Dictionary<string, object>()))
            .Add(x => x.TypStammGaben, typStammGaben)
            .Add(x => x.TypProfilGaben, typProfilGaben)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Einheit, einheit ?? Energieeinheit.MWh)
            .Add(x => x.EinheitGewaehlt, einheitGewaehlt)
            .Add(x => x.MeldungWertUngueltig, meldungWert)
            .Add(x => x.LabelJahresverbrauch, labelJahresverbrauch)
            .Add(x => x.LabelSumme, labelSumme)
            .Add(x => x.BtnDbAendernText, btnDbAendern)
            .Add(x => x.MeldungGeloescht, meldungGeloescht)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    /// <summary>Stufe 4: eine Katalogzeile wählen — die Zeile ist die Wahl (Klick auf die Namenszelle).</summary>
    private static void KatalogKlick(IRenderedComponent<BedarfsProfileDialog> cut, int i)
        => cut.FindAll(".epos-katalogliste .epos-zeilenzelle--name")[i].Click();

    /// <summary>
    /// Stufe 4 (UeS2): Die Felder des Satzes stehen nur in der Satz-Überlagerung — „Bearbeiten…" der
    /// Knopfleiste am Satz (AW3b) öffnet sie (falls zu): beim Projektsatz die Zuordnung, beim Katalogsatz
    /// ohne Katalogeditor den Satz; zurück kommt ihr Körper.
    /// </summary>
    private static IElement Satz(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt, .epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog").Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    /// <summary>Ein Feld der Satz-Überlagerung (öffnet sie, falls zu).</summary>
    private static IElement Feld(IRenderedComponent<BedarfsProfileDialog> cut, string selektor)
    {
        Satz(cut);
        return cut.Find(".epos-satzueberlagerung-koerper " + selektor);
    }

    private static IElement Knopf(IRenderedComponent<BedarfsProfileDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>
    /// Die zwei Richtungsknöpfe stehen seit dem Anwenderentscheid #76 in der
    /// Mittelspalte des Bausteins <c>Zweispaltenauswahl</c>; ihr Zeichen ist ein
    /// eigenes Element neben dem Text.
    /// </summary>
    private static IElement Uebernehmen(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0];

    private static IElement Entfernen(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindAll(".epos-zweispalten-knopf--entfernen")[0];

    // =================================================================================
    // Feldbestand JE AUSPRAEGUNG
    // =================================================================================

    [Fact]
    public void Prozesswaerme_traegt_die_Felder_ihrer_Karte()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme);
        Assert.Contains("Summe aller ausgew. Prozesse:", cut.Find(".epos-zweispalten-summe").TextContent);
        Satz(cut);   // Stufe 4: die Felder stehen in der Satz-Überlagerung

        Assert.Contains("jährlicher Prozesswärmebedarf:", cut.Markup);
        Assert.Contains("Summe aller ausgew. Prozesse:", cut.Markup);
        Assert.Contains("Ändern des Jahresverbrauchs", cut.Markup);

        // Ein Zahlenfeld ("neuer Wert"), drei gesperrte Textfelder (Name, Typ, Jahresverbrauch;
        // die Summe steht in der Projekt-Kopfleiste) + ein Textbereich.
        Assert.Single(cut.FindAll("input[inputmode=decimal]"));
        Assert.Equal(3, cut.FindAll(".epos-satzueberlagerung-koerper input[type=text][readonly]").Count);
        Assert.Single(cut.FindAll("textarea[readonly]"));

        // Seit Stufe S3.1 zeigen ALLE DREI Auspraegungen dieselben fuenf Spalten
        // (Konzept_Katalogfilter 4.9) - die drei Kataloge sind Drillinge DERSELBEN
        // Tabellenform. Vorher trugen Prozess und Brauchwasser eine Typspalte und der
        // Stromverbraucher keine; der Unterschied war Bestand, keine Fachaussage.
        // Wahl + vier Spalten des Profils + Verwendung: Die Spalte „Auslieferung" ist
        // dem Schloss hinter dem Namen gewichen (Konzept Administrationsdialoge, V10).
        Assert.Equal(6, cut.FindAll(".epos-katalogliste thead th").Count);   // Kästchen + vier Spalten + Stift (Stufe 4); „im Projekt verwendet“ standardmaessig aus (4.10)
        Assert.Contains("KFLT_SP_TYP", cut.Markup);

        // AW3b: „Bearbeiten…" am Katalogsatz traegt den Weg „DB ändern…"; sein Text ist der Titel des Editors.
        Assert.Equal("Prozess in DB ändern", cut.Instance.TitelDbAendern);
        foreach (string t in new[] { "Neu…",
                                     "Löschen", "Simulation…",
                                     "Übernehmen", "OK", "Abbrechen" })
            Assert.NotNull(Knopf(cut, t));

        // Eine Handlung, ein Knopf: Neben „Simulation" steht kein zweiter Knopf, der
        // denselben Ergebnisdialog nur noch einmal oeffnet.
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "monatlicher Verlauf");

        // Die zwei Pfeile: Klartext statt blossem Zeichen (Entscheid #76).
        Assert.Contains("In das Projekt übernehmen", Uebernehmen(cut).TextContent);
        Assert.Contains("Aus dem Projekt entfernen", Entfernen(cut).TextContent);
    }

    /// <summary>
    /// <b>Stufe S3.1 (W14a-E-10):</b> Auch der Stromverbraucher zeigt seit dem
    /// Spaltenmodell die Typspalte. Bis dahin führte seine Liste NUR den Namen —
    /// „der Unterschied ist Bestand und bleibt" hieß es hier; er war es aber nicht:
    /// Die drei Kataloge sind Drillinge derselben Tabellenform, und der Typ ist bei
    /// allen dreien die Profilzuordnung, also der Grund, warum zwei gleich große
    /// Bedarfe verschieden rechnen (Konzept 2.9).
    /// </summary>
    [Fact]
    public void Stromverbraucher_zeigt_dieselben_Spalten_wie_die_beiden_anderen()
    {
        var cut = Aufbauen(BedarfsArt.Stromverbraucher,
                           labelJahresverbrauch: "jährlicher Strombedarf:",
                           labelSumme: "Summe aller ausgewählten Strombedarfe:",
                           btnDbAendern: "Stromverbraucher ändern...",
                           meldungGeloescht: "");
        Satz(cut);

        Assert.Contains("jährlicher Strombedarf:", cut.Markup);
        Assert.Contains("Summe aller ausgewählten Strombedarfe:", cut.Markup);
        // Wahl + vier Spalten des Profils + Verwendung: Die Spalte „Auslieferung" ist
        // dem Schloss hinter dem Namen gewichen (Konzept Administrationsdialoge, V10).
        Assert.Equal(6, cut.FindAll(".epos-katalogliste thead th").Count);   // Kästchen + vier Spalten + Stift (Stufe 4); „im Projekt verwendet“ standardmaessig aus (4.10)
        Assert.Contains("KFLT_SP_TYP", cut.Markup);
        Assert.Equal("Stromverbraucher ändern...", cut.Instance.TitelDbAendern);   // AW3b: Titel des Katalogeditors
    }

    [Fact]
    public void Brauchwasser_traegt_die_Felder_seiner_Karte()
    {
        var cut = Aufbauen(BedarfsArt.Brauchwasser,
                           labelJahresverbrauch: "jährlicher Wärmebedarf:",
                           labelSumme: "Summe Brauchwasserprofile:",
                           btnDbAendern: "Profil in DB ändern",
                           meldungGeloescht: "");
        Satz(cut);

        Assert.Contains("jährlicher Wärmebedarf:", cut.Markup);
        Assert.Contains("Summe Brauchwasserprofile:", cut.Markup);
        // Wahl + vier Spalten des Profils + Verwendung: Die Spalte „Auslieferung" ist
        // dem Schloss hinter dem Namen gewichen (Konzept Administrationsdialoge, V10).
        Assert.Equal(6, cut.FindAll(".epos-katalogliste thead th").Count);   // Kästchen + vier Spalten + Stift (Stufe 4); „im Projekt verwendet“ standardmaessig aus (4.10)
        Assert.Contains("KFLT_SP_TYP", cut.Markup);
        Assert.Equal("Profil in DB ändern", cut.Instance.TitelDbAendern);   // AW3b: Titel des Katalogeditors
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

        KatalogKlick(cut, 0);   // erste Katalogzeile

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

        KatalogKlick(cut, 0);
        Uebernehmen(cut).Click();

        Assert.Single(zeilen);
        Assert.Equal(42.0, zeilen[0].Summe);
        Assert.True(gemeldet);
    }

    [Fact]
    public void Der_Pfeil_nach_rechts_trifft_die_markierte_Zeile()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1), Zeile(2, "Profil B") };
        var cut = Aufbauen(zeilen: zeilen);

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[1].Click();   // zweite Projektzeile (AW3b: die Zeile ist die Wahl)
        Entfernen(cut).Click();

        Assert.Single(zeilen);
        Assert.Equal(1, zeilen[0].IdZ);
    }

    [Fact]
    public void Die_Satzzeile_nennt_die_gewaehlte_Projektzeile()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { Zeile(1, "Wohnhaus 10 Wohnungen") });

        string satz = cut.Find(".epos-zweispalten-satzname").TextContent.Trim();

        Assert.Equal("Wohnhaus 10 Wohnungen", satz);
        Assert.Contains("epos-zweispalten-marke--satz", cut.Markup);
    }

    [Fact]
    public void Die_Satzzeile_nennt_die_gewaehlte_Katalogzeile()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { Zeile(1, "Wohnhaus 10 Wohnungen") });

        KatalogKlick(cut, 0);   // erste Katalogzeile

        Assert.Equal("Profil A", cut.Find(".epos-zweispalten-satzname").TextContent.Trim());
    }

    // =================================================================================
    // Uebernehmen
    // =================================================================================

    [Fact]
    public void Uebernehmen_ohne_Zeile_meldet()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile>());

        // Stufe 4: Ohne Projektzeile gibt es weder „Bearbeiten…" noch die Verbrauchsfelder.
        Assert.True(cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-knopf--bearbeiten-projekt"));
        KatalogKlick(cut, 0);
        Satz(cut);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Übernehmen");
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-koerper input[inputmode=decimal]"));
    }

    [Fact]
    public void Uebernehmen_schreibt_den_Wert_in_die_Zeile()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 1) };
        var cut = Aufbauen(zeilen: zeilen);

        Feld(cut, "input[inputmode=decimal]").Input("33,5");
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
        Feld(cut, "input[inputmode=decimal]").Input("-1");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal("Bitte den Jahresverbrauch als Zahl in MWh eingeben, z. B. 12,5.",
                     cut.Instance.Meldung);
    }

    // =================================================================================
    // Die Einheitenwahl (Anwenderentscheid W9-O-3 vom 04.09.2026)
    // =================================================================================

    private static IElement Einheitenfeld(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        Satz(cut);
        return cut.FindAll(".epos-satzueberlagerung-koerper select").First();
    }

    [Fact]
    public void Die_Vorgabe_ist_MWh()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) });
        Satz(cut);

        Assert.Same(Energieeinheit.MWh, cut.Instance.Anzeigeeinheit);
        Assert.Equal("12,50", cut.Instance.Jahresverbrauch);
        Assert.Contains("Einheit:", cut.Markup);

        // Die Einheit hinter dem Eingabefeld und die beiden Wahlmoeglichkeiten.
        var wahl = Einheitenfeld(cut);
        Assert.Equal(new[] { "MWh", "kWh" },
                     wahl.QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray());
        Assert.Contains("MWh", cut.Find(".epos-einheit").TextContent);
    }

    /// <summary>
    /// Umschalten auf kWh nimmt Zahl UND Einheitentext mit — die Summe der Projektzeile
    /// liegt in MWh, angezeigt wird sie mal 1 000.
    /// </summary>
    [Fact]
    public void Umschalten_auf_kWh_aendert_Zahl_und_Einheitentext()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) });

        Einheitenfeld(cut).Change("1");

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
        Assert.Equal("12500", cut.Instance.Jahresverbrauch);
        Assert.Contains("kWh", cut.Find(".epos-einheit").TextContent);

        Einheitenfeld(cut).Change("0");
        Assert.Same(Energieeinheit.MWh, cut.Instance.Anzeigeeinheit);
        Assert.Equal("12,50", cut.Instance.Jahresverbrauch);
    }

    [Fact]
    public void Die_Wahl_wird_beim_Aendern_gemeldet()
    {
        Energieeinheit? gemerkt = null;
        var cut = Aufbauen(einheitGewaehlt: e => gemerkt = e);

        Einheitenfeld(cut).Change("1");
        Assert.Same(Energieeinheit.KWh, gemerkt);

        Einheitenfeld(cut).Change("0");
        Assert.Same(Energieeinheit.MWh, gemerkt);
    }

    [Fact]
    public void Der_Dialog_oeffnet_mit_der_gemerkten_Einheit()
    {
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) },
                           einheit: Energieeinheit.KWh);

        Assert.Same(Energieeinheit.KWh, cut.Instance.Anzeigeeinheit);
        Assert.Equal("12500", cut.Instance.Jahresverbrauch);
    }

    /// <summary>
    /// Der SPEICHERWEG bleibt MWh. Bei der Vorgabe MWh geht der eingegebene Wert
    /// bitgleich in die Zeile (Fall <c>Uebernehmen_schreibt_den_Wert_in_die_Zeile</c>);
    /// in kWh wird er vor dem Schreiben durch 1 000 geteilt.
    /// </summary>
    [Fact]
    public void Uebernehmen_in_kWh_schreibt_MWh_in_die_Zeile()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 1) };
        var cut = Aufbauen(zeilen: zeilen, einheit: Energieeinheit.KWh);

        Feld(cut, "input[inputmode=decimal]").Input("33500");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(33.5, zeilen[0].Summe, 10);
        Assert.Equal("33500", cut.Instance.Jahresverbrauch);
    }

    /// <summary>
    /// Ein eingetippter Wert steht in der ALTEN Einheit — beim Umschalten wandert er
    /// mit, statt still um den Faktor 1 000 umgedeutet zu werden.
    /// </summary>
    [Fact]
    public void Der_eingegebene_Wert_wandert_beim_Umschalten_mit()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 1) };
        var cut = Aufbauen(zeilen: zeilen);

        Feld(cut, "input[inputmode=decimal]").Input("12,5");
        Einheitenfeld(cut).Change("1");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal(12.5, zeilen[0].Summe, 10);
    }

    [Fact]
    public void Ein_negativer_Wert_meldet_in_kWh_die_kWh()
    {
        var cut = Aufbauen(einheit: Energieeinheit.KWh);

        Feld(cut, "input[inputmode=decimal]").Input("-1");
        Knopf(cut, "Übernehmen").Click();

        Assert.Equal("Bitte den Jahresverbrauch als Zahl in kWh eingeben, z. B. 12,5.",
                     cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Gesamtsumme_folgt_der_Wahl()
    {
        var zeilen = new List<BedarfsProfilZeile>
        {
            Zeile(1, "Profil A", 12.5), Zeile(2, "Profil B", 7.5)
        };
        var cut = Aufbauen(zeilen: zeilen);

        Assert.Contains("20,00", cut.Markup);

        Einheitenfeld(cut).Change("1");
        Assert.Contains("20000", cut.Markup);
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

        Knopf(cut, "Simulation…").Click();

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

        Knopf(cut, "Simulation…").Click();

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

        Knopf(cut, "Simulation…").Click();

        Assert.Single(uebergeben);
        Assert.Equal("Profil A", uebergeben[0]);
    }

    /// <summary>
    /// „Simulation" rechnet UND zeigt: Jeder Klick rechnet neu und öffnet das Ergebnis.
    /// </summary>
    [Fact]
    public void Simulation_rechnet_bei_jedem_Klick_und_oeffnet_das_Ergebnis()
    {
        int laeufe = 0;
        var cut = Aufbauen(simulieren: _ => { laeufe++; return new Dictionary<string, object>(); });

        Knopf(cut, "Simulation…").Click();
        Assert.True(cut.Instance.ErgebnisOffen);

        Knopf(cut, "Simulation…").Click();
        Assert.Equal(2, laeufe);
    }

    /// <summary>
    /// Der mit „Übernehmen" gesetzte Jahresverbrauch steht in der Zeile, BEVOR gerechnet
    /// wird — die Hülle rechnet mit den Zeilen des Dialogs, nicht mit dem gespeicherten
    /// Stand (150 000 kWh ergeben 150 MWh, nicht die Katalogsumme).
    /// </summary>
    [Fact]
    public void Simulation_sieht_den_uebernommenen_Jahresverbrauch_in_der_Zeile()
    {
        var zeile = Zeile(1, "Beckenwasseraufheizung", 365.0);
        double gesehen = -1;
        var cut = Aufbauen(zeilen: new List<BedarfsProfilZeile> { zeile },
                           einheit: Energieeinheit.KWh,
                           simulieren: _ => { gesehen = zeile.Summe; return new Dictionary<string, object>(); });

        Feld(cut, "input[inputmode=decimal]").Input("150000");
        Knopf(cut, "Übernehmen").Click();
        Knopf(cut, "Simulation…").Click();

        Assert.Equal(150.0, gesehen, 9);
    }

    /// <summary>
    /// Im ASSISTENTEN gilt der eingegebene Wert mit dem Lauf als übernommen: Er steht in
    /// der Zeile, mit der die Vorschau rechnet — auch bei ungespeichertem Projekt, dann
    /// ohne Schreibweg und ohne Hinweis. Die Vorschau rechnet mit dem Dialogstand.
    /// </summary>
    [Fact]
    public void Im_Assistenten_rechnet_die_Vorschau_mit_dem_eingegebenen_Wert()
    {
        bool gesichert = false;
        var zeile = Zeile(1);
        double gesehen = -1;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, wizard: true,
                           zeilen: new List<BedarfsProfilZeile> { zeile },
                           projektGespeichert: () => false,
                           summeSichern: (_, _) => gesichert = true,
                           simulieren: _ => { gesehen = zeile.Summe; return new Dictionary<string, object>(); });

        Feld(cut, "input[inputmode=decimal]").Input("10");
        Knopf(cut, "Simulation…").Click();

        Assert.False(gesichert);
        Assert.Equal(10.0, gesehen, 9);
        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>Der Schreibweg des Assistenten bekommt MWh, auch wenn kWh gewählt ist.</summary>
    [Fact]
    public void Im_Assistenten_sichert_der_Lauf_in_MWh()
    {
        double gesichert = -1;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, wizard: true, einheit: Energieeinheit.KWh,
                           projektGespeichert: () => true,
                           summeSichern: (_, w) => gesichert = w);

        Feld(cut, "input[inputmode=decimal]").Input("150000");
        Knopf(cut, "Simulation…").Click();

        Assert.Equal(150.0, gesichert, 9);
    }

    [Fact]
    public void Bei_gespeichertem_Projekt_wird_die_Summe_vor_dem_Lauf_gesichert()
    {
        double gesichert = -1;
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, wizard: true,
                           projektGespeichert: () => true,
                           summeSichern: (_, w) => gesichert = w);

        Feld(cut, "input[inputmode=decimal]").Input("10");
        Knopf(cut, "Simulation…").Click();

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

        Feld(cut, "input[inputmode=decimal]").Input("8");
        Knopf(cut, "Simulation…").Click();

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

        KatalogKlick(cut, 0);
        cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--db-aendern").Click();   // AW3b: „Bearbeiten…" am Katalogsatz

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

        Knopf(cut, "Neu…").Click();
        Assert.False(cut.Instance.TypStammOffen);

        cut.Find("[role=dialog] input[type=text]").Input("Neues Profil");
        cut.FindAll("[role=dialog] button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal("Neues Profil", uebergeben);
        Assert.True(cut.Instance.TypStammOffen);
    }

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Typ_aendern_Knopf()
    {
        Assert.DoesNotContain("Typ ändern…", Aufbauen().Markup);
        Assert.Contains("Typ ändern…",
                        Aufbauen(typProfilGaben: () => new Dictionary<string, object>()).Markup);
    }

    [Fact]
    public void Loeschen_fragt_nach()
    {
        string geloescht = "";
        var cut = Aufbauen(katalogLoeschen: n => { geloescht = n; return true; });

        KatalogKlick(cut, 0);
        Knopf(cut, "Löschen").Click();

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

        Knopf(cut, "Simulation…").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(gerufen);
    }

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc: Abbrechen ohne zu speichern.</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titel: "");

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Die beiden Hilfeknoepfe bleiben - sie haengen nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    /// <summary>
    /// Die drei Ueberlagerungen (Ergebnis, Stammkopf, Profil) trugen bislang kein ✕
    /// (<c>Schliessbar="false"</c>) — jetzt schließt ihr Kreuz wie „Abbrechen": Die
    /// Ueberlagerung geht wieder zu.
    /// </summary>
    [Fact]
    public void Ueberlagerungskreuz_schliesst_den_Ergebnisdialog()
    {
        var cut = Aufbauen();

        Knopf(cut, "Simulation…").Click();
        Assert.True(cut.Instance.ErgebnisOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.ErgebnisOffen);
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Ergebnis-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>BedarfErgebnisDialog</c> (<c>TitelAnzeigen="false"</c>) keins von
    /// beidem — sonst stünden zwei Kreuze und zwei Titel übereinander.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Ergebnis_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen();

        Knopf(cut, "Simulation…").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Stammkopf-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>TypStammDialog</c> (<c>TitelText=""</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Stammkopf_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(typStammGaben: (_, _, _, _) => new Dictionary<string, object>());

        KatalogKlick(cut, 0);
        cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--db-aendern").Click();   // AW3b: „Bearbeiten…" am Katalogsatz
        Assert.True(cut.Instance.TypStammOffen);

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// „Das Kreuz steht beim Titel": Die Profil-Überlagerung trägt Titel und ✕, der
    /// eingebettete <c>TypProfilDialog</c> (<c>TitelAnzeigen="false"</c>) keins von beidem.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Typprofil_zeigt_nur_ein_Kreuz()
    {
        var cut = Aufbauen(typProfilGaben: () => new Dictionary<string, object>());

        Knopf(cut, "Typ ändern…").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    // =================================================================================
    // Die Anordnung - Anwenderentscheid #76 vom 05.09.2026
    // =================================================================================

    /// <summary>
    /// Der Anwender hat nach der Windows-Abnahme entschieden, dass auch dieser Dialog
    /// dem BHKW-PLAN-Schema folgt: Projektliste LINKS, Katalog RECHTS, die zwei
    /// Pfeilknöpfe in einer schmalen Mittelspalte dazwischen. Bis dahin standen die
    /// beiden Listen untereinander und die Knöpfe unter der Projektliste.
    /// </summary>
    [Fact]
    public void Projektliste_links_Katalog_rechts_Pfeile_dazwischen()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme);

        var bereiche = cut.FindAll(".epos-zweispalten > *")
                          .Select(e => e.ClassName ?? "").ToList();

        Assert.True(bereiche.Count >= 3);
        Assert.Contains("epos-zweispalten-spalte--oben", bereiche[0]);
        Assert.Contains("epos-zweispalten-trenner", bereiche[1]);
        Assert.Contains("epos-zweispalten-spalte--unten", bereiche[2]);

        // Beide Listen stehen weiterhin in ihrem Rahmen (Befund W9-B-2).
        Assert.Equal(2, cut.FindAll(".epos-zweispalten-spalte .epos-raster-huelle").Count);
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Infoblock und der Block "Jahresverbrauch" stehen im Formularraster. Der Uebernehmen-Knopf ist kein Feld und bleibt darunter.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Infoblock_und_Jahresverbrauch_stehen_im_Formularraster()
    {
        var cut = Aufbauen(BedarfsArt.Prozesswaerme);
        Satz(cut);

        Assert.Equal(2, cut.FindAll(".epos-formularraster").Count);
        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld"));

        // Der neue Wert ist ein Zahlenfeld, also ein KURZES Feld.
        Assert.NotEmpty(cut.FindAll(".epos-formularraster .epos-feld--kurz"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>BedarfsProfileKiSicht</c>: Die Brücke liest den Infoblock des
    /// markierten Profils und setzt den neuen Jahresverbrauch; <c>dialog_speichern</c>
    /// nimmt den Weg des Knopfes „Übernehmen".
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_uebernimmt_den_Verbrauch()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1) };
        var cut = Aufbauen(zeilen: zeilen);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BEDARFSPROFILE));

        WindowsFormsApplication1.KiFeldzugang profil =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "profil");
        Assert.NotNull(profil);
        Assert.False(profil.Setzbar);

        WindowsFormsApplication1.KiFeldzugang wert =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "neuer_wert");
        Assert.True(wert.Setzbar);

        wert.Setzen(30.0);
        cut.Render();
        Assert.Equal(30.0, wert.Lesen());

        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.BEDARFSPROFILE);
        Assert.NotNull(haken.Speichern);
        Assert.True(haken.Speichern!().Result.Erfolg);

        // Die Zeile trägt den neuen Verbrauch - in MWh, der Einheit des Speicherwegs.
        Assert.Equal(30.0, zeilen[0].Summe);
    }

    /// <summary>
    /// <b>Die Ausprägung ist LESBAR</b> (Welle KI‑F3): Dieselbe Komponente pflegt
    /// Prozesswärme, Stromverbraucher und Brauchwasser; der Assistent muss nicht
    /// raten, welche offen ist.
    /// </summary>
    [Fact]
    public void Der_Assistent_liest_die_Bedarfsart_der_Maske()
    {
        Aufbauen(art: BedarfsArt.Brauchwasser);

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "bedarfsart");
        Assert.NotNull(zugang);
        Assert.False(zugang.Setzbar);
        Assert.Equal(BedarfsArt.Brauchwasser.ToString(), zugang.Lesen());
    }

    // =================================================================================
    // Zapfprofil (Umsetzungskonzept Zapfprofilgenerator 5.2, 5.7; ZU4, ZU6, ZU10)
    // =================================================================================

    /// <summary>Der Parametersatz eines Zapfprofil-Dialogs mit einer Zone — erfunden.</summary>
    private static IReadOnlyDictionary<string, object> ZapfprofilSatz()
    {
        var eingabe = new ZapfprofilEingabeDaten { Weg = ZapfprofilWeg.Bestand };
        eingabe.Zonen.Add(new ZapfprofilZoneDaten { Id = 5, Name = "Zone 1", IdNutzungsart = 1, Bezugsmenge = 10 });
        return new Dictionary<string, object>
        {
            ["Daten"] = new ZapfprofilDaten { IdProjekt = 7, Eingabe = eingabe, Verfuegbar = true },
            ["Texte"] = new ZapfprofilTexte(),
            ["Pruefen"] = new Func<ZapfprofilEingabeDaten, IReadOnlyList<ZapfprofilMeldung>>(_ => Array.Empty<ZapfprofilMeldung>()),
            ["EntprellungMs"] = 0
        };
    }

    private IRenderedComponent<BedarfsProfileDialog> AufbauenZapfprofil(
        BedarfsArt art = BedarfsArt.Brauchwasser,
        Func<IReadOnlyDictionary<string, object>>? gaben = null,
        Action<ZapfprofilErgebnisDaten>? uebernommen = null,
        string sperrgrund = "",
        ZapfprofilWeg weg = ZapfprofilWeg.Bestand,
        Action<ZapfprofilWeg>? wegGesetzt = null,
        int zonen = 0,
        List<BedarfsProfilZeile>? zeilen = null,
        Func<IReadOnlyList<string>, IReadOnlyDictionary<string, object>?>? simulieren = null,
        Func<string>? simulationMeldung = null,
        Action<bool>? geschlossen = null,
        Func<string>? speichern = null)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.TitelText, "Brauchwasserwärme")
            .Add(x => x.KopfbandText, "Brauchwasserwärme")
            .Add(x => x.Zeilen, zeilen ?? new List<BedarfsProfilZeile> { Zeile(1) })
            .Add(x => x.Katalogzeilen, Katalogzeilen)
            .Add(x => x.Katalogprofil, Profil(art))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung " + n, "Typ 1"))
            .Add(x => x.Jahressumme, _ => 42.0)
            .Add(x => x.Simulieren, simulieren ?? (_ => new Dictionary<string, object>()))
            .Add(x => x.ZapfprofilGaben, gaben)
            .Add(x => x.ZapfprofilUebernommen, uebernommen)
            .Add(x => x.ZapfprofilSperrgrund, sperrgrund)
            .Add(x => x.RechenwegBrauchwasser, weg)
            .Add(x => x.RechenwegGesetzt, wegGesetzt)
            .Add(x => x.ZapfprofilZonen, zonen)
            .Add(x => x.SimulationMeldung, simulationMeldung)
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement? ZapfprofilKnopf(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == "Zapfprofil erzeugen…");

    private static IElement Rechenweg(IRenderedComponent<BedarfsProfileDialog> cut, string text)
        => cut.FindAll("fieldset[aria-label='Rechenweg Brauchwasser'] label.epos-option")
              .First(l => l.TextContent.Trim() == text).QuerySelector("input")!;

    /// <summary>ZU6: Der Knopf steht im Aktionsschlitz der Fußleiste — vor Abbrechen und OK, nicht in der Leiste „Simulation".</summary>
    [Fact]
    public void Der_Zapfprofil_Knopf_steht_im_Aktionsschlitz_der_Fussleiste()
    {
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, wegGesetzt: _ => { });

        IElement fuss = cut.FindAll(".epos-leiste").Last();
        string[] knoepfe = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Zapfprofil erzeugen…", "Abbrechen", "OK" }, knoepfe);

        // Stufe 4: „Simulation…" steht in der Projekt-Kopfleiste, nicht in der Fussleiste.
        IElement simulation = cut.Find(".epos-knopf--simulation");
        Assert.DoesNotContain("Zapfprofil erzeugen…", simulation.ParentElement!.TextContent);
    }

    /// <summary>Kein Delegat, kein Knopf — und nur bei Brauchwasser.</summary>
    [Fact]
    public void Der_Knopf_gibt_es_nur_mit_Delegat_und_nur_bei_Brauchwasser()
    {
        Assert.Null(ZapfprofilKnopf(AufbauenZapfprofil()));
        Assert.Null(ZapfprofilKnopf(AufbauenZapfprofil(BedarfsArt.Prozesswaerme, gaben: ZapfprofilSatz)));
        Assert.Null(ZapfprofilKnopf(Aufbauen(BedarfsArt.Brauchwasser)));
        Assert.NotNull(ZapfprofilKnopf(AufbauenZapfprofil(gaben: ZapfprofilSatz)));
    }

    /// <summary>ZU10: Ohne gespeichertes Projekt reicht die Hülle nur den Grund — der Knopf steht weich gesperrt da und nennt ihn.</summary>
    [Fact]
    public void Ohne_gespeichertes_Projekt_ist_der_Knopf_benannt_gesperrt()
    {
        var cut = AufbauenZapfprofil(sperrgrund: "Das Zapfprofil braucht ein gespeichertes Projekt.");

        IElement knopf = ZapfprofilKnopf(cut)!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal("Das Zapfprofil braucht ein gespeichertes Projekt.", knopf.GetAttribute("title"));

        knopf.Click();

        Assert.False(cut.Instance.ZapfprofilOffen);
        Assert.Equal("Das Zapfprofil braucht ein gespeichertes Projekt.", cut.Instance.Meldung);
        Assert.Empty(cut.FindAll("fieldset[aria-label='Rechenweg Brauchwasser']"));
    }

    /// <summary>ZU4: Das OK des Zapfprofils übergibt den Stand und stellt die Optionsgruppe auf Zapfprofil.</summary>
    [Fact]
    public void Das_OK_des_Zapfprofils_uebergibt_den_Stand_und_setzt_den_Weg()
    {
        ZapfprofilErgebnisDaten? uebernommen = null;
        ZapfprofilWeg? gesetzt = null;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, uebernommen: e => uebernommen = e,
                                     wegGesetzt: w => gesetzt = w);

        Assert.True(Rechenweg(cut, "Bestandsprofile").HasAttribute("checked"));
        ZapfprofilKnopf(cut)!.Click();
        Assert.True(cut.Instance.ZapfprofilOffen);

        // N35: Das Blatt trägt Rückknopf und Titel, kein eigenes Kreuz — der eingebettete
        // ZapfprofilDialog zeigt deshalb keinen eigenen h1, und der Haupt-Inhalt (Katalog,
        // "Simulation" ...) ist vom Baum verschwunden.
        Assert.Equal("‹ Brauchwasserwärme", cut.Find(".epos-blatt-zurueck").TextContent);
        Assert.Empty(cut.FindAll(".epos-blatt-inhalt h1.epos-dialog-titel"));
        Assert.Equal("Brauchwasser-Zapfprofil", cut.Find(".epos-blatt-titel").TextContent);
        Assert.DoesNotContain("Simulation", cut.FindAll(".epos-leiste button").Select(b => b.TextContent));

        cut.FindAll(".epos-blatt-inhalt .epos-leiste .epos-knopf--primaer").Last().Click();

        Assert.False(cut.Instance.ZapfprofilOffen);
        Assert.NotNull(uebernommen);
        Assert.Equal(ZapfprofilWeg.Generator, uebernommen!.Weg);
        Assert.Equal("Zone 1", Assert.Single(uebernommen.Eingabe.Zonen).Name);
        Assert.Equal(ZapfprofilWeg.Generator, cut.Instance.Rechenweg);
        Assert.True(Rechenweg(cut, "Zapfprofil").HasAttribute("checked"));
        Assert.Contains("Die Bestandsprofile rechnen nicht mit", cut.Markup);
        Assert.Null(gesetzt);   // den Weg des OK trägt der übernommene Stand, nicht die Optionsgruppe
    }

    /// <summary>
    /// N35: Abbrechen im Zapfprofil lässt Stand und Weg stehen; Esc führt nur vom Blatt zurück
    /// (kein Abbrechen des Wirtsdialogs — derselbe Grundsatz wie beim Kreuz der abgelösten
    /// Überlagerung).
    /// </summary>
    [Fact]
    public void Abbrechen_im_Zapfprofil_laesst_alles_stehen_und_Esc_fuehrt_nur_vom_Blatt_zurueck()
    {
        bool uebernommen = false;
        bool geschlossen = false;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, uebernommen: _ => uebernommen = true,
                                     wegGesetzt: _ => { }, geschlossen: _ => geschlossen = true);

        ZapfprofilKnopf(cut)!.Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(geschlossen);

        cut.FindAll(".epos-blatt-inhalt button").First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.False(cut.Instance.ZapfprofilOffen);
        Assert.False(uebernommen);
        Assert.False(geschlossen);
        Assert.Equal(ZapfprofilWeg.Bestand, cut.Instance.Rechenweg);

        // Esc auf dem Blatt führt nur dorthin zurück, es schließt nicht den Wirtsdialog.
        ZapfprofilKnopf(cut)!.Click();
        cut.Find(".epos-blatt").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.ZapfprofilOffen);
        Assert.False(uebernommen);
        Assert.False(geschlossen);
    }

    /// <summary>N35: Der Rückknopf „‹ {Wirtstitel}" führt ebenso zurück wie Esc, ohne zu übernehmen.</summary>
    [Fact]
    public void Der_Rueckknopf_fuehrt_vom_Blatt_zurueck_ohne_zu_uebernehmen()
    {
        bool uebernommen = false;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, uebernommen: _ => uebernommen = true,
                                     wegGesetzt: _ => { });

        ZapfprofilKnopf(cut)!.Click();
        Assert.True(cut.Instance.ZapfprofilOffen);

        cut.Find(".epos-blatt-zurueck").Click();

        Assert.False(cut.Instance.ZapfprofilOffen);
        Assert.False(uebernommen);
        Assert.Equal(ZapfprofilWeg.Bestand, cut.Instance.Rechenweg);
        // Der Haupt-Inhalt (Katalog, "Simulation" ...) steht wieder.
        Assert.NotNull(ZapfprofilKnopf(cut));
    }

    /// <summary>
    /// N35: Hin und zurück bleibt der Arbeitsstand des Wirts — die übernommene Summe, der
    /// angefangene Wert im Eingabefeld —, und es wird nichts geschrieben; geschrieben wird
    /// erst mit dem OK des Wirts, auch nach einem Besuch auf dem Blatt.
    /// </summary>
    [Fact]
    public void Der_Arbeitsstand_des_Wirts_uebersteht_den_Blattwechsel_und_das_OK_schreibt_weiter()
    {
        int geschrieben = 0;
        bool? geschlossen = null;
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1) };
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, wegGesetzt: _ => { }, zeilen: zeilen,
                                     geschlossen: b => geschlossen = b,
                                     speichern: () => { geschrieben++; return ""; });

        Feld(cut, "input[inputmode=decimal]").Input("33,5");
        Knopf(cut, "Übernehmen").Click();
        Feld(cut, "input[inputmode=decimal]").Input("44,5");   // angefangen, nicht übernommen

        ZapfprofilKnopf(cut)!.Click();
        cut.Find(".epos-blatt-zurueck").Click();

        Assert.Equal(33.5, zeilen[0].Summe, 6);
        Assert.Equal("44,5", Feld(cut, "input[inputmode=decimal]").GetAttribute("value"));
        Assert.Same(zeilen[0], cut.Instance.Gewaehlt);
        Assert.Equal(0, geschrieben);

        // Auch das OK des Blattes schreibt nicht - es übernimmt nur in den Arbeitsstand.
        ZapfprofilKnopf(cut)!.Click();
        cut.FindAll(".epos-blatt-inhalt .epos-leiste .epos-knopf--primaer").Last().Click();
        Assert.False(cut.Instance.ZapfprofilOffen);
        Assert.Equal(0, geschrieben);
        Assert.Null(geschlossen);

        Knopf(cut, "OK").Click();

        Assert.Equal(1, geschrieben);
        Assert.True(geschlossen);
    }

    /// <summary>
    /// N35: Nur EINE Ebene zeigt ihre Hilfe. Auf dem Blatt stehen die zwei Hilfepillen des
    /// Zapfprofils und keine des Wirts (sein Kopf ist vom Baum), zurück stehen die zwei des
    /// Wirts — und in beiden Fällen gibt es keine Überlagerung, die eine dritte Ebene machte.
    /// </summary>
    [Fact]
    public void Auf_dem_Blatt_steht_nur_die_Hilfe_des_Blattes_und_keine_Ueberlagerung()
    {
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, wegGesetzt: _ => { });

        Assert.Equal(2, cut.FindAll(".epos-hilfepille").Count);
        Assert.Empty(cut.FindAll(".epos-blatt-inhalt"));

        ZapfprofilKnopf(cut)!.Click();

        Assert.Equal(2, cut.FindAll(".epos-hilfepille").Count);
        Assert.Equal(2, cut.FindAll(".epos-blatt-inhalt .epos-hilfepille").Count);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Single(cut.FindAll(".epos-blatt"));
        Assert.Contains("epos-blatt--breit", cut.Find(".epos-blatt").ClassList);

        cut.Find(".epos-blatt-zurueck").Click();

        Assert.Equal(2, cut.FindAll(".epos-hilfepille").Count);
        Assert.Empty(cut.FindAll(".epos-blatt-inhalt"));
    }

    /// <summary>ZU4: Zurückschalten geht über die Optionsgruppe und meldet den Weg an die Hülle (die Zonen bleiben dort).</summary>
    [Fact]
    public void Die_Optionsgruppe_setzt_den_Weg_und_schaltet_zurueck()
    {
        var gesetzt = new List<ZapfprofilWeg>();
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, weg: ZapfprofilWeg.Generator, zonen: 1,
                                     wegGesetzt: gesetzt.Add);

        Assert.True(Rechenweg(cut, "Zapfprofil").HasAttribute("checked"));
        Assert.Contains("rechnet den Zapfprofilweg", cut.Markup);
        Assert.Contains("Brauchwasser (Trinkwarmwasser)", cut.Markup);

        Rechenweg(cut, "Bestandsprofile").Change("0");
        Assert.Equal(new[] { ZapfprofilWeg.Bestand }, gesetzt);
        Assert.Equal(ZapfprofilWeg.Bestand, cut.Instance.Rechenweg);
        Assert.DoesNotContain("rechnet den Zapfprofilweg", cut.Markup);

        Rechenweg(cut, "Zapfprofil").Change("1");
        Assert.Equal(new[] { ZapfprofilWeg.Bestand, ZapfprofilWeg.Generator }, gesetzt);
    }

    /// <summary>Ohne Zone gibt es nichts zu rechnen: „Zapfprofil" ist weich gesperrt und nennt den Weg dorthin.</summary>
    [Fact]
    public void Ohne_Zone_ist_der_Zapfprofilweg_benannt_gesperrt()
    {
        bool gesetzt = false;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, zonen: 0, wegGesetzt: _ => gesetzt = true);

        IElement option = Rechenweg(cut, "Zapfprofil");
        Assert.Equal("true", option.GetAttribute("aria-disabled"));
        option.Change("1");

        Assert.False(gesetzt);
        Assert.Equal(ZapfprofilWeg.Bestand, cut.Instance.Rechenweg);
        Assert.StartsWith("Es gibt noch kein Zapfprofil", cut.Instance.Meldung);
    }

    /// <summary>
    /// Die Leiste „monatlicher Verlauf" rechnet auf dem Zapfprofilweg ohne gewähltes
    /// Bestandsprofil und zeigt die Meldung des Laufs (eine Zone, die 0 trägt) nach dem
    /// Bestandsmuster als Banner.
    /// </summary>
    [Fact]
    public void Die_Leiste_rechnet_den_Zapfprofilweg_und_zeigt_seine_Meldung()
    {
        IReadOnlyList<string>? namen = null;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, weg: ZapfprofilWeg.Generator, zonen: 1,
                                     wegGesetzt: _ => { }, zeilen: new List<BedarfsProfilZeile>(),
                                     simulieren: n => { namen = n; return new Dictionary<string, object>(); },
                                     simulationMeldung: () => "Zone „Zone 1“ trägt 0: Bezugsmenge fehlt.");

        Knopf(cut, "Simulation…").Click();

        Assert.NotNull(namen);
        Assert.Empty(namen!);
        Assert.True(cut.Instance.ErgebnisOffen);
        Assert.Equal("Zone „Zone 1“ trägt 0: Bezugsmenge fehlt.", cut.Instance.Meldung);
    }

    /// <summary>Ein Abbruch des Zapfprofilwegs (kein Ergebnis) nennt seinen Grund, statt still nichts zu tun.</summary>
    [Fact]
    public void Ein_Abbruch_des_Zapfprofilwegs_nennt_den_Grund()
    {
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, weg: ZapfprofilWeg.Generator, zonen: 1,
                                     wegGesetzt: _ => { }, simulieren: _ => null,
                                     simulationMeldung: () => "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau.");

        Knopf(cut, "Simulation…").Click();

        Assert.False(cut.Instance.ErgebnisOffen);
        Assert.Contains("keine Klimaregion", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// 5.2 und die OK-Regel (EPOS.UI/CLAUDE.md): Das OK schreibt über den Schreibweg der Hülle,
    /// BEVOR der Dialog schließt. Lehnt er ab, steht sein Grund als Banner da, der Dialog bleibt
    /// offen, Zeilen und Weg bleiben stehen; ein zweites OK schreibt erneut und schließt.
    /// </summary>
    [Fact]
    public void Eine_Ablehnung_des_Schreibwegs_haelt_den_Dialog_offen()
    {
        var antworten = new Queue<string>(new[]
        {
            "Das Zapfprofil wurde nicht gespeichert — Zone „Zone 1“: Die Nutzungsart steht nicht im Katalog.",
            ""
        });
        int geschrieben = 0;
        bool? geschlossen = null;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, weg: ZapfprofilWeg.Generator, zonen: 1,
                                     wegGesetzt: _ => { }, geschlossen: b => geschlossen = b,
                                     speichern: () => { geschrieben++; return antworten.Dequeue(); });

        Knopf(cut, "OK").Click();

        Assert.Equal(1, geschrieben);
        Assert.Null(geschlossen);
        Assert.Contains("Die Nutzungsart steht nicht im Katalog", cut.Find(".epos-warnbanner").TextContent);
        Assert.Single(cut.Instance.Zeilen);
        Assert.Equal(ZapfprofilWeg.Generator, cut.Instance.Rechenweg);

        Knopf(cut, "OK").Click();

        Assert.Equal(2, geschrieben);
        Assert.True(geschlossen);
    }

    /// <summary>Abbrechen fragt den Schreibweg nicht.</summary>
    [Fact]
    public void Abbrechen_schreibt_nicht()
    {
        int geschrieben = 0;
        bool? geschlossen = null;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, wegGesetzt: _ => { },
                                     geschlossen: b => geschlossen = b,
                                     speichern: () => { geschrieben++; return ""; });

        Knopf(cut, "Abbrechen").Click();

        Assert.Equal(0, geschrieben);
        Assert.False(geschlossen);
    }

    // =================================================================================
    // Die Zapfprofil-Weiche beim Hilfe-Assistenten (Welle #458, Stufe 3a)
    // =================================================================================

    /// <summary>
    /// <b>Die Optionsgruppe „Rechenweg Brauchwasser" ist ein Feld der Maske</b>: Der
    /// Assistent liest sie und stellt sie auf dem Weg des Klicks um — die Hülle erfährt den
    /// Weg, die Optionsgruppe zeigt ihn.
    /// </summary>
    [Fact]
    public void Der_Assistent_stellt_den_Rechenweg_Brauchwasser_um()
    {
        var gesetzt = new List<ZapfprofilWeg>();
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, zonen: 1, wegGesetzt: gesetzt.Add);

        WindowsFormsApplication1.KiFeldzugang weg =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "rechenweg");
        Assert.NotNull(weg);
        Assert.True(weg.Setzbar);
        Assert.True(weg.IstWahl);
        Assert.Equal((int)ZapfprofilWeg.Bestand, weg.Lesen());
        Assert.Equal(new[] { "Bestandsprofile", "Zapfprofil" }, weg.Wahleintraege().Select(e => e.Text).ToArray());

        KiFeldumsetzung u = KiFeldwandler.Wandle(weg, "Zapfprofil");
        Assert.True(u.Ok, u.Grund);
        weg.Setzen(u.Wert);
        cut.Render();

        Assert.Equal(new[] { ZapfprofilWeg.Generator }, gesetzt);
        Assert.Equal(ZapfprofilWeg.Generator, cut.Instance.Rechenweg);
        Assert.True(Rechenweg(cut, "Zapfprofil").HasAttribute("checked"));
        Assert.Equal((int)ZapfprofilWeg.Generator, weg.Lesen());
    }

    /// <summary>
    /// „Zapfprofil" ohne Zone lehnt der Assistent ab wie der weich gesperrte Knopf — mit
    /// dessen Grund; der Weg bleibt stehen.
    /// </summary>
    [Fact]
    public void Ohne_Zone_lehnt_der_Assistent_den_Zapfprofilweg_benannt_ab()
    {
        bool gesetzt = false;
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, zonen: 0, wegGesetzt: _ => gesetzt = true);

        WindowsFormsApplication1.KiFeldzugang weg =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "rechenweg");
        var ex = Assert.Throws<InvalidOperationException>(() => weg.Setzen((int)ZapfprofilWeg.Generator));

        Assert.Equal(new ZapfprofilEinstiegTexte().HinweisOhneZonen, ex.Message);
        Assert.False(gesetzt);
        Assert.Equal(ZapfprofilWeg.Bestand, cut.Instance.Rechenweg);
    }

    /// <summary>
    /// Außerhalb des Brauchwassers steht keine Optionsgruppe: Das Feld ist leer, und das
    /// Setzen nennt den Grund.
    /// </summary>
    [Fact]
    public void Ausserhalb_des_Brauchwassers_nennt_der_Assistent_den_Grund()
    {
        Aufbauen(BedarfsArt.Prozesswaerme);

        WindowsFormsApplication1.KiFeldzugang weg =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "rechenweg");
        Assert.Null(weg.Lesen());
        var ex = Assert.Throws<InvalidOperationException>(() => weg.Setzen((int)ZapfprofilWeg.Bestand));
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KI_DLG_BPF_RECHENWEG_NUR_BW, ex.Message);
    }

    /// <summary>
    /// <b>Das Zapfprofil-Blatt „Brauchwasser-Zapfprofil" ist eine EIGENE Maske</b> (Welle #458,
    /// Stufe 3a; N35: Blatt statt Überlagerung): Solange es offen steht, meint der Assistent
    /// es; geht es zu, meint er wieder die Bedarfsprofile.
    /// </summary>
    [Fact]
    public void Das_offene_Zapfprofil_ist_die_aktive_Maske_des_Assistenten()
    {
        KiMaskenbruecke.Leeren();   // die aktive Maske ist die zuletzt angemeldete - ohne Reste anderer Fälle
        var cut = AufbauenZapfprofil(gaben: ZapfprofilSatz, wegGesetzt: _ => { });
        Assert.Equal(KiMaskennamen.BEDARFSPROFILE, KiMaskenbruecke.AktiveMaske());

        ZapfprofilKnopf(cut)!.Click();
        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.ZAPFPROFIL));
        Assert.Equal(KiMaskennamen.ZAPFPROFIL, KiMaskenbruecke.AktiveMaske());

        cut.FindAll(".epos-blatt-inhalt button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.ZAPFPROFIL));
        Assert.Equal(KiMaskennamen.BEDARFSPROFILE, KiMaskenbruecke.AktiveMaske());
    }

    /// <summary>Ohne gespeichertes Projekt nennt die Absage den Grund der Hülle (ZU10).</summary>
    [Fact]
    public void Ohne_gespeichertes_Projekt_nennt_der_Assistent_den_Grund_der_Huelle()
    {
        AufbauenZapfprofil(sperrgrund: "Das Zapfprofil braucht ein gespeichertes Projekt.");

        WindowsFormsApplication1.KiFeldzugang weg =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.BEDARFSPROFILE, "rechenweg");
        var ex = Assert.Throws<InvalidOperationException>(() => weg.Setzen((int)ZapfprofilWeg.Generator));
        Assert.Equal("Das Zapfprofil braucht ein gespeichertes Projekt.", ex.Message);
    }

    // =================================================================================
    // Stufe 4 der Katalogauswahl: Kopfleiste, Fussleiste, Stift, Zusammenfassung,
    // Satz-Ueberlagerung, Mehrfachloeschen, Kaeltezweig
    // =================================================================================

    private IRenderedComponent<BedarfsProfileDialog> AufbauenS4(
        List<BedarfsProfilZeile> zeilen, IReadOnlyList<Katalogfilterzeile>? katalog = null,
        BedarfsArt art = BedarfsArt.Prozesswaerme, Func<string, bool>? loeschen = null,
        Func<string, string, string, bool, IReadOnlyDictionary<string, object>>? typStamm = null,
        IReadOnlyList<(int Id, string Text)>? kalender = null, Action? geaendert = null, EPOS.UI.Bausteine.Schlossweg? schloss = null)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Schloss, schloss)
            .Add(x => x.Art, art)
            .Add(x => x.TitelText, "Bedarf")
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => katalog ?? Katalogzeilen())
            .Add(x => x.Katalogprofil, Profil(art))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung " + n, "Typ 1", 6.0, 12.0))
            .Add(x => x.Jahressumme, _ => 42.0)
            .Add(x => x.Aufnehmen, n => new BedarfsProfilZeile { IdZ = 100000, IdStamm = 9, Name = n, Summe = 42.0 })
            .Add(x => x.KatalogLoeschen, loeschen ?? (_ => true))
            .Add(x => x.TypStammGaben, typStamm)
            .Add(x => x.TypProfilGaben, () => new Dictionary<string, object>())
            .Add(x => x.TemperaturPruefen, (v, r) => v is < -50 ? "zu kalt" : null)
            .Add(x => x.Betriebskalender, kalender)
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Einheit, Energieeinheit.MWh));

    [Fact]
    public void S4_Die_Kopfleiste_traegt_Jahressumme_Bearbeiten_und_Simulation()
    {
        var cut = AufbauenS4(new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5), Zeile(2, "Profil B", 7.5) });

        Assert.Contains("20,00", cut.Find(".epos-zweispalten-summe").TextContent);
        Assert.False(cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").HasAttribute("disabled"));
        Assert.Null(cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste").QuerySelector(".epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(cut.Find(".epos-knopf--simulation"));
        // Zwei angekreuzte Zeilen: „Bearbeiten…" wirkt auf genau eine.
        foreach (int i in new[] { 0, 1 })
            cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen")[i].Click();
        Assert.True(cut.Find(".epos-knopf--bearbeiten-projekt").HasAttribute("disabled"));
    }

    [Fact]
    public void S4_Die_Fussleiste_des_Katalogs_steht_im_Baustein_ohne_alte_Leiste()
    {
        var cut = AufbauenS4(new List<BedarfsProfilZeile> { Zeile(1) },
                             typStamm: (_, _, _, _) => new Dictionary<string, object>());

        // AW3b: keine Fussleiste unter dem Katalog - die Knoepfe stehen am gewaehlten Satz.
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        string text = cut.Find(".epos-zweispalten-satzknoepfe").TextContent;
        foreach (string t in new[] { "Typ ändern…", "Bearbeiten…", "Löschen", "Neu…" })
            Assert.Contains(t, text);
        // Die alte Knopfleiste unter der Katalogliste ist entfallen.
        Assert.Empty(cut.FindAll(".epos-zweispalten-spalte .epos-katalogliste + .epos-leiste"));
        // „Bearbeiten…" des Katalogs („DB ändern…") steht erst beim Katalogsatz.
        Assert.Empty(cut.FindAll(".epos-knopf--db-aendern"));
        KatalogKlick(cut, 0);
        Assert.False(cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--db-aendern").HasAttribute("disabled"));
    }

    [Fact]
    public void S4_Der_Stift_der_Projektzeile_oeffnet_die_Satz_Ueberlagerung()
    {
        var cut = AufbauenS4(new List<BedarfsProfilZeile> { Zeile(1), Zeile(2, "Profil B") });

        cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-zeilenstift")[1].Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal("Profil B", cut.Instance.Gewaehlt?.Name);
        Assert.NotEmpty(cut.FindAll(".epos-satzueberlagerung-koerper input[inputmode=decimal]"));
    }

    [Fact]
    public void S4_Der_Stift_der_Katalogzeile_oeffnet_gesperrt_nur_lesend_sonst_den_Katalogeditor()
    {
        var katalog = Katalogzeilen().ToList();
        katalog[1].Geschuetzt = true;
        bool editor = false;
        var cut = AufbauenS4(new List<BedarfsProfilZeile>(), katalog,
                             typStamm: (_, _, _, neu) => { editor = !neu; return new Dictionary<string, object>(); });

        cut.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[0].Click();
        Assert.True(editor);
        Assert.True(cut.Instance.TypStammOffen);

        var cut2 = AufbauenS4(new List<BedarfsProfilZeile>(), katalog,
                              typStamm: (_, _, _, _) => new Dictionary<string, object>());
        cut2.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenstift")[1].Click();
        Assert.True(cut2.Instance.SatzUeberlagerungOffen);
        Assert.False(cut2.Instance.TypStammOffen);
        Assert.NotEmpty(cut2.FindAll(".epos-satzueberlagerung-schliessen"));
        Assert.Empty(cut2.FindAll(".epos-satzueberlagerung-ok"));
    }

    [Fact]
    public void S4_Die_Detailzeile_zeigt_die_Zusammenfassung_des_Satzes()
    {
        var cut = AufbauenS4(new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) },
                             kalender: new[] { (4, "Werk") });

        string zf = cut.Find(".epos-satzzusammenfassung").TextContent;
        Assert.Contains("Typ 1", zf);
        Assert.Contains("12,50", zf);
        Assert.Contains("Kalender", zf);
        Assert.Contains("Beschreibung Profil A", zf);
        // Ohne Ueberlagerung stehen die Felder nicht im Dialog.
        Assert.Empty(cut.FindAll("input[inputmode=decimal]"));

        KatalogKlick(cut, 1);
        zf = cut.Find(".epos-satzzusammenfassung").TextContent;
        Assert.Contains("Beschreibung Profil B", zf);
        Assert.Contains("6/12", zf.Replace(" ", "").Replace(",0", ""));
    }

    [Fact]
    public void S4_OK_der_Ueberlagerung_nimmt_die_angefangene_Eingabe_mit()
    {
        int geaendert = 0;
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) };
        var cut = AufbauenS4(zeilen, geaendert: () => geaendert++);

        Feld(cut, "input[inputmode=decimal]").Input("33,5");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(33.5, zeilen[0].Summe, 6);
        Assert.True(geaendert > 0);
    }

    [Fact]
    public void S4_Eine_abgelehnte_Eingabe_haelt_die_Ueberlagerung_mit_Grund_offen()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) };
        var cut = AufbauenS4(zeilen);

        Feld(cut, "input[inputmode=decimal]").Input("-1");
        cut.Find(".epos-satzueberlagerung-ok").Click();

        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(12.5, zeilen[0].Summe, 6);
        Assert.NotEmpty(cut.FindAll(".epos-satzueberlagerung-fuss .epos-status--fehler"));
    }

    [Fact]
    public void S4_Abbrechen_stellt_den_Stand_beim_Oeffnen_wieder_her()
    {
        var zeilen = new List<BedarfsProfilZeile> { Zeile(1, summe: 12.5) };
        var cut = AufbauenS4(zeilen, kalender: new[] { (4, "Werk") });

        Feld(cut, "input[inputmode=decimal]").Input("33,5");
        Knopf(cut, "Übernehmen").Click();
        cut.FindAll(".epos-satzueberlagerung-koerper select").First(s => s.InnerHtml.Contains("Werk")).Change("4");
        Assert.Equal(33.5, zeilen[0].Summe, 6);
        Assert.Equal(4, zeilen[0].KalenderId);

        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(12.5, zeilen[0].Summe, 6);
        Assert.Null(zeilen[0].KalenderId);
    }

    [Fact]
    public void S4_Mehrere_Katalogsaetze_loeschen_mit_einer_Rueckfrage_gesperrte_und_zugeordnete_uebersprungen()
    {
        var katalog = new List<Katalogfilterzeile>
        {
            Katalogzeile(1, "Profil A", "Typ 1", 1, "a"), Katalogzeile(2, "Profil B", "Typ 1", 1, "b"),
            Katalogzeile(3, "Profil C", "Typ 1", 1, "c"), Katalogzeile(4, "Profil D", "Typ 1", 1, "d")
        };
        katalog[3].Geschuetzt = true;
        var geloescht = new List<string>();
        var cut = AufbauenS4(new List<BedarfsProfilZeile> { Zeile(1, "Profil A") }, katalog,
                             loeschen: n => { geloescht.Add(n); return true; });

        KatalogKlick(cut, 0);   // AW3b: Loeschen wirkt, wenn ein Katalogsatz gewaehlt ist
        for (int i = 0; i < 4; i++)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
        cut.Find(".epos-knopf--loeschen").Click();

        string frage = cut.Find(".epos-rueckfrage").TextContent;
        Assert.Contains("„Profil B“", frage);
        Assert.Contains("„Profil D“", frage);   // gesperrt, genannt
        Assert.Contains("„Profil A“", frage);   // zugeordnet, genannt
        Knopf(cut, "Ja").Click();

        Assert.Equal(new[] { "Profil B", "Profil C" }, geloescht);
    }

    [Fact]
    public void S4_Die_Sammeluebernahme_nimmt_alle_angekreuzten_Katalogsaetze()
    {
        var zeilen = new List<BedarfsProfilZeile>();
        var cut = AufbauenS4(zeilen);

        for (int i = 0; i < 2; i++)
            cut.FindAll(".epos-raster")[1].QuerySelectorAll("td .epos-kaestchenzelle input")[i].Change(true);
        Uebernehmen(cut).Click();

        Assert.Equal(new[] { "Profil A", "Profil B" }, zeilen.Select(z => z.Name));
        Assert.Equal(2, cut.Instance.ProjektWahl.Anzahl);
    }

    [Fact]
    public void S4_Kaelte_zeigt_Katalog_und_Projektliste_mit_Temperaturpaar()
    {
        var katalog = new List<Katalogfilterzeile>
        {
            Katalogzeile(1, "Serverraum", "Serverraum", 20, "Kühlung eines Serverraums"),
            Katalogzeile(2, "Kühlraum", "Kühlraum", 10, "Kühlraum")
        };
        var zeilen = new List<BedarfsProfilZeile>
        {
            new() { IdZ = 1, IdStamm = 3, Name = "Serverraum", Summe = 20, Vorlauf = 18, Ruecklauf = 24 }
        };
        var cut = AufbauenS4(zeilen, katalog, BedarfsArt.Kaelte);

        Assert.Equal(2, cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr").Length);
        Assert.Contains("Serverraum", cut.FindAll(".epos-raster")[0].TextContent);
        Assert.Contains("18", cut.Find(".epos-satzzusammenfassung").TextContent);
        Satz(cut);
        // Kälte führt das Temperaturpaar wie die Prozesswärme (Konzept Kältebedarf 3.3).
        Assert.Equal(3, cut.FindAll(".epos-satzueberlagerung-koerper input[inputmode=decimal]").Count);
    }

    // =================================================================================
    // AW3b: eine Wahl je Zeile, Knopfleiste am gewaehlten Satz (Schema AW1)
    // =================================================================================

    private static IElement AmSatz(IRenderedComponent<BedarfsProfileDialog> cut, string klasse)
        => cut.Find(".epos-zweispalten-satzknoepfe ." + klasse);

    private static List<BedarfsProfilZeile> DreiZeilen()
        => new() { Zeile(1, "Profil A"), Zeile(2, "Profil B"), Zeile(3, "Profil C") };

    [Fact]
    public void AW3b_Die_Projektliste_traegt_keinen_Wahlknopf_die_Zeile_ist_die_Wahl()
    {
        var cut = AufbauenS4(DreiZeilen());
        var liste = cut.FindAll(".epos-raster")[0];
        Assert.Empty(liste.QuerySelectorAll(".epos-zeilenwahl, .epos-anlagenwahl, input[type=radio]"));
        Assert.Equal(3, liste.QuerySelectorAll("td.epos-spalte-zeilenzelle > .epos-zeilenzelle").Length);
        Assert.NotNull(cut.Find(".epos-zweispalten-bereich--projekt .epos-zweispalten-inhalt--zeilenwahl"));

        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[1].Click();

        var zeilen = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr").ToList();
        Assert.Single(zeilen, tr => tr.ClassList.Contains("epos-zeile--gewaehlt"));
        Assert.Contains("epos-zeile--gewaehlt", zeilen[1].ClassList);
        Assert.Equal("true", zeilen[1].QuerySelector(".epos-zeilenzelle")!.GetAttribute("aria-current"));
        Assert.Equal("Profil B", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Empty(cut.FindAll(".epos-raster")[0].QuerySelectorAll("td .epos-wahlkaestchen[aria-checked=true]"));
    }

    [Fact]
    public void AW3b_Die_Pfeiltasten_bewegen_die_Wahl_der_Projektliste()
    {
        var cut = AufbauenS4(DreiZeilen());
        const string huelle = ".epos-zweispalten-bereich--projekt .epos-raster-huelle";
        Assert.Equal("0", cut.Find(huelle).GetAttribute("tabindex"));
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[0].Click();
        cut.Find(huelle).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("Profil B", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(huelle).KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal("Profil C", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(huelle).KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal("Profil A", cut.Find(".epos-zweispalten-satzname").TextContent);
    }

    [Fact]
    public void AW3b_Die_Knopfleiste_steht_am_Satz_ohne_Stift_unten()
    {
        var cut = AufbauenS4(DreiZeilen(), typStamm: (_, _, _, _) => new Dictionary<string, object>(),
                             schloss: new Schlosspruefung().Weg());
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten"));
        string[] folge = { "epos-knopf--typ-aendern", "epos-katalogschloss", "epos-knopf--loeschen",
                           "epos-knopf--bearbeiten-projekt", "epos-knopf--neu" };
        var namen = cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-satzknoepfe").Children
            .Select(e => folge.FirstOrDefault(f => e.ClassList.Contains(f))).OfType<string>().ToList();
        Assert.Equal(folge, namen.ToArray());
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--vergleichen"));   // der Dialog vergleicht nicht
    }

    [Fact]
    public void AW3b_Beim_Projektsatz_sind_Schloss_und_Loeschen_weich_gesperrt()
    {
        var pruefung = new Schlosspruefung();
        var geloescht = new List<string>();
        var cut = AufbauenS4(DreiZeilen(), schloss: pruefung.Weg(), loeschen: n => { geloescht.Add(n); return true; });
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-projektzeile")[0].Click();

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
        {
            var knopf = AmSatz(cut, klasse);
            Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.AUSWAHL_SPERRE_KATALOGSATZ, knopf.GetAttribute("title"));
        }
        AmSatz(cut, "epos-knopf--loeschen").Click();
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.AUSWAHL_SPERRE_KATALOGSATZ, cut.Find(".epos-warnbanner-text").TextContent);
        AmSatz(cut, "epos-katalogschloss").Click();
        Assert.Empty(pruefung.Aufrufe);
        Assert.Empty(geloescht);

        AmSatz(cut, "epos-knopf--bearbeiten-projekt").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
    }

    [Fact]
    public void AW3b_Beim_Katalogsatz_wirken_Schloss_Loeschen_und_Bearbeiten_auf_den_Katalog()
    {
        var gaben = 0;
        var cut = AufbauenS4(DreiZeilen(), schloss: new Schlosspruefung().Weg(),
                             typStamm: (_, _, _, _) => { gaben++; return new Dictionary<string, object>(); });
        KatalogKlick(cut, 1);

        foreach (string klasse in new[] { "epos-katalogschloss", "epos-knopf--loeschen" })
            Assert.False(AmSatz(cut, klasse).HasAttribute("aria-disabled"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt"));
        AmSatz(cut, "epos-knopf--bearbeiten-katalog").Click();
        Assert.Equal(1, gaben);
    }

    [Fact]
    public void AW3b_Der_Stift_je_Zeile_ist_Bearbeiten_fuer_diese_Zeile()
    {
        var cut = AufbauenS4(DreiZeilen());
        cut.FindAll(".epos-raster")[0].QuerySelectorAll(".epos-zeilenstift")[2].Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains("epos-zeile--gewaehlt", cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[2].ClassList);
        Assert.Equal("Profil C", cut.Find(".epos-zweispalten-satzname").TextContent);
    }

    // =================================================================================
    // K5b (Konzept Kaeltebedarf 3.3, 3.4): die Art Kaelte - Deckung an der Projektzeile
    // =================================================================================

    private static List<BedarfsProfilZeile> Kaeltezeilen() => new()
    {
        new() { IdZ = 1, IdStamm = 3, Name = "Serverraum", Summe = 20, Vorlauf = 18, Ruecklauf = 24, Deckung = new DeckungsartDaten() },
        new() { IdZ = 2, IdStamm = 4, Name = "Büro", Summe = 10, Deckung = new DeckungsartDaten() }
    };

    /// <summary>Klappt die Gruppe „Deckung" der Bearbeiten-Überlagerung auf (zu bei „zentral").</summary>
    private static void Klappe(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        var felder = cut.FindComponent<DeckungsartFelder>();
        if (!felder.Instance.Offen) felder.Find("h3 button, h4 button").Click();
    }

    [Fact]
    public void K5b_Deckung_steht_nur_an_einer_Projektzeile_der_Art_Kaelte()
    {
        var kaelte = AufbauenS4(Kaeltezeilen(), art: BedarfsArt.Kaelte);
        Assert.True(kaelte.Instance.DeckungSichtbar);
        // Anwenderentscheid: in der Detailzeile nur die Zusammenfassung, kein eigener Knopf „Deckung…".
        Assert.Empty(kaelte.FindAll(".epos-knopf--deckung"));
        string zf = kaelte.Find(".epos-satzzusammenfassung").TextContent;
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.DECK_GRUPPE, zf);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.DECK_ART_ZENTRAL, zf);
        // Die Felder stehen in der Bearbeiten-Überlagerung unter den Projektfeldern.
        Assert.NotNull(Satz(kaelte).QuerySelector(".epos-deckung"));

        // Die Katalogzeile trägt keine Deckung.
        kaelte.Find(".epos-satzueberlagerung-abbrechen").Click();
        kaelte.FindAll(".epos-raster")[1].QuerySelectorAll(".epos-zeilenzelle")[0].Click();
        Assert.False(kaelte.Instance.DeckungSichtbar);

        // Die Prozesswärme kennt die Gruppe nicht, auch nicht mit einem Feldsatz in der Zeile.
        var prozess = AufbauenS4(Kaeltezeilen(), art: BedarfsArt.Prozesswaerme);
        Assert.False(prozess.Instance.DeckungSichtbar);
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.DECK_GRUPPE, prozess.Find(".epos-satzzusammenfassung").TextContent);
        Assert.Null(Satz(prozess).QuerySelector(".epos-deckung"));
    }

    [Fact]
    public void K5b_Bearbeiten_Abbrechen_verwirft_die_Deckung_OK_prueft_und_uebernimmt()
    {
        int gemeldet = 0;
        var zeilen = Kaeltezeilen();
        var cut = AufbauenS4(zeilen, art: BedarfsArt.Kaelte, geaendert: () => gemeldet++);

        // Abbrechen verwirft: die Zeile bleibt zentral, nichts gemeldet.
        Satz(cut);
        Klappe(cut);
        cut.FindComponent<DeckungsartFelder>().FindAll("select")[0].Change("1");
        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, cut.Instance.DeckungArbeitsstand!.Deckungsart);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[0].Deckung!.Deckungsart);
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[0].Deckung!.Deckungsart);
        Assert.Equal(0, gemeldet);

        // OK mit EER 0 bleibt offen und nennt den Grund; mit EER 3,2 übernimmt es.
        Satz(cut);
        Klappe(cut);
        cut.FindComponent<DeckungsartFelder>().FindAll("select")[0].Change("1");
        cut.Instance.DeckungArbeitsstand!.Eer1 = 0;
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.DECK_MSG_OHNE_EER, cut.Markup);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[0].Deckung!.Deckungsart);

        cut.Instance.DeckungArbeitsstand!.Eer1 = 3.2;
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, zeilen[0].Deckung!.Deckungsart);
        Assert.Equal(3.2, zeilen[0].Deckung!.Eer1);
        Assert.True(gemeldet > 0);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.DECK_ART_SPLIT, cut.Find(".epos-satzzusammenfassung").TextContent);
        Assert.Contains("3,20", cut.Find(".epos-satzzusammenfassung").TextContent);
    }

    [Fact]
    public void K5b_OK_des_Dialogs_lehnt_Split_ohne_EER_benannt_ab_und_waehlt_die_Zeile()
    {
        var zeilen = Kaeltezeilen();
        zeilen[1].Deckung = new DeckungsartDaten { Deckungsart = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_FEST };
        bool? geschlossen = null;
        var cut = Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.TitelText, "Kältebedarf")
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => Katalogzeilen())
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Kaelte))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1", 6.0, 12.0))
            .Add(x => x.Jahressumme, _ => 1.0)
            .Add(x => x.Geschlossen, b => geschlossen = b));

        Assert.Equal("Serverraum", cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.FindAll(".epos-speichernleiste .epos-knopf--primaer, .epos-leiste .epos-knopf--primaer")
           .Last().Click();

        Assert.Null(geschlossen);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.DECK_MSG_OHNE_EER + " (Büro)", cut.Find(".epos-warnbanner").TextContent);
        Assert.Equal("Büro", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Same(zeilen[1], cut.Instance.UnzulaessigeDeckung());

        zeilen[1].Deckung!.Eer1 = 3.0;
        zeilen[1].Deckung!.Taussen1 = 35;
        cut.FindAll(".epos-speichernleiste .epos-knopf--primaer, .epos-leiste .epos-knopf--primaer").Last().Click();
        Assert.True(geschlossen);
    }

    [Fact]
    public void K5b_Kaelte_zeigt_die_Temperaturangabe_ohne_Wirkung()
    {
        var cut = Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.Zeilen, Kaeltezeilen())
            .Add(x => x.Katalogzeilen, () => Katalogzeilen())
            .Add(x => x.Katalogprofil, Profil(BedarfsArt.Kaelte))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1", 6.0, 12.0))
            .Add(x => x.TemperaturPruefen, (v, r) => null)
            .Add(x => x.HinweisTemperatur, WindowsFormsApplication1.MyResource.Resource.BPF_HINW_TEMPERATUR_KAELTE));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.BPF_HINW_TEMPERATUR_KAELTE, Satz(cut).TextContent);
    }
}

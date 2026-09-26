using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KiFeldumsetzung = WindowsFormsApplication1.KiFeldumsetzung;
using KiFeldwandler = WindowsFormsApplication1.KiFeldwandler;
using KiFeldzugang = WindowsFormsApplication1.KiFeldzugang;
using KiMaskenbruecke = WindowsFormsApplication1.KiMaskenbruecke;
using KiMaskennamen = WindowsFormsApplication1.KiMaskennamen;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// BV-E1 (Konzept Berichtsvorlagen 9.7, 10.2) — die Gruppe „Vorlage" der Berichtsseite: das
/// Auswahlfeld mit Sperrgrund und Schloss, „Neue Vorlage…" über den Namensdialog, das Menü „…"
/// aus Handlungsdaten, die Prüfzeile mit der Überlagerung „Prüfliste", der Platzhalterkatalog
/// mit der Suche beim Wirt, die weiche Sperre während eines Laufs, das Nachladen des Standes
/// und die erweiterte Startrückfrage mit drei Wegen. Dazu der Fall ohne Gaben und die Sicht des
/// Assistenten.
///
/// <para>Kultur de-DE (deutsche Text-Asserts); alle Werte erfunden.</para>
/// </summary>
public class BerichtSeiteVorlagenTests : EposBunitContext
{
    public BerichtSeiteVorlagenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    // Prüfstand
    // =====================================================================

    private static BerichtStand Stand() => new()
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00", IstStamm = true },
            new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "Kessel groß",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00" },
            new VarianteZeile { IdProjekt = 1032, Art = "Variante", Bezeichner = "WP klein",
                                Projektname = "Musterhaus", SimStand = "01.09.2026 08:00" }
        },
        GewaehlteVarianten = new[] { 1030, 1031, 1032 },
        Bausteine = new[]
        {
            new BausteinZeile { Schluessel = "KOPF", Titel = "Projektkopf" },
            new BausteinZeile { Schluessel = "ERGEBNISSE", Titel = "Ergebnisse je Variante" }
        },
        AktiveBausteine = new[] { "KOPF" },
        Zielordner = @"C:\Berichte"
    };

    private const string FEHLT = "nicht vorhanden – Standard verwendet";

    private static readonly Vorlagenzeile Standard = new(1, "Standard (EPOS-Plan)", Mitgeliefert: true);
    private static readonly Vorlagenzeile Kurzbericht = new(2, "Kurzbericht Kunde");
    private static readonly Vorlagenzeile Fehlend = new(3, "Alte Vorlage", Gesperrt: true, GesperrtHinweis: FEHLT);

    private static IReadOnlyList<Vorlagenzeile> Drei() => new[] { Standard, Kurzbericht, Fehlend };

    private IRenderedComponent<BerichtSeite> Zeige(
        Action<ComponentParameterCollectionBuilder<BerichtSeite>>? mehr = null)
        => Render<BerichtSeite>(p =>
        {
            p.Add(x => x.Laden, Stand);
            mehr?.Invoke(p);
        });

    /// <summary>Der Knopf „Erstellen" der Seite (nicht die Knöpfe der Gruppe).</summary>
    private static IElement Erstellenknopf(IRenderedComponent<BerichtSeite> cut)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == "Erstellen");

    private static Func<BerichtAuftrag, Action<Laufschritt>, Task<LaufErgebnis>> Lauf(Action<BerichtAuftrag>? merken = null)
        => (a, _) =>
        {
            merken?.Invoke(a);
            return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig" });
        };

    // =====================================================================
    // Ohne Gaben
    // =====================================================================

    /// <summary>
    /// Hausregel „jede Seite zeichnet auch ohne Gaben": ohne Vorlagen und ohne Rückrufe steht
    /// die Gruppe nicht da — kein Delegat, kein Knopf —, und die Seite zeichnet wie bisher.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_die_Seite_ohne_Vorlagengruppe()
    {
        var cut = Render<BerichtSeite>();

        Assert.Empty(cut.FindAll(".epos-vorlage"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Equal(3, cut.FindAll(".epos-leiste button").Count);   // Alle, Keine, Erstellen
        Assert.Null(cut.Instance.GewaehlteVorlageId);
        Assert.Equal("", cut.Instance.Assistentensicht.Pruefzeile);
    }

    // =====================================================================
    // Wahl, Sperrgrund, Schloss
    // =====================================================================

    [Fact]
    public void Die_Gruppe_steht_ueber_den_Bausteinen_mit_drei_Vorlagen_Sperrgrund_und_Schloss()
    {
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1));

        IElement gruppe = cut.Find(".epos-vorlage");
        Assert.Contains("Vorlage:", gruppe.QuerySelector(".epos-untergruppe")!.TextContent);
        Assert.Contains("Word-Vorlage:", gruppe.QuerySelector(".epos-feld-text")!.TextContent);

        // Oben rechts UEBER den Haekchen: in derselben Spalte, vor der Bausteinliste.
        IElement spalte = gruppe.ParentElement!;
        var kinder = spalte.Children.ToList();
        Assert.True(kinder.IndexOf(gruppe) < kinder.FindIndex(k => k.ClassList.Contains("epos-mehrfachauswahl")));

        var optionen = gruppe.QuerySelectorAll("select option");
        Assert.Equal(new[] { "Standard (EPOS-Plan)", "Kurzbericht Kunde", "Alte Vorlage" },
                     optionen.Select(o => o.TextContent.Trim()));
        Assert.True(optionen[0].HasAttribute("selected"));
        Assert.True(optionen[2].HasAttribute("disabled"));
        Assert.Equal(FEHLT, optionen[2].GetAttribute("title"));

        // Das Schloss steht NEBEN dem Feld, mit Kurztext und ohne Wort.
        IElement schloss = cut.Find(".epos-vorlage-wahl .epos-schloss");
        Assert.StartsWith("Mitgelieferte Vorlage", schloss.GetAttribute("aria-label"));
        Assert.Equal("", schloss.TextContent.Trim());
    }

    [Fact]
    public void Ein_Wechsel_meldet_die_Id_und_das_Schloss_folgt_der_Wahl()
    {
        int? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
                              .Add(x => x.VorlageIdChanged, (int? id) => gemeldet = id));

        cut.Find(".epos-vorlage select").Change("2");

        Assert.Equal(2, gemeldet);
        Assert.Equal(2, cut.Instance.GewaehlteVorlageId);
        Assert.Empty(cut.FindAll(".epos-vorlage-wahl .epos-schloss"));
    }

    [Fact]
    public void Eine_gewaehlte_fehlende_Vorlage_nennt_ihren_Grund_unter_dem_Feld()
    {
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 3));

        Assert.Contains(FEHLT, cut.Find(".epos-vorlage .epos-herleitung-text").TextContent);
    }

    // =====================================================================
    // „Neue Vorlage…" und das Nachladen
    // =====================================================================

    [Fact]
    public void Neue_Vorlage_fragt_den_Namen_und_die_Seite_holt_den_frischen_Stand()
    {
        string? name = null;
        Vorlagenstand? frisch = null;
        int geladen = 0;
        var neu = new Vorlagenzeile(4, "Angebot Müller");

        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.NeueVorlage, (string n) =>
            {
                name = n;
                frisch = new Vorlagenstand
                {
                    Vorlagen = Drei().Append(neu).ToList(),
                    VorlageId = 4,
                    Pruefzeile = new Pruefstand("✓", "geprüft, 23 Platzhalter, keine Befunde"),
                    Meldung = "Vorlage „Angebot Müller“ angelegt."
                };
            })
            .Add(x => x.VorlagenNeuLaden, () => { geladen++; return frisch; }));

        cut.Find(".epos-vorlage-neu").Click();

        // Der Namensdialog steht in einer Ueberlagerung - ein Titel, eine Stelle.
        IElement ueber = cut.Find(".epos-ueberlagerung");
        Assert.Equal("Neue Vorlage", ueber.QuerySelector(".epos-ueberlagerung-titel")!.TextContent);
        Assert.Empty(ueber.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Empty(ueber.QuerySelectorAll(".epos-dialog-zu"));
        Assert.Contains("Kopie der gewählten mitgelieferten Vorlage", ueber.TextContent);

        cut.Find(".epos-ueberlagerung input[type=text]").Input("  Angebot Müller ");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal("Angebot Müller", name);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Equal(1, geladen);
        Assert.Equal(4, cut.Instance.GewaehlteVorlageId);
        Assert.Equal(4, cut.FindAll(".epos-vorlage select option").Count);
        Assert.Contains("keine Befunde", cut.Find(".epos-vorlage-pruefzeile").TextContent);
        Assert.Equal("Vorlage „Angebot Müller“ angelegt.", cut.Instance.Status);
    }

    /// <summary>
    /// BV-E5 (Konzept 10.2): „Neue Vorlage…" als Kopie der Standardvorlage ODER des Kurzberichts — mit mehr als einem
    /// Muster steht über dem Namen eine Optionsgruppe, das erste Muster vorgewählt; gemeldet werden Name und Kennung
    /// des gewählten Musters über <c>NeueVorlageAus</c>, nicht über <c>NeueVorlage</c>.
    /// </summary>
    [Fact]
    public void Neue_Vorlage_waehlt_zwischen_Standardvorlage_und_Kurzbericht()
    {
        Neuvorlage? gemeldet = null;
        string? alt = null;
        var muster = new List<(int Id, string Text)> { (0, "Standardvorlage"), (1, "Kurzbericht") };
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.NeueVorlage, (string n) => alt = n)
            .Add(x => x.Vorlagenmuster, muster)
            .Add(x => x.NeueVorlageAus, (Neuvorlage n) => gemeldet = n));

        cut.Find(".epos-vorlage-neu").Click();
        IElement gruppe = cut.Find(".epos-ueberlagerung .epos-vorlage-muster");
        Assert.Contains("Kopie von:", gruppe.TextContent);
        var knoepfe = cut.FindAll(".epos-vorlage-muster input[type=radio]");
        Assert.Equal(2, knoepfe.Count);
        Assert.True(knoepfe[0].HasAttribute("checked"));

        knoepfe[1].Change("1");
        cut.Find(".epos-ueberlagerung input[type=text]").Input(" Angebot kurz ");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal(new Neuvorlage("Angebot kurz", 1), gemeldet);
        Assert.Null(alt);

        // Beim nächsten Öffnen ist wieder das erste Muster gewählt.
        cut.Find(".epos-vorlage-neu").Click();
        Assert.True(cut.FindAll(".epos-vorlage-muster input[type=radio]")[0].HasAttribute("checked"));
    }

    /// <summary>
    /// Mit nur einem Muster (der Kurzbericht ist nicht mitgeliefert) keine Wahl — die Kopie kommt aus diesem Muster;
    /// ohne Muster und ohne <c>NeueVorlageAus</c> geht der Name wie bisher an <c>NeueVorlage</c>.
    /// </summary>
    [Fact]
    public void Mit_einem_Muster_keine_Wahl_ohne_Muster_wie_bisher()
    {
        Neuvorlage? gemeldet = null;
        string? name = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.NeueVorlage, (string n) => name = n)
            .Add(x => x.Vorlagenmuster, new List<(int Id, string Text)> { (0, "Standardvorlage") })
            .Add(x => x.NeueVorlageAus, (Neuvorlage n) => gemeldet = n));
        cut.Find(".epos-vorlage-neu").Click();
        Assert.Empty(cut.FindAll(".epos-vorlage-muster"));
        cut.Find(".epos-ueberlagerung input[type=text]").Input("Angebot");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();
        Assert.Equal(new Neuvorlage("Angebot", 0), gemeldet);
        Assert.Null(name);

        var ohne = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.NeueVorlage, (string n) => name = n));
        ohne.Find(".epos-vorlage-neu").Click();
        Assert.Empty(ohne.FindAll(".epos-vorlage-muster"));
        ohne.Find(".epos-ueberlagerung input[type=text]").Input("Angebot 2");
        ohne.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();
        Assert.Equal("Angebot 2", name);
    }

    [Fact]
    public void Ein_vergebener_Name_haelt_den_Namensdialog_offen()
    {
        string? name = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.NeueVorlage, (string n) => name = n)
            .Add(x => x.VorlagennamePruefen, (Func<string, string?>)(n =>
                n == "Kurzbericht Kunde" ? "Eine Vorlage dieses Namens gibt es schon." : null)));

        cut.Find(".epos-vorlage-neu").Click();
        cut.Find(".epos-ueberlagerung input[type=text]").Input("Kurzbericht Kunde");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Null(name);
        Assert.Contains("gibt es schon", cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent);
    }

    [Fact]
    public void Abbrechen_Esc_und_Kreuz_des_Namensdialogs_rufen_nichts()
    {
        int gerufen = 0;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.NeueVorlage, (string _) => gerufen++));

        cut.Find(".epos-vorlage-neu").Click();
        cut.FindAll(".epos-ueberlagerung .epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        cut.Find(".epos-vorlage-neu").Click();
        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        cut.Find(".epos-vorlage-neu").Click();
        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        Assert.Equal(0, gerufen);
    }

    // =====================================================================
    // Das Menü „…"
    // =====================================================================

    private static IReadOnlyList<Handlung> Handlungen() => new[]
    {
        new Handlung("oeffnen", "In Word öffnen"),
        new Handlung("ordner", "Im Ordner zeigen", Aktiv: false, Grund: "Der Ordner ist gerade nicht erreichbar."),
        new Handlung("entfernen", "Entfernen")
    };

    [Fact]
    public void Das_Menue_zeigt_die_Handlungen_und_ein_inaktiver_Eintrag_meldet_seinen_Grund()
    {
        string? gewaehlt = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Vorlagenhandlungen, Handlungen())
            .Add(x => x.HandlungGewaehlt, (string id) => gewaehlt = id));

        IElement knopf = cut.Find(".epos-vorlage-menueknopf");
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Equal("Weitere Handlungen zur gewählten Vorlage", knopf.GetAttribute("aria-label"));
        knopf.Click();

        Assert.Equal("true", cut.Find(".epos-vorlage-menueknopf").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll(".epos-vorlage-menue-schliessflaeche"));
        var eintraege = cut.FindAll(".epos-vorlage-menue-eintrag");
        Assert.Equal(new[] { "In Word öffnen", "Im Ordner zeigen", "Entfernen" },
                     eintraege.Select(e => e.TextContent.Trim()));

        // Weich gesperrt, nie nur disabled: Grund als Kurztext, der Klick meldet ihn.
        Assert.Equal("true", eintraege[1].GetAttribute("aria-disabled"));
        Assert.Equal("Der Ordner ist gerade nicht erreichbar.", eintraege[1].GetAttribute("title"));
        Assert.False(eintraege[1].HasAttribute("disabled"));
        eintraege[1].Click();

        Assert.Null(gewaehlt);
        Assert.Empty(cut.FindAll(".epos-vorlage-menue-liste"));
        Assert.Contains("nicht erreichbar", cut.Find(".epos-vorlage .epos-warnbanner").TextContent);

        cut.Find(".epos-vorlage-menueknopf").Click();
        cut.FindAll(".epos-vorlage-menue-eintrag")[2].Click();
        Assert.Equal("entfernen", gewaehlt);
    }

    /// <summary>
    /// „In den Vorlagenordner exportieren…": eine Handlung mit Namensvorschlag öffnet den Namensdialog von „Neue Vorlage…"
    /// ohne Musterwahl, vorbelegt mit dem Vorschlag und mit dem Titel der Handlung; gemeldet werden Kennung und Name über
    /// <c>HandlungMitNameGewaehlt</c>, nicht über <c>HandlungGewaehlt</c> und nicht als neue Vorlage.
    /// </summary>
    [Fact]
    public void Eine_Handlung_mit_Namensvorschlag_fragt_erst_den_Namen()
    {
        string? ohneName = null;
        string? neu = null;
        Benannthandlung? gemeldet = null;
        var muster = new List<(int Id, string Text)> { (0, "Standardvorlage"), (1, "Kurzbericht") };
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.Vorlagenhandlungen, new[]
            {
                new Handlung("schreibgeschuetzt", "Schreibgeschützt öffnen"),
                new Handlung("exportieren", "In den Vorlagenordner exportieren…", Namensvorschlag: "Beispiel – Standard")
            })
            .Add(x => x.HandlungGewaehlt, (string id) => ohneName = id)
            .Add(x => x.NeueVorlage, (string n) => neu = n)
            .Add(x => x.Vorlagenmuster, muster)
            .Add(x => x.NeueVorlageAus, (Neuvorlage n) => neu = n.Name)
            .Add(x => x.HandlungMitNameGewaehlt, (Benannthandlung h) => gemeldet = h));

        cut.Find(".epos-vorlage-menueknopf").Click();
        cut.FindAll(".epos-vorlage-menue-eintrag")[1].Click();

        IElement ueber = cut.Find(".epos-ueberlagerung");
        Assert.Equal("In den Vorlagenordner exportieren", ueber.QuerySelector(".epos-ueberlagerung-titel")!.TextContent);
        Assert.Empty(ueber.QuerySelectorAll(".epos-vorlage-muster"));
        Assert.Contains("gewählt bleibt die aktuelle Vorlage", ueber.TextContent);
        Assert.Equal("Beispiel – Standard", cut.Find(".epos-ueberlagerung input[type=text]").GetAttribute("value"));

        cut.Find(".epos-ueberlagerung input[type=text]").Input(" Beispiel – Büro ");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();

        Assert.Equal(new Benannthandlung("exportieren", "Beispiel – Büro"), gemeldet);
        Assert.Null(ohneName);
        Assert.Null(neu);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        // „Neue Vorlage…" danach wieder mit Musterwahl und ohne Vorschlag.
        cut.Find(".epos-vorlage-neu").Click();
        Assert.Equal("Neue Vorlage", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-vorlage-muster"));
        Assert.True(string.IsNullOrEmpty(cut.Find(".epos-ueberlagerung input[type=text]").GetAttribute("value")));
    }

    /// <summary>
    /// Eine Handlung mit Rückfrage („Entfernen") geht erst auf „Ja" hinaus — die Frage steht im
    /// selben Fenster, Vorgabe „Nein"; ein freier Eintrag trägt seinen Kurztext.
    /// </summary>
    [Fact]
    public void Eine_Handlung_mit_Rueckfrage_fragt_erst_mit_Vorgabe_Nein()
    {
        string? gewaehlt = null;
        var handlungen = new[]
        {
            new Handlung("schreibgeschuetzt", "Schreibgeschützt öffnen",
                         Kurztext: "Änderungen an der mitgelieferten Vorlage werden nicht gespeichert."),
            new Handlung("entfernen", "Entfernen",
                         Rueckfrage: "Die Vorlage „Kurzbericht Kunde“ aus dem Vorlagenordner entfernen?")
        };
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Vorlagenhandlungen, handlungen)
            .Add(x => x.HandlungGewaehlt, (string id) => gewaehlt = id));

        cut.Find(".epos-vorlage-menueknopf").Click();
        Assert.Equal("Änderungen an der mitgelieferten Vorlage werden nicht gespeichert.",
                     cut.FindAll(".epos-vorlage-menue-eintrag")[0].GetAttribute("title"));
        Assert.Null(cut.FindAll(".epos-vorlage-menue-eintrag")[1].GetAttribute("title"));
        cut.FindAll(".epos-vorlage-menue-eintrag")[1].Click();

        Assert.Null(gewaehlt);
        Assert.Equal("Entfernen", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Contains("aus dem Vorlagenordner entfernen?", cut.Find(".epos-rueckfrage-text").TextContent);
        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Equal(new[] { "Ja", "Nein" }, knoepfe.Select(k => k.TextContent.Trim()));
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName);   // Vorgabe „Nein"
        knoepfe[1].Click();
        Assert.Null(gewaehlt);

        cut.Find(".epos-vorlage-menueknopf").Click();
        cut.FindAll(".epos-vorlage-menue-eintrag")[1].Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja
        Assert.Equal("entfernen", gewaehlt);
    }

    [Fact]
    public void Das_Menue_schliesst_ueber_die_Schliessflaeche_und_Esc()
    {
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Vorlagenhandlungen, Handlungen())
            .Add(x => x.HandlungGewaehlt, (string _) => { }));

        cut.Find(".epos-vorlage-menueknopf").Click();
        cut.Find(".epos-vorlage-menue-schliessflaeche").Click();
        Assert.Empty(cut.FindAll(".epos-vorlage-menue-liste"));

        cut.Find(".epos-vorlage-menueknopf").Click();
        cut.Find(".epos-vorlage-menue").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".epos-vorlage-menue-liste"));
        Assert.Empty(cut.FindAll(".epos-vorlage-menue-schliessflaeche"));
    }

    [Fact]
    public void Ohne_Rueckruf_steht_kein_Menue_und_ohne_Delegaten_kein_Knopf()
    {
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Vorlagenhandlungen, Handlungen()));

        Assert.Single(cut.FindAll(".epos-vorlage select"));
        Assert.Empty(cut.FindAll(".epos-vorlage-menueknopf"));
        Assert.Empty(cut.FindAll(".epos-vorlage-leiste button"));
    }

    // =====================================================================
    // Prüfzeile und Prüfliste
    // =====================================================================

    [Fact]
    public void Die_Pruefzeile_zeigt_Zeichen_und_Text_und_anzeigen_oeffnet_die_Pruefliste()
    {
        int geholt = 0;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Pruefzeile, new Pruefstand("⚠", "2 unbekannte Platzhalter", HatBefunde: true))
            .Add(x => x.PrueflisteGaben, () =>
            {
                geholt++;
                return new Dictionary<string, object>
                {
                    ["Meldungen"] = new[]
                    {
                        new Pruefmeldungszeile(Pruefstufe.Fehler, "Unbekannter Platzhalter {{projekt.kundename}}",
                                               "Absatz 3", "Vorschlag {{projekt.kunde}} übernehmen")
                    },
                    ["Vorlagenname"] = "Kurzbericht Kunde",
                    ["Platzhalterzahl"] = 21
                };
            }));

        IElement zeile = cut.Find(".epos-vorlage-pruefzeile");
        Assert.Contains("epos-vorlage-pruefzeile--befund", zeile.ClassName);
        IElement zeichen = zeile.QuerySelector(".epos-vorlage-pruefsymbol")!;
        Assert.Equal("⚠", zeichen.TextContent);
        Assert.Equal("true", zeichen.GetAttribute("aria-hidden"));
        Assert.Equal("2 unbekannte Platzhalter", zeile.QuerySelector(".epos-herleitung-text")!.TextContent);
        Assert.Empty(zeile.QuerySelectorAll(".epos-schloss"));       // kein Kennzeichen (9.7)

        IElement anzeigen = zeile.QuerySelector("button.epos-vorlage-anzeigen")!;
        Assert.Equal("anzeigen", anzeigen.TextContent.Trim());
        anzeigen.Click();

        Assert.Equal(1, geholt);
        Assert.Equal("Prüfliste der Vorlage", cut.Find(".epos-ueberlagerung-titel").TextContent);
        IElement liste = cut.Find(".epos-vorlage-pruefliste");
        Assert.Empty(liste.QuerySelectorAll(".epos-dialog-titel"));   // TitelAnzeigen="false"
        Assert.Empty(liste.QuerySelectorAll(".epos-dialog-zu"));
        Assert.Contains("Geprüft: 21 Platzhalter, Vorlage „Kurzbericht Kunde“", liste.TextContent);

        cut.Find(".epos-ueberlagerung-zu").Click();
        Assert.Empty(cut.FindAll(".epos-vorlage-pruefliste"));

        // Auch „Schließen" der Liste führt zurück.
        cut.Find("button.epos-vorlage-anzeigen").Click();
        cut.Find(".epos-vorlage-pruefliste .epos-vorlage-schliessen").Click();
        Assert.Empty(cut.FindAll(".epos-vorlage-pruefliste"));
    }

    [Fact]
    public void Ohne_Befunde_steht_kein_anzeigen()
    {
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.Pruefzeile, new Pruefstand("✓", "geprüft, 23 Platzhalter, keine Befunde"))
            .Add(x => x.PrueflisteGaben, () => new Dictionary<string, object>()));

        Assert.Empty(cut.FindAll(".epos-vorlage-anzeigen"));
        Assert.DoesNotContain("epos-vorlage-pruefzeile--befund", cut.Find(".epos-vorlage-pruefzeile").ClassName);
    }

    // =====================================================================
    // Hinzufügen, Prüfen und die weiche Sperre während eines Laufs
    // =====================================================================

    [Fact]
    public void Hinzufuegen_und_Pruefen_rufen_ihren_Rueckruf_und_holen_den_Stand_nach()
    {
        int hinzu = 0, geprueft = 0, geladen = 0;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Hinzufuegen, () => hinzu++)
            .Add(x => x.Pruefen, () => geprueft++)
            .Add(x => x.VorlagenNeuLaden, () =>
            {
                geladen++;
                return new Vorlagenstand
                {
                    Vorlagen = Drei(), VorlageId = 2,
                    Pruefzeile = new Pruefstand("⚠", "1 Warnung", HatBefunde: true),
                    Fehler = geladen == 1 ? "Die Datei konnte nicht kopiert werden." : ""
                };
            }));

        cut.Find(".epos-vorlage-hinzufuegen").Click();
        Assert.Equal(1, hinzu);
        Assert.Contains("nicht kopiert", cut.Find(".epos-vorlage .epos-warnbanner").TextContent);

        cut.Find(".epos-vorlage-pruefen").Click();
        Assert.Equal(1, geprueft);
        Assert.Equal(2, geladen);
        Assert.Empty(cut.FindAll(".epos-vorlage .epos-warnbanner"));
        Assert.Contains("1 Warnung", cut.Find(".epos-vorlage-pruefzeile").TextContent);
    }

    [Fact]
    public void Waehrend_eines_Laufs_sind_die_Knoepfe_der_Gruppe_weich_gesperrt()
    {
        var lauf = new TaskCompletionSource<LaufErgebnis>();
        int hinzu = 0;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.NeueVorlage, (string _) => { })
            .Add(x => x.Hinzufuegen, () => hinzu++)
            .Add(x => x.Pruefen, () => { })
            .Add(x => x.PlatzhalterkatalogGaben, () => new Dictionary<string, object>())
            .Add(x => x.Erstellen, (BerichtAuftrag _, Action<Laufschritt> _) => lauf.Task));

        Erstellenknopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja
        Assert.True(cut.Instance.Beschaeftigt);

        var knoepfe = cut.FindAll(".epos-vorlage-leiste > button");
        Assert.Equal(4, knoepfe.Count);
        Assert.All(knoepfe, k =>
        {
            Assert.Equal("true", k.GetAttribute("aria-disabled"));
            Assert.False(k.HasAttribute("disabled"));
            Assert.Equal("Während ein Bericht entsteht, bleibt die Vorlage, wie sie ist.", k.GetAttribute("title"));
        });
        Assert.True(cut.Find(".epos-vorlage select").HasAttribute("disabled"));   // die Wahl hart wie die übrigen Felder

        cut.Find(".epos-vorlage-hinzufuegen").Click();
        Assert.Equal(0, hinzu);
        Assert.Contains("Während ein Bericht entsteht", cut.Find(".epos-vorlage .epos-warnbanner").TextContent);

        cut.Find(".epos-vorlage-neu").Click();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        lauf.SetResult(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig" });
        cut.WaitForAssertion(() => Assert.Null(cut.Find(".epos-vorlage-hinzufuegen").GetAttribute("aria-disabled")));

        cut.Find(".epos-vorlage-hinzufuegen").Click();
        Assert.Equal(1, hinzu);
    }

    // =====================================================================
    // Der Platzhalterkatalog — die Suche gehört dem Wirt
    // =====================================================================

    private static IReadOnlyList<Katalogzeile> Katalog() => new[]
    {
        new Katalogzeile("projekt.kunde", "Text", "Bericht", "Kunde des Projekts"),
        new Katalogzeile("bericht.datum", "Datum", "Bericht", "Datum des Berichts")
    };

    [Fact]
    public void Platzhalter_oeffnet_den_Katalog_und_die_Suche_steht_beim_Wirt()
    {
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.PlatzhalterkatalogGaben, () => new Dictionary<string, object> { ["Eintraege"] = Katalog() }));

        cut.Find(".epos-vorlage-platzhalter").Click();

        Assert.Single(cut.FindAll(".epos-vorlage-katalog"));
        Assert.Equal("Platzhalterkatalog", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(cut.FindAll(".epos-vorlage-katalog .epos-dialog-zu"));
        Assert.Equal(2, cut.FindAll(".epos-vorlage-katalogtabelle tbody tr").Count);

        cut.Find(".epos-vorlage-katalogsuche input").Input("kunde");
        Assert.Single(cut.FindAll(".epos-vorlage-katalogtabelle tbody tr"));
        Assert.Equal("kunde", cut.Instance.Assistentensicht.Katalogsuche);

        // Der Assistent setzt die Suche über den Wirt - das offene Blatt folgt.
        cut.InvokeAsync(() => cut.Instance.Assistentensicht.Katalogsuche = "datum");
        cut.WaitForAssertion(() => Assert.Equal("bericht.datum",
            cut.Find(".epos-vorlage-katalogtabelle tbody tr .epos-vorlage-katalogwahl").TextContent.Trim()));

        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".epos-vorlage-katalog"));

        // Wieder geöffnet steht die Suche noch da.
        cut.Find(".epos-vorlage-platzhalter").Click();
        Assert.Equal("datum", cut.Find(".epos-vorlage-katalogsuche input").GetAttribute("value"));
    }

    // =====================================================================
    // Die erweiterte Startrückfrage (BV-Q6)
    // =====================================================================

    private static Startrueckfrage Frage(bool eigeneMoeglich = true) => new(
        "Vorlage mit Befunden",
        "{0} Version(en) werden neu simuliert. Die Vorlage „Kurzbericht Kunde“ hat Befunde:",
        new[] { "2 unbekannte Platzhalter: {{projekt.kundename}}, {{bericht.titl}}", " " },
        "Mit meiner Vorlage", "Mit Standardvorlage", "Abbrechen", eigeneMoeglich);

    [Theory]
    [InlineData(0, Startweg.Eigene, true)]
    [InlineData(1, Startweg.Standard, true)]
    [InlineData(2, Startweg.Abbruch, false)]
    public void Die_erweiterte_Rueckfrage_bietet_drei_Wege_und_meldet_den_gewaehlten(int knopf, string erwartet, bool laeuft)
    {
        string? weg = null;
        BerichtAuftrag? auftrag = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Startrueckfrage, Frage())
            .Add(x => x.StartGewaehlt, (string w) => weg = w)
            .Add(x => x.Erstellen, Lauf(a => auftrag = a)));

        Erstellenknopf(cut).Click();

        Assert.Equal("Vorlage mit Befunden", cut.Find(".epos-ueberlagerung-titel").TextContent);
        string text = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.StartsWith("3 Version(en) werden neu simuliert.", text);
        Assert.Contains("\n• 2 unbekannte Platzhalter: {{projekt.kundename}}, {{bericht.titl}}", text);
        Assert.DoesNotContain("• \n", text + "\n");

        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Equal(new[] { "Mit meiner Vorlage", "Mit Standardvorlage", "Abbrechen" },
                     knoepfe.Select(k => k.TextContent.Trim()));
        knoepfe[knopf].Click();

        Assert.Equal(erwartet, weg);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        if (laeuft)
        {
            Assert.NotNull(auftrag);
            Assert.Equal(erwartet, auftrag!.Vorlagenweg);
            Assert.Equal(2, auftrag.VorlageId);
            Assert.Equal(3, auftrag.AnzahlMitStamm);
        }
        else
        {
            Assert.Null(auftrag);
        }
    }

    [Fact]
    public void Ohne_eigenen_Weg_stehen_nur_Standard_und_Abbrechen_und_Esc_bricht_ab()
    {
        var wege = new List<string>();
        int laeufe = 0;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Startrueckfrage, Frage(eigeneMoeglich: false))
            .Add(x => x.StartGewaehlt, (string w) => wege.Add(w))
            .Add(x => x.Erstellen, Lauf(_ => laeufe++)));

        Erstellenknopf(cut).Click();
        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Equal(new[] { "Mit Standardvorlage", "Abbrechen" }, knoepfe.Select(k => k.TextContent.Trim()));

        cut.Find(".epos-ueberlagerung").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(new[] { Startweg.Abbruch }, wege);
        Assert.Equal(0, laeufe);

        Erstellenknopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();
        Assert.Equal(new[] { Startweg.Abbruch, Startweg.Standard }, wege);
        Assert.Equal(1, laeufe);
    }

    /// <summary>
    /// BV-E7-3: Die Rückfrage allein aus Befunden der Excel-Vorlage — derselbe Dialog, der zweite Weg heißt
    /// „Ohne Excel-Vorlage“ und reist als „standard“ im Auftrag mit.
    /// </summary>
    [Fact]
    public void Die_Rueckfrage_der_Excel_Vorlage_nimmt_denselben_Weg()
    {
        string? weg = null;
        BerichtAuftrag? auftrag = null;
        var frage = new Startrueckfrage("Bericht erstellen",
            "{0} Projekt(e); die Excel-Mappe entsteht aus der Excel-Vorlage „Mappe“.",
            new[] { "Excel-Vorlage: Unbekannter Platzhalter {{projekt.kundename}} (Deckblatt!A2)" },
            "Mit meiner Vorlage", "Ohne Excel-Vorlage", "Abbrechen");
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.Startrueckfrage, frage)
            .Add(x => x.StartGewaehlt, (string w) => weg = w)
            .Add(x => x.Erstellen, Lauf(a => auftrag = a)));

        Erstellenknopf(cut).Click();
        Assert.Contains("\n• Excel-Vorlage: Unbekannter Platzhalter", cut.Find(".epos-rueckfrage-text").TextContent);
        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Equal(new[] { "Mit meiner Vorlage", "Ohne Excel-Vorlage", "Abbrechen" }, knoepfe.Select(k => k.TextContent.Trim()));
        knoepfe[1].Click();

        Assert.Equal(Startweg.Standard, weg);
        Assert.Equal(Startweg.Standard, auftrag!.Vorlagenweg);
    }

    [Fact]
    public void Ohne_Startrueckfrage_gilt_die_heutige_und_der_Weg_bleibt_leer()
    {
        int gemeldet = 0;
        BerichtAuftrag? auftrag = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.FrageStart, "{0} Version(en) neu rechnen?")
            .Add(x => x.StartGewaehlt, (string _) => gemeldet++)
            .Add(x => x.Erstellen, Lauf(a => auftrag = a)));

        Erstellenknopf(cut).Click();
        Assert.Contains("3 Version(en) neu rechnen?", cut.Find(".epos-rueckfrage-text").TextContent);
        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.Equal(new[] { "Ja", "Nein" }, knoepfe.Select(k => k.TextContent.Trim()));
        knoepfe[0].Click();

        Assert.Equal(0, gemeldet);
        Assert.NotNull(auftrag);
        Assert.Equal("", auftrag!.Vorlagenweg);
        Assert.Equal(2, auftrag.VorlageId);
    }

    /// <summary>
    /// Konzept 6.8: Die Schnellprüfung läuft vor JEDEM Start — der frische Stand entscheidet,
    /// welche Rückfrage steht, auch wenn der Anfangsstand keine erweiterte trug.
    /// </summary>
    [Fact]
    public void Vor_dem_Start_holt_die_Seite_die_Rueckfrage_frisch()
    {
        int geladen = 0;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 2)
            .Add(x => x.VorlagenNeuLaden, () =>
            {
                geladen++;
                return new Vorlagenstand { Vorlagen = Drei(), VorlageId = 2, Startrueckfrage = Frage() };
            })
            .Add(x => x.Erstellen, Lauf()));

        Erstellenknopf(cut).Click();

        Assert.Equal(1, geladen);
        Assert.Equal(3, cut.FindAll(".epos-rueckfrage .epos-leiste button").Count);
    }

    // =====================================================================
    // Die Sicht des Assistenten
    // =====================================================================

    [Fact]
    public void Der_Assistent_liest_und_setzt_die_Vorlage_und_liest_die_Pruefzeile()
    {
        int? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.VorlageIdChanged, (int? id) => gemeldet = id)
            .Add(x => x.Pruefzeile, new Pruefstand("✓", "geprüft, 23 Platzhalter, keine Befunde")));

        BerichtSeiteKiSicht sicht = cut.Instance.Assistentensicht;
        Assert.Equal(1, sicht.Vorlage);
        Assert.Equal(new[] { "1", "2", "3" }, sicht.VorlageWahl.Select(w => w.Schluessel));
        Assert.Equal("Kurzbericht Kunde", sicht.VorlageWahl[1].Text);
        Assert.Equal("geprüft, 23 Platzhalter, keine Befunde", sicht.Pruefzeile);

        cut.InvokeAsync(() => sicht.Vorlage = 2);
        cut.WaitForAssertion(() => Assert.Equal(2, gemeldet));
        Assert.Equal(2, sicht.Vorlage);

        // Ein gesperrter Eintrag lehnt benannt ab - mit seinem eigenen Grund.
        var fehler = Assert.Throws<InvalidOperationException>(() => sicht.Vorlage = 3);
        Assert.Equal(FEHLT, fehler.Message);
    }

    /// <summary>
    /// <b>Das Katalogfeld „vorlage“ an der Maskenbrücke</b> (BV-E1): eine Wahl aus der
    /// Vorlagenliste der Seite — gelesen wird die Id der Hülle, gesetzt über denselben Weg wie das
    /// Auswahlfeld; der gesperrte Eintrag wird mit seinem Grund abgelehnt.
    /// </summary>
    [Fact]
    public void Das_Katalogfeld_vorlage_liest_und_setzt_ueber_die_Maskenbruecke()
    {
        int? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.VorlageIdChanged, (int? id) => gemeldet = id));

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BERICHTSEITE));
        KiFeldzugang vorlage = KiMaskenbruecke.Feldzugang(KiMaskennamen.BERICHTSEITE, "vorlage");
        Assert.NotNull(vorlage);
        Assert.True(vorlage.Setzbar);
        Assert.Equal(new[] { "1", "2", "3" }, vorlage.Wahleintraege().Select(w => w.Schluessel));
        Assert.Equal(1, Convert.ToInt32(vorlage.Lesen()));

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(vorlage, "2");
        Assert.True(wahl.Ok, wahl.Grund);
        cut.InvokeAsync(() => vorlage.Setzen(wahl.Wert));
        cut.WaitForAssertion(() => Assert.Equal(2, gemeldet));

        KiFeldumsetzung gesperrt = KiFeldwandler.Wandle(vorlage, "3");
        Assert.True(gesperrt.Ok, gesperrt.Grund);
        var fehler = Assert.ThrowsAny<Exception>(() => vorlage.Setzen(gesperrt.Wert));
        Assert.Contains(FEHLT, fehler.Message + (fehler.InnerException?.Message ?? ""));
    }

    // =====================================================================
    // BV-E7: die Zeile „Excel-Vorlage"
    // =====================================================================

    private static readonly Vorlagenzeile OhneExcel = new(10, "Ohne Vorlage (EPOS-Plan)");
    private static readonly Vorlagenzeile Kennzahlmappe = new(11, "Kennzahlmappe");

    /// <summary>
    /// BV-E7 (Konzept 10.2, Zeile „Excel-Vorlage“): Die Zeile steht nur, wenn eine Mappe entsteht (Ausgabe Excel oder
    /// Beide) — dann mit Auswahlfeld, Prüfzeile und „anzeigen“; ein Wechsel meldet die Id, der Assistent liest und setzt
    /// dieselbe Wahl. Bei Ausgabe Word fehlt sie, und der Assistent lehnt benannt ab.
    /// </summary>
    [Fact]
    public void Die_Zeile_Excel_Vorlage_steht_nur_mit_Mappe_und_meldet_die_Wahl()
    {
        int? gemeldet = null;
        IReadOnlyDictionary<string, object>? liste = null;
        int ausgabe = 0;
        var cut = Render<BerichtSeite>(p => p
            .Add(x => x.Laden, () => { BerichtStand s = Stand(); s.AusgabeId = ausgabe; return s; })
            .Add(x => x.Vorlagen, Drei()).Add(x => x.VorlageId, 1)
            .Add(x => x.ExcelVorlagen, new[] { OhneExcel, Kennzahlmappe })
            .Add(x => x.ExcelVorlageId, 11)
            .Add(x => x.ExcelVorlageIdChanged, (int? id) => gemeldet = id)
            .Add(x => x.ExcelPruefzeile, new Pruefstand("✖", "geprüft, 3 Platzhalter, 1 Befund", true))
            .Add(x => x.ExcelPrueflisteGaben, () => liste = new Dictionary<string, object>
            {
                ["Meldungen"] = new List<Pruefmeldungszeile>(), ["Vorlagenname"] = "Kennzahlmappe",
            }));

        // Word: keine Zeile, der Assistent lehnt ab.
        Assert.Empty(cut.FindAll(".epos-vorlage-excel"));
        Assert.False(cut.Instance.ExcelVorlagenzeileSichtbar);
        Assert.ThrowsAny<Exception>(() => cut.Instance.Assistentensicht.ExcelVorlage = 10);

        // Excel: die Zeile mit Wahl und Prüfzeile.
        ausgabe = 1;
        cut.InvokeAsync(() => cut.Instance.Auffrischen());
        IElement zeile = cut.Find(".epos-vorlage-excel");
        Assert.Contains("Excel-Vorlage:", zeile.QuerySelector(".epos-feld-text")!.TextContent);
        var optionen = zeile.QuerySelectorAll("select option");
        Assert.Equal(new[] { "Ohne Vorlage (EPOS-Plan)", "Kennzahlmappe" }, optionen.Select(o => o.TextContent.Trim()));
        Assert.True(optionen[1].HasAttribute("selected"));
        Assert.Contains(cut.FindAll(".epos-vorlage-pruefzeile"), z => z.TextContent.Contains("1 Befund"));
        Assert.Equal("geprüft, 3 Platzhalter, 1 Befund", cut.Instance.Assistentensicht.ExcelPruefzeile);
        Assert.Equal(11, cut.Instance.Assistentensicht.ExcelVorlage);

        cut.Find(".epos-vorlage-excel-anzeigen").Click();
        Assert.NotNull(liste);

        cut.Find(".epos-vorlage-excel select").Change("10");
        Assert.Equal(10, gemeldet);
        Assert.Equal(10, cut.Instance.GewaehlteExcelVorlageId);

        cut.InvokeAsync(() => cut.Instance.Assistentensicht.ExcelVorlage = 11);
        cut.WaitForAssertion(() => Assert.Equal(11, gemeldet));
        Assert.ThrowsAny<Exception>(() => cut.Instance.Assistentensicht.ExcelVorlage = 99);
    }
}

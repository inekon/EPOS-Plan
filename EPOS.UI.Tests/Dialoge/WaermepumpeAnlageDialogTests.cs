using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Detailansicht einer Wärmepumpen-Anlage (iU9-W7.4). Soll ist die Feldkarte von
/// <c>Wizard_WPItem</c>: 47 Zeilen, zwei Reiterblätter mit den Kennlinienbildern, die
/// Kostenzeile — und OHNE die Pufferspeichergruppe (Ä19).
///
/// <para><b>Seit dem 16.09.2026 stehen ZWEI Gruppen im Dialogkörper.</b> Der Block
/// „Wärmeerzeuger Spitzenlast:" heißt „Konfiguration", liegt im Baustein
/// <c>WaermepumpeKonfiguration</c> und geht über den gleichnamigen Knopf in einer
/// <c>Ueberlagerung</c> auf; die Kenndaten sind ein Modulbereich mit Knopfzeile und den
/// bearbeitbaren Stammfeldern. Die Regeln der beiden Bausteine halten
/// <c>WaermepumpeKonfigurationTests</c> und <c>WaermepumpeStammFelderTests</c>.</para>
/// </summary>
public class WaermepumpeAnlageDialogTests : EposBunitContext
{
    private static readonly byte[] BildCop = { 1, 2, 3 };
    private static readonly byte[] BildLeistung = { 4, 5, 6 };

    /// <summary>
    /// Die Stammliste führt seit <b>W7‑B‑1</b> auch den HERSTELLER — die Liste zeigt
    /// „Wahl | Hersteller | Typ", und Typ ist die Modellbezeichnung.
    /// </summary>
    private static readonly WaermepumpeStammZeile[] Stammliste =
    {
        new(1, "WP Alpha", false, "Alpha"),
        new(2, "WP Beta", false, "Beta")
    };

    public WaermepumpeAnlageDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Eine vollständig ausgefüllte Anlagenzeile — so kommt sie aus der Verwaltung.</summary>
    private static WaermepumpeAnlageDaten Voll() => new()
    {
        Bezeichner = "WP Alpha",
        IdWp = 77,                      // PROJEKT-Geräte-Id, nicht die Stamm-Id
        Vorlauf = 35,
        Ruecklauf = 28,
        Heizstab = true,
        HeizstabLeistung = 6,
        Sperrung = false,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        Nutzungszeit = 24,
        BivalenterBetrieb = false,
        Betriebsart = "",
        Abschaltpunkt = -5,
        Beschreibung = "Testgerät",
        Baujahr = 2023,
        Regelung = "stetig",
        Typ = "Luft-Wasser",
        Firma = "Alpha",
        Nennleistung = 12,
        Modulkosten = 4000,
        Volumen = 1.5,
        Solaranteil = 30,
        RendeMix = true
    };

    private static WaermepumpeStammDaten Stamm(int id) => new()
    {
        Id = id,
        Name = id == 1 ? "WP Alpha" : "WP Beta",
        Firma = id == 1 ? "Alpha" : "Beta",
        Beschreibung = id == 1 ? "Testgerät" : "Zweitgerät",
        Typ = id == 1 ? "Luft-Wasser" : "Sole-Wasser",
        Baujahr = id == 1 ? 2023 : 2019,
        Regelung = id == 1 ? "stetig" : "einstufig",
        Nennleistung = id == 1 ? 12 : 25,
        Heizstab = id == 1 ? 6 : 9,
        Modulkosten = id == 1 ? 4000 : 7000
    };

    private IRenderedComponent<WaermepumpeAnlageDialog> Aufbauen(
        WaermepumpeAnlageDaten? daten = null,
        Func<int?, int?, string?>? temperaturen = null,
        Func<bool>? kostenBereit = null,
        Func<(double, double)>? kostensumme = null,
        Func<bool, Task>? kostenOeffnen = null,
        Func<WaermepumpeAnlageDaten, Task>? energiekosten = null,
        bool wizard = false,
        bool nurLesen = false,
        Func<int, IReadOnlyList<KennlinienZeile>>? kennlinien = null,
        Func<int, IReadOnlyList<KennlinienZeile>, bool>? kennlinienAbgleichen = null,
        Action<bool>? geschlossen = null,
        IReadOnlyList<EnergietraegerWahl.Eintrag>? traegerkatalog = null,
        bool eingebettet = false,
        Func<bool, bool>? extrapolationSchreiben = null,
        bool extrapolationErlaubt = true,
        Func<int, bool>? projektkopieVorhanden = null,
        Func<int, WaermepumpeUebernahmeVorschau?>? uebernahmeVorschau = null,
        Func<int, bool, KatalogSpeicherErgebnis>? inStammUebernehmen = null)
        => Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, daten ?? Voll())
            .Add(x => x.Traegerkatalog, traegerkatalog ?? Array.Empty<EnergietraegerWahl.Eintrag>())
            .Add(x => x.Eingebettet, eingebettet)
            .Add(x => x.ExtrapolationSchreiben, extrapolationSchreiben)
            .Add(x => x.ExtrapolationErlaubt, extrapolationErlaubt)
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Vorlaeufe, _ => new[] { 35, 45, 55 })
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung))
            .Add(x => x.Stammdaten, Stamm)
            .Add(x => x.TemperaturenPruefen, temperaturen ?? ((v, r) =>
                (v is null || r is null || v <= r) ? "Die Vorlauftemperatur muss über der Rücklauftemperatur liegen." : null))
            .Add(x => x.KostenBereit, kostenBereit ?? (() => true))
            .Add(x => x.Kostensumme, kostensumme ?? (() => (12000d, 340d)))
            .Add(x => x.KostenOeffnen, kostenOeffnen)
            .Add(x => x.EnergiekostenOeffnen, energiekosten)
            .Add(x => x.Wizard, wizard)
            .Add(x => x.NurLesen, nurLesen)
            .Add(x => x.Katalog, () => new[]
            {
                // W14a-E-10 / S2.2: eine gewoehnliche Katalogfilterzeile, wie in
                // jedem anderen Katalog des Hauses.
                new Katalogfilterzeile(1, "WP Beta")
                    .MitText(Katalogfilterprofil.SpHersteller, "Beta")
                    .MitText(Katalogfilterprofil.SpBezeichner, "WP Beta")
                    .MitText(Katalogfilterprofil.SpQuelle, "Sole-Wasser")
                    .MitZahl(Katalogfilterprofil.SpNennleistung, 25, 1)
                    .MitZahl(Katalogfilterprofil.SpVlMin, 35, 0)
                    .MitZahl(Katalogfilterprofil.SpVlMax, 60, 0)
                    .MitZahl(Katalogfilterprofil.SpZuheizung, 9, 1)
                    .MitZahl(Katalogfilterprofil.SpKuehlleistung, 0.0, 1)
                    .MitZahl(Katalogfilterprofil.SpCop, 4.1, 2)
            })
            .Add(x => x.Katalogprofil, Katalogfilterprofil.Finde(Anlagenart.Waermepumpe))
            // 16.09.2026: Die Stammfelder binden an die ANLAGENDATEN und gehen mit dem OK
            // hinaus; StammSatz und StammSpeichern (der Katalogweg) sind entfallen. Neu ist
            // die Frage nach der Projektkopie — sie entscheidet über die weiche Sperre.
            .Add(x => x.ProjektkopieVorhanden, projektkopieVorhanden)
            .Add(x => x.UebernahmeVorschau, uebernahmeVorschau)
            .Add(x => x.InStammUebernehmen, inStammUebernehmen)
            .Add(x => x.Kennlinien, kennlinien)
            .Add(x => x.KennlinienAbgleichen, kennlinienAbgleichen)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<WaermepumpeAnlageDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>Der Auswahlpfad der Kostenknöpfe — drei Stück, in dieser Reihenfolge.</summary>
    private const string KOSTENKNOEPFE = ".epos-kostenleiste button.epos-knopf";

    /// <summary>Die Überlagerung „Konfiguration" öffnen — der Weg zu den acht Feldern.</summary>
    private static void KonfigurationOeffnen(IRenderedComponent<WaermepumpeAnlageDialog> cut)
        => Knopf(cut, "Konfiguration…").Click();

    /// <summary>Der Block der Konfiguration; er steht seit dem 16.09.2026 in der Überlagerung.</summary>
    private static IElement Konfiguration(IRenderedComponent<WaermepumpeAnlageDialog> cut)
        => cut.Find(".epos-ueberlagerung .epos-wp-konfiguration");

    /// <summary>Ein Knopf INNERHALB der Überlagerung — OK und Abbrechen gibt es zweimal.</summary>
    private static IElement Ueberlagerungsknopf(IRenderedComponent<WaermepumpeAnlageDialog> cut, string text)
        => cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
              .First(b => b.TextContent.Trim() == text);

    /// <summary>
    /// Das Eingabefeld zu einer Beschriftung — die Felder des Bausteins tragen keinen
    /// eigenen Haken, wohl aber ihr <c>label</c> mit der Beschriftung darin.
    /// </summary>
    private static IElement Feld(IRenderedComponent<WaermepumpeAnlageDialog> cut, string bezeichnung)
        => cut.FindAll("label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung)
              .QuerySelector("input")!;

    // =================================================================================
    // Feldbestand
    // =================================================================================

    /// <summary>
    /// <b>ZWEI Gruppen seit dem 16.09.2026.</b> „Wärmeerzeuger Spitzenlast:" ist aus dem
    /// Dialogkörper verschwunden — der Block heißt „Konfiguration" und steht in einer
    /// Überlagerung hinter dem gleichnamigen Knopf.
    /// </summary>
    [Fact]
    public void Die_zwei_Gruppen_und_die_zwei_Reiter_stehen()
    {
        var cut = Aufbauen();

        var gruppen = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Auslegung für Verteilung", "Wärmepumpen Kenndaten" }, gruppen);

        var reiter = cut.FindAll(".epos-reiter-knopf").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "COP", "Leistung" }, reiter);
    }

    [Fact]
    public void Die_Pufferspeichergruppe_wird_gar_nicht_gezeichnet()
    {
        // Ä19: Volumen, Kapazitaet, Anteil Solaranlage und rende MIX laufen im
        // Datensatz mit, gepflegt wird der Puffer in der Simulation-Konfiguration.
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        Assert.DoesNotContain("Volumen", texte);
        Assert.DoesNotContain("Kapazität", texte);
        Assert.DoesNotContain("Anteil Speicher für Solaranlage", texte);
        Assert.DoesNotContain(cut.FindAll(".epos-schalter").Select(e => e.TextContent),
                              t => t.Contains("rende MIX"));
    }

    /// <summary>
    /// <b>Die Stammfelder binden an die ANLAGENDATEN</b> (Anwenderentscheid 16.09.2026):
    /// Sie schreiben nicht mehr in den Katalog, sondern in den Feldsatz dieser Anlage —
    /// und gehen damit mit dem OK des Dialogs hinaus, wie jedes andere Feld auch.
    /// </summary>
    [Fact]
    public void Die_Stammfelder_binden_an_die_Anlagendaten_und_gehen_mit_OK_hinaus()
    {
        bool? ergebnis = null;
        var daten = Voll();
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);

        // Der Baustein zeigt die Werte der ANLAGE, nicht die eines Katalogsatzes.
        var felder = cut.FindComponent<WaermepumpeStammFelder>();
        Assert.Equal("WP Alpha", felder.Instance.Daten.Name);
        Assert.Equal(12, felder.Instance.Daten.Nennleistung);

        Feld(cut, "Nennleistung").Input("18");
        Assert.Equal(18, daten.Nennleistung);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
        Assert.Equal(18, daten.Nennleistung);
    }

    /// <summary>
    /// <b>Die KÜHLLEISTUNG ist im Anlagendialog bedienbar</b> (Anwenderentscheid
    /// 16.09.2026) — sie ist eine Spalte der Projektkopie (<c>Tab_WP.Kuehlleistung</c>)
    /// wie die Nennleistung daneben und geht denselben Weg: in den Feldsatz, mit dem OK
    /// hinaus, von dort über <c>WPCtrl.ProjektgeraetSchreiben</c> in die Zeile.
    ///
    /// <para>In der KATALOGPFLEGE bleibt das Feld ein Lesewert; das prüft
    /// <c>WaermepumpeStammFelderTests</c>.</para>
    /// </summary>
    [Fact]
    public void Die_Kuehlleistung_ist_bedienbar_und_geht_in_die_Anlagendaten()
    {
        var daten = Voll();
        daten.Kuehlleistung = 4.0;
        var cut = Aufbauen(daten);

        var felder = cut.FindComponent<WaermepumpeStammFelder>();
        Assert.True(felder.Instance.KuehlleistungAenderbar);
        Assert.Equal(4.0, felder.Instance.Daten.Kuehlleistung);

        IElement feld = Feld(cut, "Kühlleistung");
        Assert.False(feld.HasAttribute("disabled"));

        feld.Input("5,5");
        Assert.Equal(5.5, daten.Kuehlleistung);
    }

    /// <summary>
    /// <b>Der Bezeichner ist im Anlagendialog nur lesend.</b> Projektkopie und
    /// Katalogsatz kennen einander allein über den Namen, und die Wärmesenken hängen an
    /// (Typ, Bezeichner) — umbenannt wird in der Katalogverwaltung.
    /// </summary>
    [Fact]
    public void Der_Bezeichner_ist_im_Anlagendialog_nur_lesend()
    {
        var cut = Aufbauen();

        var felder = cut.FindComponent<WaermepumpeStammFelder>();
        Assert.False(felder.Instance.BezeichnerAenderbar);

        IElement name = felder.FindAll("input[type=text]")[0];
        Assert.True(name.HasAttribute("readonly"));

        // Die übrigen Textfelder bleiben bedienbar — gesperrt ist NUR der Name.
        Assert.False(felder.FindAll("input[type=text]")[1].HasAttribute("readonly"));
    }

    /// <summary>
    /// <b>Kein eigener Speichern-Knopf mehr.</b> Die Stammfelder sind Teil der
    /// Anlagendaten; ein zweiter Schreibweg neben dem OK wäre ein zweiter Zeitpunkt, zu
    /// dem etwas in der Datenbank steht.
    /// </summary>
    [Fact]
    public void Es_gibt_keinen_eigenen_Speichern_Knopf_fuer_die_Stammfelder()
    {
        var cut = Aufbauen();
        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Speichern");
    }

    // =================================================================================
    // Kennlinieneditor und „In Stamm übernehmen…" (Anwenderentscheid 16.09.2026)
    // =================================================================================

    private static WaermepumpeAnlageDaten Ungespeichert()
    {
        var d = Voll();
        d.IdWp = 1;                     // die KATALOG-Id, wie bei „Neu.."
        return d;
    }

    /// <summary>
    /// <b>Weiche Sperre, kein <c>disabled</c></b> (Hausregel „Bedienung"): Vor dem ersten
    /// Speichern gibt es keine Projektkennlinien — der Knopf MELDET den Grund, statt still
    /// nichts zu tun.
    /// </summary>
    [Fact]
    public void Kennliniendaten_ist_vor_dem_ersten_Speichern_weich_gesperrt_und_meldet()
    {
        bool gerufen = false;
        var cut = Aufbauen(Ungespeichert(),
                           projektkopieVorhanden: _ => false,
                           kennlinien: _ => { gerufen = true; return Array.Empty<KennlinienZeile>(); });

        IElement knopf = Knopf(cut, "Kennliniendaten Ansicht/Bearbeiten...");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Contains("noch nicht gespeichert", knopf.GetAttribute("title"));

        knopf.Click();

        Assert.False(gerufen);
        Assert.False(cut.Instance.KennlinieneditorOffen);
        Assert.Contains("noch nicht gespeichert", cut.Markup);
    }

    /// <summary>
    /// Mit Projektkopie öffnet der Knopf den Editor — <b>mit der PROJEKT-Id</b>
    /// (<c>Daten.IdWp</c>), nicht mit einer Katalog-Id: Gerechnet wird ausschließlich mit
    /// den Projektkennlinien.
    /// </summary>
    [Fact]
    public void Kennliniendaten_oeffnet_den_Editor_mit_der_Projekt_Id()
    {
        var gefragt = new List<int>();
        var cut = Aufbauen(projektkopieVorhanden: _ => true,
                           kennlinien: id => { gefragt.Add(id); return Array.Empty<KennlinienZeile>(); });

        IElement knopf = Knopf(cut, "Kennliniendaten Ansicht/Bearbeiten...");
        Assert.Equal("false", knopf.GetAttribute("aria-disabled"));

        knopf.Click();

        Assert.Equal(new[] { 77 }, gefragt);        // Voll().IdWp = die Projektkopie
        Assert.True(cut.Instance.KennlinieneditorOffen);
    }

    private static WaermepumpeUebernahmeVorschau Vorhanden(int andere = 3)
        => new("WP Alpha", true, false, andere);

    private IRenderedComponent<WaermepumpeAnlageDialog> MitUebernahme(
        WaermepumpeUebernahmeVorschau vorschau,
        Action<(int IdWp, bool MitKennlinien)>? uebernommen = null)
        => Aufbauen(projektkopieVorhanden: _ => true,
                    uebernahmeVorschau: _ => vorschau,
                    inStammUebernehmen: (id, mit) =>
                    {
                        uebernommen?.Invoke((id, mit));
                        return new KatalogSpeicherErgebnis(true, "Katalogsatz „WP Alpha“ überschrieben.", "WP Alpha");
                    });

    /// <summary>Die Warnung nennt den Namen UND die Zahl der anderen Projekte.</summary>
    [Fact]
    public void In_Stamm_uebernehmen_zeigt_die_Vorschau_mit_Anzahl()
    {
        var cut = MitUebernahme(Vorhanden(3));

        Knopf(cut, "In Stamm übernehmen…").Click();

        Assert.True(cut.Instance.UebernahmeOffen);
        string text = cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent;
        Assert.Contains("WP Alpha", text);
        Assert.Contains("überschrieben", text);
        Assert.Contains("3 weitere Projekte", text);

        // Der Schalter steht da und ist AUS.
        Assert.False(cut.Find(".epos-ueberlagerung input[type=checkbox]").HasAttribute("checked"));
    }

    /// <summary>
    /// <b>Der UMBENANNTE Katalogsatz wird beim Namen genannt</b> (Anwenderentscheid
    /// 16.09.2026, Schemaschritt 80).
    ///
    /// <para>Seit die Projektkopie über <c>Tab_WP.ID_Stamm</c> an ihrem Katalogsatz
    /// hängt, überlebt die Klammer eine Umbenennung im Katalog — überschrieben wird dann
    /// aber ein Satz, der ANDERS heißt als die Anlage. Stünde dort weiter nur der Name
    /// der Anlage, suchte der Anwender im Katalog nach einem Satz, den es unter diesem
    /// Namen nicht mehr gibt.</para>
    /// </summary>
    [Fact]
    public void In_Stamm_uebernehmen_nennt_den_umbenannten_Katalogsatz()
    {
        var cut = MitUebernahme(
            new WaermepumpeUebernahmeVorschau("WP Alpha", true, false, 2, "WP Alpha II"));

        Knopf(cut, "In Stamm übernehmen…").Click();

        string text = cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent;
        Assert.Contains("WP Alpha", text);
        Assert.Contains("jetzt", text);
        Assert.Contains("WP Alpha II", text);
        Assert.Contains("überschrieben", text);
        Assert.Contains("2 weitere Projekte", text);
    }

    /// <summary>
    /// GLEICHER Name heißt: der bisherige Wortlaut. Und ein LEERER
    /// <c>KatalogBezeichner</c> ist keine Umbenennung, sondern eine Vorschau, die die
    /// Angabe nicht führt — auch dann bleibt es beim bisherigen Satz.
    /// </summary>
    [Theory]
    [InlineData("WP Alpha")]
    [InlineData("")]
    public void Ohne_Umbenennung_bleibt_der_bisherige_Warntext(string katalogname)
    {
        var cut = MitUebernahme(
            new WaermepumpeUebernahmeVorschau("WP Alpha", true, false, 3, katalogname));

        Knopf(cut, "In Stamm übernehmen…").Click();

        string text = cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent;
        Assert.Contains("WP Alpha", text);
        Assert.Contains("überschrieben", text);
        Assert.DoesNotContain("jetzt", text);
    }

    /// <summary>Ohne Katalogsatz gleichen Namens wird ein neuer angelegt — und das steht da.</summary>
    [Fact]
    public void In_Stamm_uebernehmen_zeigt_den_Neu_Anlegen_Text_ohne_Katalogsatz()
    {
        var cut = MitUebernahme(new WaermepumpeUebernahmeVorschau("WP Alpha", false, false, 0));

        Knopf(cut, "In Stamm übernehmen…").Click();

        string text = cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent;
        Assert.Contains("keinen Katalogsatz", text);
        Assert.Contains("neu angelegt", text);
    }

    /// <summary>
    /// Ein Auslieferungssatz wird nicht überschrieben: der Grund steht da, und es gibt
    /// KEINEN Übernehmen-Knopf — ein Knopf, der nur ablehnen kann, ist ein Versprechen,
    /// das der Weg nicht halten darf.
    /// </summary>
    [Fact]
    public void In_Stamm_uebernehmen_bietet_bei_ReadOnly_kein_Uebernehmen()
    {
        bool gerufen = false;
        var cut = Aufbauen(projektkopieVorhanden: _ => true,
                           uebernahmeVorschau: _ => new WaermepumpeUebernahmeVorschau("WP Alpha", true, true, 0),
                           inStammUebernehmen: (_, _) =>
                           {
                               gerufen = true;
                               return new KatalogSpeicherErgebnis(true, "", "");
                           });

        Knopf(cut, "In Stamm übernehmen…").Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Contains("Auslieferungssätze", ueberlagerung.QuerySelector(".epos-warnbanner")!.TextContent);
        Assert.DoesNotContain(ueberlagerung.QuerySelectorAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Übernehmen");
        Assert.Null(ueberlagerung.QuerySelector("input[type=checkbox]"));
        Assert.False(gerufen);
    }

    /// <summary>„Übernehmen" ruft die Gabe — mit der Geräte-Id und dem Stand des Schalters.</summary>
    [Fact]
    public void Uebernehmen_ruft_die_Gabe_mit_dem_Kennlinien_Schalter()
    {
        var gerufen = new List<(int IdWp, bool MitKennlinien)>();
        var cut = MitUebernahme(Vorhanden(), gerufen.Add);

        Knopf(cut, "In Stamm übernehmen…").Click();
        cut.Find(".epos-ueberlagerung input[type=checkbox]").Change(true);
        Ueberlagerungsknopf(cut, "Übernehmen").Click();

        Assert.Equal(new[] { (77, true) }, gerufen);
        Assert.False(cut.Instance.UebernahmeOffen);
        Assert.Contains("überschrieben", cut.Markup);
    }

    /// <summary>„Abbrechen" ruft nichts — die Überlagerung schließt, der Katalog bleibt.</summary>
    [Fact]
    public void Abbrechen_der_Uebernahme_ruft_nichts()
    {
        var gerufen = new List<(int, bool)>();
        var cut = MitUebernahme(Vorhanden(), gerufen.Add);

        Knopf(cut, "In Stamm übernehmen…").Click();
        Ueberlagerungsknopf(cut, "Abbrechen").Click();

        Assert.Empty(gerufen);
        Assert.False(cut.Instance.UebernahmeOffen);
    }

    /// <summary>Ohne Delegat kein Knopf (Hausregel seit W2).</summary>
    [Fact]
    public void Ohne_Uebernahmeweg_gibt_es_den_Knopf_In_Stamm_nicht()
    {
        var cut = Aufbauen();
        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "In Stamm übernehmen…");
    }

    /// <summary>
    /// „Parameter Bearbeiten…" gibt es nicht mehr (16.09.2026) — der Knopf öffnete die
    /// ganze Stammdatenpflege in einer Überlagerung, nur um ein Feldraster zu zeigen.
    /// </summary>
    [Fact]
    public void Den_Knopf_Parameter_Bearbeiten_gibt_es_nicht_mehr()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()),
                              t => t.StartsWith("Parameter Bearbeiten", StringComparison.Ordinal));
        Assert.Empty(cut.FindComponents<WaermepumpeStammDialog>());
    }

    /// <summary>
    /// <b>Die Knopfzeile steht direkt unter dem Kenndaten-Kopf</b> (16.09.2026, dieselbe
    /// Bauart wie in den sechs Erzeugerdialogen): links Kosten und Konfiguration, rechts
    /// der Kennlinieneditor, dazwischen der Füller.
    /// </summary>
    [Fact]
    public void Die_Knopfzeile_steht_direkt_unter_dem_Kenndaten_Kopf()
    {
        var cut = Aufbauen(kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask,
                           kennlinien: _ => Array.Empty<KennlinienZeile>());

        var gruppe = cut.FindAll(".epos-gruppenkopf-koerper")[1];
        var erstes = gruppe.Children[0];

        // 17.09.2026: Links steht die KostenKnoepfeLeiste der sechs Erzeugerdialoge
        // (drei Knoepfe) statt des einen Sammelknopfs "Kosten bearbeiten…".
        Assert.Contains("epos-leiste", erstes.ClassName);
        Assert.Equal(new[] { "Investitionskosten…", "Betriebskosten…", "Energiekosten…",
                             "Konfiguration…",
                             "Kennliniendaten Ansicht/Bearbeiten..." },
                     erstes.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray());
        Assert.NotNull(erstes.QuerySelector(".epos-leiste-fueller"));
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Designer()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        // Die Stammfelder der Anlage und ihre Auslegungsfelder.
        foreach (string soll in new[]
                 {
                     "Name", "Hersteller", "Beschreibung", "Wärmepumpentyp", "Leistungsstufen",
                     "Aufstellung", "Baujahr", "Nennleistung", "Heizstab", "Kühlleistung",
                     "Vorlauf", "Rücklauf", "Nutzungsdauer"
                 })
            Assert.Contains(soll, texte);

        // EIN Heizstabfeld, nicht zwei (16.09.2026): Das eigene Feld der Anlage
        // („Leistung Heizstab") ist mit dem des Bausteins VERSCHMOLZEN — beide schreiben
        // dieselbe Spalte Tab_WP.Heizung, und zwei gleich beschriftete Felder in einem
        // Block waren nicht auseinanderzuhalten.
        Assert.DoesNotContain("Leistung Heizstab", texte);
        Assert.Single(texte, t => t == "Heizstab");

        // Die Konfigurationsfelder stehen NICHT mehr im Dialogkoerper.
        Assert.DoesNotContain("Sperrzeit von", texte);
        Assert.DoesNotContain("Wärmeerzeuger Spitzenlast:",
                              cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()));

        KonfigurationOeffnen(cut);
        Assert.Contains("Sperrzeit von",
                        cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("Wärmepumpenleistung / maximale Betriebszeit:",
                        cut.FindAll(".epos-formulargruppe-titel").Select(e => e.TextContent.Trim()));
        Assert.Contains("Bivalenter Betrieb",
                        cut.FindAll(".epos-schalter").Select(e => e.TextContent.Trim()));
    }

    /// <summary>Die Maske ist lokalisiert (W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.TitelText, "Detail view")
            .Add(x => x.GruppeKonfiguration, "Configuration")
            .Add(x => x.BtnKonfigurationText, "Configuration…")
            .Add(x => x.LabelNutzungszeit, "Duration of use"));

        Assert.Equal("Detail view", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Duration of use", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Configuration…").Click();
        Assert.Equal("Configuration", cut.Find(".epos-ueberlagerung-titel").TextContent);
    }

    // =================================================================================
    // Die stille Vorwahl (Ä21) und die Nutzerwahl (Ä23)
    // =================================================================================

    [Fact]
    public void Der_Aufbau_laesst_die_Projekt_Geraete_Id_stehen()
    {
        // Ä21: Der Vorlaeufer musste sich dafuer mit m_bStilleFuellung gegen seinen
        // eigenen Auswahl-Handler wehren. Hier gibt es das Problem nicht.
        var daten = Voll();
        Aufbauen(daten);
        Assert.Equal(77, daten.IdWp);
    }

    [Fact]
    public void Eine_Nutzerwahl_wechselt_die_Id_UND_die_Stammfelder()
    {
        // Ä23: Sonst truege das Listenobjekt nach einem Wechsel weiter die
        // Nennleistung der vorherigen Wahl, und die Verwaltungsliste zeigte 0 kW.
        var daten = Voll();
        var cut = Aufbauen(daten);

        cut.FindAll(".epos-raster tbody tr button")[1].Click();   // WP Beta

        Assert.Equal(2, daten.IdWp);
        Assert.Equal("WP Beta", daten.Bezeichner);
        Assert.Equal(25, daten.Nennleistung);
        Assert.Equal("einstufig", daten.Regelung);
        Assert.Equal(7000, daten.Modulkosten);
    }

    [Fact]
    public void Die_Vorlaufliste_kommt_aus_den_Kennlinien()
    {
        var cut = Aufbauen();
        var stufen = cut.FindAll(".epos-gruppenkopf-koerper")[0]
                        .QuerySelectorAll("select option").Select(o => o.TextContent).ToList();
        Assert.Equal(new[] { "35", "45", "55" }, stufen);
    }

    [Fact]
    public void Ein_Vorlauf_ausserhalb_der_Kennlinien_bleibt_stehen()
    {
        // A-16: Der Vorlaeufer hatte eine frei beschreibbare ComboBox.
        var daten = Voll();
        daten.Vorlauf = 60;
        var cut = Aufbauen(daten);

        var stufen = cut.FindAll(".epos-gruppenkopf-koerper")[0]
                        .QuerySelectorAll("select option").Select(o => o.TextContent).ToList();
        Assert.Equal(new[] { "60", "35", "45", "55" }, stufen);
    }

    [Fact]
    public void Der_Ruecklauf_bleibt_frei_eingebbar_und_nennt_die_Vorschlaege()
    {
        // A-18: RUECKLAUF_VORSCHLAEGE ist ausdruecklich eine Vorschlagsliste ohne
        // Grenzwirkung - fuer 35/28 gab es frueher gar keinen Eintrag.
        var daten = Voll();
        var cut = Aufbauen(daten);

        var auslegung = cut.FindAll(".epos-gruppenkopf-koerper")[0];
        auslegung.QuerySelectorAll("input")[0].Input("26");
        Assert.Equal(26, daten.Ruecklauf);

        Assert.Contains("20, 22, 25, 28, 30, 32, 35, 40, 45",
                        cut.FindAll(".epos-herleitung").Select(e => e.TextContent).First(t => t.Contains("20, 22")));
    }

    // =================================================================================
    // Sichtbarkeitsregeln
    // =================================================================================

    /// <summary>
    /// Die Sichtbarkeitsregeln selbst hält seit dem 16.09.2026
    /// <c>WaermepumpeKonfigurationTests</c>; hier steht, dass der Dialog sie durch die
    /// Überlagerung hindurch zeigt.
    /// </summary>
    [Fact]
    public void Die_Betriebsart_erscheint_erst_mit_bivalentem_Betrieb()
    {
        var cut = Aufbauen();
        KonfigurationOeffnen(cut);

        Assert.DoesNotContain("Betriebsart",
                              cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        Konfiguration(cut).QuerySelectorAll(".epos-schalter input[type=checkbox]")[2]
                          .Change(true);                                  // Bivalenter Betrieb

        Assert.Contains("Betriebsart", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    [Fact]
    public void Die_Betriebsarten_sind_die_Steuerwerte_aus_DbWerte()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten);
        KonfigurationOeffnen(cut);

        var werte = Konfiguration(cut).QuerySelectorAll("select option")
                                      .Select(o => o.TextContent).ToList();

        // Der leere erste Eintrag ist der Platzhalter - er entspricht der leeren
        // ComboBox des Vorlaeufers, bei der btn_Beenden "Bitte Betriebsart
        // auswaehlen!" meldete.
        Assert.Equal(new[] { "", DbWerte.WP_BETRIEBSART_ALTERNATIV,
                             DbWerte.WP_BETRIEBSART_PARALLEL,
                             DbWerte.WP_BETRIEBSART_TEILPARALLEL }, werte);
    }

    // =================================================================================
    // Die Kostenleiste
    //
    // 17.09.2026 (Anwenderentscheid „angleichen!"): Die Wärmepumpe trägt dieselbe
    // KostenKnoepfeLeiste wie die sechs Erzeugerdialoge. Bis dahin stand hier EIN Knopf
    // „Kosten bearbeiten…", der die Kostenverwaltung immer auf der Investitionsseite
    // aufschlug, und gar kein Weg „Energiekosten…" — obwohl die Wärmepumpe Strom bezieht.
    // =================================================================================

    /// <summary>
    /// Der Leerlauf, den es zu vermeiden gilt: Ohne Wege der Hülle steht KEIN Knopf.
    /// Die Summenzeile darunter bleibt — sie ist keine Schaltfläche.
    /// </summary>
    [Fact]
    public void Ohne_Wege_bleibt_die_Kostenleiste_leer()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()),
                              t => t == "Kosten bearbeiten…");
    }

    [Fact]
    public void Mit_den_Wegen_stehen_die_drei_Knoepfe()
    {
        var cut = Aufbauen(kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask);

        var knoepfe = cut.FindAll(KOSTENKNOEPFE);
        Assert.Equal(3, knoepfe.Count);
        Assert.Equal("Investitionskosten…", knoepfe[0].TextContent);
        Assert.Equal("Betriebskosten…", knoepfe[1].TextContent);
        Assert.Equal("Energiekosten…", knoepfe[2].TextContent);
    }

    /// <summary>
    /// Beide Kostenknöpfe führen in dieselbe Maske dieser EINEN Anlage und
    /// unterscheiden sich nur im Schalter <c>betrieb</c> — genau der Parameter, den
    /// der Sammelknopf nie setzte (er schlug immer die Investitionsseite auf).
    /// </summary>
    [Fact]
    public void Invest_und_Betrieb_gehen_denselben_Weg_mit_dem_Schalter()
    {
        var gerufen = new List<bool>();
        var cut = Aufbauen(kostenOeffnen: b => { gerufen.Add(b); return Task.CompletedTask; },
                           energiekosten: _ => Task.CompletedTask);

        cut.FindAll(KOSTENKNOEPFE)[0].Click();
        cut.FindAll(KOSTENKNOEPFE)[1].Click();

        Assert.Equal(new[] { false, true }, gerufen.ToArray());
    }

    /// <summary>
    /// „Energiekosten…" nimmt den FELDSATZ mit — Träger und Gerät dieser Anlage. Sie
    /// stehen nicht im Modell der Hülle: Die Trägerwahl der Überlagerung
    /// „Konfiguration" ändert sie, lange bevor ein OK sie schreibt.
    /// </summary>
    [Fact]
    public void Energiekosten_nimmt_Traeger_und_Geraet_der_Anlage_mit()
    {
        WaermepumpeAnlageDaten? mitgegeben = null;
        var daten = Voll();
        daten.CarrierId = 5;

        var cut = Aufbauen(daten: daten,
                           kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: d => { mitgegeben = d; return Task.CompletedTask; });

        cut.FindAll(KOSTENKNOEPFE)[2].Click();

        Assert.NotNull(mitgegeben);
        Assert.Equal(5, mitgegeben!.CarrierId);
        Assert.Equal(77, mitgegeben!.IdWp);
    }

    /// <summary>
    /// Nur einer der beiden Wege belegt: Dann steht auch nur sein Knopf
    /// (die Regel der <c>KostenKnoepfeLeiste</c>, hier über den Dialog geprüft).
    /// </summary>
    [Fact]
    public void Ein_fehlender_Weg_nimmt_seine_Knoepfe_mit()
    {
        var cut = Aufbauen(energiekosten: _ => Task.CompletedTask);

        var knoepfe = cut.FindAll(KOSTENKNOEPFE);
        Assert.Single(knoepfe);
        Assert.Equal("Energiekosten…", knoepfe[0].TextContent);
    }

    /// <summary>
    /// Im ASSISTENTEN fehlt die Leiste ganz: Dort gibt es das Projekt noch nicht, zu
    /// dem Kostenpositionen und Projektträger gehörten. Dieselbe Weiche wie in den
    /// sechs Erzeugerdialogen — die Wärmepumpen Verwaltung reicht den Schalter durch.
    /// </summary>
    [Fact]
    public void Im_Assistenten_fehlt_die_Kostenleiste()
    {
        var cut = Aufbauen(wizard: true,
                           kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask);

        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
    }

    [Fact]
    public void Mit_Anlagenzeile_zeigt_die_Kostenzeile_die_Summen()
    {
        var cut = Aufbauen(kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask);

        Assert.Equal(3, cut.FindAll(KOSTENKNOEPFE).Count);
        Assert.Equal("Invest 12.000 € · Betrieb 340 €/a",
                     cut.Find(".epos-kostenleiste-hinweis").TextContent.Trim());
    }

    /// <summary>
    /// Ä22: Kosten und Träger hängen an der ANLAGENZEILE; bei einer noch nicht
    /// gespeicherten Neuanlage gibt es sie nicht. Der Vorläufer sperrte den Knopf und
    /// erklärte es im Tooltip; die Leiste kennt kein <c>disabled</c> — der Knopf
    /// entfällt, und die Herleitungszeile nennt den Grund weiterhin im Klartext (ein
    /// Tooltip ist auf einem Berührungsgerät ohnehin nicht erreichbar).
    /// </summary>
    [Fact]
    public void Ohne_Anlagenzeile_bleibt_die_Leiste_weg_und_der_Grund_ist_lesbar()
    {
        var cut = Aufbauen(kostenBereit: () => false,
                           kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask);

        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
        Assert.Equal("Invest — · Betrieb —", cut.Find(".epos-kostenleiste-hinweis").TextContent.Trim());
        Assert.Contains(cut.FindAll(".epos-herleitung").Select(e => e.TextContent),
                        t => t.Contains("zuerst mit OK anlegen"));
    }

    /// <summary>
    /// „Nur ansehen": Alle drei Knöpfe führen in eine BEARBEITBARE Verwaltung — in
    /// dieser Betriebsart bleiben sie weg, wie der Sammelknopf dort gesperrt war.
    /// </summary>
    [Fact]
    public void In_der_Ansicht_bleibt_die_Kostenleiste_weg()
    {
        var cut = Aufbauen(nurLesen: true,
                           kostenOeffnen: _ => Task.CompletedTask,
                           energiekosten: _ => Task.CompletedTask);

        Assert.Empty(cut.FindAll(KOSTENKNOEPFE));
    }

    // =================================================================================
    // Pruefungen beim OK
    // =================================================================================

    [Fact]
    public void OK_ohne_Betriebsart_bei_Bivalenz_meldet()
    {
        bool? ergebnis = null;
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.Null(ergebnis);
        Assert.Contains("Bitte Betriebsart auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void OK_ohne_Waermepumpe_meldet()
    {
        var daten = Voll();
        daten.Bezeichner = "";
        var cut = Aufbauen(daten);

        Knopf(cut, "OK").Click();
        Assert.Contains("Bitte Wärmepumpe auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Vorlauf_kleiner_gleich_Ruecklauf_meldet_aus_der_Kernpruefung()
    {
        var daten = Voll();
        daten.Vorlauf = 35;
        daten.Ruecklauf = 35;
        var cut = Aufbauen(daten);

        Knopf(cut, "OK").Click();
        Assert.Contains("über der Rücklauftemperatur", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Jede_der_vier_Pflicht_Ganzzahlen_wird_beim_Namen_genannt()
    {
        (string Feld, Action<WaermepumpeAnlageDaten> Leeren)[] faelle =
        {
            ("Sperrzeit von",     d => d.SperrzeitVon = null),
            ("Sperrzeit bis",     d => d.SperrzeitBis = null),
            ("Nutzungsdauer",     d => d.Nutzungszeit = null),
            ("Leistung Heizstab", d => d.HeizstabLeistung = null)
        };

        foreach (var fall in faelle)
        {
            var daten = Voll();
            fall.Leeren(daten);

            bool? ergebnis = null;
            var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);
            Knopf(cut, "OK").Click();

            Assert.Null(ergebnis);
            Assert.Contains(fall.Feld, cut.Find(".epos-warnbanner").TextContent);
        }
    }

    [Fact]
    public void Die_Bivalenztemperatur_darf_leer_bleiben()
    {
        bool? ergebnis = null;
        var daten = Voll();
        daten.Abschaltpunkt = null;
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);
    }

    [Fact]
    public void OK_meldet_true_wenn_alles_steht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);
    }

    // =================================================================================
    // Ueberlagerungen und Tastatur
    // =================================================================================

    [Fact]
    public void Der_Modulkatalog_setzt_die_Wahl()
    {
        var daten = Voll();
        var cut = Aufbauen(daten);

        Knopf(cut, "📋  Modul-Katalog...").Click();
        Assert.True(cut.Instance.KatalogOffen);

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        ueberlagerung.QuerySelector(".epos-raster tbody tr button")!.Click();
        ueberlagerung.QuerySelectorAll("button")
                     .First(b => b.TextContent.Trim() == "✔ Auswahl übernehmen").Click();

        Assert.Equal("WP Beta", daten.Bezeichner);
        Assert.Equal(2, daten.IdWp);
        Assert.False(cut.Instance.KatalogOffen);
    }

    [Fact]
    public void Abbrechen_und_Esc_melden_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "Abbrechen").Click();
        Assert.False(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    [Fact]
    public void Der_Dialog_schreibt_in_den_uebergebenen_Datensatz()
    {
        // Er ist die KOPIE der Huelle: Erst der OK-Zweig der Huelle uebertraegt sie in
        // das Listenobjekt, ein Abbruch verwirft sie. Hier steht fest, dass die
        // Eingabe im uebergebenen Satz ankommt.
        var daten = Voll();
        var cut = Aufbauen(daten);

        // Die Auslegung steht im Dialogkoerper...
        cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input[type=text]")[0].Input("26");
        Assert.Equal(26, daten.Ruecklauf);

        // ...die Konfiguration in ihrer Ueberlagerung, und auch sie schreibt in DENSELBEN Satz.
        KonfigurationOeffnen(cut);
        Konfiguration(cut).QuerySelectorAll("input[type=text]")[0].Input("3");

        Assert.Equal(3, daten.SperrzeitVon);
    }

    [Fact]
    public void Esc_bei_offener_Ueberlagerung_schliesst_den_Dialog_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "📋  Modul-Katalog...").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }

    /// <summary>Anwenderentscheid 15.09.2026: das Kreuz der Kopfzeile wirkt wie Esc.</summary>
    [Fact]
    public void Kreuz_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.False(ergebnis);
    }

    /// <summary>
    /// Das ✕ der Ueberlagerung „Konfiguration" wirkt wie Abbrechen: Es schliesst NUR die
    /// Ebene und schreibt den gesicherten Stand zurueck (Hausregel „✕ = Esc = Abbrechen").
    /// </summary>
    [Fact]
    public void Ueberlagerungskreuz_der_Konfiguration_schliesst_nur_die_Ebene_und_verwirft()
    {
        var daten = Voll();
        var cut = Aufbauen(daten);

        KonfigurationOeffnen(cut);
        Assert.True(cut.Instance.KonfigurationOffen);

        Konfiguration(cut).QuerySelectorAll("input[type=text]")[0].Input("7");
        Assert.Equal(7, daten.SperrzeitVon);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.KonfigurationOffen);
        Assert.Equal(0, daten.SperrzeitVon);        // der Stand beim Oeffnen
    }

    /// <summary>Das ✕ der Ueberlagerung "Modul-Katalog..." bricht ab, ohne zu uebernehmen.</summary>
    [Fact]
    public void Ueberlagerungskreuz_des_Katalogs_schliesst_ohne_Wahl_zu_uebernehmen()
    {
        var daten = Voll();
        var cut = Aufbauen(daten);

        Knopf(cut, "📋  Modul-Katalog...").Click();
        Assert.True(cut.Instance.KatalogOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.KatalogOffen);
        Assert.Equal("WP Alpha", daten.Bezeichner);
        Assert.Equal(77, daten.IdWp);
    }

    /// <summary>
    /// <b>Der Knopf „Konfiguration…" öffnet die Überlagerung mit dem Titel
    /// „Konfiguration" und den Konfigurationsfeldern</b> (Anwenderentscheid 16.09.2026).
    /// Ein Titel, eine Stelle: Die Überlagerung trägt Titel und Kreuz, der Baustein
    /// darin keinen eigenen Kopf.
    /// </summary>
    [Fact]
    public void Der_Knopf_Konfiguration_oeffnet_die_Ueberlagerung_mit_den_Feldern()
    {
        var cut = Aufbauen();
        Assert.False(cut.Instance.KonfigurationOffen);

        KonfigurationOeffnen(cut);

        Assert.True(cut.Instance.KonfigurationOffen);
        Assert.Equal("Konfiguration", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));

        var block = Konfiguration(cut);
        Assert.Contains("Heizstab mitrechnen", block.TextContent);
        Assert.Contains("Sperrzeit durch Energieversorger", block.TextContent);
        Assert.Contains("Bivalenter Betrieb", block.TextContent);
        Assert.Equal(3, block.QuerySelectorAll(".epos-wp-erklaerung").Length);

        // OK und Abbrechen der Ueberlagerung stehen DARIN, der Baustein traegt keinen Kopf.
        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Contains(ueberlagerung.QuerySelectorAll("button").Select(b => b.TextContent.Trim()),
                        t => t == "OK");
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));

        // Der Kopf der Detailansicht selbst steht weiterhin genau einmal.
        Assert.Single(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-dialog-zu"));
    }

    /// <summary>
    /// <b>Abbrechen verwirft die Änderung, OK übernimmt sie in <c>Daten</c></b>
    /// (Hausregel „Geschrieben wird im OK-Weg"). Der Baustein schreibt unmittelbar in
    /// den Satz; der Dialog sichert deshalb beim Öffnen die acht Felder.
    /// </summary>
    [Fact]
    public void Abbrechen_verwirft_die_Aenderung_OK_uebernimmt_sie()
    {
        var daten = Voll();
        var cut = Aufbauen(daten);

        KonfigurationOeffnen(cut);
        Konfiguration(cut).QuerySelectorAll("input[type=text]")[1].Input("9");   // Sperrzeit bis
        Assert.Equal(9, daten.SperrzeitBis);

        Ueberlagerungsknopf(cut, "Abbrechen").Click();

        Assert.False(cut.Instance.KonfigurationOffen);
        Assert.Equal(0, daten.SperrzeitBis);                                     // verworfen

        KonfigurationOeffnen(cut);
        Konfiguration(cut).QuerySelectorAll("input[type=text]")[1].Input("9");
        Ueberlagerungsknopf(cut, "OK").Click();

        Assert.False(cut.Instance.KonfigurationOffen);
        Assert.Equal(9, daten.SperrzeitBis);                                     // uebernommen
    }

    /// <summary>
    /// Die Prüfregeln der Konfiguration laufen im OK-Weg der Überlagerung — und halten
    /// sie offen, statt den Mangel in einem Dialog zu melden, in dem das Feld gar nicht
    /// steht.
    /// </summary>
    [Fact]
    public void OK_der_Ueberlagerung_haelt_sie_bei_einem_Mangel_offen()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        daten.Betriebsart = "";
        var cut = Aufbauen(daten);

        KonfigurationOeffnen(cut);
        Ueberlagerungsknopf(cut, "OK").Click();

        Assert.True(cut.Instance.KonfigurationOffen);
        Assert.Contains("Bitte Betriebsart auswählen!",
                        cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent);
    }

    /// <summary>
    /// Umgekehrt: Ein Mangel an einem Feld der Konfiguration ÖFFNET beim OK des Dialogs
    /// die Überlagerung — ein Band, das ein unsichtbares Feld nennt, hilft nicht weiter
    /// (dieselbe Überlegung wie W7‑B‑2).
    /// </summary>
    [Fact]
    public void OK_des_Dialogs_oeffnet_die_Konfiguration_wenn_dort_ein_Feld_fehlt()
    {
        var daten = Voll();
        daten.SperrzeitVon = null;
        var cut = Aufbauen(daten);

        Knopf(cut, "OK").Click();

        Assert.True(cut.Instance.KonfigurationOffen);
        Assert.Contains("Sperrzeit von",
                        cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent);
    }

    /// <summary>
    /// Ein Titel, eine Stelle: Die Ueberlagerung „Modul-Katalog..." trägt Titel und
    /// Kreuz; der Katalogdialog darin bekommt <c>TitelText=""</c> und zeichnet keinen
    /// zweiten Kopf.
    /// </summary>
    [Fact]
    public void Der_Titel_des_Modulkatalogs_erscheint_genau_einmal()
    {
        var cut = Aufbauen();

        Knopf(cut, "📋  Modul-Katalog...").Click();

        var ueberlagerung = cut.Find(".epos-ueberlagerung");
        Assert.Equal("📋  Modul-Katalog...", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-titel"));
        Assert.Empty(ueberlagerung.QuerySelectorAll(".epos-dialog-zu"));
        Assert.Single(ueberlagerung.QuerySelectorAll(".epos-dialog-kopf--ohnetitel"));

        Assert.Single(cut.FindAll(".epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-dialog-zu"));
    }
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Kenndatenblock der Anlage steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Die_Parameterbloecke_stehen_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);

        // Ein Zahlenfeld meldet sich als KURZES Feld, und seine Einheit steht in
        // derselben Feldzeile — im Vorbild 4 px hinter dem Feld, im Befund am
        // rechten Rand des Blocks.
        var kurz = cut.FindAll(".epos-formularraster .epos-feld--kurz");
        Assert.NotEmpty(kurz);
        Assert.Contains(kurz, f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }

    // =====================================================================
    //  W7-B-2 — „OK-Button funktioniert nicht im Dialog Detailansicht bei
    //  Aufruf über button Ändern" (Windows-Abnahme 06.09.2026)
    // =====================================================================

    /// <summary>
    /// Die Prüfung des Kerns, WÖRTLICH: <c>ProjektPuffer.TemperaturenPruefen</c>
    /// verwirft einen Rücklauf ≤ 0 °C. Die Vorrichtung oben tut das nicht — und
    /// genau darin lag der Unterschied zwischen Prüfstand und Anwenderrechner.
    /// </summary>
    private static string? WieDerKern(int? vorlauf, int? ruecklauf)
    {
        if (vorlauf is null) return "Bitte eine Vorlauftemperatur als ganze Zahl eingeben (°C).";
        if (ruecklauf is null) return "Bitte eine Rücklauftemperatur als ganze Zahl eingeben (°C).";
        if (ruecklauf <= 0) return "Die Rücklauftemperatur muss größer als 0 °C sein.";
        if (vorlauf <= ruecklauf) return "Die Vorlauftemperatur muss über der Rücklauftemperatur liegen.";
        return null;
    }

    /// <summary>
    /// EINE BESTANDSZEILE, wie sie „Ändern.." aus <c>Tab_Energieanlagen</c> holt:
    /// Kenndaten gefüllt, aber <c>Rücklauf</c> nie gepflegt (0). Der Weg „Neu.."
    /// setzt wenigstens den Vorlauf aus den Kennlinien
    /// (<c>AnlagenTemperaturen.VorlaufAusKennlinien</c>) — für den Rücklauf gibt es
    /// keine solche Regel, und eine Altzeile trägt dort schlicht die 0.
    /// </summary>
    private static WaermepumpeAnlageDaten Bestandszeile()
    {
        var d = Voll();
        d.Ruecklauf = 0;
        return d;
    }

    /// <summary>
    /// <b>Die Ursache, Teil 1.</b> Der OK-Knopf REAGIERT — er meldet einen Fehler.
    /// Nur stand die Meldung ganz oben im Dialog, der Knopf ganz unten in einer
    /// rollenden Überlagerung: Der Anwender drückt, sieht nichts geschehen und
    /// schließt daraus, der Knopf sei tot. Seit W7‑B‑2 steht das Band DORT, WO ER
    /// HINSCHAUT — in derselben Fußleiste wie OK und Abbrechen.
    /// </summary>
    [Fact]
    public void W7_B_2_Das_Warnband_steht_bei_der_Speichernleiste()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(Bestandszeile(), temperaturen: WieDerKern,
                           geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.Null(ergebnis);
        var band = cut.Find(".epos-dialog-fuss .epos-warnbanner");
        Assert.Contains("Rücklauftemperatur", band.TextContent);
    }

    /// <summary>
    /// <b>Die Ursache, Teil 1 — die Gegenprobe.</b> Steht der Rücklauf, geht dieselbe
    /// Zeile durch. Ohne diesen Fall bewiese der Fall darüber nur, dass ein Band da
    /// ist, nicht dass es der GRUND ist.
    /// </summary>
    [Fact]
    public void W7_B_2_Mit_gepflegtem_Ruecklauf_meldet_dieselbe_Zeile_true()
    {
        bool? ergebnis = null;
        var daten = Bestandszeile();
        daten.Ruecklauf = 28;
        var cut = Aufbauen(daten, temperaturen: WieDerKern, geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.True(ergebnis);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    /// <summary>
    /// <b>Die Ursache, Teil 2.</b> Der Vorläufer <c>Wizard_WPItem</c> zeigte die
    /// Betriebsart in einer FREI BESCHREIBBAREN <c>ComboBox</c> und schrieb deren
    /// Text ungeprüft in die Spalte (Befund L0‑1). Ein <c>select</c> kann einen
    /// nicht zeichengleichen Wert nicht zeigen: Die Klappliste stand leer, der
    /// Anwender sah eine Bestandszeile ohne Betriebsart — und beim OK meldete
    /// <c>ErsterFehler</c> „Bitte Betriebsart auswählen!", sobald der Wert dabei
    /// ganz verlorenging. Seit W7‑B‑2 liest der Dialog TOLERANT
    /// (<c>DbWerte.BetriebsartOderDefault</c>).
    /// </summary>
    [Fact]
    public void W7_B_2_Ein_alter_Betriebsart_Text_steht_gewaehlt_in_der_Klappliste()
    {
        bool? ergebnis = null;
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        daten.Betriebsart = "parallelbetrieb";          // Altwert aus der Datenbank
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);
        KonfigurationOeffnen(cut);

        var betriebsart = Konfiguration(cut).QuerySelectorAll("select")
                             .First(s => s.QuerySelectorAll("option")
                                          .Any(o => o.TextContent == DbWerte.WP_BETRIEBSART_PARALLEL));
        Assert.Equal("1", betriebsart.GetAttribute("value"));   // Parallelbetrieb steht gewählt

        Ueberlagerungsknopf(cut, "OK").Click();
        Knopf(cut, "OK").Click();
        Assert.True(ergebnis);
    }

    /// <summary>
    /// Der berichtigte Steuerwert geht auch in den Datensatz — sonst zeigte die
    /// Maske „Parallelbetrieb" und die Engine läse weiter den Altwert, den sie
    /// zeichengleich vergleicht (<c>SimulationWaermepumpe</c>:980 ff.).
    /// </summary>
    [Fact]
    public void W7_B_2_Der_Altwert_wird_beim_Aufbau_auf_den_Steuerwert_gezogen()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        daten.Betriebsart = "Bivalent-teilparallel";
        Aufbauen(daten);

        Assert.Equal(DbWerte.WP_BETRIEBSART_TEILPARALLEL, daten.Betriebsart);
    }

    /// <summary>
    /// Ist der Wert gar nicht zu deuten, bleibt die Pflichtangabe offen — und die
    /// Meldung nennt sie, wieder in der Fußleiste.
    /// </summary>
    [Fact]
    public void W7_B_2_Ohne_deutbare_Betriebsart_meldet_das_Band_bei_der_Leiste()
    {
        bool? ergebnis = null;
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        daten.Betriebsart = "Monovalent";
        var cut = Aufbauen(daten, geschlossen: b => ergebnis = b);

        Knopf(cut, "OK").Click();

        Assert.Null(ergebnis);
        Assert.Contains("Bitte Betriebsart auswählen!",
                        cut.Find(".epos-dialog-fuss .epos-warnbanner").TextContent);
    }

    /// <summary>
    /// „das genannte Feld wird markiert": Das Band nennt das Feld, und das Feld
    /// selbst trägt die Mängelklasse — ein Band allein hilft in einem Dialog mit
    /// dreißig Feldern nicht weiter.
    ///
    /// <para>Geprüft an der Nutzungsdauer: Sie steht im Dialogkörper. Ein Mangel an
    /// einem Feld der KONFIGURATION öffnet seit dem 16.09.2026 stattdessen deren
    /// Überlagerung (<c>OK_des_Dialogs_oeffnet_die_Konfiguration_wenn_dort_ein_Feld_fehlt</c>).</para>
    /// </summary>
    [Fact]
    public void W7_B_2_Das_bemaengelte_Feld_ist_markiert()
    {
        var daten = Voll();
        daten.Nutzungszeit = null;
        var cut = Aufbauen(daten);

        Knopf(cut, "OK").Click();

        Assert.Contains("Nutzungsdauer", cut.Find(".epos-dialog-fuss .epos-warnbanner").TextContent);
        var mangel = cut.Find(".epos-feldhuelle--mangel .epos-feld");
        Assert.Contains("Nutzungsdauer", mangel.TextContent);
    }

    // =====================================================================
    //  W7-B-1 — Spalten „Wahl | Hersteller | Typ"
    // =====================================================================

    /// <summary>
    /// <b>W7‑B‑1</b> (Windows-Abnahme 06.09.2026): „Anstelle Name sollte Typ stehen,
    /// es fehlt der Hersteller (vor Typ)." <c>Typ</c> ist die MODELLBEZEICHNUNG;
    /// der Wärmepumpentyp „Luft-Wasser" bleibt im Kenndatenblock.
    /// </summary>
    [Fact]
    public void W7_B_1_Die_Auswahlliste_fuehrt_Wahl_Hersteller_Typ()
    {
        var cut = Aufbauen();
        var kopf = cut.FindAll(".epos-wp-auswahl .epos-raster th")
                      .Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(new[] { "Wahl", "Hersteller", "Typ" }, kopf);

        var zellen = cut.FindAll(".epos-wp-auswahl .epos-raster tbody tr")[0]
                        .QuerySelectorAll("td").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal("Alpha", zellen[1]);        // Hersteller
        Assert.Equal("WP Alpha", zellen[2]);     // Typ = Modellbezeichnung
    }

    // =====================================================================
    //  W7-E-2 — Anordnung nach dem alten Wizard_WPItem
    // =====================================================================

    /// <summary>
    /// <b>W7‑E‑2:</b> „Dialoganordnung sehr unübersichtlich — versuche den Dialog
    /// angelehnt an die alte Version zu gestalten." Drei Spalten wie im Vorbild:
    /// links die Auswahl, in der Mitte die Auslegung, rechts Kenndaten und Kennlinien.
    ///
    /// <para>Seit dem 16.09.2026 steht links NUR noch die Auswahl — die Gruppe
    /// „Wärmeerzeuger Spitzenlast:" ist als „Konfiguration" in die Überlagerung
    /// gewandert.</para>
    /// </summary>
    [Fact]
    public void W7_E_2_Die_drei_Spalten_stehen_in_der_Reihenfolge_des_Vorbilds()
    {
        var cut = Aufbauen();
        var spalten = cut.FindAll(".epos-wp-spalten > div");

        Assert.Equal(3, spalten.Count);
        Assert.NotNull(spalten[0].QuerySelector(".epos-wp-auswahl"));
        Assert.Empty(spalten[0].QuerySelectorAll(".epos-gruppenkopf-titel"));
        Assert.Contains("Auslegung für Verteilung",
                        spalten[1].QuerySelectorAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()));
        Assert.Contains("Wärmepumpen Kenndaten",
                        spalten[2].QuerySelectorAll(".epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()));
        Assert.NotEmpty(spalten[2].QuerySelectorAll(".epos-reiter-knopf"));
    }

    /// <summary>
    /// Die DREI farbigen Erklärkästen des Vorbilds (label21 grün, label22 gelb,
    /// label23 türkis) — sie standen dort ALLE DREI und immer, weil sie die Wahl
    /// der Betriebsart erklären. Die Texte sind wortgleich aus der alten
    /// <c>.resx</c> und stehen in <c>MyResource</c>; seit dem 16.09.2026 zeichnet sie
    /// der Baustein <c>WaermepumpeKonfiguration</c> in der Überlagerung.
    /// </summary>
    [Fact]
    public void W7_E_2_Die_drei_farbigen_Erklaerkaesten_stehen_in_der_Konfiguration()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-wp-erklaerung"));

        KonfigurationOeffnen(cut);
        var kaesten = Konfiguration(cut).QuerySelectorAll(".epos-wp-erklaerung");

        Assert.Equal(3, kaesten.Length);
        Assert.Contains("epos-wp-erklaerung--gruen", kaesten[0].ClassName);
        Assert.Contains("epos-wp-erklaerung--gelb", kaesten[1].ClassName);
        Assert.Contains("epos-wp-erklaerung--tuerkis", kaesten[2].ClassName);

        Assert.Contains("bivalent-alternativen", kaesten[0].TextContent);
        Assert.Contains("bivalent-parallelen", kaesten[1].TextContent);
        Assert.Contains("bivalent-teilparallele", kaesten[2].TextContent);
    }

    /// <summary>
    /// Der Titel steht EINMAL. Das Bildschirmfoto der Abnahme zeigte ihn zweimal —
    /// als Titel der <c>Ueberlagerung</c> UND als Titel der Komponente darin. Wer
    /// <c>TitelText</c> leer übergibt, bekommt keinen zweiten Kopf.
    /// </summary>
    [Fact]
    public void W7_E_2_Ohne_TitelText_zeichnet_der_Dialog_keinen_zweiten_Kopf()
    {
        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
        // Anwenderentscheid 15.09.2026: ohne Titel auch kein Kreuz - die Ueberlagerung
        // traegt beides.
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    /// <summary>
    /// Die Häkchen tragen wieder ihren EIGENEN Text; „Wärmeerzeuger Spitzenlast:"
    /// und „Wärmepumpenleistung / maximale Betriebszeit:" sind im Vorbild
    /// Überschriften (label7, label19), keine Beschriftungen der Kästchen.
    /// </summary>
    [Fact]
    public void W7_E_2_Die_Haekchen_tragen_die_Texte_des_Vorbilds()
    {
        var cut = Aufbauen();
        KonfigurationOeffnen(cut);

        var schalter = Konfiguration(cut).QuerySelectorAll(".epos-schalter")
                                         .Select(e => e.TextContent.Trim()).ToList();

        Assert.Contains("Heizstab mitrechnen", schalter);
        Assert.Contains("Sperrzeit durch Energieversorger", schalter);
        Assert.Contains("Bivalenter Betrieb", schalter);
    }

    // =================================================================================
    // W7‑B‑3 — Herkunft der Kennlinien (Windows-Abnahme V2 vom 07.09.2026)
    // =================================================================================

    /// <summary>
    /// Der Regelfall: Die Bilder kommen aus der PROJEKTKOPIE. Dann sagt der Dialog
    /// nichts dazu — eine Herleitung, die immer da steht, sagt nichts mehr — und der
    /// Knopf erscheint nicht.
    /// </summary>
    [Fact]
    public void W7_B_3_Projektkennlinien_stehen_ohne_Herleitung_da()
    {
        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung,
                                                          Kennlinienherkunft.Projekt))
            .Add(x => x.KennlinienUebernehmen, _ => 16));

        Assert.Empty(cut.FindAll(".epos-wp-kennlinienquelle .epos-herleitung"));
        Assert.DoesNotContain("Kennlinien aus dem Katalog übernehmen", cut.Markup);
        Assert.Single(cut.FindAll(".epos-chartbild"));   // der Reiter zeichnet nur das aktive Blatt
    }

    /// <summary>
    /// <b>Der Rückfall.</b> Fehlen die Projektkennlinien, zeigt der Dialog die des
    /// Katalogsatzes — und sagt es in einer Herleitungszeile. Der Lauf rechnet
    /// ausschließlich mit den Projektkennlinien; eine Katalogkennlinie ohne diesen
    /// Satz behauptete einen Stand, den die Simulation nicht kennt.
    /// </summary>
    [Fact]
    public void W7_B_3_Katalogkennlinien_tragen_ihre_Herleitung()
    {
        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung,
                                                          Kennlinienherkunft.Katalog, true))
            .Add(x => x.KennlinienUebernehmen, _ => 16));

        Assert.Single(cut.FindAll(".epos-wp-kennlinienquelle .epos-herleitung"));
        Assert.Contains("Katalogsatzes gleichen Namens", cut.Find(".epos-wp-kennlinienquelle .epos-herleitung").TextContent);
        Assert.Single(cut.FindAll(".epos-chartbild"));   // der Reiter zeichnet nur das aktive Blatt
    }

    /// <summary>
    /// Der Knopf ruft den Weg mit der GERÄTE-Id und frischt danach auf — beim zweiten
    /// Zeichnen liefert der Delegat die Projektkennlinien, und die Herleitung ist
    /// weg. Genau das sieht der Anwender nach dem Druck.
    /// </summary>
    [Fact]
    public void W7_B_3_Der_Knopf_holt_die_Kennlinien_und_raeumt_die_Herleitung_weg()
    {
        int gerufenMit = 0;
        bool geholt = false;

        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Bilder, _ => geholt
                ? new KennlinienBilder(BildCop, BildLeistung, Kennlinienherkunft.Projekt)
                : new KennlinienBilder(BildCop, BildLeistung, Kennlinienherkunft.Katalog, true))
            .Add(x => x.KennlinienUebernehmen, id =>
            {
                gerufenMit = id;
                geholt = true;
                return 16;
            }));

        Knopf(cut, "Kennlinien aus dem Katalog übernehmen").Click();

        Assert.Equal(77, gerufenMit);            // die PROJEKT-Geraete-Id, nicht die Stamm-Id
        Assert.Empty(cut.FindAll(".epos-wp-kennlinienquelle .epos-herleitung"));
        Assert.Contains("16 Stützstellen", cut.Markup);
    }

    /// <summary>
    /// Ohne Delegat kein Knopf (Hausregel seit W2) — und ohne Gerätekopie im Projekt
    /// ebenso wenig: Vor dem ersten Speichern gibt es nichts nachzuholen.
    /// </summary>
    [Fact]
    public void W7_B_3_Ohne_Ziel_erscheint_der_Knopf_nicht()
    {
        var ohneDelegat = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung,
                                                          Kennlinienherkunft.Katalog, true)));

        Assert.Single(ohneDelegat.FindAll(".epos-wp-kennlinienquelle .epos-herleitung"));
        Assert.DoesNotContain("Kennlinien aus dem Katalog übernehmen", ohneDelegat.Markup);

        var nichtNachholbar = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung,
                                                          Kennlinienherkunft.Katalog))
            .Add(x => x.KennlinienUebernehmen, _ => 16));

        Assert.Single(nichtNachholbar.FindAll(".epos-wp-kennlinienquelle .epos-herleitung"));
        Assert.DoesNotContain("Kennlinien aus dem Katalog übernehmen", nichtNachholbar.Markup);
    }

    /// <summary>
    /// Gab es nichts zu holen — kein Katalogsatz gleichen Namens —, meldet der Dialog
    /// das als WARNUNG statt still nichts zu tun.
    /// </summary>
    [Fact]
    public void W7_B_3_Ohne_Katalogsatz_meldet_der_Knopf_es()
    {
        var cut = Render<WaermepumpeAnlageDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.Stammliste, () => Stammliste)
            .Add(x => x.Bilder, _ => new KennlinienBilder(BildCop, BildLeistung,
                                                          Kennlinienherkunft.Katalog, true))
            .Add(x => x.KennlinienUebernehmen, _ => 0));

        Knopf(cut, "Kennlinien aus dem Katalog übernehmen").Click();

        Assert.Contains("keinen Katalogsatz gleichen Namens", cut.Markup);
    }

    // =================================================================================
    // ET-5 (Anwenderentscheid 08.09.2026): der Energietraeger der Waermepumpe, Gruppe > Art
    // =================================================================================

    [Fact]
    public void Die_Traegerwahl_zeigt_Gruppe_und_Art_und_schreibt_in_die_Daten()
    {
        WaermepumpeAnlageDaten daten = Voll();
        daten.CarrierId = 60;
        var cut = Aufbauen(daten, traegerkatalog: new[]
        {
            new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
            new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
            new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2")
        });
        KonfigurationOeffnen(cut);

        var wahl = cut.Find(".epos-traegerwahl");
        var selects = wahl.QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);
        Assert.Contains("Elektrische Energie", selects[1].InnerHtml);
        Assert.DoesNotContain("Erdgas E", selects[1].InnerHtml);

        selects[1].Change("58");
        Assert.Equal(58, daten.CarrierId);

        selects[0].Change("0");     // Gruppe Gas -> erster Traeger der Gruppe
        Assert.Equal(11, daten.CarrierId);
    }

    [Fact]
    public void Ohne_Traegerkatalog_steht_keine_Traegerwahl()
    {
        var cut = Aufbauen();
        KonfigurationOeffnen(cut);
        Assert.Empty(cut.FindAll(".epos-traegerwahl"));
    }

    // =================================================================================
    // Eingebettet in der Waermepumpen Verwaltung (W7-B-3, 08.09.2026)
    // =================================================================================

    [Fact]
    public void Eingebettet_hat_keine_eigene_OK_Leiste_und_Esc_schliesst_nicht()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(eingebettet: true, geschlossen: b => ergebnis = b);

        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t == "OK");
        Assert.DoesNotContain(cut.FindAll("button").Select(b => b.TextContent.Trim()), t => t == "Abbrechen");
        Assert.Contains("epos-dialog--eingebettet", cut.Find(".epos-dialog").ClassName);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Eingebettet schliesst der WIRT (Waermepumpen Verwaltung), nicht der Dialog selbst -
    /// wie schon kein Esc, so hier auch kein Kreuz, das still nichts täte.
    /// </summary>
    [Fact]
    public void Eingebettet_zeigt_kein_Schliesskreuz()
    {
        var cut = Aufbauen(eingebettet: true);
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Pruefen_meldet_den_Mangel_und_null_wenn_alles_steht()
    {
        var cut = Aufbauen(eingebettet: true);
        Assert.Null(cut.Instance.Pruefen());

        WaermepumpeAnlageDaten ohne = Voll();
        ohne.Bezeichner = "";
        var cut2 = Aufbauen(ohne, eingebettet: true);
        string? fehler = cut2.Instance.Pruefen();
        Assert.NotNull(fehler);
        cut2.Render();
        Assert.Contains(fehler!, cut2.Markup);
    }

    // =================================================================================
    // W7-B-3 Nachtrag (08.09.2026): eingebettet ohne Innenliste, Umstellen durch den Wirt
    // =================================================================================

    [Fact]
    public void Eingebettet_steht_keine_Innenliste_und_kein_Modul_Katalog()
    {
        var cut = Aufbauen(eingebettet: true);

        Assert.Empty(cut.FindAll(".epos-wp-auswahl"));
        Assert.Empty(cut.FindAll(".epos-raster"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("Modul-Katalog"));

        Assert.NotEmpty(Aufbauen().FindAll(".epos-wp-auswahl .epos-raster"));   // frei stehend bleibt sie
    }

    [Fact]
    public async Task Umstellen_wechselt_die_Waermepumpe_und_behaelt_die_Betriebsdaten()
    {
        WaermepumpeAnlageDaten daten = Voll();
        daten.SperrzeitVon = 14;
        daten.SperrzeitBis = 17;
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten, eingebettet: true);

        Assert.True(await cut.InvokeAsync(() => cut.Instance.WaermepumpeUmstellen("WP Beta")));
        Assert.Equal("WP Beta", daten.Bezeichner);
        Assert.Equal(2, daten.IdWp);
        Assert.Equal(14, daten.SperrzeitVon);
        Assert.True(daten.BivalenterBetrieb);

        Assert.False(await cut.InvokeAsync(() => cut.Instance.WaermepumpeUmstellen("gibt es nicht")));
        Assert.Equal("WP Beta", daten.Bezeichner);
    }

    // =================================================================================
    // W10b-B-3 (08.09.2026): der Extrapolationsschalter steht bei den Kennlinien
    // =================================================================================

    [Fact]
    public void Der_Extrapolationsschalter_steht_bei_den_Kennlinien_und_ist_vorbelegt()
    {
        var geschrieben = new List<bool>();
        var cut = Aufbauen(extrapolationSchreiben: w => { geschrieben.Add(w); return true; });

        var schalter = cut.Find(".epos-wp-spalte--rechts .epos-wp-extrapolation");
        Assert.Contains("Extrapolation der WP-Kennlinie erlauben", schalter.TextContent);
        Assert.Equal("H2", schalter.PreviousElementSibling?.TagName);
        Assert.Contains("Kenndaten Kennlinien", schalter.PreviousElementSibling!.TextContent);

        var kasten = schalter.QuerySelector("input[type=checkbox]")!;
        Assert.True(kasten.HasAttribute("checked"));
        Assert.False(kasten.HasAttribute("disabled"));

        kasten.Change(false);
        Assert.Equal(new[] { false }, geschrieben);
        Assert.False(cut.Find(".epos-wp-extrapolation input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void Die_Vorbelegung_des_Projekts_kommt_am_Extrapolationsschalter_an()
    {
        var cut = Aufbauen(extrapolationSchreiben: _ => true, extrapolationErlaubt: false);
        Assert.False(cut.Find(".epos-wp-extrapolation input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void Ohne_Schreibweg_ist_der_Extrapolationsschalter_gesperrt()
    {
        var cut = Aufbauen();
        Assert.True(cut.Find(".epos-wp-extrapolation input[type=checkbox]").HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_Fehlschlag_beim_Schreiben_laesst_den_Schalter_und_sagt_es()
    {
        var cut = Aufbauen(extrapolationSchreiben: _ => false);
        cut.Find(".epos-wp-extrapolation input[type=checkbox]").Change(false);

        Assert.True(cut.Find(".epos-wp-extrapolation input[type=checkbox]").HasAttribute("checked"));
        Assert.Contains("ließ sich nicht speichern", cut.Find(".epos-warnbanner").TextContent);
    }
}

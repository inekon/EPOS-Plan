using AngleSharp.Dom;
using Bunit;
using System.Globalization;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Emissionsfaktor-Katalog (iU9-W3.3), Vorbild
/// <c>Views/Kosten/Form_Emissionskatalog</c>.
///
/// <para>Soll ist die Feldkarte: Kopf mit Kontextzeile, Modusgruppe, die
/// Artenliste mit drei Knöpfen, die Werteliste mit vier Knöpfen, Hinweiszeile,
/// OK und Abbrechen. Dazu die beiden Editoren, die früher eigene Fenster
/// waren.</para>
/// </summary>
public class EmissionskatalogDialogTests : BunitContext
{
    public EmissionskatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;      // QuickGrid im Raster
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static EmissionsartZeile Co2 => new(1, "CO2", "Kohlendioxid", "g/kWh",
        1.0, "1", "IPCC AR6", true, true, true);

    private static EmissionsartZeile Ch4 => new(2, "CH4", "Methan", "mg/kWh",
        28.0, "28", "IPCC AR6", true, false, true);

    private static EmissionsartZeile Eigen => new(3, "NH3", "Ammoniak", "mg/kWh",
        0.0, "0", "", false, false, false);

    private static readonly EmissionsartZeile[] ARTEN = { Co2, Ch4, Eigen };

    private static EmissionswertZeile Ausgeliefert => new(11, "GEMIS 5.0", "GEMIS 5.0",
        201.0, "201", false, true, true, false);

    private static EmissionswertZeile EigenerWert => new(12, "eigener Wert", "eigener Wert",
        55.5, "55,5", true, false, true, true);

    private static EmissionswertZeile OhneZahl => new(13, "Vorlage ohne Wert", "Vorlage ohne Wert",
        null, "", false, false, false, true);

    private static readonly EmissionswertZeile[] WERTE = { Ausgeliefert, EigenerWert, OhneZahl };

    // ---- Aufbau ----------------------------------------------------------

    private IRenderedComponent<EmissionskatalogDialog> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<EmissionskatalogDialog>>? mehr = null,
        bool mitTraeger = true,
        Func<int, IReadOnlyList<EmissionswertZeile>>? werteLaden = null)
    {
        return Render<EmissionskatalogDialog>(p =>
        {
            p.Add(x => x.Arten, ARTEN);
            p.Add(x => x.MitTraeger, mitTraeger);
            p.Add(x => x.ArtenLaden, () => ARTEN);
            p.Add(x => x.WerteLaden, werteLaden ?? (artId => WERTE));
            mehr?.Invoke(p);
        });
    }

    /// <summary>
    /// Die EINE Rueckfrage des Dialogs — sie traegt alle drei Wege (Art loeschen,
    /// Wert loeschen, statt dessen abwaehlen).
    /// </summary>
    private static IRenderedComponent<EPOS.UI.Bausteine.Rueckfrage> Frage(
        IRenderedComponent<EmissionskatalogDialog> cut)
        => cut.FindComponent<EPOS.UI.Bausteine.Rueckfrage>();

    /// <summary>
    /// Antwortet auf die Rueckfrage. Die Knoepfe werden ueber ihre STELLUNG gewaehlt
    /// (Ja zuerst, dann Nein), nicht ueber ihren Text: Der kommt aus
    /// <c>Resource.ALLG_BTN_JA/_NEIN</c> und haengt damit an der Kultur des Laeufers,
    /// die diese Klasse bewusst nicht pinnt.
    /// </summary>
    private static void Antworten(IRenderedComponent<EmissionskatalogDialog> cut, bool ja)
        => Frage(cut).FindAll(".epos-rueckfrage .epos-leiste button")[ja ? 0 : 1].Click();

    private static IReadOnlyList<IElement> ArtenKnoepfe(IRenderedComponent<EmissionskatalogDialog> cut)
        => cut.FindAll(".epos-gruppenkopf:first-of-type .epos-leiste button");

    /// <summary>Die Leiste unter der Werteliste (zweite Gruppe).</summary>
    private static IReadOnlyList<IElement> WerteKnoepfe(IRenderedComponent<EmissionskatalogDialog> cut)
    {
        var leisten = cut.FindAll(".epos-gruppenkopf .epos-leiste");
        var gruppe = leisten[leisten.Count - 1];
        return gruppe.QuerySelectorAll("button");
    }

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Der_Dialog_zeigt_Kopf_Modusgruppe_zwei_Listen_und_die_Schlussleiste()
    {
        var cut = Zeige(p => p
            .Add(x => x.KontextText, "Träger: Erdgas")
            .Add(x => x.HinweisText, "Übernehmen kopiert den markierten Wert"));

        Assert.Equal("Emissionsfaktor-Katalog", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal("Träger: Erdgas", cut.Find(".epos-kontextzeile").TextContent);
        Assert.Equal(2, cut.FindAll(".epos-optionsgruppe .epos-option").Count);
        Assert.Equal(2, cut.FindAll(".epos-raster").Count);
        Assert.Equal(2, cut.FindAll(".epos-gruppenkopf").Count);
        Assert.Contains("Übernehmen kopiert", cut.Markup);
    }

    [Fact]
    public void Die_Artenliste_zeigt_alle_Arten_mit_Wahl_und_Haekchen()
    {
        var cut = Zeige();

        Assert.Equal(3, cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr").Length);
        Assert.Contains("Kohlendioxid", cut.Markup);
        Assert.Contains("Ammoniak", cut.Markup);
    }

    /// <summary>CO₂ ist Pflicht: Häkchen gesetzt und gesperrt (Konzept F1).</summary>
    [Fact]
    public void Das_Haekchen_der_Pflichtart_ist_gesperrt()
    {
        var cut = Zeige();

        var ersteZeile = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[0];
        var kasten = ersteZeile.QuerySelector("input[type=checkbox]");
        Assert.NotNull(kasten);
        Assert.True(kasten!.HasAttribute("disabled"));
        Assert.True(kasten.HasAttribute("checked"));
    }

    [Fact]
    public void Die_erste_Art_ist_vorgewaehlt_und_ihre_Werte_stehen_darunter()
    {
        var cut = Zeige();

        Assert.Equal(1, cut.Instance.GewaehlteArtId);
        Assert.Equal(3, cut.Instance.AngezeigteWerte.Count);
    }

    [Fact]
    public void Eine_Vorwahl_nach_Kuerzel_trifft_die_richtige_Art()
    {
        var cut = Zeige(p => p.Add(x => x.ArtVorwahl, "CH4"));

        Assert.Equal(2, cut.Instance.GewaehlteArtId);
    }

    /// <summary>Ohne Träger fehlt „Übernehmen" (btnUebernehmen.Visible).</summary>
    [Fact]
    public void Ohne_Traeger_gibt_es_kein_Uebernehmen()
    {
        var cut = Zeige(mitTraeger: false);

        Assert.Equal(3, WerteKnoepfe(cut).Count);       // Neu, Bearbeiten, Löschen
    }

    [Fact]
    public void Mit_Traeger_gibt_es_vier_Werteknoepfe()
    {
        var cut = Zeige();

        Assert.Equal(4, WerteKnoepfe(cut).Count);
    }

    // =====================================================================
    // Auswahl und Artwechsel
    // =====================================================================

    [Fact]
    public void Ein_Artwechsel_laedt_die_Werte_neu()
    {
        int gefragt = 0;
        var cut = Zeige(werteLaden: artId => { gefragt = artId; return WERTE; });

        var zweiteZeile = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[1];
        zweiteZeile.QuerySelector("button.epos-anlagenwahl")!.Click();

        Assert.Equal(2, cut.Instance.GewaehlteArtId);
        Assert.Equal(2, gefragt);
    }

    [Fact]
    public void Das_Umschalten_eines_Haekchens_meldet_die_Aenderung()
    {
        int? id = null;
        bool? neu = null;
        var cut = Zeige(p => p.Add(x => x.AuswahlSetzen, (a, w) => { id = a; neu = w; return null; }));

        var dritteZeile = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[2];
        dritteZeile.QuerySelector("input[type=checkbox]")!.Change(true);

        Assert.Equal(3, id);
        Assert.True(neu);
        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>Scheitert das Setzen, wird der Grund genannt (Konzept: Klartextgrund).</summary>
    [Fact]
    public void Ein_verweigertes_Haekchen_nennt_den_Grund()
    {
        var cut = Zeige(p => p.Add(x => x.AuswahlSetzen,
            (a, w) => "Die letzte ausgewählte Art lässt sich nicht abwählen."));

        var dritteZeile = cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[2];
        dritteZeile.QuerySelector("input[type=checkbox]")!.Change(true);

        Assert.Contains("nicht abwählen", cut.Instance.Meldung);
        Assert.Single(cut.FindAll(".epos-warnbanner"));
    }

    // =====================================================================
    // Arteneditor — früher ein zweites Fenster
    // =====================================================================

    [Fact]
    public void Neu_oeffnet_den_Arteneditor_und_legt_die_Art_an()
    {
        EmissionsartEingabe? gesehen = null;
        var cut = Zeige(p => p.Add(x => x.ArtAnlegen, e => { gesehen = e; return null; }));

        ArtenKnoepfe(cut)[0].Click();                       // Neu…
        Assert.True(cut.Instance.EditorOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung-inhalt"));

        var block = cut.Find(".epos-ueberlagerung-inhalt");
        block.QuerySelectorAll("input[type=text]")[0].Input("N2O");
        block.QuerySelectorAll("input[type=text]")[1].Input("Lachgas");
        block.QuerySelector("input[inputmode=decimal]")!.Input("273");
        block.QuerySelectorAll(".epos-leiste button")[1].Click();   // OK

        Assert.NotNull(gesehen);
        Assert.Equal(0, gesehen!.Id);
        Assert.Equal("N2O", gesehen.Kuerzel);
        Assert.Equal("Lachgas", gesehen.Name);
        Assert.Equal(273.0, gesehen.Gwp);
        Assert.False(cut.Instance.EditorOffen);
    }

    [Fact]
    public void Ein_leeres_Kuerzel_meldet_sich_und_haelt_den_Editor_offen()
    {
        bool angelegt = false;
        var cut = Zeige(p => p
            .Add(x => x.ArtAnlegen, e => { angelegt = true; return null; })
            .Add(x => x.MeldungKuerzelLeer, "Kürzel darf nicht leer sein"));

        ArtenKnoepfe(cut)[0].Click();
        cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.False(angelegt);
        Assert.True(cut.Instance.EditorOffen);
        Assert.Contains("leer", cut.Instance.Meldung);
    }

    /// <summary>Bei CO₂ bleibt der Faktor 1 und die beiden Felder sind gesperrt (F1/F2).</summary>
    [Fact]
    public void Der_Editor_der_Pflichtart_sperrt_Faktor_und_Quelle_und_schreibt_eins()
    {
        EmissionsartEingabe? gesehen = null;
        var cut = Zeige(p => p.Add(x => x.ArtAendern, e => { gesehen = e; return null; }));

        ArtenKnoepfe(cut)[1].Click();                       // Bearbeiten… auf CO2
        var block = cut.Find(".epos-ueberlagerung-inhalt");

        Assert.True(block.QuerySelector("input[inputmode=decimal]")!.HasAttribute("disabled"));
        // Kürzel, Name, GWP (mit inputmode), Quelle — die Quelle ist die vierte.
        Assert.True(block.QuerySelectorAll("input[type=text]")[3].HasAttribute("readonly"));
        Assert.Contains("Pflichtart", cut.Markup);

        block.QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(1, gesehen!.Id);
        Assert.Equal(1.0, gesehen.Gwp);
    }

    /// <summary>Das Kürzel einer ausgelieferten Art ist unveränderlich.</summary>
    [Fact]
    public void Das_Kuerzel_einer_ausgelieferten_Art_ist_nur_lesbar()
    {
        var cut = Zeige(p => p.Add(x => x.ArtVorwahl, "CH4"));

        ArtenKnoepfe(cut)[1].Click();
        var block = cut.Find(".epos-ueberlagerung-inhalt");

        Assert.True(block.QuerySelectorAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    [Fact]
    public void Ein_gescheitertes_Anlegen_nennt_den_Grund_und_haelt_den_Editor_offen()
    {
        var cut = Zeige(p => p.Add(x => x.ArtAnlegen, e => "Das Kürzel gibt es schon."));

        ArtenKnoepfe(cut)[0].Click();
        cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll("input[type=text]")[0].Input("CO2");
        cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.True(cut.Instance.EditorOffen);
        Assert.Contains("gibt es schon", cut.Instance.Meldung);
    }

    [Fact]
    public void Solange_ein_Editor_offen_steht_ruht_der_Rest_des_Dialogs()
    {
        var cut = Zeige();

        ArtenKnoepfe(cut)[0].Click();

        Assert.True(cut.FindAll(".epos-dialog > .epos-leiste button")[0].HasAttribute("disabled"));
        Assert.True(cut.FindAll(".epos-dialog > .epos-leiste button")[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Esc_schliesst_zuerst_den_Editor_dann_den_Dialog()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        ArtenKnoepfe(cut)[0].Click();
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.False(cut.Instance.EditorOffen);
        Assert.Null(erg);

        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.NotNull(erg);
        Assert.False(erg!.Bestaetigt);
    }

    /// <summary>Anwenderentscheid 15.09.2026: Das Kreuz im Kopf wirkt wie Abbrechen.</summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_schliesst_wie_Abbrechen()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        cut.Find(".epos-dialog-zu").Click();

        Assert.NotNull(erg);
        Assert.False(erg!.Bestaetigt);
    }

    /// <summary>
    /// Die beiden Untereditoren waren <c>Schliessbar="false"</c> — seit dem
    /// Anwenderentscheid 15.09.2026 tragen betitelte Überlagerungen ihr eigenes
    /// Kreuz, das denselben Weg wie Abbrechen des Editors geht.
    /// </summary>
    [Fact]
    public void Das_Kreuz_der_Ueberlagerung_schliesst_den_offenen_Artenditor()
    {
        var cut = Zeige();

        ArtenKnoepfe(cut)[0].Click();
        Assert.True(cut.Instance.EditorOffen);

        cut.Find(".epos-ueberlagerung-zu").Click();

        Assert.False(cut.Instance.EditorOffen);
    }

    // =====================================================================
    // Artenlöschen und „abwählen statt löschen"
    // =====================================================================

    /// <summary>
    /// W-E2, Befund 04/B17: Der Loeschknopf schreibt nicht mehr sofort. Er stellt die
    /// Frage IM Fenster (Baustein <c>Rueckfrage</c> statt MessageBox der Hülle), mit
    /// Vorgabe „Nein" (A-1); geloescht wird erst die Antwort „Ja".
    /// </summary>
    [Fact]
    public void Eine_eigene_Art_wird_erst_nach_der_Rueckfrage_geloescht()
    {
        int? geloescht = null;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "NH3")
            .Add(x => x.ArtLoeschenDelegat, id => { geloescht = id; return null; })
            .Add(x => x.VorlageArtLoeschen, "Art {0} löschen?"));

        ArtenKnoepfe(cut)[2].Click();

        var frage = Frage(cut);
        Assert.True(frage.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);
        Assert.Equal("Art Ammoniak löschen?", frage.Instance.Frage);
        Assert.Null(geloescht);                        // der Knopf fragt nur

        // A-1: Der hervorgehobene Knopf ist "Nein", nicht "Ja".
        var knoepfe = frage.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.DoesNotContain("epos-knopf--primaer", knoepfe[0].ClassName);
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName);

        Antworten(cut, ja: true);
        Assert.Equal(3, geloescht);
    }

    [Fact]
    public void Ein_Nein_auf_die_Rueckfrage_loescht_nicht()
    {
        bool geloescht = false;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "NH3")
            .Add(x => x.ArtLoeschenDelegat, id => { geloescht = true; return null; }));

        ArtenKnoepfe(cut)[2].Click();
        Antworten(cut, ja: false);

        Assert.False(geloescht);
        Assert.False(Frage(cut).Instance.Offen);
    }

    /// <summary>
    /// Eine offene Rueckfrage faengt Esc ab — der Katalog darf nicht unter der
    /// Frage wegschliessen (Muster <c>GesetzeskatalogDialog</c>).
    /// </summary>
    [Fact]
    public void Esc_schliesst_nicht_solange_die_Rueckfrage_steht()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "NH3")
            .Add(x => x.ArtLoeschenDelegat, id => null)
            .Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        ArtenKnoepfe(cut)[2].Click();
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.Null(erg);

        // Nach der Antwort schliesst Esc wieder.
        Antworten(cut, ja: false);
        cut.Find(".epos-dialog").KeyDown("Escape");
        Assert.NotNull(erg);
    }

    /// <summary>
    /// Ausgelieferte Art: Der Grund wird genannt UND das Abwählen gleich
    /// angeboten (§ 4.2).
    /// </summary>
    [Fact]
    public void Eine_ausgelieferte_Art_bietet_das_Abwaehlen_an()
    {
        int? abgewaehlt = null;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "CH4")
            .Add(x => x.ArtLoeschenDelegat, id => "Ausgelieferte Arten lassen sich nicht löschen.")
            .Add(x => x.AuswahlSetzen, (a, w) => { abgewaehlt = w ? -1 : a; return null; })
            .Add(x => x.FrageAbwaehlen, "Stattdessen abwählen?"));

        ArtenKnoepfe(cut)[2].Click();

        var frage = Frage(cut);
        Assert.True(frage.Instance.Offen);
        Assert.Contains("nicht löschen", frage.Instance.Frage);
        Assert.Contains("Stattdessen abwählen?", frage.Instance.Frage);

        // Das Abwaehlen ist ein ANGEBOT, kein Loeschen: Hier bleibt „Ja" betont.
        Assert.False(frage.Instance.VorgabeNein);

        Antworten(cut, ja: true);
        Assert.Equal(2, abgewaehlt);
    }

    /// <summary>
    /// Wer das Abwaehlen ablehnt, behaelt den GRUND als Hinweis — er ist die
    /// eigentliche Auskunft, warum die Art nicht zu loeschen war.
    /// </summary>
    [Fact]
    public void Ein_abgelehntes_Abwaehlen_laesst_den_Grund_stehen()
    {
        int? abgewaehlt = null;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "CH4")
            .Add(x => x.ArtLoeschenDelegat, id => "Ausgelieferte Arten lassen sich nicht löschen.")
            .Add(x => x.AuswahlSetzen, (a, w) => { abgewaehlt = a; return null; }));

        ArtenKnoepfe(cut)[2].Click();
        Antworten(cut, ja: false);

        Assert.Null(abgewaehlt);
        Assert.Contains("nicht löschen", cut.Instance.Meldung);
    }

    /// <summary>
    /// Der verkettete Fall: Die eigene Art wird gelöscht, der Kern weist sie ab —
    /// dann steht DIESELBE Rückfrage gleich noch einmal, nun als Abwählangebot.
    /// Genau hier muss der Wegmerker stimmen, sonst liefe das „Ja" in den
    /// Löschweg zurück.
    /// </summary>
    [Fact]
    public void Ein_abgewiesenes_Loeschen_geht_in_das_Abwaehlangebot_ueber()
    {
        // Eine EIGENE, aber AUSGEWAEHLTE Art: nur sie durchlaeuft beide Fragen —
        // NH3 ist abgewaehlt (dort gibt es das Angebot nicht), CH4 ausgeliefert
        // (dort steht die erste Frage nie).
        EmissionsartZeile eigenUndGewaehlt =
            new(4, "N2O", "Lachgas", "mg/kWh", 273.0, "273", "", true, false, false);
        EmissionsartZeile[] arten = { Co2, eigenUndGewaehlt };

        int? abgewaehlt = null;
        int versuche = 0;
        var cut = Render<EmissionskatalogDialog>(p => p
            .Add(x => x.Arten, arten)
            .Add(x => x.MitTraeger, true)
            .Add(x => x.ArtenLaden, () => arten)
            .Add(x => x.WerteLaden, artId => WERTE)
            .Add(x => x.ArtVorwahl, "N2O")
            .Add(x => x.ArtLoeschenDelegat, id => { versuche++; return "Die Art ist in Gebrauch."; })
            .Add(x => x.AuswahlSetzen, (a, w) => { abgewaehlt = w ? -1 : a; return null; })
            .Add(x => x.FrageAbwaehlen, "Stattdessen abwählen?"));

        ArtenKnoepfe(cut)[2].Click();
        Antworten(cut, ja: true);                        // Ja zum Loeschen

        Assert.Equal(1, versuche);
        var frage = Frage(cut);
        Assert.True(frage.Instance.Offen);                // die zweite Frage steht
        Assert.False(frage.Instance.VorgabeNein);         // ein Angebot, kein Loeschen
        Assert.Contains("Stattdessen abwählen?", frage.Instance.Frage);

        Antworten(cut, ja: true);                        // Ja zum Abwaehlen

        Assert.Equal(4, abgewaehlt);
        Assert.Equal(1, versuche);                        // NICHT noch einmal geloescht
    }

    /// <summary>Bei der Pflichtart gibt es den Ausweg nicht — nur den Hinweis.</summary>
    [Fact]
    public void Die_Pflichtart_bekommt_nur_den_Hinweis()
    {
        var cut = Zeige(p => p
            .Add(x => x.ArtLoeschenDelegat, id => "CO₂ ist die Pflichtart."));

        ArtenKnoepfe(cut)[2].Click();

        Assert.False(Frage(cut).Instance.Offen);
        Assert.Contains("Pflichtart", cut.Instance.Meldung);
    }

    // =====================================================================
    // Werte
    // =====================================================================

    /// <summary>
    /// EMK‑B‑1 (08.09.2026): ohne Markierung gesperrt; ein AUSGELIEFERTER Wert lässt die
    /// Knöpfe frei, und der Klick sagt, warum nichts geht — ein stummer, gesperrter Knopf
    /// hieß für den Anwender „funktioniert nicht".
    /// </summary>
    [Fact]
    public void Bearbeiten_und_Loeschen_sind_ohne_Markierung_gesperrt_und_erklaeren_einen_Auslieferungswert()
    {
        var cut = Zeige();

        // Ohne Markierung
        Assert.True(WerteKnoepfe(cut)[2].HasAttribute("disabled"));
        Assert.True(WerteKnoepfe(cut)[3].HasAttribute("disabled"));

        // Ausgelieferter Wert markiert: frei, der Klick erklaert
        var ersteZeile = cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[0];
        ersteZeile.QuerySelector("button.epos-anlagenwahl")!.Click();

        Assert.False(WerteKnoepfe(cut)[2].HasAttribute("disabled"));
        Assert.False(WerteKnoepfe(cut)[3].HasAttribute("disabled"));

        WerteKnoepfe(cut)[2].Click();
        Assert.Contains("unveränderlich", cut.Instance.Meldung);

        WerteKnoepfe(cut)[3].Click();
        Assert.Contains("unveränderlich", cut.Instance.Meldung);
    }

    /// <summary>
    /// W-E2, Befund 04/B17: Auch der zweite Loeschweg fragt erst — dieselbe
    /// Rueckfrage, anderer Weg. Die Antwort setzt den RICHTIGEN fort: Hier wird der
    /// Wert geloescht, nicht die Art.
    /// </summary>
    [Fact]
    public void Ein_eigener_Wert_wird_erst_nach_der_Rueckfrage_geloescht()
    {
        int? geloescht = null;
        int artGeloescht = 0;
        var cut = Zeige(p => p
            .Add(x => x.WertLoeschenDelegat, id => { geloescht = id; return null; })
            .Add(x => x.ArtLoeschenDelegat, id => { artGeloescht++; return null; })
            .Add(x => x.VorlageWertLoeschen, "Wert {0} löschen?"));

        EigenenWertMarkieren(cut);
        WerteKnoepfe(cut)[3].Click();                    // Löschen

        var frage = Frage(cut);
        Assert.True(frage.Instance.Offen);
        Assert.True(frage.Instance.VorgabeNein);
        Assert.Equal("Wert eigener Wert löschen?", frage.Instance.Frage);
        Assert.Null(geloescht);                          // der Knopf fragt nur

        Antworten(cut, ja: true);
        Assert.Equal(12, geloescht);
        Assert.Equal(0, artGeloescht);                   // der andere Weg bleibt unberuehrt
    }

    [Fact]
    public void Ein_Nein_auf_die_Wertefrage_loescht_nicht()
    {
        bool geloescht = false;
        var cut = Zeige(p => p
            .Add(x => x.WertLoeschenDelegat, id => { geloescht = true; return null; }));

        EigenenWertMarkieren(cut);
        WerteKnoepfe(cut)[3].Click();
        Antworten(cut, ja: false);

        Assert.False(geloescht);
        Assert.False(Frage(cut).Instance.Offen);
    }

    /// <summary>Markiert den eigenen Wert (Zeile 2 der Werteliste).</summary>
    private static void EigenenWertMarkieren(IRenderedComponent<EmissionskatalogDialog> cut)
        => cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[1]
              .QuerySelector("button.epos-anlagenwahl")!.Click();

    [Fact]
    public void Ein_eigener_Wert_laesst_sich_bearbeiten()
    {
        EmissionswertEingabe? gesehen = null;
        var cut = Zeige(p => p.Add(x => x.WertAendern, e => { gesehen = e; return null; }));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button.epos-anlagenwahl")!.Click();

        WerteKnoepfe(cut)[2].Click();                    // Bearbeiten…
        var block = cut.Find(".epos-ueberlagerung-inhalt");
        block.QuerySelector("input[inputmode=decimal]")!.Input("77");
        block.QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(12, gesehen!.Id);
        Assert.Equal(77.0, gesehen.Wert);
        Assert.True(gesehen.IstCo2e);                    // stand so im Wert
    }

    [Fact]
    public void Ein_neuer_Wert_bekommt_Art_und_Vorgabetext()
    {
        EmissionswertEingabe? gesehen = null;
        var cut = Zeige(p => p
            .Add(x => x.WertAnlegen, e => { gesehen = e; return null; })
            .Add(x => x.VorgabeQuelltext, "eigener Wert"));

        WerteKnoepfe(cut)[1].Click();                    // Neu…
        var block = cut.Find(".epos-ueberlagerung-inhalt");
        block.QuerySelector("input[inputmode=decimal]")!.Input("12,5");
        block.QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(0, gesehen!.Id);
        Assert.Equal(1, gesehen.ArtId);
        Assert.Equal("eigener Wert", gesehen.QuelleText);
        Assert.Equal(12.5, gesehen.Wert);
        Assert.False(gesehen.AlsVorlage);
    }

    [Fact]
    public void Ein_Wert_ohne_Zahl_meldet_sich()
    {
        bool angelegt = false;
        var cut = Zeige(p => p
            .Add(x => x.WertAnlegen, e => { angelegt = true; return null; })
            .Add(x => x.MeldungWertUngueltig, "Wert muss eine Zahl sein"));

        WerteKnoepfe(cut)[1].Click();
        cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll(".epos-leiste button")[1].Click();

        Assert.False(angelegt);
        Assert.Contains("Zahl", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Vorlagenschalter_ist_nur_beim_Anlegen_bedienbar()
    {
        var cut = Zeige();

        WerteKnoepfe(cut)[1].Click();                    // Neu…
        var kaesten = cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll("input[type=checkbox]");
        Assert.False(kaesten[1].HasAttribute("disabled"));

        cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll(".epos-leiste button")[0].Click();

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[1]
           .QuerySelector("button.epos-anlagenwahl")!.Click();
        WerteKnoepfe(cut)[2].Click();                    // Bearbeiten…
        kaesten = cut.Find(".epos-ueberlagerung-inhalt").QuerySelectorAll("input[type=checkbox]");
        Assert.True(kaesten[1].HasAttribute("disabled"));
    }

    // =====================================================================
    // Übernehmen
    // =====================================================================

    [Fact]
    public void Im_Rueckgabemodus_reicht_Uebernehmen_die_Id_zurueck_und_schliesst()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p
            .Add(x => x.Rueckgabemodus, true)
            .Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button.epos-anlagenwahl")!.Click();
        WerteKnoepfe(cut)[0].Click();

        Assert.NotNull(erg);
        Assert.Equal(11, erg!.UebernommenId);
        Assert.True(erg.Bestaetigt);
    }

    [Fact]
    public void Im_Verwaltungsmodus_schreibt_Uebernehmen_sofort_und_bleibt_offen()
    {
        int? geschrieben = null;
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p
            .Add(x => x.WertUebernehmenDelegat, id => { geschrieben = id; return null; })
            .Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[0]
           .QuerySelector("button.epos-anlagenwahl")!.Click();
        WerteKnoepfe(cut)[0].Click();

        Assert.Equal(11, geschrieben);
        Assert.Null(erg);
    }

    [Fact]
    public void Ein_Eintrag_ohne_Zahlenwert_laesst_sich_nicht_uebernehmen()
    {
        int? geschrieben = null;
        var cut = Zeige(p => p
            .Add(x => x.WertUebernehmenDelegat, id => { geschrieben = id; return null; })
            .Add(x => x.MeldungUebernahmeLeer, "kein Zahlenwert"));

        cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[2]
           .QuerySelector("button.epos-anlagenwahl")!.Click();
        WerteKnoepfe(cut)[0].Click();

        Assert.Null(geschrieben);
        Assert.Contains("kein Zahlenwert", cut.Instance.Meldung);
    }

    // =====================================================================
    // Abschluss
    // =====================================================================

    [Fact]
    public void OK_traegt_den_Modusschalter_und_die_Aenderungsmerker()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p
            .Add(x => x.ModusCo2e, false)
            .Add(x => x.AuswahlSetzen, (a, w) => null)
            .Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        // Modus umschalten und ein Häkchen setzen
        cut.FindAll(".epos-optionsgruppe input")[1].Change(true);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[2]
           .QuerySelector("input[type=checkbox]")!.Change(true);

        cut.FindAll(".epos-dialog > .epos-leiste button")[1].Click();   // OK

        Assert.NotNull(erg);
        Assert.True(erg!.ModusCo2e);
        Assert.True(erg.ArtenGeaendert);
        Assert.True(erg.Bestaetigt);
    }

    /// <summary>
    /// Abbrechen nimmt den Modusschalter NICHT mit (der Vorläufer schrieb ihn
    /// nur in <c>Beenden</c>), die Änderungsmerker aber schon.
    /// </summary>
    [Fact]
    public void Abbrechen_laesst_den_Modus_wie_er_war_und_meldet_die_Aenderungen()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p
            .Add(x => x.ModusCo2e, false)
            .Add(x => x.AuswahlSetzen, (a, w) => null)
            .Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        cut.FindAll(".epos-optionsgruppe input")[1].Change(true);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr")[2]
           .QuerySelector("input[type=checkbox]")!.Change(true);

        cut.FindAll(".epos-dialog > .epos-leiste button")[0].Click();   // Abbrechen

        Assert.NotNull(erg);
        Assert.False(erg!.ModusCo2e);
        Assert.True(erg.ArtenGeaendert);
        Assert.False(erg.Bestaetigt);
    }

    [Fact]
    public void Enter_ist_nicht_belegt()
    {
        EmissionskatalogErgebnis? erg = null;
        var cut = Zeige(p => p.Add(x => x.Geschlossen, (EmissionskatalogErgebnis e) => erg = e));

        cut.Find(".epos-dialog").KeyDown("Enter");

        Assert.Null(erg);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Zeige();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_Emissionskatalog.btn_Help" }, hilfe.Geoeffnet);
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Die beiden Editoren in der
    /// Überlagerung sind Formularblöcke und stehen im <c>Formularraster</c> —
    /// Beschriftung neben dem Feld. Die beiden Datenraster des Dialogs bleiben
    /// DATENraster: Ihre Felder stehen in Tabellenzellen, dort darf der Raster
    /// nicht hinein.
    /// </summary>
    [Fact]
    public void Der_Arteneditor_steht_im_Formularraster()
    {
        var cut = Zeige();
        ArtenKnoepfe(cut)[0].Click();                       // Neu…

        var block = cut.Find(".epos-ueberlagerung-inhalt");
        Assert.NotNull(block.QuerySelector(".epos-formularraster"));
        Assert.True(block.QuerySelectorAll(".epos-formularraster .epos-feld").Length >= 4);

        // Kein Raster um die Tabellen.
        Assert.Empty(cut.FindAll(".epos-formularraster .epos-zeilenraster"));
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Zeige(p => p.Add(x => x.TitelText, ""));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>EmissionskatalogKiSicht</c>: die Bilanzierungsmethode, die
    /// Markierung im Artenraster als WAHLFELD und die lebenden Felder des
    /// Arteneditors.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_markiert_eine_Art()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.EMISSIONSKATALOG));

        KiFeldzugang modus =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.EMISSIONSKATALOG, "als_co2e");
        Assert.NotNull(modus);
        Assert.True(modus.Setzbar);

        KiFeldzugang art =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.EMISSIONSKATALOG, "emissionsart");
        Assert.NotNull(art);
        Assert.NotEmpty(art.Wahleintraege());

        string zweite = art.Wahleintraege()[1].Text;
        KiFeldumsetzung wahl = KiFeldwandler.Wandle(art, zweite);
        Assert.True(wahl.Ok, wahl.Grund);
        art.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(art.Wahleintraege()[1].Schluessel,
                     Convert.ToString(art.Lesen(), CultureInfo.InvariantCulture));

        // Die Felder des Arteneditors sind Felder DIESER Maske.
        KiFeldzugang gwp =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.EMISSIONSKATALOG, "art_gwp");
        Assert.NotNull(gwp);
        Assert.True(gwp.Setzbar);
    }
}

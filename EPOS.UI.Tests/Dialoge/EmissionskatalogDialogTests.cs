using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
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
        Func<int, IReadOnlyList<EmissionswertZeile>>? werteLaden = null,
        Func<string, bool>? rueckfrage = null)
    {
        return Render<EmissionskatalogDialog>(p =>
        {
            p.Add(x => x.Arten, ARTEN);
            p.Add(x => x.MitTraeger, mitTraeger);
            p.Add(x => x.ArtenLaden, () => ARTEN);
            p.Add(x => x.WerteLaden, werteLaden ?? (artId => WERTE));
            p.Add(x => x.Rueckfrage, rueckfrage ?? (text => true));
            mehr?.Invoke(p);
        });
    }

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

    // =====================================================================
    // Artenlöschen und „abwählen statt löschen"
    // =====================================================================

    [Fact]
    public void Eine_eigene_Art_wird_nach_Rueckfrage_geloescht()
    {
        int? geloescht = null;
        string? gefragt = null;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "NH3")
            .Add(x => x.ArtLoeschenDelegat, id => { geloescht = id; return null; })
            .Add(x => x.VorlageArtLoeschen, "Art {0} löschen?"),
            rueckfrage: text => { gefragt = text; return true; });

        ArtenKnoepfe(cut)[2].Click();

        Assert.Equal(3, geloescht);
        Assert.Equal("Art Ammoniak löschen?", gefragt);
    }

    [Fact]
    public void Ein_Nein_auf_die_Rueckfrage_loescht_nicht()
    {
        bool geloescht = false;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "NH3")
            .Add(x => x.ArtLoeschenDelegat, id => { geloescht = true; return null; }),
            rueckfrage: text => false);

        ArtenKnoepfe(cut)[2].Click();

        Assert.False(geloescht);
    }

    /// <summary>
    /// Ausgelieferte Art: Der Grund wird genannt UND das Abwählen gleich
    /// angeboten (§ 4.2).
    /// </summary>
    [Fact]
    public void Eine_ausgelieferte_Art_bietet_das_Abwaehlen_an()
    {
        int? abgewaehlt = null;
        string? frage = null;
        var cut = Zeige(p => p
            .Add(x => x.ArtVorwahl, "CH4")
            .Add(x => x.ArtLoeschenDelegat, id => "Ausgelieferte Arten lassen sich nicht löschen.")
            .Add(x => x.AuswahlSetzen, (a, w) => { abgewaehlt = w ? -1 : a; return null; })
            .Add(x => x.FrageAbwaehlen, "Stattdessen abwählen?"),
            rueckfrage: text => { frage = text; return true; });

        ArtenKnoepfe(cut)[2].Click();

        Assert.Equal(2, abgewaehlt);
        Assert.Contains("nicht löschen", frage);
        Assert.Contains("Stattdessen abwählen?", frage);
    }

    /// <summary>Bei der Pflichtart gibt es den Ausweg nicht — nur den Hinweis.</summary>
    [Fact]
    public void Die_Pflichtart_bekommt_nur_den_Hinweis()
    {
        bool gefragt = false;
        var cut = Zeige(p => p
            .Add(x => x.ArtLoeschenDelegat, id => "CO₂ ist die Pflichtart."),
            rueckfrage: text => { gefragt = true; return true; });

        ArtenKnoepfe(cut)[2].Click();

        Assert.False(gefragt);
        Assert.Contains("Pflichtart", cut.Instance.Meldung);
    }

    // =====================================================================
    // Werte
    // =====================================================================

    [Fact]
    public void Bearbeiten_und_Loeschen_bleiben_ohne_eigenen_Wert_gesperrt()
    {
        var cut = Zeige();

        // Ohne Markierung
        Assert.True(WerteKnoepfe(cut)[2].HasAttribute("disabled"));
        Assert.True(WerteKnoepfe(cut)[3].HasAttribute("disabled"));

        // Ausgelieferter Wert markiert
        var ersteZeile = cut.FindAll(".epos-raster")[1].QuerySelectorAll("tbody tr")[0];
        ersteZeile.QuerySelector("button.epos-anlagenwahl")!.Click();

        Assert.True(WerteKnoepfe(cut)[2].HasAttribute("disabled"));
        Assert.True(WerteKnoepfe(cut)[3].HasAttribute("disabled"));
    }

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
}

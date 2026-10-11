using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>KT-5: Wirft ein Schreibweg eines Katalogdialogs, bleibt der Dialog nicht still.</b> Die
/// Matrix zu <see cref="KatalogListeWirftTests"/> und <see cref="KatalogSatzWirftTests"/>: je an
/// <see cref="Katalogladung"/> angeschlossenem Dialog das werfende Speichern und Löschen, die
/// werfenden Nebenwege (Duplizieren, Aufnehmen in das Projekt oder den Katalog, Übersicht,
/// Editor) und der Gegenfall ohne Ausnahme beim Satz. Der Dialog zeichnet weiter, der Grund
/// steht mit der Meldung der Ausnahme im Text des Dialogs (<c>WURZEL_SCHREIBEN_FEHLER</c>,
/// <c>WURZEL_LOESCHEN_FEHLER</c>, <c>WURZEL_SATZ_FEHLER</c>), und das
/// <see cref="Ausnahmeprotokoll"/> hat einen Vermerk.
/// </summary>
/// <remarks>
/// Bereits in den Dialogklassen belegt und hier nicht wiederholt: werfendes Speichern und Löschen
/// der Kältemaschinenverwaltung (<c>KaeltemaschineKatalogDialogTests</c>) und des Heizkessels
/// (<c>HeizkesselDialogTests</c>), werfendes Löschen des Katalogbrowsers
/// (<c>KatalogBrowserDialogTests</c>) und der TWW-Nutzungsarten (<c>TwwNutzungsartAdminDialogTests</c>),
/// werfendes Speichern der Gebäudeverwaltung (<c>GebaeudeAdminDialogTests</c>, braucht deren
/// vollständigen Feldsatz) und des eingebetteten TWW-Editors samt werfendem Laden des Editorstands
/// (<c>TwwNutzungsartAdminDialogTests</c>).
/// </remarks>
public class KatalogSchreibenWirftTests : EposBunitContext
{
    private const string GRUND = "Schreibweg gesperrt";
    private const string NAME = "Gerät A";

    private readonly List<string> _vermerke = new();

    public KatalogSchreibenWirftTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Ausnahmeprotokoll.Vermerkt += Mitschreiben;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Ausnahmeprotokoll.Vermerkt -= Mitschreiben;
        base.Dispose(disposing);
    }

    private void Mitschreiben(string zeile)
    {
        lock (_vermerke) _vermerke.Add(zeile);
    }

    // =================================================================================
    // Hilfen
    // =================================================================================

    private static IReadOnlyList<Katalogfilterzeile> Eine() => new[] { new Katalogfilterzeile(1, NAME) };

    private static T Wirft<T>() => throw new InvalidOperationException(GRUND);

    /// <summary>Der Mittelteil einer Vorlage zwischen Satzname und Meldung („… wurde nicht gespeichert: ").</summary>
    private static string Mitte(string vorlage)
    {
        int a = vorlage.IndexOf("{0}", StringComparison.Ordinal) + 3;
        int b = vorlage.IndexOf("{1}", StringComparison.Ordinal);
        return vorlage[a..b];
    }

    private static string Text<T>(IRenderedComponent<T> cut) where T : IComponent
        => string.Join("\n", cut.Nodes.Select(n => n.TextContent));

    /// <summary>Der Grund steht im Dialog (Vorlage samt Meldung der Ausnahme), das Protokoll hat einen Vermerk.</summary>
    private void GrundSteht<T>(IRenderedComponent<T> cut, string vorlage) where T : IComponent
    {
        Assert.Contains(Mitte(vorlage) + GRUND, Text(cut));
        lock (_vermerke) Assert.Contains(_vermerke, v => v.StartsWith("Katalogsatz", StringComparison.Ordinal) && v.Contains(GRUND));
    }

    /// <summary>Gegenfall: kein Satzgrund im Dialog, kein Vermerk.</summary>
    private void OhneSatzgrund<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        Assert.DoesNotContain(Mitte(R.WURZEL_SATZ_FEHLER), Text(cut));
        lock (_vermerke) Assert.Empty(_vermerke);
    }

    /// <summary>Wählt die Zeile in der Katalogliste (der letzten Liste des Dialogs).</summary>
    private static void ZeileWaehlen<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.FindAll(".epos-raster").Last().QuerySelectorAll(".epos-zeilenzelle--name, .epos-anlagenwahl")[0].Click();

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Handlung<T>(IRenderedComponent<T> cut, string text) where T : IComponent
        => cut.FindAll(".epos-auswahlleiste button").First(k => k.TextContent.Trim() == text);

    /// <summary>Bestätigt die offene Rückfrage mit ihrem ersten Knopf (Ja/Löschen).</summary>
    private static void RueckfrageJa<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.FindAll(".epos-rueckfrage button")[0].Click();

    /// <summary>Bestätigt die Namensabfrage von „Duplizieren…" mit ihrer Vorbelegung.</summary>
    private static void NamensabfrageOk<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == R.ALLG_BTN_OK).Click();

    private static Katalogfilterprofil Profil(Anlagenart art)
        => Katalogfilterprofil.MitVerwendung(art, s => R.ResourceManager.GetString(s) ?? s);

    private static ErzeugerDetail Detail(string n) => new(n, "", Array.Empty<(string, string)>());

    private static ErzeugerZeile Zeile(int schluessel, string name)
        => new() { Schluessel = schluessel, Bezeichner = name, GeraetId = 9, CarrierId = 5, Vorlauf = 70, Ruecklauf = 50 };

    private static List<BrowserFeldwert> Felder() => new()
    {
        new BrowserFeldwert
        {
            Schluessel = KatalogBrowserProfil.FeldInvestitionskosten,
            Bezeichnung = "Investitionskosten:",
            Einheit = "€",
            Art = BrowserFeldArt.Zahl,
            Editierbar = true,
            Wert = "12000"
        }
    };

    /// <summary>Öffnet die Satz-Überlagerung des gewählten Katalogsatzes, ändert die Zahl und bestätigt.</summary>
    private static void SatzAendernUndOk<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        ZeileWaehlen(cut);
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten").Click();
        cut.Find(".epos-modulparameter input[inputmode=decimal]").Input("15000");
        cut.Find(".epos-satzueberlagerung-ok").Click();
    }

    /// <summary>Löscht den gewählten Katalogsatz über den Knopf „Löschen" und die Rückfrage.</summary>
    private static void KatalogsatzLoeschen<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        ZeileWaehlen(cut);
        Knopf(cut, "Löschen").Click();
        RueckfrageJa(cut);
    }

    /// <summary>„▲ In das Projekt übernehmen" für den gewählten Katalogsatz.</summary>
    private static void Uebernehmen<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        ZeileWaehlen(cut);
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
    }

    /// <summary>Die Trägerwahl vor dem Aufnehmen (Heizkessel, BHKW): Variantenname und OK.</summary>
    private static void TraegerwahlOk<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        cut.Find(".epos-ueberlagerung input[type=text]").Input("Erdgas E Variante");
        cut.Find(".epos-ueberlagerung .epos-knopf--primaer").Click();
    }

    // =================================================================================
    // Die Erzeugerdialoge
    // =================================================================================

    private IRenderedComponent<HeizkesselDialog> Heizkessel(Func<int, EnergietraegerVarianteErgebnis, AufnahmeErgebnis>? aufnehmen = null,
                                                         Func<string, ErzeugerDetail?>? detail = null)
        => Render<HeizkesselDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Heizkessel))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, detail ?? Detail)
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante") })
            .Add(x => x.Vorbereiten, _ => new TraegerVorbereitung(new[] { (3, "Erdgas E") }, 3))
            .Add(x => x.TraegerGaben, _ => new Dictionary<string, object> { ["Energietraeger"] = new[] { (3, "Erdgas E") } })
            .Add(x => x.Aufnehmen, aufnehmen));

    private IRenderedComponent<BhkwDialog> Bhkw(Func<int, EnergietraegerVarianteErgebnis, AufnahmeErgebnis>? aufnehmen = null,
                                             Func<int, string>? loeschen = null,
                                             Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? speichern = null,
                                             Func<string, ErzeugerDetail?>? detail = null)
        => Render<BhkwDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Bhkw))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, detail ?? Detail)
            .Add(x => x.Varianten, _ => new[] { (5, "Erdgas E Variante") })
            .Add(x => x.Vorbereiten, _ => new TraegerVorbereitung(new[] { (3, "Erdgas E") }, 3))
            .Add(x => x.TraegerGaben, _ => new Dictionary<string, object> { ["Energietraeger"] = new[] { (3, "Erdgas E") } })
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KatalogLoeschen, loeschen)
            .Add(x => x.Katalogfelder, _ => Felder())
            .Add(x => x.KatalogfelderSpeichern, speichern));

    private IRenderedComponent<PufferspeicherDialog> Puffer(Func<int, bool, AufnahmeErgebnis>? aufnehmen = null,
                                                         Func<int, string>? loeschen = null,
                                                         Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? speichern = null)
        => Render<PufferspeicherDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Pufferspeicher))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, _ => Detail(NAME))
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KatalogLoeschen, loeschen)
            .Add(x => x.Katalogfelder, _ => Felder())
            .Add(x => x.KatalogfelderSpeichern, speichern));

    private IRenderedComponent<StromspeicherDialog> Stromspeicher(Func<int, AufnahmeErgebnis>? aufnehmen = null,
                                                               Func<int, string>? loeschen = null,
                                                               Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? speichern = null)
        => Render<StromspeicherDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Stromspeicher))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.KatalogDetail, Detail)
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KatalogLoeschen, loeschen)
            .Add(x => x.Katalogfelder, _ => Felder())
            .Add(x => x.KatalogfelderSpeichern, speichern));

    private IRenderedComponent<PhotovoltaikDialog> Photovoltaik(Func<int, AufnahmeErgebnis>? aufnehmen = null,
                                                             Func<int, string>? loeschen = null,
                                                             Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? speichern = null)
        => Render<PhotovoltaikDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Photovoltaik))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KatalogLoeschen, loeschen)
            .Add(x => x.Katalogfelder, _ => Felder())
            .Add(x => x.KatalogfelderSpeichern, speichern));

    private IRenderedComponent<SolarkollektorenDialog> Solar(Func<int, AufnahmeErgebnis>? aufnehmen = null,
                                                          Func<int, string>? loeschen = null,
                                                          Func<string, IReadOnlyList<BrowserFeldwert>, KatalogSpeicherErgebnis>? speichern = null)
        => Render<SolarkollektorenDialog>(b => b
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Solarkollektoren))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Detail, Detail)
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KatalogLoeschen, loeschen)
            .Add(x => x.Katalogfelder, _ => Felder())
            .Add(x => x.KatalogfelderSpeichern, speichern));

    // --- Heizkessel (Speichern und Löschen in HeizkesselDialogTests) -------------------

    [Fact]
    public void Heizkessel_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Heizkessel(aufnehmen: (_, _) => Wirft<AufnahmeErgebnis>());
        Uebernehmen(cut);
        TraegerwahlOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    // --- BHKW -------------------------------------------------------------------------

    [Fact]
    public void Bhkw_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = Bhkw(speichern: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        SatzAendernUndOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Bhkw_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Bhkw(loeschen: _ => Wirft<string>());
        KatalogsatzLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Bhkw_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Bhkw(aufnehmen: (_, _) => Wirft<AufnahmeErgebnis>());
        Uebernehmen(cut);
        TraegerwahlOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Bhkw_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Bhkw();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    // --- Pufferspeicher ---------------------------------------------------------------

    [Fact]
    public void Pufferspeicher_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = Puffer(speichern: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        SatzAendernUndOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Pufferspeicher_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Puffer(loeschen: _ => Wirft<string>());
        KatalogsatzLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Pufferspeicher_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Puffer(aufnehmen: (_, _) => Wirft<AufnahmeErgebnis>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Pufferspeicher_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Puffer();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    // --- Stromspeicher ----------------------------------------------------------------

    [Fact]
    public void Stromspeicher_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = Stromspeicher(speichern: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        SatzAendernUndOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Stromspeicher_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Stromspeicher(loeschen: _ => Wirft<string>());
        KatalogsatzLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Stromspeicher_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Stromspeicher(aufnehmen: _ => Wirft<AufnahmeErgebnis>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Stromspeicher_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Stromspeicher();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    // --- Photovoltaik -----------------------------------------------------------------

    [Fact]
    public void Photovoltaik_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = Photovoltaik(speichern: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        SatzAendernUndOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Photovoltaik_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Photovoltaik(loeschen: _ => Wirft<string>());
        KatalogsatzLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Photovoltaik_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Photovoltaik(aufnehmen: _ => Wirft<AufnahmeErgebnis>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Photovoltaik_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Photovoltaik();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    // --- Solarkollektoren -------------------------------------------------------------

    [Fact]
    public void Solarkollektoren_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = Solar(speichern: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        SatzAendernUndOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Solarkollektoren_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Solar(loeschen: _ => Wirft<string>());
        KatalogsatzLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Solarkollektoren_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Solar(aufnehmen: _ => Wirft<AufnahmeErgebnis>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Solarkollektoren_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Solar();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    // =================================================================================
    // KatalogBrowser und ModulKatalog
    // =================================================================================

    private static KatalogBrowserProfil BrowserProfil()
        => KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel, s => R.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<BrowserFeldwert> BrowserFelder(string name)
        => BrowserProfil().Detailfelder.Select(f => new BrowserFeldwert
        {
            Schluessel = f.Schluessel,
            Bezeichnung = f.Bezeichnung,
            Einheit = f.Einheit,
            Art = f.Art,
            Editierbar = f.Editierbar,
            Wert = f.Schluessel == KatalogBrowserProfil.FeldBezeichner ? name
                 : f.Art == BrowserFeldArt.Schalter ? "1"
                 : f.Art == BrowserFeldArt.Zahl ? "12,50"
                 : f.Art == BrowserFeldArt.Ganzzahl ? "70"
                 : "Wert"
        }).ToList();

    private IRenderedComponent<KatalogBrowserDialog> Browser(KatalogBrowserWege wege,
                                                          Func<string, IReadOnlyList<Parameterwert>>? uebersicht = null)
        => Render<KatalogBrowserDialog>(b => b
            .Add(x => x.Art, KatalogBrowserArt.Heizkessel)
            .Add(x => x.ProfilVorgabe, BrowserProfil())
            .Add(x => x.Wege, wege)
            .Add(x => x.Uebersicht, uebersicht));

    private static KatalogBrowserWege BrowserWege() => new()
    {
        Katalogzeilen = Eine,
        Detail = BrowserFelder,
        Speichern = (n, _) => new KatalogSpeicherErgebnis(true, "", n),
        Duplizieren = (_, n) => new KatalogSpeicherErgebnis(true, "", n),
        Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
    };

    [Fact]
    public void KatalogBrowser_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        KatalogBrowserWege w = BrowserWege();
        var cut = Browser(new KatalogBrowserWege
        {
            Katalogzeilen = w.Katalogzeilen, Detail = w.Detail, Duplizieren = w.Duplizieren, Loeschen = w.Loeschen,
            Speichern = (_, _) => Wirft<KatalogSpeicherErgebnis>()
        });
        ZeileWaehlen(cut);
        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void KatalogBrowser_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        KatalogBrowserWege w = BrowserWege();
        var cut = Browser(new KatalogBrowserWege
        {
            Katalogzeilen = w.Katalogzeilen, Detail = w.Detail, Speichern = w.Speichern, Loeschen = w.Loeschen,
            Duplizieren = (_, _) => Wirft<KatalogSpeicherErgebnis>()
        });
        ZeileWaehlen(cut);
        Handlung(cut, R.ADM_BTN_DUPLIZIEREN).Click();
        NamensabfrageOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void KatalogBrowser_wirft_die_Uebersicht_nennt_der_Dialog_den_Grund()
    {
        var cut = Browser(BrowserWege(), _ => Wirft<IReadOnlyList<Parameterwert>>());
        ZeileWaehlen(cut);
        GrundSteht(cut, R.WURZEL_SATZ_FEHLER);
    }

    [Fact]
    public void KatalogBrowser_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Browser(BrowserWege(), _ => Array.Empty<Parameterwert>());
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    private static ModulKatalogProfil ModulProfil()
        => ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher, s => R.ResourceManager.GetString(s) ?? s);

    private static IReadOnlyList<ModulFeldwert> ModulFelder(string name)
        => ModulProfil().Felder.Select(f => new ModulFeldwert
        {
            Schluessel = f.Schluessel,
            Bezeichnung = f.Bezeichnung,
            Einheit = f.Einheit,
            Art = f.Art,
            LeerErlaubt = f.LeerErlaubt,
            Gesperrt = f.Gesperrt,
            Gruppe = f.Gruppe,
            Optionen = f.Optionen,
            Wert = f.Schluessel == ModulKatalogProfil.FeldBezeichner ? name
                 : f.Art == BrowserFeldArt.Zahl ? "12,50"
                 : f.Art == BrowserFeldArt.Ganzzahl ? "6000"
                 : "Wert"
        }).ToList();

    private IRenderedComponent<ModulKatalogDialog> Modul(Func<IReadOnlyList<ModulFeldwert>, bool, string, KatalogSpeicherErgebnis>? speichern = null,
                                                      Func<int, string, KatalogSpeicherErgebnis>? duplizieren = null,
                                                      Func<string, KatalogSpeicherErgebnis>? loeschen = null,
                                                      Func<string, IReadOnlyList<Parameterwert>>? uebersicht = null)
        => Render<ModulKatalogDialog>(b => b
            .Add(x => x.Art, ModulKatalogArt.Stromspeicher)
            .Add(x => x.ProfilVorgabe, ModulProfil())
            .Add(x => x.Wege, new ModulKatalogWege
            {
                Katalogzeilen = Eine,
                Detail = ModulFelder,
                Speichern = speichern ?? ((_, _, n) => new KatalogSpeicherErgebnis(true, "", n)),
                Duplizieren = duplizieren ?? ((_, n) => new KatalogSpeicherErgebnis(true, "", n)),
                Loeschen = loeschen ?? (n => new KatalogSpeicherErgebnis(true, "", n))
            })
            .Add(x => x.Uebersicht, uebersicht));

    [Fact]
    public void ModulKatalog_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = Modul(speichern: (_, _, _) => Wirft<KatalogSpeicherErgebnis>());
        ZeileWaehlen(cut);
        cut.FindAll("input[inputmode=decimal]")[0].Input("42");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void ModulKatalog_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Modul(loeschen: _ => Wirft<KatalogSpeicherErgebnis>());
        ZeileWaehlen(cut);
        Handlung(cut, "Löschen").Click();
        RueckfrageJa(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void ModulKatalog_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = Modul(duplizieren: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        ZeileWaehlen(cut);
        Handlung(cut, "Duplizieren...").Click();
        NamensabfrageOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void ModulKatalog_wirft_die_Uebersicht_nennt_der_Dialog_den_Grund()
    {
        var cut = Modul(uebersicht: _ => Wirft<IReadOnlyList<Parameterwert>>());
        ZeileWaehlen(cut);
        GrundSteht(cut, R.WURZEL_SATZ_FEHLER);
    }

    // =================================================================================
    // Wärmepumpe (Verwaltung und Projektdialog) und Kältemaschine
    // =================================================================================

    private IRenderedComponent<WaermepumpeStammDialog> WpStamm(Func<WaermepumpeStammDaten, bool, KatalogSpeicherErgebnis>? speichern = null,
                                                             Func<string, bool>? loeschen = null,
                                                             Func<int, string, KatalogSpeicherErgebnis>? duplizieren = null,
                                                             Func<string, IReadOnlyList<Parameterwert>>? uebersicht = null)
        => Render<WaermepumpeStammDialog>(b => b
            .Add(x => x.Liste, Eine)
            .Add(x => x.Satz, id => new WaermepumpeStammDaten { Id = id, Name = NAME })
            .Add(x => x.Speichern, speichern ?? ((d, _) => new KatalogSpeicherErgebnis(true, "", d.Name)))
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Duplizieren, duplizieren ?? ((_, n) => new KatalogSpeicherErgebnis(true, "", n)))
            .Add(x => x.Uebersicht, uebersicht));

    [Fact]
    public void Waermepumpe_wirft_das_Speichern_nennt_der_Dialog_den_Grund()
    {
        var cut = WpStamm(speichern: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        ZeileWaehlen(cut);
        Knopf(cut, "Speichern").Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Waermepumpe_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = WpStamm(loeschen: _ => Wirft<bool>());
        ZeileWaehlen(cut);
        Handlung(cut, "Löschen").Click();
        RueckfrageJa(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Waermepumpe_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = WpStamm(duplizieren: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        ZeileWaehlen(cut);
        cut.FindAll(".epos-auswahlleiste button").First(k => k.TextContent.Trim().StartsWith("Duplizieren", StringComparison.Ordinal)).Click();
        NamensabfrageOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Waermepumpe_wirft_die_Uebersicht_nennt_der_Dialog_den_Grund()
    {
        var cut = WpStamm(uebersicht: _ => Wirft<IReadOnlyList<Parameterwert>>());
        ZeileWaehlen(cut);
        GrundSteht(cut, R.WURZEL_SATZ_FEHLER);
    }

    [Fact]
    public void Kaeltemaschine_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = Render<KaeltemaschineKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Lies, id => new KaeltemaschineDaten { Id = id, Bezeichner = NAME })
            .Add(x => x.Duplizieren, (_, _) => Wirft<KaeltemaschineSpeicherErgebnis>()));
        ZeileWaehlen(cut);
        cut.FindAll(".epos-auswahlleiste button").First(k => k.TextContent.Trim().StartsWith("Duplizieren", StringComparison.Ordinal)).Click();
        NamensabfrageOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    private IRenderedComponent<WaermepumpenDialog> Waermepumpen(Func<string, WaermepumpeAnlageDaten?>? anlegen = null,
                                                             Func<int, string>? loeschen = null,
                                                             Func<IReadOnlyList<Katalogfilterzeile>>? katalog = null,
                                                             Func<int, WaermepumpeStammDaten?>? satz = null)
        => Render<WaermepumpenDialog>(b => b
            .Add(x => x.Zeilen, new List<WaermepumpeAnlageDaten>())
            .Add(x => x.Katalogprofil, Profil(Anlagenart.Waermepumpe))
            .Add(x => x.Katalog, katalog ?? Eine)
            .Add(x => x.KatalogSatz, satz)
            .Add(x => x.Anlegen, anlegen)
            .Add(x => x.KatalogLoeschen, loeschen));

    [Fact]
    public void Waermepumpen_wirft_das_Anlegen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Waermepumpen(anlegen: _ => Wirft<WaermepumpeAnlageDaten?>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Waermepumpen_wirft_die_Liste_nennt_der_Dialog_den_Grund()
    {
        var cut = Waermepumpen(katalog: Wirft<IReadOnlyList<Katalogfilterzeile>>);
        Assert.Contains(Mitte(R.WURZEL_LISTE_FEHLER) + GRUND, Text(cut));
        lock (_vermerke) Assert.Contains(_vermerke, v => v.StartsWith("Katalogliste", StringComparison.Ordinal) && v.Contains(GRUND));
    }

    [Fact]
    public void Waermepumpen_wirft_der_Satz_nennt_der_Dialog_den_Grund()
    {
        var cut = Waermepumpen(satz: _ => Wirft<WaermepumpeStammDaten?>());
        ZeileWaehlen(cut);
        GrundSteht(cut, R.WURZEL_SATZ_FEHLER);
    }

    [Fact]
    public void Waermepumpen_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Waermepumpen(satz: id => new WaermepumpeStammDaten { Id = id, Name = NAME });
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    [Fact]
    public void Waermepumpen_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Waermepumpen(loeschen: _ => Wirft<string>());
        KatalogsatzLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    // =================================================================================
    // Die Bedarfsdialoge
    // =================================================================================

    private IRenderedComponent<GebaeudeAdminDialog> GebaeudeAdmin(Func<string, bool>? loeschen = null,
                                                                Func<int, string, KatalogSpeicherErgebnis>? duplizieren = null)
        => Render<GebaeudeAdminDialog>(b => b
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Satz, _ => null)
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Duplizieren, duplizieren ?? ((_, n) => new KatalogSpeicherErgebnis(true, "", n))));

    [Fact]
    public void GebaeudeAdmin_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = GebaeudeAdmin(loeschen: _ => Wirft<bool>());
        Handlung(cut, "Löschen").Click();
        RueckfrageJa(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void GebaeudeAdmin_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = GebaeudeAdmin(duplizieren: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        cut.FindAll(".epos-auswahlleiste button").First(k => k.TextContent.Trim().StartsWith("Duplizieren", StringComparison.Ordinal)).Click();
        NamensabfrageOk(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void GebaeudeAdmin_ohne_Ausnahme_zeigt_keinen_Satzgrund()
        => OhneSatzgrund(GebaeudeAdmin());

    // ---- KT-7: Bedarfsverwaltung, Gebäudetypen, Baustoffe, Bauteilaufbauten ---------------

    /// <summary>Wählt in der Auswahlleiste „Duplizieren…" und bestätigt die Namensabfrage.</summary>
    private static void Duplizieren<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        Handlung(cut, R.ADM_BTN_DUPLIZIEREN).Click();
        NamensabfrageOk(cut);
    }

    /// <summary>Wählt in der Auswahlleiste den Löschknopf und bestätigt die Rückfrage.</summary>
    private static void Loeschen<T>(IRenderedComponent<T> cut, string knopf) where T : IComponent
    {
        Handlung(cut, knopf).Click();
        RueckfrageJa(cut);
    }

    private IRenderedComponent<BedarfAdminDialog> BedarfAdmin(Func<string, BedarfLoeschAusgang>? loeschen = null,
                                                              Func<int, string, KatalogSpeicherErgebnis>? duplizieren = null)
        => Render<BedarfAdminDialog>(b => b
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Stromverbraucher, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.BtnLoeschenText, "Löschen")
            .Add(x => x.Loeschen, loeschen ?? (_ => BedarfLoeschAusgang.Geloescht))
            .Add(x => x.Duplizieren, duplizieren ?? ((_, n) => new KatalogSpeicherErgebnis(true, "", n))));

    [Fact]
    public void BedarfAdmin_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = BedarfAdmin(loeschen: _ => Wirft<BedarfLoeschAusgang>());
        Loeschen(cut, "Löschen");
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void BedarfAdmin_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = BedarfAdmin(duplizieren: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        Duplizieren(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void BedarfAdmin_ohne_Ausnahme_zeigt_keinen_Satzgrund()
        => OhneSatzgrund(BedarfAdmin());

    private IRenderedComponent<GebaeudetypDialog> Gebaeudetyp(Func<int, bool>? loeschen = null,
                                                              Func<int, string, KatalogSpeicherErgebnis>? duplizieren = null)
        => Render<GebaeudetypDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeudetyp(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.Duplizieren, duplizieren ?? ((_, n) => new KatalogSpeicherErgebnis(true, "", n))));

    [Fact]
    public void Gebaeudetyp_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Gebaeudetyp(loeschen: _ => Wirft<bool>());
        Loeschen(cut, R.PSP_TITEL_LOESCHEN);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Gebaeudetyp_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = Gebaeudetyp(duplizieren: (_, _) => Wirft<KatalogSpeicherErgebnis>());
        Duplizieren(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Gebaeudetyp_ohne_Ausnahme_zeigt_keinen_Satzgrund()
        => OhneSatzgrund(Gebaeudetyp());

    private IRenderedComponent<BaustoffKatalogDialog> Baustoff(Func<int, BaustoffSpeicherErgebnis>? loeschen = null,
                                                               Func<int, string, BaustoffSpeicherErgebnis>? duplizieren = null)
        => Render<BaustoffKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBaustoff(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Loeschen, loeschen ?? (id => new BaustoffSpeicherErgebnis(true, "", id)))
            .Add(x => x.Duplizieren, duplizieren ?? ((_, _) => new BaustoffSpeicherErgebnis(true, "", 2))));

    [Fact]
    public void Baustoff_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Baustoff(loeschen: _ => Wirft<BaustoffSpeicherErgebnis>());
        Loeschen(cut, R.BST_BTN_LOESCHEN);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Baustoff_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = Baustoff(duplizieren: (_, _) => Wirft<BaustoffSpeicherErgebnis>());
        Duplizieren(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Baustoff_ohne_Ausnahme_zeigt_keinen_Satzgrund()
        => OhneSatzgrund(Baustoff());

    private IRenderedComponent<BauteilaufbauDialog> Bauteilaufbau(Func<int, BauteilaufbauSpeicherErgebnis>? loeschen = null,
                                                                  Func<int, string, BauteilaufbauSpeicherErgebnis>? duplizieren = null)
        => Render<BauteilaufbauDialog>(b => b
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBauteilaufbau(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Loeschen, loeschen ?? (id => new BauteilaufbauSpeicherErgebnis(true, "", id)))
            .Add(x => x.Duplizieren, duplizieren ?? ((_, _) => new BauteilaufbauSpeicherErgebnis(true, "", 2))));

    [Fact]
    public void Bauteilaufbau_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Bauteilaufbau(loeschen: _ => Wirft<BauteilaufbauSpeicherErgebnis>());
        Loeschen(cut, R.BTA_BTN_LOESCHEN);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void Bauteilaufbau_wirft_das_Duplizieren_nennt_der_Dialog_den_Grund()
    {
        var cut = Bauteilaufbau(duplizieren: (_, _) => Wirft<BauteilaufbauSpeicherErgebnis>());
        Duplizieren(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void Bauteilaufbau_ohne_Ausnahme_zeigt_keinen_Satzgrund()
        => OhneSatzgrund(Bauteilaufbau());

    private IRenderedComponent<GebaeudeDialog> Gebaeude(Func<string, GebaeudeProjektZeile?>? stammSatz = null,
                                                      Func<string, bool>? loeschen = null,
                                                      List<GebaeudeProjektZeile>? zeilen = null,
                                                      Func<string>? listeSpeichern = null)
        => Render<GebaeudeDialog>(b => b
            .Add(x => x.Zeilen, zeilen ?? new List<GebaeudeProjektZeile>())
            .Add(x => x.ListeSpeichern, listeSpeichern)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.StammDetail, _ => null)
            .Add(x => x.StammSatz, stammSatz)
            .Add(x => x.KatalogLoeschen, loeschen));

    [Fact]
    public void Gebaeude_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Gebaeude(stammSatz: _ => Wirft<GebaeudeProjektZeile?>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Gebaeude_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Gebaeude(loeschen: _ => Wirft<bool>());
        ZeileWaehlen(cut);
        Knopf(cut, "Gebäude in DB löschen").Click();
        Knopf(cut, "Ja").Click();
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    /// <summary>
    /// Das Speichern der Projektliste: Vor dem Löschen eines Katalogsatzes, den eine ungespeicherte
    /// Projektzeile noch braucht, schreibt der Dialog still; wirft das, unterbleibt das Löschen.
    /// </summary>
    [Fact]
    public void Gebaeude_wirft_das_Speichern_der_Liste_nennt_der_Dialog_den_Grund_und_loescht_nicht()
    {
        bool geloescht = false;
        var zeile = new GebaeudeProjektZeile { Name = NAME, IdKatalog = 1, HatProjektkopie = false };
        var cut = Gebaeude(loeschen: _ => geloescht = true,
                           zeilen: new List<GebaeudeProjektZeile> { zeile },
                           listeSpeichern: Wirft<string>);
        ZeileWaehlen(cut);
        Knopf(cut, "Gebäude in DB löschen").Click();
        Knopf(cut, "Ja").Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.False(geloescht);
    }

    [Fact]
    public void Gebaeude_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Gebaeude();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    private IRenderedComponent<BedarfsProfileDialog> Bedarf(Func<string, BedarfsProfilZeile?>? aufnehmen = null,
                                                          Func<string, bool>? loeschen = null,
                                                          Func<string>? speichern = null)
        => Render<BedarfsProfileDialog>(b => b
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Zeilen, new List<BedarfsProfilZeile>())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Prozesswaerme, s => s).MitVerwendungsspalte(s => s))
            .Add(x => x.Katalogzeilen, Eine)
            .Add(x => x.Info, _ => null)
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KatalogLoeschen, loeschen));

    [Fact]
    public void BedarfsProfile_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Bedarf(aufnehmen: _ => Wirft<BedarfsProfilZeile?>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void BedarfsProfile_wirft_das_Loeschen_nennt_der_Dialog_den_Grund()
    {
        var cut = Bedarf(loeschen: _ => Wirft<bool>());
        ZeileWaehlen(cut);
        Knopf(cut, R.BPF_BTN_KAT_LOESCHEN).Click();
        Knopf(cut, "Ja").Click();
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
    }

    [Fact]
    public void BedarfsProfile_wirft_das_Speichern_beim_OK_nennt_der_Dialog_den_Grund()
    {
        var cut = Bedarf(speichern: Wirft<string>);
        Knopf(cut, "OK").Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void BedarfsProfile_ohne_Ausnahme_zeigt_keinen_Satzgrund()
    {
        var cut = Bedarf();
        ZeileWaehlen(cut);
        OhneSatzgrund(cut);
    }

    [Fact]
    public void TwwNutzungsart_ohne_Ausnahme_zeigt_keinen_Satzgrund()
        => OhneSatzgrund(Render<TwwNutzungsartAdminDialog>(b => b
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerTwwNutzungsart(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Katalogzeilen, () => new[] { new Katalogfilterzeile(1, NAME) { Schluessel = "1" } })
            .Add(x => x.Detail, id => new TwwNutzungsartDetailDaten { Id = id, Name = NAME })));

    // =================================================================================
    // KT-6: PV-Ganglinie und Solarganglinie — Speichern beim OK, Aufnehmen, Löschen samt Frage nach der
    // Projektzuordnung, Aufnahme nach dem Import, Nennleistung und „Aus dem Katalog erneuern"
    // =================================================================================

    private static IReadOnlyList<Katalogfilterzeile> Ganglinien() => new[] { Zeitreihenproben.Zeile(31, NAME) };

    private static ErzeugerZeile Projektzeile(int id) => new() { Schluessel = 1, Bezeichner = NAME, GeraetId = id };

    private static GanglinienKatalogwege GanglinienWege(Func<string, Task<bool>>? loeschen = null,
                                                        Func<string, Task<bool>>? zuordnung = null,
                                                        Func<string, IProgress<ImportFortschritt>, Task<GanglinienKatalogimport>>? einlesen = null,
                                                        Func<string, double?, Task<GanglinienNennleistungsschrieb>>? nennleistung = null)
        => new()
        {
            Katalogzeilen = () => Task.FromResult(Ganglinien()),
            Loeschen = loeschen ?? (_ => Task.FromResult(true)),
            HatProjektzuordnung = zuordnung,
            DateiWaehlen = _ => Task.FromResult<string?>("D:/Daten/Ganglinie.csv"),
            Einlesen = einlesen,
            NennleistungSchreiben = nennleistung
        };

    private IRenderedComponent<PvGanglinieDialog> PvGanglinie(GanglinienKatalogwege? wege = null,
                                                             Func<int, ErzeugerZeile?>? aufnehmen = null,
                                                             Func<bool>? speichern = null,
                                                             Func<string, Task<PvGanglinieErneuerung>>? erneuern = null,
                                                             Action<bool>? geschlossen = null,
                                                             bool katalogbetrieb = false,
                                                             bool mitProjektzeile = false)
        => Render<PvGanglinieDialog>(p => p
            .Add(x => x.Zeilen, mitProjektzeile ? new List<ErzeugerZeile> { Projektzeile(31) } : new List<ErzeugerZeile>())
            .Add(x => x.Katalogbetrieb, katalogbetrieb)
            .Add(x => x.Katalogwege, wege ?? GanglinienWege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.PvGanglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, aufnehmen ?? (id => new ErzeugerZeile { Schluessel = 100000, Bezeichner = NAME, GeraetId = id }))
            .Add(x => x.Speichern, speichern)
            .Add(x => x.KatalogAbweichung, _ => "weicht vom Katalog ab: Reihe")
            .Add(x => x.AusKatalogErneuern, erneuern ?? (_ => Task.FromResult(new PvGanglinieErneuerung(true, "erneuert"))))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private IRenderedComponent<SolarganglinieDialog> Solarganglinie(GanglinienKatalogwege? wege = null,
                                                                   Func<int, ErzeugerZeile?>? aufnehmen = null,
                                                                   Func<bool>? speichern = null,
                                                                   Action<bool>? geschlossen = null,
                                                                   bool katalogbetrieb = false)
        => Render<SolarganglinieDialog>(p => p
            .Add(x => x.Zeilen, new List<ErzeugerZeile>())
            .Add(x => x.Katalogbetrieb, katalogbetrieb)
            .Add(x => x.Katalogwege, wege ?? GanglinienWege())
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Solarganglinie))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, aufnehmen ?? (id => new ErzeugerZeile { Schluessel = 100000, Bezeichner = NAME, GeraetId = id }))
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    /// <summary>Löscht die gewählte Katalogzeile über die Katalogseite (Knopf und Rückfrage).</summary>
    private static void GanglinieLoeschen<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        ZeileWaehlen(cut);
        cut.Find("button.epos-katalog-loeschen").Click();
        Knopf(cut, R.ALLG_BTN_JA).Click();
    }

    /// <summary>Import… → Datei wählen → Einlesen.</summary>
    private static void GanglinieEinlesen<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        cut.Find("button.epos-importknopf").Click();
        Knopf(cut, R.SGAD_BTN_DATEI).Click();
        Knopf(cut, R.SGAD_BTN_EINLESEN).Click();
    }

    private void OhneVermerk()
    {
        lock (_vermerke) Assert.Empty(_vermerke);
    }

    [Fact]
    public void PvGanglinie_wirft_das_Speichern_beim_OK_bleibt_der_Dialog_mit_den_Eingaben_offen()
    {
        bool? geschlossen = null;
        var cut = PvGanglinie(speichern: Wirft<bool>, geschlossen: b => geschlossen = b, mitProjektzeile: true);
        Knopf(cut, "OK").Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Null(geschlossen);
        Assert.Single(cut.Instance.Zeilen);
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_schliesst_OK_nach_dem_Speichern_und_ein_Nein_haelt_offen()
    {
        bool? geschlossen = null;
        bool antwort = false;
        int gerufen = 0;
        var cut = PvGanglinie(speichern: () => { gerufen++; return antwort; }, geschlossen: b => geschlossen = b,
                              mitProjektzeile: true);
        Knopf(cut, "OK").Click();
        Assert.Null(geschlossen);
        Assert.Contains(R.PVG_MSG_SCHREIBFEHLER, Text(cut));

        antwort = true;
        Knopf(cut, "OK").Click();
        Assert.True(geschlossen);
        Assert.Equal(2, gerufen);
        OhneVermerk();
    }

    [Fact]
    public void PvGanglinie_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = PvGanglinie(aufnehmen: _ => Wirft<ErzeugerZeile?>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_nimmt_auf()
    {
        var cut = PvGanglinie();
        Uebernehmen(cut);
        Assert.Single(cut.Instance.Zeilen);
        Assert.Equal("", cut.Instance.SchreibFehler);
        OhneVermerk();
    }

    [Fact]
    public void PvGanglinie_wirft_das_Loeschen_nennt_die_Katalogseite_den_Grund()
    {
        var cut = PvGanglinie(wege: GanglinienWege(loeschen: _ => Wirft<Task<bool>>()), katalogbetrieb: true);
        GanglinieLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
        Assert.Single(cut.Instance.Katalog);
    }

    [Fact]
    public void PvGanglinie_wirft_die_Frage_nach_der_Projektzuordnung_loescht_nicht_und_nennt_den_Grund()
    {
        bool geloescht = false;
        var cut = PvGanglinie(wege: GanglinienWege(loeschen: _ => { geloescht = true; return Task.FromResult(true); },
                                                   zuordnung: _ => Wirft<Task<bool>>()),
                              katalogbetrieb: true);
        ZeileWaehlen(cut);
        cut.Find("button.epos-katalog-loeschen").Click();
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
        Assert.False(geloescht);
        Assert.False(cut.Instance.Katalogseite!.Loeschfrage);
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_loescht()
    {
        var namen = new List<string>();
        var cut = PvGanglinie(wege: GanglinienWege(loeschen: n => { namen.Add(n); return Task.FromResult(true); },
                                                   zuordnung: _ => Task.FromResult(false)),
                              katalogbetrieb: true);
        GanglinieLoeschen(cut);
        Assert.Equal(new[] { NAME }, namen);
        Assert.Contains(NAME, cut.Instance.Katalogseite!.Status);
        OhneVermerk();
    }

    [Fact]
    public void PvGanglinie_wirft_die_Aufnahme_nach_dem_Import_bleibt_die_Ueberlagerung_mit_dem_Grund_offen()
    {
        var cut = PvGanglinie(wege: GanglinienWege(einlesen: (_, _) => Wirft<Task<GanglinienKatalogimport>>()),
                              katalogbetrieb: true);
        GanglinieEinlesen(cut);
        Assert.True(cut.Instance.Katalogseite!.ImportOffen);
        Assert.Contains(Mitte(R.WURZEL_SCHREIBEN_FEHLER) + GRUND, cut.Instance.Katalogseite.Importmeldung);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_nimmt_den_Import_in_den_Katalog_auf()
    {
        var cut = PvGanglinie(wege: GanglinienWege(einlesen: (_, _) =>
                                  Task.FromResult(new GanglinienKatalogimport(true, false, NAME, "", ""))),
                              katalogbetrieb: true);
        GanglinieEinlesen(cut);
        Assert.False(cut.Instance.Katalogseite!.ImportOffen);
        Assert.Equal(NAME, cut.Instance.Katalogzeile?.Bezeichner);
        OhneVermerk();
    }

    [Fact]
    public void PvGanglinie_wirft_das_Schreiben_der_Nennleistung_bleibt_die_Ueberlagerung_mit_dem_Grund_offen()
    {
        var cut = PvGanglinie(wege: GanglinienWege(nennleistung: (_, _) => Wirft<Task<GanglinienNennleistungsschrieb>>()),
                              katalogbetrieb: true);
        ZeileWaehlen(cut);
        cut.Find("button.epos-nennleistungknopf").Click();
        cut.Find(".epos-nennleistung-bearbeiten input[inputmode=decimal]").Input("5");
        cut.Find(".epos-nennleistung-bearbeiten").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(cut.Instance.Katalogseite!.NennleistungOffen);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }

    [Fact]
    public void PvGanglinie_wirft_das_Erneuern_bleibt_die_Rueckfrage_offen_und_die_Projektkopie_unveraendert()
    {
        var cut = PvGanglinie(erneuern: _ => Wirft<Task<PvGanglinieErneuerung>>(), mitProjektzeile: true);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr button")[0].Click();
        cut.Find(".epos-pvg-erneuernknopf").Click();
        cut.Find(".epos-pvg-erneuern").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.True(cut.Instance.ErneuernOffen);
        Assert.Contains(Mitte(R.WURZEL_SCHREIBEN_FEHLER) + GRUND, cut.Find(".epos-pvg-erneuern").TextContent);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        // Die Abweichung steht weiter: Die Kopie ist nicht erneuert, ihr Zeichen bleibt.
        Assert.Single(cut.FindAll(".epos-pvg-abweichung"));
        Assert.Equal("weicht vom Katalog ab: Reihe", cut.Instance.ProjektAbweichung);
    }

    [Fact]
    public void PvGanglinie_ohne_Ausnahme_erneuert()
    {
        var cut = PvGanglinie(mitProjektzeile: true);
        cut.FindAll(".epos-raster")[0].QuerySelectorAll("tbody tr button")[0].Click();
        cut.Find(".epos-pvg-erneuernknopf").Click();
        cut.Find(".epos-pvg-erneuern").QuerySelectorAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.False(cut.Instance.ErneuernOffen);
        Assert.Contains("erneuert", cut.Find(".epos-pvg-erneuernstatus").TextContent);
        OhneVermerk();
    }

    [Fact]
    public void Solarganglinie_wirft_das_Speichern_beim_OK_bleibt_der_Dialog_offen()
    {
        bool? geschlossen = null;
        var cut = Solarganglinie(speichern: Wirft<bool>, geschlossen: b => geschlossen = b);
        Knopf(cut, "OK").Click();
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Null(geschlossen);
    }

    [Fact]
    public void Solarganglinie_ohne_Ausnahme_schliesst_OK_nach_dem_Speichern()
    {
        bool? geschlossen = null;
        int gerufen = 0;
        var cut = Solarganglinie(speichern: () => { gerufen++; return true; }, geschlossen: b => geschlossen = b);
        Knopf(cut, "OK").Click();
        Assert.True(geschlossen);
        Assert.Equal(1, gerufen);
        OhneVermerk();
    }

    [Fact]
    public void Solarganglinie_wirft_das_Aufnehmen_nennt_der_Dialog_den_Grund_und_nimmt_nichts_auf()
    {
        var cut = Solarganglinie(aufnehmen: _ => Wirft<ErzeugerZeile?>());
        Uebernehmen(cut);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
        Assert.Empty(cut.Instance.Zeilen);
    }

    [Fact]
    public void Solarganglinie_wirft_das_Loeschen_nennt_die_Katalogseite_den_Grund()
    {
        var cut = Solarganglinie(wege: GanglinienWege(loeschen: _ => Wirft<Task<bool>>()), katalogbetrieb: true);
        GanglinieLoeschen(cut);
        GrundSteht(cut, R.WURZEL_LOESCHEN_FEHLER);
        Assert.Single(cut.Instance.Katalog);
    }

    [Fact]
    public void Solarganglinie_wirft_die_Aufnahme_nach_dem_Import_bleibt_die_Ueberlagerung_mit_dem_Grund_offen()
    {
        var cut = Solarganglinie(wege: GanglinienWege(einlesen: (_, _) => Wirft<Task<GanglinienKatalogimport>>()),
                                 katalogbetrieb: true);
        GanglinieEinlesen(cut);
        Assert.True(cut.Instance.Katalogseite!.ImportOffen);
        GrundSteht(cut, R.WURZEL_SCHREIBEN_FEHLER);
    }
}

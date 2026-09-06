using System.Globalization;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Photovoltaik;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Wechselrichter-VERWALTUNG (Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026,
/// <c>Konzept_Wechselrichter_EPOS-Plan.md</c>) — die dritte Ausprägung von
/// <see cref="ModulKatalogDialog"/>.
///
/// <para>Der IMPORT stand bis W6‑O‑1 mit in dieser Klasse. Seit dem einen Importwirt
/// (<c>ModulImportDialog</c>, zwei Ausprägungen) stehen seine Fälle in
/// <c>ModulImportDialogTests</c> neben denen des Modulimports.</para>
///
/// <para><b>Ohne Vorläufer, also ohne Feldkarte.</b> Die zehn anderen
/// Katalogverwaltungen des Hauses gleichen ihre Felder gegen die vermessene
/// WinForms-Maske ab; hier gibt es keine — der Wechselrichter war die einzige
/// Gerätefamilie ohne Katalog. Soll ist deshalb das PROFIL (Konzept 6): 25 Felder in
/// drei Gruppen, ein Pflichtfeld, ein Herstellerfilter.</para>
///
/// <para>Die Sprache pinnt die Klasse selbst (Regel seit iU9‑W8).</para>
/// </summary>
public class WechselrichterDialogTests : BunitContext
{
    public WechselrichterDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    /// <summary>Das Profil in DEUTSCH — so, wie die Hülle es liefert.</summary>
    private static ModulKatalogProfil Profil() =>
        ModulKatalogProfil.Finde(ModulKatalogArt.Wechselrichter,
            s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s);

    // =================================================================================
    //  Die VERWALTUNG — dritte Ausprägung des Modulkatalogs
    // =================================================================================

    private static IReadOnlyList<ModulFeldwert> Felder(string name)
    {
        var liste = new List<ModulFeldwert>();
        foreach (ModulKatalogFeld feld in Profil().Felder)
        {
            string wert = feld.Schluessel == ModulKatalogProfil.FeldBezeichner ? name
                        : feld.Schluessel == ModulKatalogProfil.FeldHerkunft ? DbWerte.WR_HERKUNFT_CEC
                        : feld.Art == BrowserFeldArt.Zahl ? "0,97"
                        : feld.Art == BrowserFeldArt.Ganzzahl ? "2"
                        : "Wert";

            liste.Add(new ModulFeldwert
            {
                Schluessel = feld.Schluessel,
                Bezeichnung = feld.Bezeichnung,
                Einheit = feld.Einheit,
                Art = feld.Art,
                LeerErlaubt = feld.LeerErlaubt,
                Gesperrt = feld.Gesperrt,
                Gruppe = feld.Gruppe,
                Wert = wert
            });
        }
        return liste;
    }

    private static IReadOnlyList<ModulZeile> Alle() => new[]
    {
        new ModulZeile(1, "Alpha 2500TL"),
        new ModulZeile(2, "Alpha 5000TL"),
        new ModulZeile(3, "Beta 10K")
    };

    private static IReadOnlyList<ModulZeile> Gefiltert(string hersteller)
    {
        if (string.IsNullOrEmpty(hersteller)) return Alle();
        return hersteller == "Alpha AG"
            ? new[] { new ModulZeile(1, "Alpha 2500TL"), new ModulZeile(2, "Alpha 5000TL") }
            : new[] { new ModulZeile(3, "Beta 10K") };
    }

    private IRenderedComponent<ModulKatalogDialog> Verwaltung(
        ModulKatalogWege? wege = null, Action<ModulErgebnis>? geschlossen = null)
    {
        ModulKatalogWege standard = new()
        {
            Liste = Alle,
            Hersteller = () => new[] { "Alpha AG", "Beta GmbH" },
            ListeGefiltert = Gefiltert,
            Detail = Felder,
            Speichern = (f, _, __) => new KatalogSpeicherErgebnis(
                true, "Datensatz gespeichert",
                f.First(x => x.Schluessel == ModulKatalogProfil.FeldBezeichner).Wert),
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
        };

        return Render<ModulKatalogDialog>(p => p
            .Add(x => x.Art, ModulKatalogArt.Wechselrichter)
            .Add(x => x.ProfilVorgabe, Profil())
            .Add(x => x.Wege, wege ?? standard)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    /// <summary>
    /// Titel, Liste und die 25 Felder des Profils — jedes mit seiner Beschriftung.
    /// </summary>
    [Fact]
    public void Die_Verwaltung_zeigt_ihren_Titel_und_alle_Felder()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        Assert.Equal("Verwaltung Wechselrichter", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Equal(3, cut.Instance.Zeilen.Count);
        Assert.Equal("Alpha 2500TL", cut.Instance.Gewaehlt);

        List<string> texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        foreach (ModulKatalogFeld feld in Profil().Felder)
            Assert.Contains(feld.Bezeichnung, texte);
    }

    /// <summary>
    /// <b>DREI Feldgruppen</b> (Konzept 6) — Gerät, Eingang, Wirkungsgrad. Der
    /// Wechselrichter ist die erste Ausprägung mit einer dritten; ein Block mit
    /// zwanzig Feldern wäre nicht lesbar.
    /// </summary>
    [Fact]
    public void Die_Verwaltung_gliedert_ihre_Felder_in_drei_Gruppen()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();
        List<string> titel = cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToList();

        // Liste + Geraet + Eingang + Wirkungsgrad
        Assert.Equal(4, titel.Count);
        Assert.Contains(Profil().GruppeBestand, titel);
        Assert.Contains(Profil().GruppeZwei, titel);
        Assert.Contains(Profil().GruppeDrei, titel);
    }

    /// <summary>
    /// <b>Der Herstellerfilter</b> (Konzept 6): Die dritte Ausprägung ist die einzige
    /// mit einem — die Wahl engt die Liste ein, „alle" hebt sie wieder auf.
    /// </summary>
    [Fact]
    public void Der_Herstellerfilter_engt_die_Liste_ein_und_gibt_sie_wieder_frei()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        // Die Filterzeile steht ueber der Liste und traegt "alle" plus zwei Firmen.
        var wahl = cut.FindAll(".epos-katalog-liste select")[0];
        Assert.Equal(3, wahl.QuerySelectorAll("option").Count);
        Assert.Equal(3, cut.Instance.Zeilen.Count);

        wahl.Change("1");                       // Alpha AG
        Assert.Equal("Alpha AG", cut.Instance.GewaehlterHersteller);
        Assert.Equal(2, cut.Instance.Zeilen.Count);

        wahl.Change("2");                       // Beta GmbH
        Assert.Equal(1, cut.Instance.Zeilen.Count);

        wahl.Change("0");                       // alle
        Assert.Equal("", cut.Instance.GewaehlterHersteller);
        Assert.Equal(3, cut.Instance.Zeilen.Count);
    }

    /// <summary>
    /// <b>Ohne Delegaten kein Bedienelement</b> — die Hausregel des Dateiwählers gilt
    /// auch hier: Ein Filter ohne Datenweg wäre eine Liste, die nichts bewirkt.
    /// </summary>
    [Fact]
    public void Ohne_Herstellerweg_gibt_es_keine_Filterzeile()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung(new ModulKatalogWege
        {
            Liste = Alle,
            Detail = Felder
        });

        Assert.Empty(cut.FindAll(".epos-katalog-liste select"));
        Assert.Equal(3, cut.Instance.Zeilen.Count);
    }

    /// <summary>
    /// Bezeichner und Herkunft sind gesperrt: der eine ist der WHERE-Schlüssel, die
    /// andere die Auskunft des Imports.
    /// </summary>
    [Fact]
    public void Bezeichner_und_Herkunft_sind_gesperrt()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();
        var gesperrt = cut.FindAll("input[readonly]");

        Assert.Equal(2, gesperrt.Count);
        Assert.Equal("Alpha 2500TL", gesperrt[0].GetAttribute("value"));
        Assert.Equal(DbWerte.WR_HERKUNFT_CEC, gesperrt[1].GetAttribute("value"));
    }

    /// <summary>
    /// Eine andere Zeile zieht ihren Feldsatz nach — der Weg, den jede
    /// Katalogverwaltung des Hauses geht.
    /// </summary>
    [Fact]
    public void Eine_andere_Zeile_zieht_ihren_Feldsatz_nach()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        cut.FindAll(".epos-anlagenwahl")[2].Click();

        Assert.Equal("Beta 10K", cut.Instance.Gewaehlt);
        Assert.Equal("Beta 10K", cut.FindAll("input[readonly]")[0].GetAttribute("value"));
    }

    /// <summary>
    /// „Neu…" fragt erst den Namen und belegt danach die Herkunft mit
    /// <see cref="DbWerte.WR_HERKUNFT_HAND"/> — ein handgepflegter Satz sagt das von
    /// sich.
    /// </summary>
    [Fact]
    public void Neu_belegt_die_Herkunft_mit_HAND()
    {
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung();

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
        Assert.True(cut.Instance.Namensfrage);

        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Neues Geraet");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        Assert.True(cut.Instance.IstNeu);

        List<string?> gesperrt = cut.FindAll("input[readonly]")
                                    .Select(e => e.GetAttribute("value")).ToList();
        Assert.Contains("Neues Geraet", gesperrt);
        Assert.Contains(DbWerte.WR_HERKUNFT_HAND, gesperrt);
    }

    /// <summary>
    /// <b>Das EINE Pflichtfeld</b> ist die AC-Nennleistung (Konzept 6): Ein leeres
    /// Feld meldet sich, alles andere darf leer bleiben und schaltet dann seine
    /// Prüfung ab.
    /// </summary>
    [Fact]
    public void Nur_die_AC_Nennleistung_ist_Pflicht()
    {
        var leer = new List<ModulFeldwert>(Felder("Alpha 2500TL"));
        foreach (ModulFeldwert f in leer)
            if (!f.Gesperrt) f.Wert = "";

        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung(new ModulKatalogWege
        {
            Liste = Alle,
            Hersteller = () => new[] { "Alpha AG" },
            ListeGefiltert = Gefiltert,
            Detail = _ => leer,
            Speichern = (_, __, ___) => new KatalogSpeicherErgebnis(true, "gespeichert", "Alpha 2500TL")
        });

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.Contains(Profil().Felder
                            .Single(f => f.Schluessel == ModulKatalogProfil.FeldPAcNenn).Feldname,
                        cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Gelöscht wird mit Rückfrage</b> — die Angleichung E-3 der Welle 14 gilt für
    /// jede Ausprägung dieser Komponente.
    /// </summary>
    [Fact]
    public void Loeschen_fragt_zurueck()
    {
        string? geloescht = null;
        IRenderedComponent<ModulKatalogDialog> cut = Verwaltung(new ModulKatalogWege
        {
            Liste = Alle,
            Hersteller = () => new[] { "Alpha AG" },
            ListeGefiltert = Gefiltert,
            Detail = Felder,
            Loeschen = n => { geloescht = n; return new KatalogSpeicherErgebnis(true, "", n); }
        });

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.Null(geloescht);

        cut.Find(".epos-rueckfrage .epos-knopf--primaer").Click();
        Assert.Equal("Alpha 2500TL", geloescht);
    }

    // =================================================================================
    //  Der IMPORT steht seit W6-O-1 (06.09.2026) in ModulImportDialogTests: Aus
    //  PvModulImportDialog und WechselrichterImportDialog ist EIN Wirt geworden
    //  (ModulImportDialog, zwei Auspraegungen), und die Faelle beider Masken stehen
    //  jetzt beieinander - samt dem OND-Zweig, der mit demselben Schritt kam.
    // =================================================================================
}

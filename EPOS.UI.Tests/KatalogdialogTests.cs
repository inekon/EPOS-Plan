using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Klimadaten;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die KATALOG- UND ADMINISTRATIONSDIALOGE nutzen die Fläche — Anwenderwunsch
/// vom 05.09.2026: „Admin-Menüs sind nicht an Größe Bildschirm angepasst".
///
/// <para><b>Der Befund am Gerät</b> (Bildschirmfoto „Administration
/// Solarkollektoren"): Überschrift, Balken „Auswahl in DB:", eine Liste in
/// ihrem eigenen kleinen Rollrahmen — und darunter, nur über den
/// SEITENrollbalken erreichbar, der Balken „Eingabe der Solarkollektoren" mit
/// den Feldern. Dazu eine Kopfzeile, die „Name | Name | Eigenschaften" las,
/// weil in der Wahlspalte die Beschriftung der NACHBARSPALTE stand.</para>
///
/// <para><b>Was dieser Fall festhält</b>, quer über die Dialogfamilien:
/// (a) die Wurzel trägt <c>epos-katalog-dialog</c> und nutzt damit die Höhe,
/// (b) Liste und Eingabeblock stehen im <c>Katalograhmen</c> — der
/// Eingabeblock ist also IM DOM und nicht hinter einem Seitenrollbalken,
/// (c) die erste Spalte der Katalogliste heißt „Wahl".</para>
///
/// <para>Die Rechnung hinter dem Fenstermaß prüft <c>FenstermassTests</c>, die
/// Regeln im Stilblatt <c>Bausteine/KatalograhmenTests</c>.</para>
///
/// <para>Die Sprache pinnt die Klasse selbst (Hausregel seit iU9‑W8): Die
/// Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class KatalogdialogTests : EposBunitContext
{
    public KatalogdialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Die Prüfstände — je Dialogfamilie der kleinste tragfähige Aufbau
    // =====================================================================

    private static KatalogBrowserProfil BrowserProfil(KatalogBrowserArt art) =>
        KatalogBrowserProfil.Finde(art, s => Resource.ResourceManager.GetString(s) ?? s);

    private IRenderedComponent<KatalogBrowserDialog> Katalogbrowser(
        KatalogBrowserArt art = KatalogBrowserArt.Solarkollektoren)
    {
        KatalogBrowserProfil profil = BrowserProfil(art);

        var wege = new KatalogBrowserWege
        {
            Katalogzeilen = () => new[]
            {
                new Katalogfilterzeile(1, "Kollektor A").MitText(Katalogfilterprofil.SpBezeichner, "Kollektor A"),
                new Katalogfilterzeile(2, "Kollektor B").MitText(Katalogfilterprofil.SpBezeichner, "Kollektor B")
            },
            Detail = name => profil.Detailfelder.Select(f => new BrowserFeldwert
            {
                Schluessel = f.Schluessel,
                Bezeichnung = f.Bezeichnung,
                Einheit = f.Einheit,
                Art = f.Art,
                Editierbar = f.Editierbar,
                Wert = f.Schluessel == KatalogBrowserProfil.FeldBezeichner ? name : "1"
            }).ToList(),
            Existiert = _ => false,
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n),
            Speichern = (n, _, __) => new KatalogSpeicherErgebnis(true, "", n)
        };

        return Render<KatalogBrowserDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.ProfilVorgabe, profil)
            .Add(x => x.Wege, wege)
            .Add(x => x.SpalteWahlText, Resource.KFAK_SP_WAHL)
            );
    }

    private IRenderedComponent<ModulKatalogDialog> Modulkatalog(
        ModulKatalogArt art = ModulKatalogArt.Photovoltaik)
    {
        ModulKatalogProfil profil =
            ModulKatalogProfil.Finde(art, s => Resource.ResourceManager.GetString(s) ?? s);

        var wege = new ModulKatalogWege
        {
            Katalogzeilen = () => new[] { new Katalogfilterzeile(1, "Modul A").MitText(Katalogfilterprofil.SpBezeichner, "Modul A") },
            Detail = name => profil.Felder.Select(f => new ModulFeldwert
            {
                Schluessel = f.Schluessel,
                Bezeichnung = f.Bezeichnung,
                Einheit = f.Einheit,
                Art = f.Art,
                Gruppe = f.Gruppe,
                Wert = f.Schluessel == ModulKatalogProfil.FeldBezeichner ? name : "1"
            }).ToList(),
            Speichern = (_, __, ___) => new KatalogSpeicherErgebnis(true, "", "Modul A"),
            Loeschen = n => new KatalogSpeicherErgebnis(true, "", n)
        };

        return Render<ModulKatalogDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.ProfilVorgabe, profil)
            .Add(x => x.Wege, wege)
            .Add(x => x.SpalteWahlText, Resource.KFAK_SP_WAHL));
    }

    private IRenderedComponent<SolarganglinieAdminDialog> Solarganglinienverwaltung() =>
        Render<SolarganglinieAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(
                (IReadOnlyList<Katalogfilterzeile>)new[]
                {
                    new Katalogfilterzeile(1, "Ganglinie A")
                        .MitText(Katalogfilterprofil.SpBezeichner, "Ganglinie A")
                        .MitText(Katalogfilterprofil.SpBeschreibung, "Beschreibung")
                }))
            .Add(x => x.Katalogprofil,
                 Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Solarganglinie, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    /// <summary>
    /// Die Stromganglinienverwaltung (A8) steht seit Stufe 1 der Neuordnung
    /// (Konzept Administrationsdialoge) im gemeinsamen Rahmen — vorher stand sie frei
    /// in der Maske.
    /// </summary>
    private IRenderedComponent<StromganglinieAdminDialog> Stromganglinienverwaltung() =>
        Render<StromganglinieAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(
                (IReadOnlyList<Katalogfilterzeile>)new[]
                {
                    new Katalogfilterzeile(1, "Lastgang A")
                        .MitText(Katalogfilterprofil.SpBezeichner, "Lastgang A")
                }))
            .Add(x => x.Katalogprofil,
                 Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Stromganglinie, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    private IRenderedComponent<WaermebedarfAdminDialog> Waermebedarfsverwaltung() =>
        Render<WaermebedarfAdminDialog>(p => p
            .Add(x => x.Katalogzeilen, () => Task.FromResult(
                (IReadOnlyList<Katalogfilterzeile>)new[]
                {
                    new Katalogfilterzeile(1, "Bedarf A")
                        .MitText(Katalogfilterprofil.SpBezeichner, "Bedarf A")
                }))
            .Add(x => x.Katalogprofil,
                 Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.Waermebedarf, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    private IRenderedComponent<BedarfAdminDialog> Bedarfsverwaltung() =>
        Render<BedarfAdminDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.Katalogzeilen, () => (IReadOnlyList<Katalogfilterzeile>)new[]
            {
                new Katalogfilterzeile(1, "Verbraucher A")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Verbraucher A")
            })
            .Add(x => x.Katalogprofil,
                 Katalogfilterprofil.FuerBedarf(BedarfsArt.Stromverbraucher, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    private IRenderedComponent<KlimadatenDialog> Klimaregionen() =>
        Render<KlimadatenDialog>(p => p
            .Add(x => x.Regionen, () => Task.FromResult((IReadOnlyList<Katalogfilterzeile>)new[]
            {
                new Katalogfilterzeile(1, "Region A")
                    .MitText(Katalogfilterprofil.SpBezeichner, "Region A")
            }))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));

    private IRenderedComponent<GesetzeskatalogDialog> Gesetzeskatalog() =>
        Render<GesetzeskatalogDialog>(p => p
            .Add(x => x.Klassen, () => Task.FromResult(
                (IReadOnlyList<(string, string)>)new[] { ("CO2", "CO₂") }.ToList()))
            .Add(x => x.Zeilen, _ => Task.FromResult(
                (IReadOnlyList<Katalogfilterzeile>)new List<Katalogfilterzeile>())));

    // =====================================================================
    //  (a) Die Wurzel nutzt die Höhe
    // =====================================================================

    /// <summary>
    /// <c>epos-katalog-dialog</c> nimmt der Dialogwurzel die Breitenbremse von
    /// 1160 px und gibt ihr die Fensterhöhe; ohne sie rollte die SEITE statt
    /// der Liste. Acht Dialoge tragen sie — die Liste steht hier, damit ein
    /// neunter nicht still danebenfällt.
    /// </summary>
    [Fact]
    public void Jeder_Katalogdialog_traegt_die_Wurzelklasse()
    {
        Assert.All(new[]
        {
            Katalogbrowser().Find("div").ClassName,
            Modulkatalog().Find("div").ClassName,
            Solarganglinienverwaltung().Find("div").ClassName,
            Waermebedarfsverwaltung().Find("div").ClassName,
            Stromganglinienverwaltung().Find("div").ClassName,
            Bedarfsverwaltung().Find("div").ClassName,
            Klimaregionen().Find("div").ClassName,
            Gesetzeskatalog().Find("div").ClassName
        }, klasse => Assert.Contains("epos-katalog-dialog", klasse ?? ""));
    }

    // =====================================================================
    //  (b) Liste und Eingabeblock stehen NEBENEINANDER im selben Bild
    // =====================================================================

    /// <summary>
    /// Die Vorbilder stellten Liste und Eingabe nebeneinander — vier
    /// Katalogbrowser (726 × 383 bis 856 × 517), zwei Modulkataloge
    /// (607 × 489 / 614 × 367), die Solarganglinienverwaltung (681 × 344) und
    /// die Klimaregionen (757 × 641).
    /// </summary>
    [Fact]
    public void Liste_und_Eingabe_stehen_im_Katalograhmen()
    {
        RahmenPruefen(Katalogbrowser(), gestapelt: false);
        RahmenPruefen(Modulkatalog(), gestapelt: false);
        RahmenPruefen(Solarganglinienverwaltung(), gestapelt: false);
        RahmenPruefen(Klimaregionen(), gestapelt: false);
    }

    /// <summary>
    /// Der Rahmen steht, die Liste ist in seiner linken Spalte, und der
    /// Eingabeblock ist IM DOM — also nicht hinter einem Seitenrollbalten.
    /// </summary>
    private static void RahmenPruefen<T>(IRenderedComponent<T> cut, bool gestapelt)
        where T : Microsoft.AspNetCore.Components.IComponent
    {
        Assert.Single(cut.FindAll(".epos-katalog-paar"));

        if (gestapelt) Assert.Single(cut.FindAll(".epos-katalog-paar--gestapelt"));
        else Assert.Empty(cut.FindAll(".epos-katalog-paar--gestapelt"));

        // Die Liste ist IM linken Feld …
        Assert.Single(cut.FindAll(".epos-katalog-liste .epos-raster-huelle"));

        // … und der Eingabeblock trägt Inhalt, nicht nur eine leere Spalte.
        Assert.NotEmpty(cut.Find(".epos-katalog-eingabe").TextContent.Trim());
    }

    /// <summary>
    /// <b>Keine Verwaltung steht mehr gestapelt.</b> Wo das Vorbild gestapelt war —
    /// <c>Form_AdminWaermeeinlesen</c> (676 × 433, Liste über die volle Breite, der
    /// Einleseblock darunter) und die Stromganglinie —, steht seit Stufe 4 der
    /// Neuordnung das Stammblatt NEBEN der Liste (V3), und das Einlesen ist eine
    /// Überlagerung (V14). Die Bedarfsverwaltung (<c>Form_Stromverbraucher_Admin</c>)
    /// kam mit Stufe 3 dorthin.
    /// </summary>
    [Fact]
    public void Die_frueher_gestapelten_Verwaltungen_stehen_im_Stammblatt()
    {
        var waerme = Waermebedarfsverwaltung();
        RahmenPruefen(waerme, gestapelt: false);
        Assert.Single(waerme.FindAll(".epos-katalog-paar--stammblatt .epos-katalog-stammblatt .epos-stammblatt"));

        var strom = Stromganglinienverwaltung();
        RahmenPruefen(strom, gestapelt: false);
        Assert.Single(strom.FindAll(".epos-katalog-paar--stammblatt .epos-katalog-stammblatt .epos-stammblatt"));

        var bedarf = Bedarfsverwaltung();
        RahmenPruefen(bedarf, gestapelt: false);
        Assert.Single(bedarf.FindAll(".epos-katalog-paar--stammblatt .epos-katalog-stammblatt .epos-stammblatt"));
    }

    /// <summary>
    /// Der Gesetzeskatalog hat keinen Eingabeblock — sein Zeileneditor ist eine
    /// Überlagerung. Sein Vorbild <c>Form_Gesetzesparameter</c> war 940 × 560
    /// gross und die Liste darin 916 × 424: Die Liste WAR die Maske. Sie
    /// bekommt deshalb keinen Rahmen, sondern die ganze Höhe.
    /// </summary>
    [Fact]
    public void Eine_Verwaltung_ohne_Eingabeblock_gibt_der_Liste_die_Hoehe()
    {
        var cut = Gesetzeskatalog();

        Assert.Empty(cut.FindAll(".epos-katalog-paar"));
        Assert.Single(cut.FindAll(".epos-katalog-liste.epos-katalog-fuellend"));
        Assert.Single(cut.FindAll(".epos-katalog-liste .epos-raster-huelle"));
    }

    // =====================================================================
    //  (c) Die Zeile ist die Wahl - die Wahlspalte ist in den Verwaltungen
    //      entfallen (Konzept Administrationsdialoge, Stufe 2, V4)
    // =====================================================================

    /// <summary>
    /// <b>Keine Wahlspalte mehr in der Verwaltung</b>: Der runde Knopf war das einzige
    /// Klickziel einer breiten Zeile; jetzt wählt ein Klick irgendwo in der Zeile. Die
    /// erste Kopfzelle ist die erste Spalte des Profils — und damit auch nicht mehr
    /// „Name | Name", der Befund, aus dem die Wahlspalte einst ihren Text bekam.
    /// </summary>
    [Theory]
    [InlineData(KatalogBrowserArt.Heizkessel)]
    [InlineData(KatalogBrowserArt.Bhkw)]
    [InlineData(KatalogBrowserArt.Solarkollektoren)]
    [InlineData(KatalogBrowserArt.Pufferspeicher)]
    public void Der_Katalogbrowser_hat_keine_Wahlspalte(KatalogBrowserArt art)
    {
        var cut = Katalogbrowser(art);
        var kopfzeilen = cut.FindAll(".epos-katalog-liste thead th")
                            .Select(e => e.TextContent.Trim()).ToList();

        Assert.DoesNotContain(Resource.KFAK_SP_WAHL, kopfzeilen);
        Assert.Empty(cut.FindAll(".epos-katalog-liste .epos-anlagenwahl"));
        Assert.NotEmpty(cut.FindAll(".epos-katalog-liste .epos-zeilenzelle"));
    }

    [Theory]
    [InlineData(ModulKatalogArt.Photovoltaik)]
    [InlineData(ModulKatalogArt.Stromspeicher)]
    public void Der_Modulkatalog_hat_keine_Wahlspalte(ModulKatalogArt art)
    {
        var cut = Modulkatalog(art);
        var kopfzeilen = cut.FindAll(".epos-katalog-liste thead th")
                            .Select(e => e.TextContent.Trim()).ToList();

        Assert.DoesNotContain(Resource.KFAK_SP_WAHL, kopfzeilen);
        Assert.Empty(cut.FindAll(".epos-katalog-liste .epos-anlagenwahl"));
    }

    /// <summary>
    /// Wo die Wahlspalte bleibt — in den Listen ohne <c>ZeileIstWahl</c> (Projektdialoge,
    /// Importe) —, heißt sie weiter nach dem Ressourcenkatalog: „Wahl" ist ein
    /// Ressourcentext, kein Literal; der englische Katalog führt „Select".
    /// </summary>
    [Fact]
    public void Der_Spaltentext_kommt_aus_dem_Ressourcenkatalog()
    {
        var cut = Render<EPOS.UI.Bausteine.Katalogliste>(p => p
            .Add(x => x.Profil, Katalogfilterprofil.Finde(Anlagenart.Heizkessel))
            .Add(x => x.Zeilen, new[] { new Katalogfilterzeile(1, "Kessel A")
                                        .MitText(Katalogfilterprofil.SpBezeichner, "Kessel A") })
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.Texte, new EPOS.UI.Bausteine.Katalogfiltertexte { SpalteWahl = Resource.KFAK_SP_WAHL }));

        Assert.Equal(Resource.KFAK_SP_WAHL, cut.FindAll("thead th")[0].TextContent.Trim());
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8‑E‑2 / W14a‑E‑7, 05.09.2026
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2 / W14a‑E‑7:</b> „Verbessere die Darstellung der Dialoge,
    /// insbesondere der Parameter auf der rechten Seite: kompakter,
    /// übersichtlicher, …"
    ///
    /// <para>Der Eingabeblock eines Katalogdialogs steht seither im
    /// <c>Formularraster</c>: Beschriftung NEBEN dem Feld, Zahlenfelder kurz mit
    /// der Einheit dahinter, zwei Feldpaare je Zeile, sobald die Spalte breit
    /// genug ist. Zuvor nahm jedes Feld die volle Breite und die Beschriftung
    /// stand darüber — der Block war doppelt so hoch wie der Dialog und
    /// rollte.</para>
    ///
    /// <para>Geprüft wird, dass der Raster IM EINGABEBLOCK steht (nicht
    /// irgendwo) und dass Felder darin liegen. Die Regeln dahinter hält
    /// <c>Bausteine/FormularrasterTests</c>.</para>
    /// </summary>
    [Fact]
    public void Der_Eingabeblock_eines_Katalogdialogs_steht_im_Formularraster()
    {
        Rasterprobe("KatalogBrowserDialog", Katalogbrowser().FindAll(Suchpfad));
        Rasterprobe("ModulKatalogDialog", Modulkatalog().FindAll(Suchpfad));
        Rasterprobe("BedarfAdminDialog", Bedarfsverwaltung().FindAll(Suchpfad));
        // Die Waermebedarfsverwaltung hat seit Stufe 4 keinen Eingabeblock mehr: Ihr
        // Stammblatt zeigt Ganglinie und Herkunft als TEXT (V13), das Einlesen steht in
        // einer Ueberlagerung (V14) - dort, im Formularraster.
    }

    private const string Suchpfad = ".epos-katalog-eingabe .epos-formularraster";

    private static void Rasterprobe(string name, IReadOnlyList<AngleSharp.Dom.IElement> raster)
    {
        Assert.True(raster.Count > 0, name + ": kein Formularraster im Eingabeblock");
        Assert.True(raster.Any(r => r.QuerySelectorAll(".epos-feld").Length > 0),
                    name + ": im Formularraster steht kein Feld");
    }

    /// <summary>
    /// Ein ZAHLENfeld des Katalogs meldet sich als kurzes Feld — daran hängt im
    /// Raster die kurze Breite und die Einheit unmittelbar dahinter. Im Vorbild
    /// (<c>Form_AdminPV</c>, gemessen) war das Feld 62 px breit und die Einheit
    /// stand 4 px danach; im Befund stand sie am rechten Rand des Blocks.
    /// </summary>
    [Fact]
    public void Die_Zahlenfelder_des_Modulkatalogs_sind_kurze_Felder()
    {
        var kurz = Modulkatalog(ModulKatalogArt.Photovoltaik)
                   .FindAll(".epos-katalog-eingabe .epos-feld--kurz");

        Assert.True(kurz.Count > 0, "kein kurzes Feld im Eingabeblock des Modulkatalogs");

        // Die Einheit steht IN der Feldzeile des kurzen Feldes.
        Assert.Contains(kurz, f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}

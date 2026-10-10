using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Übernahme, Projekttransfer, Brennstoffe des Projekts und Dublettenwerkzeug am
/// Hilfe-Assistenten (Freigabe der Masken, Teil C): je Maske ein Feld über den ganzen Weg von
/// <c>feld_setzen</c> gesetzt — der Dialog geht danach denselben Weg wie nach einer Eingabe von
/// Hand; Wahlfelder über ihren Text; gesperrte Felder vor der Bestätigung abgelehnt.
/// </summary>
public sealed class KiUebernahmeWerkzeugTests : EposBunitContext, IDisposable
{
    private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

    public KiUebernahmeWerkzeugTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
    }

    public new void Dispose()
    {
        Schreibnaht.Schreibrecht = _schreibrechtVorher;
        base.Dispose();
    }

    // =====================================================================
    //  Übernahme ins Projekt
    // =====================================================================

    private static readonly (int Id, string Text)[] PROJEKTE = { (1030, "Musterprojekt"), (1007, "Zweitprojekt") };

    private IRenderedComponent<VorlagenUebernahmeDialog> Uebernahme(
        bool zielWaehlbar = true, Func<VorlagenUebernahmeWahl, VorlagenUebernahmeVorschau>? vorschau = null,
        List<int>? anlagenGefragt = null)
        => Render<VorlagenUebernahmeDialog>(p => p
            .Add(x => x.Zielprojekte, PROJEKTE)
            .Add(x => x.ZielWaehlbar, zielWaehlbar)
            .Add(x => x.VorlagenZu, invest => invest ? new[] { (5, "Standard"), (9, "Nord") } : new[] { (7, "Betrieb") })
            .Add(x => x.Quellprojekte, PROJEKTE)
            .Add(x => x.AnlagenZu, id =>
            {
                anlagenGefragt?.Add(id);
                return id == 1007 ? new[] { (71, "Kessel 1"), (72, "BHKW 1") } : new[] { (11, "Kessel A") };
            })
            .Add(x => x.Vorschau, vorschau ?? (w => new VorlagenUebernahmeVorschau("Vorschau " + w.QuellAnlageId, true))));

    [Fact]
    public async Task Uebernahme_Quelle_und_Quellprojekt_ueber_den_Text_ziehen_die_Anlagen_nach()
    {
        var gefragt = new List<int>();
        var cut = Uebernahme(anlagenGefragt: gefragt);

        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.VORLAGEN_UEBERNAHME, "quelle", "Aus Projekt/Anlage:"))).Status);
        KiErgebnis projekt = await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.VORLAGEN_UEBERNAHME, "quellprojekt", "Zweitprojekt"));

        Assert.True(projekt.Status == KiStatus.Ausgefuehrt, projekt.Text);
        Assert.False(cut.Instance.AusVorlage);
        // Der Handweg zieht die Anlagenliste nach und rechnet die Vorschau neu.
        Assert.Contains(1007, gefragt);
        Assert.Equal(71, cut.Instance.QuellAnlage);
        cut.WaitForAssertion(() => Assert.Contains("Vorschau 71", cut.Markup));
    }

    [Fact]
    public async Task Uebernahme_Kategorie_bei_der_Quelle_Projekt_wird_vor_der_Bestaetigung_abgelehnt()
    {
        var cut = Uebernahme();
        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.VORLAGEN_UEBERNAHME, "quelle", "Aus Projekt/Anlage:"))).Status);

        KiVorbereitung kategorie = await cut.InvokeAsync(() =>
            Vorbereiten(KiMaskennamen.VORLAGEN_UEBERNAHME, "kategorie", "Betriebskosten"));
        Assert.Null(kategorie.Freigabe);
        Assert.Contains(Resource.KI_KUEB_NUR_VORLAGE, kategorie.Ablehnung.Text);

        KiVorbereitung ziel = await Uebernahme(zielWaehlbar: false).InvokeAsync(() =>
            Vorbereiten(KiMaskennamen.VORLAGEN_UEBERNAHME, "zielprojekt", "Zweitprojekt"));
        Assert.Null(ziel.Freigabe);
    }

    [Fact]
    public async Task Uebernahme_Kategorie_zieht_die_Varianten_nach_und_die_Pruefung_meldet_die_Vorschau()
    {
        var cut = Uebernahme(vorschau: w => new VorlagenUebernahmeVorschau(
            w.Invest ? "geht" : "Keine Position zu übernehmen.", w.Invest));

        Assert.Equal("", KiMaskenbruecke.Haken(KiMaskennamen.VORLAGEN_UEBERNAHME).Pruefen());
        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.VORLAGEN_UEBERNAHME, "kategorie", "Betriebskosten"))).Status);

        Assert.False(cut.Instance.Invest);
        Assert.Equal(7, cut.Instance.QuellVorlage);
        Assert.Equal("Keine Position zu übernehmen.", KiMaskenbruecke.Haken(KiMaskennamen.VORLAGEN_UEBERNAHME).Pruefen());
        Assert.False(cut.Instance.UebernahmeMoeglich);
    }

    // =====================================================================
    //  Projekt exportieren / importieren
    // =====================================================================

    private sealed class Import
    {
        public string Name = "";
        public ProjektExportImportCtrl.BeiVorhandenem Modus;
        public bool Gerufen;
    }

    private IRenderedComponent<ProjektTransferDialog> Transfer(Import import, bool mitSicherung)
        => Render<ProjektTransferDialog>(p => p.Add(x => x.Daten, new ProjektTransferDaten(
            Projekte: new[] { new ProjektKopfZeile(1030, "Referenz BHKW") },
            Varianten: _ => Array.Empty<string>(),
            Exportieren: (_, _, _, _) => true,
            PaketLesen: () => "C:\\pakete\\projekt.wpx",
            PaketSchreiben: v => "C:\\ziel\\" + v,
            Vorschau: _ => new PaketVorschau("Wöhler", "04.09.2026 12:00", 61, Array.Empty<string>(), ""),
            Importieren: (pfad, name, modus, _) =>
            {
                import.Gerufen = true; import.Name = name; import.Modus = modus;
                return new ImportErgebnis(4711, name, new[] { "Zeile eins" }, "");
            },
            SicherungAnlegen: mitSicherung ? () => "sicherung.sqlite" : null,
            BerichtSchreiben: (_, _) => null)));

    [Fact]
    public async Task Transfer_Zielname_und_Konfliktmodus_stehen_danach_im_Importblatt()
    {
        var cut = Transfer(new Import(), mitSicherung: true);

        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.PROJEKT_TRANSFER, "zielname", "Kopie Nord"))).Status);
        KiErgebnis modus = await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.PROJEKT_TRANSFER, "konflikt", "Vorhandenes Projekt überschreiben"));
        Assert.True(modus.Status == KiStatus.Ausgefuehrt, modus.Text);
        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.PROJEKT_TRANSFER, "sicherung", "false"))).Status);

        cut.FindAll(".epos-reiter-knopf")[1].Click();                 // Blatt „Importieren"
        Assert.Equal("Kopie Nord", cut.FindAll(".epos-projekttransfer input[type=text]").Last().GetAttribute("value"));
        Assert.True(cut.FindAll(".epos-optionsgruppe input[type=radio]")[1].HasAttribute("checked"));
        Assert.False(cut.FindAll(".epos-projekttransfer input[type=checkbox]").Last().HasAttribute("checked"));
    }

    [Fact]
    public async Task Transfer_Sicherung_ohne_Sicherungsweg_wird_vor_der_Bestaetigung_abgelehnt()
    {
        var cut = Transfer(new Import(), mitSicherung: false);

        KiVorbereitung sicherung = await cut.InvokeAsync(() =>
            Vorbereiten(KiMaskennamen.PROJEKT_TRANSFER, "sicherung", "true"));

        Assert.Null(sicherung.Freigabe);
        Assert.Contains(Resource.KI_PTR_KEINE_SICHERUNG, sicherung.Ablehnung.Text);
    }

    // =====================================================================
    //  Brennstoffe des Projekts
    // =====================================================================

    private static Dictionary<string, double?> Werte(double hi, double co2) => new()
    {
        [ProjektBrennstoffFelder.Hi] = hi, [ProjektBrennstoffFelder.Hs] = hi * 1.1,
        [ProjektBrennstoffFelder.Co2] = co2, [ProjektBrennstoffFelder.So2] = 0,
        [ProjektBrennstoffFelder.Nox] = 0, [ProjektBrennstoffFelder.Staub] = null,
        [ProjektBrennstoffFelder.Pe] = 1.1, [ProjektBrennstoffFelder.Grundpreis] = 0,
        [ProjektBrennstoffFelder.Arbeitspreis] = 0, [ProjektBrennstoffFelder.Leistungspreis] = 0
    };

    private static readonly ProjektBrennstoffeStand BRENNSTOFFE = new("Musterprojekt",
        new[] { new ProjektBrennstoffZeile(3, "Erdgas E", "kWh", Werte(10.5, 240), Werte(10.5, 240), true, Array.Empty<string>()) },
        new[] { new ProjektBrennstoffKatalogsatz(25, "Wasserstoff"), new ProjektBrennstoffKatalogsatz(14, "Biogas") });

    [Fact]
    public async Task Brennstoffe_Katalogwahl_ueber_den_Text_geht_in_die_Uebernahme()
    {
        int? uebernommen = null;
        var cut = Render<ProjektBrennstoffeDialog>(p => p
            .Add(x => x.Stand, BRENNSTOFFE)
            .Add(x => x.Uebernehmen, id => { uebernommen = id; return Task.FromResult(new ProjektBrennstoffeAntwort(true, "ok", null)); }));

        KiErgebnis wahl = await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.PROJEKT_BRENNSTOFFE, "katalogwahl", "Biogas"));
        Assert.True(wahl.Status == KiStatus.Ausgefuehrt, wahl.Text);
        // Der Assistent übernimmt nicht selbst — erst der Klick des Anwenders schreibt.
        Assert.Null(uebernommen);

        cut.Find(".epos-pbrs-uebernehmen").Click();
        cut.WaitForAssertion(() => Assert.Equal(14, uebernommen));
    }

    [Fact]
    public async Task Brennstoffe_Werte_nur_in_der_Bearbeitung_und_erst_Speichern_schreibt()
    {
        (int Id, IReadOnlyDictionary<string, double?> Werte)? gespeichert = null;
        var cut = Render<ProjektBrennstoffeDialog>(p => p
            .Add(x => x.Stand, BRENNSTOFFE)
            .Add(x => x.Speichern, (id, w) => { gespeichert = (id, w); return Task.FromResult(new ProjektBrennstoffeAntwort(true, "ok", null)); }));

        KiVorbereitung ohne = await cut.InvokeAsync(() =>
            Vorbereiten(KiMaskennamen.PROJEKT_BRENNSTOFFE, "co2", "230"));
        Assert.Null(ohne.Freigabe);
        Assert.Contains(Resource.KI_PBRS_NICHT_IN_BEARBEITUNG, ohne.Ablehnung.Text);

        cut.Find(".epos-pbrs-bearbeiten").Click();
        KiErgebnis co2 = await cut.InvokeAsync(() => Setzen(KiMaskennamen.PROJEKT_BRENNSTOFFE, "co2", "230"));
        Assert.True(co2.Status == KiStatus.Ausgefuehrt, co2.Text);
        Assert.Null(gespeichert);

        cut.Find(".epos-pbrs-speichern").Click();
        cut.WaitForAssertion(() => Assert.NotNull(gespeichert));
        Assert.Equal(3, gespeichert!.Value.Id);
        Assert.Equal(new[] { ProjektBrennstoffFelder.Co2 }, gespeichert.Value.Werte.Keys);
        Assert.Equal(230.0, gespeichert.Value.Werte[ProjektBrennstoffFelder.Co2]);
    }

    // =====================================================================
    //  Katalog-Dubletten
    // =====================================================================

    [Fact]
    public async Task Dubletten_Katalog_ueber_den_Text_waehlt_den_Schluessel()
    {
        var cut = Render<KatalogDublettenDialog>(p => p
            .Add(x => x.Kataloge, new List<(string, string)> { ("WP", "Wärmepumpen"), ("PV", "Photovoltaik") }));
        Assert.Equal("", cut.Instance.GewaehlterSchluessel);

        KiErgebnis katalog = await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.KATALOG_DUBLETTEN, "katalog", "Photovoltaik"));

        Assert.True(katalog.Status == KiStatus.Ausgefuehrt, katalog.Text);
        Assert.Equal("PV", cut.Instance.GewaehlterSchluessel);
    }

    // =====================================================================
    //  Der Weg von feld_setzen
    // =====================================================================

    private static IReadOnlyDictionary<string, object?> Werte(string maske, string feld, string wert)
        => new Dictionary<string, object?> { ["maske"] = maske, ["feld"] = feld, ["wert"] = wert };

    /// <summary>Nur die Vorbereitung: <c>Freigabe is null</c> heißt Absage VOR der Bestätigung.</summary>
    private static async Task<KiVorbereitung> Vorbereiten(string maske, string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen", Werte(maske, feld, wert));
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());
        return await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);
    }

    /// <summary>Vorbereiten, freigeben, ausführen — der ganze Weg einer Feldsetzung.</summary>
    private static async Task<KiErgebnis> Setzen(string maske, string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen", Werte(maske, feld, wert));
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());

        KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);
        if (vorbereitung.Freigabe is null) return vorbereitung.Ablehnung;

        vorbereitung.Freigabe.Erteilen();
        return await schicht.AusfuehrenAsync(geprueft.Aufruf, vorbereitung.Freigabe, CancellationToken.None);
    }
}

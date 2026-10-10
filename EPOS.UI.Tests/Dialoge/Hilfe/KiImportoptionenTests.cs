using System.Text;
using Bunit;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Die vier Importoptions-Dialoge am Hilfe-Assistenten (Freigabe der Importdialoge, Teil A):
/// je Maske ein Feld über den ganzen Weg von <c>feld_setzen</c> gesetzt — und der Dialog geht
/// danach denselben Weg wie nach einer Eingabe von Hand; Wahlfelder über ihren Text; gesperrte
/// Felder vor der Bestätigung abgelehnt.
/// </summary>
public sealed class KiImportoptionenTests : EposBunitContext, IDisposable
{
    private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

    public KiImportoptionenTests()
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
    //  CSV-Datei der Flotte
    // =====================================================================

    [Fact]
    public async Task FlottenCsv_Trennzeichen_ueber_den_Text_liest_die_Vorschau_neu()
    {
        var cut = Render<SpeicherFlottenCsvDialog>(p => p.Add(x => x.Datei, PrognoseCsv()).Add(x => x.Prognosen, true));
        int spaltenVorher = cut.Find("table.epos-tabelle thead").QuerySelectorAll("th").Length;

        KiErgebnis ergebnis = await cut.InvokeAsync(() => Setzen(KiMaskennamen.SPEICHER_FLOTTEN_CSV,
            "trennzeichen", Resource.FLOTTE_CSV_SEP_KOMMA));

        Assert.True(ergebnis.Status == KiStatus.Ausgefuehrt, ergebnis.Text);
        Assert.Equal("1", Auswahl(cut, Resource.FLOTTE_CSV_TRENNZEICHEN));
        // Der Handweg rechnet die Vorschau neu: Mit dem Komma zerfallen die Dezimalzahlen.
        Assert.NotEqual(spaltenVorher, cut.Find("table.epos-tabelle thead").QuerySelectorAll("th").Length);
    }

    [Fact]
    public async Task FlottenCsv_Wertspalte_und_Einheit_setzen_wie_die_Klappliste()
    {
        var cut = Render<SpeicherFlottenCsvDialog>(p => p.Add(x => x.Datei, PrognoseCsv()).Add(x => x.Prognosen, true));
        string spalte3 = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resource.FLOTTE_CSV_SPALTE, 3);

        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.SPEICHER_FLOTTEN_CSV, "last", spalte3))).Status);
        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.SPEICHER_FLOTTEN_CSV, "leistungseinheit", "MW"))).Status);

        Assert.Equal("2", Auswahl(cut, Resource.FLOTTE_CSV_LAST));
        Assert.Equal("1", Auswahl(cut, Resource.FLOTTE_CSV_LEISTUNGSEINHEIT));
    }

    [Fact]
    public async Task FlottenCsv_Verborgene_Felder_werden_vor_der_Bestaetigung_abgelehnt()
    {
        var cut = Render<SpeicherFlottenCsvDialog>(p => p.Add(x => x.Datei, PrognoseCsv()));

        KiVorbereitung datum = await cut.InvokeAsync(() => Vorbereiten(KiMaskennamen.SPEICHER_FLOTTEN_CSV,
            "datumsspalte", string.Format(System.Globalization.CultureInfo.CurrentCulture, Resource.FLOTTE_CSV_SPALTE, 1)));
        Assert.Null(datum.Freigabe);
        Assert.Contains(Resource.KI_FCSV_NUR_GETRENNT, datum.Ablehnung.Text);

        KiVorbereitung prognose = await cut.InvokeAsync(() => Vorbereiten(KiMaskennamen.SPEICHER_FLOTTEN_CSV,
            "snapshot_id", string.Format(System.Globalization.CultureInfo.CurrentCulture, Resource.FLOTTE_CSV_SPALTE, 1)));
        Assert.Null(prognose.Freigabe);
        Assert.Contains(Resource.KI_FCSV_NUR_PROGNOSEN, prognose.Ablehnung.Text);
    }

    // =====================================================================
    //  Importoptionen einer Ganglinie
    // =====================================================================

    [Fact]
    public async Task Ganglinie_Trennzeichen_und_Kopfzeile_landen_in_den_Optionen()
    {
        var cut = Ganglinie(excel: false);
        string komma = GanglinienOptionenModell.TrennzeichenTexte()[1];

        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.GANGLINIE_IMPORT_OPTIONEN, "trennzeichen", komma))).Status);
        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.GANGLINIE_IMPORT_OPTIONEN, "kopfzeile", "false"))).Status);

        GanglinienImportOptionen o = cut.Instance.Optionen();
        Assert.Equal(',', o.Trennzeichen);
        Assert.False(o.Kopfzeile);
    }

    [Fact]
    public async Task Ganglinie_Tabellenblatt_nur_bei_einer_Excel_Mappe()
    {
        var csv = Ganglinie(excel: false);
        KiVorbereitung abgelehnt = await csv.InvokeAsync(() =>
            Vorbereiten(KiMaskennamen.GANGLINIE_IMPORT_OPTIONEN, "blatt", "Notizen"));
        Assert.Null(abgelehnt.Freigabe);
        Assert.Contains(Resource.KI_GIO_NUR_EXCEL, abgelehnt.Ablehnung.Text);
        csv.Instance.Dispose();

        var excel = Ganglinie(excel: true);
        Assert.Equal(KiStatus.Ausgefuehrt, (await excel.InvokeAsync(() =>
            Setzen(KiMaskennamen.GANGLINIE_IMPORT_OPTIONEN, "blatt", "Notizen"))).Status);
        Assert.Equal("Notizen", excel.Instance.Optionen().Blattname);
    }

    // =====================================================================
    //  Spotpreise einlesen
    // =====================================================================

    [Fact]
    public async Task Spotpreis_Bezeichnung_und_Ablage_gehen_in_die_Uebernahme()
    {
        (string Bezeichnung, bool Stamm)? gespeichert = null;
        var cut = Render<SpotpreisImportDialog>(p => p
            .Add(x => x.Waehlen, f => Task.FromResult<string?>(@"C:\Daten\spot2026.csv"))
            .Add(x => x.Pruefen, pfad => Task.FromResult(new SpotpreisPruefung(true, "in Ordnung", 2026)))
            .Add(x => x.Speichern, (b, s, f) =>
            {
                gespeichert = (b, s);
                return Task.FromResult(new SpotpreisSpeicherung(42, 8760));
            }));

        Assert.Equal(Resource.KI_SPOT_KEINE_DATEI, KiMaskenbruecke.Haken(KiMaskennamen.SPOTPREIS_IMPORT).Pruefen());

        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.SPOTPREIS_IMPORT, "bezeichnung", "Börse 2026"))).Status);
        Assert.Equal(KiStatus.Ausgefuehrt, (await cut.InvokeAsync(() =>
            Setzen(KiMaskennamen.SPOTPREIS_IMPORT, "fuer_alle_projekte", "false"))).Status);

        // Datei wählen und übernehmen bleiben Handlungen des Anwenders.
        cut.Find(".epos-dateiwahl button").Click();
        cut.WaitForAssertion(() => Assert.Equal("", KiMaskenbruecke.Haken(KiMaskennamen.SPOTPREIS_IMPORT).Pruefen()));
        cut.FindAll(".epos-leiste button")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal(("Börse 2026", false), gespeichert));
    }

    // =====================================================================
    //  Konflikte beim Import
    // =====================================================================

    [Fact]
    public async Task Konflikte_Aktion_und_Name_je_Zeile_wie_von_Hand()
    {
        List<KonfliktEntscheidung>? entschieden = null;
        var cut = Konflikte(l => entschieden = l);

        KiErgebnis aktion = await cut.InvokeAsync(() => Setzen(KiMaskennamen.IMPORT_KONFLIKTE, "aktion_2",
            ImportKonfliktModell.AktionText(KonfliktAktion.Umbenennen)));
        Assert.Equal(KiStatus.Ausgefuehrt, aktion.Status);

        KiErgebnis name = await cut.InvokeAsync(() => Setzen(KiMaskennamen.IMPORT_KONFLIKTE, "name_2", "Werk Süd neu"));
        Assert.Equal(KiStatus.Ausgefuehrt, name.Status);

        Assert.Equal("", KiMaskenbruecke.Haken(KiMaskennamen.IMPORT_KONFLIKTE).Pruefen());

        // Bestätigen bleibt beim Anwender.
        cut.FindAll(".epos-leiste button")[^1].Click();
        Assert.NotNull(entschieden);
        Assert.Equal(KonfliktAktion.Umbenennen, entschieden![1].Aktion);
        Assert.Equal("Werk Süd neu", entschieden[1].NeuerName);
    }

    [Fact]
    public async Task Konflikte_Name_ohne_Umbenennen_und_unerlaubte_Aktion_werden_benannt_abgelehnt()
    {
        var cut = Konflikte(_ => { });

        KiErgebnis name = await cut.InvokeAsync(() => Setzen(KiMaskennamen.IMPORT_KONFLIKTE, "name_1", "Anders"));
        Assert.NotEqual(KiStatus.Ausgefuehrt, name.Status);
        Assert.Contains(Resource.KI_IKF_NAME_NUR_UMBENENNEN, name.Text);

        KiErgebnis aktion = await cut.InvokeAsync(() => Setzen(KiMaskennamen.IMPORT_KONFLIKTE, "aktion_1",
            ImportKonfliktModell.AktionText(KonfliktAktion.Ueberschreiben)));
        Assert.NotEqual(KiStatus.Ausgefuehrt, aktion.Status);
        Assert.Equal("Werk Nord", cut.Instance.Entscheidungen()[0].Pruefung.Kandidat!.Name);
        Assert.NotEqual(KonfliktAktion.Ueberschreiben, cut.Instance.Entscheidungen()[0].Aktion);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static SpeicherImportDatei PrognoseCsv() => new()
    {
        Dateiname = "prognose.csv",
        Inhalt = Encoding.UTF8.GetBytes(
            "timestamp;load_kw;pv_kw;bhkw_kw;buy_eur_kwh;pv_sell_eur_kwh;bhkw_sell_eur_kwh;battery_sell_eur_kwh;snapshot_id;known_at;decision_at\n" +
            "2026-01-01 00:00;1;2;0;0,2;0,08;0,05;0,04;p1;2026-01-01 00:00;2026-01-01 00:00\n" +
            "2026-01-01 00:15;1,5;1;0;0,3;0,08;0,05;0,04;p1;2026-01-01 00:00;2026-01-01 00:00\n")
    };

    private static string? Auswahl(IRenderedComponent<SpeicherFlottenCsvDialog> cut, string beschriftung)
    {
        AngleSharp.Dom.IElement select = cut.FindAll("label")
            .Single(x => x.TextContent.Contains(beschriftung.TrimEnd(':'))).QuerySelector("select")!;
        return select.QuerySelector("option[selected]")?.GetAttribute("value");
    }

    private IRenderedComponent<GanglinieImportOptionenDialog> Ganglinie(bool excel)
    {
        var v = new GanglinienVorschau { IstExcel = excel, Spaltenzahl = 2, Lesbar = true };
        v.Vorschlag.Trennzeichen = ';';
        v.Vorschlag.Dezimaltrenner = ',';
        v.Vorschlag.Kopfzeile = true;
        v.Vorschlag.WertSpalte = 1;
        v.Vorschlag.ZeitSpalte = 0;
        v.Zeilen.Add(new[] { "Zeitstempel", "Leistung kW" });
        v.Zeilen.Add(new[] { "01.01.2023 00:00", "220,00" });
        if (excel) { v.Blaetter.Add("Lastgang"); v.Blaetter.Add("Notizen"); }
        return Render<GanglinieImportOptionenDialog>(p => p
            .Add(x => x.Pfad, excel ? @"C:\Daten\lastgang.xlsx" : @"C:\Daten\lastgang.csv")
            .Add(x => x.Erkennung, v));
    }

    private IRenderedComponent<ImportKonflikteDialog> Konflikte(Action<List<KonfliktEntscheidung>?> geschlossen)
    {
        static ImportPruefung Pruefung(ImportBefund befund, string name) => new()
        {
            Kandidat = new ImportKandidat { Name = name },
            Befund = befund,
            AbweichendeSpalten = new List<string> { "Zeitinterval" }
        };
        return Render<ImportKonflikteDialog>(p => p
            .Add(x => x.Pruefungen, new[] { Pruefung(ImportBefund.Neu, "Werk Nord"),
                                             Pruefung(ImportBefund.NameVorhanden, "Werk Süd") })
            .Add(x => x.VergebeneNamen, new HashSet<string>(StringComparer.Ordinal) { "Werk Süd" })
            .Add(x => x.Geschlossen, (List<KonfliktEntscheidung>? l) => geschlossen(l)));
    }

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

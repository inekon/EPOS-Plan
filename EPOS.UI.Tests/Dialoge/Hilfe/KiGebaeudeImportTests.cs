using Bunit;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;
using static EPOS.UI.Tests.Dialoge.Hilfe.KiSetzweg;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Die Kopfeingaben des Gebäudeimports am Hilfe-Assistenten (Freigabe der Masken, Teil B): ein Feld über den ganzen
/// Weg von <c>feld_setzen</c> gesetzt — der Dialog ordnet danach neu zu wie nach der Eingabe von Hand; ein Wahlfeld über
/// seinen Text; ein Feld eines Schritts, der noch nicht vorn steht, vor der Bestätigung abgelehnt; die Prüfung meldet die
/// weiche Sperre von „Übernehmen".
/// </summary>
public sealed class KiGebaeudeImportTests : EposBunitContext, IDisposable
{
    private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

    public KiGebaeudeImportTests()
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

    private static readonly GebaeudeImportProfilDaten Profil =
        new("Format Alpha", "(*.alpha)|*.alpha", "25 MB", new[] { "Regel eins – eine Zone" }, "Form_Alpha.btn_Help");

    private static readonly IReadOnlyList<string> Klassen = Enumerable.Range(0, 13).Select(i => "Klasse " + (char)('A' + i)).ToList();

    private static GebaeudeImportStand Stand(GebaeudeZuordnungsanfrage a) => new()
    {
        Kopftext = "Kopf-Probe",
        Vorschlagsname = "Haus",
        CadSollwertMoeglich = true,
        Zeilen = new[]
        {
            new GebaeudeFeldzeileDaten { Zielfeld = "SOLL", Gruppe = "Sollwerte", Feld = "Heizsollwert Tag",
                                         Wert = a.RaumtemperaturAlsSollwert ? 19.3 : 20, Einheit = "°C",
                                         WertText = a.RaumtemperaturAlsSollwert ? "19,3" : "20",
                                         HerkunftSchluessel = "VORGABE", HerkunftText = "x",
                                         Haken = true, Eingebbar = true, HakenSetzbar = true },
        },
    };

    private IRenderedComponent<GebaeudeImportDialog> Bauen(List<GebaeudeZuordnungsanfrage> anfragen)
        => Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, Profil);
            c.Add(x => x.Baualtersklassen, Klassen);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)((_, _, _) =>
                Task.FromResult(new GebaeudeLesestand(true, new GebaeudeImportKopf("haus.alpha", "Format Alpha", "Schema 1", "20 KB", "Regel eins"),
                                                      new[] { "Haus 1" }, Array.Empty<GebaeudeImportMeldung>()))));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { anfragen.Add(a); return Stand(a); }));
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)(_ => Array.Empty<GebaeudeImportMeldung>()));
            c.Add(x => x.Uebernehmen, (Func<GebaeudeImportErgebnis, Task<string?>>)(_ => Task.FromResult<string?>(null)));
        });

    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        cut.FindAll("button").First(k => k.TextContent.Contains("Datei wählen")).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
    }

    [Fact]
    public async Task Der_Heizsollwert_aus_der_Datei_ordnet_neu_zu_wie_von_Hand()
    {
        var anfragen = new List<GebaeudeZuordnungsanfrage>();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(anfragen);
        Einlesen(cut);
        int vorher = anfragen.Count;

        KiErgebnis e = await cut.InvokeAsync(() => Setzen(KiMaskennamen.GEBAEUDE_IMPORT, "cad_sollwert", "true"));

        Assert.True(e.Status == KiStatus.Ausgefuehrt, e.Text);
        // Der Handweg: eine neue Zuordnungsanfrage mit dem Schalter, der Sollwert folgt.
        Assert.Equal(vorher + 1, anfragen.Count);
        Assert.True(anfragen.Last().RaumtemperaturAlsSollwert);
        Assert.True(cut.Instance.CadSollwertWirksam);
        Assert.Equal(19.3, cut.Instance.Zeilen.Single(z => z.Zielfeld == "SOLL").Wert);
    }

    [Fact]
    public async Task Die_Baualtersklasse_ueber_den_Text_gilt_als_eigene_Wahl_und_ordnet_neu_zu()
    {
        var anfragen = new List<GebaeudeZuordnungsanfrage>();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(anfragen);
        Einlesen(cut);

        KiErgebnis e = await cut.InvokeAsync(() => Setzen(KiMaskennamen.GEBAEUDE_IMPORT, "baualtersklasse", "Klasse D"));

        Assert.True(e.Status == KiStatus.Ausgefuehrt, e.Text);
        Assert.True(cut.Instance.KlasseGewaehlt);
        Assert.Equal(3, anfragen.Last().Baualtersklasse);
        Assert.True(anfragen.Last().KlasseGewaehlt);
    }

    [Fact]
    public async Task Felder_der_Zuordnung_vor_dem_Lesen_werden_vor_der_Bestaetigung_abgelehnt()
    {
        var anfragen = new List<GebaeudeZuordnungsanfrage>();
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(anfragen);

        KiVorbereitung name = await cut.InvokeAsync(() => Vorbereiten(KiMaskennamen.GEBAEUDE_IMPORT, "name", "Altbau"));
        Assert.Null(name.Freigabe);
        Assert.Contains(Resource.KI_GIMP_ERST_LESEN, name.Ablehnung.Text);
        Assert.Empty(anfragen);

        // Vor dem Lesen meldet die Prüfung die weiche Sperre von „Übernehmen".
        Assert.Equal(cut.Instance.Texte.OkOhneDatei, KiMaskenbruecke.Haken(KiMaskennamen.GEBAEUDE_IMPORT).Pruefen());

        // Nach dem Lesen: die Gebäudewahl bleibt gesperrt (ein Gebäude), ebenso die Zonierung ohne Projektdatei.
        Einlesen(cut);
        KiVorbereitung gebaeude = await cut.InvokeAsync(() => Vorbereiten(KiMaskennamen.GEBAEUDE_IMPORT, "gebaeude", "Haus 1"));
        Assert.Null(gebaeude.Freigabe);
        Assert.Contains(Resource.KI_GIMP_EIN_GEBAEUDE, gebaeude.Ablehnung.Text);
        KiVorbereitung zonierung = await cut.InvokeAsync(() => Vorbereiten(KiMaskennamen.GEBAEUDE_IMPORT, "zonierung", "DIN"));
        Assert.Null(zonierung.Freigabe);
        Assert.Equal("", KiMaskenbruecke.Haken(KiMaskennamen.GEBAEUDE_IMPORT).Pruefen());
    }
}

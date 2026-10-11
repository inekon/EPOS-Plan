using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// KT-8b: <b>Was die Fangstelle meldet, sagt auch der Assistent.</b> Wirft in einem Katalogdialog
/// das Lesen der Liste oder des gewählten Satzes (<see cref="Katalogladung"/>), zeigt der Banner den
/// Grund — und die KI-Sicht führt ihn als lesbares Feld <c>ladestand</c>, sperrt die Felder mit
/// demselben Grund (<c>KiMaskenhaken.Sperrgrund</c>, vor der Bestätigung) und lehnt „Speichern“
/// benannt ab, statt still zu scheitern. Die Satzwahl bleibt bei einem gescheiterten Satz frei.
/// </summary>
public class KatalogKiSperreTests : EposBunitContext
{
    private const string GRUND = "Datei gesperrt";
    private const string NAME = "Satz A";

    public KatalogKiSperreTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<Katalogfilterzeile> Eine() => new[] { new Katalogfilterzeile(1, NAME) };

    private static T Wirft<T>() => throw new InvalidOperationException(GRUND);

    private static string Ladestand(string maske)
        => KiMaskenbruecke.Feldzugang(maske, "ladestand")!.Lesen() as string ?? "";

    private static string Sperre(string maske, string feld) => KiMaskenbruecke.Haken(maske).Feldsperre(feld);

    private static KiErgebnis Speichern(string maske) => KiMaskenbruecke.Haken(maske).Speichern().GetAwaiter().GetResult();

    /// <summary>Die Liste ist nicht geladen: Ladestand, jedes Feld und Speichern nennen den Grund.</summary>
    private static void ListeGesperrt(string maske, string feld, string satzwahl)
    {
        Assert.Contains(GRUND, Ladestand(maske));
        string vorspann = R.KI_DLG_LISTE_NICHT_GELADEN.Split('{')[0];
        Assert.StartsWith(vorspann, Sperre(maske, feld));
        Assert.Contains(GRUND, Sperre(maske, feld));
        Assert.Contains(GRUND, Sperre(maske, satzwahl));
        KiErgebnis e = Speichern(maske);
        Assert.Equal(KiStatus.Abgelehnt, e.Status);
        Assert.Contains(GRUND, e.Text);
    }

    /// <summary>Der Satz ist nicht geladen: die Felder gesperrt, die Satzwahl frei.</summary>
    private static void SatzGesperrt(string maske, string feld, string satzwahl)
    {
        Assert.Contains(GRUND, Ladestand(maske));
        Assert.StartsWith(R.KI_DLG_SATZ_NICHT_GELADEN.Split('{')[0], Sperre(maske, feld));
        Assert.Contains(GRUND, Sperre(maske, feld));
        Assert.Equal("", Sperre(maske, satzwahl));
    }

    private static void Frei(string maske, string feld)
    {
        Assert.Equal("", Ladestand(maske));
        Assert.Equal("", Sperre(maske, feld));
    }

    // =================================================================================
    // Der Baustein
    // =================================================================================

    [Fact]
    public void Der_Baustein_sperrt_bei_der_Liste_alles_und_beim_Satz_alles_ausser_der_Satzwahl()
    {
        Assert.Equal("a b", KatalogKiSperre.Ladestand("a", "", null, " b "));
        Assert.Equal("", KatalogKiSperre.Feldsperre("x", "", "", "satz"));
        Assert.Null(KatalogKiSperre.Aktionssperre("", null));

        Assert.Contains("L", KatalogKiSperre.Feldsperre("satz", "L", "S", "satz"));
        Assert.Equal("", KatalogKiSperre.Feldsperre("satz", "", "S", "satz"));
        Assert.Contains("S", KatalogKiSperre.Feldsperre("typ", "", "S", "satz"));
        Assert.Contains("S", KatalogKiSperre.Aktionssperre("", "S"));
    }

    // =================================================================================
    // Bedarfsverwaltung
    // =================================================================================

    private IRenderedComponent<BedarfAdminDialog> BedarfAdmin(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                              Func<string, string>? jahressumme = null)
        => Render<BedarfAdminDialog>(b => b
            .Add(x => x.Art, BedarfsArt.Stromverbraucher)
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Stromverbraucher, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Jahressumme, jahressumme ?? (_ => "1"))
            .Add(x => x.Kopf, _ => ("B", "T"))
            .Add(x => x.Monatswerte, _ => new double[12])
            .Add(x => x.Speichern, (_, _, _, _) => new KatalogSpeicherErgebnis(true, "", NAME)));

    [Fact]
    public void BedarfAdmin_Liste_wirft_die_KI_Sicht_nennt_den_Grund_und_sperrt()
    {
        BedarfAdmin(Wirft<IReadOnlyList<Katalogfilterzeile>>);
        ListeGesperrt(KiMaskennamen.STROMVERBRAUCHER_ADMIN, "januar", "satz");
    }

    [Fact]
    public void BedarfAdmin_Satz_wirft_die_KI_Sicht_sperrt_die_Felder_und_Speichern()
    {
        BedarfAdmin(Eine, _ => Wirft<string>());
        SatzGesperrt(KiMaskennamen.STROMVERBRAUCHER_ADMIN, "januar", "satz");
        KiErgebnis e = Speichern(KiMaskennamen.STROMVERBRAUCHER_ADMIN);
        Assert.Equal(KiStatus.Abgelehnt, e.Status);
        Assert.Contains(GRUND, e.Text);
    }

    [Fact]
    public void BedarfAdmin_ohne_Ausnahme_ist_der_Ladestand_leer()
    {
        BedarfAdmin(Eine);
        Frei(KiMaskennamen.STROMVERBRAUCHER_ADMIN, "januar");
    }

    // =================================================================================
    // Gebäudetypen, Baustoffe, Bauteilaufbauten
    // =================================================================================

    private IRenderedComponent<GebaeudetypDialog> Gebaeudetyp(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                              Func<string, GebaeudetypDaten?>? lies = null)
        => Render<GebaeudetypDialog>(b => b
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeudetyp(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, lies ?? (_ => null)));

    [Fact]
    public void Gebaeudetyp_Liste_wirft_die_KI_Sicht_nennt_den_Grund_und_sperrt()
    {
        Gebaeudetyp(Wirft<IReadOnlyList<Katalogfilterzeile>>);
        ListeGesperrt(KiMaskennamen.GEBAEUDETYP, "stundenwerte", "typ");
    }

    [Fact]
    public void Gebaeudetyp_Satz_wirft_die_Typwahl_bleibt_frei()
    {
        Gebaeudetyp(Eine, _ => Wirft<GebaeudetypDaten?>());
        SatzGesperrt(KiMaskennamen.GEBAEUDETYP, "stundenwerte", "typ");
    }

    private IRenderedComponent<BaustoffKatalogDialog> Baustoff(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                               Func<int, BaustoffDaten?>? lies = null)
        => Render<BaustoffKatalogDialog>(b => b
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBaustoff(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, lies ?? (_ => null)));

    [Fact]
    public void Baustoff_Liste_wirft_die_KI_Sicht_nennt_den_Grund_und_sperrt()
    {
        Baustoff(Wirft<IReadOnlyList<Katalogfilterzeile>>);
        ListeGesperrt(KiMaskennamen.BAUSTOFF_KATALOG, "bezeichner", "baustoff");
    }

    [Fact]
    public void Baustoff_Satz_wirft_die_Baustoffwahl_bleibt_frei()
    {
        Baustoff(Eine, _ => Wirft<BaustoffDaten?>());
        SatzGesperrt(KiMaskennamen.BAUSTOFF_KATALOG, "bezeichner", "baustoff");
    }

    private IRenderedComponent<BauteilaufbauDialog> Bauteilaufbau(Func<IReadOnlyList<Katalogfilterzeile>> zeilen,
                                                                  Func<int, BauteilaufbauDaten?>? lies = null)
        => Render<BauteilaufbauDialog>(b => b
            .Add(x => x.Katalogzeilen, zeilen)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBauteilaufbau(s => R.ResourceManager.GetString(s) ?? s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Lies, lies ?? (_ => null)));

    [Fact]
    public void Bauteilaufbau_Liste_wirft_die_KI_Sicht_nennt_den_Grund_und_sperrt()
    {
        Bauteilaufbau(Wirft<IReadOnlyList<Katalogfilterzeile>>);
        ListeGesperrt(KiMaskennamen.BAUTEILAUFBAU, "bezeichner", "aufbau");
    }

    [Fact]
    public void Bauteilaufbau_Satz_wirft_die_Aufbauwahl_bleibt_frei()
    {
        Bauteilaufbau(Eine, _ => Wirft<BauteilaufbauDaten?>());
        SatzGesperrt(KiMaskennamen.BAUTEILAUFBAU, "bezeichner", "aufbau");
    }

    [Fact]
    public void Bauteilaufbau_ohne_Ausnahme_ist_der_Ladestand_leer()
    {
        Bauteilaufbau(Eine, _ => null);
        Frei(KiMaskennamen.BAUTEILAUFBAU, "bezeichner");
    }

    // =================================================================================
    // Gebäudekatalog: die Klapplisten der Typen und Arten
    // =================================================================================

    [Fact]
    public void GebaeudeKatalog_Typliste_wirft_sperrt_Typ_Art_und_Speichern_nicht_den_Rest()
    {
        Render<GebaeudeKatalogDialog>(b => b
            .Add(x => x.Gebaeudetypen, () => throw new InvalidOperationException(GRUND))
            .Add(x => x.Gebaeudearten, () => new[] { "Wohnen" })
            .Add(x => x.Speichern, (_, _, _) => new GebaeudeKatalogErgebnis(true, "")));

        string maske = KiMaskennamen.GEBAEUDE_KATALOG;
        Assert.Contains(GRUND, Ladestand(maske));
        Assert.Contains(GRUND, Sperre(maske, "gebaeudetyp"));
        Assert.Contains(GRUND, Sperre(maske, "gebaeudeart"));
        Assert.Equal("", Sperre(maske, "beschreibung"));
        KiErgebnis e = Speichern(maske);
        Assert.Equal(KiStatus.Abgelehnt, e.Status);
        Assert.Contains(GRUND, e.Text);
    }

    [Fact]
    public void GebaeudeKatalog_ohne_Ausnahme_ist_der_Ladestand_leer()
    {
        Render<GebaeudeKatalogDialog>(b => b
            .Add(x => x.Gebaeudetypen, () => new[] { "EFH" })
            .Add(x => x.Gebaeudearten, () => new[] { "Wohnen" }));
        Frei(KiMaskennamen.GEBAEUDE_KATALOG, "gebaeudetyp");
    }

    // =================================================================================
    // Wärmepumpen-Anlagenfelder: Ladestand ohne Speicherweg
    // =================================================================================

    [Fact]
    public void WaermepumpeAnlage_Vorlaufstufen_wirft_der_Ladestand_nennt_den_Grund()
    {
        Render<WaermepumpeAnlageFelder>(p => p
            .Add(x => x.Daten, new WaermepumpeAnlageDaten { IdWp = 1, Bezeichner = "WP" })
            .Add(x => x.Vorlaeufe, _ => throw new InvalidOperationException(GRUND)));
        Assert.Contains(GRUND, Ladestand(KiMaskennamen.WAERMEPUMPE_ANLAGE));
    }

    [Fact]
    public void WaermepumpeAnlage_ohne_Ausnahme_ist_der_Ladestand_leer()
    {
        Render<WaermepumpeAnlageFelder>(p => p
            .Add(x => x.Daten, new WaermepumpeAnlageDaten { IdWp = 1, Bezeichner = "WP" })
            .Add(x => x.Vorlaeufe, _ => Array.Empty<int>()));
        Assert.Equal("", Ladestand(KiMaskennamen.WAERMEPUMPE_ANLAGE));
    }
}

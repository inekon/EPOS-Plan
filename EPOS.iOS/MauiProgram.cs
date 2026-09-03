using System.Globalization;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Der Aufbau der Anwendung - das iOS-Gegenstueck zu
/// <c>WindowsFormsApplication1.Program.Main</c>.
///
/// <para><b>Die Reihenfolge ist dieselbe wie dort</b> (Program.cs:93-122, 148)
/// und aus demselben Grund: Die neun Umgebungsdienste des Kerns werden VOR
/// jedem Programmtext belegt, der eine Meldung absetzen oder auf die Datenbank
/// zugreifen koennte. Stuende die Belegung darunter, ginge die erste Meldung
/// eines Startfehlers ins Leere statt in einen Dialog.</para>
///
/// <para><b>Meldung.* wird NICHT belegt.</b> Die vier Melde-Haken zeigen seit
/// iU5 selbst auf <c>Dienste.Dialog</c> (siehe <c>Meldung.cs</c>); eine
/// Belegung hier waere eine zweite Wahrheit. Genau so haelt es
/// <c>Program.Main</c>.</para>
///
/// <para><b>Zwei Verzeichnisse, nicht eins.</b> Die Umgebungsdienste des Kerns
/// liegen im statischen Halter <c>Dienste</c> (iU5); das DI-Verzeichnis von
/// MAUI traegt nur, was die <c>BlazorWebView</c> und die Komponenten von
/// EPOS.UI brauchen. Genau diese Aufteilung beschreibt
/// <c>WindowsFormsApplication1/Allgemein/Blazor/BlazorDienste.cs</c> - ein
/// zweiter, konkurrierender Weg waere die Stelle, an der zwei Fassungen
/// desselben Dienstes nebeneinander leben.</para>
///
/// <para><b>Das Startprotokoll ist ein Nachweis.</b> Die Zeilen
/// <c>SQLite &lt;Fassung&gt;</c>, <c>STRICT=&lt;n&gt;</c> und
/// <c>EPOS.iOS bereit: Projekte=&lt;n&gt;</c> werden vom CI-Job aus dem
/// Simulator-Protokoll gelesen. Sie belegen, dass die statisch gelinkte
/// SQLite-Fassung stimmt, dass die Datenbank ihre STRICT-Tabellen mitbringt und
/// dass die Anwendung sie lesen kann.</para>
/// </summary>
public static class MauiProgram
{
    /// <summary>Baut die Anwendung auf; gerufen von <see cref="AppDelegate"/>.</summary>
    public static MauiApp CreateMauiApp()
    {
        // 1. DIE DIENSTE VOR ALLEM ANDEREN (Umsetzungskonzept iU5).
        var sprache = DiensteBelegen();

        // 2. Die Sprache aus der Einstellung (bzw. vom Geraet) - erst JETZT,
        //    weil sie ueber Dienste.Einstellungen liest.
        sprache.AusEinstellungUebernehmen();
        Protokoll("Sprache: " + Dienste.Sprache.Kuerzel);
        Protokoll("Ablage: " + Dienste.Pfade.Gemeinsam);
        Protokoll("Dokumente: " + Dienste.Pfade.Dokumente);

        // 3. Die Datenbank, bevor irgendetwas sie liest.
        DatenbankBereitstellen();

        // 4. Die Anwendung selbst.
        var bauer = MauiApp.CreateBuilder();
        bauer.UseMauiApp<App>();

        // Alles, was eine BlazorWebView selbst braucht (WebViewManager,
        // JS-Laufzeit, Dateianbieter) - das Gegenstueck zu
        // AddWindowsFormsBlazorWebView in der Windows-Huelle.
        bauer.Services.AddMauiBlazorWebView();

        // Die beiden Aussenschnittstellen von EPOS.UI.
        bauer.Services.AddSingleton<IHilfeDienst>(new IosHilfeDienst(PaketZuordnungen, AdresseOeffnen));
        bauer.Services.AddSingleton<IProjektQuelle, KeineProjekte>();

        MauiApp anwendung = bauer.Build();

        Protokoll("EPOS.iOS bereit: Projekte=" +
                  Projektzahl().ToString(CultureInfo.InvariantCulture));
        return anwendung;
    }

    // =====================================================================
    // Die neun Umgebungsdienste
    // =====================================================================

    /// <summary>
    /// Legt die iOS-Fassungen der neun Umgebungsdienste ein - in derselben
    /// Reihenfolge wie <c>Program.Main</c> unter Windows. Liefert die
    /// Sprachfassung zurueck, weil ihr Startweg
    /// (<see cref="IosSprache.AusEinstellungUebernehmen"/>) erst laufen darf,
    /// wenn <c>Dienste.Einstellungen</c> steht.
    /// </summary>
    private static IosSprache DiensteBelegen()
    {
        var sprache = new IosSprache();

        Dienste.Dialog = new IosDialogDienst();
        Dienste.Datei = new IosDateiDienst();
        Dienste.Pfade = new IosPfade();
        Dienste.Einstellungen = new IosEinstellungen();
        Dienste.Lizenzablage = new IosLizenzAblage();
        Dienste.GeraeteId = new IosGeraeteId();
        Dienste.Sprache = sprache;
        Dienste.Navigation = new IosNavigation();
        Dienste.Projekt = new IosProjektKontext();

        return sprache;
    }

    // =====================================================================
    // Datenbank
    // =====================================================================

    /// <summary>
    /// Legt beim Erststart die Arbeitskopie der Datenbank an, biegt die
    /// Zugriffsschicht darauf um und schreibt die beiden Gate-Zeilen.
    /// </summary>
    private static void DatenbankBereitstellen()
    {
        Datenbankbereitstellung.Sicherstellen(PaketDatenbank, Protokoll);

        (string fassung, int strict) = Datenbankbereitstellung.Auskunft();
        Protokoll("SQLite " + fassung);
        Protokoll("STRICT=" + strict.ToString(CultureInfo.InvariantCulture));
    }

    // =====================================================================
    // Zugriff auf das Anwendungspaket
    // =====================================================================

    /// <summary>
    /// Oeffnet die mitgelieferte Datenbank im Anwendungspaket (MauiAsset
    /// <c>Kenndaten.sqlite</c>).
    ///
    /// <para><b>Warum synchron.</b> <c>OpenAppPackageFileAsync</c> ist die
    /// einzige API, die an den Paketbestand kommt, und sie ist asynchron. Der
    /// Start ist an dieser Stelle einfaedig - es gibt weder Oberflaeche noch
    /// Fensterschleife -, ein <c>GetAwaiter().GetResult()</c> kann hier also
    /// nicht verklemmen. Danach steht die Datei im Dateisystem und alles
    /// Weitere laeuft ueber gewoehnliche Datenstroeme.</para>
    /// </summary>
    private static Stream? PaketDatenbank() => Paketdatei(Datenbankbereitstellung.DATEI);

    /// <summary>Oeffnet <c>help_mapping.txt</c> im Anwendungspaket.</summary>
    private static Stream? PaketZuordnungen() => Paketdatei(IosHilfeDienst.ZUORDNUNGSDATEI);

    private static Stream? Paketdatei(string name)
    {
        try { return FileSystem.OpenAppPackageFileAsync(name).GetAwaiter().GetResult(); }
        catch { return null; }
    }

    /// <summary>
    /// Oeffnet eine Adresse im Browser - der iOS-Weg fuer den Infoknopf. Unter
    /// Windows zeigt der Hilfedienst statt dessen ein angeheftetes Fenster; das
    /// gibt es hier nicht.
    /// </summary>
    private static void AdresseOeffnen(string adresse)
    {
        if (string.IsNullOrWhiteSpace(adresse)) return;
        try { Launcher.Default.OpenAsync(new Uri(adresse)); } catch { }
    }

    // =====================================================================
    // Kleinigkeiten
    // =====================================================================

    /// <summary>Zahl der Projekte in der Datenbank; <c>-1</c>, wenn sie nicht lesbar ist.</summary>
    private static int Projektzahl()
    {
        try
        {
            object wert = DataRepository.ExecuteScalar("SELECT count(*) FROM Tab_Projekt", null);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt32(wert, CultureInfo.InvariantCulture);
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Eine Zeile ins Startprotokoll. Auf dem Geraet landet sie im Systemlog,
    /// im Simulator liest sie <c>xcrun simctl launch --console-pty</c> mit.
    /// </summary>
    private static void Protokoll(string zeile)
    {
        try { Console.WriteLine(zeile); } catch { }
    }
}

using System.Globalization;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Der Aufbau der Anwendung - das iOS-Gegenstueck zu
/// <c>WindowsFormsApplication1.Program.Main</c>.
///
/// <para><b>Die Reihenfolge ist dieselbe wie dort</b> (Program.cs:93-122) und aus
/// demselben Grund: Die neun Umgebungsdienste des Kerns werden VOR jedem
/// Programmtext belegt, der eine Meldung absetzen oder auf die Datenbank
/// zugreifen koennte. Stuende die Belegung darunter, ginge die erste Meldung
/// eines Startfehlers ins Leere statt in einen Dialog.</para>
///
/// <para><b>Zwei Verzeichnisse, nicht eins.</b> Die Umgebungsdienste des Kerns
/// liegen weiterhin im statischen Halter <c>Dienste</c> (iU5); das
/// DI-Verzeichnis von MAUI traegt nur, was die <c>BlazorWebView</c> und die
/// Komponenten von EPOS.UI brauchen. Genau diese Aufteilung beschreibt
/// <c>WindowsFormsApplication1/Allgemein/Blazor/BlazorDienste.cs</c> - ein
/// zweiter, konkurrierender Weg waere die Stelle, an der zwei Fassungen
/// desselben Dienstes nebeneinander leben.</para>
///
/// <para><b>Das Startprotokoll ist ein Nachweis.</b> Die drei Zeilen
/// <c>SQLite &lt;Fassung&gt;</c>, <c>STRICT=&lt;n&gt;</c> und
/// <c>EPOS.iOS bereit: Projekte=&lt;n&gt;</c> werden vom CI-Job aus dem
/// Simulator-Protokoll gelesen. Sie belegen, dass die statisch gelinkte
/// SQLite-Fassung stimmt, dass die Datenbank die STRICT-Tabellen mitbringt und
/// dass die Anwendung sie lesen kann.</para>
/// </summary>
public static class MauiProgram
{
    /// <summary>Baut die Anwendung auf; gerufen von <see cref="AppDelegate"/>.</summary>
    public static MauiApp CreateMauiApp()
    {
        // 1. Die Umgebungsdienste des Kerns - die iOS-Fassungen kommen mit iU10-5.

        // 2. Die Datenbank, bevor irgendetwas sie liest.
        DatenbankBereitstellen();

        // 3. Die Anwendung selbst.
        var bauer = MauiApp.CreateBuilder();
        bauer.UseMauiApp<App>();

        // Alles, was eine BlazorWebView selbst braucht (WebViewManager,
        // JS-Laufzeit, Dateianbieter) - das Gegenstueck zu
        // AddWindowsFormsBlazorWebView in der Windows-Huelle.
        bauer.Services.AddMauiBlazorWebView();

        // Die beiden Aussenschnittstellen von EPOS.UI. Ihre iOS-Fassungen
        // kommen mit iU10-5 (Hilfe) und iU10-7 (Projektquelle); bis dahin
        // gelten die Vorbelegungen der Bibliothek - sie zeigen eine leere
        // Liste statt abzustuerzen.
        bauer.Services.AddSingleton<IHilfeDienst, KeineHilfe>();
        bauer.Services.AddSingleton<IProjektQuelle, KeineProjekte>();

        MauiApp anwendung = bauer.Build();

        Protokoll("EPOS.iOS bereit: Projekte=" +
                  Projektzahl().ToString(CultureInfo.InvariantCulture));
        return anwendung;
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
    private static Stream? PaketDatenbank()
    {
        try
        {
            return FileSystem.OpenAppPackageFileAsync(Datenbankbereitstellung.DATEI)
                             .GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

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

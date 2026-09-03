using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;

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
/// </summary>
public static class MauiProgram
{
    /// <summary>Baut die Anwendung auf; gerufen von <see cref="AppDelegate"/>.</summary>
    public static MauiApp CreateMauiApp()
    {
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

        return bauer.Build();
    }
}

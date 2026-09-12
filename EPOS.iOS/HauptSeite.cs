using Microsoft.AspNetCore.Components.WebView.Maui;

namespace EPOS.iOS;

/// <summary>
/// Die einzige Seite der Anwendung: eine <see cref="BlazorWebView"/> ueber die
/// ganze Flaeche.
///
/// <para><b>Das Gegenstueck zu <c>BlazorDialogForm&lt;T&gt;</c>.</b> Unter Windows
/// oeffnet jede Razor-Komponente ein eigenes modales Fenster. Auf dem iPad gibt
/// es kein zweites Fenster; deshalb haengt hier genau EINE Komponente -
/// <c>EPOS.UI.Seiten.AppWurzel</c> -, und die entscheidet selbst, ob sie die
/// Projektliste oder einen Dialog zeigt.</para>
///
/// <para>Die Hintergrundfarbe ist dieselbe Themaflaeche wie in der
/// Windows-Huelle (<c>KartenStil.FLAECHE</c> = #F5F4EF, in EPOS.UI als
/// <c>--epos-flaeche</c>). Sie steht hier, damit beim Zeichnen der WebView
/// nicht kurz Weiss aufblitzt.</para>
/// </summary>
public sealed class HauptSeite : ContentPage
{
    /// <summary>Baut die Seite mit der Wurzelkomponente von EPOS.UI.</summary>
    public HauptSeite()
    {
        BackgroundColor = Color.FromArgb("#F5F4EF");

        var web = new BlazorWebView
        {
            HostPage = "wwwroot/index.html"
        };
        // FEHLERSCHRANKE (Befund W13-B-1, 05.09.2026): Gemountet wird nicht
        // AppWurzel selbst, sondern Wurzel<AppWurzel> - eine ErrorBoundary mit
        // AppWurzel darin. Dieselbe Regel wie in den zwei Windows-Huellen
        // (BlazorDialogForm, BlazorSeite): jede Wurzel steht in der
        // Fehlerschranke. Ein Parametersatz kommt hier nicht vor; Wurzel.Gaben
        // bleibt leer und reicht nichts durch.
        web.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(EPOS.UI.Bausteine.Wurzel<EPOS.UI.Seiten.AppWurzel>)
        });

        Content = web;
    }
}

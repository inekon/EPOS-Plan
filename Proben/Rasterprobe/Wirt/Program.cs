// Der Wirt der Rasterprobe (#235) — Blazor Server, eine Seite, kein Zustand.
//
// Blazor SERVER und nicht WebAssembly: Der Rechenweg der Komponenten soll
// derselbe .NET-Code sein, den auch die Anwendung faehrt, und der Browser soll
// GENAU das Markup, dieselben Stilblaetter und dieselben JavaScript-Beobachter
// bekommen wie unter WebView2. Was die WebView darueber hinaus mitbringt
// (interop ueber den Prozess statt ueber SignalR), aendert am Layout nichts.

using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Rasterprobe.Wirt.Seiten;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// AUSDRUECKLICH und nicht ueber ASPNETCORE_ENVIRONMENT=Development: Die
// statischen Dateien der Bibliothek (_content/EPOS.UI/epos-ui.css) kommen aus
// dem Manifest daneben, und ohne diesen Aufruf liefert der Wirt sie nur in der
// Entwicklungsumgebung aus. Ein Lauf ohne Stilblatt misst NICHTS - er zeigt
// eine Tabelle ohne Rahmen, ohne feste Hoehe und ohne Virtualisierung, und das
// sieht in der Zahlenspalte aus wie ein Erfolg. Die Probe prueft es zusaetzlich
// selbst (siehe STILPROBE in rasterprobe.mjs).
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

WebApplication app = builder.Build();

// MapStaticAssets und nicht UseStaticFiles: Nur so kommen die Stilblaetter der
// Bibliothek (_content/EPOS.UI/...) und das gebuendelte Komponenten-CSS des
// Wirtes (Rasterprobe.Wirt.styles.css) heraus - und DARIN steckt QuickGrids
// eigenes Stilblatt, ohne das die Probe weder Platzhalter noch Blinken saehe.
app.MapStaticAssets();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

// Der Wirt der Rasterprobe (#235) — Blazor Server, eine Seite, kein Zustand.
//
// Blazor SERVER und nicht WebAssembly: Der Rechenweg der Komponenten soll
// derselbe .NET-Code sein, den auch die Anwendung faehrt, und der Browser soll
// GENAU das Markup, dieselben Stilblaetter und dieselben JavaScript-Beobachter
// bekommen wie unter WebView2. Was die WebView darueber hinaus mitbringt
// (interop ueber den Prozess statt ueber SignalR), aendert am Layout nichts.

using System.Globalization;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Localization;
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

// Der Hilfedienst des Hauses. Die KATALOGPROBE stellt ganze Dialoge, und jeder
// von ihnen traegt im Kopf einen InfoKnopf - der spricht IHilfeDienst an. Ohne
// Eintrag im Verzeichnis bricht die Seite beim Aufbau ab; die Probe misst
// Layout, nicht den Hilfekatalog, deshalb die leere Ausfuehrung.
builder.Services.AddSingleton<EPOS.UI.Dienste.IHilfeDienst>(new EPOS.UI.Dienste.KeineHilfe());
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Die MARKENPROBE (Seite /vorlagenfeldprobe, BV-E6): der Zustand der
// Platzhalteranzeige wie in den Schalen als Singleton, der echte Katalog als
// Quelle des Halters. Keine Zwischenablage - die Marke zeigt den Text dann
// markiert, und genau diesen Weg misst die Probe.
builder.Services.AddSingleton(new EPOS.UI.Dienste.Vorlagenfeldansicht());
WindowsFormsApplication1.VorlagenfeldanzeigeHuelle.Einhaengen();

// ---------------------------------------------------------------------------
// DIE GEBAEUDEIMPORT-SICHTPROBE (Seite /gebaeudeimport) stellt den echten
// Zuordnungsdialog mit der echten GebaeudeImportHuelle. Drei Dinge belegt sonst
// eine Schale:
//
// 1. KEINE DATENBANK. Die Huelle fragt ohne Projekt keine Datenbank (Wache
//    GebaeudeImportHuelleTests.Ohne_Projekt_fragt_die_Huelle_keine_Datenbank).
//    Als Netz darunter zeigt die Zugriffsschicht fuer den GANZEN Wirt auf einen
//    Ordner, den es nicht gibt: Ein Zugriff, der doch geschieht, scheitert laut
//    (Meldung auf der Konsole), statt die Datenbank des Anwenders unter
//    %ProgramData%\EPOS_PLAN zu oeffnen. Keine Seite des Wirtes braucht eine.
WindowsFormsApplication1.DataRepository.PfadUeberschreibung = Path.Combine(
    Path.GetTempPath(), "Rasterprobe-ohne-Datenbank-" + Guid.NewGuid().ToString("N"), "Kenndaten.sqlite");

// 2. DIE DATEIWAHL. Dienste.Datei antwortet mit der Probe, die die Seite fuer
//    genau ihren Aufruf vorgemerkt hat (AsyncLocal, je Schaltung getrennt); sonst
//    leer wie die Vorgabe KeineDateiwahl.
WindowsFormsApplication1.Dienste.Datei = new Probenwaehler();

// 3. DIE KULTUR AUS DER ADRESSE (?kultur=de-DE|en-US). Die Seite zeichnet vor
//    (Adresse), die Schaltung entsteht spaeter ueber /_blazor - ohne die Adresse
//    der Seite. Deshalb setzt der Seitenabruf mit kultur= das Kulturkeks, und nur
//    /_blazor liest es; ein Seitenabruf ohne kultur= loescht es. Ohne Angabe gilt
//    die Kultur des Prozesses - die uebrigen Seiten laufen wie bisher.
CultureInfo prozessKultur = CultureInfo.CurrentCulture, prozessSprache = CultureInfo.CurrentUICulture;
CultureInfo[] kulturen = new[] { prozessKultur, prozessSprache, new CultureInfo("de-DE"), new CultureInfo("en-US") }
    .DistinctBy(k => k.Name).ToArray();
var lokalisierung = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(prozessKultur, prozessSprache),
    SupportedCultures = kulturen,
    SupportedUICultures = kulturen,
};
lokalisierung.RequestCultureProviders.Clear();
lokalisierung.RequestCultureProviders.Add(
    new QueryStringRequestCultureProvider { QueryStringKey = "kultur", UIQueryStringKey = "kultur" });
lokalisierung.RequestCultureProviders.Add(new CustomRequestCultureProvider(ctx => Task.FromResult(
    ctx.Request.Path.StartsWithSegments("/_blazor")
        ? CookieRequestCultureProvider.ParseCookieValue(ctx.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName] ?? "")
        : null)));

WebApplication app = builder.Build();

app.UseRequestLocalization(lokalisierung);
app.Use(async (ctx, next) =>
{
    // Nur der Seitenabruf des Browsers (GET mit text/html), nie /_blazor und keine Datei.
    if (HttpMethods.IsGet(ctx.Request.Method) && !ctx.Request.Path.StartsWithSegments("/_blazor")
        && ctx.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        string kultur = ctx.Request.Query["kultur"].ToString();
        CultureInfo? gewaehlt = kulturen.FirstOrDefault(k => k.Name.Length > 0
            && string.Equals(k.Name, kultur, StringComparison.OrdinalIgnoreCase));
        if (gewaehlt is not null)
            ctx.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(gewaehlt)),
                new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax, IsEssential = true });
        else if (ctx.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName))
            ctx.Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName, new CookieOptions { Path = "/" });
    }
    await next();
});

// MapStaticAssets und nicht UseStaticFiles: Nur so kommen die Stilblaetter der
// Bibliothek (_content/EPOS.UI/...) und das gebuendelte Komponenten-CSS des
// Wirtes (Rasterprobe.Wirt.styles.css) heraus - und DARIN steckt QuickGrids
// eigenes Stilblatt, ohne das die Probe weder Platzhalter noch Blinken saehe.
app.MapStaticAssets();

// Die SVG-PROBE (Konzept DG-1): dieselben Bilder wie die Seite /svgprobe, aber
// als nackte Antwort. svgprobe.mjs holt sie mit fetch() aus der Seite heraus und
// misst das Einsetzen ins Dokument NETZFREI - so bleibt die Renderzeit des
// Browsers von der SignalR-Uebertragung des Blazor-Zeichenlaufs getrennt.
app.MapGet("/svgprobe/svg", (string? variante, int? reihen) =>
{
    double[][] r = SvgProbe.SvgZeichner.Jahresreihen(Math.Clamp(reihen ?? 3, 1, 6));
    return Results.Text(SvgProbe.SvgZeichner.Svg(r, SvgProbe.SvgZeichner.Art(variante)), "image/svg+xml");
});
app.MapGet("/svgprobe/png", (int? reihen) =>
{
    double[][] r = SvgProbe.SvgZeichner.Jahresreihen(Math.Clamp(reihen ?? 3, 1, 6));
    return Results.Bytes(Bilder.Png(r), "image/png");
});

app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Fehlerschranke — das Sicherheitsnetz um eine Wurzelkomponente
/// (Befund <b>W13‑B‑1</b>, Windows-Abnahme 05.09.2026).
///
/// <para><b>Was hier bewiesen wird.</b> Eine Ausnahme aus einer Kindkomponente
/// beendet den Prozess NICHT mehr, sondern wird ein lesbarer Kasten. Das ist die
/// Lücke, die <c>WebViewWache</c> im eigenen Klassenkopf als ihre Grenze nennt:
/// „Was danach kommt — eine Ausnahme beim Zeichnen der Komponente — sieht diese
/// Wache nicht."</para>
///
/// <para><b>Gegenprobe inbegriffen:</b> Ein Fall rendert dieselbe werfende
/// Komponente OHNE Schranke und zeigt, dass bunit die Ausnahme dann
/// durchreicht — genau das tut am Gerät der Blazor-Verteiler, nur endet es dort
/// im Prozessabbruch.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8): Die Erwartungen
/// sind deutsche Beschriftungen, und der Windows-Läufer läuft englisch.</para>
/// </summary>
public class FehlerschrankeTests : BunitContext
{
    public FehlerschrankeTests()
    {
        DeutscheOberflaeche();
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =====================================================================
    //  Prüfstand: Komponenten, die auf Zuruf werfen
    // =====================================================================

    /// <summary>Der Wortlaut, an dem jede Zusicherung die Ausnahme wiedererkennt.</summary>
    private const string WORTLAUT = "Die Datei konnte nicht gelesen werden.";

    /// <summary>Wirft beim ERSTEN Zeichnen — der Fall „Maske geht gar nicht auf".</summary>
    private sealed class WirftBeimZeichnen : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => throw new InvalidOperationException(WORTLAUT);
    }

    /// <summary>
    /// Zeichnet erst und wirft dann im EREIGNIS — der Fall des Katalogimports:
    /// Der Dialog steht, der Anwender wählt eine Datei, und beim Lesen fliegt
    /// etwas.
    /// </summary>
    private sealed class WirftImEreignis : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "id", "ausloeser");
            builder.AddAttribute(2, "onclick",
                EventCallback.Factory.Create(this, () => throw new NotSupportedException(WORTLAUT)));
            builder.AddContent(3, "Datei laden");
            builder.CloseElement();
        }
    }

    /// <summary>Zeichnet still — der Gegenzeuge für „ohne Wurf ist nichts zu sehen".</summary>
    private sealed class ZeichnetStill : ComponentBase
    {
        [Parameter] public string Gruss { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "id", "inhalt");
            builder.AddContent(2, Gruss);
            builder.CloseElement();
        }
    }

    private IRenderedComponent<Fehlerschranke> MitSchranke<TKind>() where TKind : IComponent
        => Render<Fehlerschranke>(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<TKind>(0);
            b.CloseComponent();
        })));

    // =====================================================================
    //  1 — Die Schranke fängt
    // =====================================================================

    /// <summary>
    /// Eine Kindkomponente, die beim Zeichnen wirft, wird ein Kasten mit Typ und
    /// Wortlaut — und nicht das Ende des Prüflaufs.
    /// </summary>
    [Fact]
    public void Ein_Wurf_beim_Zeichnen_wird_ein_lesbarer_Kasten()
    {
        var cut = MitSchranke<WirftBeimZeichnen>();

        Assert.Single(cut.FindAll(".epos-fehlerschranke"));
        Assert.Equal("In dieser Ansicht ist ein Fehler aufgetreten",
                     cut.Find(".epos-fehlerschranke-titel").TextContent);

        string wortlaut = cut.Find(".epos-fehlerschranke-wortlaut").TextContent;
        Assert.Contains("System.InvalidOperationException", wortlaut);
        Assert.Contains(WORTLAUT, wortlaut);
    }

    /// <summary>
    /// Der eigentliche Fall des Befunds: Der Dialog STEHT, und erst das Ereignis
    /// wirft. Bis W13‑B‑1 riss das die Anwendung mit.
    /// </summary>
    [Fact]
    public void Ein_Wurf_im_Ereignis_reisst_die_Ansicht_nicht_mit()
    {
        var cut = MitSchranke<WirftImEreignis>();

        // Vorher steht die Maske.
        Assert.Empty(cut.FindAll(".epos-fehlerschranke"));

        cut.Find("#ausloeser").Click();

        Assert.Single(cut.FindAll(".epos-fehlerschranke"));
        Assert.Contains("System.NotSupportedException",
                        cut.Find(".epos-fehlerschranke-wortlaut").TextContent);
    }

    /// <summary>
    /// Die innerste Ausnahme steht mit im Kasten — sie trägt in der Regel den
    /// Satz, der weiterhilft (dieselbe Darstellung wie in <c>WebViewWache</c>).
    /// </summary>
    [Fact]
    public void Der_Kasten_nennt_die_innerste_Ausnahme()
    {
        Exception? gemeldet = null;

        var cut = Render<Fehlerschranke>(p =>
        {
            p.Add(x => x.Bezeichnung, "KatalogImportDialog");
            p.Add(x => x.Gefangen, EventCallback.Factory.Create<Exception>(this, e => gemeldet = e));
            p.Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<WirftImEreignis>(0);
                b.CloseComponent();
            }));
        });

        cut.Find("#ausloeser").Click();

        string wortlaut = cut.Find(".epos-fehlerschranke-wortlaut").TextContent;
        Assert.StartsWith("KatalogImportDialog: ", wortlaut);
        Assert.NotNull(gemeldet);
        Assert.IsType<NotSupportedException>(gemeldet);
    }

    /// <summary>
    /// „Weiter" nimmt den Kasten weg und zeichnet den Inhalt neu — der Anwender
    /// ist nicht in eine Sackgasse geraten.
    /// </summary>
    [Fact]
    public void Weiter_stellt_den_Inhalt_wieder_her()
    {
        var cut = MitSchranke<WirftImEreignis>();
        cut.Find("#ausloeser").Click();
        Assert.Single(cut.FindAll(".epos-fehlerschranke"));

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Empty(cut.FindAll(".epos-fehlerschranke"));
        Assert.Single(cut.FindAll("#ausloeser"));
    }

    /// <summary>
    /// Ohne Wurf ist die Schranke NICHT zu sehen — kein Rahmen, keine Fläche,
    /// kein zusätzliches Element. Sonst hätte sie sechzig Dialoge verschoben.
    /// </summary>
    [Fact]
    public void Ohne_Wurf_steht_nur_der_Inhalt()
    {
        var cut = Render<Fehlerschranke>(p => p.Add(x => x.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<ZeichnetStill>(0);
            b.AddComponentParameter(1, nameof(ZeichnetStill.Gruss), "Alles in Ordnung");
            b.CloseComponent();
        })));

        Assert.Empty(cut.FindAll(".epos-fehlerschranke"));
        Assert.Equal("Alles in Ordnung", cut.Find("#inhalt").TextContent);
        Assert.Equal("<p id=\"inhalt\">Alles in Ordnung</p>", cut.Markup);
    }

    /// <summary>
    /// GEGENPROBE. Dieselbe werfende Komponente OHNE Schranke reicht die Ausnahme
    /// durch. Am Gerät endet genau das im Prozessabbruch, weil der WinForms-
    /// <c>BlazorWebView</c> (10.0.100) kein <c>UnhandledException</c>-Ereignis
    /// führt — der Fall belegt, dass die Schranke wirklich etwas tut.
    /// </summary>
    [Fact]
    public void Ohne_Schranke_kommt_die_Ausnahme_durch()
    {
        Assert.Throws<InvalidOperationException>(() => Render<WirftBeimZeichnen>());
    }

    // =====================================================================
    //  2 — Der Wirt Wurzel<T>
    // =====================================================================

    /// <summary>
    /// <c>Wurzel&lt;T&gt;</c> ist das fehlende Zwischenglied: Eine
    /// <c>ErrorBoundary</c> fängt nur ihre NACHFAHREN, eine Wurzelkomponente hat
    /// aber nichts über sich. Der Wirt reicht den Parametersatz UNVERÄNDERT
    /// durch — das ist die Zusage, auf der die zwei Windows-Hüllen beruhen.
    /// </summary>
    [Fact]
    public void Der_Wirt_reicht_den_Parametersatz_unveraendert_durch()
    {
        var cut = Render<Wurzel<ZeichnetStill>>(p =>
            p.Add(x => x.Gaben, new System.Collections.Generic.Dictionary<string, object>
            {
                ["Gruss"] = "Aus der Huelle"
            }));

        Assert.Equal("Aus der Huelle", cut.Find("#inhalt").TextContent);
        Assert.Empty(cut.FindAll(".epos-fehlerschranke"));
    }

    /// <summary>
    /// Und er fängt: Dieselbe Wurzel mit einer werfenden Komponente zeigt den
    /// Kasten samt Typnamen der Komponente statt den Lauf mitzureißen.
    /// </summary>
    [Fact]
    public void Der_Wirt_faengt_den_Wurf_seiner_Wurzelkomponente()
    {
        var cut = Render<Wurzel<WirftImEreignis>>();

        cut.Find("#ausloeser").Click();

        string wortlaut = cut.Find(".epos-fehlerschranke-wortlaut").TextContent;
        Assert.StartsWith("WirftImEreignis: ", wortlaut);
        Assert.Contains(WORTLAUT, wortlaut);
    }

    /// <summary>
    /// Ein LEERER Parametersatz ist erlaubt — die iOS-Wurzel mountet
    /// <c>Wurzel&lt;AppWurzel&gt;</c> ganz ohne Gaben.
    /// </summary>
    [Fact]
    public void Der_Wirt_kommt_ohne_Gaben_aus()
    {
        var cut = Render<Wurzel<ZeichnetStill>>();

        Assert.Equal("", cut.Find("#inhalt").TextContent);
    }

    // =====================================================================
    //  3 — Die Sprache
    // =====================================================================

    /// <summary>
    /// Der Kasten spricht die Oberflächensprache. Auf Englisch stehen die
    /// englischen Texte — geprüft am Titel und an den zwei Knöpfen.
    /// </summary>
    [Fact]
    public void Der_Kasten_gibt_es_auch_auf_Englisch()
    {
        var en = new CultureInfo("en-US");
        CultureInfo vorher = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = en;
            CultureInfo.DefaultThreadCurrentUICulture = en;
            CultureInfo.CurrentUICulture = en;

            var cut = MitSchranke<WirftBeimZeichnen>();

            Assert.Equal("An error occurred in this view",
                         cut.Find(".epos-fehlerschranke-titel").TextContent);
            Assert.Equal("Continue", cut.Find(".epos-knopf--primaer").TextContent);
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = vorher;
            CultureInfo.DefaultThreadCurrentUICulture = vorher;
            CultureInfo.CurrentUICulture = vorher;
        }
    }
}

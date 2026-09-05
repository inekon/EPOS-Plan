using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die HÜLLENGLEICHE Probe über die Ziele der 21 Startkacheln — Befund
/// <b>W16b‑B‑1</b> der Windows-Abnahme vom 05.09.2026.
///
/// <para><b>Der Befund.</b> Ein aus der Startseite geöffneter Dialog stand am
/// Gerät als leere beige Fläche da. Zwei Ursachen wären in dieser Bibliothek zu
/// suchen: ein Parametersatz, den die Komponente nicht annimmt (das prüft
/// <c>ParametersatzTests</c>), oder eine Ausnahme beim ERSTEN Zeichnen. Dieser
/// Fall schließt die zweite aus.</para>
///
/// <para><b>Warum „ohne Gaben".</b> Eine Windows-Hülle reicht ihren
/// Parametersatz über <c>AddMultipleAttributes</c> herein — und genau diesen Weg
/// geht die Probe, nur mit einem LEEREN Wörterbuch. Das ist der härtere Fall:
/// Jeder Delegat ist <c>null</c>, jede Liste leer, jeder Text der deutsche
/// Rückfall. Wer so zeichnet, zeichnet auch mit Daten — wer hier bricht, hätte
/// beim Anwender die beige Fläche erzeugt. Die Datenbank bleibt dabei außen vor
/// (Hausregel EPOS.UI).</para>
///
/// <para>Die Sprache ist auf de-DE gepinnt (Regel seit iU9-W8).</para>
/// </summary>
public class StartkachelDialogeTests : BunitContext
{
    public StartkachelDialogeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        Services.AddSingleton<IProjektQuelle>(new KeineProjekte());
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

    /// <summary>
    /// Die Ziele der 21 Kachelwege aus <c>StartseiteHuelle.Kachelweg</c> —
    /// dazu die zwei Assistentenseiten ohne eigene Kachel
    /// (<c>KomponentenauswahlDialog</c>, <c>ProjektKopfSeite</c>) und die
    /// Ergebnisblätter, die aus ihnen aufgehen.
    /// </summary>
    public static TheoryData<string> Kachelziele => new()
    {
        "AssistentSeite",            // Projekt neu / Projekt öffnen-bearbeiten
        "ProjektWahlDialog",         // Zuletzt geöffnet (Rückfall), Projekt löschen
        "ProjektKopieDialog",        // Speichern unter…
        "GebaeudeDialog",            // Gebäudedaten
        "GebaeudeKatalogDialog",
        "GebaeudeWohnflaecheDialog",
        "WaermebedarfExternDialog",  // Daten importieren
        "BedarfsProfileDialog",      // Prozesswärme, Brauchwasser, Standardlastprofil
        "BedarfErgebnisDialog",
        "TypStammDialog",            // Eigenes Profil (Stammdaten)
        "TypProfilDialog",
        "StromganglinieDialog",      // Messdaten importieren
        "WaermepumpenDialog",
        "HeizkesselDialog",
        "SolarkollektorenDialog",
        "BhkwDialog",
        "PhotovoltaikDialog",
        "StromspeicherDialog",
        "PufferspeicherDialog",
        "KomponentenauswahlDialog",  // Assistentenschritt 0
        "ProjektKopfSeite"           // Assistentenschritt 1
    };

    /// <summary>
    /// Jedes Kachelziel zeichnet — auf dem Weg, den die Windows-Hülle geht,
    /// und ohne einen einzigen Parameter.
    /// </summary>
    [Theory]
    [MemberData(nameof(Kachelziele))]
    public void Jedes_Kachelziel_zeichnet_auch_ohne_Gaben(string name)
    {
        Type komponente = Komponente(name);

        var gezeichnet = AusHuelle(komponente, new Dictionary<string, object>());

        Assert.False(string.IsNullOrWhiteSpace(gezeichnet.Markup),
                     name + " hat nichts gezeichnet.");
    }

    /// <summary>
    /// Der PROJEKTASSISTENT mit dem Parametersatz, den
    /// <c>AssistentHuelle.Gaben</c> baut — Schlüssel für Schlüssel derselbe
    /// Satz, nur mit stillen Delegaten. Er ist das Ziel der Kachel „Neues
    /// Projekt", also genau der Weg des Befunds.
    /// </summary>
    [Fact]
    public void Der_Assistent_zeichnet_mit_dem_Parametersatz_seiner_Huelle()
    {
        var gaben = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Betriebsart"] = 0,
            ["Projekte"] = Array.Empty<ProjektKopfZeile>(),
            ["SeiteAktiv"] = new Func<int, bool>(_ => true),
            ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>?>(_ => null),
            ["SeiteVerlassen"] = new Action<int>(_ => { }),
            ["ProjektMarkiert"] = new Action<int, string>((_, _) => { }),
            ["ProjektOeffnen"] = new Action<int, string>((_, _) => { }),
            ["Speichern"] = new Func<(string Text, string Titel)?>(() => null),
            ["AbbrechenText"] = "Abbrechen",
            ["ZurueckText"] = "◀ Zurück",
            ["WeiterText"] = "Weiter ▶",
            ["SpeichernText"] = "Speichern",
            ["ProjektLabelText"] = "Bestehendes Projekt auswählen",
            ["ProjektOeffnenText"] = "Projekt öffnen",
            ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), _ => { })
        };

        var gezeichnet = AusHuelle(Komponente("AssistentSeite"), gaben);

        // Die Wurzel steht - und mit ihr die Fussleiste des Rahmens. Genau das
        // fehlte am Geraet: eine Flaeche in --epos-flaeche und sonst nichts.
        Assert.Contains("epos-assistentseite", gezeichnet.Markup, StringComparison.Ordinal);
        Assert.Contains("epos-assistent-fuss", gezeichnet.Markup, StringComparison.Ordinal);

        string[] knoepfe = gezeichnet.FindAll(".epos-assistent-fuss .epos-knopf")
                                     .Select(k => k.TextContent.Trim())
                                     .ToArray();
        Assert.Equal(new[] { "Abbrechen", "◀ Zurück", "Weiter ▶" }, knoepfe);
    }

    /// <summary>
    /// Ein Schlüssel ohne <c>[Parameter]</c> bricht beim ERSTEN Zeichnen —
    /// die Gegenprobe zu <c>ParametersatzTests</c> und zu
    /// <c>Parametersatzwache</c>. Sie steht hier, damit die Begründung der
    /// beiden Wachen nachprüfbar ist und nicht nur behauptet.
    /// </summary>
    [Fact]
    public void Ein_fremder_Schluessel_bricht_beim_ersten_Zeichnen()
    {
        var gaben = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["GibtEsNichtProbe"] = 1
        };

        Assert.ThrowsAny<InvalidOperationException>(
            () => AusHuelle(Komponente("AssistentSeite"), gaben));
    }

    // =====================================================================
    //  Der Weg der Hülle
    // =====================================================================

    /// <summary>
    /// Zeichnet <paramref name="komponente"/> auf demselben Weg, den die
    /// Windows-Hülle geht: ein WÖRTERBUCH wird zum Parametersatz.
    ///
    /// <para><c>BlazorDialogForm&lt;T&gt;</c> reicht es über
    /// <c>RootComponents.Add&lt;T&gt;("#app", parameter)</c> herein; hier tut
    /// es <see cref="DynamicComponent"/>. Beide enden in
    /// <c>ParameterView.FromDictionary</c> und
    /// <c>SetParameterProperties</c> — und genau dort bricht ein Schlüssel
    /// ohne <c>[Parameter]</c>. Der Umweg über <c>DynamicComponent</c> ist
    /// nötig, weil der Typ hier erst zur Laufzeit feststeht.</para>
    /// </summary>
    private Bunit.IRenderedComponent<DynamicComponent> AusHuelle(
        Type komponente, IDictionary<string, object> gaben)
    {
        return Render<DynamicComponent>(builder =>
        {
            builder.OpenComponent<DynamicComponent>(0);
            builder.AddComponentParameter(1, nameof(DynamicComponent.Type), komponente);
            builder.AddComponentParameter(2, nameof(DynamicComponent.Parameters),
                                          (IDictionary<string, object?>)gaben!);
            builder.CloseComponent();
        });
    }

    private static Type Komponente(string name)
    {
        Type? t = typeof(EPOS.UI.Bausteine.Kachel).Assembly
                                                  .GetTypes()
                                                  .FirstOrDefault(x => x.Name == name);
        Assert.True(t is not null, "Die Komponente " + name + " gibt es in EPOS.UI nicht.");
        return t!;
    }
}

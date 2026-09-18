using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Schliesskreuz — das ✕ rechts aussen in der Kopfzeile eines Dialogs
/// (Anwenderentscheid 15.09.2026: „Alle Dialoge sollten mit einem Kreuz zu schließen
/// sein und nicht erst ganz unten mit den Buttons"). Es wirkt wie Esc in diesem
/// Dialog, also wie <b>Abbrechen</b>, und meldet deshalb ohne Ergebnis.
///
/// <para>Die Klasse pinnt die Kultur (<see cref="EposBunitContext"/>), weil der
/// Vorgabe-Kurztext ein DEUTSCHER Ressourcentext ist
/// (<c>SCHLIESSKREUZ_TOOLTIP</c>, Rückfall „Schließen") — der CI-Läufer steht auf
/// <c>en-US</c>.</para>
/// </summary>
public class SchliesskreuzTests : EposBunitContext
{
    // =====================================================================
    //  Wirte für die Tastenfälle
    // =====================================================================

    /// <summary>
    /// Ein Wirt wie eine Dialogwurzel: ein Kasten mit eigenem <c>onkeydown</c>, darin
    /// das Kreuz. In einem reinen OK-Dialog löst Enter dort das OK aus — und genau
    /// davor schützt <c>@onkeydown:stopPropagation</c> am Kreuz: Ohne die Sperre
    /// feuerten OK (keydown) UND Abbrechen (click) zugleich.
    ///
    /// <para><c>public</c>, nicht <c>private</c>: bunit erzeugt den Wirt und belegt
    /// seine <c>[Parameter]</c> über Reflexion — dafür muss auch der TYP erreichbar
    /// sein, nicht nur die Eigenschaft.</para>
    /// </summary>
    public sealed class Wirt : ComponentBase
    {
        [Parameter] public EventCallback Geschlossen { get; set; }
        [Parameter] public EventCallback<KeyboardEventArgs> WirtTaste { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "epos-dialog");
            builder.AddAttribute(2, "onkeydown", WirtTaste);
            builder.OpenComponent<Schliesskreuz>(3);
            builder.AddAttribute(4, nameof(Schliesskreuz.Geschlossen), Geschlossen);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// Derselbe Wirt mit einem NACKTEN Knopf statt des Kreuzes — die Gegenprobe
    /// (Lehre W6‑B‑1: eine Wache, die nie rot werden kann, prüft nichts). Sie zeigt,
    /// dass der Prüfstand Tastendrücke überhaupt aufsteigen lässt; erst dadurch ist
    /// „Enter erreicht den Wirt NICHT" eine Aussage über die Sperre und nicht über
    /// bunit.
    /// </summary>
    public sealed class WirtOhneSperre : ComponentBase
    {
        [Parameter] public EventCallback<KeyboardEventArgs> WirtTaste { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "epos-dialog");
            builder.AddAttribute(2, "onkeydown", WirtTaste);
            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "type", "button");
            builder.AddAttribute(5, "class", "nacktes-kreuz");
            builder.AddContent(6, "✕");
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    // =====================================================================
    //  1 — Der Knopf selbst
    // =====================================================================

    [Fact]
    public void Es_steht_ein_Knopf_mit_der_Hausklasse_und_dem_Zeichen()
    {
        var cut = Render<Schliesskreuz>();

        var knopf = cut.Find("button.epos-dialog-zu");
        Assert.Equal("button", knopf.GetAttribute("type"));
        Assert.Contains("epos-knopf", knopf.ClassName);
        Assert.Equal("✕", knopf.TextContent.Trim());
    }

    /// <summary>
    /// Ohne eigenen Kurztext steht der Ressourcentext an BEIDEN Stellen — der Tooltip
    /// für die Maus, das <c>aria-label</c> für die Sprachausgabe; das Zeichen ✕ ist
    /// als Text kein Name.
    /// </summary>
    [Fact]
    public void Ohne_eigenen_Kurztext_tragen_Tooltip_und_aria_label_den_Vorgabetext()
    {
        var cut = Render<Schliesskreuz>();

        var knopf = cut.Find("button.epos-dialog-zu");
        Assert.Equal("Schließen", knopf.GetAttribute("title"));
        Assert.Equal("Schließen", knopf.GetAttribute("aria-label"));
    }

    [Fact]
    public void Ein_eigener_Kurztext_gewinnt()
    {
        var cut = Render<Schliesskreuz>(p => p.Add(x => x.Kurztext, "Katalog schließen"));

        var knopf = cut.Find("button.epos-dialog-zu");
        Assert.Equal("Katalog schließen", knopf.GetAttribute("title"));
        Assert.Equal("Katalog schließen", knopf.GetAttribute("aria-label"));
    }

    // =====================================================================
    //  2 — Der Rückweg
    // =====================================================================

    [Fact]
    public void Der_Klick_meldet_Geschlossen_genau_einmal()
    {
        int geschlossen = 0;
        var cut = Render<Schliesskreuz>(p => p.Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find("button.epos-dialog-zu").Click();

        Assert.Equal(1, geschlossen);
    }

    /// <summary>
    /// Esc auf dem fokussierten Kreuz wirkt wie ein Klick darauf — das Kreuz IST die
    /// Esc-Aktion des Dialogs.
    /// </summary>
    [Fact]
    public void Escape_auf_dem_Kreuz_meldet_Geschlossen()
    {
        int geschlossen = 0;
        var cut = Render<Schliesskreuz>(p => p.Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, geschlossen);
    }

    /// <summary>
    /// Enter meldet NICHTS: Auf einem <c>&lt;button&gt;</c> erzeugt es den nativen
    /// Klick, und der meldet bereits. Eine zweite Behandlung hier feuerte doppelt.
    /// </summary>
    [Fact]
    public void Enter_auf_dem_Kreuz_meldet_Geschlossen_nicht()
    {
        int geschlossen = 0;
        var cut = Render<Schliesskreuz>(p => p.Add(x => x.Geschlossen, () => geschlossen++));

        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(0, geschlossen);
    }

    // =====================================================================
    //  3 — Die Sperre (@onkeydown:stopPropagation)
    // =====================================================================

    [Fact]
    public void Enter_auf_dem_Kreuz_erreicht_den_Wirt_nicht()
    {
        int geschlossen = 0;
        int wirtTreffer = 0;
        var cut = Render<Wirt>(p => p
            .Add(x => x.Geschlossen, () => geschlossen++)
            .Add(x => x.WirtTaste, (KeyboardEventArgs _) => wirtTreffer++));

        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(0, wirtTreffer);
        Assert.Equal(0, geschlossen);
    }

    [Fact]
    public void Escape_auf_dem_Kreuz_erreicht_den_Wirt_nicht_meldet_aber_Geschlossen()
    {
        int geschlossen = 0;
        int wirtTreffer = 0;
        var cut = Render<Wirt>(p => p
            .Add(x => x.Geschlossen, () => geschlossen++)
            .Add(x => x.WirtTaste, (KeyboardEventArgs _) => wirtTreffer++));

        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, wirtTreffer);
        Assert.Equal(1, geschlossen);
    }

    /// <summary>
    /// Die Gegenprobe zu den beiden Fällen darüber: Ohne die Sperre steigt derselbe
    /// Tastendruck sehr wohl bis zum Wirt auf. Wird dieser Fall rot, misst der
    /// Prüfstand kein Aufsteigen mehr — und die beiden Fälle darüber wären grün,
    /// ohne etwas zu beweisen.
    /// </summary>
    [Fact]
    public void Ohne_die_Sperre_erreicht_Enter_den_Wirt()
    {
        int wirtTreffer = 0;
        var cut = Render<WirtOhneSperre>(p => p
            .Add(x => x.WirtTaste, (KeyboardEventArgs _) => wirtTreffer++));

        cut.Find(".nacktes-kreuz").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(1, wirtTreffer);
    }

    // =====================================================================
    //  4 — Die Sperre am Baustein (Gesperrt, Anwenderentscheid 18.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>Die Vorgabe ist <c>false</c></b> — und das ist der Kern des Entscheids: Die
    /// rund achtzig bestehenden Verwendungen setzen den Parameter nicht und dürfen sich
    /// deshalb um kein Zeichen ändern. Gemessen wird beides: kein <c>disabled</c> im
    /// Markup UND der Klick, der weiterhin meldet.
    /// </summary>
    [Fact]
    public void Ohne_gesetzten_Parameter_bleibt_das_Kreuz_offen_und_klickbar()
    {
        int geschlossen = 0;
        var cut = Render<Schliesskreuz>(p => p.Add(x => x.Geschlossen, () => geschlossen++));

        var knopf = cut.Find("button.epos-dialog-zu");
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.False(cut.Instance.Gesperrt);

        knopf.Click();
        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(2, geschlossen);
    }

    /// <summary>
    /// Gesperrt heißt <b>hart</b> gesperrt: Der Knopf trägt <c>disabled</c>, nicht
    /// <c>aria-disabled</c>. Die weiche Bauart (Hausregel W16b‑E‑6) gehört dorthin, wo
    /// ein Tooltip den GRUND nennt — das Kreuz nennt keinen, weil der Rückweg daneben
    /// auch keinen nennt.
    /// </summary>
    [Fact]
    public void Gesperrt_sperrt_den_Knopf_hart()
    {
        var cut = Render<Schliesskreuz>(p => p.Add(x => x.Gesperrt, true));

        var knopf = cut.Find("button.epos-dialog-zu");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.False(knopf.HasAttribute("aria-disabled"));

        // Es bleibt DASSELBE Kreuz: gleiche Klassen, gleiches Zeichen, gleicher Name.
        Assert.Contains("epos-knopf", knopf.ClassName);
        Assert.Contains("epos-dialog-zu", knopf.ClassName);
        Assert.Equal("✕", knopf.TextContent.Trim());
        Assert.Equal("Schließen", knopf.GetAttribute("title"));
    }

    /// <summary>
    /// <b>Weder Zeiger noch Tastatur führen hinaus.</b> Ein <c>disabled</c>-Knopf
    /// bekommt im Browser keine Ereignisse mehr — darauf verlässt sich der Baustein
    /// nicht: Auch der Tastenweg schweigt, und gemessen wird es mit Klick UND
    /// Tastendruck in EINEM Fall.
    /// </summary>
    [Fact]
    public void Im_gesperrten_Zustand_meldet_weder_Klick_noch_Taste()
    {
        int geschlossen = 0;
        var cut = Render<Schliesskreuz>(p => p
            .Add(x => x.Geschlossen, () => geschlossen++)
            .Add(x => x.Gesperrt, true));

        cut.Find("button.epos-dialog-zu").Click();
        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(0, geschlossen);
    }

    /// <summary>
    /// Die Sperre ist ein ZUSTAND, kein Abschied: Fällt sie, meldet dasselbe Kreuz
    /// wieder. Ohne diesen Fall wäre „gesperrt meldet nicht" auch dann grün, wenn der
    /// Baustein überhaupt nicht mehr meldete.
    /// </summary>
    [Fact]
    public void Faellt_die_Sperre_meldet_dasselbe_Kreuz_wieder()
    {
        int geschlossen = 0;
        var cut = Render<Schliesskreuz>(p => p
            .Add(x => x.Geschlossen, () => geschlossen++)
            .Add(x => x.Gesperrt, true));

        cut.Find("button.epos-dialog-zu").Click();
        Assert.Equal(0, geschlossen);

        cut.Render(p => p.Add(x => x.Gesperrt, false));

        Assert.False(cut.Find("button.epos-dialog-zu").HasAttribute("disabled"));
        cut.Find("button.epos-dialog-zu").Click();
        cut.Find(".epos-dialog-zu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(2, geschlossen);
    }
}

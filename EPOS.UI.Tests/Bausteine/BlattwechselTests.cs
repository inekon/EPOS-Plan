using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <see cref="Blattwechsel"/> (Umsetzungskonzept Zapfprofilgenerator, Nachtrag
/// N35, Variante a): Er tauscht den Inhalt seines Wirtsdialogs gegen ein Blatt, statt eine
/// weitere <c>Ueberlagerung</c> zu öffnen. bunit rechnet weder Rollstand noch Fokus — es
/// prüft hier, WAS der Baustein anstößt (die Aufrufe seines Moduls <c>epos-blatt.js</c>);
/// gemessen ist das im Browser (Probe zu N35, siehe Statuszeile).
/// </summary>
public class BlattwechselTests : EposBunitContext
{
    private const string MODUL = "./_content/EPOS.UI/epos-blatt.js";

    public BlattwechselTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<Blattwechsel> Aufbauen(
        bool offen = true, string hilfe = "", Action? zurueck = null, string inhalt = "Der Inhalt")
        => Render<Blattwechsel>(p => p
            .Add(x => x.Offen, offen)
            .Add(x => x.WirtTitel, "Warmwasser")
            .Add(x => x.Titel, "Zapfprofil")
            .Add(x => x.HilfeSchluessel, hilfe)
            .Add(x => x.Zurueck, () => zurueck?.Invoke())
            .Add(x => x.KindInhalt, (RenderFragment)(b => b.AddContent(0, inhalt))));

    /// <summary>Zu, zeichnet der Baustein nichts als seinen unsichtbaren Anker im Wirtsbaum.</summary>
    [Fact]
    public void Geschlossen_steht_nur_der_unsichtbare_Anker()
    {
        var cut = Aufbauen(offen: false);

        Assert.Empty(cut.FindAll(".epos-blatt"));
        Assert.DoesNotContain("Der Inhalt", cut.Markup);
        Assert.True(cut.Find(".epos-blatt-anker").HasAttribute("hidden"));
    }

    /// <summary>Offen: Rückknopf „‹ {Wirtstitel}", Titel des Blattes und der Inhalt — ohne eigenes Kreuz.</summary>
    [Fact]
    public void Offen_zeigt_Rueckknopf_Titel_und_Inhalt_ohne_Kreuz()
    {
        var cut = Aufbauen();

        Assert.Equal("‹ Warmwasser", cut.Find(".epos-blatt-zurueck").TextContent.Trim());
        Assert.Equal("Zapfprofil", cut.Find(".epos-blatt-titel").TextContent);
        Assert.Contains("Der Inhalt", cut.Find(".epos-blatt-inhalt").TextContent);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    /// <summary>Eine Hilfepille trägt das Blatt nur mit Schlüssel — sonst gäbe es sie doppelt, wo die Komponente darin ihre eigene führt.</summary>
    [Fact]
    public void Die_Hilfepille_steht_nur_mit_Schluessel()
    {
        Assert.Empty(Aufbauen().FindAll(".epos-blatt-kopf .epos-hilfepille"));
        Assert.Single(Aufbauen(hilfe: "Form_Test.btn_Help").FindAll(".epos-blatt-kopf .epos-hilfepille"));
    }

    /// <summary>Die Zusatzklasse markiert das Blatt für die Regel, die die tragende Überlagerung weitet.</summary>
    [Fact]
    public void Die_Zusatzklasse_kommt_am_Blatt_an()
    {
        var cut = Render<Blattwechsel>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.ZusatzKlasse, "epos-blatt--breit"));

        Assert.Contains("epos-blatt--breit", cut.Find(".epos-blatt").ClassList);
    }

    /// <summary>Der Rückknopf führt zurück — einmal.</summary>
    [Fact]
    public void Der_Rueckknopf_ruft_Zurueck_einmal()
    {
        int n = 0;
        var cut = Aufbauen(zurueck: () => n++);

        cut.Find(".epos-blatt-zurueck").Click();

        Assert.Equal(1, n);
    }

    /// <summary>Esc auf dem Blatt wirkt wie der Rückknopf, jede andere Taste nichts.</summary>
    [Fact]
    public void Esc_fuehrt_zurueck_andere_Tasten_nicht()
    {
        int n = 0;
        var cut = Aufbauen(zurueck: () => n++);

        cut.Find(".epos-blatt").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.Find(".epos-blatt").KeyDown(new KeyboardEventArgs { Key = "a" });
        Assert.Equal(0, n);

        cut.Find(".epos-blatt").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, n);
    }

    /// <summary>
    /// Esc gehört dem Blatt allein: Die Taste steigt nicht zum Wirt auf — sonst schlösse sein
    /// eigener Esc-Halter, der erst NACH dem Blatt läuft und dessen Sperre dann schon offen ist,
    /// den ganzen Dialog (gefunden mit dem Fall des Zapfprofils in <c>BedarfsProfileDialogTests</c>).
    /// </summary>
    [Fact]
    public void Esc_steigt_nicht_zum_Wirt_auf()
    {
        int blatt = 0;
        int wirt = 0;
        var cut = Render(b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "wirt");
            b.AddAttribute(2, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, _ => wirt++));
            b.OpenComponent<Blattwechsel>(3);
            b.AddComponentParameter(4, nameof(Blattwechsel.Offen), true);
            b.AddComponentParameter(5, nameof(Blattwechsel.Zurueck),
                EventCallback.Factory.Create(this, () => blatt++));
            b.CloseComponent();
            b.CloseElement();
        });

        cut.Find(".epos-blatt").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, blatt);
        Assert.Equal(0, wirt);
    }

    /// <summary>
    /// Der Rollstand: Der Baustein merkt sich den Stand des Wirts, ehe der Teilbaum getauscht
    /// ist, rollt das Blatt beim Öffnen nach oben und stellt den Stand beim Rückweg wieder her
    /// (Browsermessung zu N35: ohne das stand das Blatt 124 px gerollt, seine Kopfzeile über dem Rand).
    /// </summary>
    [Fact]
    public void Der_Rollstand_des_Wirts_wird_gemerkt_das_Blatt_rollt_nach_oben_und_der_Rueckweg_stellt_ihn_wieder_her()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var modul = JSInterop.SetupModule(MODUL);
        modul.Setup<double>("rollstand", _ => true).SetResult(137);
        var stelle = modul.SetupVoid("stelleRollstand", _ => true);

        var cut = Aufbauen(offen: false);
        Assert.Empty(stelle.Invocations);

        cut.Render(p => p.Add(x => x.Offen, true));
        cut.WaitForAssertion(() => Assert.Single(stelle.Invocations));
        Assert.Equal(0d, stelle.Invocations.Single().Arguments[1]);

        cut.Render(p => p.Add(x => x.Offen, false));
        cut.WaitForAssertion(() => Assert.Equal(2, stelle.Invocations.Count));
        Assert.Equal(137d, stelle.Invocations.Last().Arguments[1]);
    }

    /// <summary>Ohne Modul bleibt das Blatt ein Blatt ohne Rollnachführung — kein Fehlschlag.</summary>
    [Fact]
    public void Ohne_Modul_steht_das_Blatt_trotzdem_da_und_fuehrt_zurueck()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;   // der import ist nicht eingerichtet und wirft
        int n = 0;
        var cut = Aufbauen(offen: false, zurueck: () => n++);

        cut.Render(p => p.Add(x => x.Offen, true));

        Assert.Equal("Zapfprofil", cut.Find(".epos-blatt-titel").TextContent);
        cut.Find(".epos-blatt-zurueck").Click();
        Assert.Equal(1, n);
    }
}

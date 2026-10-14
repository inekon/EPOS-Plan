using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Schließen einer <see cref="Ueberlagerung"/> mit offenem Blatt</b> (Anwenderwunsch 08.10.2026): Steht im
/// Inhalt ein <see cref="Blattwechsel"/>, führen Kreuz, Esc und Hintergrund nur zum vorigen Blatt zurück; erst vom
/// Wurzelblatt aus schließt der Dialog. Ein kleiner Wirt nach dem Muster des Gebäudedialogs (Überlagerung mit
/// Editor, darin zwei Blätter, eines im anderen) hält den Zustand wie ein echter Wirt.
/// </summary>
public class UeberlagerungBlattTests : EposBunitContext
{
    public UeberlagerungBlattTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Der Prüfwirt: eine Überlagerung (Editor), darin Blatt A und in A das Blatt B.</summary>
    private sealed class Wirt : ComponentBase
    {
        [Parameter] public bool Hintergrund { get; set; }
        public bool Offen = true;
        public bool BlattA;
        public bool BlattB;
        public int Geschlossen;
        public int RueckwegA;
        public int RueckwegB;

        public void Oeffne(bool a, bool b) { BlattA = a; BlattB = b; StateHasChanged(); }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Ueberlagerung>(0);
            b.AddAttribute(1, nameof(Ueberlagerung.Offen), Offen);
            b.AddAttribute(2, nameof(Ueberlagerung.Titel), "Gebäude im Projekt");
            b.AddAttribute(3, nameof(Ueberlagerung.SchliessenBeiHintergrund), Hintergrund);
            b.AddAttribute(4, nameof(Ueberlagerung.Geschlossen),
                EventCallback.Factory.Create(this, () => { Offen = false; Geschlossen++; }));
            b.AddAttribute(5, nameof(Ueberlagerung.KindInhalt), (RenderFragment)(i =>
            {
                if (!BlattA) i.AddMarkupContent(0, "<p class=\"wurzelblatt\">Gebäude bearbeiten</p>");
                i.OpenComponent<Blattwechsel>(1);
                i.AddAttribute(2, nameof(Blattwechsel.Offen), BlattA);
                i.AddAttribute(3, nameof(Blattwechsel.WirtTitel), "Gebäude bearbeiten");
                i.AddAttribute(4, nameof(Blattwechsel.Titel), "Vorlagen der Konditionierung");
                i.AddAttribute(5, nameof(Blattwechsel.Zurueck),
                    EventCallback.Factory.Create(this, () => { BlattA = false; RueckwegA++; }));
                i.AddAttribute(6, nameof(Blattwechsel.KindInhalt), (RenderFragment)(a =>
                {
                    if (!BlattA) return;
                    if (!BlattB) a.AddMarkupContent(0, "<p class=\"blatt-a\">Vorlagen</p>");
                    a.OpenComponent<Blattwechsel>(1);
                    a.AddAttribute(2, nameof(Blattwechsel.Offen), BlattB);
                    a.AddAttribute(3, nameof(Blattwechsel.WirtTitel), "Vorlagen der Konditionierung");
                    a.AddAttribute(4, nameof(Blattwechsel.Titel), "Kalender");
                    a.AddAttribute(5, nameof(Blattwechsel.Zurueck),
                        EventCallback.Factory.Create(this, () => { BlattB = false; RueckwegB++; }));
                    a.AddAttribute(6, nameof(Blattwechsel.KindInhalt),
                        (RenderFragment)(k => k.AddMarkupContent(0, "<p class=\"blatt-b\">Kalender</p>")));
                    a.CloseComponent();
                }));
                i.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    private IRenderedComponent<Wirt> Aufbauen(bool a, bool b, bool hintergrund = false)
    {
        var cut = Render<Wirt>(p => p.Add(x => x.Hintergrund, hintergrund));
        cut.InvokeAsync(() => cut.Instance.Oeffne(a, b));
        return cut;
    }

    private static void Kreuz(IRenderedComponent<Wirt> cut) => cut.Find(".epos-ueberlagerung-zu").Click();
    private static void Esc(IRenderedComponent<Wirt> cut) => cut.Find(".epos-ueberlagerung").KeyDown("Escape");
    private static void Hintergrund(IRenderedComponent<Wirt> cut) => cut.Find(".epos-ueberlagerung-hintergrund").Click();

    public static TheoryData<string> Wege => new() { "Kreuz", "Esc", "Hintergrund" };

    private static void Schliessen(IRenderedComponent<Wirt> cut, string weg)
    {
        switch (weg)
        {
            case "Kreuz": Kreuz(cut); break;
            case "Esc": Esc(cut); break;
            default: Hintergrund(cut); break;
        }
    }

    [Theory]
    [MemberData(nameof(Wege))]
    public void Mit_offenem_Blatt_fuehrt_Schliessen_zum_vorigen_Blatt_zurueck(string weg)
    {
        var cut = Aufbauen(a: true, b: false, hintergrund: true);
        Assert.Single(cut.FindAll("section.epos-blatt"));

        Schliessen(cut, weg);

        Assert.Equal(1, cut.Instance.RueckwegA);
        Assert.Equal(0, cut.Instance.Geschlossen);
        Assert.True(cut.Instance.Offen);
        Assert.Empty(cut.FindAll("section.epos-blatt"));
        Assert.NotNull(cut.Find(".wurzelblatt"));
    }

    [Theory]
    [MemberData(nameof(Wege))]
    public void Ohne_offenes_Blatt_schliesst_Schliessen_den_Dialog(string weg)
    {
        var cut = Aufbauen(a: false, b: false, hintergrund: true);

        Schliessen(cut, weg);

        Assert.Equal(1, cut.Instance.Geschlossen);
        Assert.False(cut.Instance.Offen);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Zwei_Blaetter_gehen_einzeln_zurueck_dann_schliesst_der_Dialog()
    {
        var cut = Aufbauen(a: true, b: true);
        Assert.Equal(2, cut.FindAll("section.epos-blatt").Count);

        Kreuz(cut);
        Assert.Equal((1, 0, 0), (cut.Instance.RueckwegB, cut.Instance.RueckwegA, cut.Instance.Geschlossen));
        Assert.NotNull(cut.Find(".blatt-a"));

        Kreuz(cut);
        Assert.Equal((1, 1, 0), (cut.Instance.RueckwegB, cut.Instance.RueckwegA, cut.Instance.Geschlossen));
        Assert.NotNull(cut.Find(".wurzelblatt"));

        Kreuz(cut);
        Assert.Equal(1, cut.Instance.Geschlossen);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ohne_SchliessenBeiHintergrund_bleibt_der_Hintergrund_folgenlos_auch_mit_Blatt()
    {
        var cut = Aufbauen(a: true, b: false, hintergrund: false);

        Hintergrund(cut);

        Assert.Equal((0, 0), (cut.Instance.RueckwegA, cut.Instance.Geschlossen));
        Assert.Single(cut.FindAll("section.epos-blatt"));
    }

    [Fact]
    public void Esc_im_Blatt_fuehrt_nur_das_Blatt_zurueck()
    {
        var cut = Aufbauen(a: true, b: false);

        cut.Find("section.epos-blatt").KeyDown("Escape");

        Assert.Equal((1, 0), (cut.Instance.RueckwegA, cut.Instance.Geschlossen));
        Assert.True(cut.Instance.Offen);
    }

    [Fact]
    public void Der_Stapel_meldet_ein_geschlossenes_Blatt_ab()
    {
        var stapel = new Blattstapel();
        object blatt = new();
        int rueck = 0;
        stapel.Anmelden(blatt, () => { rueck++; return Task.CompletedTask; });
        stapel.Anmelden(blatt, () => { rueck += 10; return Task.CompletedTask; });
        Assert.Equal(1, stapel.Anzahl);

        Assert.True(stapel.ZurueckZumVorigen().Result);
        Assert.Equal(10, rueck);

        stapel.Abmelden(blatt);
        Assert.False(stapel.BlattOffen);
        Assert.False(stapel.ZurueckZumVorigen().Result);
    }
}

using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Einzeltage der Kalenderbedienung als Klappgruppe</b> (KAL1): beim Öffnen zugeklappt, die Anzahl im Kopf
/// (0, 1, 9), Klick auf die Kopfzeile klappt auf und zu (aria-expanded, aria-controls; Enter und Leertaste trägt der
/// echte Knopf), „Neu", „Feiertage laden" und ein Einzeltag aus dem Jahresraster klappen auf, Bearbeiten und Entfernen
/// lassen offen. Über dem echten Weg der Hülle ohne Datenbank wie <see cref="KalenderbedienungAbschnittTests"/>.
/// </summary>
public class EinzeltageKlappgruppeTests : EposBunitContext
{
    public EinzeltageKlappgruppeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const KonditionierungGroesse H = KonditionierungGroesse.Heizen;

    private IRenderedComponent<KonditionierungReiter> Aufbauen()
    {
        GebaeudeArbeitsstand arbeit = new();
        arbeit.Laden(KalenderkarteTests.Satz(), neu: false);
        KonditionierungWeg w = KalenderkarteTests.Weg(null);
        KonditionierungBearbeitung bearbeitung = new(arbeit, () => w) { Melden = (_, _) => { } };
        return Render<KonditionierungReiter>(p => p.Add(x => x.Bearbeitung, bearbeitung).Add(x => x.EntprellungMs, 0));
    }

    private static IElement Knopf(IElement wurzel, string klasse)
        => wurzel.QuerySelector("button." + klasse) ?? throw new InvalidOperationException("Kein Knopf ." + klasse);

    /// <summary>Legt den Kalender an und klappt die Karte auf; liefert die Bedienung.</summary>
    private static IRenderedComponent<KalenderbedienungAbschnitt> Bedienung(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g)
    {
        Knopf(KalenderkarteTests.Karte(cut, g), "epos-kond-anlegen").Click();
        Knopf(KalenderkarteTests.Karte(cut, g), "epos-kond-einzelheiten").Click();
        return cut.FindComponents<KalenderbedienungAbschnitt>().Single();
    }

    private static IElement Wurzel(IRenderedComponent<KalenderbedienungAbschnitt> b) => b.Find(".epos-kalb");

    private static void EinzeltagAnlegen(IRenderedComponent<KalenderbedienungAbschnitt> b, string datum, string name)
    {
        Knopf(Wurzel(b), "epos-kalb-einzeltag-neu").Click();
        b.Find(".epos-kalb-einzeltage .epos-kalb-editor input[type=date]").Change(datum);
        b.Find(".epos-kalb-einzeltage .epos-kalb-editor input:not([type=date])").Input(name);
        Knopf(b.Find(".epos-kalb-einzeltage .epos-kalb-editor"), "epos-kalb-uebernehmen").Click();
    }

    /// <summary>Die Kopfzeile der Einzeltage: der Klappknopf in der Überschrift.</summary>
    private static IElement EinzeltageKopf(IRenderedComponent<KalenderbedienungAbschnitt> b)
        => b.Find(".epos-kalb-einzeltage .epos-klappgruppe-knopf");

    private static int Einzeltage(IRenderedComponent<KalenderbedienungAbschnitt> b)
        => b.Instance.Ansicht!.Zuordnungen.Count(x => x.IstEinzeltag);

    [Fact]
    public void Die_Einzeltage_stehen_beim_Oeffnen_zugeklappt_mit_Anzahl_im_Kopf()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        Assert.Equal(0, Einzeltage(b));

        IElement kopf = EinzeltageKopf(b);
        Assert.Equal("button", kopf.TagName.ToLowerInvariant());
        Assert.Equal("button", kopf.GetAttribute("type"));
        Assert.Equal("H4", kopf.ParentElement!.TagName);
        Assert.Contains("epos-kond-abschnitt-titel", kopf.ParentElement!.ClassList);
        Assert.Equal("false", kopf.GetAttribute("aria-expanded"));
        Assert.False(string.IsNullOrEmpty(kopf.GetAttribute("aria-controls")));
        Assert.Contains("Einzeltage (Feiertage, Ausnahmen)", kopf.TextContent);
        Assert.Equal("0 Tage", b.Find(".epos-kalb-einzeltage .epos-klappgruppe-zusatz").TextContent);
        Assert.False(b.Instance.EinzeltageOffen);
        Assert.Empty(b.FindAll(".epos-kalb-tage"));
        Assert.Empty(b.FindAll(".epos-kalb-einzeltage .epos-klappgruppe-inhalt"));

        // Die Handlungen der Kopfzeile stehen auch zugeklappt da.
        Assert.NotNull(b.Find(".epos-kalb-einzeltage .epos-klappgruppe-handlungen button.epos-kalb-einzeltag-neu"));
        Assert.NotNull(b.Find(".epos-kalb-einzeltage .epos-klappgruppe-handlungen button.epos-kalb-feiertage-laden"));
    }

    [Fact]
    public void Ein_Klick_auf_die_Kopfzeile_klappt_die_Einzeltage_auf_und_wieder_zu()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);

        EinzeltageKopf(b).Click();
        IElement kopf = EinzeltageKopf(b);
        Assert.Equal("true", kopf.GetAttribute("aria-expanded"));
        IElement inhalt = b.Find(".epos-kalb-einzeltage .epos-klappgruppe-inhalt");
        Assert.Equal(kopf.GetAttribute("aria-controls"), inhalt.Id);
        Assert.NotNull(inhalt.QuerySelector("table.epos-kalb-tage"));
        Assert.True(b.Instance.EinzeltageOffen);

        EinzeltageKopf(b).Click();
        Assert.Equal("false", EinzeltageKopf(b).GetAttribute("aria-expanded"));
        Assert.Empty(b.FindAll(".epos-kalb-tage"));
    }

    [Fact]
    public void Anlegen_klappt_auf_und_die_Anzahl_folgt_Anlegen_und_Entfernen()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);

        Knopf(Wurzel(b), "epos-kalb-einzeltag-neu").Click();
        Assert.True(b.Instance.EinzeltageOffen);
        Assert.Equal("true", EinzeltageKopf(b).GetAttribute("aria-expanded"));
        Assert.NotNull(b.Find(".epos-kalb-einzeltage .epos-klappgruppe-inhalt .epos-kalb-editor"));

        EinzeltagAnlegen(b, "2025-06-02", "Betriebsausflug");
        Assert.Equal(1, Einzeltage(b));
        Assert.Equal("1 Tag", b.Find(".epos-kalb-einzeltage .epos-klappgruppe-zusatz").TextContent);
        Assert.NotNull(b.Find(".epos-kalb-tage tr[data-name='Betriebsausflug']"));

        // Bearbeiten lässt die Gruppe offen.
        Knopf(b.Find(".epos-kalb-tage tr[data-name='Betriebsausflug']"), "epos-kalb-bearbeiten").Click();
        Assert.True(b.Instance.EinzeltageOffen);
        Assert.NotNull(b.Find(".epos-kalb-einzeltage .epos-kalb-editor"));

        // Entfernen senkt die Anzahl, die Gruppe bleibt offen.
        Knopf(b.Find(".epos-kalb-tage tr[data-name='Betriebsausflug']"), "epos-kalb-entfernen").Click();
        Assert.Equal(0, Einzeltage(b));
        Assert.Equal("0 Tage", b.Find(".epos-kalb-einzeltage .epos-klappgruppe-zusatz").TextContent);
        Assert.True(b.Instance.EinzeltageOffen);
        Assert.NotNull(b.Find(".epos-kalb-tage"));
    }

    [Fact]
    public void Feiertage_laden_klappt_auf_und_nennt_neun_Tage()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        Knopf(Wurzel(b), "epos-kalb-feiertage-laden").Click();
        Assert.True(b.Instance.EinzeltageOffen);
        Assert.Equal("9 Tage", b.Find(".epos-kalb-einzeltage .epos-klappgruppe-zusatz").TextContent);
        Assert.Equal(9, b.FindAll(".epos-kalb-tage tbody tr").Count);

        // Zuklappen hält die Anzahl im Kopf.
        EinzeltageKopf(b).Click();
        Assert.Empty(b.FindAll(".epos-kalb-tage"));
        Assert.Equal("9 Tage", b.Find(".epos-kalb-einzeltage .epos-klappgruppe-zusatz").TextContent);
    }

    [Fact]
    public void Ein_Einzeltag_aus_dem_Jahresraster_klappt_die_Einzeltage_auf()
    {
        IRenderedComponent<KonditionierungReiter> cut = Aufbauen();
        IRenderedComponent<KalenderbedienungAbschnitt> b = Bedienung(cut, H);
        EinzeltagAnlegen(b, "2025-06-02", "Betriebsausflug");
        EinzeltageKopf(b).Click();
        Assert.False(b.Instance.EinzeltageOffen);

        b.Find(".epos-kalb-jahrtag[data-tag='153']").Click();
        Assert.True(b.Instance.EinzeltageOffen);
        Assert.NotNull(b.Find(".epos-kalb-einzeltage .epos-kalb-editor"));
    }
}

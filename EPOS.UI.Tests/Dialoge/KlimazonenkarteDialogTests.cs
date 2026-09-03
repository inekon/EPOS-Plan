using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// KlimazonenkarteDialog (iU9-W10a.2) - der Ersatz fuer Form_Klimazonenkarte samt
/// dem Steuerelement KlimazonenKarte.
///
/// <para>FELDBESTAND laut Feldkarte: vier Steuerelemente - die Karte, eine
/// Statuszeile, OK und Abbrechen. Dazu NEU der Hilfeknopf (A-2).</para>
/// </summary>
public class KlimazonenkarteDialogTests : BunitContext
{
    /// <summary>
    /// Der Zonentext traegt die Volllaststunden mit Tausenderpunkt ("2.000 h/a") -
    /// das ist CurrentCulture, nicht die Oberflaechensprache. Der Windows-Laeufer
    /// steht auf en-US und schriebe "2,000"; die Faelle legen deshalb beide Kulturen
    /// fest (Regel seit W8).
    /// </summary>
    public KlimazonenkarteDialogTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
    }

    /// <summary>
    /// Drei Zonen genuegen fuer alle Faelle; die Volllaststunden sind die echten
    /// Werte der Zonen 1, 3 und 8 aus VDI4640Pruefung.
    /// </summary>
    private static IReadOnlyList<KlimazonenkarteDialog.Zone> Zonen() => new[]
    {
        new KlimazonenkarteDialog.Zone(1, "M0 0 L10 0 L10 10 Z", 1650),
        new KlimazonenkarteDialog.Zone(3, "M20 0 L30 0 L30 10 Z", 1650),
        new KlimazonenkarteDialog.Zone(8, "M40 0 L50 0 L50 10 Z", 2000)
    };

    private IRenderedComponent<KlimazonenkarteDialog> Zeige(
        int aktuelleZone,
        Action<int?>? geschlossen = null,
        IReadOnlyList<KlimazonenkarteDialog.Zone>? zonen = null)
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        return Render<KlimazonenkarteDialog>(p =>
        {
            p.Add(x => x.AktuelleZone, aktuelleZone);
            p.Add(x => x.Zonen, zonen ?? Zonen());
            p.Add(x => x.ViewBox, "0 0 1303.65 1349.50");
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });
    }

    // ================================================================== Feldbestand

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_vollstaendig()
    {
        var cut = Zeige(0);

        Assert.Equal("Klimazonen nach DIN 4710", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.NotNull(cut.Find("img.epos-bildkarte-bild"));
        Assert.NotNull(cut.Find("p.epos-kartenauswahl"));
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);
        Assert.NotNull(cut.Find("button.epos-infoknopf"));      // NEU, A-2
    }

    [Fact]
    public void Jede_Zone_wird_eine_Klickflaeche_mit_Kurztext()
    {
        var cut = Zeige(0);
        var pfade = cut.FindAll("path.epos-bildkarte-flaeche");

        Assert.Equal(3, pfade.Count);
        Assert.Equal("Zone 1 — 1.650 h/a", pfade[0].GetAttribute("aria-label"));
        Assert.Equal("Zone 8 — 2.000 h/a", pfade[2].GetAttribute("aria-label"));
    }

    // ================================================================== Statuszeile

    [Fact]
    public void Ohne_Auswahl_steht_die_Aufforderung()
    {
        var cut = Zeige(0);

        Assert.Equal("Noch keine Zone gewählt — eine Zonenfläche auf der Karte anklicken.",
                     cut.Find("p.epos-kartenauswahl").TextContent);
        Assert.Equal(0, cut.Instance.GewaehlteZone);
    }

    /// <summary>
    /// Die Statuszeile traegt denselben Zonentext wie die Auswahlliste des
    /// Erdreich-Dialogs: Nummer, Gedankenstrich, Volllaststunden (ZonenText:65-69).
    /// </summary>
    [Fact]
    public void Die_Vorauswahl_steht_in_der_Statuszeile()
    {
        var cut = Zeige(8);

        Assert.Equal(8, cut.Instance.GewaehlteZone);
        Assert.Equal("Gewählte Zone: 8 — 2.000 h/a",
                     cut.Find("p.epos-kartenauswahl").TextContent);
    }

    /// <summary>
    /// Zone 0 und alles, was die Karte nicht kennt, heisst "keine Auswahl" - die
    /// Karte kennt "nicht zugeordnet" nicht (Befund W10-B4).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]     // in dieser Probe nicht besetzt
    [InlineData(99)]
    public void Eine_unbekannte_Vorauswahl_gilt_als_keine(int zone)
    {
        Assert.Equal(0, Zeige(zone).Instance.GewaehlteZone);
    }

    [Fact]
    public void Der_Klick_setzt_Auswahl_und_Statuszeile()
    {
        var cut = Zeige(0);

        cut.FindAll("path.epos-bildkarte-flaeche")[1].Click();

        Assert.Equal(3, cut.Instance.GewaehlteZone);
        Assert.Equal("Gewählte Zone: 3 — 1.650 h/a",
                     cut.Find("p.epos-kartenauswahl").TextContent);
    }

    // ===================================================================== Ergebnis

    [Fact]
    public void Der_Doppelklick_uebernimmt_sofort()
    {
        int? ergebnis = null;
        bool gerufen = false;

        var cut = Zeige(0, z => { ergebnis = z; gerufen = true; });
        cut.FindAll("path.epos-bildkarte-flaeche")[2].DoubleClick();

        Assert.True(gerufen);
        Assert.Equal(8, ergebnis);
    }

    [Fact]
    public void OK_liefert_die_gewaehlte_Zone()
    {
        int? ergebnis = null;
        var cut = Zeige(1, z => ergebnis = z);

        cut.Find("button.epos-knopf--primaer").Click();

        Assert.Equal(1, ergebnis);
    }

    /// <summary>
    /// OHNE Auswahl heisst OK dasselbe wie Abbrechen: null. Der Aufrufer nahm das
    /// Ergebnis ohnehin nur bei 1 &lt;= Zone &lt;= 15 an (Form_QuelleErdreich:1084).
    /// </summary>
    [Fact]
    public void OK_ohne_Auswahl_liefert_null()
    {
        int? ergebnis = 7;
        bool gerufen = false;

        var cut = Zeige(0, z => { ergebnis = z; gerufen = true; });
        cut.Find("button.epos-knopf--primaer").Click();

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Abbrechen_und_Esc_liefern_null()
    {
        int? ergebnis = 7;
        var cut = Zeige(3, z => ergebnis = z);

        cut.FindAll(".epos-leiste button")[0].Click();
        Assert.Null(ergebnis);

        ergebnis = 7;
        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Ohne Zonen steht die LADEFEHLERZEILE ueber dem Bild, und die Auswahl bleibt
    /// ueber die Liste des Erdreich-Dialogs moeglich - woertlich das Verhalten des
    /// Vorlaeufers, wenn die Zuordnung nicht zustande kam (Befund W10-B4/B5).
    /// </summary>
    [Fact]
    public void Ohne_Zonen_steht_die_Ladefehlerzeile()
    {
        var cut = Zeige(0, null, Array.Empty<KlimazonenkarteDialog.Zone>());

        Assert.Empty(cut.FindAll("path.epos-bildkarte-flaeche"));
        Assert.Contains("die Auswahl bleibt über die Liste möglich",
                        cut.Find("p.epos-bildkarte-ladefehler").TextContent);
        Assert.NotNull(cut.Find("img.epos-bildkarte-bild"));
    }
}

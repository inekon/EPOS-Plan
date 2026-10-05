using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gruppe „Betriebszeiten" im Erzeugerdialog</b> (Anlagenkopplung 9.3, AK2-3): Zeitprogramm über das
/// Wochenraster (leer = immer verfügbar), höchster Vorlauf mit Vorgabe aus dem projektierten Vorlauf, Vorrang der
/// Sperrzeit, die Prüfregel über den Leser des Kerns (167 Werte sind ein benannter Fehler) und die Abbildung der
/// Hülle, NULL-erhaltend.
/// </summary>
public class WaermepumpeBetriebszeitenTests : EposBunitContext
{
    public WaermepumpeBetriebszeitenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static WaermepumpeAnlageDaten Daten(string? zeitprogramm = null, double? vorlaufMax = null) => new()
    {
        Bezeichner = "WP Alpha",
        Vorlauf = 55,
        Ruecklauf = 45,
        SperrzeitVon = 11,
        SperrzeitBis = 14,
        Abschaltpunkt = -5,
        CarrierId = 60,
        Zeitprogramm = zeitprogramm,
        VorlaufMax = vorlaufMax
    };

    private static string Programm(int werte, double faktor = 1.0)
    {
        var w = Enumerable.Repeat(faktor, Anlagenzeitprogramm.WOCHENSTUNDEN).ToArray();
        string text = AnlagenkopplungSchema.WochenprofilSchreiben(w);
        if (werte == Anlagenzeitprogramm.WOCHENSTUNDEN) return text;
        string[] teile = text.Split(';');
        return string.Join(";", teile.Take(werte));
    }

    private IRenderedComponent<WaermepumpeKonfiguration> Aufbauen(WaermepumpeAnlageDaten d, Action? geaendert = null)
        => Render<WaermepumpeKonfiguration>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Traegerkatalog, Array.Empty<EPOS.UI.Bausteine.EnergietraegerWahl.Eintrag>())
            .Add(x => x.Aktiv, true)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    [Fact]
    public void Die_Gruppe_zeigt_Zeitprogramm_Vorgabe_und_Vorrang_aber_keine_Nutzungszeit()
    {
        var t = new WaermepumpeKonfigurationTexte();
        var c = Aufbauen(Daten());
        var gruppe = c.Find("[data-gruppe=betriebszeiten]");
        Assert.Contains(t.ZeileZeitprogrammLeer, gruppe.TextContent);
        Assert.Contains("Vorgabe: projektierter Vorlauf 55 °C", gruppe.TextContent);
        Assert.Contains(t.HinweisSperrzeitVorrang, gruppe.TextContent);
        Assert.Contains(t.GruppeBetriebszeiten, c.Markup);
        Assert.DoesNotContain("Nutzungs", gruppe.TextContent);
        // Leer = Vorgabe: Das Feld ist leer, der Platzhalter zeigt den projektierten Vorlauf.
        Assert.Contains(c.FindAll("input"), i => i.GetAttribute("placeholder") == "55");
        // Kein Raster, solange der Knopf nicht gedrückt ist.
        Assert.Empty(c.FindAll("[data-kennung=wp-zeitprogramm]"));
    }

    [Fact]
    public void Der_Knopf_oeffnet_das_Wochenraster_und_laesst_das_Programm_leer()
    {
        var d = Daten();
        var c = Aufbauen(d);
        c.Find("[data-aktion=zeitprogramm]").Click();
        Assert.NotNull(c.Find("[data-kennung=wp-zeitprogramm]"));
        Assert.Contains(new WaermepumpeKonfigurationTexte().KnopfZeitprogrammSchliessen, c.Markup);
        // Öffnen allein schreibt nichts: leer bleibt leer (immer verfügbar).
        Assert.Null(d.Zeitprogramm);
    }

    [Fact]
    public void Ein_gepflegtes_Programm_nennt_volle_und_gesperrte_Stunden()
    {
        var w = Enumerable.Repeat(1.0, 168).ToArray();
        for (int h = 0; h < 6; h++) w[h] = 0.0;
        w[10] = 0.5;
        var d = Daten(AnlagenkopplungSchema.WochenprofilSchreiben(w));
        Assert.Null(WaermepumpeKonfiguration.BetriebszeitenFehler(d, null));
        Assert.Equal("Gepflegt: 161 von 168 Wochenstunden mit voller Verfügbarkeit, 6 gesperrt.",
                     WaermepumpeKonfiguration.ZeitprogrammZeile(d, new WaermepumpeKonfigurationTexte()));
    }

    [Fact]
    public void Ein_Programm_mit_167_Werten_ist_ein_benannter_Fehler_und_bleibt_stehen()
    {
        string text = Programm(167);
        var d = Daten(text);
        string? fehler = WaermepumpeKonfiguration.BetriebszeitenFehler(d, null);
        Assert.NotNull(fehler);
        Assert.Contains("167", fehler);
        Assert.Contains("168", fehler);

        var c = Aufbauen(d);
        Assert.Contains("167", c.Find("[data-gruppe=betriebszeiten]").TextContent);
        // Kein Auffüllen: der Text bleibt, wie er gelesen wurde.
        Assert.Equal(text, d.Zeitprogramm);
    }

    [Fact]
    public void Ein_Faktor_ueber_eins_und_ein_Vorlauf_ausserhalb_der_Grenzen_sind_Fehler()
    {
        Assert.NotNull(WaermepumpeKonfiguration.BetriebszeitenFehler(Daten(Programm(168, 1.5)), null));
        string? v = WaermepumpeKonfiguration.BetriebszeitenFehler(Daten(vorlaufMax: 150.0), null);
        Assert.Equal("Der höchste Vorlauf muss zwischen 20 und 120 °C liegen.", v);
        Assert.Null(WaermepumpeKonfiguration.BetriebszeitenFehler(Daten(vorlaufMax: 50.0), null));
    }

    [Fact]
    public void Sperrzeit_und_Zeitprogramm_ueberschneiden_sich_die_Sperrzeit_gilt()
    {
        var d = Daten(Programm(168));
        d.Sperrung = true;                       // Altfenster 11 bis 14 Uhr, täglich
        Assert.Equal(21, WaermepumpeKonfiguration.UeberschneidungSperrzeit(d));
        d.Sperrfenster = new() { new() { VonH = 17, DauerH = 2, Wochentage = 1 } };   // nur montags
        Assert.Equal(2, WaermepumpeKonfiguration.UeberschneidungSperrzeit(d));
        Assert.Equal(0, WaermepumpeKonfiguration.UeberschneidungSperrzeit(Daten()));
    }

    // ---- Hülle (EPOS.UI.Daten): NULL-erhaltend ------------------------------------------

    [Fact]
    public void Die_Abbildung_traegt_beide_Felder_NULL_erhaltend()
    {
        var leer = new WErzeugerModel();
        var d = new WaermepumpeAnlageDaten { Zeitprogramm = "alt", VorlaufMax = 1 };
        BetriebszeitenAbbildung.Lesen(leer, d);
        Assert.Null(d.Zeitprogramm);
        Assert.Null(d.VorlaufMax);

        var m = new WErzeugerModel { Zeitprogramm = "1;1", Vorlauf_Max = 48.5 };
        BetriebszeitenAbbildung.Lesen(m, d);
        Assert.Equal("1;1", d.Zeitprogramm);                 // ein ungültiger Text bleibt stehen
        Assert.Equal(48.5, d.VorlaufMax);

        d.Zeitprogramm = "  ";
        d.VorlaufMax = null;
        BetriebszeitenAbbildung.Schreiben(d, m);
        Assert.Null(m.Zeitprogramm);
        Assert.Null(m.Vorlauf_Max);

        // Die Kopie des Feldsatzes trägt beide mit.
        var k = new WaermepumpeAnlageDaten { Zeitprogramm = "x", VorlaufMax = 42 }.Kopie();
        Assert.Equal("x", k.Zeitprogramm);
        Assert.Equal(42.0, k.VorlaufMax);
    }
}

using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// Farbfeld (Auftrag DF-1) — der erste Farbwähler des Hauses: Systemwähler,
/// beschreibbares Hexfeld und das Muster der Hausfarbe in einer Zeile.
///
/// <para>Geprüft wird, was das Feld tragfähig macht: es meldet <b>nur gültige</b>
/// Farben nach außen (der Arbeitsstand eines Dialogs bleibt damit jederzeit eine
/// Farbe), eine Fehleingabe bleibt sichtbar stehen und sagt den Grund, ein neuer
/// Arbeitsstand verwirft sie, und die Beschriftung gehört über <c>for</c> zum
/// Wähler.</para>
///
/// <para>Die Kultur ist gepinnt (<see cref="EposBunitContext"/>): Die Meldung ist
/// ein deutscher Ressourcentext.</para>
/// </summary>
public class FarbfeldTests : EposBunitContext
{
    private IRenderedComponent<Farbfeld> Zeige(
        string wert = "#4172C4",
        string vorgabe = "#4172C4",
        Action<string>? gesetzt = null,
        int stand = 0,
        bool aktiv = true)
    {
        return Render<Farbfeld>(p => p
            .Add(x => x.Bezeichnung, "Wärmepumpe")
            .Add(x => x.Kennung, "epos-farbe-WAERME_WP")
            .Add(x => x.Wert, wert)
            .Add(x => x.Vorgabe, vorgabe)
            .Add(x => x.Stand, stand)
            .Add(x => x.Aktiv, aktiv)
            .Add(x => x.WertChanged, (string w) => gesetzt?.Invoke(w)));
    }

    // =====================================================================
    //  Aufbau
    // =====================================================================

    /// <summary>
    /// Vier Stücke in einer Zeile — und die Beschriftung gehört über <c>for</c> zum
    /// WÄHLER, nicht zum Hexfeld: Zwei Felder derselben Zeile bekämen sonst dieselbe
    /// Ansage.
    /// </summary>
    [Fact]
    public void Die_Zeile_traegt_Beschriftung_Waehler_Hexfeld_und_Vorgabemuster()
    {
        var cut = Zeige(vorgabe: "#70AD47");

        Assert.Equal("epos-farbe-WAERME_WP",
                     cut.Find(".epos-farbfeld-name").GetAttribute("for"));
        Assert.Equal("Wärmepumpe", cut.Find(".epos-farbfeld-name").TextContent.Trim());

        Assert.Equal("#4172C4", cut.Find("input[type=color]").GetAttribute("value"));
        Assert.Equal("#4172C4", cut.Find(".epos-farbfeld-hex").GetAttribute("value"));

        // Das Muster zeigt die HAUSFARBE, nicht den eingestellten Wert.
        Assert.Contains("#70AD47", cut.Find(".epos-farbfeld-vorgabe").GetAttribute("style"));

        // Es ist eine Anzeige und keine Ansage: aria-hidden.
        Assert.Equal("true", cut.Find(".epos-farbfeld-vorgabe").GetAttribute("aria-hidden"));
    }

    /// <summary>Das Hexfeld trägt ein eigenes aria-label und meldet seine Gültigkeit.</summary>
    [Fact]
    public void Das_Hexfeld_ist_eigenstaendig_beschriftet()
    {
        var cut = Zeige();
        var hex = cut.Find(".epos-farbfeld-hex");

        Assert.Contains("Wärmepumpe", hex.GetAttribute("aria-label"));
        Assert.Contains("Farbwert", hex.GetAttribute("aria-label"));
        Assert.Equal("false", hex.GetAttribute("aria-invalid"));
    }

    [Fact]
    public void Gesperrt_nimmt_keine_Eingabe_an()
    {
        var cut = Zeige(aktiv: false);

        Assert.True(cut.Find("input[type=color]").HasAttribute("disabled"));
        Assert.True(cut.Find(".epos-farbfeld-hex").HasAttribute("disabled"));
    }

    // =====================================================================
    //  Der Wähler
    // =====================================================================

    /// <summary>
    /// Der Systemwähler liefert Kleinbuchstaben; gemeldet wird die Hausschreibweise
    /// in Großbuchstaben — sonst stünde dieselbe Farbe je nach Bedienweg anders im
    /// Einstellungstext.
    /// </summary>
    [Fact]
    public void Der_Waehler_meldet_die_Farbe_in_Grossbuchstaben()
    {
        string? erhalten = null;
        var cut = Zeige(gesetzt: w => erhalten = w);

        cut.Find("input[type=color]").Change("#ff8000");

        Assert.Equal("#FF8000", erhalten);
        Assert.False(cut.Instance.Ungueltig);
    }

    // =====================================================================
    //  Das Hexfeld
    // =====================================================================

    [Fact]
    public void Ein_gueltiges_Hex_wird_uebernommen()
    {
        string? erhalten = null;
        var cut = Zeige(gesetzt: w => erhalten = w);

        cut.Find(".epos-farbfeld-hex").Change("#00a000");

        Assert.Equal("#00A000", erhalten);
        Assert.False(cut.Instance.Ungueltig);
        Assert.Empty(cut.FindAll(".epos-farbfeld-meldung"));
    }

    /// <summary>
    /// <b>Ungültiges wird benannt abgewiesen und NICHT gemeldet.</b> Die Eingabe
    /// bleibt sichtbar stehen — der Anwender soll lesen, was er getippt hat —, und
    /// der Grund steht als Text darunter, nicht nur als Farbe.
    /// </summary>
    [Theory]
    [InlineData("rot")]
    [InlineData("#F00")]
    [InlineData("4172C4")]
    [InlineData("#4172C4FF")]
    public void Ein_ungueltiges_Hex_wird_abgewiesen(string eingabe)
    {
        string? erhalten = null;
        var cut = Zeige(gesetzt: w => erhalten = w);

        cut.Find(".epos-farbfeld-hex").Change(eingabe);

        Assert.Null(erhalten);
        Assert.True(cut.Instance.Ungueltig);
        Assert.Equal(eingabe, cut.Find(".epos-farbfeld-hex").GetAttribute("value"));
        Assert.Equal("true", cut.Find(".epos-farbfeld-hex").GetAttribute("aria-invalid"));
        Assert.Contains("epos-farbfeld--ungueltig", cut.Find(".epos-farbfeld").ClassName);
        Assert.Contains("#RRGGBB", cut.Find(".epos-farbfeld-meldung").TextContent);
    }

    /// <summary>Eine gültige Eingabe nach einer Fehleingabe räumt die Meldung weg.</summary>
    [Fact]
    public void Eine_gueltige_Eingabe_hebt_die_Meldung_auf()
    {
        var cut = Zeige();

        cut.Find(".epos-farbfeld-hex").Change("rot");
        Assert.True(cut.Instance.Ungueltig);

        cut.Find(".epos-farbfeld-hex").Change("#112233");
        Assert.False(cut.Instance.Ungueltig);
        Assert.Empty(cut.FindAll(".epos-farbfeld-meldung"));
    }

    // =====================================================================
    //  Der Arbeitsstand
    // =====================================================================

    /// <summary>
    /// Erhöht der Dialog den Arbeitsstand — „Hausfarben", „Standardwerte" —, verwirft
    /// das Feld eine stehengebliebene Fehleingabe und zeigt wieder den Wert. Das gilt
    /// AUCH dann, wenn der Wert derselbe bleibt; sonst behauptete die Zeile einen
    /// Fehler, den es nicht mehr gibt.
    /// </summary>
    [Fact]
    public void Ein_neuer_Arbeitsstand_verwirft_die_Fehleingabe()
    {
        var cut = Zeige();

        cut.Find(".epos-farbfeld-hex").Change("rot");
        Assert.True(cut.Instance.Ungueltig);

        cut.Render(p => p.Add(x => x.Stand, 1));

        Assert.False(cut.Instance.Ungueltig);
        Assert.Equal("#4172C4", cut.Find(".epos-farbfeld-hex").GetAttribute("value"));
    }

    /// <summary>Ohne neuen Arbeitsstand bleibt die Fehleingabe sichtbar.</summary>
    [Fact]
    public void Ohne_neuen_Arbeitsstand_bleibt_die_Fehleingabe_stehen()
    {
        var cut = Zeige();

        cut.Find(".epos-farbfeld-hex").Change("rot");
        cut.Render(p => p.Add(x => x.Bezeichnung, "Wärmepumpe"));

        Assert.True(cut.Instance.Ungueltig);
        Assert.Equal("rot", cut.Find(".epos-farbfeld-hex").GetAttribute("value"));
    }

    /// <summary>Ein von außen gesetzter Wert steht in beiden Feldern.</summary>
    [Fact]
    public void Ein_von_aussen_gesetzter_Wert_steht_in_beiden_Feldern()
    {
        var cut = Zeige();

        cut.Render(p => p.Add(x => x.Wert, "#ABCDEF"));

        Assert.Equal("#ABCDEF", cut.Find("input[type=color]").GetAttribute("value"));
        Assert.Equal("#ABCDEF", cut.Find(".epos-farbfeld-hex").GetAttribute("value"));
    }
}

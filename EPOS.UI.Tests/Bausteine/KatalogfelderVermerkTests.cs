using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// ETAPPE E10 — <b>Kennzeichnung A8</b> (Empfehlung E10‑Q4 a): Der Vermerk des
/// Katalogprofils (<c>BrowserDetailfeld.Hinweis</c> → <see cref="BrowserFeldwert.Hinweis"/>)
/// steht als Tooltip an der Beschriftung des Feldes — gleich ob es als Zahlen-, Ganzzahl-
/// oder (nur lesend) als Textfeld erscheint. Ein Feld ohne Vermerk trägt kein
/// <c>title</c>. Die Beschriftung selbst nennt die Gerätedaten; der Tooltip ergänzt
/// sie nur, denn auf einem Touchgerät erscheint er nicht.
/// </summary>
public class KatalogfelderVermerkTests : BunitContext
{
    private const string VERMERK = "Gerätedaten — nicht rechenwirksam";

    private static List<BrowserFeldwert> Felder() => new()
    {
        new BrowserFeldwert
        {
            Schluessel = "NUTZUNGSDAUER", Bezeichnung = "Nutzungsdauer (Gerätedaten):", Einheit = "Jahre",
            Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "20", Hinweis = VERMERK
        },
        new BrowserFeldwert
        {
            Schluessel = "NUTZUNGSDAUER_BHKW", Bezeichnung = "Nutzungsdauer (Gerätedaten):", Einheit = "Jahre",
            Art = BrowserFeldArt.Ganzzahl, Editierbar = true, Wert = "15", Hinweis = VERMERK
        },
        new BrowserFeldwert
        {
            Schluessel = "RAUMBEDARF", Bezeichnung = "Raumbedarf:", Einheit = "m³",
            Art = BrowserFeldArt.Zahl, Editierbar = true, Wert = "2"
        }
    };

    [Fact]
    public void Der_Vermerk_steht_als_Tooltip_an_der_Beschriftung()
    {
        var cut = Render<Katalogfelder>(p => p.Add(x => x.Felder, Felder()));

        var beschriftungen = cut.FindAll("label.epos-feld");
        Assert.Equal(3, beschriftungen.Count);
        Assert.Equal(VERMERK, beschriftungen[0].GetAttribute("title"));
        Assert.Equal(VERMERK, beschriftungen[1].GetAttribute("title"));
        Assert.Null(beschriftungen[2].GetAttribute("title"));
    }

    [Fact]
    public void Auch_nur_lesend_traegt_die_Beschriftung_den_Vermerk()
    {
        var cut = Render<Katalogfelder>(p => p
            .Add(x => x.Felder, Felder())
            .Add(x => x.NurLesen, true));

        var beschriftungen = cut.FindAll("label.epos-feld");
        Assert.Equal(VERMERK, beschriftungen[0].GetAttribute("title"));
        Assert.Null(beschriftungen[2].GetAttribute("title"));
    }

    /// <summary>Die drei Standardfelder tragen den Parameter <c>Titel</c>; leer heißt kein Attribut.</summary>
    [Fact]
    public void Die_Standardfelder_setzen_den_Titel_nur_mit_Text()
    {
        Assert.Equal(VERMERK, Render<Zahlenfeld>(p => p.Add(x => x.Titel, VERMERK))
                                  .Find("label").GetAttribute("title"));
        Assert.Equal(VERMERK, Render<Ganzzahlfeld>(p => p.Add(x => x.Titel, VERMERK))
                                  .Find("label").GetAttribute("title"));
        Assert.Equal(VERMERK, Render<Textfeld>(p => p.Add(x => x.Titel, VERMERK))
                                  .Find("label").GetAttribute("title"));

        Assert.Null(Render<Zahlenfeld>().Find("label").GetAttribute("title"));
        Assert.Null(Render<Ganzzahlfeld>().Find("label").GetAttribute("title"));
        Assert.Null(Render<Textfeld>().Find("label").GetAttribute("title"));
    }
}

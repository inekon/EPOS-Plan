using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Der ganze Weg von <c>feld_setzen</c> für Testklassen außerhalb der Hilfe-Tests (Freigabe der
/// Masken, Teil C): prüfen, vorbereiten, freigeben, ausführen. Der Aufrufer stellt
/// <c>Schreibnaht.Schreibrecht</c> selbst und setzt es zurück.
/// </summary>
internal static class KiSetzweg
{
    private static IReadOnlyDictionary<string, object?> Werte(string maske, string feld, string wert)
        => new Dictionary<string, object?> { ["maske"] = maske, ["feld"] = feld, ["wert"] = wert };

    /// <summary>Nur die Vorbereitung: <c>Freigabe is null</c> heißt Absage VOR der Bestätigung.</summary>
    public static async Task<KiVorbereitung> Vorbereiten(string maske, string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen", Werte(maske, feld, wert));
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());
        return await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);
    }

    /// <summary>Vorbereiten, freigeben, ausführen — der ganze Weg einer Feldsetzung.</summary>
    public static async Task<KiErgebnis> Setzen(string maske, string feld, string wert)
    {
        var schicht = new KiAusfuehrung { Schreibrecht = () => true };
        KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, "feld_setzen", Werte(maske, feld, wert));
        Assert.True(geprueft.Gueltig, geprueft.FehlerText());

        KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(geprueft.Aufruf, CancellationToken.None);
        if (vorbereitung.Freigabe is null) return vorbereitung.Ablehnung;

        vorbereitung.Freigabe.Erteilen();
        return await schicht.AusfuehrenAsync(geprueft.Aufruf, vorbereitung.Freigabe, CancellationToken.None);
    }
}

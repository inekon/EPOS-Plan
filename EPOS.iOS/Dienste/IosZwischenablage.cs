using EPOS.UI.Dienste;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IZwischenablage"/> (Konzept Berichtsvorlagen 9.6, BV-E6): die
/// Zwischenablage des Geräts über <c>Clipboard.Default.SetTextAsync</c>, auf dem Hauptfaden.
///
/// <para><b>Scheitern ist benannt:</b> Wirft die Plattform, liefert der Adapter <c>false</c>, und
/// die Marke zeigt den Text markiert — der Anwender kopiert dann über das Menü des Systems.</para>
/// </summary>
public sealed class IosZwischenablage : IZwischenablage
{
    /// <inheritdoc />
    public async Task<bool> TextSetzenAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(text));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

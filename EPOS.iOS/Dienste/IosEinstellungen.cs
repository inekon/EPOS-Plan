using System.Globalization;
using Microsoft.Maui.Storage;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IEinstellungen"/>: <c>NSUserDefaults</c> ueber
/// die MAUI-Fassade <see cref="Preferences"/>.
///
/// <para><b>Das Gegenstueck zur Registry.</b> Unter Windows liegen dieselben
/// Werte in <c>HKCU\Software\wp-plan</c>. Der Praefix <c>wp-plan.</c> hier
/// erfuellt denselben Zweck wie der Registry-Zweig: Er haelt die Werte des
/// Programms von denen anderer Bestandteile getrennt - <c>NSUserDefaults</c>
/// ist ein flacher Namensraum je App.</para>
///
/// <para><b><see cref="LiesMaschine"/> liefert stets die Vorgabe.</b> Ein
/// maschinenweiter, vom Anwender nicht ueberschreibbarer Wert entspraeche auf
/// iOS einer verwalteten Einstellung per MDM
/// (<c>NSUserDefaults</c>-Domaene <c>com.apple.configuration.managed</c>). Die
/// einzige Fundstelle ist der maschinenweite KI-Abschalter; ihn anzuschliessen
/// gehoert zu iU11, nicht hierher. Bis dahin gilt „nicht abgeschaltet", also
/// der unauffaellige Zustand - dieselbe Antwort wie
/// <see cref="FluechtigeEinstellungen.LiesMaschine"/>.</para>
/// </summary>
public sealed class IosEinstellungen : IEinstellungen
{
    /// <summary>Namenspraefix aller Werte dieses Programms.</summary>
    private const string PRAEFIX = "wp-plan.";

    /// <inheritdoc/>
    public string? Lies(string schluessel, string? vorgabe = null)
    {
        if (string.IsNullOrEmpty(schluessel)) return vorgabe;

        try
        {
            string voll = PRAEFIX + schluessel;
            if (!Preferences.Default.ContainsKey(voll)) return vorgabe;
            return Preferences.Default.Get(voll, vorgabe ?? "");
        }
        catch
        {
            return vorgabe;
        }
    }

    /// <inheritdoc/>
    public int LiesZahl(string schluessel, int vorgabe = 0)
    {
        string? text = Lies(schluessel, null);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : vorgabe;
    }

    /// <inheritdoc/>
    public void Schreib(string schluessel, string wert)
    {
        if (string.IsNullOrEmpty(schluessel)) return;
        try { Preferences.Default.Set(PRAEFIX + schluessel, wert ?? ""); } catch { }
    }

    /// <inheritdoc/>
    public void SchreibZahl(string schluessel, int wert)
    {
        // Als TEXT, nicht als Zahl: Lies() und LiesZahl() muessen denselben
        // Wert sehen - genau wie unter Windows, wo ein DWord ueber
        // Convert.ToString gelesen wird.
        Schreib(schluessel, wert.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc/>
    public void Loesche(string schluessel)
    {
        if (string.IsNullOrEmpty(schluessel)) return;
        try { Preferences.Default.Remove(PRAEFIX + schluessel); } catch { }
    }

    /// <inheritdoc/>
    public string? LiesMaschine(string schluessel, string? vorgabe = null) => vorgabe;
}

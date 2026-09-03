namespace EPOS.iOS;

/// <summary>
/// Die MAUI-Anwendung. Sie hat genau ein Fenster und darin genau eine Seite -
/// mehr braucht eine Huelle nicht, deren Oberflaeche in Blazor liegt.
///
/// <para><b>CreateWindow statt MainPage.</b> <c>Application.MainPage</c> ist seit
/// .NET 9 abgekuendigt; der gestuetzte Weg ist das Ueberschreiben von
/// <see cref="CreateWindow"/>. Fachlich ist es dasselbe: ein Fenster, eine
/// Seite.</para>
/// </summary>
public sealed class App : Application
{
    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? aktivierung)
    {
        return new Window(new HauptSeite()) { Title = "EPOS-Plan" };
    }
}

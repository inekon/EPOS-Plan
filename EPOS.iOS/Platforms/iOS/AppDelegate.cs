using Foundation;

namespace EPOS.iOS;

/// <summary>
/// Die Anwendungsdelegate von iOS. Sie reicht den Aufbau an
/// <see cref="MauiProgram.CreateMauiApp"/> weiter - mehr steht hier mit
/// Absicht nicht: Was beim Start geschehen soll, gehoert an EINE Stelle, und
/// das ist MauiProgram.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

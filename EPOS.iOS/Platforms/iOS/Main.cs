using UIKit;

namespace EPOS.iOS;

/// <summary>
/// Der Einstiegspunkt des Prozesses - das iOS-Gegenstueck zu
/// <c>WindowsFormsApplication1.Program.Main</c>.
///
/// <para>Er tut nichts ausser <see cref="UIApplication.Main(string[], string, string)"/>
/// zu rufen; alles Weitere entscheidet <see cref="AppDelegate"/> und von dort
/// <see cref="MauiProgram"/>. Das ist der Aufbau, den die iOS-Laufzeit
/// verlangt.</para>
/// </summary>
public static class Start
{
    private static void Main(string[] argumente)
    {
        // Der dritte Parameter benennt die Delegatenklasse; sie traegt dafuer
        // ein [Register("AppDelegate")].
        UIApplication.Main(argumente, null, typeof(AppDelegate));
    }
}

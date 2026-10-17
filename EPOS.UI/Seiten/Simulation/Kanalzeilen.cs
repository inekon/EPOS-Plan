using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die „davon“-Zeilen einer Kanalzeile der Wärme in den Erzeugerreitern: Heizung und
/// Warmwasser immer, Prozesswärme nur, wenn der Erzeuger welche gedeckt hat — die
/// meisten Projekte führen keinen Prozesswärmebedarf.
/// </summary>
internal static class Kanalzeilen
{
    /// <summary>Beschriftung und Wert [MWh/a] je anzuzeigendem Kanal, in Kanalreihenfolge.</summary>
    public static IEnumerable<(string Text, double Wert)> Davon(double[]? kanalMwh)
    {
        double Wert(int k) => kanalMwh != null && k < kanalMwh.Length ? kanalMwh[k] : 0.0;

        yield return (Resource.SIMERG_LBL_DAVON_HEIZUNG, Wert(Kanal.HEIZUNG));
        yield return (Resource.SIMERG_LBL_DAVON_WARMWASSER, Wert(Kanal.BRAUCHWASSER));
        if (Wert(Kanal.PROZESS) > 0)
            yield return (Resource.SIMERG_LBL_DAVON_PROZESS, Wert(Kanal.PROZESS));
    }
}

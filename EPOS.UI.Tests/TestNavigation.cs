using WindowsFormsApplication1;

namespace EPOS.UI.Tests;

/// <summary>
/// Eine Navigation, die nichts öffnet und alles mitschreibt (Auftrag #199).
/// Gegenstück zu <see cref="TestHilfe"/> und <see cref="TestProjektquelle"/>.
/// </summary>
/// <remarks>
/// <c>Dienste.Navigation</c> ist PROZESSWEIT. Wer sie tauscht, gehört in die
/// Sammlung <c>KiDialogweg</c> und legt in <c>Dispose</c> die vorige Fassung zurück
/// — dieselbe Regel wie <c>[Collection("Testdatenbank")]</c> in
/// <c>EPOS.Kern.Tests</c> (Befund iU5‑O‑1).
/// </remarks>
public sealed class TestNavigation : INavigation
{
    /// <summary>Die Schlüssel in Aufrufreihenfolge.</summary>
    public List<string> Masken { get; } = new();

    /// <summary>Die Argumente des letzten Aufrufs; nie <c>null</c>.</summary>
    public object[] LetzteArgumente { get; private set; } = Array.Empty<object>();

    /// <summary>Der <see cref="KiAufrufkontext"/> des letzten Aufrufs; <c>null</c> = keiner.</summary>
    public KiAufrufkontext? LetzterKontext
        => LetzteArgumente.Length == 0 ? null : LetzteArgumente[0] as KiAufrufkontext;

    /// <summary>Was <see cref="OeffneMaske"/> zurückgeben soll.</summary>
    public bool Antwort { get; set; } = true;

    public bool OeffneMaske(string maske, params object[] argumente)
    {
        Masken.Add(maske);
        LetzteArgumente = argumente ?? Array.Empty<object>();
        return Antwort;
    }

    public void MenueAktualisieren() { }

    public void AnsichtAktualisieren(string bereich) { }
}

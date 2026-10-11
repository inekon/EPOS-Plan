using WindowsFormsApplication1;

namespace EPOS.UI.Tests;

/// <summary>
/// KT-7: Schreibt für die Dauer eines Tests die Vermerke des <see cref="Ausnahmeprotokoll"/> mit
/// (<c>using var vermerke = new Ausnahmevermerke();</c>) — für die Fälle „wirft → Banner und
/// Vermerk" in Testklassen, die das Protokoll nicht selbst abonnieren.
/// </summary>
public sealed class Ausnahmevermerke : IDisposable
{
    private readonly List<string> _zeilen = new();

    public Ausnahmevermerke() => Ausnahmeprotokoll.Vermerkt += Mitschreiben;

    private void Mitschreiben(string zeile)
    {
        lock (_zeilen) _zeilen.Add(zeile);
    }

    /// <summary>Ob ein Vermerk den Text enthält.</summary>
    public bool Enthaelt(string text)
    {
        lock (_zeilen) return _zeilen.Any(z => z.Contains(text, StringComparison.Ordinal));
    }

    /// <summary>Ob gar kein Vermerk steht.</summary>
    public bool Leer
    {
        get { lock (_zeilen) return _zeilen.Count == 0; }
    }

    public void Dispose() => Ausnahmeprotokoll.Vermerkt -= Mitschreiben;
}

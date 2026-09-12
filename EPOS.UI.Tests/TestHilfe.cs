using EPOS.UI.Dienste;

namespace EPOS.UI.Tests;

/// <summary>
/// Hilfedienst fuer die Tests: liefert einen festen Eintrag und merkt sich,
/// welcher Schluessel geoeffnet wurde.
/// </summary>
internal sealed class TestHilfe : IHilfeDienst
{
    private readonly HilfeEintrag? _eintrag;

    internal TestHilfe(HilfeEintrag? eintrag = null)
    {
        _eintrag = eintrag;
    }

    /// <summary>Die Schluessel, zu denen <see cref="Oeffnen"/> gerufen wurde.</summary>
    internal List<string> Geoeffnet { get; } = new();

    public HilfeEintrag? Aufloesen(string schluessel) => _eintrag;

    public void Oeffnen(string schluessel) => Geoeffnet.Add(schluessel);
}

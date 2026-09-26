using System.Globalization;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild des <see cref="LuftaustauschDialog"/> für den Hilfe-Assistenten (Gebäudesimulation
/// G6b, Welle W2): die Luftströme als RASTER (Sammlungsform <c>Luftstroeme[]</c>, Kennzeichen die
/// Nummer) — die beiden Zonen zum Lesen, der Volumenstrom setzbar. Zeilen anlegen und entfernen und die
/// Zonen wählen bleiben Klicks des Anwenders. Sie hält keinen Zustand: Jede Eigenschaft ruft bei jedem
/// Zugriff ihren Delegaten.
/// </summary>
public sealed class LuftaustauschKiSicht
{
    public Func<IReadOnlyList<ZonenluftstromDaten>>? StroemeLesen { get; init; }

    /// <summary>Der Name einer Zone zu ihrer Id im Arbeitsstand.</summary>
    public Func<int?, string>? Zonenname { get; init; }

    /// <summary>Die Luftströme — je Zugriff neu über dem Arbeitsstand.</summary>
    public IReadOnlyList<LuftstromKiZeile> Luftstroeme
    {
        get
        {
            IReadOnlyList<ZonenluftstromDaten> liste = StroemeLesen?.Invoke() ?? Array.Empty<ZonenluftstromDaten>();
            return liste.Select((l, i) => new LuftstromKiZeile(l, i, Zonenname)).ToList();
        }
    }
}

/// <summary>Ein Luftstrom im Raster des Assistenten — eine Sicht auf die Zeile des Arbeitsstands.</summary>
public sealed class LuftstromKiZeile
{
    private readonly ZonenluftstromDaten _l;
    private readonly int _index;
    private readonly Func<int?, string>? _zonenname;

    internal LuftstromKiZeile(ZonenluftstromDaten l, int index, Func<int?, string>? zonenname)
    {
        _l = l;
        _index = index;
        _zonenname = zonenname;
    }

    /// <summary>Die Nummer der Zeile, ab 1 — das Kennzeichen.</summary>
    public string Nummer => (_index + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>Die eine Zone (Name) — nur lesbar.</summary>
    public string ZoneA => _zonenname?.Invoke(_l.IdZoneA) ?? "";

    /// <summary>Die andere Zone (Name) — nur lesbar.</summary>
    public string ZoneB => _zonenname?.Invoke(_l.IdZoneB) ?? "";

    /// <summary>Der Volumenstrom V̇ [m³/h].</summary>
    public double? Volumenstrom { get => _l.Volumenstrom; set => _l.Volumenstrom = value; }
}

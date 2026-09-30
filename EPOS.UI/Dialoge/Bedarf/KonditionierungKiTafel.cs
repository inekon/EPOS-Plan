using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Vorgabe-Matrix als FELDTAFEL des Assistenten</b> (Stufe KP2, Wellen U1 und U4) — EIN Weg für
/// die Sichtklassen, die die Felder des Profils <c>KiKonditionierungsfelder</c> beantworten: der
/// Gebäude-Katalogeditor und die Gebäudeverwaltung (<see cref="GebaeudeKatalogKiSicht"/>, Karte
/// <c>Alle</c>) und der Zonendialog (<see cref="ZonenKiSicht"/>, Karte <c>Zonenfelder</c>). Gelesen und
/// gesetzt wird über die <see cref="KonditionierungBearbeitung"/> des Wirts — derselbe Weg wie die
/// Zellen der Matrix, mit Zellenort, Folgeregel und Rückfrage.
/// </summary>
/// <remarks>
/// Ein Zeitfenster hat nur beide Grenzen zusammen (E53, E43): Die erste gesetzte Grenze wartet in der
/// Tafel auf die zweite — erst beide oder keine gehen an den Weg, wie im Reiter. Das ist der einzige
/// Zustand der Tafel.
/// </remarks>
public sealed class KonditionierungKiTafel
{
    private readonly Func<string, KiKonditionierungsfelder.Feld?> _finde;

    /// <summary>Die halbe Angabe eines Zeitfensters (Nachtfenster, Saison), bis beide Grenzen stehen.</summary>
    private readonly Dictionary<string, (int? Von, int? Bis)> _halb = new(StringComparer.Ordinal);

    /// <param name="finde">Die Karte: <c>KiKonditionierungsfelder.Finde</c> bzw. <c>FindeZone</c>.</param>
    public KonditionierungKiTafel(Func<string, KiKonditionierungsfelder.Feld?> finde) => _finde = finde;

    /// <summary>Die Ablehnung einer Handlung — mit dem Grund, den der Reiter nennt.</summary>
    public static InvalidOperationException Ablehnung(KonditionierungBearbeitung b)
        => new(b.OffeneFrage?.Text is { Length: > 0 } frage ? frage
               : b.LetzteMeldung is { Length: > 0 } m ? m
               : b.Sperrgrund ?? b.Texte.GrundOhneTabellen);

    /// <summary>
    /// Liest ein Feld der Karte: der Wert der Zelle (leer = <c>null</c>; an einer Zone „wie Gebäude"),
    /// „aus", eine Grenze eines Zeitfensters oder ΔT; <c>null</c> ohne Bearbeitung oder ohne Feld.
    /// </summary>
    public object? Lesen(KonditionierungBearbeitung? b, string schluessel)
    {
        if (b is null || _finde(schluessel) is not KiKonditionierungsfelder.Feld f) return null;
        var g = (KonditionierungGroesse)f.Groessenplatz;
        var z = (KonditionierungZeile)f.Zeilenplatz;
        switch (f.Teil)
        {
            case KiKonditionierungsfelder.Teil.Aus:
                return b.Wert(g, z) is double a && double.IsNaN(a);
            case KiKonditionierungsfelder.Teil.Von:
                return _halb.TryGetValue(Fenster(schluessel), out var hv) ? hv.Von : b.Zeiten(g, z).Von;
            case KiKonditionierungsfelder.Teil.Bis:
                return _halb.TryGetValue(Fenster(schluessel), out var hb) ? hb.Bis : b.Zeiten(g, z).Bis;
            case KiKonditionierungsfelder.Teil.DeltaT:
                return b.DeltaT;
            default:
                return b.Wert(g, z) is double w && double.IsFinite(w) ? w : null;
        }
    }

    /// <summary>
    /// Setzt ein Feld der Karte über die Bearbeitung; eine abgelehnte Handlung wirft mit dem Grund des
    /// Reiters (<see cref="Ablehnung"/>), ohne Bearbeitung mit dem Grund „ohne Tabellen".
    /// </summary>
    public void Setzen(KonditionierungBearbeitung? b, string schluessel, object? wert)
    {
        if (_finde(schluessel) is not KiKonditionierungsfelder.Feld f)
            throw new InvalidOperationException(schluessel);
        if (b is null)
            throw new InvalidOperationException(new KonditionierungTexte().GrundOhneTabellen);
        var g = (KonditionierungGroesse)f.Groessenplatz;
        var z = (KonditionierungZeile)f.Zeilenplatz;
        bool ok;
        switch (f.Teil)
        {
            case KiKonditionierungsfelder.Teil.Aus:
                bool aus = wert is bool an && an;
                bool istAus = b.Wert(g, z) is double a && double.IsNaN(a);
                if (aus == istAus) return;
                ok = b.WertSetzen(g, z, aus ? double.NaN : null);
                break;
            case KiKonditionierungsfelder.Teil.Von:
            case KiKonditionierungsfelder.Teil.Bis:
                string fenster = Fenster(schluessel);
                (int? von, int? bis) = _halb.TryGetValue(fenster, out var halb) ? halb : b.Zeiten(g, z);
                int? neu = wert is int n ? n : null;
                if (f.Teil == KiKonditionierungsfelder.Teil.Von) von = neu; else bis = neu;
                if (von.HasValue != bis.HasValue)
                {
                    _halb[fenster] = (von, bis);
                    return;
                }
                _halb.Remove(fenster);
                ok = b.ZeitenSetzen(g, z, von, bis);
                break;
            case KiKonditionierungsfelder.Teil.DeltaT:
                ok = b.DeltaTSetzen(wert as double?);
                break;
            default:
                ok = b.WertSetzen(g, z, wert as double?);
                break;
        }
        if (!ok) throw Ablehnung(b);
    }

    private static string Fenster(string schluessel) => schluessel[..schluessel.LastIndexOf('_')];
}

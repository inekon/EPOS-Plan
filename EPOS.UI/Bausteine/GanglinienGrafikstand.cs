namespace EPOS.UI.Bausteine;

using WindowsFormsApplication1.Zeichnung;

/// <summary>
/// <b>Der Stand der Ganglinie in der Detailzeile</b> (DZ1, Konzept Projektdialoge mit
/// Katalogauswahl 4.9) — was ein Ganglinien-Dialog zur markierten Zeile hält: die Wahl,
/// ihre Kennzahlen und den Bildauftrag für <see cref="GanglinienGrafik"/>.
///
/// <para><b>Einmal je Markierung.</b> Die Kennzahlen fragt <see cref="LadenAsync"/> bei der
/// Hülle ab (dahinter liegen 8 760 bzw. 35 040 Wertzeilen); jedes Umschalten von
/// „sortiert" fragt nur noch den Bildauftrag. Ohne Delegat bleibt die Grafik weg.</para>
/// </summary>
public sealed class GanglinienGrafikstand
{
    /// <summary>Die Wahl, zu der die Kennzahlen gehören; <c>null</c> = nichts markiert.</summary>
    public GanglinienWahl? Wahl { get; private set; }

    /// <summary>Die Kennzahlen der Wahl; <c>null</c> = keine Grafik.</summary>
    public GanglinienKennzahlen? Kennzahlen { get; private set; }

    /// <summary>
    /// Die Identität der gezeigten Ganglinie — Katalog und Projektkopie gleichen Namens
    /// zeigen verschiedene Tabellen und dürfen keinen Bildvorrat teilen.
    /// </summary>
    public string Schluessel
        => Wahl is null ? "" : (Wahl.AusKatalog ? "K|" : "P|") + Wahl.GanglinieId + "|" + Wahl.Bezeichner;

    /// <summary>Der Name über der Grafik.</summary>
    public string Bezeichner => Wahl?.Bezeichner ?? "";

    /// <summary>Holt die Kennzahlen zur neuen Wahl.</summary>
    public async Task LadenAsync(GanglinienWahl wahl, Func<GanglinienWahl, Task<GanglinienKennzahlen?>>? kennzahlen)
    {
        Wahl = wahl;
        Kennzahlen = null;
        if (kennzahlen is null) return;
        GanglinienKennzahlen? k = await kennzahlen(wahl);
        if (ReferenceEquals(Wahl, wahl)) Kennzahlen = k;      // eine spaetere Wahl gewinnt
    }

    /// <summary>Die Grafik fällt weg (Zeile entfernt, nichts mehr markiert).</summary>
    public void Verwerfen()
    {
        Wahl = null;
        Kennzahlen = null;
    }

    /// <summary>Der Bildauftrag der Grafik — die Wahl steckt schon darin.</summary>
    public Func<bool, Zeichenmodell?>? Bild(Func<GanglinienWahl, bool, Zeichenmodell?>? auftrag)
    {
        GanglinienWahl? wahl = Wahl;
        return auftrag is null || wahl is null ? null : sortiert => auftrag(wahl, sortiert);
    }
}

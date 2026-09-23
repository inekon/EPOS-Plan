using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Ein Wert des Stammblatts im LESEMODUS</b> (Konzept Administrationsdialoge, Stufe 4:
/// V13 „Auslieferungssätze als Text", V9 Gruppe „Herkunft").
///
/// <para><b>Warum als Daten und nicht als gesperrtes Feld.</b> Ein gesperrtes
/// Eingabefeld sagt „hier könntest du tippen, nur jetzt nicht" — ein Auslieferungssatz
/// ist aber nie zu ändern, und eine Herkunftsangabe (Quelle, Importdatum) war nie ein
/// Feld. Der Wirt reicht deshalb Beschriftung, Wert und Einheit herein, und
/// <see cref="Stammblattwerte"/> zeichnet sie als Liste aus Name und Wert.</para>
///
/// <para>Ein <b>Abschnitt</b> (<see cref="IstAbschnitt"/>) ist eine leise
/// Zwischenüberschrift ohne Wert — die Gruppen des Modulprofils (Gerät, Eingang,
/// Wirkungsgrad) bleiben damit auch im Lesemodus erkennbar.</para>
/// </summary>
/// <param name="Name">Die Beschriftung, ohne Doppelpunkt.</param>
/// <param name="Wert">Der fertig formatierte Wert; leer = „—".</param>
/// <param name="Einheit">Die Einheit hinter dem Wert; leer = keine.</param>
public sealed record Stammblattwert(string Name, string Wert, string Einheit = "")
{
    /// <summary>Eine Zwischenüberschrift statt eines Werts.</summary>
    public bool IstAbschnitt { get; init; }

    /// <summary>Eine Zwischenüberschrift — leise, über die ganze Breite.</summary>
    public static Stammblattwert Abschnitt(string titel) => new(titel ?? "", "") { IstAbschnitt = true };

    /// <summary>
    /// Was im Blatt steht: der Wert samt Einheit, ohne Wert der Halbgeviertstrich — die
    /// Einheit fällt dann weg, sonst stünde „— kW" da.
    /// </summary>
    public string Anzeige
    {
        get
        {
            string wert = (Wert ?? "").Trim();
            if (wert.Length == 0 || wert == ParameterVerwendung.LEER) return ParameterVerwendung.LEER;
            return string.IsNullOrEmpty(Einheit) ? wert : wert + " " + Einheit;
        }
    }

    /// <summary>
    /// Die Beschriftung ohne den Doppelpunkt der Feldbeschriftung — Feldtexte des Hauses
    /// tragen ihn („Name:"), eine Werteliste nicht.
    /// </summary>
    public static string OhneDoppelpunkt(string text)
    {
        string t = (text ?? "").Trim();
        return t.EndsWith(':') ? t[..^1].TrimEnd() : t;
    }

    /// <summary>Eine Liste ohne Einträge — der Rückfall ohne Gaben.</summary>
    public static IReadOnlyList<Stammblattwert> Keine { get; } = System.Array.Empty<Stammblattwert>();
}

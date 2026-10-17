using WindowsFormsApplication1;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>„Vergleichen…" der Projektzeilen eines Ganglinien-Projektdialogs</b> (KA-E-19): Strom-, Solar- und
/// PV-Ganglinie stellen die angekreuzten Projektzeilen mit den Kennzahlen ihrer Grafik nebeneinander
/// (<c>Katalogliste.VergleichenWerte</c>) — Jahresarbeit, Spitze und Vollbenutzungsstunden in derselben
/// Schreibweise wie Wärmebedarf extern. Keine eigene Rechnung: Die Zahlen kommen über den Kennzahlen-Delegat
/// des Wirts (<c>GanglinienAuswertungCtrl</c>), derselbe Weg wie die Grafik der Detailzeile.
/// </summary>
public static class Ganglinienvergleich
{
    /// <summary>Eine Feldbeschriftung ohne Doppelpunkt am Ende („Jahresarbeit:“ → „Jahresarbeit“).</summary>
    public static string Beschriftung(string text) => (text ?? "").Trim().TrimEnd(':').TrimEnd();

    /// <summary>
    /// Die drei Kennzahlen einer Ganglinie als Vergleichswerte; ohne Kennzahlen bleiben die Werte leer.
    /// Die Jahresarbeit steht in der Anzeigeeinheit des Dialogs (<c>Energieeinheit.FormatiereJahreswert</c>).
    /// </summary>
    public static void Kennzahlen(List<(string Beschriftung, string Wert)> werte, GanglinienKennzahlen? k,
                                  Energieeinheit einheit, string labelJahresarbeit, string labelSpitze,
                                  string labelVollbenutzung, string einheitKw, string einheitStunden)
    {
        ArgumentNullException.ThrowIfNull(werte);
        ArgumentNullException.ThrowIfNull(einheit);
        werte.Add((Beschriftung(labelJahresarbeit),
                   k is null ? "" : einheit.FormatiereJahreswert(einheit.AusMWh(k.JahresarbeitMwh)) + " " + einheit.Text));
        werte.Add((Beschriftung(labelSpitze), k is null ? "" : Satzangaben.Zahl(k.SpitzeKw, einheitKw)));
        werte.Add((Beschriftung(labelVollbenutzung),
                   k?.VollbenutzungsstundenH is double h ? Satzangaben.Zahl(h, einheitStunden) : ""));
    }

    /// <summary>
    /// Liest je Projektzeile die Kennzahlen über die Fangstelle (<c>Katalogladung.SatzAsync</c>) und baut ihre
    /// Vergleichswerte. Wirft das Lesen eines Satzes, endet der Lauf mit dessen Grund (der Wirt meldet ihn im Banner)
    /// und ohne Sätze. Ohne Kennzahlen-Delegat stehen die Zeilen mit leeren Kennzahlen da.
    /// </summary>
    public static async Task<(IReadOnlyList<(string Name, IReadOnlyList<(string Beschriftung, string Wert)> Werte)> Saetze, string Fehler)> SaetzeAsync<TZeile>(
        IEnumerable<TZeile> zeilen, Func<TZeile, string> name, Func<TZeile, GanglinienWahl> wahl,
        Func<GanglinienWahl, Task<GanglinienKennzahlen?>>? kennzahlen,
        Func<TZeile, GanglinienKennzahlen?, IReadOnlyList<(string Beschriftung, string Wert)>> werte)
    {
        ArgumentNullException.ThrowIfNull(zeilen);
        var saetze = new List<(string Name, IReadOnlyList<(string Beschriftung, string Wert)> Werte)>();
        foreach (TZeile z in zeilen)
        {
            GanglinienKennzahlen? k = null;
            if (kennzahlen is not null)
            {
                GanglinienWahl w = wahl(z);
                (k, string grund) = await Katalogladung.SatzAsync(() => kennzahlen(w), w.Bezeichner);
                if (grund.Length > 0) return (Array.Empty<(string, IReadOnlyList<(string, string)>)>(), grund);
            }
            saetze.Add((name(z), werte(z, k)));
        }
        return (saetze, "");
    }
}

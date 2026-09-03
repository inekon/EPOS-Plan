using System.Globalization;

namespace EPOS.UI.Standards;

/// <summary>
/// Zahlen lesen und schreiben - dieselbe Regel wie im Bestand.
///
/// <para>
/// Wortgleiche Uebernahme von <c>WindowsFormsApplication1/Program.cs</c>
/// (<c>ZahlParsen</c>, <c>GanzzahlParsen</c>): Dezimal-Komma ODER -Punkt,
/// invariant geparst, KEIN Tausendertrennzeichen. "1.234,5" wird bewusst
/// abgelehnt, statt wie <c>double.Parse(CurrentCulture)</c> still zu 12345 zu
/// werden. Bei Ganzzahlen sind Komma und Punkt ueberhaupt keine gueltigen
/// Zeichen - es geht um Stueckzahlen, Tage, Nutzungsdauern und ganze Grad.
/// </para>
/// <para>
/// Die Regel gehoert hierher und nicht in die einzelnen Felder, weil sie in
/// beiden Richtungen gilt: Was <see cref="ZahlParsen"/> annimmt, muss
/// <see cref="Anzeigetext"/> auch wieder erzeugen koennen.
/// </para>
/// </summary>
public static class Zahlen
{
    /// <summary>
    /// Parst eine Dezimalzahl mit Komma oder Punkt. Ein Text mit
    /// Tausendertrennzeichen ("1.234,5") ist ungueltig.
    /// </summary>
    public static bool ZahlParsen(string? szText, out double dWert)
    {
        dWert = 0.0;
        if (string.IsNullOrEmpty(szText)) return false;
        string sz = szText.Trim().Replace(',', '.');
        return double.TryParse(sz, NumberStyles.Float, CultureInfo.InvariantCulture, out dWert);
    }

    /// <summary>
    /// Ganzzahl-Gegenstueck zu <see cref="ZahlParsen"/>: invariant geparst,
    /// Komma und Punkt sind keine gueltigen Zeichen.
    /// </summary>
    public static bool GanzzahlParsen(string? szText, out int nWert)
    {
        nWert = 0;
        if (string.IsNullOrEmpty(szText)) return false;
        return int.TryParse(szText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out nWert);
    }

    /// <summary>
    /// Der Anzeigetext einer Dezimalzahl: Komma als Trennzeichen, kein
    /// Tausenderpunkt - so, wie <see cref="ZahlParsen"/> ihn wieder annimmt.
    /// </summary>
    /// <param name="nNachkommastellen">Feste Stellenzahl; <c>null</c> = so
    /// genau wie noetig.</param>
    public static string Anzeigetext(double? dWert, int? nNachkommastellen = null)
    {
        if (!dWert.HasValue) return "";
        string szRoh = nNachkommastellen.HasValue
            ? dWert.Value.ToString("F" + nNachkommastellen.Value.ToString(CultureInfo.InvariantCulture),
                                   CultureInfo.InvariantCulture)
            : dWert.Value.ToString(CultureInfo.InvariantCulture);
        return szRoh.Replace('.', ',');
    }

    /// <summary>Anzeigetext einer Ganzzahl - ohne jedes Trennzeichen.</summary>
    public static string Anzeigetext(int? nWert)
    {
        return nWert.HasValue ? nWert.Value.ToString(CultureInfo.InvariantCulture) : "";
    }
}

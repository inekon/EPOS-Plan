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
    /// Hoechstens so viele Nachkommastellen zeigt ein Zahlenfeld ohne eigene
    /// Vorgabe (Auftrag <b>#224</b>, Anwenderwunsch 11.09.2026, Konzept
    /// „Stromspeicher-Dialoge" 7.8).
    /// </summary>
    /// <remarks>
    /// <para>Der Befund am Bildschirmfoto: Ladewirkungsgrad
    /// <c>94,86832980505137 %</c>, SoC-Obergrenze <c>89,99999999999999 %</c>,
    /// Energie-Ausgleichswert <c>0,31746000000002055</c>. Keine dieser Zahlen
    /// hat der Anwender getippt — sie entstehen als Wurzel, als Quotient oder
    /// als Mittelwert, und <c>double.ToString()</c> schreibt den
    /// Gleitkommarest aus.</para>
    /// <para><b>Gerundet wird die ANZEIGE, nicht der Stand.</b> Der
    /// gespeicherte Wert bleibt, bis jemand ins Feld schreibt; erst eine
    /// Eingabe aendert ihn.</para>
    /// </remarks>
    public const int HOECHSTE_NACHKOMMASTELLEN = 4;

    /// <summary>
    /// Der Anzeigetext einer Dezimalzahl: Komma als Trennzeichen, kein
    /// Tausenderpunkt - so, wie <see cref="ZahlParsen"/> ihn wieder annimmt.
    /// </summary>
    /// <param name="dWert">Der Wert; <c>null</c> = leerer Text.</param>
    /// <param name="nNachkommastellen">Feste Stellenzahl; <c>null</c> = so
    /// genau wie noetig, aber hoechstens
    /// <see cref="HOECHSTE_NACHKOMMASTELLEN"/> Stellen.</param>
    public static string Anzeigetext(double? dWert, int? nNachkommastellen = null)
    {
        if (!dWert.HasValue) return "";
        string szRoh = nNachkommastellen.HasValue
            ? dWert.Value.ToString("F" + nNachkommastellen.Value.ToString(CultureInfo.InvariantCulture),
                                   CultureInfo.InvariantCulture)
            : dWert.Value.ToString(Hoechstformat, CultureInfo.InvariantCulture);
        return szRoh.Replace('.', ',');
    }

    /// <summary>
    /// Das Format ohne feste Stellenzahl: bis zu
    /// <see cref="HOECHSTE_NACHKOMMASTELLEN"/> Stellen, nachlaufende Nullen
    /// weg - <c>"0.####"</c>.
    /// </summary>
    /// <remarks>
    /// Es steht als Konstante da, weil es die Regel IST: Wer sie aendern will,
    /// aendert eine Zahl und keine Zeichenkette an sechzig Stellen.
    /// </remarks>
    private static readonly string Hoechstformat =
        "0." + new string('#', HOECHSTE_NACHKOMMASTELLEN);

    /// <summary>Anzeigetext einer Ganzzahl - ohne jedes Trennzeichen.</summary>
    public static string Anzeigetext(int? nWert)
    {
        return nWert.HasValue ? nWert.Value.ToString(CultureInfo.InvariantCulture) : "";
    }
}

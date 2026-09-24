namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die hergeleiteten Vorgaben der Wärmeübergabe</b> (Anlagenkopplung 8.4, H10), wie die Hülle
/// sie aus dem Kern liefert (<c>UebergabeHerleitungsquelle</c>) — die zwei Zahlen, die die Gruppe
/// „Wärmeübergabe" als Vorgabe neben die leeren Felder schreibt.
/// </summary>
/// <param name="AuslegungAussenC">Kältestes Tagesmittel der Klimareihe, abgerundet [°C]; <c>null</c> = keine Zahl.</param>
/// <param name="AuslegungsheizlastKw">Stationäre Heizlast des Gebäudes im Auslegungspunkt [kW]; <c>null</c> = keine Zahl.</param>
/// <param name="Befund">Warum es keine Zahl gibt (eine Prüfung des Kerns); leer mit Zahl.</param>
public sealed record UebergabeHerleitungDaten(double? AuslegungAussenC, double? AuslegungsheizlastKw, string Befund);

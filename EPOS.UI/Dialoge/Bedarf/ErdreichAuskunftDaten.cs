namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Erdreichauskunft der Bodenplatte</b> (DIN EN ISO 13370, Rechenweg RP2a; Vorgabe E65), wie die Hülle
/// sie aus dem Kern liefert (<c>Erdreichwiderstand.Bauteilsatz</c>) — für die Auskunftszeile neben dem Feld
/// „Wirksamer U-Wert Bodenplatte".
/// </summary>
/// <param name="BStrichM">Das charakteristische Bodenplattenmaß B′ [m]; <c>null</c> bei Vorgabe (dann rechnet kein B′).</param>
/// <param name="UgWM2K">Der Wärmedurchgangskoeffizient U_g der Bodenplatte samt Erdreich [W/(m²K)].</param>
/// <param name="Vorgabe">Gilt die Vorgabe <c>Erdreich_U_Wirksam</c> statt der Rechnung nach 13370?</param>
public sealed record ErdreichAuskunftDaten(double? BStrichM, double UgWM2K, bool Vorgabe);

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Was der Dialog <see cref="EnergietraegerVarianteDialog"/> zurueckgibt.
///
/// <para>
/// Der WinForms-Vorlaeufer <c>Views/Kosten/Form_Kosten_Auswahl</c> lieferte
/// neun oeffentliche Eigenschaften, von denen sechs
/// (<c>SelectedGroupCode</c>, <c>SelectedBillingUnit</c>, <c>SelectedHi</c>,
/// <c>SelectedHs</c>, <c>SelectedBrennstoffCode</c>, <c>SelectedConvID</c>)
/// nicht aus der Eingabe stammten, sondern aus einer Datenbankabfrage, die der
/// Dialog beim Schliessen selbst ausfuehrte
/// (<c>FetchAdditionalData</c>, <c>GetConvID</c>).
/// </para>
/// <para>
/// Diese Abfrage gehoert nicht in die Oberflaeche. Der Dialog gibt deshalb nur
/// zurueck, was der Anwender tatsaechlich eingegeben hat; die Zusatzdaten holt
/// die Huelle anhand der <see cref="BrennstoffId"/> nach.
/// </para>
/// </summary>
/// <param name="BrennstoffId">Id des gewaehlten Energietraegers (frueher <c>SelectedBrennstoffID</c>).</param>
/// <param name="BrennstoffName">Bezeichner des gewaehlten Energietraegers (frueher <c>SelectedCode</c>).</param>
/// <param name="VariantenName">Die vom Anwender vergebene Bezeichnung der Variante (frueher <c>SelectedName</c>).</param>
public sealed record EnergietraegerVarianteErgebnis(
    int BrennstoffId,
    string BrennstoffName,
    string VariantenName);

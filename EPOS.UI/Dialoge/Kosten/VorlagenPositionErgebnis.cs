namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Was der Dialog <see cref="VorlagenPositionDialog"/> zurueckgibt (iU9-W1.1).
///
/// <para>
/// Der WinForms-Vorlaeufer <c>Views/Kosten/Form_VorlagenPosition</c> schrieb in
/// <c>btnOk_Click</c> unmittelbar in die uebergebene
/// <c>KostenVorlagenPosition</c>. Das geht in einer plattformfreien Komponente
/// nicht: Sie kennt die Fachklasse des Kerns nicht und soll sie auch nicht
/// kennen. Der Dialog liefert deshalb einen Satz reiner Werte; das Eintragen in
/// die Position und das Speichern bleiben beim Aufrufer - genau dort, wo sie
/// schon vorher standen (<c>Form_KostenKomponente.Zeile_EditorAngefordert</c>).
/// </para>
/// </summary>
/// <param name="Bezeichnung">Die getrimmte Bezeichnung der Position.</param>
/// <param name="KostenartId">Id der gewaehlten Kostenart - der Index in der Liste,
/// die der Aufrufer als <c>Kostenarten</c> hereingegeben hat. Die Uebersetzung in
/// den Persistenzwert (<c>DbWerte.KOSTENART_*</c>) macht der Aufrufer.</param>
/// <param name="IstErloes">Erlös/Zuschuss, also negativer Ausweis.</param>
/// <param name="EmpfehlungVon">Untere Empfehlung; <c>null</c> = nicht gepflegt.</param>
/// <param name="EmpfehlungBis">Obere Empfehlung; <c>null</c> = nicht gepflegt.</param>
public sealed record VorlagenPositionErgebnis(
    string Bezeichnung,
    int KostenartId,
    bool IstErloes,
    double? EmpfehlungVon,
    double? EmpfehlungBis);

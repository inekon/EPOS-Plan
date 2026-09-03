namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Was der Dialog <see cref="CaseEingabeDialog"/> zurueckgibt (iU9-W1.3).
///
/// <para>
/// Die Werte sind IMMER Betraege, nie Prozente — genau wie
/// <c>Form_CaseEingabe.btn_OK_Click</c> es hinterliess (KD6 § 11, KL9: keine
/// neuen Spalten). Der Prozentmodus ist eine Eingabehilfe des Dialogs; er endet
/// an dieser Stelle.
/// </para>
/// </summary>
/// <param name="BestCase">Bester Fall [€]; 0 = nicht gepflegt.</param>
/// <param name="WorstCase">Schlechtester Fall [€]; 0 = nicht gepflegt.</param>
/// <param name="BestNutzungsdauer">Nutzungsdauer im besten Fall [a].</param>
/// <param name="WorstNutzungsdauer">Nutzungsdauer im schlechtesten Fall [a].</param>
/// <param name="StartJahr">Startjahr der Position (FK10): 0 = sofort (t0), sonst
/// das Jahr X ≥ 2. Der Wert 1 wird wie 0 behandelt — so hielt es
/// <c>btn_OK_Click</c>.</param>
/// <param name="IstZuschuss">Zuschuss-Kennzeichen (K5). Ohne angebotenen
/// Schalter unveraendert wie hereingegeben.</param>
public sealed record CaseEingabeErgebnis(
    double BestCase,
    double WorstCase,
    double BestNutzungsdauer,
    double WorstNutzungsdauer,
    int StartJahr,
    bool IstZuschuss);
